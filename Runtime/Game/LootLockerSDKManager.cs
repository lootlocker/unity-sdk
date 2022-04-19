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
using static LootLocker.LootLockerConfig;
using System.Linq;

namespace LootLocker.Requests
{
    public partial class LootLockerSDKManager
    {
        /// <summary>
        /// Stores which platform the player currently has a session for.
        /// </summary>
        static string CurrentPlatform;

        public static string GetCurrentPlatform()
        {
            return CurrentPlatform;
        }

        #region Init

        static bool initialized;
        static bool Init()
        {
            DebugMessage("SDK is Intializing");
            LootLockerServerManager.CheckInit();
            return LoadConfig();
        }

        public static bool Init(string apiKey, string gameVersion, platformType platform, bool onDevelopmentMode, string domainKey)
        {
            DebugMessage("SDK is Intializing");
            LootLockerServerManager.CheckInit();
            return LootLockerConfig.CreateNewSettings(apiKey, gameVersion, platform, onDevelopmentMode, domainKey);
        }

        static bool LoadConfig()
        {
            initialized = true;
            if (LootLockerConfig.current == null)
            {
                Debug.LogError("SDK could not find settings, please contact support \n You can also set config manually by calling init");
                return false;
            }
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

            return true;
        }

        public static void DebugMessage(string message, bool IsError = false)
        {
#if     UNITY_EDITOR
            if (LootLockerConfig.current == null)
            {
                if (IsError)
                    Debug.LogError(message);
                else
                    Debug.Log(message);
                return;
            }

            if (LootLockerConfig.current != null && LootLockerConfig.current.currentDebugLevel == LootLockerConfig.DebugLevel.All)
            {
                if (IsError)
                    Debug.LogError(message);
                else
                    Debug.Log(message);
            }
            else if (LootLockerConfig.current.currentDebugLevel == LootLockerConfig.DebugLevel.ErrorOnly)
            {
                if (IsError)
                    Debug.LogError(message);
            }
            else if (LootLockerConfig.current.currentDebugLevel == LootLockerConfig.DebugLevel.NormalOnly)
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
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerVerifyResponse>());
                return;
            }
            LootLockerVerifySteamRequest verifyRequest = new LootLockerVerifySteamRequest(steamSessionTicket);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }

        public static string SteamSessionTicket(ref byte[] ticket, uint ticketSize)
        {
            Array.Resize(ref ticket, (int)ticketSize);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ticketSize; i++)
            {
                sb.AppendFormat("{0:x2}", ticket[i]);
            }
            return sb.ToString();
        }

        public static void VerifyID(string deviceId, Action<LootLockerVerifyResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerVerifyResponse>());
            }
            LootLockerVerifyRequest verifyRequest = new LootLockerVerifyRequest(deviceId);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }

        public static void StartSession(string deviceId, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            CurrentPlatform = LootLockerConfig.current.platform.ToString();

            LootLockerConfig.current.deviceID = deviceId;
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(deviceId);
            LootLockerAPIManager.Session(sessionRequest, onComplete);
        }

        public static void StartGuestSession(Action<LootLockerGuestSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGuestSessionResponse>());
                return;
            }

            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest();
            string existingPlayerID = PlayerPrefs.GetString("LootLockerGuestPlayerID", "");
            if (existingPlayerID != "")
            {
                sessionRequest = new LootLockerSessionRequest(existingPlayerID);
            }

            LootLockerAPIManager.GuestSession(sessionRequest, response =>
            {
                CurrentPlatform = "guest";

                PlayerPrefs.SetString("LootLockerGuestPlayerID", response.player_identifier);
                PlayerPrefs.Save();

                onComplete(response);
            });
        }

        public static void StartGuestSession(string identifier, Action<LootLockerGuestSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGuestSessionResponse>());
                return;
            }

            if (identifier.Length == 0)
            {
                onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerGuestSessionResponse>("identifier cannot be empty"));
                return;
            }

            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(identifier);

            LootLockerAPIManager.GuestSession(sessionRequest, response =>
            {
                CurrentPlatform = "guest";

                PlayerPrefs.SetString("LootLockerGuestPlayerID", response.player_identifier);
                PlayerPrefs.Save();

                onComplete(response);
            });
        }

        public static void StartSteamSession(string steamId64, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            CurrentPlatform = "steam";

            LootLockerSteamSessionRequest sessionRequest = new LootLockerSteamSessionRequest(steamId64);
            LootLockerAPIManager.Session(sessionRequest, onComplete);
        }

        /// <summary>
        /// Checks if we have a stored session and also if that session is valid.
        /// 
        /// Depending on response of this method the developer can either start a session using the token,
        /// or show a login form.
        /// 
        /// White label platform must be enabled in the web console for this to work.
        /// </summary>
        public static void CheckWhiteLabelSession(Action<bool> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete(false);
                return;
            }

            string existingSessionToken = PlayerPrefs.GetString("LootLockerWhiteLabelSessionToken", "");
            if (existingSessionToken == "")
            {
                onComplete(false);
                return;
            }

            string existingSessionEmail = PlayerPrefs.GetString("LootLockerWhiteLabelSessionEmail", "");
            if (existingSessionEmail == "")
            {
                onComplete(false);
                return;
            }

            LootLockerWhiteLabelVerifySessionRequest sessionRequest = new LootLockerWhiteLabelVerifySessionRequest();
            sessionRequest.email = existingSessionEmail;
            sessionRequest.token = existingSessionToken;

            LootLockerAPIManager.WhiteLabelVerifySession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    onComplete(false);
                    return;
                }

                onComplete(true);
            });
        }

        public static void CheckWhiteLabelSession(string email, string token, Action<bool> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete(false);
                return;
            }

            LootLockerWhiteLabelVerifySessionRequest sessionRequest = new LootLockerWhiteLabelVerifySessionRequest();
            sessionRequest.email = email;
            sessionRequest.token = token;

            LootLockerAPIManager.WhiteLabelVerifySession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    onComplete(false);
                    return;
                }

                onComplete(true);
            });
        }

        /// <summary>
        /// Create new user using the white label login system.
        /// 
        /// White label platform must be enabled in the web console for this to work.
        /// </summary>
        public static void StartWhiteLabelSession(string email, string password, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }
            LootLockerWhiteLabelSessionRequest sessionRequest = new LootLockerWhiteLabelSessionRequest(email, password);
            CurrentPlatform = "white_label";
            LootLockerAPIManager.WhiteLabelSession(sessionRequest, onComplete);
        }

        public static void StartWhiteLabelSession(LootLockerWhiteLabelSessionRequest input, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }
            LootLockerWhiteLabelSessionRequest sessionRequest = new LootLockerWhiteLabelSessionRequest(input.email);
            sessionRequest.token = input.token;
            CurrentPlatform = "white_label";
            LootLockerAPIManager.WhiteLabelSession(sessionRequest, onComplete);
        }

        public static void StartWhiteLabelSession(Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            string existingSessionToken = PlayerPrefs.GetString("LootLockerWhiteLabelSessionToken", "");
            if (existingSessionToken == "")
            {
                onComplete(LootLockerResponseFactory.Error<LootLockerSessionResponse>("no session token found"));
                return;
            }

            string existingSessionEmail = PlayerPrefs.GetString("LootLockerWhiteLabelSessionEmail", "");
            if (existingSessionEmail == "")
            {
                onComplete(LootLockerResponseFactory.Error<LootLockerSessionResponse>("no session email found"));
                return;
            }

            CurrentPlatform = "white_label";
            LootLockerWhiteLabelSessionRequest sessionRequest = new LootLockerWhiteLabelSessionRequest(existingSessionEmail);
            sessionRequest.token = existingSessionToken;
            LootLockerAPIManager.WhiteLabelSession(sessionRequest, onComplete);
        }

        /// <summary>
        /// Create a new session for a Nintendo Switch user
        ///
        /// The Nintendo Switch platform must be enabled in the web console for this to work.
        /// </summary>
        public static void StartNintendoSwitchSession(string nsa_id_token, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }
            LootLockerNintendoSwitchSessionRequest sessionRequest = new LootLockerNintendoSwitchSessionRequest(nsa_id_token);
            LootLockerAPIManager.NintendoSwitchSession(sessionRequest, onComplete);
        }

        /// <summary>
        /// Create a new session for a Xbox One user
        ///
        /// The Xbox One platform must be enabled in the web console for this to work.
        /// </summary>
        public static void StartXboxOneSession(string xbox_user_token, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }
            LootLockerXboxOneSessionRequest sessionRequest = new LootLockerXboxOneSessionRequest(xbox_user_token);
            LootLockerAPIManager.XboxOneSession(sessionRequest, onComplete);
        }

        /// <summary>
        /// Create a new session for Sign in with Apple
        ///
        /// The Apple sign in platform must be enabled in the web console for this to work.
        /// </summary>
        public static void StartAppleSession(string token, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }
            LootLockerAppleSignInSessionRequest sessionRequest = new LootLockerAppleSignInSessionRequest(token);
            LootLockerAPIManager.AppleSession(sessionRequest, onComplete);
        }

        public static void EndSession(string deviceId, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            CurrentPlatform = "";
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(deviceId);
            LootLockerAPIManager.EndSession(sessionRequest, onComplete);
        }
        #endregion

        #region White Label Login

        /// <summary>
        /// Create new user using the white label login system.
        /// 
        /// White label platform must be enabled in the web console for this to work.
        /// </summary>
        public static void WhiteLabelLogin(string email, string password, Action<LootLockerWhiteLabelLoginResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerWhiteLabelLoginResponse>());
                return;
            }

            LootLockerWhiteLabelUserRequest input = new LootLockerWhiteLabelUserRequest
            {
                email = email,
                password = password
            };

            LootLockerAPIManager.WhiteLabelLogin(input, onComplete);
        }

        public static void WhiteLabelLogin(string email, string password, bool remember, Action<LootLockerWhiteLabelLoginResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerWhiteLabelLoginResponse>());
                return;
            }

            LootLockerWhiteLabelUserRequest input = new LootLockerWhiteLabelUserRequest
            {
                email = email,
                password = password,
                remember = remember
            };

            LootLockerAPIManager.WhiteLabelLogin(input, response => {
                PlayerPrefs.SetString("LootLockerWhiteLabelSessionToken", response.SessionToken);
                PlayerPrefs.SetString("LootLockerWhiteLabelSessionEmail", email);
                PlayerPrefs.Save();

                onComplete(response);
            });
        }

        /// <summary>
        /// Create new user using the white label login system.
        /// 
        /// White label platform must be enabled in the web console for this to work.
        /// </summary>
        public static void WhiteLabelSignUp(string email, string password, Action<LootLockerWhiteLabelSignupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerWhiteLabelSignupResponse>());
                return;
            }

            LootLockerWhiteLabelUserRequest input = new LootLockerWhiteLabelUserRequest
            {
                email = email,
                password = password
            };

            LootLockerAPIManager.WhiteLabelSignUp(input, onComplete);
        }

        /// <summary>
        /// Request password reset email for the user.
        /// 
        /// White label platform must be enabled in the web console for this to work.
        /// </summary>
        public static void WhiteLabelRequestPassword(string email, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            LootLockerAPIManager.WhiteLabelRequestPasswordReset(email, onComplete);
        }

        /// <summary>
        /// Request verify account email for the user.
        /// 
        /// White label platform must be enabled in the web console for this to work.
        /// Account verification must also be enabled.
        /// </summary>
        public static void WhiteLabelRequestVerification(int userID, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            LootLockerAPIManager.WhiteLabelRequestAccountVerification(userID, onComplete);
        }

        #endregion

        #region Player
        //Player calls
        public static void GetPlayerInfo(Action<LootLockerGetPlayerInfoResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPlayerInfoResponse>());
                return;
            }
            LootLockerAPIManager.GetPlayerInfo(onComplete);
        }

        public static void GetInventory(Action<LootLockerInventoryResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInventoryResponse>());
                return;
            }
            LootLockerAPIManager.GetInventory(onComplete);
        }

        public static void GetBalance(Action<LootLockerBalanceResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerBalanceResponse>());
                return;
            }
            LootLockerAPIManager.GetBalance(onComplete);
        }

        public static void SubmitXp(int xpToSubmit, Action<LootLockerXpSubmitResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerXpSubmitResponse>());
                return;
            }
            LootLockerXpSubmitRequest xpSubmitRequest = new LootLockerXpSubmitRequest(xpToSubmit);
            LootLockerAPIManager.SubmitXp(xpSubmitRequest, onComplete);
        }

        public static void GetXpAndLevel(Action<LootLockerXpResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerXpResponse>());
                return;
            }
            LootLockerXpRequest xpRequest = new LootLockerXpRequest();
            LootLockerAPIManager.GetXpAndLevel(xpRequest, onComplete);
        }

        public static void GetAssetNotification(Action<LootLockerPlayerAssetNotificationsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerAssetNotificationsResponse>());
                return;
            }
            LootLockerAPIManager.GetPlayerAssetNotification(onComplete);
        }

        public static void GetDeactivatedAssetNotification(Action<LootLockerDeactivatedAssetsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDeactivatedAssetsResponse>());
                return;
            }
            LootLockerAPIManager.GetDeactivatedAssetNotification(onComplete);
        }

        public static void InitiateDLCMigration(Action<LootLockerDlcResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDlcResponse>());
                return;
            }
            LootLockerAPIManager.InitiateDLCMigration(onComplete);
        }

        public static void GetDLCMigrated(Action<LootLockerDlcResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDlcResponse>());
                return;
            }
            LootLockerAPIManager.GetDLCMigrated(onComplete);
        }

        public static void SetProfilePrivate(Action<LootLockerStandardResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStandardResponse>());
                return;
            }
            LootLockerAPIManager.SetProfilePrivate(onComplete);
        }

        public static void SetProfilePublic(Action<LootLockerStandardResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStandardResponse>());
                return;
            }
            LootLockerAPIManager.SetProfilePublic(onComplete);
        }

        public static void GetPlayerName(Action<PlayerNameResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameResponse>());
                return;
            }
            LootLockerAPIManager.GetPlayerName(onComplete);
        }

        public static void SetPlayerName(string name, Action<PlayerNameResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameResponse>());
                return;
            }

            PlayerNameRequest data = new PlayerNameRequest();
            data.name = name;

            LootLockerAPIManager.SetPlayerName(data, onComplete);
        }
        #endregion

        #region Character
        public static void CreateCharacter(string characterTypeId, string newCharacterName, bool isDefault, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterLoadoutResponse>());
                return;
            }

            LootLockerCreateCharacterRequest data = new LootLockerCreateCharacterRequest();

            data.name = newCharacterName;
            data.is_default = isDefault;
            data.character_type_id = characterTypeId;

            LootLockerAPIManager.CreateCharacter(data, onComplete);
        }

        public static void ListCharacterTypes(Action<LootLockerListCharacterTypesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListCharacterTypesResponse>());
                return;
            }
            LootLockerAPIManager.ListCharacterTypes(onComplete);
        }

        public static void GetCharacterLoadout(Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterLoadoutResponse>());
                return;
            }
            LootLockerAPIManager.GetCharacterLoadout(onComplete);
        }

        public static void GetOtherPlayersCharacterLoadout(string characterID, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterLoadoutResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();

            data.getRequests.Add(characterID);
            data.getRequests.Add(LootLockerConfig.current.platform.ToString());
            LootLockerAPIManager.GetOtherPlayersCharacterLoadout(data, onComplete);
        }

        public static void UpdateCharacter(string characterID, string newCharacterName, bool isDefault, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterLoadoutResponse>());
                return;
            }

            LootLockerUpdateCharacterRequest data = new LootLockerUpdateCharacterRequest();

            data.name = newCharacterName;
            data.is_default = isDefault;

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(characterID);

            LootLockerAPIManager.UpdateCharacter(lootLockerGetRequest, data, onComplete);
        }

        public static void EquipIdAssetToDefaultCharacter(string assetInstanceId, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToCharacterLoadoutResponse>());
                return;
            }
            LootLockerEquipByIDRequest data = new LootLockerEquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceId);
            LootLockerAPIManager.EquipIdAssetToDefaultCharacter(data, onComplete);
        }

        public static void EquipGlobalAssetToDefaultCharacter(string assetId, string assetVariationId, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToCharacterLoadoutResponse>());
                return;
            }
            LootLockerEquipByAssetRequest data = new LootLockerEquipByAssetRequest();
            data.asset_id = int.Parse(assetId);
            data.asset_variation_id = int.Parse(assetVariationId);
            LootLockerAPIManager.EquipGlobalAssetToDefaultCharacter(data, onComplete);
        }

        public static void EquipIdAssetToCharacter(string characterID, string assetInstanceId, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToCharacterLoadoutResponse>());
                return;
            }
            LootLockerEquipByIDRequest data = new LootLockerEquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceId);

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID);
            LootLockerAPIManager.EquipIdAssetToCharacter(lootLockerGetRequest, data, onComplete);
        }

        public static void EquipGlobalAssetToCharacter(string assetId, string assetVariationId, string characterID, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToCharacterLoadoutResponse>());
                return;
            }
            LootLockerEquipByAssetRequest data = new LootLockerEquipByAssetRequest();
            data.asset_id = int.Parse(assetId);
            data.asset_variation_id = int.Parse(assetVariationId);
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID);
            LootLockerAPIManager.EquipGlobalAssetToCharacter(lootLockerGetRequest, data, onComplete);
        }

        public static void UnEquipIdAssetToDefaultCharacter(string assetId, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToCharacterLoadoutResponse>());
                return;
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(assetId);
            LootLockerAPIManager.UnEquipIdAssetToDefaultCharacter(lootLockerGetRequest, onComplete);
        }

        public static void UnEquipIdAssetToCharacter(string assetId, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToCharacterLoadoutResponse>());
                return;
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(assetId);
            LootLockerAPIManager.UnEquipIdAssetToCharacter(lootLockerGetRequest, onComplete);
        }

        public static void GetCurrentLoadOutToDefaultCharacter(Action<LootLockerGetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCurrentLoadouttoDefaultCharacterResponse>());
                return;
            }
            LootLockerAPIManager.GetCurrentLoadOutToDefaultCharacter(onComplete);
        }

        public static void GetCurrentLoadOutToOtherCharacter(string characterID, Action<LootLockerGetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCurrentLoadouttoDefaultCharacterResponse>());
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID);
            lootLockerGetRequest.getRequests.Add(LootLockerConfig.current.platform.ToString());
            LootLockerAPIManager.GetCurrentLoadOutToOtherCharacter(lootLockerGetRequest, onComplete);
        }

        public static void GetEquipableContextToDefaultCharacter(Action<LootLockerContextResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerContextResponse>());
                return;
            }
            LootLockerAPIManager.GetEquipableContextToDefaultCharacter(onComplete);
        }
        #endregion

        #region PlayerStorage
        public static void GetEntirePersistentStorage(Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStoragResponse>());
                return;
            }
            LootLockerAPIManager.GetEntirePersistentStorage(onComplete);
        }

        public static void GetSingleKeyPersistentStorage(string key, Action<LootLockerGetPersistentSingle> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentSingle>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(key);
            LootLockerAPIManager.GetSingleKeyPersistentStorage(data, onComplete);
        }

        public static void UpdateOrCreateKeyValue(string key, string value, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStoragResponse>());
                return;
            }
            LootLockerGetPersistentStorageRequest data = new LootLockerGetPersistentStorageRequest();
            data.AddToPayload(new LootLockerPayload { key = key, value = value });
            LootLockerAPIManager.UpdateOrCreateKeyValue(data, onComplete);
        }

        public static void UpdateOrCreateKeyValue(LootLockerGetPersistentStorageRequest data, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStoragResponse>());
                return;
            }
            LootLockerAPIManager.UpdateOrCreateKeyValue(data, onComplete);
        }

        public static void DeleteKeyValue(string keyToDelete, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStoragResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(keyToDelete);
            LootLockerAPIManager.DeleteKeyValue(data, onComplete);
        }

        public static void GetOtherPlayersPublicKeyValuePairs(string otherPlayerId, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {

            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStoragResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(otherPlayerId);
            LootLockerAPIManager.GetOtherPlayersPublicKeyValuePairs(data, onComplete);
        }
        #endregion

        #region Assets
        public static void GetContext(Action<LootLockerContextResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerContextResponse>());
                return;
            }
            LootLockerAPIManager.GetContext(onComplete);
        }

        public static void GetAssetsOriginal(int assetCount, Action<LootLockerAssetResponse> onComplete, int? idOfLastAsset = null, List<LootLocker.LootLockerEnums.AssetFilter> filter = null, bool includeUGC = false, Dictionary<string, string> assetFilters = null, int UGCCreatorPlayerID = 0)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>());
                return;
            }
            LootLockerAPIManager.GetAssetsOriginal(onComplete, assetCount, idOfLastAsset, filter, includeUGC, assetFilters, UGCCreatorPlayerID);
        }

        public static void GetAssetListWithCount(int assetCount, Action<LootLockerAssetResponse> onComplete, List<LootLocker.LootLockerEnums.AssetFilter> filter = null, bool includeUGC = false, Dictionary<string, string> assetFilters = null, int UGCCreatorPlayerID = 0)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>());
                return;
            }
            LootLockerAPIManager.GetAssetsOriginal((response) =>
            {
                if (response.statusCode == 200)
                {
                    if (response != null && response.assets != null && response.assets.Length > 0)
                        LootLockerAssetRequest.lastId = response.assets.Last()?.id != null ? response.assets.Last().id : 0;
                }

                onComplete?.Invoke(response);
            }, assetCount, null, filter, includeUGC, assetFilters, UGCCreatorPlayerID);
        }

        public static void GetAssetNextList(int assetCount, Action<LootLockerAssetResponse> onComplete, List<LootLocker.LootLockerEnums.AssetFilter> filter = null, bool includeUGC = false, Dictionary<string, string> assetFilters = null, int UGCCreatorPlayerID = 0)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>());
                return;
            }

            LootLockerAPIManager.GetAssetsOriginal((response) =>
            {
                if (response.statusCode == 200)
                {
                    if (response != null && response.assets != null && response.assets.Length > 0)
                        LootLockerAssetRequest.lastId = response.assets.Last()?.id != null ? response.assets.Last().id : 0;
                }
                onComplete?.Invoke(response);
            }, assetCount, LootLockerAssetRequest.lastId, filter, includeUGC, assetFilters, UGCCreatorPlayerID);
        }

        public static void ResetAssetCalls()
        {
            LootLockerAssetRequest.lastId = 0;
        }

        public static void GetAssetInformation(string assetId, Action<LootLockerCommonAsset> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCommonAsset>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.GetAssetInformation(data, onComplete);
        }

        public static void ListFavouriteAssets(Action<LootLockerFavouritesListResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFavouritesListResponse>());
                return;
            }
            LootLockerAPIManager.ListFavouriteAssets(onComplete);
        }

        public static void AddFavouriteAsset(string assetId, Action<LootLockerAssetResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.AddFavouriteAsset(data, onComplete);
        }

        public static void RemoveFavouriteAsset(string assetId, Action<LootLockerAssetResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.RemoveFavouriteAsset(data, onComplete);
        }

        public static void GetAssetsById(string[] assetIdsToRetrieve, Action<LootLockerAssetResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();

            for (int i = 0; i < assetIdsToRetrieve.Length; i++)
                data.getRequests.Add(assetIdsToRetrieve[i]);

            LootLockerAPIManager.GetAssetsById(data, onComplete);
        }

        #endregion

        #region AssetInstance
        public static void GetAllKeyValuePairsForAssetInstances(Action<LootLockerGetAllKeyValuePairsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetAllKeyValuePairsResponse>());
                return;
            }
            LootLockerAPIManager.GetAllKeyValuePairs(onComplete);
        }

        public static void GetAllKeyValuePairsToAnInstance(int instanceId, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(instanceId.ToString());
            LootLockerAPIManager.GetAllKeyValuePairsToAnInstance(data, onComplete);
        }

        public static void GetAKeyValuePairByIdForAssetInstances(int assetId, int instanceId, Action<LootLockerGetSingleKeyValuePairsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetSingleKeyValuePairsResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            data.getRequests.Add(instanceId.ToString());
            LootLockerAPIManager.GetAKeyValuePairById(data, onComplete);
        }

        public static void CreateKeyValuePairForAssetInstances(int assetId, string key, string value, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerCreateKeyValuePairRequest createKeyValuePairRequest = new LootLockerCreateKeyValuePairRequest();
            createKeyValuePairRequest.key = key;
            createKeyValuePairRequest.value = value;
            LootLockerAPIManager.CreateKeyValuePair(data, createKeyValuePairRequest, onComplete);
        }

        public static void UpdateOneOrMoreKeyValuePairForAssetInstances(int assetId, Dictionary<string, string> data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
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
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerCreateKeyValuePairRequest createKeyValuePairRequest = new LootLockerCreateKeyValuePairRequest();
            createKeyValuePairRequest.key = key;
            createKeyValuePairRequest.value = value;
            LootLockerAPIManager.UpdateKeyValuePairById(data, createKeyValuePairRequest, onComplete);
        }

        public static void DeleteKeyValuePairForAssetInstances(int assetId, int instanceId, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            data.getRequests.Add(instanceId.ToString());
            LootLockerAPIManager.DeleteKeyValuePair(data, onComplete);
        }

        public static void InspectALootBoxForAssetInstances(int assetId, Action<LootLockerInspectALootBoxResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInspectALootBoxResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.InspectALootBox(data, onComplete);
        }

        public static void OpenALootBoxForAssetInstances(int assetId, Action<LootLockerOpenLootBoxResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerOpenLootBoxResponse>());
                return;
            }
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
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>());
                return;
            }

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
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>());
                return;
            }

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
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.DeletingAnAssetCandidate(data, onComplete);
        }

        public static void GettingASingleAssetCandidate(int assetId, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.GettingASingleAssetCandidate(data, onComplete);
        }

        public static void ListingAssetCandidates(Action<LootLockerListingAssetCandidatesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListingAssetCandidatesResponse>());
                return;
            }
            LootLockerAPIManager.ListingAssetCandidates(onComplete);
        }

        public static void AddingFilesToAssetCandidates(int assetId, string filePath, string fileName,
            FilePurpose filePurpose, Action<LootLockerUserGenerateContentResponse> onComplete, string fileContentType = null)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>());
                return;
            }

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
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>());
                return;
            }

            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            data.getRequests.Add(fileId.ToString());

            LootLockerAPIManager.RemovingFilesFromAssetCandidates(data, onComplete);
        }
        #endregion

        #region Missions
        public static void GettingAllMissions(Action<LootLockerGettingAllMissionsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGettingAllMissionsResponse>());
                return;
            }
            LootLockerAPIManager.GettingAllMissions(onComplete);
        }

        public static void GettingASingleMission(int missionId, Action<LootLockerGettingASingleMissionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGettingASingleMissionResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.GettingASingleMission(data, onComplete);
        }

        public static void StartingAMission(int missionId, Action<LootLockerStartingAMissionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStartingAMissionResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.StartingAMission(data, onComplete);
        }

        public static void FinishingAMission(int missionId, string startingMissionSignature, string playerId,
            LootLockerFinishingPayload finishingPayload, Action<LootLockerFinishingAMissionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFinishingAMissionResponse>());
                return;
            }

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
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerMapsResponse>());
                return;
            }
            LootLockerAPIManager.GettingAllMaps(onComplete);
        }
        #endregion

        #region Purchasing
        public static void NormalPurchaseCall(int asset_id, int variation_id, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseResponse>());
                return;
            }
            LootLockerNormalPurchaseRequest data = new LootLockerNormalPurchaseRequest { asset_id = asset_id, variation_id = variation_id };
            List<LootLockerNormalPurchaseRequest> datas = new List<LootLockerNormalPurchaseRequest>();
            datas.Add(data);
            LootLockerAPIManager.NormalPurchaseCall(datas.ToArray(), onComplete);
        }

        public static void RentalPurchaseCall(int asset_id, int variation_id, int rental_option_id, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseResponse>());
                return;
            }
            LootLockerRentalPurchaseRequest data = new LootLockerRentalPurchaseRequest { asset_id = asset_id, variation_id = variation_id, rental_option_id = rental_option_id };
            LootLockerAPIManager.RentalPurchaseCall(data, onComplete);
        }

        public static void IosPurchaseVerification(string receipt_data, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseResponse>());
                return;
            }
            LootLockerIosPurchaseVerificationRequest[] data = new LootLockerIosPurchaseVerificationRequest[] { new LootLockerIosPurchaseVerificationRequest { receipt_data = receipt_data } };
            LootLockerAPIManager.IosPurchaseVerification(data, onComplete);
        }

        public static void AndroidPurchaseVerification(string purchase_token, int asset_id, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseResponse>());
                return;
            }
            LootLockerAndroidPurchaseVerificationRequest[] data = new LootLockerAndroidPurchaseVerificationRequest[] { new LootLockerAndroidPurchaseVerificationRequest { purchase_token = purchase_token, asset_id = asset_id } };

            LootLockerAPIManager.AndroidPurchaseVerification(data, onComplete);
        }

        public static void PollingOrderStatus(int assetId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterLoadoutResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.PollingOrderStatus(data, onComplete);
        }

        public static void ActivatingARentalAsset(int assetId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterLoadoutResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.ActivatingARentalAsset(data, onComplete);
        }
        #endregion

        #region Collectables
        public static void GettingCollectables(Action<LootLockerGettingCollectablesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGettingCollectablesResponse>());
                return;
            }
            LootLockerAPIManager.GettingCollectables(onComplete);
        }

        public static void CollectingAnItem(string slug, Action<LootLockerCollectingAnItemResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCollectingAnItemResponse>());
                return;
            }
            LootLockerCollectingAnItemRequest data = new LootLockerCollectingAnItemRequest();
            data.slug = slug;
            LootLockerAPIManager.CollectingAnItem(data, onComplete);
        }

        #endregion

        #region Messages

        public static void GetMessages(Action<LootLockerGetMessagesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMessagesResponse>());
                return;
            }
            LootLockerAPIManager.GetMessages(onComplete);
        }

        #endregion

        #region TriggerEvents
        public static void TriggeringAnEvent(string eventName, Action<LootLockerTriggerAnEventResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerTriggerAnEventResponse>());
                return;
            }
            LootLockerTriggerAnEventRequest data = new LootLockerTriggerAnEventRequest { name = eventName };
            LootLockerAPIManager.TriggeringAnEvent(data, onComplete);
        }

        public static void ListingTriggeredTriggerEvents(Action<LootLockerListingAllTriggersResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListingAllTriggersResponse>());
                return;
            }
            LootLockerAPIManager.ListingTriggeredTriggerEvents(onComplete);
        }

        #endregion

        #region Crashes
        public static void SubmittingACrashLog(string logFIlePath, string game_version, string type_identifier, string local_crash_time,
            Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }
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

        #region Leaderboard
        public static void GetMemberRank(string leaderboardId, string member_id, Action<LootLockerGetMemberRankResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMemberRankResponse>());
                return;
            }
            LootLockerGetMemberRankRequest lootLockerGetMemberRankRequest = new LootLockerGetMemberRankRequest();

            lootLockerGetMemberRankRequest.leaderboardId = leaderboardId;
            lootLockerGetMemberRankRequest.member_id = member_id;

            LootLockerAPIManager.GetMemberRank(lootLockerGetMemberRankRequest, onComplete);
        }

        public static void GetMemberRank(string leaderboardId, int member_id, Action<LootLockerGetMemberRankResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMemberRankResponse>());
                return;
            }
            LootLockerGetMemberRankRequest lootLockerGetMemberRankRequest = new LootLockerGetMemberRankRequest();

            lootLockerGetMemberRankRequest.leaderboardId = leaderboardId;
            lootLockerGetMemberRankRequest.member_id = member_id.ToString();

            LootLockerAPIManager.GetMemberRank(lootLockerGetMemberRankRequest, onComplete);
        }

        public static void GetByListOfMembers(string[] members, int id, Action<LootLockerGetByListOfMembersResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetByListOfMembersResponse>());
                return;
            }
            LootLockerGetByListMembersRequest request = new LootLockerGetByListMembersRequest();

            request.members = members;

            LootLockerAPIManager.GetByListOfMembers(request, id.ToString(), onComplete);
        }

        public static void GetAllMemberRanksMain(int member_id, int count, int after, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetAllMemberRanksResponse>());
                return;
            }
            LootLockerGetAllMemberRanksRequest request = new LootLockerGetAllMemberRanksRequest();
            request.member_id = member_id;
            request.count = count;
            request.after = after > 0 ? after.ToString() : null;
            Action<LootLockerGetAllMemberRanksResponse> callback = (response) =>
            {
                if (response != null && response.pagination != null)
                {
                    LootLockerGetAllMemberRanksRequest.nextCursor = response.pagination.next_cursor;
                    LootLockerGetAllMemberRanksRequest.prevCursor = response.pagination.previous_cursor;
                    response.pagination.allowNext = response.pagination.next_cursor > 0;
                    response.pagination.allowPrev = (response.pagination.previous_cursor != null);
                }
                onComplete?.Invoke(response);
            };
            LootLockerAPIManager.GetAllMemberRanks(request, callback);
        }

        public static void GetAllMemberRanks(int member_id, int count, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            GetAllMemberRanksMain(member_id, count, -1, onComplete);
        }

        public static void GetAllMemberRanksNext(int member_id, int count, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            GetAllMemberRanksMain(member_id, count, int.Parse(LootLockerGetAllMemberRanksRequest.nextCursor.ToString()), onComplete);
        }

        public static void GetAllMemberRanksPrev(int member_id, int count, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            GetAllMemberRanksMain(member_id, count, int.Parse(LootLockerGetAllMemberRanksRequest.prevCursor.ToString()), onComplete);
        }

        public static void ResetAllMemberRanksCalls()
        {
            LootLockerGetAllMemberRanksRequest.Reset();
        }

        public static void GetAllMemberRanksOriginal(int member_id, int count, int after, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetAllMemberRanksResponse>());
                return;
            }
            LootLockerGetAllMemberRanksRequest request = new LootLockerGetAllMemberRanksRequest();
            request.member_id = member_id;
            request.count = count;
            request.after = after > 0 ? after.ToString() : null;

            LootLockerAPIManager.GetAllMemberRanks(request, onComplete);
        }

        public static void GetScoreListMain(int leaderboardId, int count, int after, Action<LootLockerGetScoreListResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetScoreListResponse>());
                return;
            }
            LootLockerGetScoreListRequest request = new LootLockerGetScoreListRequest();
            request.leaderboardId = leaderboardId;
            request.count = count;
            request.after = after > 0 ? after.ToString() : null;
            Action<LootLockerGetScoreListResponse> callback = (response) =>
            {
                if (response != null && response.pagination != null)
                {
                    LootLockerGetScoreListRequest.nextCursor = response.pagination.next_cursor;
                    LootLockerGetScoreListRequest.prevCursor = response.pagination.previous_cursor;
                    response.pagination.allowNext = response.pagination.next_cursor > 0;
                    response.pagination.allowPrev = (response.pagination.previous_cursor != null);
                }
                onComplete?.Invoke(response);
            };
            LootLockerAPIManager.GetScoreList(request, callback);
        }

        public static void GetScoreList(int leaderboardId, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreListMain(leaderboardId, count, -1, onComplete);
        }

        public static void GetNextScoreList(int leaderboardId, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreListMain(leaderboardId, count, int.Parse(LootLockerGetScoreListRequest.nextCursor.ToString()), onComplete);
        }

        public static void GetPrevScoreList(int leaderboardId, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreListMain(leaderboardId, count, int.Parse(LootLockerGetScoreListRequest.prevCursor.ToString()), onComplete);
        }

        public static void ResetScoreCalls()
        {
            LootLockerGetScoreListRequest.Reset();
        }

        public static void GetScoreListOriginal(int leaderboardId, int count, int after, Action<LootLockerGetScoreListResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetScoreListResponse>());
                return;
            }
            LootLockerGetScoreListRequest request = new LootLockerGetScoreListRequest();
            request.leaderboardId = leaderboardId;
            request.count = count;
            request.after = after > 0 ? after.ToString() : null;

            LootLockerAPIManager.GetScoreList(request, onComplete);
        }

        public static void SubmitScore(string memberId, int score, int leaderboardId, Action<LootLockerSubmitScoreResponse> onComplete)
        {
            SubmitScore(memberId, score, leaderboardId.ToString(), "", onComplete);
        }

        public static void SubmitScore(string memberId, int score, string leaderboardKey, Action<LootLockerSubmitScoreResponse> onComplete)
        {
            SubmitScore(memberId, score, leaderboardKey, "", onComplete);
        }

        public static void SubmitScore(string memberId, int score, string leaderboardId, string metadata, Action<LootLockerSubmitScoreResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSubmitScoreResponse>());
                return;
            }
            LootLockerSubmitScoreRequest request = new LootLockerSubmitScoreRequest();
            request.member_id = memberId;
            request.score = score;
            if (!string.IsNullOrEmpty(metadata))
                request.metadata = metadata;

            LootLockerAPIManager.SubmitScore(request, leaderboardId, onComplete);
        }

        public static void ComputeAndLockDropTable(int tableInstanceId, Action<LootLockerComputeAndLockDropTableResponse> onComplete, bool AddAssetDetails = false, string tag = "")
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerComputeAndLockDropTableResponse>());
                return;
            }
            LootLockerAPIManager.ComputeAndLockDropTable(tableInstanceId, onComplete, AddAssetDetails, tag);
        }

        public static void PickDropsFromDropTable(int[] picks, int tableInstanceId, Action<LootLockerPickDropsFromDropTableResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPickDropsFromDropTableResponse>());
                return;
            }
            PickDropsFromDropTableRequest data = new PickDropsFromDropTableRequest();
            data.picks = picks;

            LootLockerAPIManager.PickDropsFromDropTable(data, tableInstanceId, onComplete);
        }
        #endregion

        #region Reports

        /// <summary>
        /// Retrieves the different types of report possible.
        /// 
        /// These can be changed in the web interface or through the Admin API.
        /// </summary>
        public static void GetReportTypes(Action<LootLockerReportsGetTypesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReportsGetTypesResponse>());
                return;
            }
            
            LootLockerAPIManager.GetReportTypes(onComplete);
        }

        /// <summary>
        /// Create a report of a player
        /// </summary>
        public static void CreatePlayerReport(ReportsCreatePlayerRequest input, Action<LootLockerReportsCreatePlayerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReportsCreatePlayerResponse>());
                return;
            }

            LootLockerAPIManager.CreatePlayerReport(input, onComplete);
        }

        /// <summary>
        /// Create a report of an asset
        /// </summary>
        public static void CreateAssetReport(ReportsCreateAssetRequest input, Action<LootLockerReportsCreateAssetResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReportsCreateAssetResponse>());
                return;
            }

            LootLockerAPIManager.CreateAssetReport(input, onComplete);
        }

        /// <summary>
        /// Get removed UGC for the current player. 
        /// 
        /// If any of their UGC has been removed as a result of reports they will be returned in this method
        /// </summary>
        public static void GetRemovedUGCForPlayer(GetRemovedUGCForPlayerInput input, Action<LootLockerReportsGetRemovedAssetsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReportsGetRemovedAssetsResponse>());
                return;
            }
            
            LootLockerAPIManager.GetRemovedUGCForPlayer(input, onComplete);
        }

        #endregion

        #region Misc

        public static void Ping(Action<LootLockerPingResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPingResponse>());
                return;
            }

            LootLockerAPIManager.Ping(onComplete);
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
