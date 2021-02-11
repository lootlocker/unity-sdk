using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Admin.Requests;
using LootLocker.Admin;
using LootLocker.Requests;


    namespace LootLocker.Admin.Requests
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

                LootLockerBaseServerAPI.activeConfig = LootLockerAdminConfig.Get();

                return LootLockerEndPointsAdmin.current != null;

            }

            public static void DebugMessage(string message, bool IsError = false)
            {
#if UNITY_EDITOR
#if UNITY_EDITOR
                if (LootLockerAdminConfig.current != null && LootLockerAdminConfig.current.currentDebugLevel == LootLockerGenericConfig.DebugLevel.All)
                {
                    if (IsError)
                        Debug.LogError(message);
                    else
                        Debug.Log(message);
                }
                else if (LootLockerAdminConfig.current != null && LootLockerAdminConfig.current.currentDebugLevel == LootLockerGenericConfig.DebugLevel.ErrorOnly)
                {
                    if (IsError)
                        Debug.LogError(message);
                }
                else if (LootLockerAdminConfig.current != null && LootLockerAdminConfig.current.currentDebugLevel == LootLockerGenericConfig.DebugLevel.NormalOnly)
                {
                    if (!IsError)
                        Debug.LogError(message);
                }
#endif

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
                    LootLockerBaseServerAPI.activeConfig = LootLockerAdminConfig.current;
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

            public static void InitialAuthRequest(string email, string password, Action<LootLockerAuthResponse> onComplete)
            {
                if (!CheckInitialized()) return;
                var data = new LootLockerInitialAuthRequest();
                data.email = email;
                data.password = password;
                LootLockerAPIManagerAdmin.InitialAuthenticationRequest(data, onComplete);
            }

            public static void TwoFactorAuthVerification(string mfa_key, string secret, Action<LootLockerAuthResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                var data = new LootLockerTwoFactorAuthVerficationRequest();
                data.mfa_key = mfa_key;
                data.secret = secret;
                LootLockerAPIManagerAdmin.TwoFactorAuthVerification(data, onComplete);

            }

            public static void SubsequentRequestsRequest(Action<LootLockerSubsequentRequestsResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                LootLockerAPIManagerAdmin.SubsequentRequests(onComplete);

            }

            #endregion

            #region Games

            public static void GetAllGamesToTheCurrentUser(Action<LootLockerGetAllGamesToTheCurrentUserResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                LootLockerAPIManagerAdmin.GetAllGamesToTheCurrentUser(onComplete);

            }

            public static void CreatingAGame(string name, string steam_app_id, bool sandbox_mode, int organisation_id, bool demo, Action<LootLockerCreatingAGameResponse> onComplete)
            {

                if (!CheckInitialized()) return;

                LootLockerCreatingAGameRequest data = new LootLockerCreatingAGameRequest
                {

                    name = name,
                    steam_app_id = steam_app_id,
                    sandbox_mode = sandbox_mode,
                    organisation_id = organisation_id,
                    demo = demo

                };

                LootLockerAPIManagerAdmin.CreatingAGame(data, onComplete);

            }

            public static void GetDetailedInformationAboutAGame(string id, Action<LootLockerCreatingAGameResponse> onComplete)
            {
                if (!CheckInitialized()) return;
                LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
                lootLockerGetRequest.getRequests.Add(id.ToString());
                LootLockerAPIManagerAdmin.GetDetailedInformationAboutAGame(lootLockerGetRequest, onComplete);
            }

            public static void UpdatingInformationAboutAGame(int gameIDToUpdateInfo, Dictionary<string, object> requestData, Action<LootLockerCreatingAGameResponse> onComplete)
            {
                if (!CheckInitialized()) return;

                LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

                lootLockerGetRequest.getRequests.Add(gameIDToUpdateInfo.ToString());

                LootLockerAPIManagerAdmin.UpdatingInformationAboutAGame(lootLockerGetRequest, requestData, onComplete);
            }

            public static void DeletingGames(int gameIDToDelete, Action<LootLockerDeletingGamesResponse> onComplete)
            {
                if (!CheckInitialized()) return;
                LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

                lootLockerGetRequest.getRequests.Add(gameIDToDelete.ToString());
                LootLockerAPIManagerAdmin.DeletingGames(lootLockerGetRequest, onComplete);

            }

            #endregion

            #region Players

            public static void SearchingForPlayers(int game_id, Action<LootLockerSearchingForPlayersResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                LootLockerGetRequest data = new LootLockerGetRequest();
                data.getRequests.Add(game_id.ToString());
                LootLockerAPIManagerAdmin.SearchingForPlayers(data, onComplete);

            }

            #endregion

            #region Maps

            public static void GettingAllMapsToAGame(int gameID, Action<LootLockerGettingAllMapsToAGameResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
                lootLockerGetRequest.getRequests.Add(gameID.ToString());
                LootLockerAPIManagerAdmin.GettingAllMapsToAGame(lootLockerGetRequest, onComplete);

            }

            public static void CreatingMaps(LootLockerCreatingMapsRequest request, bool sendAssetID, bool sendSpawnPoints, Action<LootLockerCreatingMapsResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                LootLockerAPIManagerAdmin.CreatingMaps(request, sendAssetID, sendSpawnPoints, onComplete);

            }

            public static void UpdatingMaps(LootLockerCreatingMapsRequest request, int mapID, Action<LootLockerCreatingMapsResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
                lootLockerGetRequest.getRequests.Add(mapID.ToString());
                LootLockerAPIManagerAdmin.UpdatingMaps(lootLockerGetRequest, request, onComplete);

            }


            #endregion

            #region Events

            public static void CreatingEvent(Dictionary<string, object> requestData, Action<LootLockerCreatingEventResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                LootLockerAPIManagerAdmin.CreatingEvent(requestData, onComplete);

            }

            public static void UpdatingEvent(int eventID, Dictionary<string, object> requestData, Action<LootLockerCreatingEventResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
                lootLockerGetRequest.getRequests.Add(eventID.ToString());
                LootLockerAPIManagerAdmin.UpdatingEvent(lootLockerGetRequest, requestData, onComplete);

            }

            public static void GettingAllEvents(int gameID, Action<LootLockerGettingAllEventsResponse> onComplete)
            {

                if (!CheckInitialized()) return;
                LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
                lootLockerGetRequest.getRequests.Add(gameID.ToString());
                LootLockerAPIManagerAdmin.GettingAllEvents(lootLockerGetRequest, onComplete);

            }

            #endregion

            #region Upload
            public static string UploadAFile(string filePath, string assetId, int gameId, Action<LootLockerUploadAFileResponse> onComplete, string[] tags = null)
            {
                if (!CheckInitialized()) throw new Exception("please initialize sdk first");
                return LootLockerAPIManagerAdmin.UploadAFile(filePath, assetId, gameId, onComplete, tags);
            }
            #endregion

            #region Assets

            public static void GetAssets(Action<LootLockerGetAssetsResponse> onComplete, string search = null)
            {
                if (!CheckInitialized()) return;

                LootLockerAPIManagerAdmin.GetAssets(onComplete, search);
            }

            public static void CreateAsset(LootLockerCreateAssetRequest request, Action<LootLockerCreateAssetResponse> onComplete)
            {
                if (!CheckInitialized()) return;

                LootLockerAPIManagerAdmin.CreateAsset(request, onComplete);
            }

            public static void GetContexts(Action<LootLockerGetContextsResponse> onComplete)
            {
                if (!CheckInitialized()) return;

                LootLockerAPIManagerAdmin.GetContexts(onComplete);
            }

            #endregion

            #region Triggers

            public static void ListTriggers(int game_id, Action<LootLockerListTriggersResponse> onComplete)
            {
                if (!CheckInitialized()) return;
                LootLockerGetRequest data = new LootLockerGetRequest();
                data.getRequests.Add(game_id.ToString());
                LootLockerAPIManagerAdmin.ListTriggers(data, onComplete);
            }

            public static void CreateTriggers(LootLockerCreateTriggersRequest requestData, int game_id, Action<LootLockerListTriggersResponse> onComplete)
            {
                if (!CheckInitialized()) return;
                LootLockerGetRequest data = new LootLockerGetRequest();
                data.getRequests.Add(game_id.ToString());
                LootLockerAPIManagerAdmin.CreateTriggers(requestData, data, onComplete);
            }

            #endregion

            #region Files
            public static void GetFiles(LootLocker.Admin.Requests.LootLockerFileFilterType filter, Action<LootLockerGetFilesResponse> onComplete)
            {
                if (!CheckInitialized()) return;

                LootLockerAPIManagerAdmin.GetFiles(filter, onComplete);
            }

            public static void DeleteFile(string fileId, Action<LootLockerDeleteFileResponse> onComplete)
            {
                if (!CheckInitialized()) return;

                LootLockerAPIManagerAdmin.DeleteFile(fileId, onComplete);
            }
            public static void UpdateFile(string fileId, LootLockerUpdateFileRequest request, Action<LootLockerUpdateFileResponse> onComplete)
            {
                if (!CheckInitialized()) return;

                LootLockerAPIManagerAdmin.UpdateFile(fileId, request, onComplete);
            }

            #endregion

            #region Organisations

            public static void GetUsersToAnOrganisation(int organisation_id, Action<LootLockerGetUsersToAnOrganisationResponse> onComplete)
            {
                if (!CheckInitialized()) return;
                LootLockerGetRequest data = new LootLockerGetRequest();
                data.getRequests.Add(organisation_id.ToString());
                LootLockerAPIManagerAdmin.GetUsersToAnOrganisation(data, onComplete);
            }

            #endregion

            #region User

            public static void SetupTwoFactorAuthentication(Action<LootLockerSetupTwoFactorAuthenticationResponse> onComplete)
            {
                if (!CheckInitialized()) return;
                LootLockerAPIManagerAdmin.SetupTwoFactorAuthentication(onComplete);
            }

            public static void VerifyTwoFactorAuthenticationSetup(int verify2FASecret, Action<LootLockerVerifyTwoFactorAuthenticationResponse> onComplete)
            {
                if (!CheckInitialized()) return;
                LootLockerVerifyTwoFactorAuthenticationRequest request = new LootLockerVerifyTwoFactorAuthenticationRequest { secret = verify2FASecret };
                LootLockerAPIManagerAdmin.VerifyTwoFactorAuthenticationSetup(request, onComplete);
            }

            public static void RemoveTwoFactorAuthentication(int remove2FASecret, Action<LootLockerRemoveTwoFactorAuthenticationResponse> onComplete)
            {
                if (!CheckInitialized()) return;
                LootLockerVerifyTwoFactorAuthenticationRequest request = new LootLockerVerifyTwoFactorAuthenticationRequest { secret = remove2FASecret };
                LootLockerAPIManagerAdmin.RemoveTwoFactorAuthentication(request, onComplete);
            }



            #endregion

            #endregion

        }
    }
