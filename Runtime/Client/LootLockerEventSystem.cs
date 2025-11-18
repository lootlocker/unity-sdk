using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    #region Event Data Classes

    /// <summary>
    /// Base class for all LootLocker event data
    /// </summary>
    [Serializable]
    public abstract class LootLockerEventData
    {
        public DateTime timestamp { get; private set; }
        public LootLockerEventType eventType { get; private set; }

        protected LootLockerEventData(LootLockerEventType eventType)
        {
            this.eventType = eventType;
            this.timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event data for session started events
    /// </summary>
    [Serializable]
    public class LootLockerSessionStartedEventData : LootLockerEventData
    {
        /// <summary>
        /// The complete player data for the player whose session started
        /// </summary>
        public LootLockerPlayerData playerData { get; set; }

        public LootLockerSessionStartedEventData(LootLockerPlayerData playerData) 
            : base(LootLockerEventType.SessionStarted)
        {
            this.playerData = playerData;
        }
    }

    /// <summary>
    /// Event data for session refreshed events
    /// </summary>
    [Serializable]
    public class LootLockerSessionRefreshedEventData : LootLockerEventData
    {
        /// <summary>
        /// The complete player data for the player whose session was refreshed
        /// </summary>
        public LootLockerPlayerData playerData { get; set; }

        public LootLockerSessionRefreshedEventData(LootLockerPlayerData playerData) 
            : base(LootLockerEventType.SessionRefreshed)
        {
            this.playerData = playerData;
        }
    }

    /// <summary>
    /// Event data for session ended events
    /// </summary>
    [Serializable]
    public class LootLockerSessionEndedEventData : LootLockerEventData
    {
        /// <summary>
        /// The ULID of the player whose session ended
        /// </summary>
        public string playerUlid { get; set; }
        
        /// <summary>
        /// Whether local state should be cleared for this player
        /// </summary>
        public bool clearLocalState { get; set; }

        public LootLockerSessionEndedEventData(string playerUlid, bool clearLocalState = false) 
            : base(LootLockerEventType.SessionEnded)
        {
            this.playerUlid = playerUlid;
            this.clearLocalState = clearLocalState;
        }
    }

    /// <summary>
    /// Event data for session expired events
    /// </summary>
    [Serializable]
    public class LootLockerSessionExpiredEventData : LootLockerEventData
    {
        /// <summary>
        /// The ULID of the player whose session expired
        /// </summary>
        public string playerUlid { get; set; }

        public LootLockerSessionExpiredEventData(string playerUlid) 
            : base(LootLockerEventType.SessionExpired)
        {
            this.playerUlid = playerUlid;
        }
    }

    /// <summary>
    /// Event data for local session deactivated events
    /// </summary>
    [Serializable]
    public class LootLockerLocalSessionDeactivatedEventData : LootLockerEventData
    {
        /// <summary>
        /// The ULID of the player whose local session was deactivated (null if all sessions were deactivated)
        /// </summary>
        public string playerUlid { get; set; }

        public LootLockerLocalSessionDeactivatedEventData(string playerUlid) 
            : base(LootLockerEventType.LocalSessionDeactivated)
        {
            this.playerUlid = playerUlid;
        }
    }

    /// <summary>
    /// Event data for local session activated events
    /// </summary>
    [Serializable]
    public class LootLockerLocalSessionActivatedEventData : LootLockerEventData
    {
        /// <summary>
        /// The complete player data for the player whose session was activated
        /// </summary>
        public LootLockerPlayerData playerData { get; set; }

        public LootLockerLocalSessionActivatedEventData(LootLockerPlayerData playerData) 
            : base(LootLockerEventType.LocalSessionActivated)
        {
            this.playerData = playerData;
        }
    }

    #endregion

    #region Event Delegates

    /// <summary>
    /// Delegate for LootLocker events
    /// </summary>
    public delegate void LootLockerEventHandler<T>(T eventData) where T : LootLockerEventData;

    #endregion

    #region Event Types

    /// <summary>
    /// Predefined event types for the LootLocker SDK
    /// </summary>
    public enum LootLockerEventType
    {
        // Session Events
        SessionStarted,
        SessionRefreshed,
        SessionEnded,
        SessionExpired,
        LocalSessionDeactivated,
        LocalSessionActivated
    }

    #endregion

    /// <summary>
    /// Centralized event system for the LootLocker SDK
    /// Manages event subscriptions, event firing, and event data
    /// </summary>
    public class LootLockerEventSystem : MonoBehaviour, ILootLockerService
    {
        #region ILootLockerService Implementation

        public bool IsInitialized { get; private set; } = false;
        public string ServiceName => "EventSystem";

        void ILootLockerService.Initialize()
        {
            if (IsInitialized) return;
            
            // Initialize event system configuration
            isEnabled = true;
            logEvents = false;
            IsInitialized = true;
            
            LootLockerLogger.Log("LootLockerEventSystem initialized", LootLockerLogger.LogLevel.Debug);
        }

        void ILootLockerService.Reset()
        {
            ClearAllSubscribers();
            isEnabled = true;
            logEvents = false;
            IsInitialized = false;
        }

        void ILootLockerService.HandleApplicationPause(bool pauseStatus)
        {
            // Event system doesn't need to handle pause events
        }

        void ILootLockerService.HandleApplicationFocus(bool hasFocus)
        {
            // Event system doesn't need to handle focus events
        }

        void ILootLockerService.HandleApplicationQuit()
        {
            ClearAllSubscribers();
        }

        #endregion

        #region Singleton Management
        
        private static LootLockerEventSystem _instance;
        private static readonly object _instanceLock = new object();

        /// <summary>
        /// Get the EventSystem service instance through the LifecycleManager.
        /// Services are automatically registered and initialized on first access if needed.
        /// </summary>
        private static LootLockerEventSystem GetInstance()
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
                    _instance = LootLockerLifecycleManager.GetService<LootLockerEventSystem>();
                }
                return _instance;
            }
        }
        
        #endregion

        #region Private Fields

        // Event storage with strong references to prevent premature GC
        // Using regular List instead of WeakReference to avoid delegate GC issues
        private Dictionary<LootLockerEventType, List<object>> eventSubscribers = new Dictionary<LootLockerEventType, List<object>>();
        private readonly object eventSubscribersLock = new object(); // Thread safety for event subscribers

        // Configuration
        private bool isEnabled = true;
        private bool logEvents = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the event system is enabled
        /// </summary>
        public static bool IsEnabled
        {
            get => GetInstance().isEnabled;
            set => GetInstance().isEnabled = value;
        }

        /// <summary>
        /// Whether to log events to the console for debugging
        /// </summary>
        public static bool LogEvents
        {
            get => GetInstance().logEvents;
            set => GetInstance().logEvents = value;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the event system (called automatically by SDK)
        /// </summary>
        internal static void Initialize()
        {
            // Services are now registered through LootLockerLifecycleManager.InitializeAllServices()
            // This method is kept for backwards compatibility but does nothing during registration
            GetInstance(); // This will retrieve the already-registered service
        }

        /// <summary>
        /// Subscribe to a specific event type with typed event data
        /// </summary>
        public static void Subscribe<T>(LootLockerEventType eventType, LootLockerEventHandler<T> handler) where T : LootLockerEventData
        {
            GetInstance().SubscribeInstance(eventType, handler);
        }

        /// <summary>
        /// Instance method to subscribe to events without triggering circular dependency through GetInstance()
        /// Used during initialization when we already have the EventSystem instance
        /// </summary>
        public void SubscribeInstance<T>(LootLockerEventType eventType, LootLockerEventHandler<T> handler) where T : LootLockerEventData
        {
            if (!isEnabled || handler == null)
                return;

            lock (eventSubscribersLock)
            {
                if (!eventSubscribers.ContainsKey(eventType))
                {
                    eventSubscribers[eventType] = new List<object>();
                }

                // Add new subscription with strong reference to prevent GC issues
                eventSubscribers[eventType].Add(handler);
                
                if (logEvents)
                {
                    LootLockerLogger.Log($"SubscribeInstance to {eventType}, total subscribers: {eventSubscribers[eventType].Count}", LootLockerLogger.LogLevel.Debug);
                }
            }
        }

        /// <summary>
        /// Unsubscribe from a specific event type with typed handler using this instance
        /// </summary>
        public void UnsubscribeInstance<T>(LootLockerEventType eventType, LootLockerEventHandler<T> handler) where T : LootLockerEventData
        {
            if (!eventSubscribers.ContainsKey(eventType))
                return;

            lock (eventSubscribersLock)
            {
                // Find and remove the matching handler
                var subscribers = eventSubscribers[eventType];
                for (int i = subscribers.Count - 1; i >= 0; i--)
                {
                    if (subscribers[i].Equals(handler))
                    {
                        subscribers.RemoveAt(i);
                        break;
                    }
                }
                
                // Clean up empty lists
                if (subscribers.Count == 0)
                {
                    eventSubscribers.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Unsubscribe from a specific event type with typed handler
        /// </summary>
        public static void Unsubscribe<T>(LootLockerEventType eventType, LootLockerEventHandler<T> handler) where T : LootLockerEventData
        {
            GetInstance().UnsubscribeInstance(eventType, handler);
        }

        /// <summary>
        /// Fire an event with specific event data
        /// </summary>
        public static void TriggerEvent<T>(T eventData) where T : LootLockerEventData
        {
            var instance = GetInstance();
            if (!instance.isEnabled || eventData == null)
                return;

            LootLockerEventType eventType = eventData.eventType;

            if (!instance.eventSubscribers.ContainsKey(eventType))
                return;

            // Get a copy of subscribers to avoid lock contention during event handling
            List<object> subscribers;
            lock (instance.eventSubscribersLock)
            {
                subscribers = new List<object>(instance.eventSubscribers[eventType]);
            }

            // Trigger event handlers outside the lock
            foreach (var subscriber in subscribers)
            {
                try
                {
                    if (subscriber is LootLockerEventHandler<T> typedHandler)
                    {
                        typedHandler.Invoke(eventData);
                    }
                }
                catch (Exception ex)
                {
                    LootLockerLogger.Log($"Error in event handler for {eventType}: {ex.Message}", LootLockerLogger.LogLevel.Error);
                }
            }

            if (instance.logEvents)
            {
                LootLockerLogger.Log($"LootLocker Event: {eventType} at {eventData.timestamp}. Notified {subscribers.Count} subscribers", LootLockerLogger.LogLevel.Debug);
            }
        }

        /// <summary>
        /// Clear all subscribers for a specific event type
        /// </summary>
        public static void ClearSubscribers(LootLockerEventType eventType)
        {
            var instance = GetInstance();
            lock (instance.eventSubscribersLock)
            {
                instance.eventSubscribers.Remove(eventType);
            }
        }

        /// <summary>
        /// Clear all event subscribers
        /// </summary>
        public static void ClearAllSubscribers()
        {
            var instance = GetInstance();
            lock (instance.eventSubscribersLock)
            {
                instance.eventSubscribers.Clear();
            }
        }

        /// <summary>
        /// Get the number of subscribers for a specific event type
        /// </summary>
        public static int GetSubscriberCount(LootLockerEventType eventType)
        {
            var instance = GetInstance();
            
            lock (instance.eventSubscribersLock)
            {
                if (instance.eventSubscribers.ContainsKey(eventType))
                    return instance.eventSubscribers[eventType].Count;
                    
                return 0;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            ClearAllSubscribers();
        }

        #endregion

        #region Helper Methods for Session Events

        /// <summary>
        /// Helper method to trigger session started event
        /// </summary>
        public static void TriggerSessionStarted(LootLockerPlayerData playerData)
        {
            var eventData = new LootLockerSessionStartedEventData(playerData);
            TriggerEvent(eventData);
        }

        /// <summary>
        /// Helper method to trigger session ended event
        /// </summary>
        /// <param name="playerUlid">The player whose session ended</param>
        /// <param name="clearLocalState">Whether to clear local state for this player</param>
        public static void TriggerSessionEnded(string playerUlid, bool clearLocalState = false)
        {
            var eventData = new LootLockerSessionEndedEventData(playerUlid, clearLocalState);
            TriggerEvent(eventData);
        }

        /// <summary>
        /// Helper method to trigger session refreshed event
        /// </summary>
        public static void TriggerSessionRefreshed(LootLockerPlayerData playerData)
        {
            var eventData = new LootLockerSessionRefreshedEventData(playerData);
            TriggerEvent(eventData);
        }

        /// <summary>
        /// Helper method to trigger session expired event
        /// </summary>
        public static void TriggerSessionExpired(string playerUlid)
        {
            var eventData = new LootLockerSessionExpiredEventData(playerUlid);
            TriggerEvent(eventData);
        }

        /// <summary>
        /// Helper method to trigger local session cleared event for a specific player
        /// </summary>
        public static void TriggerLocalSessionDeactivated(string playerUlid)
        {
            var eventData = new LootLockerLocalSessionDeactivatedEventData(playerUlid);
            TriggerEvent(eventData);
        }

        /// <summary>
        /// Helper method to trigger session activated event
        /// </summary>
        public static void TriggerLocalSessionActivated(LootLockerPlayerData playerData)
        {
            var eventData = new LootLockerLocalSessionActivatedEventData(playerData);
            TriggerEvent(eventData);
        }

        #endregion

    }
}