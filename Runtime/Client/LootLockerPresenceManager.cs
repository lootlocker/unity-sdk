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
        private readonly Dictionary<string, LootLockerPresenceClient> _activeClients = new Dictionary<string, LootLockerPresenceClient>();
        private readonly Dictionary<string, LootLockerPresenceClient> _disconnectedClients = new Dictionary<string, LootLockerPresenceClient>(); // Track disconnected but not destroyed clients
        private readonly HashSet<string> _connectingClients = new HashSet<string>(); // Track clients that are in the process of connecting
        private readonly object _activeClientsLock = new object(); // Thread safety for _activeClients dictionary
        private bool _isEnabled = true;
        private bool _autoConnectEnabled = true;
        private bool _autoDisconnectOnFocusChange = false; // Developer-configurable setting for focus-based disconnection
        private bool _isShuttingDown = false; // Track if we're shutting down to prevent double disconnect

        #endregion

        #region Public Fields
        /// <summary>
        /// Whether the presence system is enabled
        /// </summary>
        public static bool IsEnabled
        {
            get => Get()?._isEnabled ?? false;
            set 
            {
                var instance = Get();
                if(!instance)
                    return;
                instance._SetPresenceEnabled(value);
            }
        }

        /// <summary>
        /// Whether presence should automatically connect when sessions are started
        /// </summary>
        public static bool AutoConnectEnabled
        {
            get => Get()?._autoConnectEnabled ?? false;
            set { 
                var instance = Get();
                if (instance != null) 
                {
                    instance._SetAutoConnectEnabled(value);
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
            get => Get()?._autoDisconnectOnFocusChange ?? false;
            set { var instance = Get(); if (instance != null) instance._autoDisconnectOnFocusChange = value; }
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
                
                lock (instance._activeClientsLock)
                {
                    var result = new List<string>(instance._activeClients.Keys);
                    result.AddRange(instance._disconnectedClients.Keys);
                    return result;
                }
            }
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
            // During Unity shutdown, don't create new instances
            if (!Application.isPlaying)
            {
                return _instance;
            }

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

        #region ILootLockerService Implementation

        public bool IsInitialized { get; private set; } = false;
        public string ServiceName => "PresenceManager";

        void ILootLockerService.Initialize()
        {
            if (IsInitialized) return;

            #if UNITY_EDITOR
                _isEnabled = LootLockerConfig.current.enablePresence && LootLockerConfig.current.enablePresenceInEditor;
            #else
                _isEnabled = LootLockerConfig.current.enablePresence;
            #endif
            _autoConnectEnabled = LootLockerConfig.current.enablePresenceAutoConnect;
            _autoDisconnectOnFocusChange = LootLockerConfig.current.enablePresenceAutoDisconnectOnFocusChange;
            
            IsInitialized = true;
        }
        
        /// <summary>
        /// Perform deferred initialization after services are fully ready
        /// </summary>
        public void SetEventSystem(LootLockerEventSystem eventSystemInstance)
        {

            if (!_isEnabled || !IsInitialized)
            {
                return;
            }
            
            // Subscribe to session events (handle errors separately)
            try
            {
                _SubscribeToEvents(eventSystemInstance);
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error subscribing to session events: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
            
            // Auto-connect existing active sessions if enabled
            StartCoroutine(_AutoConnectExistingSessions());
        }

        void ILootLockerService.Reset()
        {
            if (!_isShuttingDown)
            {
                _DestroyAllClients();
            }
            
            _UnsubscribeFromEvents();
            
            _connectedSessions?.Clear();
            
            IsInitialized = false;
            lock(_instanceLock) {
                _instance = null;
            }
        }

        void ILootLockerService.HandleApplicationPause(bool pauseStatus)
        {
            if(!IsInitialized || !_isEnabled)
            {
                return;
            }

            if (pauseStatus && _autoDisconnectOnFocusChange)
            {
                DisconnectAll();
            }
            else
            {
                StartCoroutine(_AutoConnectExistingSessions());
            }
        }

        void ILootLockerService.HandleApplicationFocus(bool hasFocus)
        {
            if(!IsInitialized || !_isEnabled)
                return;

            if (hasFocus)
            {
                // App gained focus - ensure presence is reconnected
                StartCoroutine(_AutoConnectExistingSessions());
            }
            else if (_autoDisconnectOnFocusChange)
            {
                // App lost focus - disconnect presence to save resources
                DisconnectAll();
            }
        }

        void ILootLockerService.HandleApplicationQuit()
        {
            if (!_isShuttingDown)
            {
                _isShuttingDown = true;
                
                _UnsubscribeFromEvents();
                _DestroyAllClients();
                _connectedSessions?.Clear();
            }
        }

        #endregion

        #region Event Subscription Handling

        /// <summary>
        /// Subscribe to session lifecycle events
        /// </summary>
        private void _SubscribeToEvents(LootLockerEventSystem eventSystemInstance)
        {
            if (!_isEnabled || _isShuttingDown) 
            {
                return;
            }

            if (eventSystemInstance == null)
            {
                eventSystemInstance = LootLockerLifecycleManager.GetService<LootLockerEventSystem>();
                if (eventSystemInstance == null)
                {
                    LootLockerLogger.Log("Cannot subscribe to session events: EventSystem service not available", LootLockerLogger.LogLevel.Warning);
                    return;
                }
            }

            try {
                // Subscribe to session started events
                eventSystemInstance.SubscribeInstance<LootLockerSessionStartedEventData>(
                    LootLockerEventType.SessionStarted,
                    _HandleSessionStartedEvent
                );

                // Subscribe to session refreshed events
                eventSystemInstance.SubscribeInstance<LootLockerSessionRefreshedEventData>(
                    LootLockerEventType.SessionRefreshed,
                    _HandleSessionRefreshedEvent
                );

                // Subscribe to session ended events
                eventSystemInstance.SubscribeInstance<LootLockerSessionEndedEventData>(
                    LootLockerEventType.SessionEnded,
                    _HandleSessionEndedEvent
                );

                // Subscribe to session expired events
                eventSystemInstance.SubscribeInstance<LootLockerSessionExpiredEventData>(
                    LootLockerEventType.SessionExpired,
                    _HandleSessionExpiredEvent
                );

                // Subscribe to local session deactivated events
                eventSystemInstance.SubscribeInstance<LootLockerLocalSessionDeactivatedEventData>(
                    LootLockerEventType.LocalSessionDeactivated,
                    _HandleLocalSessionDeactivatedEvent
                );

                // Subscribe to local session activated events
                eventSystemInstance.SubscribeInstance<LootLockerLocalSessionActivatedEventData>(
                    LootLockerEventType.LocalSessionActivated,
                    _HandleLocalSessionActivatedEvent
                );

                // Subscribe to presence client connection change events
                eventSystemInstance.SubscribeInstance<LootLockerPresenceConnectionStateChangedEventData>(
                    LootLockerEventType.PresenceConnectionStateChanged,
                    _HandleClientConnectionStateChanged
                );
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error subscribing to session events: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        /// <summary>
        /// Unsubscribe from session lifecycle events
        /// </summary>
        private void _UnsubscribeFromEvents()
        {
            if (!LootLockerLifecycleManager.HasService<LootLockerEventSystem>() || _isShuttingDown)
            {
                return;
            }
            LootLockerEventSystem.Unsubscribe<LootLockerSessionStartedEventData>(
                LootLockerEventType.SessionStarted,
                _HandleSessionStartedEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerSessionRefreshedEventData>(
                LootLockerEventType.SessionRefreshed,
                _HandleSessionRefreshedEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerSessionEndedEventData>(
                LootLockerEventType.SessionEnded,
                _HandleSessionEndedEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerSessionExpiredEventData>(
                LootLockerEventType.SessionExpired,
                _HandleSessionExpiredEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerLocalSessionDeactivatedEventData>(
                LootLockerEventType.LocalSessionDeactivated,
                _HandleLocalSessionDeactivatedEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerLocalSessionActivatedEventData>(
                LootLockerEventType.LocalSessionActivated,
                _HandleLocalSessionActivatedEvent
            );

            LootLockerEventSystem.Unsubscribe<LootLockerPresenceConnectionStateChangedEventData>(
                LootLockerEventType.PresenceConnectionStateChanged,
                _HandleClientConnectionStateChanged
            );
        }

        /// <summary>
        /// Handle session started events
        /// </summary>
        private void _HandleSessionStartedEvent(LootLockerSessionStartedEventData eventData)
        {
            if (!_isEnabled || !_autoConnectEnabled || _isShuttingDown)
            {
                return;
            }

            var playerData = eventData.playerData;
            if (playerData != null && !string.IsNullOrEmpty(playerData.ULID))
            {
                LootLockerLogger.Log($"Session started event received for {playerData.ULID}, checking for existing clients", LootLockerLogger.LogLevel.Debug);
                
                // Check if we have existing clients (active or disconnected)
                bool hasExistingClient = false;
                lock (_activeClientsLock)
                {
                    hasExistingClient = _activeClients.ContainsKey(playerData.ULID) || _disconnectedClients.ContainsKey(playerData.ULID);
                }

                if (hasExistingClient)
                {
                    // Update existing client with new session token and reconnect
                    _UpdateClientSessionTokenAndReconnect(playerData);
                }
                else
                {
                    // Create and initialize new client, then defer connection
                    var client = _CreatePresenceClientWithoutConnecting(playerData);
                    if (client == null)
                    {
                        return;
                    }

                    // Start auto-connect in a coroutine to avoid blocking the event thread
                    StartCoroutine(_DelayPresenceClientConnection(playerData));
                }
            }
        }

        /// <summary>
        /// Handle session refreshed events
        /// </summary>
        private void _HandleSessionRefreshedEvent(LootLockerSessionRefreshedEventData eventData)
        {
            if (!_isEnabled || !_autoConnectEnabled || _isShuttingDown)
            {
                return;
            }

            var playerData = eventData.playerData;
            if (playerData != null && !string.IsNullOrEmpty(playerData.ULID))
            {
                LootLockerLogger.Log($"Session refreshed event received for {playerData.ULID}, checking for existing clients", LootLockerLogger.LogLevel.Debug);
                
                // Check if we have existing clients (active or disconnected)
                bool hasExistingClient = false;
                lock (_activeClientsLock)
                {
                    hasExistingClient = _activeClients.ContainsKey(playerData.ULID) || _disconnectedClients.ContainsKey(playerData.ULID);
                }

                if (hasExistingClient)
                {
                    // Update existing client with new session token and reconnect
                    _UpdateClientSessionTokenAndReconnect(playerData);
                }
                else
                {
                    // Create and initialize new client, then defer connection
                    var client = _CreatePresenceClientWithoutConnecting(playerData);
                    if (client == null)
                    {
                        return;
                    }

                    // Start auto-connect in a coroutine to avoid blocking the event thread
                    StartCoroutine(_DelayPresenceClientConnection(playerData));
                }
            }
        }

        /// <summary>
        /// Handle session ended events
        /// </summary>
        private void _HandleSessionEndedEvent(LootLockerSessionEndedEventData eventData)
        {
            if(!_isEnabled || _isShuttingDown)
            {
                return;
            }
            if (!string.IsNullOrEmpty(eventData.playerUlid))
            {
                LootLockerLogger.Log($"Session ended event received for {eventData.playerUlid}, destroying presence client", LootLockerLogger.LogLevel.Debug);
                _DestroyPresenceClientForUlid(eventData.playerUlid);
            }
        }

        /// <summary>
        /// Handle session expired events
        /// </summary>
        private void _HandleSessionExpiredEvent(LootLockerSessionExpiredEventData eventData)
        {
            if(!_isEnabled || _isShuttingDown)
            {
                return;
            }
            if (!string.IsNullOrEmpty(eventData.playerUlid))
            {
                LootLockerLogger.Log($"Session expired event received for {eventData.playerUlid}, destroying presence client", LootLockerLogger.LogLevel.Debug);
                _DestroyPresenceClientForUlid(eventData.playerUlid);
            }
        }

        /// <summary>
        /// Handle local session deactivated events
        /// Note: If this is part of a session end flow, presence will already be destroyed by _HandleSessionEndedEvent
        /// This handler destroys presence client for local state management scenarios
        /// </summary>
        private void _HandleLocalSessionDeactivatedEvent(LootLockerLocalSessionDeactivatedEventData eventData)
        {
            if(!_isEnabled || _isShuttingDown)
            {
                return;
            }
            if (!string.IsNullOrEmpty(eventData.playerUlid))
            {
                LootLockerLogger.Log($"Local session deactivated event received for {eventData.playerUlid}, destroying presence client", LootLockerLogger.LogLevel.Debug);
                _DestroyPresenceClientForUlid(eventData.playerUlid);
            }
        }

        /// <summary>
        /// Handles local session activation by checking if presence and auto-connect are enabled,
        /// and, if so, automatically connects presence for the activated player session.
        /// </summary>
        private void _HandleLocalSessionActivatedEvent(LootLockerLocalSessionActivatedEventData eventData)
        {
            if (!_isEnabled || !_autoConnectEnabled || _isShuttingDown)
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

        /// <summary>
        /// Handle connection state changed events from individual presence clients
        /// </summary>
        private void _HandleClientConnectionStateChanged(LootLockerPresenceConnectionStateChangedEventData eventData)
        {
            if (eventData.newState == LootLockerPresenceConnectionState.Destroyed)
            {
                LootLockerLogger.Log($"Auto-cleaning up presence client for {eventData.playerUlid} due to state change: {eventData.newState}", LootLockerLogger.LogLevel.Debug);
                
                // Clean up the client from our tracking
                LootLockerPresenceClient clientToCleanup = null;
                lock (_activeClientsLock)
                {
                    if (_activeClients.TryGetValue(eventData.playerUlid, out clientToCleanup))
                    {
                        _activeClients.Remove(eventData.playerUlid);
                    }
                    else if (_disconnectedClients.TryGetValue(eventData.playerUlid, out clientToCleanup))
                    {
                        _disconnectedClients.Remove(eventData.playerUlid);
                    }
                }
                
                // Destroy the GameObject to fully clean up resources
                if (clientToCleanup != null)
                {
                    UnityEngine.Object.Destroy(clientToCleanup.gameObject);
                }
            }
            else if (eventData.newState == LootLockerPresenceConnectionState.Disconnected)
            {
                // Move client from active to disconnected state (don't destroy)
                LootLockerPresenceClient clientToMove = null;
                lock (_activeClientsLock)
                {
                    if (_activeClients.TryGetValue(eventData.playerUlid, out clientToMove))
                    {
                        _activeClients.Remove(eventData.playerUlid);
                        _disconnectedClients[eventData.playerUlid] = clientToMove;
                        LootLockerLogger.Log($"Moved presence client for {eventData.playerUlid} to disconnected state", LootLockerLogger.LogLevel.Debug);
                    }
                }
            }
            else if (eventData.newState == LootLockerPresenceConnectionState.Failed)
            {
                // For failed states, we need to check if it's an authentication failure or network failure
                // Authentication failures should destroy, network failures should move to disconnected
                LootLockerPresenceClient clientToHandle = null;
                lock (_activeClientsLock)
                {
                    if (_activeClients.TryGetValue(eventData.playerUlid, out clientToHandle))
                    {
                        _activeClients.Remove(eventData.playerUlid);
                    }
                }

                if (clientToHandle != null)
                {
                    // If the error indicates authentication failure, destroy the client
                    // Otherwise, move to disconnected state for potential reconnection
                    if (eventData.errorMessage != null && (eventData.errorMessage.Contains("authentication") || eventData.errorMessage.Contains("unauthorized") || eventData.errorMessage.Contains("invalid token")))
                    {
                        LootLockerLogger.Log($"Destroying presence client for {eventData.playerUlid} due to authentication failure: {eventData.errorMessage}", LootLockerLogger.LogLevel.Debug);
                        UnityEngine.Object.Destroy(clientToHandle.gameObject);
                    }
                    else
                    {
                        // Network or other failure - move to disconnected for potential reconnection
                        lock (_activeClientsLock)
                        {
                            _disconnectedClients[eventData.playerUlid] = clientToHandle;
                        }
                        LootLockerLogger.Log($"Moved presence client for {eventData.playerUlid} to disconnected state due to failure: {eventData.errorMessage}", LootLockerLogger.LogLevel.Debug);
                    }
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

            if (!instance._isEnabled)
            {
                    string errorMessage = "Presence is disabled. Enable it in Project Settings > LootLocker SDK > Presence Settings or use _SetPresenceEnabled(true).";
                LootLockerLogger.Log(errorMessage, LootLockerLogger.LogLevel.Debug);
                onComplete?.Invoke(false, errorMessage);
                return;
            }

            if (string.IsNullOrEmpty(playerUlid))
            {
                playerUlid = LootLockerStateData.GetDefaultPlayerULID();
            }

            // Get player data
            var playerData = LootLockerStateData.GetPlayerDataForPlayerWithUlidWithoutChangingState(playerUlid);
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

            lock (instance._activeClientsLock)
            {
                // Check if already connecting
                if (instance._connectingClients.Contains(ulid))
                {
                    LootLockerLogger.Log($"Presence client for {ulid} is already being connected, skipping new connection attempt", LootLockerLogger.LogLevel.Debug);
                    onComplete?.Invoke(false, "Already connecting");
                    return;
                }

                // Check for active client first
                if (instance._activeClients.ContainsKey(ulid))
                {
                    var existingClient = instance._activeClients[ulid];
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

                // Check for disconnected client that can be reused
                if (instance._disconnectedClients.ContainsKey(ulid))
                {
                    var disconnectedClient = instance._disconnectedClients[ulid];
                    
                    // Check if the session token needs to be updated
                    if (disconnectedClient.SessionToken != playerData.SessionToken)
                    {
                        LootLockerLogger.Log($"Session token changed for {ulid}, updating token on existing client", LootLockerLogger.LogLevel.Debug);
                        // Update the session token on the existing client
                        disconnectedClient.UpdateSessionToken(playerData.SessionToken);
                    }
                    
                    // Reuse the disconnected client (with updated token if needed)
                    LootLockerLogger.Log($"Reusing disconnected presence client for {ulid}", LootLockerLogger.LogLevel.Debug);
                    instance._disconnectedClients.Remove(ulid);
                    instance._activeClients[ulid] = disconnectedClient;
                    instance._connectingClients.Add(ulid);
                    
                    // Reconnect the existing client outside the lock
                    instance._ConnectPresenceClient(ulid, disconnectedClient, onComplete);
                    return;
                }

                // Mark as connecting to prevent race conditions
                instance._connectingClients.Add(ulid);
            }

            // Create and connect client outside the lock
            LootLockerPresenceClient client = null;
            try
            {
                client = instance.gameObject.AddComponent<LootLockerPresenceClient>();
                client.Initialize(ulid, playerData.SessionToken);
            }
            catch (Exception ex)
            {
                // Clean up on creation failure
                lock (instance._activeClientsLock)
                {
                    instance._connectingClients.Remove(ulid);
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
            instance._ConnectPresenceClient(ulid, client, onComplete);
            instance._activeClients[ulid] = client;
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

            if (!instance._isEnabled)
            {
                onComplete?.Invoke(false, "Presence is disabled");
                return;
            }

            string ulid = playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                ulid = LootLockerStateData.GetDefaultPlayerULID();
            }

            // Use shared internal disconnect logic
            instance._DisconnectPresenceForUlid(ulid, onComplete);
        }

        /// <summary>
        /// Disconnect all presence connections
        /// </summary>
        public static void DisconnectAll()
        {
            Get()?._DisconnectAll();
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
            
            if (!instance._isEnabled)
            {
                onComplete?.Invoke(false, "Presence system is disabled");
                return;
            }
            
            LootLockerPresenceClient client = instance._GetPresenceClientForUlid(playerUlid);
            if(client == null)
            {
                onComplete?.Invoke(false, "No active presence client found for the specified player");
                return;
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

            LootLockerPresenceClient client = instance._GetPresenceClientForUlid(playerUlid);
            return client?.ConnectionState ?? LootLockerPresenceConnectionState.Disconnected;
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
            // Return empty stats during shutdown to prevent service access
            if (!Application.isPlaying)
            {
                return new LootLockerPresenceConnectionStats();
            }

            var instance = Get();
            if (instance == null) return new LootLockerPresenceConnectionStats();
            
            LootLockerPresenceClient client = instance._GetPresenceClientForUlid(playerUlid);

            if(client == null)
            {
                return new LootLockerPresenceConnectionStats();
            }
            return client.ConnectionStats;
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

            LootLockerPresenceClient client = instance._GetPresenceClientForUlid(playerUlid);

            if(client == null)
            {
                return string.Empty;
            }

            return client.LastSentStatus;
        }

        #endregion

        #region Private Helper Methods

        private void _SetPresenceEnabled(bool enabled)
        {
            bool changingState = _isEnabled != enabled;
            _isEnabled = enabled;
            if(changingState && enabled && _autoConnectEnabled)
            {
                _SubscribeToEvents(null);
                StartCoroutine(_AutoConnectExistingSessions());
            } 
            else if (changingState && !enabled)
            {
                _UnsubscribeFromEvents();
                _DisconnectAll();
            }
        }

        private void _SetAutoConnectEnabled(bool enabled)
        {
            bool changingState = _autoConnectEnabled != enabled;
            _autoConnectEnabled = enabled;
            if(changingState && _isEnabled && enabled)
            {
                _SubscribeToEvents(null);
                StartCoroutine(_AutoConnectExistingSessions());
            } 
            else if (changingState && !enabled)
            {
                _UnsubscribeFromEvents();
                _DisconnectAll();
            }
        }

        /// <summary>
        /// Destroy a presence client immediately (for session ending scenarios)
        /// </summary>
        private void _DestroyPresenceClientForUlid(string playerUlid, LootLockerPresenceCallback onComplete = null)
        {
            if (!_isEnabled)
            {
                onComplete?.Invoke(false, "Presence is disabled");
                return;
            }
            else if (_isShuttingDown)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (string.IsNullOrEmpty(playerUlid))
            {
                onComplete?.Invoke(true);
                return;
            }

            LootLockerPresenceClient clientToDestroy = null;

            lock (_activeClientsLock)
            {
                // Remove from both active and disconnected clients
                if (_activeClients.TryGetValue(playerUlid, out clientToDestroy))
                {
                    _activeClients.Remove(playerUlid);
                }
                else if (_disconnectedClients.TryGetValue(playerUlid, out clientToDestroy))
                {
                    _disconnectedClients.Remove(playerUlid);
                }
                
                // Also remove from connecting clients if it's there
                _connectingClients.Remove(playerUlid);
            }

            // Destroy the client
            if (clientToDestroy != null)
            {
                UnityEngine.Object.Destroy(clientToDestroy.gameObject);
                onComplete?.Invoke(true);
            }
            else
            {
                onComplete?.Invoke(true);
            }
        }

        /// <summary>
        /// Destroy all presence clients (for shutdown scenarios)
        /// </summary>
        private void _DestroyAllClients()
        {
            List<LootLockerPresenceClient> clientsToDestroy = new List<LootLockerPresenceClient>();

            lock (_activeClientsLock)
            {
                // Collect all clients from both active and disconnected collections
                clientsToDestroy.AddRange(_activeClients.Values);
                clientsToDestroy.AddRange(_disconnectedClients.Values);
                
                // Clear all collections
                _activeClients.Clear();
                _disconnectedClients.Clear();
                _connectingClients.Clear();
            }

            // During Unity shutdown, don't destroy objects manually to avoid conflicts with LifecycleManager
            if (!Application.isPlaying || _isShuttingDown)
            {
                // Just clear the collections, let Unity handle object destruction during shutdown
                return;
            }

            // Destroy all clients outside the lock (only during normal operation)
            foreach (var client in clientsToDestroy)
            {
                if (client != null)
                {
                    UnityEngine.Object.Destroy(client.gameObject);
                }
            }
        }

        /// <summary>
        /// Update session token on existing client and reconnect
        /// </summary>
        private void _UpdateClientSessionTokenAndReconnect(LootLockerPlayerData playerData)
        {
            if (playerData == null || string.IsNullOrEmpty(playerData.ULID) || string.IsNullOrEmpty(playerData.SessionToken))
            {
                LootLockerLogger.Log("Cannot update client session token: Invalid player data", LootLockerLogger.LogLevel.Warning);
                return;
            }

            LootLockerPresenceClient clientToUpdate = null;
            bool wasActiveClient = false;
            bool wasDisconnectedClient = false;

            lock (_activeClientsLock)
            {
                // Find client in active clients
                if (_activeClients.TryGetValue(playerData.ULID, out clientToUpdate))
                {
                    wasActiveClient = true;
                }
                // Or in disconnected clients
                else if (_disconnectedClients.TryGetValue(playerData.ULID, out clientToUpdate))
                {
                    wasDisconnectedClient = true;
                }
            }

            if (clientToUpdate != null)
            {
                // Capture current status before any operations
                string lastStatus = clientToUpdate.LastSentStatus;
                
                // Update the session token
                clientToUpdate.UpdateSessionToken(playerData.SessionToken);

                if (wasActiveClient)
                {
                    // For active clients: disconnect first, then reconnect
                    LootLockerLogger.Log($"Disconnecting active client for {playerData.ULID} to update session token", LootLockerLogger.LogLevel.Debug);
                    
                    DisconnectPresence(playerData.ULID, (disconnectSuccess, disconnectError) => {
                        if (disconnectSuccess)
                        {
                            // After disconnect, the client should be in disconnected state
                            // Now reconnect with the updated token
                            LootLockerLogger.Log($"Reconnecting presence for {playerData.ULID} with updated session token", LootLockerLogger.LogLevel.Debug);
                            ConnectPresence(playerData.ULID);
                        }
                        else
                        {
                            LootLockerLogger.Log($"Failed to disconnect presence for session token update: {disconnectError}", LootLockerLogger.LogLevel.Warning);
                        }
                    });
                }
                else if (wasDisconnectedClient && _autoConnectEnabled)
                {
                    // For disconnected clients: just reconnect with new token
                    LootLockerLogger.Log($"Reconnecting disconnected client for {playerData.ULID} with updated session token", LootLockerLogger.LogLevel.Debug);
                    ConnectPresence(playerData.ULID);
                }

                LootLockerLogger.Log($"Updated session token for presence client {playerData.ULID}, last status was: {lastStatus}", LootLockerLogger.LogLevel.Debug);
            }
            else
            {
                // No existing client, create new one if auto-connect is enabled
                if (_autoConnectEnabled)
                {
                    LootLockerLogger.Log($"No existing client found for {playerData.ULID}, creating new one", LootLockerLogger.LogLevel.Debug);
                    ConnectPresence(playerData.ULID);
                }
            }
        }

        private IEnumerator _AutoConnectExistingSessions()
        {
            // Wait a frame to ensure everything is initialized
            yield return null;
            
            if (!_isEnabled || !_autoConnectEnabled || _isShuttingDown)
            {
                yield break;
            }

            // Get all active sessions from state data and auto-connect
            var activePlayerUlids = LootLockerStateData.GetActivePlayerULIDs();
            if (activePlayerUlids == null)
            {
                yield break;
            }
            
            foreach (var ulid in activePlayerUlids)
            {
                if (string.IsNullOrEmpty(ulid))
                {
                    continue;
                }
                
                var state = LootLockerStateData.GetPlayerDataForPlayerWithUlidWithoutChangingState(ulid);
                if (state == null)
                {
                    continue;
                }

                // Check if we already have an active or in-progress presence client for this ULID
                bool shouldConnect = false;
                lock (_activeClientsLock)
                {
                    // Check if already connecting
                    if (_connectingClients.Contains(state.ULID))
                    {
                        shouldConnect = false;
                    }
                    else if (!_activeClients.ContainsKey(state.ULID) && !_disconnectedClients.ContainsKey(state.ULID))
                    {
                        shouldConnect = true;
                    }
                    else if (_activeClients.ContainsKey(state.ULID))
                    {
                        // Check if existing active client is in a failed or disconnected state
                        var existingClient = _activeClients[state.ULID];
                        var clientState = existingClient.ConnectionState;
                        
                        if (clientState == LootLockerPresenceConnectionState.Failed ||
                            clientState == LootLockerPresenceConnectionState.Disconnected)
                        {
                            shouldConnect = true;
                        }
                    }
                    else if (_disconnectedClients.ContainsKey(state.ULID))
                    {
                        // Have disconnected client - should reconnect
                        shouldConnect = true;
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
        
        /// <summary>
        /// Shared internal method for disconnecting a presence client by ULID
        /// </summary>
        private void _DisconnectPresenceForUlid(string playerUlid, LootLockerPresenceCallback onComplete = null)
        {
            if(!_isEnabled)
            {
                onComplete?.Invoke(false, "Presence is disabled");
                return;
            }
            else if(_isShuttingDown)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (string.IsNullOrEmpty(playerUlid))
            {
                onComplete?.Invoke(true);
                return;
            }

            LootLockerPresenceClient client = null;
            bool alreadyDisconnectedOrFailed = false;

            lock (_activeClientsLock)
            {
                if (!_activeClients.TryGetValue(playerUlid, out client))
                {
                    // Check if already in disconnected state
                    if (_disconnectedClients.ContainsKey(playerUlid))
                    {
                        onComplete?.Invoke(true);
                        return;
                    }
                    onComplete?.Invoke(true);
                    return;
                }

                // Check connection state to prevent multiple disconnect attempts
                var connectionState = client.ConnectionState;
                if (connectionState == LootLockerPresenceConnectionState.Disconnected ||
                    connectionState == LootLockerPresenceConnectionState.Failed ||
                    connectionState == LootLockerPresenceConnectionState.Destroying ||
                    connectionState == LootLockerPresenceConnectionState.Destroyed)
                {
                    alreadyDisconnectedOrFailed = true;
                }

                // Remove from _activeClients immediately to prevent other events from trying to disconnect
                _activeClients.Remove(playerUlid);
            }

            // Disconnect outside the lock to avoid blocking other operations
            if (client != null)
            {
                if (alreadyDisconnectedOrFailed)
                {
                    // Move to disconnected clients instead of destroying
                    lock (_activeClientsLock)
                    {
                        if (!_disconnectedClients.ContainsKey(playerUlid))
                        {
                            _disconnectedClients[playerUlid] = client;
                        }
                    }
                    onComplete?.Invoke(true);
                }
                else
                {
                    client.Disconnect((success, error) => {
                        if (!success)
                        {
                            LootLockerLogger.Log($"Error disconnecting presence for {playerUlid}: {error}", LootLockerLogger.LogLevel.Debug);
                        }
                        // Move to disconnected clients instead of destroying
                        lock (_activeClientsLock)
                        {
                            if (!_disconnectedClients.ContainsKey(playerUlid))
                            {
                                _disconnectedClients[playerUlid] = client;
                            }
                        }
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
        private void _DisconnectAll()
        {
            List<string> ulidsToDisconnect;
            lock (_activeClientsLock)
            {
                ulidsToDisconnect = new List<string>(_activeClients.Keys);
                // Clear connecting clients as we're disconnecting everything
                _connectingClients.Clear();
            }
            
            foreach (var ulid in ulidsToDisconnect)
            {
                _DisconnectPresenceForUlid(ulid);
            }
        }

        /// <summary>
        /// Creates and initializes a presence client without connecting it
        /// </summary>
        private LootLockerPresenceClient _CreatePresenceClientWithoutConnecting(LootLockerPlayerData playerData)
        {
            var instance = Get();
            if (instance == null) return null;
            
            if (!instance._isEnabled)
            {
                return null;
            }

            // Use the provided player data directly
            if (playerData == null || string.IsNullOrEmpty(playerData.ULID) || string.IsNullOrEmpty(playerData.SessionToken))
            {
                return null;
            }

            lock (instance._activeClientsLock)
            {
                // Check if already connected for this player
                if (instance._activeClients.ContainsKey(playerData.ULID))
                {
                    LootLockerLogger.Log($"Presence already connected for player {playerData.ULID}", LootLockerLogger.LogLevel.Debug);
                    return instance._activeClients[playerData.ULID];
                }

                // Create new presence client as a GameObject component
                var clientGameObject = new GameObject($"PresenceClient_{playerData.ULID}");
                clientGameObject.transform.SetParent(instance.transform);
                var client = clientGameObject.AddComponent<LootLockerPresenceClient>();
                
                client.Initialize(playerData.ULID, playerData.SessionToken);
                
                // Add to active clients immediately
                instance._activeClients[playerData.ULID] = client;
                
                return client;
            }
        }

        /// <summary>
        /// Connects an existing presence client
        /// </summary>
        private void _ConnectPresenceClient(string ulid, LootLockerPresenceClient client, LootLockerPresenceCallback onComplete = null)
        {
            if (client == null)
            {
                onComplete?.Invoke(false, "Client is null");
                return;
            }

            client.Connect((success, error) =>
            {
                lock (_activeClientsLock)
                {
                    // Remove from connecting clients
                    _connectingClients.Remove(ulid);
                }  
                if (!success)
                {
                    DisconnectPresence(ulid);
                }              
                onComplete?.Invoke(success, error);
            });
        }

        /// <summary>
        /// Coroutine to handle auto-connecting presence after session events
        /// </summary>
        private System.Collections.IEnumerator _DelayPresenceClientConnection(LootLockerPlayerData playerData)
        {
            // Yield one frame to let the session event complete fully
            yield return null;
            
            var instance = Get();
            if (instance == null)
            {
                yield break;
            }

            LootLockerPresenceClient existingClient = null;

            lock (instance._activeClientsLock)
            {
                // Check if already connected for this player
                if (instance._activeClients.ContainsKey(playerData.ULID))
                {
                    existingClient = instance._activeClients[playerData.ULID];
                }
            }
            
            // Now attempt to connect the pre-created client
            _ConnectPresenceClient(playerData.ULID, existingClient);
        }

        private LootLockerPresenceClient _GetPresenceClientForUlid(string playerUlid)
        {
            string ulid = string.IsNullOrEmpty(playerUlid) ? LootLockerStateData.GetDefaultPlayerULID() : playerUlid;
            if (string.IsNullOrEmpty(ulid))
            {
                return null;
            }

            lock (_activeClientsLock)
            {
                // Check active clients first
                if (_activeClients.TryGetValue(ulid, out LootLockerPresenceClient activeClient))
                {
                    return activeClient;
                }

                // Then check disconnected clients
                if (_disconnectedClients.TryGetValue(ulid, out LootLockerPresenceClient disconnectedClient))
                {
                    return disconnectedClient;
                }

                return null;
            }
        }

        #endregion

        #region Unity Lifecycle Events

        private void OnDestroy()
        {
            // During Unity shutdown, avoid any complex operations
            if (!Application.isPlaying)
            {
                return;
            }

            if (!_isShuttingDown)
            {
                _isShuttingDown = true;
                _UnsubscribeFromEvents();
                
                // Only destroy clients if we're not in Unity shutdown
                _DestroyAllClients();
            }

            // Skip lifecycle manager operations during shutdown
            if(!LootLockerLifecycleManager.IsReady) return;

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
