#if LOOTLOCKER_ENABLE_PRESENCE
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using LootLocker.Requests;

namespace LootLocker
{
    #region Enums and Data Types

    /// <summary>
    /// Possible WebSocket connection states
    /// </summary>
    public enum LootLockerPresenceConnectionState
    {
        Disconnected,
        Initializing,
        Connecting,
        Connected,
        Authenticating,
        Active,
        Reconnecting,
        Failed
    }

    /// <summary>
    /// Types of presence messages that the client can receive
    /// </summary>
    public enum LootLockerPresenceMessageType
    {
        Authentication,
        Pong,
        Error,
        Unknown
    }

    #endregion

    #region Request and Response Models

    /// <summary>
    /// Authentication request sent to the Presence WebSocket
    /// </summary>
    [Serializable]
    public class LootLockerPresenceAuthRequest
    {
        public string token { get; set; }

        public LootLockerPresenceAuthRequest(string sessionToken)
        {
            token = sessionToken;
        }
    }

    /// <summary>
    /// Status update request for Presence
    /// </summary>
    [Serializable]
    public class LootLockerPresenceStatusRequest
    {
        public string status { get; set; }
        public Dictionary<string, string> metadata { get; set; }

        public LootLockerPresenceStatusRequest(string status, Dictionary<string, string> metadata = null)
        {
            this.status = status;
            this.metadata = metadata;
        }
    }

    /// <summary>
    /// Ping message to keep the WebSocket connection alive
    /// </summary>
    [Serializable]
    public class LootLockerPresencePingRequest
    {
        public string type { get; set; } = "ping";
        public DateTime timestamp { get; set; }

