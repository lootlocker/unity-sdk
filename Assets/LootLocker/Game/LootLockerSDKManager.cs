using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLockerRequests;
using LootLocker;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using enums;

namespace LootLockerRequests
{
    public partial class LootLockerSDKManager
    {
        #region Init

        static bool initialized;
        public static bool Init()
        {
            DebugMessage("SDK is Intializing");
            ServerManager.CheckInit();
            return LoadConfig();
        }

        static bool LoadConfig()
        {
            if (LootLockerConfig.current == null)
                LootLockerConfig.current = Resources.Load("Config/LootLockerConfig") as LootLockerConfig;
            if (LootLockerEndPoints.current == null)
                LootLockerEndPoints.current = Resources.Load("Config/LootLockerEndPoints") as LootLockerEndPoints;
            BaseServerAPI.activeConfig = LootLockerConfig.current;

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
                BaseServerAPI.activeConfig = LootLockerConfig.current;
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
            if (IsError)
                Debug.LogError(message);
            else
                Debug.Log(message);
#endif
        }

        #endregion

        #region Authentication
        public static void VerifySteamID(string steamSessionTicket, Action<VerifyResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            VerifyRequest verifyRequest = new VerifyRequest(steamSessionTicket);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }

        public static void VerifyID(string deviceId, Action<VerifyResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            VerifyRequest verifyRequest = new VerifyRequest(deviceId);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }

        public static void StartSession(string deviceId, Action<SessionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            SessionRequest sessionRequest = new SessionRequest(deviceId);
            LootLockerAPIManager.Session(sessionRequest, onComplete);
        }
        public static void StartSteamSession(string steamId64, Action<SessionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            SessionRequest sessionRequest = new SessionRequest(steamId64);
            LootLockerAPIManager.Session(sessionRequest, onComplete);
        }

        public static void EndSession(string deviceId, Action<SessionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            SessionRequest sessionRequest = new SessionRequest(deviceId);
            LootLockerAPIManager.EndSession(sessionRequest, onComplete);
        }
        #endregion

        #region Player
        //Player calls
        public static void GetPlayerInfo(Action<GetPlayerInfoResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetPlayerInfo(onComplete);
        }

        public static void GetInventory(Action<InventoryResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetInventory(onComplete);
        }

        public static void GetBalance(Action<BalanceResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetBalance(onComplete);
        }

        public static void SubmitXp(int xpToSubmit, Action<XpSubmitResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            XpSubmitRequest xpSubmitRequest = new XpSubmitRequest(xpToSubmit);
            LootLockerAPIManager.SubmitXp(xpSubmitRequest, onComplete);
        }

        public static void GetXpAndLevel(Action<XpResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            XpRequest xpRequest = new XpRequest();
            LootLockerAPIManager.GetXpAndLevel(xpRequest, onComplete);
        }

        public static void GetAssetNotification(Action<PlayerAssetNotificationsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetPlayerAssetNotification(onComplete);
        }

        public static void GetDeactivatedAssetNotification(Action<DeactivatedAssetsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetDeactivatedAssetNotification(onComplete);
        }

        public static void InitiateDLCMigration(Action<DlcResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.InitiateDLCMigration(onComplete);
        }

        public static void GetDLCMigrated(Action<DlcResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetDLCMigrated(onComplete);
        }

        public static void SetProfilePrivate(Action<StandardResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.SetProfilePrivate(onComplete);
        }

        public static void SetProfilePublic(Action<StandardResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.SetProfilePublic(onComplete);
        }
        #endregion

        #region Character
        public static void GetCharacterLoadout(Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetCharacterLoadout(onComplete);
        }

        public static void GetOtherPlayersCharacterLoadout(string characterID, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();

            data.getRequests.Add(characterID);
            data.getRequests.Add(LootLockerConfig.current.platform.ToString());
            LootLockerAPIManager.GetOtherPlayersCharacterLoadout(data, onComplete);
        }

        public static void UpdateCharacter(string characterID, string newCharacterName, bool isDefault, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            UpdateCharacterRequest data = new UpdateCharacterRequest();

            data.name = newCharacterName;
            data.is_default = isDefault;

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(characterID);

            LootLockerAPIManager.UpdateCharacter(lootLockerGetRequest, data, onComplete);
        }

