using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using LootLocker;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using LootLocker.LootLockerEnums;

namespace LootLocker.Requests
{
    public partial class LootLockerSDKManager
    {
        #region Init

        static bool initialized;
        public static bool Init()
        {
            DebugMessage("SDK is Intializing");
            LootLockerServerManager.CheckInit();
            return LoadConfig();
        }

        static bool LoadConfig()
        {
            LootLockerBaseServerAPI.activeConfig = LootLockerConfig.current;

            initialized = true;
            if (string.IsNullOrEmpty(LootLockerConfig.current.apiKey))
            {
                DebugMessage("Key has not been set, Please login to sdk manager or set key manually and then try again");
                initialized = false;
                return false;
            }

            return initialized;
        }

        /// <summary>
        /// Utility function to check if the sdk has been initiazed
        /// </summary>
        /// <returns></returns>
        public static bool CheckInitialized()
        {
            if (!initialized)
            {
                return Init();
            }

            try
            {
                LootLockerBaseServerAPI.activeConfig = LootLockerConfig.current;
            }
            catch (Exception ex)
            {

                DebugMessage("Couldn't change activeConfig on ServerAPI to User config. " + ex, true);

            }

            return true;
        }

        public static void DebugMessage(string message, bool IsError = false)
        {
#if     UNITY_EDITOR
            if(LootLockerConfig.current!=null && LootLockerConfig.current.currentDebugLevel == LootLockerGenericConfig.DebugLevel.All)
            {
                if (IsError)
                    Debug.LogError(message);
                else
                    Debug.Log(message);
            }
            else if (LootLockerConfig.current != null && LootLockerConfig.current.currentDebugLevel == LootLockerGenericConfig.DebugLevel.ErrorOnly)
            {
                if (IsError)
                    Debug.LogError(message);
            }
            else if (LootLockerConfig.current != null && LootLockerConfig.current.currentDebugLevel == LootLockerGenericConfig.DebugLevel.NormalOnly)
            {
                if (!IsError)
                    Debug.LogError(message);
            }
#endif
        }

        #endregion

        #region Authentication
        public static void VerifySteamID(string steamSessionTicket, Action<LootLockerVerifyResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerVerifyRequest verifyRequest = new LootLockerVerifyRequest(steamSessionTicket);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }

        public static void VerifyID(string deviceId, Action<LootLockerVerifyResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerVerifyRequest verifyRequest = new LootLockerVerifyRequest(deviceId);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }

        public static void StartSession(string deviceId, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerConfig.current.deviceID = deviceId;
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(deviceId);
            LootLockerAPIManager.Session(sessionRequest, onComplete);
        }
        public static void StartSteamSession(string steamId64, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(steamId64);
            LootLockerAPIManager.Session(sessionRequest, onComplete);
        }

        public static void EndSession(string deviceId, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(deviceId);
            LootLockerAPIManager.EndSession(sessionRequest, onComplete);
        }
        #endregion

        #region Player
        //Player calls
        public static void GetPlayerInfo(Action<LootLockerGetPlayerInfoResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetPlayerInfo(onComplete);
        }

        public static void GetInventory(Action<LootLockerInventoryResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetInventory(onComplete);
        }

        public static void GetBalance(Action<LootLockerBalanceResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetBalance(onComplete);
        }

        public static void SubmitXp(int xpToSubmit, Action<LootLockerXpSubmitResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerXpSubmitRequest xpSubmitRequest = new LootLockerXpSubmitRequest(xpToSubmit);
            LootLockerAPIManager.SubmitXp(xpSubmitRequest, onComplete);
        }

        public static void GetXpAndLevel(Action<LootLockerXpResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerXpRequest xpRequest = new LootLockerXpRequest();
            LootLockerAPIManager.GetXpAndLevel(xpRequest, onComplete);
        }

        public static void GetAssetNotification(Action<LootLockerPlayerAssetNotificationsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetPlayerAssetNotification(onComplete);
        }

        public static void GetDeactivatedAssetNotification(Action<LootLockerDeactivatedAssetsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetDeactivatedAssetNotification(onComplete);
        }