        public LootLockerPresencePingRequest()
        {
            timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Base response for Presence WebSocket messages
    /// </summary>
    [Serializable]
    public class LootLockerPresenceResponse
    {
        public string type { get; set; }
        public string status { get; set; }
        public string metadata { get; set; }
    }

    /// <summary>
    /// Authentication response from the Presence WebSocket
    /// </summary>
    [Serializable] 
    public class LootLockerPresenceAuthResponse : LootLockerPresenceResponse
    {
        public bool authenticated { get; set; }
        public string message { get; set; }
    }

    /// <summary>
    /// Ping response from the server
    /// </summary>
    [Serializable]
    public class LootLockerPresencePingResponse : LootLockerPresenceResponse
    {
        public DateTime timestamp { get; set; }
    }

    /// <summary>
    /// Statistics about the presence connection to LootLocker
    /// </summary>
    [Serializable]
    public class LootLockerPresenceConnectionStats
    {
        /// <summary>
        /// The player ULID this connection belongs to
        /// </summary>
        public string playerUlid { get; set; }

        /// <summary>
        /// Current connection state
        /// </summary>
        public LootLockerPresenceConnectionState connectionState { get; set; }

        /// <summary>
        /// The last status that was sent to the server (e.g., "online", "in_game", "away")
        /// </summary>
        public string lastSentStatus { get; set; }

        /// <summary>
        /// Current one-way latency to LootLocker in milliseconds
        /// </summary>
        public float currentLatencyMs { get; set; }

        /// <summary>
        /// Average one-way latency over the last few pings in milliseconds
        /// </summary>
        public float averageLatencyMs { get; set; }

        /// <summary>
        /// Minimum recorded one-way latency in milliseconds
        /// </summary>
        public float minLatencyMs { get; set; }

        /// <summary>
        /// Maximum recorded one-way latency in milliseconds
        /// </summary>
        public float maxLatencyMs { get; set; }

        /// <summary>
        /// Total number of pings sent
        /// </summary>
        public int totalPingsSent { get; set; }

        /// <summary>
        /// Total number of pongs received
        /// </summary>
        public int totalPongsReceived { get; set; }

        /// <summary>
        /// When the connection was established
        /// </summary>
        public DateTime connectionStartTime { get; set; }

        /// <summary>
        /// How long the connection has been active
        /// </summary>
        public TimeSpan connectionDuration => DateTime.UtcNow - connectionStartTime;

        /// <summary>
        /// Returns a formatted string representation of the connection statistics
        /// </summary>
        public override string ToString()
        {
            return $"LootLocker Presence Connection Statistics\n" +
                   $"  Player ID: {playerUlid}\n" +
                   $"  Connection State: {connectionState}\n" +
                   $"  Last Status: {lastSentStatus}\n" +
                   $"  Current Latency: {currentLatencyMs:F1} ms\n" +
                   $"  Average Latency: {averageLatencyMs:F1} ms\n" +
                   $"  Min/Max Latency: {minLatencyMs:F1} ms / {maxLatencyMs:F1} ms\n" +
                   $"  Pings Sent/Received: {totalPingsSent}/{totalPongsReceived}\n" +
                   $"  Connection Duration: {connectionDuration:hh\\:mm\\:ss}";
        }
    }

    #endregion

    #region Event Delegates

    /// <summary>
    /// Delegate for connection state changes
    /// </summary>
    public delegate void LootLockerPresenceConnectionStateChanged(string playerUlid, LootLockerPresenceConnectionState previousState, LootLockerPresenceConnectionState newState, string error = null);
    
    /// <summary>
    /// Delegate for ping responses
    /// </summary>
    public delegate void LootLockerPresencePingReceived(string playerUlid, LootLockerPresencePingResponse response);

    /// <summary>
    /// Delegate for presence operation responses (connect, disconnect, status update)
    /// </summary>
    public delegate void LootLockerPresenceCallback(bool success, string error = null);

    #endregion

    /// <summary>
    /// Individual WebSocket client for LootLocker Presence feature
    /// Managed internally by LootLockerPresenceManager
    /// </summary>
    public class LootLockerPresenceClient : MonoBehaviour, IDisposable
    {
        #region Private Fields

        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        private readonly ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();

        private LootLockerPresenceConnectionState connectionState = LootLockerPresenceConnectionState.Disconnected;
        private string playerUlid;
        private string sessionToken;
        private string lastSentStatus; // Track the last status sent to the server
        private static string webSocketUrl;

        // Connection settings
        private const float PING_INTERVAL = 20f;
        private const float RECONNECT_DELAY = 5f;
        private const int MAX_RECONNECT_ATTEMPTS = 5;

        // State tracking
        private bool shouldReconnect = true;
        private int reconnectAttempts = 0;
        private Coroutine pingCoroutine;
        private Coroutine statusUpdateCoroutine; // Track active status update coroutine
        private bool isDestroying = false;
        private bool isDisposed = false;
        private bool isExpectedDisconnect = false; // Track if disconnect is expected (due to session end)
        private LootLockerPresenceCallback pendingConnectionCallback; // Store callback until authentication completes

        // Latency tracking
        private readonly Queue<DateTime> pendingPingTimestamps = new Queue<DateTime>();
        private readonly Queue<float> recentLatencies = new Queue<float>();
        private const int MAX_LATENCY_SAMPLES = 10;
        private LootLockerPresenceConnectionStats connectionStats = new LootLockerPresenceConnectionStats
        {
            minLatencyMs = float.MaxValue,
            maxLatencyMs = 0f
        };

        #endregion

        #region Public Events

        /// <summary>
        /// Event fired when the connection state changes
        /// </summary>
        public event System.Action<LootLockerPresenceConnectionState, LootLockerPresenceConnectionState, string> OnConnectionStateChanged;

        /// <summary>
        /// Event fired when a ping response is received
        /// </summary>
        public event System.Action<LootLockerPresencePingResponse> OnPingReceived;

        #endregion

        #region Public Properties

        /// <summary>
        /// Current connection state
        /// </summary>
        public LootLockerPresenceConnectionState ConnectionState => connectionState;

        /// <summary>
        /// Whether the client is connected and active (authenticated and operational)
        /// </summary>
        public bool IsConnectedAndAuthenticated => connectionState == LootLockerPresenceConnectionState.Active;

        /// <summary>
        /// Whether the client is currently connecting or reconnecting
        /// </summary>
        public bool IsConnecting => connectionState == LootLockerPresenceConnectionState.Connecting || 
                                   connectionState == LootLockerPresenceConnectionState.Reconnecting;

        /// <summary>
        /// Whether the client is currently authenticating
        /// </summary>
        public bool IsAuthenticating => connectionState == LootLockerPresenceConnectionState.Authenticating;

        /// <summary>
        /// The player ULID this client is associated with
        /// </summary>
        public string PlayerUlid => playerUlid;

        /// <summary>
        /// The last status that was sent to the server (e.g., "online", "in_game", "away")
        /// </summary>
        public string LastSentStatus => lastSentStatus;

        /// <summary>
        /// Get connection statistics including latency to LootLocker
        /// </summary>
        public LootLockerPresenceConnectionStats ConnectionStats => connectionStats;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // Process any messages that have been received in the main Unity thread
            while (receivedMessages.TryDequeue(out string message))
            {
                ProcessReceivedMessage(message);
            }
        }

        private void OnDestroy()
        {
            isDestroying = true;
            Dispose();
        }

        /// <summary>
        /// Properly dispose of all resources including WebSocket connections
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;
            
            isDisposed = true;
            shouldReconnect = false;

            if (pingCoroutine != null)
            {
                StopCoroutine(pingCoroutine);
                pingCoroutine = null;
            }

            // Use synchronous cleanup for dispose to ensure immediate resource release
            CleanupConnectionSynchronous();
            
            // Clear all queues
            while (receivedMessages.TryDequeue(out _)) { }
            pendingPingTimestamps.Clear();
            recentLatencies.Clear();
        }
        
