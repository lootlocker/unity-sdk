using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LootLocker.Requests;
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
        public bool MultiUserInitialLoadCompleted { get; set; } //TODO: Deprecated (or rather temporary) - Remove after 20251001
    }

    public class LootLockerStateData
    {
        //TODO: Deprecated (or rather temporary) - Remove after 20251001
        private static bool MultiUserMigrationInProgress = false;
        //TODO: Deprecated (or rather temporary) - Remove after 20251001
        public LootLockerStateData()
        {
            LoadMetaDataFromPlayerPrefsIfNeeded();
            if (!ActiveMetaData.MultiUserInitialLoadCompleted)
            {
                TransferPlayerCacheToMultiUserSystem();
            }
        }
#if UNITY_EDITOR
        //TODO: Deprecated (or rather temporary) - Remove after 20251001
        public static void ResetMultiUserTransferFlag()
        {
            LoadMetaDataFromPlayerPrefsIfNeeded();
            ActiveMetaData.MultiUserInitialLoadCompleted = false;
            MultiUserMigrationInProgress = false;
        }
#endif

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
            string metadataAsString = PlayerPrefs.GetString(MetaDataSaveSlot, "{}");
            if (!LootLockerJson.TryDeserializeObject(metadataAsString, out ActiveMetaData))
            {
                ActiveMetaData = new LootLockerStateMetaData();
            }

            // If there is only 1 player that has ever played, consider that the default is that one player
            if (string.IsNullOrEmpty(ActiveMetaData.DefaultPlayer) && ActiveMetaData.SavedPlayerStateULIDs.Count == 1)
            {
                ActiveMetaData.DefaultPlayer = ActiveMetaData.SavedPlayerStateULIDs[0];
            }

            if (!ActiveMetaData.MultiUserInitialLoadCompleted)
            {
                TransferPlayerCacheToMultiUserSystem();
            }

            SaveMetaDataToPlayerPrefs();
        }

        private static void SaveMetaDataToPlayerPrefs()
        {
            string metadataJson = LootLockerJson.SerializeObject(ActiveMetaData);
            PlayerPrefs.SetString(MetaDataSaveSlot, metadataJson);
            PlayerPrefs.Save();
        }

        private static void SavePlayerDataToPlayerPrefs(string playerULID)
        {
            if (!ActivePlayerData.TryGetValue(playerULID, out var playerData))
            {
                return;
            }

            string playerDataJson = LootLockerJson.SerializeObject(playerData);
            PlayerPrefs.SetString($"{PlayerDataSaveSlot}_{playerULID}", playerDataJson);
            PlayerPrefs.Save();
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

            string playerDataJson = PlayerPrefs.GetString($"{PlayerDataSaveSlot}_{playerULID}");
            if (!LootLockerJson.TryDeserializeObject(playerDataJson, out LootLockerPlayerData parsedPlayerData)) //TODO: auth platform is not parsed correctly. Write json test
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
            return PlayerPrefs.HasKey($"{PlayerDataSaveSlot}_{playerULID}");
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

            if (ActivePlayerData.TryGetValue(playerULIDToGetDataFor, out var data))
            {
                return data;
            }

            if (LoadPlayerDataFromPlayerPrefs(playerULIDToGetDataFor))
            {
                if (ActivePlayerData.TryGetValue(playerULIDToGetDataFor, out var data2))
                {
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
            if (string.IsNullOrEmpty(ActiveMetaData.DefaultPlayer))
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
            PlayerPrefs.DeleteKey($"{PlayerDataSaveSlot}_{playerULID}");
            PlayerPrefs.Save();

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

        public static void SetPlayerULIDToInactive(string playerULID)
        {
            if (string.IsNullOrEmpty(playerULID) || !ActivePlayerData.ContainsKey(playerULID))
            {
                return;
            }

            ActivePlayerData.Remove(playerULID);
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

        //TODO: Deprecated (or rather temporary) - Remove after 20251001
        public static bool TransferPlayerCacheToMultiUserSystem()
        {
            if (MultiUserMigrationInProgress)
            {
                return false;
            }
            var cache = LootLockerConfig.current;
#pragma warning disable CS0618 // This is the transfer mechanic from the obsolete members
            if (cache == null || string.IsNullOrEmpty(cache.token))
            {
                ActiveMetaData.MultiUserInitialLoadCompleted = true;
                SaveMetaDataToPlayerPrefs();
                return false;
            }

            MultiUserMigrationInProgress = true;
            LootLockerPlayerData playerData = new LootLockerPlayerData();
            playerData.CreatedAt = new DateTime(1970, 1, 1);
            playerData.CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation((LL_AuthPlatforms)PlayerPrefs.GetInt("LastActivePlatform"));
            playerData.Identifier = playerData.CurrentPlatform.Platform == LL_AuthPlatforms.Guest ? PlayerPrefs.GetString("LootLockerGuestPlayerID", "") : cache.deviceID;
            playerData.LastSignIn = new DateTime(1970, 1, 1);
            playerData.LegacyID = -1;
            playerData.Name = "";
            playerData.PublicUID = "";
            playerData.RefreshToken = cache.refreshToken;
            playerData.SessionToken = cache.token;
            playerData.ULID = string.IsNullOrEmpty(cache.playerULID) ? "temp-ulid" : cache.playerULID;
            playerData.WalletID = "";
#pragma warning restore CS0618 // This was the transfer mechanic from the obsolete members
            PlayerPrefs.GetString("LootLockerWhiteLabelSessionEmail", playerData.WhiteLabelEmail);
            PlayerPrefs.GetString("LootLockerWhiteLabelSessionToken", playerData.WhiteLabelToken);

            ActivePlayerData[playerData.ULID] = playerData;

            LootLockerSDKManager.GetCurrentPlayerInfo((response) =>
            {
                if (!response.success)
                {
                    ActivePlayerData.Remove(playerData.ULID);
                    return;
                }

                string oldULID = playerData.ULID;
                var playerInfo = response.info;
                playerData.ULID = playerInfo.id;
                playerData.CreatedAt = playerInfo.created_at;
                playerData.Name = playerInfo.name;
                playerData.PublicUID = playerInfo.public_uid;
                playerData.LegacyID = playerInfo.legacy_id;
                playerData.LastSignIn = DateTime.Now;
                ActivePlayerData[playerData.ULID] = playerData;
                if (!oldULID.Equals(playerData.ULID))
                {
                    ActivePlayerData.Remove(oldULID);
                }
                ActiveMetaData.SavedPlayerStateULIDs.AddUnique(playerData.ULID);
                if (string.IsNullOrEmpty(ActiveMetaData.DefaultPlayer) || ActiveMetaData.DefaultPlayer.Equals(oldULID))
                {
                    ActiveMetaData.DefaultPlayer = playerData.ULID;
                }

                if (!string.IsNullOrEmpty(playerData.WhiteLabelEmail))
                {
                    ActiveMetaData.WhiteLabelEmailToPlayerUlidMap[playerData.WhiteLabelEmail] = playerData.ULID;
                }

                ActiveMetaData.MultiUserInitialLoadCompleted = true;
                SaveMetaDataToPlayerPrefs();
                SavePlayerDataToPlayerPrefs(playerData.ULID);
                PlayerPrefs.DeleteKey("LootLockerWhiteLabelSessionEmail");
                PlayerPrefs.DeleteKey("LootLockerWhiteLabelSessionToken");
                PlayerPrefs.DeleteKey("LootLockerGuestPlayerID");
                PlayerPrefs.DeleteKey("LastActivePlatform");
                PlayerPrefs.Save();
                MultiUserMigrationInProgress = false;
            }, playerData.ULID);
            return true;
        }
    }
    #endregion // Public Methods
}