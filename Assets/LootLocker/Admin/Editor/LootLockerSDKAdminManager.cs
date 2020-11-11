using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerAdminRequests;
using LootLockerAdmin;

namespace LootLockerAdminRequests
{
    public class LootLockerSDKAdminManager
    {
        #region Init

        public static void Init()
        {
            AdminServerAPI.Init();
            initialized = LoadConfig();
            DebugMessage("SDK Intialised: " + initialized);
        }

        /// <summary>
        /// are configs and endpoints loaded 
        /// </summary>
        static bool initialized;

        static bool LoadConfig()
        {
            LootLockerSDKAdminManager.DebugMessage("Loading admin config..");

            if (LootLockerAdminConfig.current == null)
                LootLockerAdminConfig.current = Resources.Load("Config/LootLockerAdminConfig") as LootLockerAdminConfig;

            if (LootLockerEndPointsAdmin.current == null)
                LootLockerEndPointsAdmin.current = Resources.Load("Config/LootLockerEndPointsAdmin") as LootLockerEndPointsAdmin;

            BaseServerAPI.activeConfig = LootLockerAdminConfig.current;

            return LootLockerEndPointsAdmin.current != null;

        }

        public static void DebugMessage(string message, bool IsError = false)
        {
#if UNITY_EDITOR
            if (IsError)
                Debug.LogError(message);
            else
                Debug.Log(message);
#endif
        }

        public static bool CheckInitialized()
        {
            if (!initialized)
            {
                Init();
                return true;
            }

            try
            {
                BaseServerAPI.activeConfig = LootLockerAdminConfig.current;
            }
            catch (Exception ex)
            {

                LootLockerSDKAdminManager.DebugMessage("Couldn't change activeConfig on ServerAPI to Admin config. " + ex, true);

            }

            return true;
        }

        #endregion

        #region Admin

        #region Authentication

        public static void InitialAuthRequest(string email, string password, Action<AuthResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            var data = new InitialAuthRequest();
            data.email = email;
            data.password = password;
            LootLockerAPIManagerAdmin.InitialAuthenticationRequest(data, onComplete);
        }

        public static void TwoFactorAuthVerification(string mfa_key, string secret, Action<AuthResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            var data = new TwoFactorAuthVerficationRequest();
            data.mfa_key = mfa_key;
            data.secret = secret;
            LootLockerAPIManagerAdmin.TwoFactorAuthVerification(data, onComplete);

        }

        public static void SubsequentRequestsRequest(Action<SubsequentRequestsResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerAPIManagerAdmin.SubsequentRequests(onComplete);

        }

        #endregion

        #region Games

        public static void GetAllGamesToTheCurrentUser(Action<GetAllGamesToTheCurrentUserResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerAPIManagerAdmin.GetAllGamesToTheCurrentUser(onComplete);

        }

        public static void CreatingAGame(string name, string steam_app_id, bool sandbox_mode, int organisation_id, bool demo, Action<CreatingAGameResponse> onComplete)
        {

            if (!CheckInitialized()) return;

            CreatingAGameRequest data = new CreatingAGameRequest
            {

                name = name,
                steam_app_id = steam_app_id,
                sandbox_mode = sandbox_mode,
                organisation_id = organisation_id,
                demo = demo

            };

            LootLockerAPIManagerAdmin.CreatingAGame(data, onComplete);

        }

        public static void GetDetailedInformationAboutAGame(string id, Action<CreatingAGameResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(id.ToString());
            LootLockerAPIManagerAdmin.GetDetailedInformationAboutAGame(lootLockerGetRequest, onComplete);
        }

        public static void UpdatingInformationAboutAGame(int gameIDToUpdateInfo, Dictionary<string, object> requestData, Action<CreatingAGameResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(gameIDToUpdateInfo.ToString());

            LootLockerAPIManagerAdmin.UpdatingInformationAboutAGame(lootLockerGetRequest, requestData, onComplete);
        }

        public static void DeletingGames(int gameIDToDelete, Action<DeletingGamesResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(gameIDToDelete.ToString());
            LootLockerAPIManagerAdmin.DeletingGames(lootLockerGetRequest, onComplete);

        }

        #endregion

        #region Players

        public static void SearchingForPlayers(int game_id, Action<SearchingForPlayersResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(game_id.ToString());
            LootLockerAPIManagerAdmin.SearchingForPlayers(data, onComplete);

        }

        #endregion

        #region Maps

        public static void GettingAllMapsToAGame(int gameID, Action<GettingAllMapsToAGameResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(gameID.ToString());
            LootLockerAPIManagerAdmin.GettingAllMapsToAGame(lootLockerGetRequest, onComplete);

        }

