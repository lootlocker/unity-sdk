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

        public LootLockerSessionEndedEventData(string playerUlid) 
            : base(LootLockerEventType.SessionEnded)
        {
            this.playerUlid = playerUlid;
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
            
            LootLockerLogger.Log("LootLockerEventSystem initialized", LootLockerLogger.LogLevel.Verbose);
        }

        void ILootLockerService.Reset()
        {
            ClearAllSubscribers();
            isEnabled = true;
            logEvents = false;
            IsInitialized = false;
        }

        void ILootLockerService.HandleApplicationQuit()
        {
            ClearAllSubscribers();
        }

        #endregion

        #region Instance Handling

        /// <summary>
        /// Get the EventSystem service instance through the LifecycleManager
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

        public static void ResetInstance()
        {
            lock (_instanceLock)
            {
                _instance = null;
            }
        }
        
        #endregion

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(UnityEditor.EnterPlayModeOptions options)
        {
            ResetInstance();
        }
#endif

        #endregion

        #region Private Fields

        // Event storage with weak references to prevent memory leaks
        private Dictionary<LootLockerEventType, List<WeakReference>> eventSubscribers = new Dictionary<LootLockerEventType, List<WeakReference>>();
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
            var instance = GetInstance();
            if (!instance.isEnabled || handler == null)
                return;

            lock (instance.eventSubscribersLock)
            {
                if (!instance.eventSubscribers.ContainsKey(eventType))
                {
                    instance.eventSubscribers[eventType] = new List<WeakReference>();
                }

                // Clean up dead references before adding new one
                instance.CleanupDeadReferences(eventType);

                instance.eventSubscribers[eventType].Add(new WeakReference(handler));
            }
        }

        /// <summary>
        /// Unsubscribe from a specific event type with typed handler
        /// </summary>
        public static void Unsubscribe<T>(LootLockerEventType eventType, LootLockerEventHandler<T> handler) where T : LootLockerEventData
        {
            var instance = GetInstance();
            if (!instance.eventSubscribers.ContainsKey(eventType))
                return;

            lock (instance.eventSubscribersLock)
            {
                // Clean up dead references and remove matching handler
                instance.CleanupDeadReferencesAndRemove(eventType, handler);
                
                // Clean up empty lists
                if (instance.eventSubscribers[eventType].Count == 0)
                {
                    instance.eventSubscribers.Remove(eventType);
                }
            }
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

            // Log event if enabled
            if (instance.logEvents)
            {
                LootLockerLogger.Log($"LootLocker Event: {eventType} at {eventData.timestamp}", LootLockerLogger.LogLevel.Verbose);
            }

            if (!instance.eventSubscribers.ContainsKey(eventType))
                return;

            // Get live subscribers and clean up dead references
            List<object> liveSubscribers = new List<object>();
            lock (instance.eventSubscribersLock)
            {
                // Clean up dead references first
                instance.CleanupDeadReferences(eventType);
                
                // Then collect live subscribers
                var subscribers = instance.eventSubscribers[eventType];
                foreach (var weakRef in subscribers)
                {
                    if (weakRef.IsAlive)
                    {
                        liveSubscribers.Add(weakRef.Target);
                    }
                }

                // Clean up empty event type
                if (subscribers.Count == 0)
                {
                    instance.eventSubscribers.Remove(eventType);
                }
            }

            // Trigger event handlers outside the lock
            foreach (var subscriber in liveSubscribers)
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
        /// Clean up all dead references across all event types
        /// </summary>
        public static void CleanupAllDeadReferences()
        {
            var instance = GetInstance();
            lock (instance.eventSubscribersLock)
            {
                var eventTypesToRemove = new List<LootLockerEventType>();
                
                foreach (var eventType in instance.eventSubscribers.Keys)
                {
                    instance.CleanupDeadReferences(eventType);
                    
                    // Mark empty event types for removal
                    if (instance.eventSubscribers[eventType].Count == 0)
                    {
                        eventTypesToRemove.Add(eventType);
                    }
                }
                
                // Remove empty event types
                foreach (var eventType in eventTypesToRemove)
                {
                    instance.eventSubscribers.Remove(eventType);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Clean up dead references for a specific event type (called within lock)
        /// </summary>
        private void CleanupDeadReferences(LootLockerEventType eventType)
        {
            if (!eventSubscribers.ContainsKey(eventType))
                return;

            var subscribers = eventSubscribers[eventType];
            for (int i = subscribers.Count - 1; i >= 0; i--)
            {
                if (!subscribers[i].IsAlive)
                {
                    subscribers.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Clean up dead references and remove a specific handler (called within lock)
        /// </summary>
        private void CleanupDeadReferencesAndRemove(LootLockerEventType eventType, object targetHandler)
        {
            if (!eventSubscribers.ContainsKey(eventType))
                return;

            var subscribers = eventSubscribers[eventType];
            for (int i = subscribers.Count - 1; i >= 0; i--)
            {
                var weakRef = subscribers[i];
                if (!weakRef.IsAlive)
                {
                    // Remove dead reference
                    subscribers.RemoveAt(i);
                }
                else if (ReferenceEquals(weakRef.Target, targetHandler))
                {
                    // Remove matching handler
                    subscribers.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Clear all event subscribers
        /// </summary>
        public static void ClearAllSubscribers()
        {
            var instance = GetInstance();
            instance.eventSubscribers.Clear();
        }

        /// <summary>
        /// Get the number of subscribers for a specific event type
        /// </summary>
        public static int GetSubscriberCount(LootLockerEventType eventType)
        {
            var instance = GetInstance();
            
            if (instance.eventSubscribers.ContainsKey(eventType))
                return instance.eventSubscribers[eventType].Count;
                
            return 0;
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
        public static void TriggerSessionEnded(string playerUlid)
        {
            var eventData = new LootLockerSessionEndedEventData(playerUlid);
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