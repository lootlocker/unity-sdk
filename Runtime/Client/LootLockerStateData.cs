using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace LootLocker
{
    public class UniqueList<T> : List<T>
    {
        public bool AddUnique(T val)
        {
            if (val == null || Contains(val))
            {
                return false;
            }
            Add(val);
            return true;
        }

        public bool IsEmpty()
        {
            return Count < 1;
        }
    }

    public class LootLockerStateMetaData
    {
        public UniqueList<string> SavedPlayerStateULIDs { get; set; } = new UniqueList<string>();
        public string DefaultPlayer { get; set; }
        public Dictionary<string, string> WhiteLabelEmailToPlayerUlidMap { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Manages player state data persistence and session lifecycle
    /// Now an instantiable service for better architecture and dependency management
    /// </summary>
    public class LootLockerStateData : MonoBehaviour, ILootLockerService
    {
        #region ILootLockerService Implementation

        public bool IsInitialized { get; private set; } = false;
        public string ServiceName => "StateData";

        void ILootLockerService.Initialize()
        {
            if (IsInitialized) return;
            
            // Event subscriptions will be set up via SetEventSystem() method
            // to avoid circular dependency during LifecycleManager initialization
            
            IsInitialized = true;
            
            LootLockerLogger.Log("LootLockerStateData service initialized", LootLockerLogger.LogLevel.Verbose);
        }

        /// <summary>
        /// Set the EventSystem dependency and subscribe to events
        /// </summary>
        public void SetEventSystem(LootLockerEventSystem eventSystem)
        {
            if (eventSystem != null)
            {
                // Subscribe to session started events using the provided EventSystem instance
                eventSystem.SubscribeInstance<LootLockerSessionStartedEventData>(
                    LootLockerEventType.SessionStarted,
                    OnSessionStartedEvent
                );
                
                // Subscribe to session refreshed events using the provided EventSystem instance
                eventSystem.SubscribeInstance<LootLockerSessionRefreshedEventData>(
                    LootLockerEventType.SessionRefreshed,
                    OnSessionRefreshedEvent
                );
                
                // Subscribe to session ended events using the provided EventSystem instance
                eventSystem.SubscribeInstance<LootLockerSessionEndedEventData>(
                    LootLockerEventType.SessionEnded,
                    OnSessionEndedEvent
                );
                
                LootLockerLogger.Log("StateData event subscriptions established", LootLockerLogger.LogLevel.Debug);
            }
        }

        void ILootLockerService.Reset()
        {
            // Unsubscribe from events using static methods (safe during reset)
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

            IsInitialized = false;

            lock (_instanceLock)
            {
                _instance = null;
            }
        }

        void ILootLockerService.HandleApplicationPause(bool pauseStatus)
        {
            // StateData doesn't need to handle pause events
        }

        void ILootLockerService.HandleApplicationFocus(bool hasFocus)
        {
            // StateData doesn't need to handle focus events
        }

        void ILootLockerService.HandleApplicationQuit()
        {
            // Clean up any pending operations - Reset will handle event unsubscription
        }

        #endregion

        #region Singleton Management
        
        private static LootLockerStateData _instance;
        private static readonly object _instanceLock = new object();

        /// <summary>
        /// Get the StateData service instance through the LifecycleManager.
        /// Services are automatically registered and initialized on first access if needed.
        /// </summary>
        private static LootLockerStateData GetInstance()
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
                    _instance = LootLockerLifecycleManager.GetService<LootLockerStateData>();
                }
                return _instance;
            }
        }
        
        #endregion

        /// <summary>
        /// Handle session started events by saving the player data
        /// </summary>
        private void OnSessionStartedEvent(LootLockerSessionStartedEventData eventData)
        {
            LootLockerLogger.Log("LootLockerStateData: Handling SessionStarted event for player " + eventData?.playerData?.ULID, LootLockerLogger.LogLevel.Debug);
            if (eventData?.playerData != null)
            {
                SetPlayerData(eventData.playerData);
            }
        }

        /// <summary>
        /// Handle session refreshed events by updating the player data
        /// </summary>
        private void OnSessionRefreshedEvent(LootLockerSessionRefreshedEventData eventData)
        {
            LootLockerLogger.Log("LootLockerStateData: Handling SessionRefreshed event for player " + eventData?.playerData?.ULID, LootLockerLogger.LogLevel.Debug);
            if (eventData?.playerData != null)
            {
                SetPlayerData(eventData.playerData);
            }
        }

        /// <summary>
        /// Handle session ended events by managing local state appropriately
        /// </summary>
        private void OnSessionEndedEvent(LootLockerSessionEndedEventData eventData)
        {
            if (eventData == null || string.IsNullOrEmpty(eventData.playerUlid))
            {
                return;
            }

            LootLockerLogger.Log($"LootLockerStateData: Handling SessionEnded event for player {eventData.playerUlid}, clearLocalState: {eventData.clearLocalState}", LootLockerLogger.LogLevel.Debug);
            
            if (eventData.clearLocalState)
            {
                // Clear all saved state for this player
                ClearSavedStateForPlayerWithULID(eventData.playerUlid);
            }
            else
            {
                // Just set the player to inactive (remove from active players)
                SetPlayerULIDToInactive(eventData.playerUlid);
            }
        }

        //==================================================
        // Writer
        //==================================================
        private ILootLockerStateWriter _stateWriter =
            #if LOOTLOCKER_DISABLE_PLAYERPREFS
                new LootLockerNullStateWriter();
            #else
                new LootLockerPlayerPrefsStateWriter();
            #endif
        
        public void OverrideStateWriter(ILootLockerStateWriter newWriter)
        {
            if (newWriter != null)
            {
                _stateWriter = newWriter;
            }
        }

        //==================================================
        // Constants
        //==================================================
        private const string BaseSaveSlot = "LootLocker";
        private const string MetaDataSaveSlot = BaseSaveSlot + "_md";
        private const string PlayerDataSaveSlot = BaseSaveSlot + "_pd";

        //==================================================
        // Actual state
        //==================================================
        private LootLockerStateMetaData ActiveMetaData = null;
        private Dictionary<string, LootLockerPlayerData> ActivePlayerData = new Dictionary<string, LootLockerPlayerData>();

        #region Private Methods
        //==================================================
        // Private Methods
        //==================================================

        private void _LoadMetaDataFromPlayerPrefsIfNeeded()
        {
            if (ActiveMetaData != null)
            {
                return;
            }

            // Load from player prefs
            string metadataAsString = _stateWriter.GetString(MetaDataSaveSlot, "{}");
            if (!LootLockerJson.TryDeserializeObject(metadataAsString, out ActiveMetaData))
            {
                ActiveMetaData = new LootLockerStateMetaData();
            }

            // If there is only 1 player that has ever played, consider that the default is that one player
            if (string.IsNullOrEmpty(ActiveMetaData.DefaultPlayer) && ActiveMetaData.SavedPlayerStateULIDs.Count == 1)
            {
                ActiveMetaData.DefaultPlayer = ActiveMetaData.SavedPlayerStateULIDs[0];
            }

            _SaveMetaDataToPlayerPrefs();
        }

        private void _SaveMetaDataToPlayerPrefs()
        {
            string metadataJson = LootLockerJson.SerializeObject(ActiveMetaData);
            _stateWriter.SetString(MetaDataSaveSlot, metadataJson);
        }

        private void _SavePlayerDataToPlayerPrefs(string playerULID)
        {
            if (!ActivePlayerData.TryGetValue(playerULID, out var playerData))
            {
                return;
            }

            string playerDataJson = LootLockerJson.SerializeObject(playerData);
            _stateWriter.SetString($"{PlayerDataSaveSlot}_{playerULID}", playerDataJson);
        }

        private bool _LoadPlayerDataFromPlayerPrefs(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID))
            {
                return false;
            }

            if (!_SaveStateExistsForPlayer(playerULID))
            {
                return false;
            }

            string playerDataJson = _stateWriter.GetString($"{PlayerDataSaveSlot}_{playerULID}");
            if (!LootLockerJson.TryDeserializeObject(playerDataJson, out LootLockerPlayerData parsedPlayerData))
            {
                return false;
            }

            if (string.IsNullOrEmpty(parsedPlayerData.ULID))
            {
                return false;
            }

            ActivePlayerData.Add(parsedPlayerData.ULID, parsedPlayerData);
            LootLockerEventSystem.TriggerLocalSessionActivated(parsedPlayerData);
            return true;
        }

        #endregion // Private Methods

        #region Private Instance Methods (Used by Static Interface)
        //==================================================
        // Private Instance Methods (Used by Static Interface)
        //==================================================
        
        private void _OverrideStateWriter(ILootLockerStateWriter newWriter)
        {
            if (newWriter != null)
            {
                _stateWriter = newWriter;
            }
        }
        
        private bool _SaveStateExistsForPlayer(string playerULID)
        {
            return _stateWriter.HasKey($"{PlayerDataSaveSlot}_{playerULID}");
        }

        private LootLockerPlayerData _GetPlayerDataForPlayerWithUlidWithoutChangingState(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID))
            {
                return new LootLockerPlayerData();
            }
            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return new LootLockerPlayerData();
            }

            if (!_SaveStateExistsForPlayer(playerULID))
            {
                return new LootLockerPlayerData();
            }

            if (ActivePlayerData.TryGetValue(playerULID, out var data))
            {
                return data;
            }

            string playerDataJson = _stateWriter.GetString($"{PlayerDataSaveSlot}_{playerULID}");
            if (!LootLockerJson.TryDeserializeObject(playerDataJson, out LootLockerPlayerData parsedPlayerData))
            {
                return new LootLockerPlayerData();
            }
            return parsedPlayerData;
        }

        [CanBeNull]
        private LootLockerPlayerData _GetStateForPlayerOrDefaultStateOrEmpty(string playerULID)
        {
            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(playerULID) && string.IsNullOrEmpty(ActiveMetaData.DefaultPlayer))
            {
                return null;
            }
            string playerULIDToGetDataFor = string.IsNullOrEmpty(playerULID) ? ActiveMetaData.DefaultPlayer : playerULID;

            // Make this player the default for requests if there is no default yet or if the current default is not currently active
            bool shouldBeMadeDefault = ActivePlayerData.Count == 0 && !playerULIDToGetDataFor.Equals(ActiveMetaData.DefaultPlayer, StringComparison.OrdinalIgnoreCase);

            if (ActivePlayerData.TryGetValue(playerULIDToGetDataFor, out var data))
            {
                return data;
            }

            if (_LoadPlayerDataFromPlayerPrefs(playerULIDToGetDataFor))
            {
                if (ActivePlayerData.TryGetValue(playerULIDToGetDataFor, out var data2))
                {
                    if (shouldBeMadeDefault)
                    {
                        SetDefaultPlayerULID(data2.ULID);
                    }
                    return data2;
                }
            }
            return null;
        }

        private string _GetDefaultPlayerULID()
        {
            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return string.Empty;
            }

            return ActiveMetaData.DefaultPlayer;
        }

        private bool _SetDefaultPlayerULID(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID) || !SaveStateExistsForPlayer(playerULID))
            {
                return false;
            }

            if (!ActivePlayerData.ContainsKey(playerULID) && !_LoadPlayerDataFromPlayerPrefs(playerULID))
            {
                return false;
            }

            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return false;
            }

            ActiveMetaData.DefaultPlayer = playerULID;
            _SaveMetaDataToPlayerPrefs();
            return true;
        }

        private bool _SetPlayerData(LootLockerPlayerData updatedPlayerData)
        {
            if (updatedPlayerData == null || string.IsNullOrEmpty(updatedPlayerData.ULID))
            {
                return false;
            }

            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return false;
            }

            ActivePlayerData[updatedPlayerData.ULID] = updatedPlayerData;
            _SavePlayerDataToPlayerPrefs(updatedPlayerData.ULID);
            ActiveMetaData.SavedPlayerStateULIDs.AddUnique(updatedPlayerData.ULID);
            if (!string.IsNullOrEmpty(updatedPlayerData.WhiteLabelEmail))
            {
                ActiveMetaData.WhiteLabelEmailToPlayerUlidMap[updatedPlayerData.WhiteLabelEmail] = updatedPlayerData.ULID;
            }
            if (string.IsNullOrEmpty(ActiveMetaData.DefaultPlayer) || !ActivePlayerData.ContainsKey(ActiveMetaData.DefaultPlayer))
            {
                _SetDefaultPlayerULID(updatedPlayerData.ULID);
            }
            _SaveMetaDataToPlayerPrefs();

            return true;
        }

        private bool _ClearSavedStateForPlayerWithULID(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID))
            {
                return false;
            }

            if (!_SaveStateExistsForPlayer(playerULID))
            {
                return true;
            }

            ActivePlayerData.Remove(playerULID);
            _stateWriter.DeleteKey($"{PlayerDataSaveSlot}_{playerULID}");

            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData != null)
            {
                ActiveMetaData.SavedPlayerStateULIDs.Remove(playerULID);
                if (ActiveMetaData.DefaultPlayer.Equals(playerULID))
                {
                    ActiveMetaData.DefaultPlayer = "";
                }

                ActivePlayerData.TryGetValue(playerULID, out var playerData);
                if (!string.IsNullOrEmpty(playerData?.WhiteLabelEmail))
                {
                    ActiveMetaData.WhiteLabelEmailToPlayerUlidMap.Remove(playerData?.WhiteLabelEmail);
                }
                _SaveMetaDataToPlayerPrefs();
            }
            
            LootLockerEventSystem.TriggerLocalSessionDeactivated(playerULID);
            return true;
        }

        private List<string> _ClearAllSavedStates()
        {
            List<string> removedULIDs = new List<string>();
            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return removedULIDs;
            }

            List<string> ulidsToRemove = new List<string>(ActiveMetaData.SavedPlayerStateULIDs);
            foreach (string ULID in ulidsToRemove)
            {
                if (_ClearSavedStateForPlayerWithULID(ULID))
                {
                    removedULIDs.Add(ULID);
                }
            }

            ActiveMetaData = new LootLockerStateMetaData();
            _SaveMetaDataToPlayerPrefs();
            return removedULIDs;
        }

        private List<string> _ClearAllSavedStatesExceptForPlayer(string playerULID)
        {
            List<string> removedULIDs = new List<string>();
            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return removedULIDs;
            }

            List<string> ulidsToRemove = new List<string>(ActiveMetaData.SavedPlayerStateULIDs);
            foreach (string ULID in ulidsToRemove)
            {
                if (!ULID.Equals(playerULID, StringComparison.OrdinalIgnoreCase))
                {
                    if (_ClearSavedStateForPlayerWithULID(ULID))
                    {
                        removedULIDs.Add(ULID);
                    }
                }
            }

            _SetDefaultPlayerULID(playerULID);
            return removedULIDs;
        }

        private void _SetPlayerULIDToInactive(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID) || !ActivePlayerData.ContainsKey(playerULID))
            {
                return;
            }

            ActivePlayerData.Remove(playerULID);
            LootLockerEventSystem.TriggerLocalSessionDeactivated(playerULID);
        }

        private void _SetAllPlayersToInactive()
        {
            var activePlayers = ActivePlayerData.Keys.ToList();
            foreach (string playerULID in activePlayers)
            {
                _SetPlayerULIDToInactive(playerULID);
            }
        }

        private void _SetAllPlayersToInactiveExceptForPlayer(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID))
            {
                return;
            }

            var keysToRemove = ActivePlayerData.Keys.Where(key => !key.Equals(playerULID, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (string key in keysToRemove)
            {
                _SetPlayerULIDToInactive(key);
            }

            _SetDefaultPlayerULID(playerULID);
        }

        private List<string> _GetActivePlayerULIDs()
        {
            return ActivePlayerData.Keys.ToList();
        }

        private List<string> _GetCachedPlayerULIDs()
        {
            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return new List<string>();
            }
            return ActiveMetaData.SavedPlayerStateULIDs;
        }

        [CanBeNull]
        private string _GetPlayerUlidFromWLEmail(string email)
        {
            _LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return null;
            }

            ActiveMetaData.WhiteLabelEmailToPlayerUlidMap.TryGetValue(email, out string playerUlid);
            return playerUlid;
        }

        private void _UnloadState()
        {
            ActiveMetaData = null;
            ActivePlayerData.Clear();
        }

        #endregion // Private Instance Methods

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // Unsubscribe from events on destruction using static methods
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
        }

        #endregion

        #region Static Methods
        //==================================================
        // Static Methods (Primary Interface)
        //==================================================
        
        public static void overrideStateWriter(ILootLockerStateWriter newWriter)
        {
            GetInstance()._OverrideStateWriter(newWriter);
        }
        
        public static bool SaveStateExistsForPlayer(string playerULID)
        {
            return GetInstance()._SaveStateExistsForPlayer(playerULID);
        }

        public static LootLockerPlayerData GetPlayerDataForPlayerWithUlidWithoutChangingState(string playerULID)
        {
            return GetInstance()._GetPlayerDataForPlayerWithUlidWithoutChangingState(playerULID);
        }

        [CanBeNull]
        public static LootLockerPlayerData GetStateForPlayerOrDefaultStateOrEmpty(string playerULID)
        {
            return GetInstance()._GetStateForPlayerOrDefaultStateOrEmpty(playerULID);
        }

        public static string GetDefaultPlayerULID()
        {
            return GetInstance()._GetDefaultPlayerULID();
        }

        public static bool SetDefaultPlayerULID(string playerULID)
        {
            return GetInstance()._SetDefaultPlayerULID(playerULID);
        }

        public static bool SetPlayerData(LootLockerPlayerData updatedPlayerData)
        {
            return GetInstance()._SetPlayerData(updatedPlayerData);
        }

        public static bool ClearSavedStateForPlayerWithULID(string playerULID)
        {
            return GetInstance()._ClearSavedStateForPlayerWithULID(playerULID);
        }

        public static List<string> ClearAllSavedStates()
        {
            return GetInstance()._ClearAllSavedStates();
        }

        public static List<string> ClearAllSavedStatesExceptForPlayer(string playerULID)
        {
            return GetInstance()._ClearAllSavedStatesExceptForPlayer(playerULID);
        }

        public static void SetPlayerULIDToInactive(string playerULID)
        {
            GetInstance()._SetPlayerULIDToInactive(playerULID);
        }

        public static void SetAllPlayersToInactive()
        {
            GetInstance()._SetAllPlayersToInactive();
        }

        public static void SetAllPlayersToInactiveExceptForPlayer(string playerULID)
        {
            GetInstance()._SetAllPlayersToInactiveExceptForPlayer(playerULID);
        }

        public static List<string> GetActivePlayerULIDs()
        {
            return GetInstance()._GetActivePlayerULIDs();
        }

        public static List<string> GetCachedPlayerULIDs()
        {
            return GetInstance()._GetCachedPlayerULIDs();
        }

        [CanBeNull]
        public static string GetPlayerUlidFromWLEmail(string email)
        {
            return GetInstance()._GetPlayerUlidFromWLEmail(email);
        }

        public static void UnloadState()
        {
            GetInstance()._UnloadState();
        }

        #endregion // Static Methods
    }
}