        public static void EquipIdAssetToDefaultCharacter(string assetInstanceId, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            EquipByIDRequest data = new EquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceId);
            LootLockerAPIManager.EquipIdAssetToDefaultCharacter(data, onComplete);
        }

        public static void EquipGlobalAssetToDefaultCharacter(string assetId, string assetVariationId, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            EquipByAssetRequest data = new EquipByAssetRequest();
            data.asset_id = int.Parse(assetId);
            data.asset_variation_id = int.Parse(assetVariationId);
            LootLockerAPIManager.EquipGlobalAssetToDefaultCharacter(data, onComplete);
        }

        public static void EquipIdAssetToCharacter(string characterID, string assetInstanceId, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            EquipByIDRequest data = new EquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceId);

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID);
            LootLockerAPIManager.EquipIdAssetToCharacter(lootLockerGetRequest, data, onComplete);
        }

        public static void EquipGlobalAssetToCharacter(string assetId, string assetVariationId, string characterID, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            EquipByAssetRequest data = new EquipByAssetRequest();
            data.asset_id = int.Parse(assetId);
            data.asset_variation_id = int.Parse(assetVariationId);
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID);
            LootLockerAPIManager.EquipGlobalAssetToCharacter(lootLockerGetRequest, data, onComplete);
        }

        public static void UnEquipIdAssetToDefaultCharacter(string assetId, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(assetId);
            LootLockerAPIManager.UnEquipIdAssetToDefaultCharacter(lootLockerGetRequest, onComplete);
        }

        public static void UnEquipIdAssetToCharacter(string assetId, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(assetId);
            LootLockerAPIManager.UnEquipIdAssetToCharacter(lootLockerGetRequest, onComplete);
        }

        public static void GetCurrentLoadOutToDefaultCharacter(Action<GetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetCurrentLoadOutToDefaultCharacter(onComplete);
        }

        public static void GetCurrentLoadOutToOtherCharacter(string characterID, Action<GetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID);
            lootLockerGetRequest.getRequests.Add(LootLockerConfig.current.platform.ToString());
            LootLockerAPIManager.GetCurrentLoadOutToOtherCharacter(lootLockerGetRequest, onComplete);
        }

        public static void GetEquipableContextToDefaultCharacter(Action<ContextResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetEquipableContextToDefaultCharacter(onComplete);
        }
        #endregion

        #region PlayerStorage
        public static void GetEntirePersistentStorage(Action<GetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetEntirePersistentStorage(onComplete);
        }

        public static void GetSingleKeyPersistentStorage(Action<GetPersistentSingle> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetSingleKeyPersistentStorage(onComplete);
        }

        public static void UpdateOrCreateKeyValue(string key, string value, Action<GetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            GetPersistentStoragRequest data = new GetPersistentStoragRequest();
            data.AddToPayload(new Payload { key = key, value = value });
            LootLockerAPIManager.UpdateOrCreateKeyValue(data, onComplete);
        }

        public static void UpdateOrCreateKeyValue(GetPersistentStoragRequest data, Action<GetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.UpdateOrCreateKeyValue(data, onComplete);
        }

        public static void DeleteKeyValue(string keyToDelete, Action<GetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(keyToDelete);
            LootLockerAPIManager.DeleteKeyValue(data, onComplete);
        }

        public static void GetOtherPlayersPublicKeyValuePairs(string otherPlayerId, Action<GetPersistentStoragResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(otherPlayerId);
            LootLockerAPIManager.GetOtherPlayersPublicKeyValuePairs(data, onComplete);
        }
        #endregion

        #region Assets
        public static void GetContext(Action<ContextResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetContext(onComplete);
        }

        public static void GetAssetListWithCount(int assetCount, Action<AssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetCount.ToString());
            LootLockerAPIManager.GetAssetListWithCount(data, onComplete);// GetContext(LootLockerGetRequest data, Action<ContextResponse> onComplete)
        }

        public static void GetAssetNextList(int assetCount, Action<AssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            if (AssetRequest.lastId != 0)
            {
                AssetRequest data = new AssetRequest();
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
            AssetRequest.lastId = 0;
        }

        public static void GetAssetInformation(string assetId, Action<AssetInformationResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.GetAssetInformation(data, onComplete);
        }

        public static void ListFavouriteAssets(Action<FavouritesListResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.ListFavouriteAssets(onComplete);
        }

        public static void AddFavouriteAsset(string assetId, Action<AssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.AddFavouriteAsset(data, onComplete);
        }

        public static void RemoveFavouriteAsset(string assetId, Action<AssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.RemoveFavouriteAsset(data, onComplete);
        }

        public static void GetAssetsById(string[] assetIdsToRetrieve, Action<AssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();

            for (int i = 0; i < assetIdsToRetrieve.Length; i++) 
            data.getRequests.Add(assetIdsToRetrieve[i]);

            LootLockerAPIManager.RemoveFavouriteAsset(data, onComplete);
        }

        #endregion

        #region AssetInstance
        public static void GetAllKeyValuePairsForAssetInstances(Action<GetAllKeyValuePairsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetAllKeyValuePairs(onComplete);
        }

        public static void GetAllKeyValuePairsToAnInstance(int instanceId, Action<AssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(instanceId.ToString());
            LootLockerAPIManager.GetAllKeyValuePairsToAnInstance(data, onComplete);
        }

        public static void GetAKeyValuePairByIdForAssetInstances(int assetId, int instanceId, Action<AssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            data.getRequests.Add(instanceId.ToString());
            LootLockerAPIManager.GetAKeyValuePairById(data, onComplete);
        }

        public static void CreateKeyValuePairForAssetInstances(int assetId, string key, string value, Action<AssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            CreateKeyValuePairRequest createKeyValuePairRequest = new CreateKeyValuePairRequest();
            createKeyValuePairRequest.key = key;
            createKeyValuePairRequest.value = value;
            LootLockerAPIManager.CreateKeyValuePair(data, createKeyValuePairRequest, onComplete);
        }

        public static void UpdateOneOrMoreKeyValuePairForAssetInstances(int assetId, Dictionary<string, string> data, Action<AssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest request = new LootLockerGetRequest();
            request.getRequests.Add(assetId.ToString());
            UpdateOneOrMoreKeyValuePairRequest createKeyValuePairRequest = new UpdateOneOrMoreKeyValuePairRequest();
            List<CreateKeyValuePairRequest> temp = new List<CreateKeyValuePairRequest>();
            foreach (var d in data)
            {
                temp.Add(new CreateKeyValuePairRequest { key = d.Key, value = d.Value });
            }
            createKeyValuePairRequest.storage = temp.ToArray();
            LootLockerAPIManager.UpdateOneOrMoreKeyValuePair(request, createKeyValuePairRequest, onComplete);
        }

        public static void UpdateKeyValuePairByIdForAssetInstances(int assetId, string key, string value, Action<AssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            CreateKeyValuePairRequest createKeyValuePairRequest = new CreateKeyValuePairRequest();
            createKeyValuePairRequest.key = key;
            createKeyValuePairRequest.value = value;
            LootLockerAPIManager.UpdateKeyValuePairById(data, createKeyValuePairRequest, onComplete);
        }

        public static void DeleteKeyValuePairForAssetInstances(int assetId, int instanceId, Action<AssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            data.getRequests.Add(instanceId.ToString());
            LootLockerAPIManager.DeleteKeyValuePair(data, onComplete);
        }

        public static void InspectALootBoxForAssetInstances(int assetId, Action<InspectALootBoxResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.InspectALootBox(data, onComplete);
        }

        public static void OpenALootBoxForAssetInstances(int assetId, Action<OpenLootBoxResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.OpenALootBox(data, onComplete);
        }
        #endregion

        #region UserGeneratedContent
        private static void ConvertAssetDictionaries(Dictionary<string, string> kv_storage, Dictionary<string, string> filters,
            Dictionary<string, string> data_entities, out List<AssetKVPair> temp_kv, out List<AssetKVPair> temp_filters, out List<DataEntity> temp_data)
        {
            temp_kv = new List<AssetKVPair>();
            if (kv_storage != null)
            {
                foreach (var d in kv_storage)
                {
                    temp_kv.Add(new AssetKVPair { key = d.Key, value = d.Value });
                }
            }

            temp_filters = new List<AssetKVPair>();
            if (filters != null)
            {
                foreach (var d in filters)
                {
                    temp_filters.Add(new AssetKVPair { key = d.Key, value = d.Value });
                }
            }

            temp_data = new List<DataEntity>();
            if (data_entities != null)
            {
                foreach (var d in data_entities)
                {
                    temp_data.Add(new DataEntity { name = d.Key, data = d.Value });
                }
            }
        }

        public static void CreatingAnAssetCandidate(string name, Action<UserGenerateContentResponse> onComplete,
            Dictionary<string, string> kv_storage = null, Dictionary<string, string> filters = null,
            Dictionary<string, string> data_entities = null, int context_id = -1)
        {
            if (!CheckInitialized()) return;

            ConvertAssetDictionaries(kv_storage, filters, data_entities,
                out List<AssetKVPair> temp_kv, out List<AssetKVPair> temp_filters, out List<DataEntity> temp_data);

            AssetData assetData = new AssetData
            {
                name = name,
                kv_storage = temp_kv.ToArray(),
                filters = temp_filters.ToArray(),
                data_entities = temp_data.ToArray(),
                context_id = context_id,
            };

            CreatingOrUpdatingAnAssetCandidateRequest data = new CreatingOrUpdatingAnAssetCandidateRequest
            {
                data = assetData,
            };

            LootLockerAPIManager.CreatingAnAssetCandidate(data, onComplete);
        }

        public static void UpdatingAnAssetCandidate(int assetId, bool isCompleted, Action<UserGenerateContentResponse> onComplete,
            string name = null, Dictionary<string, string> kv_storage = null, Dictionary<string, string> filters = null,
            Dictionary<string, string> data_entities = null, int context_id = -1)
        {
            if (!CheckInitialized()) return;

            ConvertAssetDictionaries(kv_storage, filters, data_entities,
                out List<AssetKVPair> temp_kv, out List<AssetKVPair> temp_filters, out List<DataEntity> temp_data);

            AssetData assetData = new AssetData
            {
                name = name,
                kv_storage = temp_kv.ToArray(),
                filters = temp_filters.ToArray(),
                data_entities = temp_data.ToArray(),
                context_id = context_id,
            };

            CreatingOrUpdatingAnAssetCandidateRequest data = new CreatingOrUpdatingAnAssetCandidateRequest
            {
                data = assetData,
                completed = isCompleted,
            };

            LootLockerGetRequest getRequest = new LootLockerGetRequest();
            getRequest.getRequests.Add(assetId.ToString());

            LootLockerAPIManager.UpdatingAnAssetCandidate(data, getRequest, onComplete);
        }

        public static void DeletingAnAssetCandidate(int assetId, Action<UserGenerateContentResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.DeletingAnAssetCandidate(data, onComplete);
        }

        public static void ListingAssetCandidates(Action<ListingAssetCandidatesResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.ListingAssetCandidates(onComplete);
        }

        public static void AddingFilesToAssetCandidates(int assetId, string filePath, string fileName,
            FilePurpose filePurpose, Action<UserGenerateContentResponse> onComplete, string fileContentType = null)
        {
            if (!CheckInitialized()) return;

            AddingFilesToAssetCandidatesRequest data = new AddingFilesToAssetCandidatesRequest()
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

        public static void RemovingFilesFromAssetCandidates(int assetId, int fileId, Action<UserGenerateContentResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            data.getRequests.Add(fileId.ToString());

            LootLockerAPIManager.RemovingFilesFromAssetCandidates(data, onComplete);
        }
        #endregion

        #region Events
        public static void GettingAllEvents(Action<EventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GettingAllEvents(onComplete);
        }

        public static void GettingASingleEvent(int missionId, Action<SingleEventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.GettingASingleEvent(data, onComplete);
        }

        public static void StartingEvent(int missionId, Action<StartinEventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.StartingEvent(data, onComplete);
        }

        public static void FinishingEvent(int missionId, string signature, string finishTime, string finishScore, CheckpointTimes[] checkpointsScores, Action<FinishEventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            EventPayload payload = new EventPayload { finish_score = finishScore, finish_time = finishTime };
            payload.checkpoint_times = checkpointsScores;
            FinishEventRequest data = new FinishEventRequest { signature = signature, payload = payload };
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.FinishingEvent(lootLockerGetRequest, data, onComplete);
        }

        #endregion

        #region Missions
        public static void GettingAllMissions(Action<GettingAllMissionsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GettingAllMissions(onComplete);
        }

        public static void GettingASingleMission(int missionId, Action<GettingASingleMissionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.GettingASingleMission(data, onComplete);
        }

        public static void StartingAMission(int missionId, Action<StartingAMissionResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.StartingAMission(data, onComplete);
        }

        public static void FinishingAMission(int missionId, string startingMissionSignature, string playerId,
            FinishingPayload finishingPayload, Action<FinishingAMissionResponse> onComplete)
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

            FinishingAMissionRequest data = new FinishingAMissionRequest()
            {
                signature = hash,
                payload = finishingPayload
            };
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.FinishingAMission(data, onComplete);
        }
        #endregion

        #region Maps
        public static void GettingAllMaps(Action<MapsResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GettingAllMaps(onComplete);
        }
        #endregion

        #region Purchasing
        public static void NormalPurchaseCall(int asset_id, int variation_id, Action<PurchaseResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            NormalPurchaseRequest data = new NormalPurchaseRequest { asset_id = asset_id, variation_id = variation_id };
            List<NormalPurchaseRequest> datas = new List<NormalPurchaseRequest>();
            datas.Add(data);
            LootLockerAPIManager.NormalPurchaseCall(datas.ToArray(), onComplete);
        }

        public static void RentalPurchaseCall(int asset_id, int variation_id, int rental_option_id, Action<PurchaseResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            RentalPurchaseRequest data = new RentalPurchaseRequest { asset_id = asset_id, variation_id = variation_id, rental_option_id = rental_option_id };
            LootLockerAPIManager.RentalPurchaseCall(data, onComplete);
        }

        public static void IosPurchaseVerification(string receipt_data, Action<PurchaseResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            IosPurchaseVerificationRequest[] data = new IosPurchaseVerificationRequest[] { new IosPurchaseVerificationRequest { receipt_data = receipt_data } };
            LootLockerAPIManager.IosPurchaseVerification(data, onComplete);
        }

        public static void AndroidPurchaseVerification(string purchase_token, int asset_id, Action<PurchaseResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            AndroidPurchaseVerificationRequest[] data = new AndroidPurchaseVerificationRequest[] { new AndroidPurchaseVerificationRequest { purchase_token = purchase_token, asset_id = asset_id } };

            LootLockerAPIManager.AndroidPurchaseVerification(data, onComplete);
        }

        public static void PollingOrderStatus(int assetId, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.PollingOrderStatus(data, onComplete);
        }

        public static void ActivatingARentalAsset(int assetId, Action<CharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.ActivatingARentalAsset(data, onComplete);
        }
        #endregion

        #region Collectables
        public static void GettingCollectables(Action<GettingCollectablesResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GettingCollectables(onComplete);
        }

        public static void CollectingAnItem(string slug, Action<CollectingAnItemResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            CollectingAnItemRequest data = new CollectingAnItemRequest();
            data.slug = slug;
            LootLockerAPIManager.CollectingAnItem(data, onComplete);
        }

        #endregion

        #region Messages

        public static void GetMessages(Action<GetMessagesResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManager.GetMessages(onComplete);
        }

        #endregion

        #region Events
        public static void TriggeringAnEvent(string eventName, Action<TriggerAnEventResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            TriggerAnEventRequest data = new TriggerAnEventRequest { name = eventName };
            LootLockerAPIManager.TriggeringAnEvent(data, onComplete);
        }

        public static void ListingTriggeredTriggerEvents(Action<ListingAllTriggersResponse> onComplete)
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
            SubmittingACrashLogRequest data = new SubmittingACrashLogRequest()
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