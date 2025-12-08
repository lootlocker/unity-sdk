#if LOOTLOCKER_ENABLE_PRESENCE
using System;
using System.Collections;
using System.Collections.Generic;
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
        private bool autoDisconnectOnFocusChange = false; // Developer-configurable setting for focus-based disconnection
        private bool isShuttingDown = false; // Track if we're shutting down to prevent double disconnect

        #endregion

        #region ILootLockerService Implementation

        public bool IsInitialized { get; private set; } = false;
        public string ServiceName => "PresenceManager";

        void ILootLockerService.Initialize()
        {
            if (IsInitialized) return;
            isEnabled = LootLockerConfig.current.enablePresence;
            autoConnectEnabled = LootLockerConfig.current.enablePresenceAutoConnect;
            autoDisconnectOnFocusChange = LootLockerConfig.current.enablePresenceAutoDisconnectOnFocusChange;
            
            IsInitialized = true;
            
            // Defer event subscriptions and auto-connect to avoid circular dependencies during service initialization
            StartCoroutine(DeferredInitialization());
        }
        
        /// <summary>
        /// Perform deferred initialization after services are fully ready
        /// </summary>
        private IEnumerator DeferredInitialization()
        {
            // Wait a frame to ensure all services are fully initialized
            yield return null;

            if (!isEnabled)
            {
                yield break;
            }
            
            // Subscribe to session events (handle errors separately)
            try
            {
                SubscribeToSessionEvents();
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error subscribing to session events: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
            
            // Auto-connect existing active sessions if enabled
            yield return StartCoroutine(AutoConnectExistingSessions());
        }

        void ILootLockerService.Reset()
        {
            DisconnectAllInternal();
            
            UnsubscribeFromSessionEvents();
            
            _connectedSessions?.Clear();
            
            IsInitialized = false;
            lock(_instanceLock) {
                _instance = null;
            }
        }

        // TODO: Handle pause/focus better to avoid concurrency issues
        void ILootLockerService.HandleApplicationPause(bool pauseStatus)
        {
            if(!IsInitialized || !autoDisconnectOnFocusChange || !isEnabled)
            {
                return;
            }

            if (pauseStatus)
            {
                LootLockerLogger.Log("Application paused - disconnecting all presence connections (auto-disconnect enabled)", LootLockerLogger.LogLevel.Debug);
                DisconnectAll();
            }
            else
            {
                LootLockerLogger.Log("Application resumed - will reconnect presence connections", LootLockerLogger.LogLevel.Debug);
                StartCoroutine(AutoConnectExistingSessions());
            }
        }

        void ILootLockerService.HandleApplicationFocus(bool hasFocus)
        {
            if(!IsInitialized || !autoDisconnectOnFocusChange || !isEnabled)
                return;

            if (hasFocus)
            {
                // App gained focus - ensure presence is reconnected
                LootLockerLogger.Log("Application gained focus - ensuring presence connections (auto-disconnect enabled)", LootLockerLogger.LogLevel.Debug);
                StartCoroutine(AutoConnectExistingSessions());
            }
            else
            {
                // App lost focus - disconnect presence to save resources
                LootLockerLogger.Log("Application lost focus - disconnecting presence (auto-disconnect enabled)", LootLockerLogger.LogLevel.Debug);
                DisconnectAll();
            }
        }

        void ILootLockerService.HandleApplicationQuit()
        {
            isShuttingDown = true;
            
            UnsubscribeFromSessionEvents();
            DisconnectAllInternal();
            _connectedSessions?.Clear();
        }

        #endregion


        #region Singleton Management
        
        private static LootLockerPresenceManager _instance;
        private static readonly object _instanceLock = new object();

        /// <summary>
        /// Get the PresenceManager service instance
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
                                    shouldConnect = true;
                                }
                            }
                        }

                        if (shouldConnect)
                        {
                            LootLockerLogger.Log($"Auto-connecting presence for existing session: {state.ULID}", LootLockerLogger.LogLevel.Debug);
                            ConnectPresence(state.ULID);
                            
                            // Small delay between connections to avoid overwhelming the system
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
            }
        }

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
            if (!LootLockerLifecycleManager.HasService<LootLockerEventSystem>() || isShuttingDown)
            {
                return;
            }
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
                LootLockerLogger.Log($"Session started event received for {playerData.ULID}, auto-connecting presence", LootLockerLogger.LogLevel.Debug);
                
                // Create and initialize client immediately, but defer connection
                var client = CreateAndInitializePresenceClient(playerData);
                if (client == null)
                {
                    return;
                }

                // Start auto-connect in a coroutine to avoid blocking the event thread
                StartCoroutine(AutoConnectPresenceCoroutine(playerData));
            }
        }

        /// <summary>
        /// Coroutine to handle auto-connecting presence after session events
        /// </summary>
        private System.Collections.IEnumerator AutoConnectPresenceCoroutine(LootLockerPlayerData playerData)
        {
            // Yield one frame to let the session event complete fully
            yield return null;
            
            var instance = Get();
            if (instance == null)
            {
                yield break;
            }

            LootLockerPresenceClient existingClient = null;

            lock (instance.activeClientsLock)
            {
                // Check if already connected for this player
                if (instance.activeClients.ContainsKey(playerData.ULID))
                {
                    existingClient = instance.activeClients[playerData.ULID];
                }
            }
            
            // Now attempt to connect the pre-created client
            ConnectExistingPresenceClient(playerData.ULID, existingClient);
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
                LootLockerLogger.Log($"Session refreshed event received for {playerData.ULID}, reconnecting presence with new token", LootLockerLogger.LogLevel.Debug);
                
                // Disconnect existing connection first, then reconnect with new session token
                DisconnectPresence(playerData.ULID, (disconnectSuccess, disconnectError) => {
                    if (disconnectSuccess)
                    {
                        // Only reconnect if auto-connect is enabled
                        if (autoConnectEnabled)
                        {
                            ConnectPresence(playerData.ULID);
                        }
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
                LootLockerLogger.Log($"Session ended event received for {eventData.playerUlid}, disconnecting presence", LootLockerLogger.LogLevel.Debug);
                _DisconnectPresenceForUlid(eventData.playerUlid);
            }
        }

        /// <summary>
        /// Handle session expired events
        /// </summary>
        private void OnSessionExpiredEvent(LootLockerSessionExpiredEventData eventData)
        {
            if (!string.IsNullOrEmpty(eventData.playerUlid))
            {
                LootLockerLogger.Log($"Session expired event received for {eventData.playerUlid}, disconnecting presence", LootLockerLogger.LogLevel.Debug);
                _DisconnectPresenceForUlid(eventData.playerUlid);
            }
        }

        /// <summary>
        /// Handle local session deactivated events
        /// Note: If this is part of a session end flow, presence will already be disconnected by OnSessionEndedEvent
        /// This handler only disconnects presence for local state management scenarios
        /// </summary>
        private void OnLocalSessionDeactivatedEvent(LootLockerLocalSessionDeactivatedEventData eventData)
        {
            if (!string.IsNullOrEmpty(eventData.playerUlid))
            {
                LootLockerLogger.Log($"Local session deactivated event received for {eventData.playerUlid}, disconnecting presence", LootLockerLogger.LogLevel.Debug);
                _DisconnectPresenceForUlid(eventData.playerUlid);
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
                LootLockerLogger.Log($"Session activated event received for {playerData.ULID}, auto-connecting presence", LootLockerLogger.LogLevel.Debug);
                ConnectPresence(playerData.ULID);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the presence system is enabled
        /// </summary>
        public static bool IsEnabled
        {
            get => Get()?.isEnabled ?? false;
            set 
            {
                var instance = Get();
                if(!instance)
                    return;
                instance.SetPresenceEnabled(value);
            }
        }

        /// <summary>
        /// Whether presence should automatically connect when sessions are started
        /// </summary>
        public static bool AutoConnectEnabled
        {
            get => Get()?.autoConnectEnabled ?? false;
            set { 
                var instance = Get();
                if (instance != null) 
                {
                    instance.SetAutoConnectEnabled(value);
                }
            }
        }

        /// <summary>
        /// Whether presence should automatically disconnect when the application loses focus or is paused.
        /// When enabled, presence will disconnect when the app goes to background and reconnect when it returns to foreground.
        /// Useful for saving battery on mobile or managing resources.
        /// </summary>
        public static bool AutoDisconnectOnFocusChange
        {
            get => Get()?.autoDisconnectOnFocusChange ?? false;
            set { var instance = Get(); if (instance != null) instance.autoDisconnectOnFocusChange = value; }
        }

        /// <summary>
        /// Get all active presence client ULIDs
        /// </summary>
        public static IEnumerable<string> ActiveClientUlids 
        {
            get
            {
                var instance = Get();
                if (instance == null) return new List<string>();
                
                lock (instance.activeClientsLock)
                {
                    return new List<string>(instance.activeClients.Keys);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Connect presence for a specific player session
        /// </summary>
        public static void ConnectPresence(string playerUlid = null, LootLockerPresenceCallback onComplete = null)
        {
            var instance = Get();
            if (instance == null)
            {
                onComplete?.Invoke(false, "PresenceManager not available");
                return;
            }

            if (!instance.isEnabled)
            {
                string errorMessage = "Presence is disabled. Enable it in Project Settings > LootLocker SDK > Presence Settings or use SetPresenceEnabled(true).";
                LootLockerLogger.Log(errorMessage, LootLockerLogger.LogLevel.Debug);
                onComplete?.Invoke(false, errorMessage);
                return;
            }

            // Get player data
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
            if (playerData == null || string.IsNullOrEmpty(playerData.SessionToken))
            {
                LootLockerLogger.Log("Cannot connect presence: No valid session token found", LootLockerLogger.LogLevel.Warning);
                onComplete?.Invoke(false, "No valid session token found");
                return;
            }

            string ulid = playerData.ULID;
            if (string.IsNullOrEmpty(ulid))
            {
                LootLockerLogger.Log("Cannot connect presence: No valid player ULID found", LootLockerLogger.LogLevel.Warning);
                onComplete?.Invoke(false, "No valid player ULID found");
                return;
            }

            // Early out if presence is not enabled (redundant, but ensures future-proofing)
            if (!IsEnabled)
            {
                onComplete?.Invoke(false, "Presence is disabled");
                return;
            }

            lock (instance.activeClientsLock)
            {
                // Check if already connecting
                if (instance.connectingClients.Contains(ulid))
                {
                    LootLockerLogger.Log($"Presence client for {ulid} is already being connected, skipping new connection attempt", LootLockerLogger.LogLevel.Debug);
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
                        LootLockerLogger.Log($"Presence client for {ulid} is already in progress (state: {state}), skipping new connection attempt", LootLockerLogger.LogLevel.Debug);
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

                // Subscribe to client events - client will trigger events directly
                // Note: Event unsubscription happens automatically when GameObject is destroyed
                client.OnConnectionStateChanged += (previousState, newState, error) =>
                    Get()?.OnClientConnectionStateChanged(ulid, previousState, newState, error);
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
                LootLockerLogger.Log($"Failed to create presence client for {ulid}: {ex.Message}", LootLockerLogger.LogLevel.Warning);
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
            if (instance == null)
            {
                onComplete?.Invoke(false, "PresenceManager not available");
                return;
            }

            if (!instance.isEnabled)
            {
                onComplete?.Invoke(false, "Presence is disabled");
                return;
            }

            string ulid = playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
                ulid = playerData?.ULID;
            }

            // Use shared internal disconnect logic
            instance._DisconnectPresenceForUlid(ulid, onComplete);
        }

        /// <summary>
        /// Shared internal method for disconnecting a presence client by ULID
        /// </summary>
        private void _DisconnectPresenceForUlid(string playerUlid, LootLockerPresenceCallback onComplete = null)
        {
            if (string.IsNullOrEmpty(playerUlid))
            {
                onComplete?.Invoke(true);
                return;
            }

            LootLockerPresenceClient client = null;
            bool alreadyDisconnectedOrFailed = false;

            lock (activeClientsLock)
            {
                if (!activeClients.TryGetValue(playerUlid, out client))
                {
                    onComplete?.Invoke(true);
                    return;
                }

                // Check connection state to prevent multiple disconnect attempts
                var connectionState = client.ConnectionState;
                if (connectionState == LootLockerPresenceConnectionState.Disconnected ||
                    connectionState == LootLockerPresenceConnectionState.Failed)
                {
                    alreadyDisconnectedOrFailed = true;
                }

                // Remove from activeClients immediately to prevent other events from trying to disconnect
                activeClients.Remove(playerUlid);
            }

            // Disconnect outside the lock to avoid blocking other operations
            if (client != null)
            {
                if (alreadyDisconnectedOrFailed)
                {
                    UnityEngine.Object.Destroy(client);
                    onComplete?.Invoke(true);
                }
                else
                {
                    client.Disconnect((success, error) => {
                        if (!success)
                        {
                            LootLockerLogger.Log($"Error disconnecting presence for {playerUlid}: {error}", LootLockerLogger.LogLevel.Debug);
                        }
                        UnityEngine.Object.Destroy(client);
                        onComplete?.Invoke(success, error);
                    });
                }
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
            Get()?.DisconnectAllInternal();
        }

        /// <summary>
        /// Disconnect all presence connections
        /// </summary>
        private void DisconnectAllInternal()
        {
            List<string> ulidsToDisconnect;
            lock (activeClientsLock)
            {
                ulidsToDisconnect = new List<string>(activeClients.Keys);
                // Clear connecting clients as we're disconnecting everything
                connectingClients.Clear();
            }
            
            foreach (var ulid in ulidsToDisconnect)
            {
                _DisconnectPresenceForUlid(ulid);
            }
        }

        /// <summary>
        /// Update presence status for a specific player
        /// </summary>
        public static void UpdatePresenceStatus(string status, Dictionary<string, string> metadata = null, string playerUlid = null, LootLockerPresenceCallback onComplete = null)
        {
            var instance = Get();
            if (instance == null)
            {
                onComplete?.Invoke(false, "PresenceManager not available");
                return;
            }
            
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

            if (string.IsNullOrEmpty(ulid))
            {
                onComplete?.Invoke(false, "No valid player ULID found");
                return;
            }

            LootLockerPresenceClient client = null;
            lock (instance.activeClientsLock)
            {
                if (!instance.activeClients.ContainsKey(ulid))
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
            if (instance == null) return LootLockerPresenceConnectionState.Disconnected;
            
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
            return GetPresenceConnectionState(playerUlid) == LootLockerPresenceConnectionState.Active;
        }

        /// <summary>
        /// Get connection statistics including latency to LootLocker for a specific player
        /// </summary>
        public static LootLockerPresenceConnectionStats GetPresenceConnectionStats(string playerUlid = null)
        {
            var instance = Get();
            if (instance == null) return new LootLockerPresenceConnectionStats();
            
            string ulid = playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
                ulid = playerData?.ULID;
            }

            lock (instance.activeClientsLock)
            {
                if (string.IsNullOrEmpty(ulid))
                {
                    return null;
                }
                
                if (!instance.activeClients.ContainsKey(ulid))
                {
                    return null;
                }

                var client = instance.activeClients[ulid];
                return client.ConnectionStats;
            }
        }

        /// <summary>
        /// Get the last status that was sent for a specific player
        /// </summary>
        /// <param name="playerUlid">Optional: The player's ULID. If not provided, uses the default player</param>
        /// <returns>The last sent status string, or null if no client is found or no status has been sent</returns>
        public static string GetLastSentStatus(string playerUlid = null)
        {
            var instance = Get();
            if (instance == null) return string.Empty;
            
            string ulid = playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
                ulid = playerData?.ULID;
            }

            lock (instance.activeClientsLock)
            {
                if (string.IsNullOrEmpty(ulid))
                {
                    return null;
                }
                
                if (!instance.activeClients.ContainsKey(ulid))
                {
                    return null;
                }

                var client = instance.activeClients[ulid];
                return client.LastSentStatus;
            }
        }

        #endregion

        #region Private Helper Methods

        private void SetPresenceEnabled(bool enabled)
        {
            bool changingState = isEnabled != enabled;
            isEnabled = enabled;
            if(changingState && enabled && autoConnectEnabled)
            {
                SubscribeToSessionEvents();
                StartCoroutine(AutoConnectExistingSessions());
            } 
            else if (changingState && !enabled)
            {
                UnsubscribeFromSessionEvents();
                DisconnectAllInternal();
            }
        }

        private void SetAutoConnectEnabled(bool enabled)
        {
            bool changingState = autoConnectEnabled != enabled;
            autoConnectEnabled = enabled;
            if(changingState && isEnabled && enabled)
            {
                SubscribeToSessionEvents();
                StartCoroutine(AutoConnectExistingSessions());
            } 
            else if (changingState && !enabled)
            {
                UnsubscribeFromSessionEvents();
                DisconnectAllInternal();
            }
        }

        /// <summary>
        /// Handle client state changes for automatic cleanup
        /// </summary>
        private void HandleClientStateChange(string playerUlid, LootLockerPresenceConnectionState newState)
        {
            // Auto-cleanup clients that become disconnected or failed
            if (newState == LootLockerPresenceConnectionState.Disconnected ||
                newState == LootLockerPresenceConnectionState.Failed)
            {
                LootLockerLogger.Log($"Auto-cleaning up presence client for {playerUlid} due to state change: {newState}", LootLockerLogger.LogLevel.Debug);
                
                // Clean up the client from our tracking
                LootLockerPresenceClient clientToCleanup = null;
                lock (activeClientsLock)
                {
                    if (activeClients.TryGetValue(playerUlid, out clientToCleanup))
                    {
                        activeClients.Remove(playerUlid);
                    }
                }
                
                // Destroy the GameObject to fully clean up resources
                if (clientToCleanup != null)
                {
                    UnityEngine.Object.Destroy(clientToCleanup.gameObject);
                }
            }
        }

        /// <summary>
        /// Handle connection state changed events from individual presence clients
        /// </summary>
        private void OnClientConnectionStateChanged(string playerUlid, LootLockerPresenceConnectionState previousState, LootLockerPresenceConnectionState newState, string error)
        {
            // First handle internal cleanup and management
            HandleClientStateChange(playerUlid, newState);
            
            // Then notify external systems via the unified event system
            LootLockerEventSystem.TriggerPresenceConnectionStateChanged(playerUlid, previousState, newState, error);
        }

        /// <summary>
        /// Creates and initializes a presence client without connecting it
        /// </summary>
        private LootLockerPresenceClient CreateAndInitializePresenceClient(LootLockerPlayerData playerData)
        {
            var instance = Get();
            if (instance == null) return null;
            
            if (!instance.isEnabled)
            {
                return null;
            }

            // Use the provided player data directly
            if (playerData == null || string.IsNullOrEmpty(playerData.ULID) || string.IsNullOrEmpty(playerData.SessionToken))
            {
                return null;
            }

            lock (instance.activeClientsLock)
            {
                // Check if already connected for this player
                if (instance.activeClients.ContainsKey(playerData.ULID))
                {
                    LootLockerLogger.Log($"Presence already connected for player {playerData.ULID}", LootLockerLogger.LogLevel.Debug);
                    return instance.activeClients[playerData.ULID];
                }

                // Create new presence client as a GameObject component
                var clientGameObject = new GameObject($"PresenceClient_{playerData.ULID}");
                clientGameObject.transform.SetParent(instance.transform);
                var client = clientGameObject.AddComponent<LootLockerPresenceClient>();
                
                client.Initialize(playerData.ULID, playerData.SessionToken);
                
                // Add to active clients immediately
                instance.activeClients[playerData.ULID] = client;
                
                return client;
            }
        }

        /// <summary>
        /// Connects an existing presence client
        /// </summary>
        private void ConnectExistingPresenceClient(string ulid, LootLockerPresenceClient client, LootLockerPresenceCallback onComplete = null)
        {
            if (client == null)
            {
                onComplete?.Invoke(false, "Client is null");
                return;
            }

            client.Connect((success, error) =>
            {
                if (!success)
                {
                    DisconnectPresence(ulid);
                }
                
                onComplete?.Invoke(success, error);
            });
        }

        #endregion

        #region Unity Lifecycle Events

        private void OnDestroy()
        {
            if (!isShuttingDown)
            {
                isShuttingDown = true;
                UnsubscribeFromSessionEvents();
                
                DisconnectAllInternal();
            }

            // Only unregister if the LifecycleManager exists and we're actually registered
            // During application shutdown, services may already be reset
            try
            {
                if (LootLockerLifecycleManager.Instance != null && 
                    LootLockerLifecycleManager.HasService<LootLockerPresenceManager>())
                {
                    LootLockerLifecycleManager.UnregisterService<LootLockerPresenceManager>();
                }
            }
            catch (System.Exception ex)
            {
                // Ignore unregistration errors during shutdown
                LootLockerLogger.Log($"Error unregistering PresenceManager during shutdown (this is expected): {ex.Message}", LootLockerLogger.LogLevel.Debug);
            }
        }

        #endregion
    }
}
#endif
