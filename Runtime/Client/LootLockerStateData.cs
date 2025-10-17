using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

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

    public class LootLockerStateData
    {
        public LootLockerStateData()
        {
            LoadMetaDataFromPlayerPrefsIfNeeded();
        }

        //==================================================
        // Writer
        //==================================================
        private static ILootLockerStateWriter _stateWriter =
            #if LOOTLOCKER_DISABLE_PLAYERPREFS
                new LootLockerNullStateWriter();
            #else
                new LootLockerPlayerPrefsStateWriter();
            #endif
        public static void overrideStateWriter(ILootLockerStateWriter newWriter)
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
        private static LootLockerStateMetaData ActiveMetaData = null;
        private static Dictionary<string, LootLockerPlayerData> ActivePlayerData = new Dictionary<string, LootLockerPlayerData>();

        #region Private Methods
        //==================================================
        // Private Methods
        //==================================================

        private static void LoadMetaDataFromPlayerPrefsIfNeeded()
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

            SaveMetaDataToPlayerPrefs();
        }

        private static void SaveMetaDataToPlayerPrefs()
        {
            string metadataJson = LootLockerJson.SerializeObject(ActiveMetaData);
            _stateWriter.SetString(MetaDataSaveSlot, metadataJson);
        }

        private static void SavePlayerDataToPlayerPrefs(string playerULID)
        {
            if (!ActivePlayerData.TryGetValue(playerULID, out var playerData))
            {
                return;
            }

            string playerDataJson = LootLockerJson.SerializeObject(playerData);
            _stateWriter.SetString($"{PlayerDataSaveSlot}_{playerULID}", playerDataJson);
        }

        private static bool LoadPlayerDataFromPlayerPrefs(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID))
            {
                return false;
            }

            if (!SaveStateExistsForPlayer(playerULID))
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
            return true;
        }

        #endregion // Private Methods

        #region Public Methods
        //==================================================
        // Public Methods
        //==================================================
        public static bool SaveStateExistsForPlayer(string playerULID)
        {
            return _stateWriter.HasKey($"{PlayerDataSaveSlot}_{playerULID}");
        }

        public static LootLockerPlayerData GetPlayerDataForPlayerWithUlidWithoutChangingState(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID))
            {
                return new LootLockerPlayerData();
            }
            LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return new LootLockerPlayerData();
            }

            if (!SaveStateExistsForPlayer(playerULID))
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
        public static LootLockerPlayerData GetStateForPlayerOrDefaultStateOrEmpty(string playerULID)
        {
            LoadMetaDataFromPlayerPrefsIfNeeded();
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

            if (LoadPlayerDataFromPlayerPrefs(playerULIDToGetDataFor))
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

        public static string GetDefaultPlayerULID()
        {
            LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return string.Empty;
            }

            return ActiveMetaData.DefaultPlayer;
        }

        public static bool SetDefaultPlayerULID(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID) || !SaveStateExistsForPlayer(playerULID))
            {
                return false;
            }

            if (!ActivePlayerData.ContainsKey(playerULID) && !LoadPlayerDataFromPlayerPrefs(playerULID))
            {
                return false;
            }

            LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return false;
            }

            ActiveMetaData.DefaultPlayer = playerULID;
            SaveMetaDataToPlayerPrefs();
            return true;
        }

        public static bool SetPlayerData(LootLockerPlayerData updatedPlayerData)
        {
            if (updatedPlayerData == null || string.IsNullOrEmpty(updatedPlayerData.ULID))
            {
                return false;
            }

            LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return false;
            }

            ActivePlayerData[updatedPlayerData.ULID] = updatedPlayerData;
            SavePlayerDataToPlayerPrefs(updatedPlayerData.ULID);
            ActiveMetaData.SavedPlayerStateULIDs.AddUnique(updatedPlayerData.ULID);
            if (!string.IsNullOrEmpty(updatedPlayerData.WhiteLabelEmail))
            {
                ActiveMetaData.WhiteLabelEmailToPlayerUlidMap[updatedPlayerData.WhiteLabelEmail] = updatedPlayerData.ULID;
            }
            if (string.IsNullOrEmpty(ActiveMetaData.DefaultPlayer) || !ActivePlayerData.ContainsKey(ActiveMetaData.DefaultPlayer))
            {
                SetDefaultPlayerULID(updatedPlayerData.ULID);
            }
            SaveMetaDataToPlayerPrefs();

            return true;
        }

        public static bool ClearSavedStateForPlayerWithULID(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID))
            {
                return false;
            }

            if (!SaveStateExistsForPlayer(playerULID))
            {
                return true;
            }

            ActivePlayerData.Remove(playerULID);
            _stateWriter.DeleteKey($"{PlayerDataSaveSlot}_{playerULID}");

            LoadMetaDataFromPlayerPrefsIfNeeded();
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
                SaveMetaDataToPlayerPrefs();
            }
            return true;
        }

        public static List<string> ClearAllSavedStates()
        {
            List<string> removedULIDs = new List<string>();
            LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return removedULIDs;
            }

            List<string> ulidsToRemove = new List<string>(ActiveMetaData.SavedPlayerStateULIDs);
            foreach (string ULID in ulidsToRemove)
            {
                if (ClearSavedStateForPlayerWithULID(ULID))
                {
                    removedULIDs.Add(ULID);
                }
            }

            ActiveMetaData = new LootLockerStateMetaData();
            SaveMetaDataToPlayerPrefs();
            return removedULIDs;
        }

        public static List<string> ClearAllSavedStatesExceptForPlayer(string playerULID)
        {
            List<string> removedULIDs = new List<string>();
            LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return removedULIDs;
            }

            List<string> ulidsToRemove = new List<string>(ActiveMetaData.SavedPlayerStateULIDs);
            foreach (string ULID in ulidsToRemove)
            {
                if (!ULID.Equals(playerULID, StringComparison.OrdinalIgnoreCase))
                {
                    if (ClearSavedStateForPlayerWithULID(ULID))
                    {
                        removedULIDs.Add(ULID);
                    }
                }
            }

            SetDefaultPlayerULID(playerULID);
            return removedULIDs;
        }

        public static void SetPlayerULIDToInactive(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID) || !ActivePlayerData.ContainsKey(playerULID))
            {
                return;
            }

            ActivePlayerData.Remove(playerULID);
        }

        public static void SetAllPlayersToInactive()
        {
            ActivePlayerData.Clear();
        }

        public static void SetAllPlayersToInactiveExceptForPlayer(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID))
            {
                return;
            }

            var keysToRemove = ActivePlayerData.Keys.Where(key => !key.Equals(playerULID, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (string key in keysToRemove)
            {
                ActivePlayerData.Remove(key);
            }

            SetDefaultPlayerULID(playerULID);
        }

        public static List<string> GetActivePlayerULIDs()
        {
            return ActivePlayerData.Keys.ToList();
        }

        public static List<string> GetCachedPlayerULIDs()
        {
            LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return new List<string>();
            }
            return ActiveMetaData.SavedPlayerStateULIDs;
        }

        [CanBeNull]
        public static string GetPlayerUlidFromWLEmail(string email)
        {
            LoadMetaDataFromPlayerPrefsIfNeeded();
            if (ActiveMetaData == null)
            {
                return null;
            }

            ActiveMetaData.WhiteLabelEmailToPlayerUlidMap.TryGetValue(email, out string playerUlid);
            return playerUlid;
        }

        public static void Reset()
        {
            ActiveMetaData = null;
            ActivePlayerData.Clear();
        }
    }
    #endregion // Public Methods
}