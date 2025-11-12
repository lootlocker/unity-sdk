#if LOOTLOCKER_ENABLE_PRESENCE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LootLocker.Requests;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LootLocker
{
    /// <summary>
    /// Manager for all LootLocker Presence functionality
    /// Automatically manages presence connections for all active sessions
    /// </summary>
    public class LootLockerPresenceManager : MonoBehaviour, ILootLockerService
    {
        #region ILootLockerService Implementation

        public bool IsInitialized { get; private set; } = false;
        public string ServiceName => "PresenceManager";

        void ILootLockerService.Initialize()
        {
            if (IsInitialized) return;
            
            // Initialize presence configuration
            isEnabled = LootLockerConfig.IsPresenceEnabledForCurrentPlatform();
            
            // Subscribe to session events
            SubscribeToSessionEvents();
            
            // Auto-connect existing active sessions if enabled
            StartCoroutine(AutoConnectExistingSessions());
            
            IsInitialized = true;
            LootLockerLogger.Log("LootLockerPresenceManager initialized", LootLockerLogger.LogLevel.Verbose);
        }

        void ILootLockerService.Reset()
        {
            // Disconnect all presence connections
            DisconnectAll();
            
            // Unsubscribe from events
            UnsubscribeFromSessionEvents();
            
            // Clear session tracking
            _connectedSessions?.Clear();
            
            IsInitialized = false;
            lock(_instanceLock) {
                _instance = null;
            }
        }

        void ILootLockerService.HandleApplicationPause(bool pauseStatus)
        {
            if(!IsInitialized)
                return;
            if (!LootLockerConfig.ShouldUseBatteryOptimizations() || !isEnabled)
                return;

            if (pauseStatus)
            {
                // App paused - disconnect for battery optimization
                LootLockerLogger.Log("App paused - disconnecting presence sessions", LootLockerLogger.LogLevel.Verbose);
                DisconnectAll();
            }
            else
            {
                // App resumed - reconnect
                LootLockerLogger.Log("App resumed - reconnecting presence sessions", LootLockerLogger.LogLevel.Verbose);
                StartCoroutine(AutoConnectExistingSessions());
            }
        }

        void ILootLockerService.HandleApplicationFocus(bool hasFocus)
        {
            if(!IsInitialized)
                return;
            if (!LootLockerConfig.ShouldUseBatteryOptimizations() || !isEnabled)
                return;

            if (hasFocus)
            {
                // App regained focus - use existing AutoConnectExistingSessions logic
                LootLockerLogger.Log("App returned to foreground - reconnecting presence sessions", LootLockerLogger.LogLevel.Verbose);
                StartCoroutine(AutoConnectExistingSessions());
            }
            else
            {
                // App lost focus - disconnect all active sessions to save battery
                LootLockerLogger.Log("App went to background - disconnecting all presence sessions for battery optimization", LootLockerLogger.LogLevel.Verbose);
                DisconnectAll();
            }
        }

        void ILootLockerService.HandleApplicationQuit()
        {
            // Cleanup all connections and subscriptions
            DisconnectAll();
            UnsubscribeFromSessionEvents();
            _connectedSessions?.Clear();
        }

        #endregion


        #region Singleton Management
        
        private static LootLockerPresenceManager _instance;
        private static readonly object _instanceLock = new object();

        /// <summary>
        /// Get the PresenceManager service instance through the LifecycleManager.
        /// Services are automatically registered and initialized on first access if needed.
        /// </summary>
        public static LootLockerPresenceManager Get()
        {
            if (_instance != null)
            {
                return _instance;
            }
            
            lock (_instanceLock)
            {
                if (_instance == null)
                {
                    // Register with LifecycleManager (will auto-initialize if needed)
                    _instance = LootLockerLifecycleManager.GetService<LootLockerPresenceManager>();
                }
                return _instance;
            }
        }
        
        #endregion

        private IEnumerator AutoConnectExistingSessions()
        {
            // Wait a frame to ensure everything is initialized
            yield return null;
            
            if (!isEnabled || !autoConnectEnabled)
            {
                yield break;
            }

            // Get all active sessions from state data and auto-connect
            var activePlayerUlids = LootLockerStateData.GetActivePlayerULIDs();
            if (activePlayerUlids != null)
            {
                foreach (var ulid in activePlayerUlids)
                {
                    if (!string.IsNullOrEmpty(ulid))
                    {
                        var state = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(ulid);
                        if (state == null)
                        {
                            continue;
                        }

                        // Check if we already have an active or in-progress presence client for this ULID
                        bool shouldConnect = false;
                        lock (activeClientsLock)
                        {
                            // Check if already connecting
                            if (connectingClients.Contains(state.ULID))
                            {
                                LootLockerLogger.Log($"Presence already connecting for session: {state.ULID}, skipping auto-connect", LootLockerLogger.LogLevel.Verbose);
                                shouldConnect = false;
                            }
                            else if (!activeClients.ContainsKey(state.ULID))
                            {
                                shouldConnect = true;
                            }
                            else
                            {
                                // Check if existing client is in a failed or disconnected state
                                var existingClient = activeClients[state.ULID];
                                var clientState = existingClient.ConnectionState;
                                
                                if (clientState == LootLockerPresenceConnectionState.Failed ||
                                    clientState == LootLockerPresenceConnectionState.Disconnected)
                                {
                                    LootLockerLogger.Log($"Auto-connect found failed/disconnected client for {state.ULID}, will reconnect", LootLockerLogger.LogLevel.Verbose);
                                    shouldConnect = true;
                                }
                                else
                                {
                                    LootLockerLogger.Log($"Presence already active or in progress for session: {state.ULID} (state: {clientState}), skipping auto-connect", LootLockerLogger.LogLevel.Verbose);
                                }
                            }
                        }

                        if (shouldConnect)
                        {
                            LootLockerLogger.Log($"Auto-connecting presence for existing session: {state.ULID}", LootLockerLogger.LogLevel.Verbose);
                            ConnectPresence(state.ULID);
                            
                            // Small delay between connections to avoid overwhelming the system
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
            }
        }

        #region Private Fields

        /// <summary>
        /// Track connected sessions for proper cleanup
        /// </summary>
        private readonly HashSet<string> _connectedSessions = new HashSet<string>();

        // Instance fields
        private readonly Dictionary<string, LootLockerPresenceClient> activeClients = new Dictionary<string, LootLockerPresenceClient>();
        private readonly HashSet<string> connectingClients = new HashSet<string>(); // Track clients that are in the process of connecting
        private readonly object activeClientsLock = new object(); // Thread safety for activeClients dictionary
        private bool isEnabled = true;
        private bool autoConnectEnabled = true;

        #endregion

        #region Event Subscriptions

        /// <summary>
        /// Subscribe to session lifecycle events
        /// </summary>
        private void SubscribeToSessionEvents()
        {
            // Subscribe to session started events
            LootLockerEventSystem.Subscribe<LootLockerSessionStartedEventData>(
                LootLockerEventType.SessionStarted,
                OnSessionStartedEvent
            );

            // Subscribe to session refreshed events
            LootLockerEventSystem.Subscribe<LootLockerSessionRefreshedEventData>(
                LootLockerEventType.SessionRefreshed,
                OnSessionRefreshedEvent
            );

            // Subscribe to session ended events
            LootLockerEventSystem.Subscribe<LootLockerSessionEndedEventData>(
                LootLockerEventType.SessionEnded,
                OnSessionEndedEvent
            );

            // Subscribe to session expired events
            LootLockerEventSystem.Subscribe<LootLockerSessionExpiredEventData>(
                LootLockerEventType.SessionExpired,
                OnSessionExpiredEvent
            );

            // Subscribe to local session deactivated events
            LootLockerEventSystem.Subscribe<LootLockerLocalSessionDeactivatedEventData>(
                LootLockerEventType.LocalSessionDeactivated,
                OnLocalSessionDeactivatedEvent
            );

            // Subscribe to local session activated events
            LootLockerEventSystem.Subscribe<LootLockerLocalSessionActivatedEventData>(
                LootLockerEventType.LocalSessionActivated,
                OnLocalSessionActivatedEvent
            );
        }

        /// <summary>
        /// Unsubscribe from session lifecycle events
        /// </summary>
        private void UnsubscribeFromSessionEvents()
        {
            LootLockerEventSystem.Unsubscribe<LootLockerSessionStartedEventData>(
                LootLockerEventType.SessionStarted,
                OnSessionStartedEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerSessionRefreshedEventData>(
                LootLockerEventType.SessionRefreshed,
                OnSessionRefreshedEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerSessionEndedEventData>(
                LootLockerEventType.SessionEnded,
                OnSessionEndedEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerSessionExpiredEventData>(
                LootLockerEventType.SessionExpired,
                OnSessionExpiredEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerLocalSessionDeactivatedEventData>(
                LootLockerEventType.LocalSessionDeactivated,
                OnLocalSessionDeactivatedEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerLocalSessionActivatedEventData>(
                LootLockerEventType.LocalSessionActivated,
                OnLocalSessionActivatedEvent
            );
        }

        /// <summary>
        /// Handle session started events
        /// </summary>
        private void OnSessionStartedEvent(LootLockerSessionStartedEventData eventData)
        {
            if (!isEnabled || !autoConnectEnabled)
            {
                return;
            }

            var playerData = eventData.playerData;
            if (playerData != null && !string.IsNullOrEmpty(playerData.ULID))
            {
                LootLockerLogger.Log($"Session started event received for {playerData.ULID}, auto-connecting presence", LootLockerLogger.LogLevel.Verbose);
                ConnectPresence(playerData.ULID);
            }
        }

        /// <summary>
        /// Handle session refreshed events
        /// </summary>
        private void OnSessionRefreshedEvent(LootLockerSessionRefreshedEventData eventData)
        {
            if (!isEnabled)
            {
                return;
            }

            var playerData = eventData.playerData;
            if (playerData != null && !string.IsNullOrEmpty(playerData.ULID))
            {
                LootLockerLogger.Log($"Session refreshed event received for {playerData.ULID}, reconnecting presence with new token", LootLockerLogger.LogLevel.Verbose);
                
                // Disconnect existing connection first, then reconnect with new session token
                DisconnectPresence(playerData.ULID, (disconnectSuccess, disconnectError) => {
                    if (disconnectSuccess)
                    {
                        // Only reconnect if auto-connect is enabled
                        if (autoConnectEnabled)
                        {
                            LootLockerLogger.Log($"Reconnecting presence for {playerData.ULID} with refreshed session token", LootLockerLogger.LogLevel.Verbose);
                            ConnectPresence(playerData.ULID);
                        }
                    }
                    else
                    {
                        LootLockerLogger.Log($"Failed to disconnect presence during session refresh for {playerData.ULID}: {disconnectError}", LootLockerLogger.LogLevel.Error);
                    }
                });
            }
        }

        /// <summary>
        /// Handle session ended events
        /// </summary>
        private void OnSessionEndedEvent(LootLockerSessionEndedEventData eventData)
        {
            if (!string.IsNullOrEmpty(eventData.playerUlid))
            {
                LootLockerLogger.Log($"Session ended event received for {eventData.playerUlid}, disconnecting presence", LootLockerLogger.LogLevel.Verbose);
                DisconnectPresence(eventData.playerUlid);
            }
        }

        /// <summary>
        /// Handle session expired events
        /// </summary>
        private void OnSessionExpiredEvent(LootLockerSessionExpiredEventData eventData)
        {
            if (!string.IsNullOrEmpty(eventData.playerUlid))
            {
                LootLockerLogger.Log($"Session expired event received for {eventData.playerUlid}, disconnecting presence", LootLockerLogger.LogLevel.Verbose);
                DisconnectPresence(eventData.playerUlid);
            }
        }

        /// <summary>
        /// Handle local session deactivated events
        /// </summary>
        private void OnLocalSessionDeactivatedEvent(LootLockerLocalSessionDeactivatedEventData eventData)
        {
            if (!string.IsNullOrEmpty(eventData.playerUlid))
            {
                LootLockerLogger.Log($"Local session deactivated event received for {eventData.playerUlid}, disconnecting presence", LootLockerLogger.LogLevel.Verbose);
                DisconnectPresence(eventData.playerUlid);
            }
        }

        /// <summary>
        /// Handles local session activation by checking if presence and auto-connect are enabled,
        /// and, if so, automatically connects presence for the activated player session.
        /// </summary>
        private void OnLocalSessionActivatedEvent(LootLockerLocalSessionActivatedEventData eventData)
        {
            if (!isEnabled || !autoConnectEnabled)
            {
                return;
            }

            var playerData = eventData.playerData;
            if (playerData != null && !string.IsNullOrEmpty(playerData.ULID))
            {
                LootLockerLogger.Log($"Session activated event received for {playerData.ULID}, auto-connecting presence", LootLockerLogger.LogLevel.Verbose);
                ConnectPresence(playerData.ULID);
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Event fired when any presence connection state changes
        /// </summary>
        public static event LootLockerPresenceConnectionStateChanged OnConnectionStateChanged;

        /// <summary>
        /// Event fired when any presence message is received
        /// </summary>
        public static event LootLockerPresenceMessageReceived OnMessageReceived;

        /// <summary>
        /// Event fired when any ping response is received
        /// </summary>
        public static event LootLockerPresencePingReceived OnPingReceived;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the presence system is enabled
        /// </summary>
        public static bool IsEnabled
        {
            get => Get().isEnabled;
            set 
            {
                var instance = Get();
                if (!value && instance.isEnabled)
                {
                    DisconnectAll();
                }
                instance.isEnabled = value;
            }
        }

        /// <summary>
        /// Whether presence should automatically connect when sessions are started
        /// </summary>
        public static bool AutoConnectEnabled
        {
            get => Get().autoConnectEnabled;
            set => Get().autoConnectEnabled = value;
        }

        /// <summary>
        /// Get all active presence client ULIDs
        /// </summary>
        public static IEnumerable<string> ActiveClientUlids 
        {
            get
            {
                var instance = Get();
                lock (instance.activeClientsLock)
                {
                    return new List<string>(instance.activeClients.Keys);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the presence manager (called automatically by SDK)
        /// </summary>
        internal static void Initialize()
        {
            var instance = Get(); // This will create the instance if it doesn't exist
            
            // Set enabled state from config once at initialization
            instance.isEnabled = LootLockerConfig.IsPresenceEnabledForCurrentPlatform();
            
            if (!instance.isEnabled)
            {
                var currentPlatform = LootLockerConfig.GetCurrentPresencePlatform();
                LootLockerLogger.Log($"Presence disabled for current platform: {currentPlatform}", LootLockerLogger.LogLevel.Verbose);
                return;
            }
        }

        /// <summary>
        /// Connect presence for a specific player session
        /// </summary>
        public static void ConnectPresence(string playerUlid = null, LootLockerPresenceCallback onComplete = null)
        {
            var instance = Get();
            
            if (!instance.isEnabled)
            {
                var currentPlatform = LootLockerConfig.GetCurrentPresencePlatform();
                string errorMessage = $"Presence is disabled for current platform: {currentPlatform}. Enable it in Project Settings > LootLocker SDK > Presence Settings.";
                LootLockerLogger.Log(errorMessage, LootLockerLogger.LogLevel.Verbose);
                onComplete?.Invoke(false, errorMessage);
                return;
            }

            // Get player data
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
            if (playerData == null || string.IsNullOrEmpty(playerData.SessionToken))
            {
                LootLockerLogger.Log("Cannot connect presence: No valid session token found", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(false, "No valid session token found");
                return;
            }

            string ulid = playerData.ULID;
            if (string.IsNullOrEmpty(ulid))
            {
                LootLockerLogger.Log("Cannot connect presence: No valid player ULID found", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(false, "No valid player ULID found");
                return;
            }

            lock (instance.activeClientsLock)
            {
                // Check if already connecting
                if (instance.connectingClients.Contains(ulid))
                {
                    LootLockerLogger.Log($"Presence client for {ulid} is already being connected, skipping new connection attempt", LootLockerLogger.LogLevel.Verbose);
                    onComplete?.Invoke(false, "Already connecting");
                    return;
                }

                if (instance.activeClients.ContainsKey(ulid))
                {
                    var existingClient = instance.activeClients[ulid];
                    var state = existingClient.ConnectionState;
                    
                    if (existingClient.IsConnectedAndAuthenticated)
                    {
                        onComplete?.Invoke(true);
                        return;
                    }
                    
                    // If client is in any active state (connecting, authenticating), don't interrupt it
                    if (existingClient.IsConnecting ||
                        existingClient.IsAuthenticating)
                    {
                        LootLockerLogger.Log($"Presence client for {ulid} is already in progress (state: {state}), skipping new connection attempt", LootLockerLogger.LogLevel.Verbose);
                        onComplete?.Invoke(false, $"Already in progress (state: {state})");
                        return;
                    }
                    
                    // Clean up existing client that's failed or disconnected
                    DisconnectPresence(ulid, (success, error) => {
                        if (success)
                        {
                            // Try connecting again after cleanup
                            ConnectPresence(playerUlid, onComplete);
                        }
                        else
                        {
                            onComplete?.Invoke(false, "Failed to cleanup existing connection");
                        }
                    });
                    return;
                }

                // Mark as connecting to prevent race conditions
                instance.connectingClients.Add(ulid);
            }

            // Create and connect client outside the lock
            LootLockerPresenceClient client = null;
            try
            {
                client = instance.gameObject.AddComponent<LootLockerPresenceClient>();
                client.Initialize(ulid, playerData.SessionToken);

                // Subscribe to events
                client.OnConnectionStateChanged += (state, error) => OnConnectionStateChanged?.Invoke(ulid, state, error);
                client.OnMessageReceived += (message, messageType) => OnMessageReceived?.Invoke(ulid, message, messageType);
                client.OnPingReceived += (pingResponse) => OnPingReceived?.Invoke(ulid, pingResponse);
            }
            catch (Exception ex)
            {
                // Clean up on creation failure
                lock (instance.activeClientsLock)
                {
                    instance.connectingClients.Remove(ulid);
                }
                if (client != null)
                {
                    UnityEngine.Object.Destroy(client);
                }
                LootLockerLogger.Log($"Failed to create presence client for {ulid}: {ex.Message}", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(false, $"Failed to create presence client: {ex.Message}");
                return;
            }

            // Start connection
            client.Connect((success, error) => {
                lock (instance.activeClientsLock)
                {
                    // Remove from connecting set
                    instance.connectingClients.Remove(ulid);
                    
                    if (success)
                    {
                        // Add to active clients on success
                        instance.activeClients[ulid] = client;
                    }
                    else
                    {
                        // Clean up on failure
                        UnityEngine.Object.Destroy(client);
                    }
                }
                onComplete?.Invoke(success, error);
            });
        }

        /// <summary>
        /// Disconnect presence for a specific player session
        /// </summary>
        public static void DisconnectPresence(string playerUlid = null, LootLockerPresenceCallback onComplete = null)
        {
            var instance = Get();
            string ulid = playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
                ulid = playerData?.ULID;
            }

            if (string.IsNullOrEmpty(ulid))
            {
                onComplete?.Invoke(true);
                return;
            }

            LootLockerPresenceClient client = null;
            
            lock (instance.activeClientsLock)
            {
                if (!instance.activeClients.ContainsKey(ulid))
                {
                    onComplete?.Invoke(true);
                    return;
                }

                client = instance.activeClients[ulid];
                instance.activeClients.Remove(ulid);
            }

            if (client != null)
            {
                client.Disconnect((success, error) => {
                    UnityEngine.Object.Destroy(client);
                    onComplete?.Invoke(success, error);
                });
            }
            else
            {
                onComplete?.Invoke(true);
            }
        }

        /// <summary>
        /// Disconnect all presence connections
        /// </summary>
        public static void DisconnectAll()
        {
            var instance = Get();
            
            List<string> ulidsToDisconnect;
            lock (instance.activeClientsLock)
            {
                ulidsToDisconnect = new List<string>(instance.activeClients.Keys);
                // Clear connecting clients as we're disconnecting everything
                instance.connectingClients.Clear();
            }
            
            foreach (var ulid in ulidsToDisconnect)
            {
                DisconnectPresence(ulid);
            }
        }

        /// <summary>
        /// Update presence status for a specific player
        /// </summary>
        public static void UpdatePresenceStatus(string status, string metadata = null, string playerUlid = null, LootLockerPresenceCallback onComplete = null)
        {
            var instance = Get();
            if (!instance.isEnabled)
            {
                onComplete?.Invoke(false, "Presence system is disabled");
                return;
            }

            string ulid = playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
                ulid = playerData?.ULID;
            }

            LootLockerPresenceClient client = null;
            lock (instance.activeClientsLock)
            {
                if (string.IsNullOrEmpty(ulid) || !instance.activeClients.ContainsKey(ulid))
                {
                    onComplete?.Invoke(false, "No active presence connection found");
                    return;
                }
                client = instance.activeClients[ulid];
            }

            client.UpdateStatus(status, metadata, onComplete);
        }

        /// <summary>
        /// Get presence connection state for a specific player
        /// </summary>
        public static LootLockerPresenceConnectionState GetPresenceConnectionState(string playerUlid = null)
        {
            var instance = Get();
            string ulid = playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
                ulid = playerData?.ULID;
            }

            lock (instance.activeClientsLock)
            {
                if (string.IsNullOrEmpty(ulid) || !instance.activeClients.ContainsKey(ulid))
                {
                    return LootLockerPresenceConnectionState.Disconnected;
                }

                return instance.activeClients[ulid].ConnectionState;
            }
        }

        /// <summary>
        /// Check if presence is connected for a specific player
        /// </summary>
        public static bool IsPresenceConnected(string playerUlid = null)
        {
            return GetPresenceConnectionState(playerUlid) == LootLockerPresenceConnectionState.Authenticated;
        }

        /// <summary>
        /// Get the presence client for a specific player
        /// </summary>
        /// <param name="playerUlid">Optional : Get the client for the specified player. If not supplied, the default player will be used.</param>
        /// <returns>The active LootLockerPresenceClient instance, or null if not connected</returns>
        public static LootLockerPresenceClient GetPresenceClient(string playerUlid = null)
        {
            var instance = Get();
            string ulid = playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
                ulid = playerData?.ULID;
            }

            lock (instance.activeClientsLock)
            {
                if (string.IsNullOrEmpty(ulid) || !instance.activeClients.ContainsKey(ulid))
                {
                    return null;
                }

                return instance.activeClients[ulid];
            }
        }

        /// <summary>
        /// Get connection statistics including latency to LootLocker for a specific player
        /// </summary>
        public static LootLockerPresenceConnectionStats GetPresenceConnectionStats(string playerUlid = null)
        {
            var instance = Get();
            string ulid = playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
                ulid = playerData?.ULID;
            }

            lock (instance.activeClientsLock)
            {
                if (string.IsNullOrEmpty(ulid) || !instance.activeClients.ContainsKey(ulid))
                {
                    return null;
                }

                return instance.activeClients[ulid].ConnectionStats;
            }
        }

        #endregion

        #region Unity Lifecycle Events

        private void OnDestroy()
        {
            UnsubscribeFromSessionEvents();
            
            DisconnectAll();

            LootLockerLifecycleManager.UnregisterService<LootLockerPresenceManager>();
        }

        #endregion
    }
}
#endif