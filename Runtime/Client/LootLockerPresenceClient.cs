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
        Authenticated,
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
        public string metadata { get; set; }

        public LootLockerPresenceStatusRequest(string status, string metadata = null)
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
        public long timestamp { get; set; }

        public LootLockerPresencePingRequest()
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
        public long timestamp { get; set; }
    }

    /// <summary>
    /// Statistics about the presence connection to LootLocker
    /// </summary>
    [Serializable]
    public class LootLockerPresenceConnectionStats
    {
        /// <summary>
        /// Current round-trip latency to LootLocker in milliseconds
        /// </summary>
        public float currentLatencyMs { get; set; }

        /// <summary>
        /// Average latency over the last few pings in milliseconds
        /// </summary>
        public float averageLatencyMs { get; set; }

        /// <summary>
        /// Minimum recorded latency in milliseconds
        /// </summary>
        public float minLatencyMs { get; set; }

        /// <summary>
        /// Maximum recorded latency in milliseconds
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
        /// Packet loss percentage (0-100)
        /// </summary>
        public float packetLossPercentage => totalPingsSent > 0 ? ((totalPingsSent - totalPongsReceived) / (float)totalPingsSent) * 100f : 0f;

        /// <summary>
        /// When the connection was established
        /// </summary>
        public DateTime connectionStartTime { get; set; }

        /// <summary>
        /// How long the connection has been active
        /// </summary>
        public TimeSpan connectionDuration => DateTime.UtcNow - connectionStartTime;
    }

    #endregion

    #region Event Delegates

    /// <summary>
    /// Delegate for connection state changes
    /// </summary>
    public delegate void LootLockerPresenceConnectionStateChanged(string playerUlid, LootLockerPresenceConnectionState newState, string error = null);
    
    /// <summary>
    /// Delegate for general presence messages
    /// </summary>
    public delegate void LootLockerPresenceMessageReceived(string playerUlid, string message, LootLockerPresenceMessageType messageType);

    /// <summary>
    /// Delegate for ping responses
    /// </summary>
    public delegate void LootLockerPresencePingReceived(string playerUlid, LootLockerPresencePingResponse response);

    /// <summary>
    /// Delegate for presence operation responses (connect, disconnect, status update)
    /// </summary>
    public delegate void LootLockerPresenceCallback(bool success, string error = null);

    #endregion

    // LootLockerPresenceManager moved to LootLockerPresenceManager.cs

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

        private LootLockerPresenceConnectionState connectionState = LootLockerPresenceConnectionState.Initializing;
        private string playerUlid;
        private string sessionToken;
        private static string webSocketBaseUrl;

        // Connection settings
        private const float PING_INTERVAL = 25f;
        private const float RECONNECT_DELAY = 5f;
        private const int MAX_RECONNECT_ATTEMPTS = 5;

        // Battery optimization settings
        private float GetEffectivePingInterval()
        {
            if (LootLockerConfig.ShouldUseBatteryOptimizations() && LootLockerConfig.current.mobilePresenceUpdateInterval > 0)
            {
                return LootLockerConfig.current.mobilePresenceUpdateInterval;
            }
            return PING_INTERVAL;
        }

        // State tracking
        private bool shouldReconnect = true;
        private int reconnectAttempts = 0;
        private Coroutine pingCoroutine;
        private bool isDestroying = false;
        private bool isDisposed = false;

        // Latency tracking
        private readonly Queue<long> pendingPingTimestamps = new Queue<long>();
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
        public event System.Action<LootLockerPresenceConnectionState, string> OnConnectionStateChanged;

        /// <summary>
        /// Event fired when any presence message is received
        /// </summary>
        public event System.Action<string, LootLockerPresenceMessageType> OnMessageReceived;

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
        /// Whether the client is connected and authenticated
        /// </summary>
        public bool IsConnectedAndAuthenticated => connectionState == LootLockerPresenceConnectionState.Authenticated;

        /// <summary>
        /// Whether the client is currently connecting or reconnecting
        /// </summary>
        public bool IsConnecting => connectionState == LootLockerPresenceConnectionState.Initializing ||
                                   connectionState == LootLockerPresenceConnectionState.Connecting || 
                                   connectionState == LootLockerPresenceConnectionState.Reconnecting;

        /// <summary>
        /// Whether the client is currently connecting or reconnecting
        /// </summary>
        public bool IsAuthenticating => connectionState == LootLockerPresenceConnectionState.Authenticating;

        /// <summary>
        /// The player ULID this client is associated with
        /// </summary>
        public string PlayerUlid => playerUlid;

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
                LootLockerLogger.Log($"Error during synchronous cleanup: {ex.Message}", LootLockerLogger.LogLevel.Error);
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

            StartCoroutine(ConnectCoroutine(onComplete));
        }

        /// <summary>
        /// Disconnect from the Presence WebSocket
        /// </summary>
        internal void Disconnect(LootLockerPresenceCallback onComplete = null)
        {
            shouldReconnect = false;
            StartCoroutine(DisconnectCoroutine(onComplete));
        }

        /// <summary>
        /// Send a status update to the Presence service
        /// </summary>
        internal void UpdateStatus(string status, string metadata = null, LootLockerPresenceCallback onComplete = null)
        {
            if (!IsConnectedAndAuthenticated)
            {
                onComplete?.Invoke(false, "Not connected and authenticated");
                return;
            }

            var statusRequest = new LootLockerPresenceStatusRequest(status, metadata);
            StartCoroutine(SendMessageCoroutine(LootLockerJson.SerializeObject(statusRequest), onComplete));
        }

        /// <summary>
        /// Send a ping to test the connection
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
            pendingPingTimestamps.Enqueue(pingRequest.timestamp);
            connectionStats.totalPingsSent++;

            // Clean up old pending pings (in case pongs are lost)
            while (pendingPingTimestamps.Count > 10)
            {
                pendingPingTimestamps.Dequeue();
            }

            StartCoroutine(SendMessageCoroutine(LootLockerJson.SerializeObject(pingRequest), onComplete));
        }

        #endregion

        #region Private Methods

        private IEnumerator ConnectCoroutine(LootLockerPresenceCallback onComplete = null)
        {
            if (isDestroying || isDisposed || string.IsNullOrEmpty(sessionToken))
            {
                onComplete?.Invoke(false, "Invalid state or session token");
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
                HandleConnectionError("Failed to initialize WebSocket", onComplete);
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
                HandleConnectionError(connectionError ?? "Connection failed", onComplete);
                yield break;
            }

            ChangeConnectionState(LootLockerPresenceConnectionState.Connected);

            // Initialize connection stats
            InitializeConnectionStats();

            // Start listening for messages
            StartCoroutine(ListenForMessagesCoroutine());

            // Send authentication
            bool authSuccess = false;
            yield return StartCoroutine(AuthenticateCoroutine((success, error) => {
                authSuccess = success;
            }));

            if (!authSuccess)
            {
                HandleConnectionError("Authentication failed", onComplete);
                yield break;
            }

            // Start ping routine
            StartPingRoutine();

            reconnectAttempts = 0;
            onComplete?.Invoke(true);
        }

        private bool InitializeWebSocket()
        {
            try
            {
                webSocket = new ClientWebSocket();
                cancellationTokenSource = new CancellationTokenSource();

                // Cache base URL on first use to avoid repeated string operations
                if (string.IsNullOrEmpty(webSocketBaseUrl))
                {
                    webSocketBaseUrl = LootLockerConfig.current.url.Replace("https://", "wss://").Replace("http://", "ws://");
                }
                return true;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Failed to initialize WebSocket: {ex.Message}", LootLockerLogger.LogLevel.Error);
                return false;
            }
        }

        private IEnumerator ConnectWebSocketCoroutine(LootLockerPresenceCallback onComplete)
        {
            var uri = new Uri($"{webSocketBaseUrl}/game/presence/v1");
            LootLockerLogger.Log($"Connecting to Presence WebSocket: {uri}", LootLockerLogger.LogLevel.Verbose);

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

        private void HandleConnectionError(string errorMessage, LootLockerPresenceCallback onComplete)
        {
            LootLockerLogger.Log($"Failed to connect to Presence WebSocket: {errorMessage}", LootLockerLogger.LogLevel.Error);
            ChangeConnectionState(LootLockerPresenceConnectionState.Failed, errorMessage);
            
            if (shouldReconnect && reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
            {
                StartCoroutine(ScheduleReconnectCoroutine());
            }

            onComplete?.Invoke(false, errorMessage);
        }

        private IEnumerator DisconnectCoroutine(LootLockerPresenceCallback onComplete = null)
        {
            // Stop ping routine
            if (pingCoroutine != null)
            {
                StopCoroutine(pingCoroutine);
                pingCoroutine = null;
            }

            // Close WebSocket connection
            bool closeSuccess = true;
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                cancellationTokenSource?.Cancel();
                var closeTask = webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                    "Client disconnecting", CancellationToken.None);
                
                // Wait for close with timeout
                float timeoutSeconds = 5f;
                float elapsed = 0f;
                
                while (!closeTask.IsCompleted && elapsed < timeoutSeconds)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (closeTask.IsFaulted)
                {
                    closeSuccess = false;
                    LootLockerLogger.Log($"Error during disconnect: {closeTask.Exception?.Message}", LootLockerLogger.LogLevel.Error);
                }
            }

            // Always cleanup regardless of close success
            yield return StartCoroutine(CleanupConnectionCoroutine());

            ChangeConnectionState(LootLockerPresenceConnectionState.Disconnected);
            onComplete?.Invoke(closeSuccess, closeSuccess ? null : "Error during disconnect");
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
                LootLockerLogger.Log($"Error during cleanup: {ex.Message}", LootLockerLogger.LogLevel.Error);
            }
            
            yield return null;
        }

        private IEnumerator AuthenticateCoroutine(LootLockerPresenceCallback onComplete = null)
        {
            if (webSocket?.State != WebSocketState.Open)
            {
                onComplete?.Invoke(false, "WebSocket not open for authentication");
                yield break;
            }

            ChangeConnectionState(LootLockerPresenceConnectionState.Authenticating);

            var authRequest = new LootLockerPresenceAuthRequest(sessionToken);
            string jsonPayload = LootLockerJson.SerializeObject(authRequest);

            yield return StartCoroutine(SendMessageCoroutine(jsonPayload, onComplete));
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
                LootLockerLogger.Log($"Sent Presence message: {message}", LootLockerLogger.LogLevel.Verbose);
                onComplete?.Invoke(true);
            }
            else
            {
                string error = sendTask.Exception?.GetBaseException()?.Message ?? "Send timeout";
                LootLockerLogger.Log($"Failed to send Presence message: {error}", LootLockerLogger.LogLevel.Error);
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
                    if (exception is OperationCanceledException)
                    {
                        LootLockerLogger.Log("Presence WebSocket listening cancelled", LootLockerLogger.LogLevel.Verbose);
                    }
                    else
                    {
                        LootLockerLogger.Log($"Error listening for Presence messages: {exception?.Message}", LootLockerLogger.LogLevel.Error);
                        
                        if (shouldReconnect && reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
                        {
                            StartCoroutine(ScheduleReconnectCoroutine());
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
                    LootLockerLogger.Log("Presence WebSocket closed by server", LootLockerLogger.LogLevel.Verbose);
                    break;
                }
            }
        }

        private void ProcessReceivedMessage(string message)
        {
            try
            {
                LootLockerLogger.Log($"Received Presence message: {message}", LootLockerLogger.LogLevel.Verbose);

                // Determine message type
                var messageType = DetermineMessageType(message);

                // Fire general message event
                OnMessageReceived?.Invoke(message, messageType);

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
                LootLockerLogger.Log($"Error processing Presence message: {ex.Message}", LootLockerLogger.LogLevel.Error);
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
                    ChangeConnectionState(LootLockerPresenceConnectionState.Authenticated);
                    LootLockerLogger.Log("Presence authentication successful", LootLockerLogger.LogLevel.Verbose);
                }
                else
                {
                    ChangeConnectionState(LootLockerPresenceConnectionState.Failed, "Authentication failed");
                }
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error handling authentication response: {ex.Message}", LootLockerLogger.LogLevel.Error);
            }
        }

        private void HandlePongResponse(string message)
        {
            try
            {
                var pongResponse = LootLockerJson.DeserializeObject<LootLockerPresencePingResponse>(message);
                
                // Calculate latency if we have matching ping timestamp
                if (pendingPingTimestamps.Count > 0 && pongResponse.timestamp > 0)
                {
                    var pongReceivedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var pingTimestamp = pendingPingTimestamps.Dequeue();
                    
                    // Calculate round-trip time
                    var latencyMs = pongReceivedTime - pingTimestamp;
                    
                    if (latencyMs >= 0) // Sanity check
                    {
                        UpdateLatencyStats(latencyMs);
                    }
                }
                
                connectionStats.totalPongsReceived++;
                OnPingReceived?.Invoke(pongResponse);
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error handling pong response: {ex.Message}", LootLockerLogger.LogLevel.Error);
            }
        }

        private void UpdateLatencyStats(long latencyMs)
        {
            var latency = (float)latencyMs;
            
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
            LootLockerLogger.Log($"Received presence error: {message}", LootLockerLogger.LogLevel.Error);
        }

        private void HandleGeneralMessage(string message)
        {
            // This method can be extended for other specific message types
            LootLockerLogger.Log($"Received general presence message: {message}", LootLockerLogger.LogLevel.Verbose);
        }

        private void ChangeConnectionState(LootLockerPresenceConnectionState newState, string error = null)
        {
            if (connectionState != newState)
            {
                var previousState = connectionState;
                connectionState = newState;

                LootLockerLogger.Log($"Presence connection state changed: {previousState} -> {newState}", LootLockerLogger.LogLevel.Verbose);

                OnConnectionStateChanged?.Invoke(newState, error);
            }
        }

        private void StartPingRoutine()
        {
            if (pingCoroutine != null)
            {
                StopCoroutine(pingCoroutine);
            }

            pingCoroutine = StartCoroutine(PingRoutine());
        }

        private IEnumerator PingRoutine()
        {
            while (IsConnectedAndAuthenticated && !isDestroying)
            {
                float pingInterval = GetEffectivePingInterval();
                yield return new WaitForSeconds(pingInterval);

                if (IsConnectedAndAuthenticated && !isDestroying)
                {
                    SendPing(); // Use callback version instead of async
                }
            }
        }

        private IEnumerator ScheduleReconnectCoroutine()
        {
            if (!shouldReconnect || isDestroying || reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                yield break;
            }

            reconnectAttempts++;
            LootLockerLogger.Log($"Scheduling Presence reconnect attempt {reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS} in {RECONNECT_DELAY} seconds", LootLockerLogger.LogLevel.Verbose);

            yield return new WaitForSeconds(RECONNECT_DELAY);

            if (shouldReconnect && !isDestroying)
            {
                StartCoroutine(ConnectCoroutine());
            }
        }

        #endregion
    }
}

#endif