        public static void CreatingMaps(CreatingMapsRequest request, bool sendAssetID, bool sendSpawnPoints, Action<CreatingMapsResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerAPIManagerAdmin.CreatingMaps(request, sendAssetID, sendSpawnPoints, onComplete);

        }

        public static void UpdatingMaps(CreatingMapsRequest request, int mapID, Action<CreatingMapsResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(mapID.ToString());
            LootLockerAPIManagerAdmin.UpdatingMaps(lootLockerGetRequest, request, onComplete);

        }


        #endregion

        #region Events

        public static void CreatingEvent(Dictionary<string, object> requestData, Action<CreatingEventResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerAPIManagerAdmin.CreatingEvent(requestData, onComplete);

        }

        public static void UpdatingEvent(int eventID, Dictionary<string, object> requestData, Action<CreatingEventResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(eventID.ToString());
            LootLockerAPIManagerAdmin.UpdatingEvent(lootLockerGetRequest, requestData, onComplete);

        }

        public static void GettingAllEvents(int gameID, Action<GettingAllEventsResponse> onComplete)
        {

            if (!CheckInitialized()) return;
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(gameID.ToString());
            LootLockerAPIManagerAdmin.GettingAllEvents(lootLockerGetRequest, onComplete);

        }

        #endregion

        #region Upload
        public static string UploadAFile(string filePath, string assetId, int gameId, Action<UploadAFileResponse> onComplete, string[] tags = null)
        {
            if (!CheckInitialized()) throw new Exception("please initialize sdk first");
            return LootLockerAPIManagerAdmin.UploadAFile(filePath, assetId, gameId, onComplete, tags);
        }
        #endregion

        #region Assets

        public static void GetAssets(Action<GetAssetsResponse> onComplete, string search = null)
        {
            if (!CheckInitialized()) return;

            LootLockerAPIManagerAdmin.GetAssets(onComplete, search);
        }

        public static void CreateAsset(CreateAssetRequest request, Action<CreateAssetResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            LootLockerAPIManagerAdmin.CreateAsset(request, onComplete);
        }

        public static void GetContexts(Action<GetContextsResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            LootLockerAPIManagerAdmin.GetContexts(onComplete);
        }

        #endregion

        #region Triggers

        public static void ListTriggers(int game_id, Action<ListTriggersResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(game_id.ToString());
            LootLockerAPIManagerAdmin.ListTriggers(data, onComplete);
        }

        public static void CreateTriggers(CreateTriggersRequest requestData, int game_id, Action<ListTriggersResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(game_id.ToString());
            LootLockerAPIManagerAdmin.CreateTriggers(requestData, data, onComplete);
        }

        #endregion

        #region Files
        public static void GetFiles(LootLockerAdminRequests.FileFilterType filter, Action<GetFilesResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            LootLockerAPIManagerAdmin.GetFiles(filter, onComplete);
        }

        public static void DeleteFile(string fileId, Action<DeleteFileResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            LootLockerAPIManagerAdmin.DeleteFile(fileId, onComplete);
        }
        public static void UpdateFile(string fileId, UpdateFileRequest request, Action<UpdateFileResponse> onComplete)
        {
            if (!CheckInitialized()) return;

            LootLockerAPIManagerAdmin.UpdateFile(fileId, request, onComplete);
        }

        #endregion

        #region Organisations

        public static void GetUsersToAnOrganisation(int organisation_id, Action<GetUsersToAnOrganisationResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(organisation_id.ToString());
            LootLockerAPIManagerAdmin.GetUsersToAnOrganisation(data, onComplete);
        }

        #endregion

        #region User

        public static void SetupTwoFactorAuthentication(Action<SetupTwoFactorAuthenticationResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            LootLockerAPIManagerAdmin.SetupTwoFactorAuthentication(onComplete);
        }

        public static void VerifyTwoFactorAuthenticationSetup(int verify2FASecret, Action<VerifyTwoFactorAuthenticationResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            VerifyTwoFactorAuthenticationRequest request = new VerifyTwoFactorAuthenticationRequest { secret = verify2FASecret };
            LootLockerAPIManagerAdmin.VerifyTwoFactorAuthenticationSetup(request, onComplete);
        }

        public static void RemoveTwoFactorAuthentication(int remove2FASecret, Action<RemoveTwoFactorAuthenticationResponse> onComplete)
        {
            if (!CheckInitialized()) return;
            VerifyTwoFactorAuthenticationRequest request = new VerifyTwoFactorAuthenticationRequest { secret = remove2FASecret };
            LootLockerAPIManagerAdmin.RemoveTwoFactorAuthentication(request, onComplete);
        }



        #endregion

        #endregion

    }
}