        /// <summary>
        /// Synchronous cleanup for disposal scenarios
        /// </summary>
        private void CleanupConnectionSynchronous()
        {
            try
            {
                // Cancel any ongoing operations
                cancellationTokenSource?.Cancel();
                
                // Close WebSocket if open
                if (webSocket?.State == WebSocketState.Open)
                {
                    try
                    {
                        // Close with a short timeout for disposal
                        var closeTask = webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                            "Client disposing", CancellationToken.None);
                        
                        // Don't wait indefinitely during disposal
                        if (!closeTask.Wait(TimeSpan.FromSeconds(2)))
                        {
                            LootLockerLogger.Log("WebSocket close timed out during disposal", LootLockerLogger.LogLevel.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        LootLockerLogger.Log($"Error closing WebSocket during disposal: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                    }
                }
                
                // Always dispose resources
                webSocket?.Dispose();
                webSocket = null;
                
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error during synchronous cleanup: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Initialize the presence client with player information
        /// </summary>
        internal void Initialize(string playerUlid, string sessionToken)
        {
            this.playerUlid = playerUlid;
            this.sessionToken = sessionToken;
            ChangeConnectionState(LootLockerPresenceConnectionState.Initializing);
        }

        /// <summary>
        /// Connect to the Presence WebSocket
        /// </summary>
        internal void Connect(LootLockerPresenceCallback onComplete = null)
        {
            if (isDisposed)
            {
                onComplete?.Invoke(false, "Client has been disposed");
                return;
            }
            
            if (IsConnecting || IsConnectedAndAuthenticated)
            {
                onComplete?.Invoke(false, "Already connected or connecting");
                return;
            }

            if (string.IsNullOrEmpty(sessionToken))
            {
                ChangeConnectionState(LootLockerPresenceConnectionState.Failed, "No session token provided");
                onComplete?.Invoke(false, "No session token provided");
                return;
            }

            shouldReconnect = true;
            reconnectAttempts = 0;
            pendingConnectionCallback = onComplete;

            StartCoroutine(ConnectCoroutine());
        }

        /// <summary>
        /// Disconnect from the Presence WebSocket
        /// </summary>
        internal void Disconnect(LootLockerPresenceCallback onComplete = null)
        {
            // Prevent multiple disconnect attempts
            if (isDestroying || isDisposed)
            {
                onComplete?.Invoke(true, null);
                return;
            }
            
            // Check if already disconnected
            if (connectionState == LootLockerPresenceConnectionState.Disconnected ||
                connectionState == LootLockerPresenceConnectionState.Failed)
            {
                LootLockerLogger.Log($"Presence client already in disconnected state: {connectionState}", LootLockerLogger.LogLevel.Debug);
                onComplete?.Invoke(true, null);
                return;
            }
            
            // Mark as expected disconnect to prevent error logging for server-side aborts
            isExpectedDisconnect = true;
            shouldReconnect = false;
            StartCoroutine(DisconnectCoroutine(onComplete));
        }

        /// <summary>
        /// Send a status update to the Presence service
        /// </summary>
        internal void UpdateStatus(string status, Dictionary<string, string> metadata = null, LootLockerPresenceCallback onComplete = null)
        {
            if (!IsConnectedAndAuthenticated)
            {
                // Stop any existing status update coroutine before starting a new one
                if (statusUpdateCoroutine != null)
                {
                    StopCoroutine(statusUpdateCoroutine);
                    statusUpdateCoroutine = null;
                }
                
                statusUpdateCoroutine = StartCoroutine(WaitForConnectionAndUpdateStatus(status, metadata, onComplete));
                return;
            }

            // Track the status being sent
            lastSentStatus = status;
            connectionStats.lastSentStatus = status;

            var statusRequest = new LootLockerPresenceStatusRequest(status, metadata);
            StartCoroutine(SendMessageCoroutine(LootLockerJson.SerializeObject(statusRequest), onComplete));
        }

        private IEnumerator WaitForConnectionAndUpdateStatus(string status, Dictionary<string, string> metadata = null, LootLockerPresenceCallback onComplete = null)
        {
            int maxWaitTimes = 10;
            int waitCount = 0;
            while(!IsConnectedAndAuthenticated && waitCount < maxWaitTimes)
            {
                yield return new WaitForSeconds(0.1f);
                waitCount++;
            }
            
            // Clear the tracked coroutine reference when we're done
            statusUpdateCoroutine = null;
            
            if (IsConnectedAndAuthenticated)
            {
                UpdateStatus(status, metadata, onComplete);
            }
            else
            {
                onComplete?.Invoke(false, "Not connected and authenticated after wait");
            }
        }

        /// <summary>
        /// Send a ping to test the connection
        /// </summary>
        internal void SendPing(LootLockerPresenceCallback onComplete = null)
        {
            LootLockerLogger.Log($"SendPing called. Connected: {IsConnectedAndAuthenticated}, State: {connectionState}", LootLockerLogger.LogLevel.Debug);
            
            if (!IsConnectedAndAuthenticated)
            {
                LootLockerLogger.Log("Not sending ping - not connected and authenticated", LootLockerLogger.LogLevel.Debug);
                onComplete?.Invoke(false, "Not connected and authenticated");
                return;
            }

            var pingRequest = new LootLockerPresencePingRequest();
            LootLockerLogger.Log($"Sending ping with timestamp {pingRequest.timestamp}", LootLockerLogger.LogLevel.Debug);
            
            // Track the ping timestamp for latency calculation
            pendingPingTimestamps.Enqueue(pingRequest.timestamp);

            // Clean up old pending pings (in case pongs are lost)
            while (pendingPingTimestamps.Count > 10)
            {
                pendingPingTimestamps.Dequeue();
            }

            StartCoroutine(SendMessageCoroutine(LootLockerJson.SerializeObject(pingRequest), (success, error) => {
                if (success)
                {
                    // Only count the ping as sent if it was actually sent successfully
                    connectionStats.totalPingsSent++;
                }
                else
                {
                    // Remove the timestamp since the ping failed to send
                    if (pendingPingTimestamps.Count > 0)
                    {
                        // Remove the most recent timestamp (the one we just added)
                        var tempQueue = new Queue<DateTime>();
                        while (pendingPingTimestamps.Count > 1)
                        {
                            tempQueue.Enqueue(pendingPingTimestamps.Dequeue());
                        }
                        if (pendingPingTimestamps.Count > 0) pendingPingTimestamps.Dequeue(); // Remove the failed ping
                        while (tempQueue.Count > 0)
                        {
                            pendingPingTimestamps.Enqueue(tempQueue.Dequeue());
                        }
                    }
                }
                onComplete?.Invoke(success, error);
            }));
        }

        #endregion

        #region Private Methods

        private IEnumerator ConnectCoroutine()
        {
            if (isDestroying || isDisposed || string.IsNullOrEmpty(sessionToken))
            {
                HandleConnectionError("Invalid state or session token");
                yield break;
            }

            // Set state
            ChangeConnectionState(reconnectAttempts > 0 ? 
                LootLockerPresenceConnectionState.Reconnecting : 
                LootLockerPresenceConnectionState.Connecting);

            // Cleanup any existing connections
            yield return StartCoroutine(CleanupConnectionCoroutine());

            // Initialize WebSocket
            bool initSuccess = InitializeWebSocket();
            if (!initSuccess)
            {
                HandleConnectionError("Failed to initialize WebSocket");
                yield break;
            }

            // Connect with timeout
            bool connectionSuccess = false;
            string connectionError = null;
            yield return StartCoroutine(ConnectWebSocketCoroutine((success, error) => {
                connectionSuccess = success;
                connectionError = error;
            }));

            if (!connectionSuccess)
            {
                HandleConnectionError(connectionError ?? "Connection failed");
                yield break;
            }

            ChangeConnectionState(LootLockerPresenceConnectionState.Connected);
            reconnectAttempts = 0;

            InitializeConnectionStats();

            // Start listening for messages
            StartCoroutine(ListenForMessagesCoroutine());

            // Send authentication
            yield return StartCoroutine(AuthenticateCoroutine());
        }

        private bool InitializeWebSocket()
        {
            try
            {
                webSocket = new ClientWebSocket();
                cancellationTokenSource = new CancellationTokenSource();

                // Cache base URL on first use to avoid repeated string operations
                if (string.IsNullOrEmpty(webSocketUrl))
                {
                    webSocketUrl = LootLockerConfig.current.webSocketBaseUrl + "/presence/v1";
                }
                return true;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Failed to initialize WebSocket: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                return false;
            }
        }

        private IEnumerator ConnectWebSocketCoroutine(LootLockerPresenceCallback onComplete)
        {
            var uri = new Uri(webSocketUrl);
            LootLockerLogger.Log($"Connecting to Presence WebSocket: {uri}", LootLockerLogger.LogLevel.Debug);

            // Start WebSocket connection in background
            var connectTask = webSocket.ConnectAsync(uri, cancellationTokenSource.Token);
            
            // Wait for connection with timeout
            float timeoutSeconds = 10f;
            float elapsed = 0f;
            
            while (!connectTask.IsCompleted && elapsed < timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!connectTask.IsCompleted || connectTask.IsFaulted)
            {
                string error = connectTask.Exception?.Message ?? "Connection timeout";
                onComplete?.Invoke(false, error);
            }
            else
            {
                onComplete?.Invoke(true);
            }
        }

        private void InitializeConnectionStats()
        {
            connectionStats.playerUlid = this.playerUlid;
            connectionStats.connectionState = this.connectionState;
            connectionStats.lastSentStatus = this.lastSentStatus;
            connectionStats.connectionStartTime = DateTime.UtcNow;
            connectionStats.totalPingsSent = 0;
            connectionStats.totalPongsReceived = 0;
            connectionStats.currentLatencyMs = 0f;
            connectionStats.averageLatencyMs = 0f;
            connectionStats.minLatencyMs = float.MaxValue;
            connectionStats.maxLatencyMs = 0f;
            recentLatencies.Clear();
            pendingPingTimestamps.Clear();
        }

        private void HandleConnectionError(string errorMessage)
        {
            LootLockerLogger.Log($"Failed to connect to Presence WebSocket: {errorMessage}", LootLockerLogger.LogLevel.Warning);
            ChangeConnectionState(LootLockerPresenceConnectionState.Failed, errorMessage);
            
            // Invoke pending callback on error
            pendingConnectionCallback?.Invoke(false, errorMessage);
            pendingConnectionCallback = null;
            
            if (shouldReconnect && reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
            {
                StartCoroutine(ScheduleReconnectCoroutine());
            }
        }

        private void HandleAuthenticationError(string errorMessage)
        {
            LootLockerLogger.Log($"Failed to authenticate Presence WebSocket: {errorMessage}", LootLockerLogger.LogLevel.Warning);
            ChangeConnectionState(LootLockerPresenceConnectionState.Failed, errorMessage);
            
            // Invoke pending callback on error
            pendingConnectionCallback?.Invoke(false, errorMessage);
            pendingConnectionCallback = null;
        }

        private IEnumerator DisconnectCoroutine(LootLockerPresenceCallback onComplete = null)
        {
            // Don't attempt disconnect if already destroyed
            if (isDestroying || isDisposed)
            {
                onComplete?.Invoke(true, null);
                yield break;
            }

            // Stop ping routine
            if (pingCoroutine != null)
            {
                StopCoroutine(pingCoroutine);
                pingCoroutine = null;
            }

            // Stop any pending status update routine
            if (statusUpdateCoroutine != null)
            {
                StopCoroutine(statusUpdateCoroutine);
                statusUpdateCoroutine = null;
            }

            // Close WebSocket connection
            bool closeSuccess = true;
            if (webSocket != null)
            {
                yield return StartCoroutine(CloseWebSocketCoroutine((success) => closeSuccess = success));
            }

            // Always cleanup regardless of close success
            yield return StartCoroutine(CleanupConnectionCoroutine());

            ChangeConnectionState(LootLockerPresenceConnectionState.Disconnected);
            
            // Reset expected disconnect flag
            isExpectedDisconnect = false;
            
            onComplete?.Invoke(closeSuccess, closeSuccess ? null : "Error during disconnect");
        }

        private IEnumerator CloseWebSocketCoroutine(System.Action<bool> onComplete)
        {
            bool closeSuccess = true;
            System.Threading.Tasks.Task closeTask = null;
            
            try
            {
                // Check if WebSocket is already closed/aborted by server
                if (webSocket.State == WebSocketState.Aborted || 
                    webSocket.State == WebSocketState.Closed)
                {
                    LootLockerLogger.Log($"WebSocket already closed by server (state: {webSocket.State}), cleanup complete", LootLockerLogger.LogLevel.Debug);
                    onComplete?.Invoke(true);
                    yield break;
                }
                
                // Only attempt to close if the WebSocket is in a valid state for closing
                if (webSocket.State == WebSocketState.Open || 
                    webSocket.State == WebSocketState.CloseReceived ||
                    webSocket.State == WebSocketState.CloseSent)
                {
                    // Don't cancel the token before close - let the close complete normally
                    closeTask = webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                        "Client disconnecting", CancellationToken.None);
                }
                else
                {
                    LootLockerLogger.Log($"WebSocket in unexpected state {webSocket.State}, treating as already closed", LootLockerLogger.LogLevel.Debug);
                    onComplete?.Invoke(true);
                    yield break;
                }
            }
            catch (Exception ex)
            {
                // If we get an exception during close (like WebSocket aborted), treat it as already closed
                if (ex.Message.Contains("invalid state") || ex.Message.Contains("Aborted"))
                {
                    if (isExpectedDisconnect)
                    {
                        LootLockerLogger.Log($"WebSocket was closed by server during session end - this is normal", LootLockerLogger.LogLevel.Debug);
                    }
                    else
                    {
                        LootLockerLogger.Log($"WebSocket was aborted by server unexpectedly: {ex.Message}", LootLockerLogger.LogLevel.Debug);
                    }
                    closeSuccess = true; // Treat server-side abort as successful close
                }
                else
                {
                    closeSuccess = false;
                    LootLockerLogger.Log($"Error during WebSocket disconnect: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                }
                
                onComplete?.Invoke(closeSuccess);
                yield break;
            }
            
            // Wait for close task completion outside of try-catch to allow yield
            if (closeTask != null)
            {
                float timeoutSeconds = 5f;
                float elapsed = 0f;
                
                while (!closeTask.IsCompleted && elapsed < timeoutSeconds)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                try
                {
                    if (closeTask.IsFaulted)
                    {
                        var exception = closeTask.Exception?.InnerException ?? closeTask.Exception;
                        if (exception?.Message.Contains("invalid state") == true || 
                            exception?.Message.Contains("Aborted") == true)
                        {
                            if (isExpectedDisconnect)
                            {
                                LootLockerLogger.Log("WebSocket close completed - session ended as expected", LootLockerLogger.LogLevel.Debug);
                            }
                            else
                            {
                                LootLockerLogger.Log($"WebSocket was aborted during close task: {exception.Message}", LootLockerLogger.LogLevel.Debug);
                            }
                            closeSuccess = true; // Treat server-side abort during close as successful
                        }
                        else
                        {
                            closeSuccess = false;
                            if (isExpectedDisconnect)
                            {
                                LootLockerLogger.Log($"Error during expected disconnect: {exception?.Message}", LootLockerLogger.LogLevel.Debug);
                            }
                            else
                            {
                                LootLockerLogger.Log($"Error during disconnect: {exception?.Message}", LootLockerLogger.LogLevel.Warning);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Catch any exceptions that occur while checking the task result
                    if (isExpectedDisconnect)
                    {
                        LootLockerLogger.Log($"Exception during expected disconnect task check: {ex.Message}", LootLockerLogger.LogLevel.Debug);
                    }
                    else
                    {
                        LootLockerLogger.Log($"Exception during disconnect task check: {ex.Message}", LootLockerLogger.LogLevel.Debug);
                    }
                    closeSuccess = true; // Treat exceptions during expected disconnect as success
                }
            }
            
            // Cancel operations after close is complete
            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error cancelling token source: {ex.Message}", LootLockerLogger.LogLevel.Debug);
            }
            
            onComplete?.Invoke(closeSuccess);
        }

        private IEnumerator CleanupConnectionCoroutine()
        {
            try
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;

                webSocket?.Dispose();
                webSocket = null;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error during cleanup: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
            
            yield return null;
        }

        private IEnumerator AuthenticateCoroutine()
        {
            if (webSocket?.State != WebSocketState.Open)
            {
                HandleAuthenticationError("WebSocket not open for authentication");
                yield break;
            }

            ChangeConnectionState(LootLockerPresenceConnectionState.Authenticating);

            var authRequest = new LootLockerPresenceAuthRequest(sessionToken);
            string jsonPayload = LootLockerJson.SerializeObject(authRequest);

            yield return StartCoroutine(SendMessageCoroutine(jsonPayload, (bool success, string error) => {
                if (!success) {
                    HandleAuthenticationError(error ?? "Failed to send authentication message");
                    return;
                }
            }));
        }

        private IEnumerator SendMessageCoroutine(string message, LootLockerPresenceCallback onComplete = null)
        {
            if (webSocket?.State != WebSocketState.Open || cancellationTokenSource?.Token.IsCancellationRequested == true)
            {
                onComplete?.Invoke(false, "WebSocket not connected");
                yield break;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(message);
            var sendTask = webSocket.SendAsync(new ArraySegment<byte>(buffer), 
                WebSocketMessageType.Text, true, cancellationTokenSource.Token);

            // Wait for send with timeout
            float timeoutSeconds = 5f;
            float elapsed = 0f;
            
            while (!sendTask.IsCompleted && elapsed < timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (sendTask.IsCompleted && !sendTask.IsFaulted)
            {
                LootLockerLogger.Log($"Sent Presence message: {message}", LootLockerLogger.LogLevel.Debug);
                onComplete?.Invoke(true);
            }
            else
            {
                string error = sendTask.Exception?.GetBaseException()?.Message ?? "Send timeout";
                LootLockerLogger.Log($"Failed to send Presence message: {error}", LootLockerLogger.LogLevel.Warning);
                onComplete?.Invoke(false, error);
            }
        }

        private IEnumerator ListenForMessagesCoroutine()
        {
            var buffer = new byte[4096];

            while (webSocket?.State == WebSocketState.Open && 
                   cancellationTokenSource?.Token.IsCancellationRequested == false)
            {
                var receiveTask = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), 
                    cancellationTokenSource.Token);

                // Wait for message
                while (!receiveTask.IsCompleted)
                {
                    yield return null;
                }

                if (receiveTask.IsFaulted)
                {
                    // Handle receive error
                    var exception = receiveTask.Exception?.GetBaseException();
                    if (exception is OperationCanceledException || exception is TaskCanceledException)
                    {
                        if (isExpectedDisconnect)
                        {
                            LootLockerLogger.Log("Presence WebSocket listening cancelled due to session end", LootLockerLogger.LogLevel.Debug);
                        }
                        else
                        {
                            LootLockerLogger.Log("Presence WebSocket listening cancelled", LootLockerLogger.LogLevel.Debug);
                        }
                    }
                    else
                    {
                        string errorMessage = exception?.Message ?? "Unknown error";
                        LootLockerLogger.Log($"Error listening for Presence messages: {errorMessage}", LootLockerLogger.LogLevel.Warning);
                        
                        // Only attempt reconnect for unexpected disconnects
                        if (shouldReconnect && reconnectAttempts < MAX_RECONNECT_ATTEMPTS && !isExpectedDisconnect)
                        {
                            // Use longer delay for server-side connection termination
                            bool isServerSideClose = errorMessage.Contains("remote party closed the WebSocket connection without completing the close handshake");
                            float reconnectDelay = isServerSideClose ? RECONNECT_DELAY * 2f : RECONNECT_DELAY;
                            
                            StartCoroutine(ScheduleReconnectCoroutine(reconnectDelay));
                        }
                    }
                    break;
                }

                var result = receiveTask.Result;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    receivedMessages.Enqueue(message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (isExpectedDisconnect)
                    {
                        LootLockerLogger.Log("Presence WebSocket closed by server during session end", LootLockerLogger.LogLevel.Debug);
                    }
                    else
                    {
                        LootLockerLogger.Log("Presence WebSocket closed by server", LootLockerLogger.LogLevel.Debug);
                    }
                    
                    // Notify manager that this client is disconnected so it can clean up
                    ChangeConnectionState(LootLockerPresenceConnectionState.Disconnected);
                    break;
                }
            }
        }

        private void ProcessReceivedMessage(string message)
        {
            try
            {
                LootLockerLogger.Log($"Received Presence message: {message}", LootLockerLogger.LogLevel.Debug);

                // Determine message type
                var messageType = DetermineMessageType(message);

                // Handle specific message types
                switch (messageType)
                {
                    case LootLockerPresenceMessageType.Authentication:
                        HandleAuthenticationResponse(message);
                        break;
                    case LootLockerPresenceMessageType.Pong:
                        HandlePongResponse(message);
                        break;
                    case LootLockerPresenceMessageType.Error:
                        HandleErrorResponse(message);
                        break;
                    default:
                        HandleGeneralMessage(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error processing Presence message: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        private LootLockerPresenceMessageType DetermineMessageType(string message)
        {
            if (message.Contains("authenticated"))
                return LootLockerPresenceMessageType.Authentication;
            
            if (message.Contains("pong"))
                return LootLockerPresenceMessageType.Pong;
            
            if (message.Contains("error"))
                return LootLockerPresenceMessageType.Error;
            
            return LootLockerPresenceMessageType.Unknown;
        }

        private void HandleAuthenticationResponse(string message)
        {
            try
            {
                if (message.Contains("authenticated"))
                {
                    ChangeConnectionState(LootLockerPresenceConnectionState.Active);
                    LootLockerLogger.Log("Presence authentication successful", LootLockerLogger.LogLevel.Debug);
                    
                    // Start ping routine now that we're active
                    StartPingRoutine();
                    
                    // Reset reconnect attempts on successful authentication
                    reconnectAttempts = 0;
                    
                    // Invoke pending connection callback on successful authentication
                    pendingConnectionCallback?.Invoke(true, null);
                    pendingConnectionCallback = null;
                }
                else
                {
                    string errorMessage = "Authentication failed";
                    ChangeConnectionState(LootLockerPresenceConnectionState.Failed, errorMessage);
                    
                    // Invoke pending connection callback on authentication failure
                    pendingConnectionCallback?.Invoke(false, errorMessage);
                    pendingConnectionCallback = null;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error handling authentication response: {ex.Message}";
                LootLockerLogger.Log(errorMessage, LootLockerLogger.LogLevel.Warning);
                
                // Invoke pending callback on exception
                pendingConnectionCallback?.Invoke(false, errorMessage);
                pendingConnectionCallback = null;
            }
        }

        private void HandlePongResponse(string message)
        {
            try
            {
                var pongResponse = LootLockerJson.DeserializeObject<LootLockerPresencePingResponse>(message);
                
                // Calculate latency if we have matching ping timestamp
                if (pendingPingTimestamps.Count > 0 && pongResponse?.timestamp != default(DateTime))
                {
                    var pongReceivedTime = DateTime.UtcNow;
                    var pingTimestamp = pendingPingTimestamps.Dequeue();
                    
                    // Calculate round-trip time in milliseconds
                    var latencyMs = (long)(pongReceivedTime - pingTimestamp).TotalMilliseconds;
                    
                    if (latencyMs >= 0) // Sanity check
                    {
                        UpdateLatencyStats(latencyMs);
                    }
                    
                    // Only count the pong if we had a matching ping timestamp
                    connectionStats.totalPongsReceived++;
                }
                
                OnPingReceived?.Invoke(pongResponse);
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error handling pong response: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        private void UpdateLatencyStats(long roundTripMs)
        {
            // Convert round-trip time to one-way latency (industry standard)
            var latency = (float)roundTripMs / 2.0f;
            
            // Update current latency
            connectionStats.currentLatencyMs = latency;
            
            // Update min/max
            if (latency < connectionStats.minLatencyMs)
                connectionStats.minLatencyMs = latency;
            if (latency > connectionStats.maxLatencyMs)
                connectionStats.maxLatencyMs = latency;
            
            // Add to recent latencies for average calculation
            recentLatencies.Enqueue(latency);
            if (recentLatencies.Count > MAX_LATENCY_SAMPLES)
            {
                recentLatencies.Dequeue();
            }
            
            // Calculate average from recent samples
            var sum = 0f;
            foreach (var sample in recentLatencies)
            {
                sum += sample;
            }
            connectionStats.averageLatencyMs = sum / recentLatencies.Count;
        }

        private void HandleErrorResponse(string message)
        {
            LootLockerLogger.Log($"Received presence error: {message}", LootLockerLogger.LogLevel.Warning);
        }

        private void HandleGeneralMessage(string message)
        {
            // This method can be extended for other specific message types
            LootLockerLogger.Log($"Received general presence message: {message}", LootLockerLogger.LogLevel.Debug);
        }

        private void ChangeConnectionState(LootLockerPresenceConnectionState newState, string error = null)
        {
            if (connectionState != newState)
            {
                var previousState = connectionState;
                connectionState = newState;

                // Update connection stats with new state
                connectionStats.connectionState = newState;

                LootLockerLogger.Log($"Presence connection state changed: {previousState} -> {newState}", LootLockerLogger.LogLevel.Debug);

                // Stop ping routine if we're no longer active
                if (newState != LootLockerPresenceConnectionState.Active && pingCoroutine != null)
                {
                    LootLockerLogger.Log("Stopping ping routine due to connection state change", LootLockerLogger.LogLevel.Debug);
                    StopCoroutine(pingCoroutine);
                    pingCoroutine = null;
                }

                OnConnectionStateChanged?.Invoke(previousState, newState, error);
            }
        }

        private void StartPingRoutine()
        {
            LootLockerLogger.Log("Starting presence ping routine after authentication", LootLockerLogger.LogLevel.Debug);
            
            if (pingCoroutine != null)
            {
                LootLockerLogger.Log("Stopping existing ping coroutine", LootLockerLogger.LogLevel.Debug);
                StopCoroutine(pingCoroutine);
            }

            LootLockerLogger.Log($"Starting ping routine. Authenticated: {IsConnectedAndAuthenticated}, Destroying: {isDestroying}", LootLockerLogger.LogLevel.Debug);
            pingCoroutine = StartCoroutine(PingRoutine());
        }

        private IEnumerator PingRoutine()
        {
            LootLockerLogger.Log("Starting presence ping routine", LootLockerLogger.LogLevel.Debug);
            
            // Send an immediate ping after authentication to help maintain connection
            if (IsConnectedAndAuthenticated && !isDestroying)
            {
                LootLockerLogger.Log("Sending initial presence ping", LootLockerLogger.LogLevel.Debug);
                SendPing();
            }
            
            while (IsConnectedAndAuthenticated && !isDestroying)
            {
                float pingInterval = PING_INTERVAL;
                LootLockerLogger.Log($"Waiting {pingInterval} seconds before next ping. Connected: {IsConnectedAndAuthenticated}, Destroying: {isDestroying}", LootLockerLogger.LogLevel.Debug);
                yield return new WaitForSeconds(pingInterval);

                if (IsConnectedAndAuthenticated && !isDestroying)
                {
                    LootLockerLogger.Log("Sending presence ping", LootLockerLogger.LogLevel.Debug);
                    SendPing(); // Use callback version instead of async
                }
                else
                {
                    LootLockerLogger.Log($"Ping routine stopping. Connected: {IsConnectedAndAuthenticated}, Destroying: {isDestroying}", LootLockerLogger.LogLevel.Debug);
                    break;
                }
            }
            
            LootLockerLogger.Log("Presence ping routine ended", LootLockerLogger.LogLevel.Debug);
        }

        private IEnumerator ScheduleReconnectCoroutine(float customDelay = -1f)
        {
            if (!shouldReconnect || isDestroying || reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                yield break;
            }

            reconnectAttempts++;
            float delayToUse = customDelay > 0 ? customDelay : RECONNECT_DELAY;
            LootLockerLogger.Log($"Scheduling Presence reconnect attempt {reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS} in {delayToUse} seconds", LootLockerLogger.LogLevel.Debug);

            yield return new WaitForSeconds(delayToUse);

            if (shouldReconnect && !isDestroying)
            {
                StartCoroutine(ConnectCoroutine());
            }
        }

        #endregion
    }
}

#endif