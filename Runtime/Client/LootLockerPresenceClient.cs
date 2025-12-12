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
        Failed,
        Destroying,
        Destroyed
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

        public LootLockerPresencePingRequest()
        {
        }
    }

    /// <summary>
    /// Ping response from the server
    /// </summary>
    [Serializable]
    public class LootLockerPresencePingResponse
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
    /// Callback for presence operations
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

        // Configuration Constants
        private const float PING_INTERVAL = 20f;
        private const float RECONNECT_DELAY = 5f;
        private const int MAX_RECONNECT_ATTEMPTS = 5;
        private const int MAX_LATENCY_SAMPLES = 10;

        // WebSocket and Connection
        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        private readonly ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();
        private LootLockerPresenceConnectionState connectionState = LootLockerPresenceConnectionState.Disconnected;
        private string playerUlid;
        private string sessionToken;
        private static string webSocketUrl;

        // State tracking
        private bool shouldReconnect = true;
        private int reconnectAttempts = 0;
        private Coroutine pingCoroutine;
        private Coroutine statusUpdateCoroutine;
        private Coroutine webSocketListenerCoroutine;
        private bool isClientInitiatedDisconnect = false; // Track if disconnect is expected (due to session end)
        private LootLockerPresenceCallback pendingConnectionCallback; // Store callback until authentication completes

        // Latency tracking
        private readonly Queue<DateTime> pendingPingTimestamps = new Queue<DateTime>();
        private readonly Queue<float> recentLatencies = new Queue<float>();
        private LootLockerPresenceConnectionStats connectionStats = new LootLockerPresenceConnectionStats
        {
            minLatencyMs = float.MaxValue,
            maxLatencyMs = 0f
        };

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
        /// The session token this client is using for authentication
        /// </summary>
        public string SessionToken => sessionToken;

        /// <summary>
        /// The last status that was sent to the server (e.g., "online", "in_game", "away")
        /// </summary>
        public string LastSentStatus => ConnectionStats.lastSentStatus;

        /// <summary>
        /// Get connection statistics including latency to LootLocker
        /// </summary>
        public LootLockerPresenceConnectionStats ConnectionStats { 
            get {
                connectionStats.connectionState = connectionState;
                return connectionStats;
            }
            set { connectionStats = value; }
        }

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
            Dispose();
        }

        /// <summary>
        /// Dispose of the presence client and release resources without syncing state to the server.
        /// Required by IDisposable interface, this method performs immediate cleanup. If you want to close the client due to runtime control flow, use Disconnect() instead.
        /// </summary>
        public void Dispose()
        {
            if (connectionState == LootLockerPresenceConnectionState.Destroying || connectionState == LootLockerPresenceConnectionState.Destroyed) return;

            ChangeConnectionState(LootLockerPresenceConnectionState.Destroying);  

            shouldReconnect = false;

            StopCoroutines();

            // Use synchronous cleanup for dispose to ensure immediate resource release
            CleanupWebsocket();
            
            // Clear all queues
            while (receivedMessages.TryDequeue(out _)) { }
            pendingPingTimestamps.Clear();
            recentLatencies.Clear();

            ChangeConnectionState(LootLockerPresenceConnectionState.Destroyed);        
        }
        
        /// <summary>
        /// Synchronous cleanup for disposal scenarios
        /// </summary>
        private void CleanupWebsocket()
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
                            LootLockerLogger.Log("WebSocket close timed out during disposal", LootLockerLogger.LogLevel.Debug);
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

        private void StopCoroutines()
        {
            if (pingCoroutine != null)
            {
                StopCoroutine(pingCoroutine);
                pingCoroutine = null;
            }

            if (statusUpdateCoroutine != null)
            {
                StopCoroutine(statusUpdateCoroutine);
                statusUpdateCoroutine = null;
            }

            if(webSocketListenerCoroutine != null)
            {
                StopCoroutine(webSocketListenerCoroutine);
                webSocketListenerCoroutine = null;
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
        /// Update the session token for this client (used during token refresh)
        /// </summary>
        internal void UpdateSessionToken(string newSessionToken)
        {
            this.sessionToken = newSessionToken;
        }

        /// <summary>
        /// Connect to the Presence WebSocket
        /// </summary>
        internal void Connect(LootLockerPresenceCallback onComplete = null)
        {
            if (connectionState == LootLockerPresenceConnectionState.Destroying ||
                connectionState == LootLockerPresenceConnectionState.Destroyed)
            {
                onComplete?.Invoke(false, "Client has been destroyed");
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
            if (connectionState == LootLockerPresenceConnectionState.Destroying ||
                connectionState == LootLockerPresenceConnectionState.Destroyed ||
                connectionState == LootLockerPresenceConnectionState.Disconnected ||
                connectionState == LootLockerPresenceConnectionState.Failed)
            {
                onComplete?.Invoke(true, null);
                return;
            }

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
        /// Send a ping to maintain connection and measure latency
        /// </summary>
        internal void SendPing(LootLockerPresenceCallback onComplete = null)
        {
            if (!IsConnectedAndAuthenticated)
            {
                onComplete?.Invoke(false, "Not connected and authenticated");
                return;
            }

            var pingRequest = new LootLockerPresencePingRequest();
            
            // Track the ping timestamp for latency calculation
            pendingPingTimestamps.Enqueue(DateTime.UtcNow);

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
            if (connectionState == LootLockerPresenceConnectionState.Destroying ||
                connectionState == LootLockerPresenceConnectionState.Destroyed)
            {
                HandleConnectionError("Presence client is destroyed");
                yield break;
            }
            if (string.IsNullOrEmpty(sessionToken))
            {
                HandleConnectionError("Invalid session token");
                yield break;
            }

            // Set state
            ChangeConnectionState(reconnectAttempts > 0 ? 
                LootLockerPresenceConnectionState.Reconnecting : 
                LootLockerPresenceConnectionState.Connecting);

            // Cleanup any existing connections
            CleanupWebsocket();

            // Initialize WebSocket
            try
            {
                webSocket = new ClientWebSocket();
                cancellationTokenSource = new CancellationTokenSource();

                // Cache base URL on first use to avoid repeated string operations
                if (string.IsNullOrEmpty(webSocketUrl))
                {
                    webSocketUrl = LootLockerConfig.current.webSocketBaseUrl + "/presence/v1";
                }
            }
            catch (Exception ex)
            {
                HandleConnectionError("Failed to initialize WebSocket with exception: " + ex.Message);
            }

            var uri = new Uri(webSocketUrl);

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
                HandleConnectionError(error);
                yield break;
            }

            ChangeConnectionState(LootLockerPresenceConnectionState.Connected);
            reconnectAttempts = 0;

            InitializeConnectionStats();

            // Start listening for messages
            webSocketListenerCoroutine = StartCoroutine(ListenForMessagesCoroutine());

            // Send authentication
            yield return StartCoroutine(AuthenticateCoroutine());
        }

        private void InitializeConnectionStats()
        {
            connectionStats.playerUlid = this.playerUlid;
            connectionStats.connectionState = this.connectionState;
            connectionStats.lastSentStatus = this.ConnectionStats.lastSentStatus;
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
            if (connectionState == LootLockerPresenceConnectionState.Destroying ||
                connectionState == LootLockerPresenceConnectionState.Destroyed)
            {
                onComplete?.Invoke(true, null);
                yield break;
            }
            
            isClientInitiatedDisconnect = true;
            shouldReconnect = false;

            StopCoroutines();

            // Close WebSocket connection
            bool closeSuccess = true;
            string closeErrorMessage = null;
            if (webSocket != null)
            {
                yield return StartCoroutine(CloseWebSocketCoroutine((success, errorMessage) => {
                    closeSuccess = success;
                    closeErrorMessage = errorMessage;
                }));
            }

            // Always cleanup regardless of close success
            CleanupWebsocket();

            ChangeConnectionState(LootLockerPresenceConnectionState.Disconnected);
            
            isClientInitiatedDisconnect = false;
            
            onComplete?.Invoke(closeSuccess, closeSuccess ? null : closeErrorMessage);
        }

        private IEnumerator CloseWebSocketCoroutine(System.Action<bool, string> onComplete)
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
                    onComplete?.Invoke(true, "WebSeocket already closed by server");
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
                    onComplete?.Invoke(true, "WebSocket in unexpected state, treated as closed");
                    yield break;
                }
            }
            catch (Exception ex)
            {
                // If we get an exception during close (like WebSocket aborted), treat it as already closed
                if (ex.Message.Contains("invalid state") || ex.Message.Contains("Aborted"))
                {
                    if (isClientInitiatedDisconnect)
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
                
                onComplete?.Invoke(closeSuccess, closeSuccess ? null : "Error during disconnect");
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
                            if (isClientInitiatedDisconnect)
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
                            if (isClientInitiatedDisconnect)
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
                    if (isClientInitiatedDisconnect)
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
            
            onComplete?.Invoke(closeSuccess, closeSuccess ? null : "Error during disconnect");
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

                yield return new WaitUntil(() => receiveTask.IsCompleted || receiveTask.IsFaulted || connectionState == LootLockerPresenceConnectionState.Destroying || connectionState == LootLockerPresenceConnectionState.Destroyed);

                if(connectionState == LootLockerPresenceConnectionState.Destroying || connectionState == LootLockerPresenceConnectionState.Destroyed)
                {
                    yield break;
                }

                if (receiveTask.IsFaulted)
                {
                    // Handle receive error
                    var exception = receiveTask.Exception?.GetBaseException();
                    if (exception is OperationCanceledException || exception is TaskCanceledException)
                    {
                        if (!isClientInitiatedDisconnect)
                        {
                            LootLockerLogger.Log("Presence WebSocket listening cancelled", LootLockerLogger.LogLevel.Debug);
                        }
                    }
                    else
                    {
                        string errorMessage = exception?.Message ?? "Unknown error";
                        LootLockerLogger.Log($"Error listening for Presence messages: {errorMessage}", LootLockerLogger.LogLevel.Warning);
                        
                        // Only attempt reconnect for unexpected disconnects
                        if (shouldReconnect && reconnectAttempts < MAX_RECONNECT_ATTEMPTS && !isClientInitiatedDisconnect)
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
                    if (!isClientInitiatedDisconnect)
                    {
                        LootLockerLogger.Log("Presence WebSocket closed by server", LootLockerLogger.LogLevel.Debug);
                    }
                    
            
                    isClientInitiatedDisconnect = true;
                    shouldReconnect = false;

                    StopCoroutines();

                    // No need to close websocket here, as server initiated close has already happened

                    CleanupWebsocket();

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
                if (message.Contains("authenticated"))
                {
                    HandleAuthenticationResponse(message);
                }
                else if (message.Contains("pong"))
                {
                    HandlePongResponse(message);
                }
                else if (message.Contains("error"))
                {
                    HandleErrorResponse(message);
                }
                else 
                {
                    HandleGeneralMessage(message);
                }
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error processing Presence message: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        private void HandleAuthenticationResponse(string message)
        {
            try
            {
                ChangeConnectionState(LootLockerPresenceConnectionState.Active);
                
                if (pingCoroutine != null)
                {
                    StopCoroutine(pingCoroutine);
                }

                pingCoroutine = StartCoroutine(PingCoroutine());
                
                // Auto-resend last status if we have one
                if (!string.IsNullOrEmpty(connectionStats.lastSentStatus))
                {
                    LootLockerLogger.Log($"Auto-resending last status '{connectionStats.lastSentStatus}' after reconnection", LootLockerLogger.LogLevel.Debug);
                    // Use a coroutine to avoid blocking the authentication flow
                    StartCoroutine(AutoResendLastStatusCoroutine());
                }
                
                // Reset reconnect attempts on successful authentication
                reconnectAttempts = 0;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error handling authentication response: {ex.Message}";
                LootLockerLogger.Log(errorMessage, LootLockerLogger.LogLevel.Warning);
                
                // Invoke pending callback on exception
                pendingConnectionCallback?.Invoke(false, errorMessage);
                pendingConnectionCallback = null;
            }

            try {
                // Invoke pending connection callback on successful authentication
                pendingConnectionCallback?.Invoke(true, null);
                pendingConnectionCallback = null;
            } 
            catch (Exception ex) {
                LootLockerLogger.Log($"Error invoking connection callback: {ex.Message}", LootLockerLogger.LogLevel.Warning);
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

                LootLockerLogger.Log($"Presence state changed from {previousState} to {newState} for player {playerUlid}", LootLockerLogger.LogLevel.Debug);

                // Then notify external systems via the unified event system
                LootLockerEventSystem.TriggerPresenceConnectionStateChanged(playerUlid, previousState, newState, error);
            }
        }

        private IEnumerator PingCoroutine()
        {
            
            while (IsConnectedAndAuthenticated)
            {
                SendPing();
                yield return new WaitForSeconds(PING_INTERVAL);
            }
        }

        private IEnumerator ScheduleReconnectCoroutine(float customDelay = -1f)
        {
            if (!shouldReconnect || reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                yield break;
            }

            reconnectAttempts++;
            float delayToUse = customDelay > 0 ? customDelay : RECONNECT_DELAY;
            LootLockerLogger.Log($"Scheduling Presence reconnect attempt {reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS} in {delayToUse} seconds", LootLockerLogger.LogLevel.Debug);
            ChangeConnectionState(LootLockerPresenceConnectionState.Reconnecting);

            yield return new WaitForSeconds(delayToUse);

            if (shouldReconnect && connectionState == LootLockerPresenceConnectionState.Reconnecting)
            {
                StartCoroutine(ConnectCoroutine());
            }
        }

        /// <summary>
        /// Coroutine to auto-resend the last status after successful reconnection
        /// </summary>
        private IEnumerator AutoResendLastStatusCoroutine()
        {
            // Wait a frame to ensure we're fully connected
            yield return null;
            
            // Double-check we're still connected and have a status to send
            if (IsConnectedAndAuthenticated && !string.IsNullOrEmpty(connectionStats.lastSentStatus))
            {
                // Find the last sent metadata if any
                // Note: We don't store metadata currently, so we'll resend with null metadata
                // This could be enhanced later if metadata preservation is needed
                UpdateStatus(connectionStats.lastSentStatus, null, (success, error) => {
                    if (success)
                    {
                        LootLockerLogger.Log($"Successfully auto-resent status '{connectionStats.lastSentStatus}' after reconnection", LootLockerLogger.LogLevel.Debug);
                    }
                    else
                    {
                        LootLockerLogger.Log($"Failed to auto-resend status after reconnection: {error}", LootLockerLogger.LogLevel.Warning);
                    }
                });
            }
        }

        #endregion
    }
}