        public static void InitiateDLCMigration(Action<LootLockerDlcResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.InitiateDLCMigration(onComplete);
        }

        public static void GetDLCMigrated(Action<LootLockerDlcResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetDLCMigrated(onComplete);
        }

        public static void SetProfilePrivate(Action<LootLockerStandardResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.SetProfilePrivate(onComplete);
        }

        public static void SetProfilePublic(Action<LootLockerStandardResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.SetProfilePublic(onComplete);
        }
        #endregion

        #region Character
        public static void GetCharacterLoadout(Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetCharacterLoadout(onComplete);
        }

        public static void GetOtherPlayersCharacterLoadout(string characterID, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();

            data.getRequests.Add(characterID);
            data.getRequests.Add(LootLockerConfig.current.platform.ToString());
            LootLockerAPIManager.GetOtherPlayersCharacterLoadout(data, onComplete);
        }

        public static void UpdateCharacter(string characterID, string newCharacterName, bool isDefault, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            LootLockerUpdateCharacterRequest data = new LootLockerUpdateCharacterRequest();

            data.name = newCharacterName;
            data.is_default = isDefault;

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(characterID);

            LootLockerAPIManager.UpdateCharacter(lootLockerGetRequest, data, onComplete);
        }

        public static void EquipIdAssetToDefaultCharacter(string assetInstanceId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerEquipByIDRequest data = new LootLockerEquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceId);
            LootLockerAPIManager.EquipIdAssetToDefaultCharacter(data, onComplete);
        }

        public static void EquipGlobalAssetToDefaultCharacter(string assetId, string assetVariationId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerEquipByAssetRequest data = new LootLockerEquipByAssetRequest();
            data.asset_id = int.Parse(assetId);
            data.asset_variation_id = int.Parse(assetVariationId);
            LootLockerAPIManager.EquipGlobalAssetToDefaultCharacter(data, onComplete);
        }

        public static void EquipIdAssetToCharacter(string characterID, string assetInstanceId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerEquipByIDRequest data = new LootLockerEquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceId);

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID);
            LootLockerAPIManager.EquipIdAssetToCharacter(lootLockerGetRequest, data, onComplete);
        }

        public static void EquipGlobalAssetToCharacter(string assetId, string assetVariationId, string characterID, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerEquipByAssetRequest data = new LootLockerEquipByAssetRequest();
            data.asset_id = int.Parse(assetId);
            data.asset_variation_id = int.Parse(assetVariationId);
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID);
            LootLockerAPIManager.EquipGlobalAssetToCharacter(lootLockerGetRequest, data, onComplete);
        }

        public static void UnEquipIdAssetToDefaultCharacter(string assetId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(assetId);
            LootLockerAPIManager.UnEquipIdAssetToDefaultCharacter(lootLockerGetRequest, onComplete);
        }

        public static void UnEquipIdAssetToCharacter(string assetId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(assetId);
            LootLockerAPIManager.UnEquipIdAssetToCharacter(lootLockerGetRequest, onComplete);
        }

        public static void GetCurrentLoadOutToDefaultCharacter(Action<LootLockerGetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetCurrentLoadOutToDefaultCharacter(onComplete);
        }

        public static void GetCurrentLoadOutToOtherCharacter(string characterID, Action<LootLockerGetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID);
            lootLockerGetRequest.getRequests.Add(LootLockerConfig.current.platform.ToString());
            LootLockerAPIManager.GetCurrentLoadOutToOtherCharacter(lootLockerGetRequest, onComplete);
        }

        public static void GetEquipableContextToDefaultCharacter(Action<LootLockerContextResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetEquipableContextToDefaultCharacter(onComplete);
        }
        #endregion

        #region PlayerStorage
        public static void GetEntirePersistentStorage(Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetEntirePersistentStorage(onComplete);
        }

        public static void GetSingleKeyPersistentStorage(string key, Action<LootLockerGetPersistentSingle> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(key);
            LootLockerAPIManager.GetSingleKeyPersistentStorage(data,onComplete);
        }

        public static void UpdateOrCreateKeyValue(string key, string value, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetPersistentStoragRequest data = new LootLockerGetPersistentStoragRequest();
            data.AddToPayload(new LootLockerPayload { key = key, value = value });
            LootLockerAPIManager.UpdateOrCreateKeyValue(data, onComplete);
        }

        public static void UpdateOrCreateKeyValue(LootLockerGetPersistentStoragRequest data, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.UpdateOrCreateKeyValue(data, onComplete);
        }

        public static void DeleteKeyValue(string keyToDelete, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(keyToDelete);
            LootLockerAPIManager.DeleteKeyValue(data, onComplete);
        }

        public static void GetOtherPlayersPublicKeyValuePairs(string otherPlayerId, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(otherPlayerId);
            LootLockerAPIManager.GetOtherPlayersPublicKeyValuePairs(data, onComplete);
        }
        #endregion

        #region Assets
        public static void GetContext(Action<LootLockerContextResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetContext(onComplete);
        }

        public static void GetAssetsOriginal(int assetCount, Action<LootLockerAssetResponse> onComplete, int? idOfLastAsset = null, LootLocker.LootLockerEnums.AssetFilter filter =  LootLocker.LootLockerEnums.AssetFilter.none)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetAssetsOriginal(onComplete, assetCount, idOfLastAsset, filter);
        }

        public static void GetAssetListWithCount(int assetCount, Action<LootLockerAssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetCount.ToString());
            LootLockerAPIManager.GetAssetListWithCount(data, onComplete);
        }

        public static void GetAssetNextList(int assetCount, Action<LootLockerAssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            if (LootLockerAssetRequest.lastId != 0)
            {
                LootLockerAssetRequest data = new LootLockerAssetRequest();
                data.count = assetCount;
                LootLockerAPIManager.GetAssetListWithAfterCount(data, onComplete);
            }
            else
            {
                GetAssetListWithCount(assetCount, onComplete);
            }
        }

        public void ResetAssetCalls()
        {
            LootLockerAssetRequest.lastId = 0;
        }

        public static void GetAssetInformation(string assetId, Action<LootLockerCommonAsset> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.GetAssetInformation(data, onComplete);
        }

        public static void ListFavouriteAssets(Action<LootLockerFavouritesListResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.ListFavouriteAssets(onComplete);
        }

        public static void AddFavouriteAsset(string assetId, Action<LootLockerAssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.AddFavouriteAsset(data, onComplete);
        }

        public static void RemoveFavouriteAsset(string assetId, Action<LootLockerAssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.RemoveFavouriteAsset(data, onComplete);
        }

        public static void GetAssetsById(string[] assetIdsToRetrieve, Action<LootLockerAssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();

            for (int i = 0; i < assetIdsToRetrieve.Length; i++) 
            data.getRequests.Add(assetIdsToRetrieve[i]);

            LootLockerAPIManager.GetAssetsById(data, onComplete);
        }

        #endregion

        #region AssetInstance
        public static void GetAllKeyValuePairsForAssetInstances(Action<LootLockerGetAllKeyValuePairsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetAllKeyValuePairs(onComplete);
        }

        public static void GetAllKeyValuePairsToAnInstance(int instanceId, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(instanceId.ToString());
            LootLockerAPIManager.GetAllKeyValuePairsToAnInstance(data, onComplete);
        }

        public static void GetAKeyValuePairByIdForAssetInstances(int assetId, int instanceId, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            data.getRequests.Add(instanceId.ToString());
            LootLockerAPIManager.GetAKeyValuePairById(data, onComplete);
        }

        public static void CreateKeyValuePairForAssetInstances(int assetId, string key, string value, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerCreateKeyValuePairRequest createKeyValuePairRequest = new LootLockerCreateKeyValuePairRequest();
            createKeyValuePairRequest.key = key;
            createKeyValuePairRequest.value = value;
            LootLockerAPIManager.CreateKeyValuePair(data, createKeyValuePairRequest, onComplete);
        }

        public static void UpdateOneOrMoreKeyValuePairForAssetInstances(int assetId, Dictionary<string, string> data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest request = new LootLockerGetRequest();
            request.getRequests.Add(assetId.ToString());
            LootLockerUpdateOneOrMoreKeyValuePairRequest createKeyValuePairRequest = new LootLockerUpdateOneOrMoreKeyValuePairRequest();
            List<LootLockerCreateKeyValuePairRequest> temp = new List<LootLockerCreateKeyValuePairRequest>();
            foreach (var d in data)
            {
                temp.Add(new LootLockerCreateKeyValuePairRequest { key = d.Key, value = d.Value });
            }
            createKeyValuePairRequest.storage = temp.ToArray();
            LootLockerAPIManager.UpdateOneOrMoreKeyValuePair(request, createKeyValuePairRequest, onComplete);
        }

        public static void UpdateKeyValuePairByIdForAssetInstances(int assetId, string key, string value, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerCreateKeyValuePairRequest createKeyValuePairRequest = new LootLockerCreateKeyValuePairRequest();
            createKeyValuePairRequest.key = key;
            createKeyValuePairRequest.value = value;
            LootLockerAPIManager.UpdateKeyValuePairById(data, createKeyValuePairRequest, onComplete);
        }

        public static void DeleteKeyValuePairForAssetInstances(int assetId, int instanceId, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            data.getRequests.Add(instanceId.ToString());
            LootLockerAPIManager.DeleteKeyValuePair(data, onComplete);
        }

        public static void InspectALootBoxForAssetInstances(int assetId, Action<LootLockerInspectALootBoxResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.InspectALootBox(data, onComplete);
        }

        public static void OpenALootBoxForAssetInstances(int assetId, Action<LootLockerOpenLootBoxResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.OpenALootBox(data, onComplete);
        }
        #endregion

        #region UserGeneratedContent
        private static void ConvertAssetDictionaries(Dictionary<string, string> kv_storage, Dictionary<string, string> filters,
            Dictionary<string, string> data_entities, out List<LootLockerAssetKVPair> temp_kv, out List<LootLockerAssetKVPair> temp_filters, out List<LootLockerDataEntity> temp_data)
        {
            temp_kv = new List<LootLockerAssetKVPair>();
            if (kv_storage != null)
            {
                foreach (var d in kv_storage)
                {
                    temp_kv.Add(new LootLockerAssetKVPair { key = d.Key, value = d.Value });
                }
            }

            temp_filters = new List<LootLockerAssetKVPair>();
            if (filters != null)
            {
                foreach (var d in filters)
                {
                    temp_filters.Add(new LootLockerAssetKVPair { key = d.Key, value = d.Value });
                }
            }

            temp_data = new List<LootLockerDataEntity>();
            if (data_entities != null)
            {
                foreach (var d in data_entities)
                {
                    temp_data.Add(new LootLockerDataEntity { name = d.Key, data = d.Value });
                }
            }
        }

        public static void CreatingAnAssetCandidate(string name, Action<LootLockerUserGenerateContentResponse> onComplete,
            Dictionary<string, string> kv_storage = null, Dictionary<string, string> filters = null,
            Dictionary<string, string> data_entities = null, int context_id = -1)
        {
            if (!CheckInitialized()) return;

            ConvertAssetDictionaries(kv_storage, filters, data_entities,
                out List<LootLockerAssetKVPair> temp_kv, out List<LootLockerAssetKVPair> temp_filters, out List<LootLockerDataEntity> temp_data);

            LootLockerAssetData assetData = new LootLockerAssetData
            {
                name = name,
                kv_storage = temp_kv.ToArray(),
                filters = temp_filters.ToArray(),
                data_entities = temp_data.ToArray(),
                context_id = context_id,
            };

            LootLockerCreatingOrUpdatingAnAssetCandidateRequest data = new LootLockerCreatingOrUpdatingAnAssetCandidateRequest
            {
                data = assetData,
            };

            LootLockerAPIManager.CreatingAnAssetCandidate(data, onComplete);
        }

        public static void UpdatingAnAssetCandidate(int assetId, bool isCompleted, Action<LootLockerUserGenerateContentResponse> onComplete,
            string name = null, Dictionary<string, string> kv_storage = null, Dictionary<string, string> filters = null,
            Dictionary<string, string> data_entities = null, int context_id = -1)
        {
            if (!CheckInitialized()) return;

            ConvertAssetDictionaries(kv_storage, filters, data_entities,
                out List<LootLockerAssetKVPair> temp_kv, out List<LootLockerAssetKVPair> temp_filters, out List<LootLockerDataEntity> temp_data);

            LootLockerAssetData assetData = new LootLockerAssetData
            {
                name = name,
                kv_storage = temp_kv.ToArray(),
                filters = temp_filters.ToArray(),
                data_entities = temp_data.ToArray(),
                context_id = context_id,
            };

            LootLockerCreatingOrUpdatingAnAssetCandidateRequest data = new LootLockerCreatingOrUpdatingAnAssetCandidateRequest
            {
                data = assetData,
                completed = isCompleted,
            };

            LootLockerGetRequest getRequest = new LootLockerGetRequest();
            getRequest.getRequests.Add(assetId.ToString());

            LootLockerAPIManager.UpdatingAnAssetCandidate(data, getRequest, onComplete);
        }

        public static void DeletingAnAssetCandidate(int assetId, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.DeletingAnAssetCandidate(data, onComplete);
        }

        public static void GettingASingleAssetCandidate(int assetId, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.GettingASingleAssetCandidate(data, onComplete);
        }

        public static void ListingAssetCandidates(Action<LootLockerListingAssetCandidatesResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.ListingAssetCandidates(onComplete);
        }

        public static void AddingFilesToAssetCandidates(int assetId, string filePath, string fileName,
            FilePurpose filePurpose, Action<LootLockerUserGenerateContentResponse> onComplete, string fileContentType = null)
        {
            if (!CheckInitialized()) return;

            LootLockerAddingFilesToAssetCandidatesRequest data = new LootLockerAddingFilesToAssetCandidatesRequest()
            {
                filePath = filePath,
                fileName = fileName,
                fileContentType = fileContentType,
                filePurpose = filePurpose.ToString()
            };

            LootLockerGetRequest getRequest = new LootLockerGetRequest();

            getRequest.getRequests.Add(assetId.ToString());

            LootLockerAPIManager.AddingFilesToAssetCandidates(data, getRequest, onComplete);
        }

        public static void RemovingFilesFromAssetCandidates(int assetId, int fileId, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            data.getRequests.Add(fileId.ToString());

            LootLockerAPIManager.RemovingFilesFromAssetCandidates(data, onComplete);
        }
        #endregion

        #region Events
        public static void GettingAllEvents(Action<LootLockerEventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GettingAllEvents(onComplete);
        }

        public static void GettingASingleEvent(int missionId, Action<LootLockerSingleEventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.GettingASingleEvent(data, onComplete);
        }

        public static void StartingEvent(int missionId, Action<LootLockerStartinEventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.StartingEvent(data, onComplete);
        }

        public static void FinishingEvent(int missionId, string signature, string finishTime, string finishScore, LootLockerCheckpointTimes[] checkpointsScores, Action<LootLockerFinishEventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerEventPayload payload = new LootLockerEventPayload { finish_score = finishScore, finish_time = finishTime };
            payload.checkpoint_times = checkpointsScores;
            FinishEventRequest data = new FinishEventRequest { signature = signature, payload = payload };
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.FinishingEvent(lootLockerGetRequest, data, onComplete);
        }

        #endregion

        #region Missions
        public static void GettingAllMissions(Action<LootLockerGettingAllMissionsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GettingAllMissions(onComplete);
        }

        public static void GettingASingleMission(int missionId, Action<LootLockerGettingASingleMissionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.GettingASingleMission(data, onComplete);
        }

        public static void StartingAMission(int missionId, Action<LootLockerStartingAMissionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.StartingAMission(data, onComplete);
        }

        public static void FinishingAMission(int missionId, string startingMissionSignature, string playerId,
            LootLockerFinishingPayload finishingPayload, Action<LootLockerFinishingAMissionResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            string source = JsonConvert.SerializeObject(finishingPayload) + startingMissionSignature + playerId;
            string hash;
            using (SHA1 sha1Hash = SHA1.Create())
            {
                byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
                byte[] hashBytes = sha1Hash.ComputeHash(sourceBytes);
                hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }

            LootLockerFinishingAMissionRequest data = new LootLockerFinishingAMissionRequest()
            {
                signature = hash,
                payload = finishingPayload
            };
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.FinishingAMission(data, onComplete);
        }
        #endregion

        #region Maps
        public static void GettingAllMaps(Action<LootLockerMapsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GettingAllMaps(onComplete);
        }
        #endregion

        #region Purchasing
        public static void NormalPurchaseCall(int asset_id, int variation_id, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerNormalPurchaseRequest data = new LootLockerNormalPurchaseRequest { asset_id = asset_id, variation_id = variation_id };
            List<LootLockerNormalPurchaseRequest> datas = new List<LootLockerNormalPurchaseRequest>();
            datas.Add(data);
            LootLockerAPIManager.NormalPurchaseCall(datas.ToArray(), onComplete);
        }

        public static void RentalPurchaseCall(int asset_id, int variation_id, int rental_option_id, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerRentalPurchaseRequest data = new LootLockerRentalPurchaseRequest { asset_id = asset_id, variation_id = variation_id, rental_option_id = rental_option_id };
            LootLockerAPIManager.RentalPurchaseCall(data, onComplete);
        }

        public static void IosPurchaseVerification(string receipt_data, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerIosPurchaseVerificationRequest[] data = new LootLockerIosPurchaseVerificationRequest[] { new LootLockerIosPurchaseVerificationRequest { receipt_data = receipt_data } };
            LootLockerAPIManager.IosPurchaseVerification(data, onComplete);
        }

        public static void AndroidPurchaseVerification(string purchase_token, int asset_id, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAndroidPurchaseVerificationRequest[] data = new LootLockerAndroidPurchaseVerificationRequest[] { new LootLockerAndroidPurchaseVerificationRequest { purchase_token = purchase_token, asset_id = asset_id } };

            LootLockerAPIManager.AndroidPurchaseVerification(data, onComplete);
        }

        public static void PollingOrderStatus(int assetId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.PollingOrderStatus(data, onComplete);
        }

        public static void ActivatingARentalAsset(int assetId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.ActivatingARentalAsset(data, onComplete);
        }
        #endregion

        #region Collectables
        public static void GettingCollectables(Action<LootLockerGettingCollectablesResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GettingCollectables(onComplete);
        }

        public static void CollectingAnItem(string slug, Action<LootLockerCollectingAnItemResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerCollectingAnItemRequest data = new LootLockerCollectingAnItemRequest();
            data.slug = slug;
            LootLockerAPIManager.CollectingAnItem(data, onComplete);
        }

        #endregion

        #region Messages

        public static void GetMessages(Action<LootLockerGetMessagesResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetMessages(onComplete);
        }

        #endregion

        #region Events
        public static void TriggeringAnEvent(string eventName, Action<LootLockerTriggerAnEventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerTriggerAnEventRequest data = new LootLockerTriggerAnEventRequest { name = eventName };
            LootLockerAPIManager.TriggeringAnEvent(data, onComplete);
        }

        public static void ListingTriggeredTriggerEvents(Action<LootLockerListingAllTriggersResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.ListingTriggeredTriggerEvents(onComplete);
        }

        #endregion

        #region Crashes
        public static void SubmittingACrashLog(string logFIlePath, string game_version, string type_identifier, string local_crash_time,
            Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerSubmittingACrashLogRequest data = new LootLockerSubmittingACrashLogRequest()
            {
                logFilePath = logFIlePath,
                game_version = game_version,
                type_identifier = type_identifier,
                local_crash_time = local_crash_time,
            };
            LootLockerAPIManager.SubmittingACrashLog(data, onComplete);
        }
        #endregion
    }

    public class ResponseError
    {
        public bool success;
        public string error;
        public string[] messages;
        public string error_id;
    }
}
