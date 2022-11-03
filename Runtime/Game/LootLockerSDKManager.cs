using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Manually initialize the SDK.
        /// </summary>
        /// <param name="apiKey">Find the Game API-key at https://my.lootlocker.io/settings/game and click on the API-tab</param>
        /// <param name="gameVersion">The current version of the game in the format 1.2.3.4 (the 3 and 4 being optional but recommended)</param>
        /// <param name="platform">What platform you are using, only used for purchases, use Android if you are unsure</param>
        /// <param name="onDevelopmentMode">Reflecting stage/live on the LootLocker webconsole</param>
        /// <param name="domainKey">Extra key needed for some endpoints, can be found by going to https://my.lootlocker.io/settings/game and click on the API-tab</param>
        /// <returns>True if initialized successfully, false otherwise</returns>
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
        /// Checks if an active session exists.
        /// </summary>
        /// <returns>True if a token is found, false otherwise.</returns>
        private static bool CheckActiveSession()
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.token))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Utility function to check if the sdk has been initiazed
        /// </summary>
        /// <returns>True if initialized, false otherwise.</returns>
        public static bool CheckInitialized(bool skipSessionCheck = false)
        {
            if (!initialized)
            {
                LootLockerConfig.current.UpdateToken("", "");
                if (!Init())
                {
                    return false;
                }
            }

            if (!skipSessionCheck && !CheckActiveSession())
            {
                Debug.LogError("You cannot call this method before an active LootLocker session is started");
                return false;
            }

            return true;
        }

        /// <summary>
        /// LootLocker Debug-messages, only visible in the Unity Editor.
        /// </summary>
        /// <param name="message">A message as a string</param>
        /// <param name="IsError">Is this an error or not?</param>
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
        /// <summary>
        /// Verify the player's steam identity with the server. You can read more on how to setup Steam with LootLocker here; https://docs.lootlocker.com/how-to/authentication/steam
        /// </summary>
        /// <param name="steamSessionTicket">A steamSessionTicket in string-format</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerVerifyResponse</param>
        public static void VerifySteamID(string steamSessionTicket, Action<LootLockerVerifyResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerVerifyResponse>());
                return;
            }
            LootLockerVerifySteamRequest verifyRequest = new LootLockerVerifySteamRequest(steamSessionTicket);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }

        /// <summary>
        /// Convert a steam ticket so LootLocker can read it. You can read more on how to setup Steam with LootLocker here; https://docs.lootlocker.com/how-to/authentication/steam
        /// </summary>
        /// <param name="ticket"></param>
        /// <param name="ticketSize"></param>
        /// <returns>A converted SteamSessionTicket as a string for use with StartSteamSession.</returns>
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

        /// <summary>
        /// Verify the player's identity with the server and selected platform.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerVerifyResponse</param>
        public static void VerifyID(string deviceId, Action<LootLockerVerifyResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerVerifyResponse>());
            }
            LootLockerVerifyRequest verifyRequest = new LootLockerVerifyRequest(deviceId);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }

        /// <summary>
        /// Start a session with the platform used in the platform selected in Project Settings -> Platform.
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// <param name="deviceId">The ID of the current device the player is on</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartSession(string deviceId, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            CurrentPlatform = LootLockerConfig.current.platform.ToString();

            LootLockerConfig.current.deviceID = deviceId;
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(deviceId);
            LootLockerAPIManager.Session(sessionRequest, onComplete);
        }

        /// <summary>
        /// Start a guest session.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGuestSessionResponse</param>
        public static void StartGuestSession(Action<LootLockerGuestSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGuestSessionResponse>());
                return;
            }

            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest();
            string existingPlayerID = PlayerPrefs.GetString("LootLockerGuestPlayerID", "");
            if (!string.IsNullOrEmpty(existingPlayerID))
            {
                sessionRequest = new LootLockerSessionRequest(existingPlayerID);
            }

            LootLockerAPIManager.GuestSession(sessionRequest, response =>
            {
                CurrentPlatform = "guest";

                if (response.success)
                {
                    PlayerPrefs.SetString("LootLockerGuestPlayerID", response.player_identifier);
                    PlayerPrefs.Save();
                }

                onComplete(response);
            });
        }

        /// <summary>
        /// Start a guest session with an identifier, you can use something like SystemInfo.deviceUniqueIdentifier to tie the account to a device.
        /// </summary>
        /// <param name="identifier">Identifier for the player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGuestSessionResponse</param>
        public static void StartGuestSession(string identifier, Action<LootLockerGuestSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
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

                onComplete(response);
            });
        }

        /// <summary>
        /// Start a steam session. You can read more on how to setup Steam with LootLocker here; https://docs.lootlocker.com/how-to/authentication/steam
        /// </summary>
        /// <param name="steamId64">Steam ID ass a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartSteamSession(string steamId64, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            CurrentPlatform = "steam";

            LootLockerSteamSessionRequest sessionRequest = new LootLockerSteamSessionRequest(steamId64);
            LootLockerAPIManager.Session(sessionRequest, onComplete);
        }

        /// <summary>
        /// Create a new session for a Nintendo Switch user
        /// The Nintendo Switch platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="nsa_id_token">nsa (Nintendo Switch Account) id token as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartNintendoSwitchSession(string nsa_id_token, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }
            LootLockerNintendoSwitchSessionRequest sessionRequest = new LootLockerNintendoSwitchSessionRequest(nsa_id_token);
            LootLockerAPIManager.NintendoSwitchSession(sessionRequest, onComplete);
        }

        /// <summary>
        /// Create a new session for a Xbox One user
        /// The Xbox One platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="xbox_user_token">Xbox user token as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of typeLootLockerSessionResponse</param>
        public static void StartXboxOneSession(string xbox_user_token, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }
            LootLockerXboxOneSessionRequest sessionRequest = new LootLockerXboxOneSessionRequest(xbox_user_token);
            LootLockerAPIManager.XboxOneSession(sessionRequest, onComplete);
        }

        /// <summary>
        /// Create a new session for Sign in with Apple
        /// The Apple sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="authorization_code">Authorization code, provided by apple</param>
        /// <param name="onComplete">onComplete Action for handling the response of type  for handling the response of type LootLockerAppleSessionResponse</param>
        public static void StartAppleSession(string authorization_code, Action<LootLockerAppleSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAppleSessionResponse>());
                return;
            }
            LootLockerAppleSignInSessionRequest sessionRequest = new LootLockerAppleSignInSessionRequest(authorization_code);
            LootLockerAPIManager.AppleSession(sessionRequest, onComplete);
        }

        /// <summary>
        /// Refresh a previous session signed in with Apple
        /// A response code of 401 (Unauthorized) means the refresh token has expired and you'll need to sign in again
        /// The Apple sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="refresh_token">Token received in response from StartAppleSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAppleSessionResponse</param>
        public static void RefreshAppleSession(string refresh_token, Action<LootLockerAppleSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAppleSessionResponse>());
                return;
            }
            LootLockerAppleRefreshSessionRequest sessionRequest = new LootLockerAppleRefreshSessionRequest(refresh_token);
            LootLockerAPIManager.AppleSession(sessionRequest, onComplete);
        }

        /// <summary>
        /// End active session (if any exists)
        /// Succeeds if a session was ended or no sessions were active
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void EndSession(Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }
            else if (!CheckActiveSession())
            {
                onComplete?.Invoke(new LootLockerSessionResponse() { success = true, hasError = false, text = "No active session" });
            }

            // Clear White Label Login credentials
            if (CurrentPlatform == "white_label")
            {
                PlayerPrefs.DeleteKey("LootLockerWhiteLabelSessionToken");
                PlayerPrefs.DeleteKey("LootLockerWhiteLabelSessionEmail");
            }

            CurrentPlatform = "";
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest();
            LootLockerAPIManager.EndSession(sessionRequest, onComplete);
        }

        [Obsolete("Calling this method with devideId is deprecated")]
        public static void EndSession(string deviceId, Action<LootLockerSessionResponse> onComplete)
        {
            EndSession(onComplete);
        }
        #endregion

        #region White Label

        /// <summary>
        /// Log in a White Label user with the given email and password combination, verify user, and start a White Label Session.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="email">E-mail for an existing user</param>
        /// <param name="password">Password for an existing user</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerWhiteLabelLoginResponse</param>
        public static void WhiteLabelLogin(string email, string password, Action<LootLockerWhiteLabelLoginResponse> onComplete)
        {
            WhiteLabelLogin(email, password, false, onComplete);
        }

        /// <summary>
        /// Log in a White Label user with the given email and password combination, verify user, and start a White Label Session.
        /// Set remember=true to prolong the session lifetime
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="email">E-mail for an existing user</param>
        /// <param name="password">Password for an existing user</param>
        /// <param name="remember">Set remember=true to prolong the session lifetime</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerWhiteLabelLoginResponse</param>
        public static void WhiteLabelLogin(string email, string password, bool remember, Action<LootLockerWhiteLabelLoginResponse> onComplete)
        {
            if (!CheckInitialized(true))
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
        /// Create new user using the White Label login system.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="email">E-mail for the new user</param>
        /// <param name="password">Password for the new user</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerWhiteLabelSignupResponse</param>
        public static void WhiteLabelSignUp(string email, string password, Action<LootLockerWhiteLabelSignupResponse> onComplete)
        {
            if (!CheckInitialized(true))
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
        /// Request a password reset email for the given email address.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="email">E-mail for the user</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        public static void WhiteLabelRequestPassword(string email, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            LootLockerAPIManager.WhiteLabelRequestPasswordReset(email, onComplete);
        }

        /// <summary>
        /// Request verify account email for the user.
        /// White Label platform must be enabled in the web console for this to work.
        /// Account verification must also be enabled.
        /// </summary>
        /// <param name="userID">ID of the player, will be retrieved when signing up/logging in.</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        public static void WhiteLabelRequestVerification(int userID, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            LootLockerAPIManager.WhiteLabelRequestAccountVerification(userID, onComplete);
        }

        /// <summary>
        /// Checks for a stored session and if that session is valid.
        /// Depending on response of this method the developer can either start a session using the token,
        /// or show a login form.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action bool that returns true if a White Label session exists </param>
        public static void CheckWhiteLabelSession(Action<bool> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete(false);
                return;
            }

            string existingSessionEmail = PlayerPrefs.GetString("LootLockerWhiteLabelSessionEmail", "");
            string existingSessionToken = PlayerPrefs.GetString("LootLockerWhiteLabelSessionToken", "");
            if (string.IsNullOrEmpty(existingSessionToken) || string.IsNullOrEmpty(existingSessionEmail))
            {
                onComplete(false);
                return;
            }

            VerifyWhiteLabelSession(existingSessionEmail, existingSessionToken, onComplete);
        }

        /// <summary>
        /// Checks if the provided session token is valid for the provided White Label email.
        /// Depending on response of this method the developer can either start a session using the token,
        /// or show a login form.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="email">E-mail for the user</param>
        /// <param name="token">The token is received when starting a White Label session.</param>
        /// <param name="onComplete">onComplete Action bool that returns true if a White Label session exists </param>
        public static void CheckWhiteLabelSession(string email, string token, Action<bool> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete(false);
                return;
            }

            VerifyWhiteLabelSession(email, token, onComplete);
        }

        /// <summary>
        /// Checks if the provided session token is valid for the provided White Label email.
        /// Depending on response of this method the developer can either start a session using the token,
        /// or show a login form.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="email">E-mail for the user</param>
        /// <param name="token">The token can be received when starting a White Label session.</param>
        /// <param name="onComplete">onComplete Action bool that returns true if a White Label session is verified </param>
        private static void VerifyWhiteLabelSession(string email, string token, Action<bool> onComplete)
        {
            LootLockerWhiteLabelVerifySessionRequest sessionRequest = new LootLockerWhiteLabelVerifySessionRequest();
            sessionRequest.email = email;
            sessionRequest.token = token;

            LootLockerAPIManager.WhiteLabelVerifySession(sessionRequest, response =>
            {
                onComplete(response.success);
            });
        }

        /// <summary>
        /// Start a LootLocker Session using the cached White Label token and email if any exist
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartWhiteLabelSession(Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            string existingSessionToken = PlayerPrefs.GetString("LootLockerWhiteLabelSessionToken", "");
            if (string.IsNullOrEmpty(existingSessionToken))
            {
                onComplete(LootLockerResponseFactory.Error<LootLockerSessionResponse>("no session token found"));
                return;
            }

            string existingSessionEmail = PlayerPrefs.GetString("LootLockerWhiteLabelSessionEmail", "");
            if (string.IsNullOrEmpty(existingSessionEmail))
            {
                onComplete(LootLockerResponseFactory.Error<LootLockerSessionResponse>("no session email found"));
                return;
            }

            LootLockerWhiteLabelSessionRequest sessionRequest = new LootLockerWhiteLabelSessionRequest() { email = existingSessionEmail, token = existingSessionToken };
            StartWhiteLabelSession(sessionRequest, onComplete);
        }

        [ObsoleteAttribute("This function is deprecated and will be removed soon, please use the parameter-less StartWhiteLabelSession method instead.")]
        public static void StartWhiteLabelSession(string email, string password, Action<LootLockerSessionResponse> onComplete)
        {
            LootLockerWhiteLabelSessionRequest sessionRequest = new LootLockerWhiteLabelSessionRequest() { email = email, password = password };
            StartWhiteLabelSession(sessionRequest, onComplete);
        }

        /// <summary>
        /// Start a LootLocker Session using the provided White Label request.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="sessionRequest">A White Label Session Request with inner values already set</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartWhiteLabelSession(LootLockerWhiteLabelSessionRequest sessionRequest, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }
            CurrentPlatform = "white_label";
            LootLockerAPIManager.WhiteLabelSession(sessionRequest, onComplete);
        }

        #endregion

        #region Player
        /// <summary>
        /// Get general information about the current current player, such as the XP, Level information and their account balance.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPlayerInfoResponse</param>
        public static void GetPlayerInfo(Action<LootLockerGetPlayerInfoResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPlayerInfoResponse>());
                return;
            }
            LootLockerAPIManager.GetPlayerInfo(onComplete);
        }

        /// <summary>
        /// Get the players inventory.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInventoryResponse</param>
        public static void GetInventory(Action<LootLockerInventoryResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInventoryResponse>());
                return;
            }
            LootLockerAPIManager.GetInventory(onComplete);
        }

        /// <summary>
        /// Get the amount of credits/currency that the player has.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerBalanceResponse</param>
        public static void GetBalance(Action<LootLockerBalanceResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerBalanceResponse>());
                return;
            }
            LootLockerAPIManager.GetBalance(onComplete);
        }

        /// <summary>
        /// Award XP to the player.
        /// </summary>
        /// <param name="xpToSubmit">Amount of XP</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerXpSubmitResponse</param>
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

        [Obsolete("This function will be removed at a later stage, use GetPlayerInfo instead")]
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

        /// <summary>
        /// Get assets that have been given to the currently logged in player since the last time this endpoint was called.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerAssetNotificationsResponse</param>
        public static void GetAssetNotification(Action<LootLockerPlayerAssetNotificationsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerAssetNotificationsResponse>());
                return;
            }
            LootLockerAPIManager.GetPlayerAssetNotification(onComplete);
        }

        /// <summary>
        /// Get asset deactivations for the currently logged in player since the last time this endpoint was called.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerDeactivatedAssetsResponse</param>
        public static void GetDeactivatedAssetNotification(Action<LootLockerDeactivatedAssetsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDeactivatedAssetsResponse>());
                return;
            }
            LootLockerAPIManager.GetDeactivatedAssetNotification(onComplete);
        }

        /// <summary>
        /// Initiate DLC Migration for the player: https://docs.lootlocker.com/background/live-ops-tools#dlc-migration
        /// 5 minutes after calling this endpoint you should issue a call to the Player Asset Notifications call to get the results of the migrated DLC, if any. If you only want the ID's of the assets you can also use  GetDLCMigrated(Action<LootLockerDlcResponse> onComplete).
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerDlcResponse</param>
        public static void InitiateDLCMigration(Action<LootLockerDlcResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDlcResponse>());
                return;
            }
            LootLockerAPIManager.InitiateDLCMigration(onComplete);
        }

        /// <summary>
        /// Get a list of DLC's migrated for the player. This response will only list the asset-ID's of the migrated DLC, if you want more information about the assets, use GetAssetNotification(Action<LootLockerPlayerAssetNotificationsResponse> onComplete) instead.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerDlcResponse</param>
        public static void GetDLCMigrated(Action<LootLockerDlcResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDlcResponse>());
                return;
            }
            LootLockerAPIManager.GetDLCMigrated(onComplete);
        }

        /// <summary>
        /// Set the players profile to be private. This means that their inventory will not be displayed publicly on Steam and other places.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerStandardResponse</param>
        public static void SetProfilePrivate(Action<LootLockerStandardResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStandardResponse>());
                return;
            }
            LootLockerAPIManager.SetProfilePrivate(onComplete);
        }

        /// <summary>
        /// Set the players profile to public. This means that their inventory will be displayed publicly on Steam and other places.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerStandardResponse</param>
        public static void SetProfilePublic(Action<LootLockerStandardResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStandardResponse>());
                return;
            }
            LootLockerAPIManager.SetProfilePublic(onComplete);
        }

        /// <summary>
        /// Get the logged in players name.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameResponse</param>
        public static void GetPlayerName(Action<PlayerNameResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameResponse>());
                return;
            }
            LootLockerAPIManager.GetPlayerName(onComplete);
        }

        /// <summary>
        /// Get players 1st party platform ID's from the provided list of playerID's.
        /// </summary>
        /// <param name="playerIds">A list of multiple player ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type Player1stPartyPlatformIDsLookupResponse</param>
        public static void LookupPlayer1stPartyPlatformIds(ulong[] playerIds, Action<Player1stPartyPlatformIDsLookupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<Player1stPartyPlatformIDsLookupResponse>());
                return;
            }

            LootLockerAPIManager.LookupPlayer1stPartyPlatformIDs(new LookupPlayer1stPartyPlatformIDsRequest()
            {
                player_ids = playerIds
            }, onComplete);
        }

        /// <summary>
        /// Get players 1st party platform ID's from the provided list of playerID's.
        /// </summary>
        /// <param name="playerPublicUIds">A list of multiple player public UID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type Player1stPartyPlatformIDsLookupResponse</param>
        public static void LookupPlayer1stPartyPlatformIds(string[] playerPublicUIds, Action<Player1stPartyPlatformIDsLookupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<Player1stPartyPlatformIDsLookupResponse>());
                return;
            }

            LootLockerAPIManager.LookupPlayer1stPartyPlatformIDs(new LookupPlayer1stPartyPlatformIDsRequest()
            {
                player_public_uids = playerPublicUIds
            }, onComplete);
        }

        /// <summary>
        /// Get player names of the players from their last active platform by playerID's.
        /// </summary>
        /// <param name="playerIds">A list of multiple player ID's<</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        public static void LookupPlayerNamesByPlayerIds(ulong[] playerIds, Action<PlayerNameLookupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>());
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(new LookupPlayerNamesRequest()
            {
                player_ids = playerIds
            }, onComplete);
        }

        /// <summary>
        /// Get player names of the players from their last active platform by public playerID's.
        /// </summary>
        /// <param name="playerPublicUIds">A list of multiple player public UID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        public static void LookupPlayerNamesByPlayerPublicUIds(string[] playerPublicUIds, Action<PlayerNameLookupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>());
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(new LookupPlayerNamesRequest()
            {
                player_public_uids = playerPublicUIds
            }, onComplete);
        }

        /// <summary>
        /// Get player names of the players from their last active platform by steam ID's. You can read more on how to setup Steam with LootLocker here; https://docs.lootlocker.com/how-to/authentication/steam
        /// </summary>
        /// <param name="steamIds">A list of multiple player Steam ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        public static void LookupPlayerNamesBySteamIds(ulong[] steamIds, Action<PlayerNameLookupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>());
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(new LookupPlayerNamesRequest()
            {
                steam_ids = steamIds
            }, onComplete);
        }

        /// <summary>
        /// Get player names of the players from their last active platform by Steam ID's. You can read more on how to setup Steam with LootLocker here; https://docs.lootlocker.com/how-to/authentication/steam
        /// </summary>
        /// <param name="steamIds">A list of multiple player Steam ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        public static void LookupPlayerNamesBySteamIds(string[] steamIds, Action<PlayerNameLookupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>());
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(new LookupPlayerNamesRequest()
            {
                steam_ids = steamIds.Select(steamId => Convert.ToUInt64(steamId)).ToArray()
            }, onComplete);
        }

        /// <summary>
        /// Get player names of the players from their last active platform by PSN ID's.
        /// </summary>
        /// <param name="psnIds">A list of multiple player PSN ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        public static void LookupPlayerNamesByPSNIds(ulong[] psnIds, Action<PlayerNameLookupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>());
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(new LookupPlayerNamesRequest()
            {
                psn_ids = psnIds
            }, onComplete);
        }

        /// <summary>
        /// Get player names of the players fropm their last active platform by PSN ID's.
        /// </summary>
        /// <param name="psnIds">A list of multiple player PSN ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        public static void LookupPlayerNamesByPSNIds(string[] psnIds, Action<PlayerNameLookupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>());
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(new LookupPlayerNamesRequest()
            {
                psn_ids = psnIds.Select(psnId => Convert.ToUInt64(psnId)).ToArray()
            }, onComplete);
        }

        /// <summary>
        /// Get player names of the players fropm their last active platform by Xbox ID's.
        /// </summary>
        /// <param name="xboxIds">A list of multiple player XBOX ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        public static void LookupPlayerNamesByXboxIds(string[] xboxIds, Action<PlayerNameLookupResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>());
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(new LookupPlayerNamesRequest()
            {
                xbox_ids = xboxIds
            }, onComplete);
        }

        /// <summary>
        /// Set the logged in players name. Max length of a name is 255 characters.
        /// </summary>
        /// <param name="name">The name to set to the currently logged in player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameResponse</param>
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

        #region Player files
        /// <summary>
        /// Returns a URL where you can access the file. You can get the ID of files when you upload them, or call the list endpoint. 
        /// </summary>
        /// <param name="fileId">Id of the file, can be retrieved with GetAllPlayerFiles() or when the file is uploaded</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void GetPlayerFile(int fileId, Action<LootLockerPlayerFile> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getSingleplayerFile.endPoint, fileId);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns all the files that your currently active player own.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFilesResponse</param>
        public static void GetAllPlayerFiles(Action<LootLockerPlayerFilesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFilesResponse>());
                return;
            }

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.getPlayerFiles.endPoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns all public files that the player with the provided playerID owns.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFilesResponse</param>
        public static void GetAllPlayerFiles(int playerId, Action<LootLockerPlayerFilesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFilesResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getPlayerFilesByPlayerId.endPoint, playerId);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Upload a file with the provided name and content. The file will be owned by the currently active player.
        /// </summary>
        /// <param name="pathToFile">Path to the file, example: Application.persistentDataPath + "/" + fileName;</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="isPublic">Should this file be viewable by other players?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void UploadPlayerFile(string pathToFile, string filePurpose, bool isPublic, Action<LootLockerPlayerFile> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>());
                return;
            }

            var body = new Dictionary<string, string>()
            {
                { "purpose", filePurpose },
                { "public", isPublic.ToString().ToLower() }
            };


            var fileBytes = new byte[] { };
            try
            {
                fileBytes = File.ReadAllBytes(pathToFile);
            }
            catch (Exception e)
            {
                DebugMessage($"File error: {e.Message}");
                return;
            }

            LootLockerServerRequest.UploadFile(LootLockerEndPoints.uploadPlayerFile, fileBytes, Path.GetFileName(pathToFile), "multipart/form-data", body,
                onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Serialize(onComplete, serverResponse);
                });
        }

/// 
        /// <summary>
        /// Upload a file with the provided name and content. The file will be owned by the player with the provided playerID.
        /// It will not be viewable by other players.
        /// </summary>
        /// <param name="pathToFile">Path to the file, example: Application.persistentDataPath + "/" + fileName;</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void UploadPlayerFile(string pathToFile, string filePurpose, Action<LootLockerPlayerFile> onComplete)
        {
            UploadPlayerFile(pathToFile, filePurpose, false, onComplete);
        }

        /// <summary>
        /// Upload a file using a Filestream. The file will be owned by the currently active player.
        /// </summary>
        /// <param name="fileStream">Filestream to upload</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="isPublic">Should this file be viewable by other players?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void UploadPlayerFile(FileStream fileStream, string filePurpose, bool isPublic, Action<LootLockerPlayerFile> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>());
                return;
            }

            var body = new Dictionary<string, string>()
            {
                { "purpose", filePurpose },
                { "public", isPublic.ToString().ToLower() }
            };

            var fileBytes = new byte[fileStream.Length];
            try
            {
                fileStream.Read(fileBytes, 0, Convert.ToInt32(fileStream.Length));
            }
            catch (Exception e)
            {
                DebugMessage($"File error: {e.Message}");
                return;
            }

            LootLockerServerRequest.UploadFile(LootLockerEndPoints.uploadPlayerFile, fileBytes, Path.GetFileName(fileStream.Name), "multipart/form-data", body,
                onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Serialize(onComplete, serverResponse);
                });
        }

        /// <summary>
        /// Upload a file using a Filestream. The file will be owned by the currently active player.
        /// </summary>
        /// <param name="fileStream">Filestream to upload</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void UploadPlayerFile(FileStream fileStream, string filePurpose, Action<LootLockerPlayerFile> onComplete)
        {
            UploadPlayerFile(fileStream, filePurpose, false, onComplete);
        }

        /// <summary>
        /// Upload a file using a byte array. Can be useful if you want to upload without storing anything on disk. The file will be owned by the currently active player.
        /// </summary>
        /// <param name="fileBytes">Byte array to upload</param>
        /// <param name="fileName">Name of the file on LootLocker</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="isPublic">Should this file be viewable by other players?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void UploadPlayerFile(byte[] fileBytes, string fileName, string filePurpose, bool isPublic, Action<LootLockerPlayerFile> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>());
                return;
            }

            var body = new Dictionary<string, string>()
            {
                { "purpose", filePurpose },
                { "public", isPublic.ToString().ToLower() }
            };

            LootLockerServerRequest.UploadFile(LootLockerEndPoints.uploadPlayerFile, fileBytes, Path.GetFileName(fileName), "multipart/form-data", body,
                onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Serialize(onComplete, serverResponse);
                });
        }

        /// <summary>
        /// Upload a file using a byte array. Can be useful if you want to upload without storing anything on disk. The file will be owned by the currently active player.
        /// </summary>
        /// <param name="fileBytes">Byte array to upload</param>
        /// <param name="fileName">Name of the file on LootLocker</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void UploadPlayerFile(byte[] fileBytes, string fileName, string filePurpose, Action<LootLockerPlayerFile> onComplete)
        {
            UploadPlayerFile(fileBytes, fileName, filePurpose, false, onComplete);
        }

        /// <summary>
        /// The file will be deleted immediately and the action can not be reversed. You will get the ID of files when you upload a file, or with GetAllPlayerFiles().
        /// </summary>
        /// <param name="fileId">Id of the file. You can get the ID of files when you upload a file, or with GetAllPlayerFiles()</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        public static void DeletePlayerFile(int fileId, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.deletePlayerFile.endPoint, fileId);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.DELETE, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }
        #endregion

        #region Character

        /// /// <summary>
        /// Create a character with the provided type and name. The character will be owned by the currently active player.
        /// Use ListCharacterTypes() to get a list of available character types for your game.
        /// </summary>
        /// <param name="characterTypeId">Use ListCharacterTypes() to get a list of available character types for your game.</param>
        /// <param name="newCharacterName">The new name for the character</param>
        /// <param name="isDefault">Should this character be the default character?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterLoadoutResponse</param>
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

        /// <summary>
        /// List all available character types for your game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerListCharacterTypesResponse</param>
        public static void ListCharacterTypes(Action<LootLockerListCharacterTypesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListCharacterTypesResponse>());
                return;
            }
            LootLockerAPIManager.ListCharacterTypes(onComplete);
        }

        /// <summary>
        /// Get all character loadouts for your game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterLoadoutResponse</param>
        public static void GetCharacterLoadout(Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterLoadoutResponse>());
                return;
            }
            LootLockerAPIManager.GetCharacterLoadout(onComplete);
        }

        /// <summary>
        /// Get a character loadout from a specific characterID.
        /// </summary>
        /// <param name="characterID">ID of the character</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterLoadoutResponse</param>
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

        /// <summary>
        /// Update information about the character. The character must be owned by the currently active player.
        /// </summary>
        /// <param name="characterID">ID of the character</param>
        /// <param name="newCharacterName">New name for the character</param>
        /// <param name="isDefault">Should the character be the default character?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterLoadoutResponse</param>
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

        /// <summary>
        /// Set the character with characterID as the default character for the currently active player. The character must be owned by the currently active player.
        /// </summary>
        /// <param name="characterID">ID of the character</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterLoadoutResponse</param>
        public static void SetDefaultCharacter(string characterID, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterLoadoutResponse>());
                return;
            }

            LootLockerUpdateCharacterRequest data = new LootLockerUpdateCharacterRequest();

            data.is_default = true;

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(characterID);

            LootLockerAPIManager.UpdateCharacter(lootLockerGetRequest, data, onComplete);
        }

        /// <summary>
        /// Equip an asset to the players default character.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance to equip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToCharacterLoadoutResponse</param>
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

        /// <summary>
        /// Equip a global asset to the players default character.
        /// </summary>
        /// <param name="assetId">ID of the asset instance to equip</param>
        /// <param name="assetVariationId">ID of the asset variation to use</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToCharacterLoadoutResponse</param>
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

        /// <summary>
        /// Equip an asset to a specific character. The character must be owned by the currently active player.
        /// </summary>
        /// <param name="characterID">ID of the character</param>
        /// <param name="assetInstanceId">ID of the asset instance to equip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToCharacterLoadoutResponse</param>
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

        /// <summary>
        /// Equip a global asset to a specific character. The character must be owned by the currently active player.
        /// </summary>
        /// <param name="assetId">ID of the asset to equip</param>
        /// <param name="assetVariationId">ID of the variation to use</param>
        /// <param name="characterID">ID of the character to equip the asset to</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToCharacterLoadoutResponse</param>
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

        /// <summary>
        /// Unequip an asset from the players default character.
        /// </summary>
        /// <param name="assetId">ID of the asset to unequip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToCharacterLoadoutResponse</param>
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

        /// <summary>
        /// Unequip an asset from a specific character. The character must be owned by the currently active player.
        /// </summary>
        /// <param name="characterID">ID of the character to unequip</param>
        /// <param name="assetId">Asset instance ID of the asset to unequip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToCharacterLoadoutResponse</param>
        public static void UnEquipIdAssetToCharacter(int characterID, int assetInstanceId, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToCharacterLoadoutResponse>());
                return;
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(characterID.ToString());
            lootLockerGetRequest.getRequests.Add(assetInstanceId.ToString());
            LootLockerAPIManager.UnEquipIdAssetToCharacter(lootLockerGetRequest, onComplete);
        }

        /// <summary>
        /// Get the loadout for the players default character.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetCurrentLoadouttoDefaultCharacterResponse</param>
        public static void GetCurrentLoadOutToDefaultCharacter(Action<LootLockerGetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCurrentLoadouttoDefaultCharacterResponse>());
                return;
            }
            LootLockerAPIManager.GetCurrentLoadOutToDefaultCharacter(onComplete);
        }

        /// <summary>
        /// Get the loadout for a specific character.
        /// </summary>
        /// <param name="characterID">ID of the character to get the loadout for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetCurrentLoadouttoDefaultCharacterResponse</param>
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

        /// <summary>
        /// Get the equippable contexts for the players default character.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerContextResponse</param>
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
        /// <summary>
        /// Get the player storage for the currently active player (key/values).
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStoragResponse</param>
        public static void GetEntirePersistentStorage(Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStoragResponse>());
                return;
            }
            LootLockerAPIManager.GetEntirePersistentStorage(onComplete);
        }

        /// <summary>
        /// Get a specific key from the player storage for the currently active player.
        /// </summary>
        /// <param name="key">Name of the key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentSingle</param>
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

        /// <summary>
        /// Update or create a key/value pair in the player storage for the currently active player.
        /// </summary>
        /// <param name="key">Name of the key</param>
        /// <param name="value">Value of the key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStoragResponse</param>
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

        /// <summary>
        /// Update or create multiple key/value pairs in the player storage for the currently active player.
        /// </summary>
        /// <param name="data">A LootLockerGetPersistentStorageRequest with multiple keys</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStoragResponse</param>
        public static void UpdateOrCreateKeyValue(LootLockerGetPersistentStorageRequest data, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStoragResponse>());
                return;
            }
            LootLockerAPIManager.UpdateOrCreateKeyValue(data, onComplete);
        }

        /// <summary>
        /// Delete a key from the player storage for the currently active player.
        /// </summary>
        /// <param name="keyToDelete">The key/value key(name) to delete</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStoragResponse</param>
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

        /// <summary>
        /// Get the public player storage(key/values) for a specific player.
        /// </summary>
        /// <param name="otherPlayerId">The ID of the player to retrieve the public ley/values for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStoragResponse</param>
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
        /// <summary>
        /// Get the available contexts for the game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerContextResponse</param>
        public static void GetContext(Action<LootLockerContextResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerContextResponse>());
                return;
            }
            LootLockerAPIManager.GetContext(onComplete);
        }

        /// <summary>
        /// Get the available assets for the game. Up to 200 at a time.
        /// </summary>
        /// <param name="assetCount">Amount of assets to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetResponse</param>
        /// <param name="idOfLastAsset">Set this to stop getting assets after a certain asset</param>
        /// <param name="filter">A list of LootLocker.LootLockerEnums.AssetFilter to get just specific assets.</param>
        /// <param name="includeUGC">Should User Generated Content be included in this response?</param>
        /// <param name="assetFilters">A Dictionary<string, string> of custom filters to use when retrieving assets</param>
        /// <param name="UGCCreatorPlayerID">Only get assets created by a specific player</param>
        public static void GetAssetsOriginal(int assetCount, Action<LootLockerAssetResponse> onComplete, int? idOfLastAsset = null, List<LootLocker.LootLockerEnums.AssetFilter> filter = null, bool includeUGC = false, Dictionary<string, string> assetFilters = null, int UGCCreatorPlayerID = 0)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>());
                return;
            }
            LootLockerAPIManager.GetAssetsOriginal(onComplete, assetCount, idOfLastAsset, filter, includeUGC, assetFilters, UGCCreatorPlayerID);
        }

        /// <summary>
        /// Get the available assets for the game. Up to 200 at a time. Includes amount of assets in the response.
        /// </summary>
        /// <param name="assetCount">Amount of assets to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetResponse</param>
        /// <param name="filter">A list of LootLocker.LootLockerEnums.AssetFilter to get just specific assets.</param>
        /// <param name="includeUGC">Should User Generated Content be included in this response?</param>
        /// <param name="assetFilters">A Dictionary<string, string> of custom filters to use when retrieving assets</param>
        /// <param name="UGCCreatorPlayerID">Only get assets created by a specific player</param>
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

        /// <summary>
        /// Get the next set of assets after a previous call to GetAssetsOriginal or GetAssetListWithCount.
        /// </summary>
        /// <param name="assetCount">Amount of assets to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetResponse</param>
        /// <param name="filter">A list of LootLocker.LootLockerEnums.AssetFilter to get just specific assets.</param>
        /// <param name="includeUGC">Should User Generated Content be included in this response?</param>
        /// <param name="assetFilters">A Dictionary<string, string> of custom filters to use when retrieving assets</param>
        /// <param name="UGCCreatorPlayerID">Only get assets created by a specific player</param>
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

        /// <summary>
        /// Reset the last id used in GetAssetNextList().
        /// </summary>
        public static void ResetAssetCalls()
        {
            LootLockerAssetRequest.lastId = 0;
        }
        
        [Obsolete("This function will soon be removed. Use GetAssetInformation(int assetId, Action<LootLockerCommonAsset> onComplete) with int parameter instead")]
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

        /// <summary>
        /// Get information about a specific asset.
        /// </summary>
        /// <param name="assetId">The ID of the asset that you want information about</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSingleAssetResponse</param>
        public static void GetAssetInformation(int assetId, Action<LootLockerSingleAssetResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSingleAssetResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();

            data.getRequests.Add(assetId.ToString());

            // Using GetAssetByID in the background
            LootLockerAPIManager.GetAssetById(data, onComplete);
        }

        /// <summary>
        /// List the current players favorite assets.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerFavouritesListResponse</param>
        public static void ListFavouriteAssets(Action<LootLockerFavouritesListResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFavouritesListResponse>());
                return;
            }
            LootLockerAPIManager.ListFavouriteAssets(onComplete);
        }

        /// <summary>
        /// Add an asset to the current players favorite assets.
        /// </summary>
        /// <param name="assetId">The ID of the asset to add to favourites</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetResponse</param>
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

        /// <summary>
        /// Remove an asset from the current players favorite assets.
        /// </summary>
        /// <param name="assetId">The ID of the asset to remove from favourites</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetResponse</param>
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

        /// <summary>
        /// Get multiple assets by their IDs.
        /// </summary>
        /// <param name="assetIdsToRetrieve">A list of multiple assets to retrieve</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetResponse</param>
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
        /// <summary>
        /// Get all key/value pairs for all asset instances.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllKeyValuePairsResponse</param>
        public static void GetAllKeyValuePairsForAssetInstances(Action<LootLockerGetAllKeyValuePairsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetAllKeyValuePairsResponse>());
                return;
            }
            LootLockerAPIManager.GetAllKeyValuePairs(onComplete);
        }

        /// <summary>
        /// Get all key/value pairs for a specific asset instance.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to get the key/value-pairs for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        public static void GetAllKeyValuePairsToAnInstance(int assetInstanceID, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.GetAllKeyValuePairsToAnInstance(data, onComplete);
        }

        /// <summary>
        /// Get a specific key/value pair for a specific asset instance.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to get the key/value-pairs for</param>
        /// <param name="keyValueID">The ID of the key-value to get. Can be obtained when creating a new key/value-pair or with GetAllKeyValuePairsToAnInstance()</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetSingleKeyValuePairsResponse</param>
        public static void GetAKeyValuePairByIdForAssetInstances(int assetInstanceID, int keyValueID, Action<LootLockerGetSingleKeyValuePairsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetSingleKeyValuePairsResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            data.getRequests.Add(keyValueID.ToString());
            LootLockerAPIManager.GetAKeyValuePairById(data, onComplete);
        }

        /// <summary>
        /// Create a new key/value pair for a specific asset instance.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to create the key/value for</param>
        /// <param name="key">Key(name)</param>
        /// <param name="value">The value of the key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        public static void CreateKeyValuePairForAssetInstances(int assetInstanceID, string key, string value, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerCreateKeyValuePairRequest createKeyValuePairRequest = new LootLockerCreateKeyValuePairRequest();
            createKeyValuePairRequest.key = key;
            createKeyValuePairRequest.value = value;
            LootLockerAPIManager.CreateKeyValuePair(data, createKeyValuePairRequest, onComplete);
        }

        /// <summary>
        /// Update a specific key/value pair for a specific asset instance. Data is provided as key/value pairs.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to create the key/value for</param>
        /// <param name="data">A Dictionary<string, string> for multiple key/values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        public static void UpdateOneOrMoreKeyValuePairForAssetInstances(int assetInstanceID, Dictionary<string, string> data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest request = new LootLockerGetRequest();
            request.getRequests.Add(assetInstanceID.ToString());
            LootLockerUpdateOneOrMoreKeyValuePairRequest createKeyValuePairRequest = new LootLockerUpdateOneOrMoreKeyValuePairRequest();
            List<LootLockerCreateKeyValuePairRequest> temp = new List<LootLockerCreateKeyValuePairRequest>();
            foreach (var d in data)
            {
                temp.Add(new LootLockerCreateKeyValuePairRequest { key = d.Key, value = d.Value });
            }
            createKeyValuePairRequest.storage = temp.ToArray();
            LootLockerAPIManager.UpdateOneOrMoreKeyValuePair(request, createKeyValuePairRequest, onComplete);
        }
        /// <summary>
        /// Update a specific key/value pair for a specific asset instance by key.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to update the key/value for</param>
        /// <param name="key">Name of the key to update</param>
        /// <param name="value">The new value of the key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        public static void UpdateKeyValuePairForAssetInstances(int assetInstanceID, string key, string value, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest request = new LootLockerGetRequest();
            request.getRequests.Add(assetInstanceID.ToString());
            LootLockerUpdateOneOrMoreKeyValuePairRequest createKeyValuePairRequest = new LootLockerUpdateOneOrMoreKeyValuePairRequest();
            List<LootLockerCreateKeyValuePairRequest> temp = new List<LootLockerCreateKeyValuePairRequest>();
            temp.Add(new LootLockerCreateKeyValuePairRequest { key = key, value = value });
            createKeyValuePairRequest.storage = temp.ToArray();
            LootLockerAPIManager.UpdateOneOrMoreKeyValuePair(request, createKeyValuePairRequest, onComplete);
        }
        [ObsoleteAttribute("This function will be removed soon, use this function with 4 parameters instead:\n(int assetInstanceID, int keyValueID, string value, string key, Action<LootLockerAssetDefaultResponse> onComplete)")]
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

/// 
        /// <summary>
        /// Update a specific key/value pair for a specific asset instance by key/value-id.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to update the key/value for</param>
        /// <param name="keyValueID">ID of the key/value, can be obtained when creating the key or by using GetAllKeyValuePairsToAnInstance()</param>
        /// <param name="value">The new value of the key</param>
        /// <param name="key">The new key(name) of the key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        public static void UpdateKeyValuePairByIdForAssetInstances(int assetInstanceID, int keyValueID, string value, string key, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            data.getRequests.Add(keyValueID.ToString());
            LootLockerCreateKeyValuePairRequest createKeyValuePairRequest = new LootLockerCreateKeyValuePairRequest();
            if (key != null)
            {
                createKeyValuePairRequest.key = key;
            }
            if (value != null)
            {
                createKeyValuePairRequest.value = value;
            }
            LootLockerAPIManager.UpdateKeyValuePairById(data, createKeyValuePairRequest, onComplete);
        }

        /// <summary>
        /// Delete a specific key/value pair for a specific asset instance by key/value-id.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to delete the key/value for</param>
        /// <param name="keyValueID">ID of the key/value, can be obtained when creating the key or by using GetAllKeyValuePairsToAnInstance()</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        public static void DeleteKeyValuePairForAssetInstances(int assetInstanceID, int keyValueID, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            data.getRequests.Add(keyValueID.ToString());
            LootLockerAPIManager.DeleteKeyValuePair(data, onComplete);
        }

        /// <summary>
        /// Get the drop rates for a loot box asset instance.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID of the loot box</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInspectALootBoxResponse</param>
        public static void InspectALootBoxForAssetInstances(int assetInstanceID, Action<LootLockerInspectALootBoxResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInspectALootBoxResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.InspectALootBox(data, onComplete);
        }

        /// <summary>
        /// Open a loot box asset instance. The loot box will be consumed and the contents will be added to the player's inventory.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID of the loot box</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerOpenLootBoxResponse</param>
        public static void OpenALootBoxForAssetInstances(int assetInstanceID, Action<LootLockerOpenLootBoxResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerOpenLootBoxResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.OpenALootBox(data, onComplete);
        }
        #endregion

        #region UserGeneratedContent
        /// <summary>
        /// Internal function to convert convert asset dictionaries to a different format.
        /// </summary>
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

        /// <summary>
        /// Create a new asset candidate.
        /// </summary>
        /// <param name="name">Name of the asset candidate</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerUserGenerateContentResponse</param>
        /// <param name="kv_storage">(Optional) Dictionary<string, string> of key-values to use</param>
        /// <param name="filters">(Optional) Dictionary<string, string> of key-values that can be used to filter out assets</param>
        /// <param name="data_entities">(Optional) Dictionary<string, string> of data to include in the asset candidate</param>
        /// <param name="context_id">(Optional) ID of the context to use when promoting to an asset, will be automatically filled if not provided</param>
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

        /// <summary>
        /// Update an existing asset candidate.
        /// </summary>
        /// <param name="assetId">ID of the asset candidate to update</param>
        /// <param name="isCompleted">If true, the asset candidate will become an asset and can not be edited any more.</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerUserGenerateContentResponse</param>
        /// <param name="name">(Optional) New name of the asset candidate</param>
        /// <param name="kv_storage">(Optional)A Dictionary<string, string> of key-values to use</param>
        /// <param name="filters">(Optional)A Dictionary<string, string> of key-values that can be used to filter out assets</param>
        /// <param name="data_entities">(Optional)A Dictionary<string, string> of data to include in the asset candidate</param>
        /// <param name="context_id">(Optional)An ID of the context to use when promoting to an asset, will be automatically filled if not provided</param>
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

        /// <summary>
        /// Delete an asset candidate.
        /// </summary>
        /// <param name="assetCandidateID">ID of the asset candidate to delete</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerUserGenerateContentResponse</param>
        public static void DeletingAnAssetCandidate(int assetCandidateID, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetCandidateID.ToString());
            LootLockerAPIManager.DeletingAnAssetCandidate(data, onComplete);
        }

        /// <summary>
        /// Get information about a single asset candidate.
        /// </summary>
        /// <param name="assetCandidateID">The ID of the asset candidate to receive information about</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerUserGenerateContentResponse</param>
        public static void GettingASingleAssetCandidate(int assetCandidateID, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetCandidateID.ToString());
            LootLockerAPIManager.GettingASingleAssetCandidate(data, onComplete);
        }

        /// <summary>
        /// Get all asset candidates for the current player.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerListingAssetCandidatesResponse</param>
        public static void ListingAssetCandidates(Action<LootLockerListingAssetCandidatesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListingAssetCandidatesResponse>());
                return;
            }
            LootLockerAPIManager.ListingAssetCandidates(onComplete);
        }

        /// <summary>
        /// Add a file to an asset candidate.
        /// </summary>
        /// <param name="assetCandidateID">The ID of the asset candidate to add a file to</param>
        /// <param name="filePath">Path to the file, example: Application.persistentDataPath + "/" + fileName;</param>
        /// <param name="fileName">File name of the file on LootLockers Server</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerUserGenerateContentResponse</param>
        /// <param name="fileContentType">Special use-case for some files, leave blank unless you know what you're doing</param>
        public static void AddingFilesToAssetCandidates(int assetCandidateID, string filePath, string fileName,
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

            getRequest.getRequests.Add(assetCandidateID.ToString());

            LootLockerAPIManager.AddingFilesToAssetCandidates(data, getRequest, onComplete);
        }

        /// <summary>
        /// Remove a file from an asset candidate.
        /// </summary>
        /// <param name="assetCandidateID">ID of the asset instance to remove the file from</param>
        /// <param name="fileId">ID of the file to remove</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerUserGenerateContentResponse</param>
        public static void RemovingFilesFromAssetCandidates(int assetCandidateID, int fileId, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>());
                return;
            }

            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetCandidateID.ToString());
            data.getRequests.Add(fileId.ToString());

            LootLockerAPIManager.RemovingFilesFromAssetCandidates(data, onComplete);
        }
        #endregion

        #region Missions
        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetAllMissions() instead")]
        public static void GettingAllMissions(Action<LootLockerGettingAllMissionsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGettingAllMissionsResponse>());
                return;
            }
            LootLockerAPIManager.GettingAllMissions(onComplete);
        }

        /// <summary>
        /// Get all available missions for the current game. Missions are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Missions here; https://docs.lootlocker.com/background/game-systems#missions
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGettingAllMissionsResponse</param>
        public static void GetAllMissions(Action<LootLockerGetAllMissionsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetAllMissionsResponse>());
                return;
            }
            LootLockerAPIManager.GetAllMissions(onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetMission() instead")]
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

        /// <summary>
        /// Get information about a single mission. Missions are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Missions here; https://docs.lootlocker.com/background/game-systems#missions
        /// </summary>
        /// <param name="missionId">The ID of the mission to get information about</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGettingASingleMissionResponse</param>
        public static void GetMission(int missionId, Action<LootLockerGetMissionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMissionResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.GetMission(data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function StartMission() instead")]
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

        /// <summary>
        /// Start a mission for the current player. Missions are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Missions here; https://docs.lootlocker.com/background/game-systems#missions
        /// </summary>
        /// <param name="missionId">The ID of the mission to start</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerStartingAMissionResponse</param>
        public static void StartMission(int missionId, Action<LootLockerStartMissionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStartMissionResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.StartMission(data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function FinishMission() instead")]
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

        /// <summary>
        /// Finish a mission for the current player. Missions are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Missions here; https://docs.lootlocker.com/background/game-systems#missions
        /// </summary>
        /// <param name="missionId">The ID of the mission to start</param>
        /// <param name="startingMissionSignature">Mission signature is received when starting a mission</param>
        /// <param name="playerId">ID of the current player</param>
        /// <param name="finishingPayload">A LootLockerFinishingPayload with variables for how the mission was completed</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerFinishingAMissionResponse</param>
        public static void FinishMission(int missionId, string startingMissionSignature, string playerId,
            LootLockerFinishingPayload finishingPayload, Action<LootLockerFinishMissionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFinishMissionResponse>());
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

            LootLockerFinishMissionRequest data = new LootLockerFinishMissionRequest()
            {
                signature = hash,
                payload = finishingPayload
            };
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.FinishMission(data, onComplete);
        }
        #endregion

        #region Maps
        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetAllMaps() instead.")]
        public static void GettingAllMaps(Action<LootLockerMapsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerMapsResponse>());
                return;
            }
            LootLockerAPIManager.GettingAllMaps(onComplete);
        }
        /// <summary>
        /// Get all available maps for the current game. Maps are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Maps here; https://docs.lootlocker.com/background/game-systems#maps
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerMapsResponse</param>
        public static void GetAllMaps(Action<LootLockerMapsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerMapsResponse>());
                return;
            }
            LootLockerAPIManager.GetAllMaps(onComplete);
        }
        #endregion

        #region Purchasing
        /// <summary>
        /// Purchase an asset. If your game uses soft currency, it will check the players account balance and grant the assets to the player if there is coverage. For rental Assets use RentalPurchaseCall() instead.
        /// </summary>
        /// <param name="assetID">The ID of the asset to purchase</param>
        /// <param name="variationID">The variation ID of the asset to purchase</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPurchaseResponse</param>
        public static void NormalPurchaseCall(int assetID, int variationID, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseResponse>());
                return;
            }
            LootLockerNormalPurchaseRequest data = new LootLockerNormalPurchaseRequest { asset_id = assetID, variation_id = variationID };
            List<LootLockerNormalPurchaseRequest> datas = new List<LootLockerNormalPurchaseRequest>();
            datas.Add(data);
            LootLockerAPIManager.NormalPurchaseCall(datas.ToArray(), onComplete);
        }

        /// <summary>
        /// Purchase a rental asset. If your game uses soft currency, it will check the players account balance and grant the assets to the player if there is coverage. For non-rental Assets use NormalPurchaseCall() instead.
        /// </summary>
        /// <param name="assetID">The ID of the asset to purchase</param>
        /// <param name="variationID">The variation ID of the asset to purchase</param>
        /// <param name="rentalOptionID">The rental option ID of the asset to purchase</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPurchaseResponse</param>
        public static void RentalPurchaseCall(int assetID, int variationID, int rentalOptionID, Action<LootLockerPurchaseResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseResponse>());
                return;
            }
            LootLockerRentalPurchaseRequest data = new LootLockerRentalPurchaseRequest { asset_id = assetID, variation_id = variationID, rental_option_id = rentalOptionID };
            LootLockerAPIManager.RentalPurchaseCall(data, onComplete);
        }

        /// <summary>
        /// Provide a receipt that you get from Apple to validate the purchase. The item will be granted to the player if the receipt is valid.
        /// </summary>
        /// <param name="receipt_data">Receipt that is received when a purchase goes through with apple</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPurchaseResponse</param>
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

        /// <summary>
        /// Provide a purchase_token that you get from Google when making an In-App purchase. The item will be granted to the player if the purchase_token is valid.
        /// </summary>
        /// <param name="purchase_token">The token that is received when a purchase has been made</param>
        /// <param name="asset_id">The ID of the asset to purchase</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPurchaseResponse</param>
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

        [Obsolete("This function is deprecated and will be removed soon. Please use the function PollOrderStatus() instead")]
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

        /// <summary>
        /// This will give you the current status of a purchase. These statuses can be returned;
        /// open - The order is being processed
        /// closed - The order have been processed successfully
        /// refunded - The order has been refunded
        /// canceled - The order has been canceled
        /// failed - The order failed
        /// </summary>
        /// <param name="assetId">The ID of the asset to check the status for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterLoadoutResponse</param>
        public static void PollOrderStatus(int assetId, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterLoadoutResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.PollOrderStatus(data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function ActivateRentalAsset() instead")]
        public static void ActivatingARentalAsset(int assetInstanceID, Action<LootLockerActivateARentalAssetResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerActivateARentalAssetResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.ActivatingARentalAsset(data, onComplete);
        }

        /// <summary>
        /// Activate a rental asset. This will grant the asset to the player and start the rental timer on the server.
        /// </summary>
        /// <param name="assetId">The asset instance ID of the asset to activate</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerActivateARentalAssetResponse</param>
        public static void ActivateRentalAsset(int assetInstanceID, Action<LootLockerActivateRentalAssetResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerActivateRentalAssetResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.ActivateRentalAsset(data, onComplete);
        }
        #endregion

        #region Collectables
        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetCollectables() instead")]
        public static void GettingCollectables(Action<LootLockerGettingCollectablesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGettingCollectablesResponse>());
                return;
            }
            LootLockerAPIManager.GettingCollectables(onComplete);
        }

        /// <summary>
        /// Get all collectables for the game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGettingCollectablesResponse</param>
        public static void GetCollectables(Action<LootLockerGetCollectablesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCollectablesResponse>());
                return;
            }
            LootLockerAPIManager.GetCollectables(onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function CollectItem() instead")]
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

        /// <summary>
        /// Collect a collectable item. This will grant the collectable to the player.
        /// </summary>
        /// <param name="slug">A string representing what was collected, example; Carsdriven.Bugs.Dune</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCollectingAnItemResponse</param>
        public static void CollectItem(string slug, Action<LootLockerCollectItemResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCollectItemResponse>());
                return;
            }
            LootLockerCollectingAnItemRequest data = new LootLockerCollectingAnItemRequest();
            data.slug = slug;
            LootLockerAPIManager.CollectItem(data, onComplete);
        }

        #endregion

        #region Messages

        /// <summary>
        /// Get the current messages.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetMessagesResponse</param>
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
        [Obsolete("This function is deprecated and will be removed soon. Please use the function ExecuteTrigger() instead")]
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

        /// <summary>
        /// Execute a trigger. This will grant the player any items that are attached to the trigger.
        /// </summary>
        /// <param name="eventName">Name of the trigger to execute</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerTriggerAnEventResponse</param>
        public static void ExecuteTrigger(string triggerName, Action<LootLockerExecuteTriggerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerExecuteTriggerResponse>());
                return;
            }
            LootLockerExecuteTriggerRequest data = new LootLockerExecuteTriggerRequest { name = triggerName };
            LootLockerAPIManager.ExecuteTrigger(data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function ListExecutedTriggers() instead")]
        public static void ListingTriggeredTriggerEvents(Action<LootLockerListingAllTriggersResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListingAllTriggersResponse>());
                return;
            }
            LootLockerAPIManager.ListingTriggeredTriggerEvents(onComplete);
        }

        /// <summary>
        ///  Lists the triggers that a player have already executed.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerListingAllTriggersResponse</param>
        public static void ListExecutedTriggers(Action<LootLockerListAllTriggersResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListAllTriggersResponse>());
                return;
            }
            LootLockerAPIManager.ListAllExecutedTriggers(onComplete);
        }

        #endregion

        #region Leaderboard
        /// <summary>
        /// Get the current ranking for a specific player on a leaderboard.
        /// </summary>
        /// <param name="leaderboardId">ID or key of the leaderboard as a string</param>
        /// <param name="member_id">ID of the player as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetMemberRankResponse</param>
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

        /// <summary>
        /// Get the current ranking for a specific player on a leaderboard.
        /// </summary>
        /// <param name="leaderboardId">ID of the leaderboard</param>
        /// <param name="member_id">ID of the player as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetMemberRankResponse</param>
        public static void GetMemberRank(int leaderboardId, string member_id, Action<LootLockerGetMemberRankResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMemberRankResponse>());
                return;
            }
            LootLockerGetMemberRankRequest lootLockerGetMemberRankRequest = new LootLockerGetMemberRankRequest();


            lootLockerGetMemberRankRequest.leaderboardId = leaderboardId.ToString();
            lootLockerGetMemberRankRequest.member_id = member_id;

            LootLockerAPIManager.GetMemberRank(lootLockerGetMemberRankRequest, onComplete);
        }

        /// <summary>
        /// Get the current ranking for a specific player on a leaderboard.
        /// </summary>
        /// <param name="leaderboardId">ID of the leaderboard as an int</param>
        /// <param name="member_id">ID of the player as an int</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetMemberRankResponse</param>
        public static void GetMemberRank(int leaderboardId, int member_id, Action<LootLockerGetMemberRankResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMemberRankResponse>());
                return;
            }
            LootLockerGetMemberRankRequest lootLockerGetMemberRankRequest = new LootLockerGetMemberRankRequest();

            lootLockerGetMemberRankRequest.leaderboardId = leaderboardId.ToString();
            lootLockerGetMemberRankRequest.member_id = member_id.ToString();

            LootLockerAPIManager.GetMemberRank(lootLockerGetMemberRankRequest, onComplete);
        }

        /// <summary>
        /// Get the current ranking for a specific player on a leaderboard.
        /// </summary>
        /// <param name="leaderboardId">ID or key of the leaderboard as a string</param>
        /// <param name="member_id">ID of the player as an int</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetMemberRankResponse</param>
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

        /// <summary>
        /// Get the current ranking for several members on a specific leaderboard.
        /// </summary>
        /// <param name="members">List of members to get as string</param>
        /// <param name="leaderboardId">ID of the leaderboard as an int</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetByListOfMembersResponse</param>
        public static void GetByListOfMembers(string[] members, int leaderboardId, Action<LootLockerGetByListOfMembersResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetByListOfMembersResponse>());
                return;
            }
            LootLockerGetByListMembersRequest request = new LootLockerGetByListMembersRequest();

            request.members = members;

            LootLockerAPIManager.GetByListOfMembers(request, leaderboardId.ToString(), onComplete);
        }

        /// <summary>
        /// Get the current ranking for several members on a specific leaderboard.
        /// </summary>
        /// <param name="members">List of members to get as string</param>
        /// <param name="leaderboardKey">ID or key of the leaderboard as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetByListOfMembersResponse</param>
        public static void GetByListOfMembers(string[] members, string leaderboardKey, Action<LootLockerGetByListOfMembersResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetByListOfMembersResponse>());
                return;
            }
            LootLockerGetByListMembersRequest request = new LootLockerGetByListMembersRequest();

            request.members = members;

            LootLockerAPIManager.GetByListOfMembers(request, leaderboardKey, onComplete);
        }

        /// <summary>
        /// Get the current ranking for a member on all available leaderboards.
        /// </summary>
        /// <param name="member_id">The ID of the player to check</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">How many extra rows after the returned position</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllMemberRanksResponse</param>
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

        /// <summary>
        /// Get the current ranking for a member on all available leaderboards.
        /// </summary>
        /// <param name="member_id">The ID of the player to check</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllMemberRanksResponse</param>
        public static void GetAllMemberRanks(int member_id, int count, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            GetAllMemberRanksMain(member_id, count, -1, onComplete);
        }

        /// <summary>
        /// Get the next current rankings for a member on all available leaderboards.
        /// </summary>
        /// <param name="member_id">The ID of the player to check</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllMemberRanksResponse</param>
        public static void GetAllMemberRanksNext(int member_id, int count, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            GetAllMemberRanksMain(member_id, count, int.Parse(LootLockerGetAllMemberRanksRequest.nextCursor.ToString()), onComplete);
        }

        /// <summary>
        /// Get the previous ranking for a member on all available leaderboards.
        /// </summary>
        /// <param name="member_id">The ID of the player to check</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllMemberRanksResponse</param>
        public static void GetAllMemberRanksPrev(int member_id, int count, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            GetAllMemberRanksMain(member_id, count, int.Parse(LootLockerGetAllMemberRanksRequest.prevCursor.ToString()), onComplete);
        }

        /// <summary>
        /// Reset the calls for getting all member ranks.
        /// </summary>
        public static void ResetAllMemberRanksCalls()
        {
            LootLockerGetAllMemberRanksRequest.Reset();
        }

        /// <summary>
        /// Get the current ranking for a member on all available leaderboards.
        /// </summary>
        /// <param name="member_id">The ID of the player to check</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">How many extra rows after the players position</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllMemberRanksResponse</param>
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

        [Obsolete("This function is deprecated and will be removed soon. Please use GetScoreList instead.")]
        public static void GetScoreListMain(int leaderboardId, int count, int after, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreList(leaderboardId, count, after, onComplete);
        }

        /// <summary>
        /// Get the entries for a specific leaderboard.
        /// </summary>
        /// <param name="leaderboardId">ID of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="after">How many after the last entry to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetScoreList(int leaderboardId, int count, int after, Action<LootLockerGetScoreListResponse> onComplete)
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

        /// <summary>
        /// Get the entries for a specific leaderboard.
        /// </summary>
        /// <param name="leaderboardId">ID of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetScoreList(int leaderboardId, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreList(leaderboardId, count, -1, onComplete);
        }

        /// <summary>
        /// Get the entries for a specific leaderboard.
        /// </summary>
        /// <param name="leaderboardKey">ID or key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="after">How many after the last entry to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetScoreList(string leaderboardKey, int count, int after, Action<LootLockerGetScoreListResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetScoreListResponse>());
                return;
            }
            LootLockerGetScoreListRequest request = new LootLockerGetScoreListRequest();
            request.leaderboardKey = leaderboardKey;
            request.count = count;
            request.after = after > 0 ? after.ToString() : "0";
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

        /// <summary>
        /// Get the entries for a specific leaderboard.
        /// </summary>
        /// <param name="leaderboardKey">ID or key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetScoreList(string leaderboardKey, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreList(leaderboardKey, count, -1, onComplete);
        }

        /// <summary>
        /// Get the next entries for a specific leaderboard. Can be called after GetScoreList.
        /// </summary>
        /// <param name="leaderboardId">ID of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetNextScoreList(int leaderboardId, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreList(leaderboardId, count, int.Parse(LootLockerGetScoreListRequest.nextCursor.ToString()), onComplete);
        }

        /// <summary>
        /// Get the next entries for a specific leaderboard. Can be called after GetScoreList.
        /// </summary>
        /// <param name="leaderboardKey">ID or key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetNextScoreList(string leaderboardKey, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreList(leaderboardKey, count, int.Parse(LootLockerGetScoreListRequest.nextCursor.ToString()), onComplete);
        }

        /// <summary>
        /// Get the previous entries for a specific leaderboard. Can be called after GetScoreList or GetNextScoreList.
        /// </summary>
        /// <param name="leaderboardId">ID of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetPrevScoreList(int leaderboardId, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreList(leaderboardId, count, int.Parse(LootLockerGetScoreListRequest.prevCursor.ToString()), onComplete);
        }

        /// <summary>
        /// Get the previous entries for a specific leaderboard. Can be called after GetScoreList or GetNextScoreList.
        /// </summary>
        /// <param name="leaderboardKey">ID of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetPrevScoreList(string leaderboardKey, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreList(leaderboardKey, count, int.Parse(LootLockerGetScoreListRequest.prevCursor.ToString()), onComplete);
        }

        /// <summary>
        /// Reset the next and previous cursors for the GetScoreList and GetNextScoreList methods.
        /// </summary>
        public static void ResetScoreCalls()
        {
            LootLockerGetScoreListRequest.Reset();
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use GetScoreList instead.")]
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

        /// <summary>
        /// Submit a score to a leaderboard.
        /// </summary>
        /// <param name="memberId">Can be left blank if it is a player leaderboard, otherwise an identifier for the player</param>
        /// <param name="score">The score to upload</param>
        /// <param name="leaderboardId">ID of the leaderboard to submit score to</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSubmitScoreResponse</param>
        public static void SubmitScore(string memberId, int score, int leaderboardId, Action<LootLockerSubmitScoreResponse> onComplete)
        {
            SubmitScore(memberId, score, leaderboardId.ToString(), "", onComplete);
        }

        /// <summary>
        /// Submit a score to a leaderboard with additional metadata.
        /// </summary>
        /// <param name="memberId">Can be left blank if it is a player leaderboard, otherwise an identifier for the player</param>
        /// <param name="score">The score to upload</param>
        /// <param name="leaderboardId">ID of the leaderboard to submit score to</param>
        /// <param name="metadata">Additional metadata to add to the score</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSubmitScoreResponse</param>
        public static void SubmitScore(string memberId, int score, int leaderboardId, string metadata, Action<LootLockerSubmitScoreResponse> onComplete)
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

            LootLockerAPIManager.SubmitScore(request, leaderboardId.ToString(), onComplete);
        }

        /// <summary>
        /// Submit a score to a leaderboard.
        /// </summary>
        /// <param name="memberId">Can be left blank if it is a player leaderboard, otherwise an identifier for the player</param>
        /// <param name="score">The score to upload</param>
        /// <param name="leaderboardKey">ID or key of the leaderboard to submit score to</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSubmitScoreResponse</param>
        public static void SubmitScore(string memberId, int score, string leaderboardKey, Action<LootLockerSubmitScoreResponse> onComplete)
        {
            SubmitScore(memberId, score, leaderboardKey, "", onComplete);
        }

        /// <summary>
        /// Submit a score to a leaderboard with additional metadata.
        /// </summary>
        /// <param name="memberId">Can be left blank if it is a player leaderboard, otherwise an identifier for the player</param>
        /// <param name="score">The score to upload</param>
        /// <param name="leaderboardKey">ID or key of the leaderboard to submit score to</param>
        /// <param name="metadata">Additional metadata to add to the score</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSubmitScoreResponse</param>
        public static void SubmitScore(string memberId, int score, string leaderboardKey, string metadata, Action<LootLockerSubmitScoreResponse> onComplete)
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

            LootLockerAPIManager.SubmitScore(request, leaderboardKey, onComplete);
        }

        #endregion

        /// <summary>
        /// Lock a drop table and return information about the assets that were computed.
        /// </summary>
        /// <param name="tableInstanceId">Asset instance ID of the drop table to compute</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerComputeAndLockDropTableResponse</param>
        /// <param name="AddAssetDetails">If true, return additional information about the asset</param>
        /// <param name="tag">Specific tag to use</param>
        public static void ComputeAndLockDropTable(int tableInstanceId, Action<LootLockerComputeAndLockDropTableResponse> onComplete, bool AddAssetDetails = false, string tag = "")
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerComputeAndLockDropTableResponse>());
                return;
            }
            LootLockerAPIManager.ComputeAndLockDropTable(tableInstanceId, onComplete, AddAssetDetails, tag);
        }

        /// <summary>
        /// Send a list of id's from a ComputeAndLockDropTable()-call to grant the assets to the player.
        /// </summary>
        /// <param name="picks">A list of the ID's of the picks to choose</param>
        /// <param name="tableInstanceId">Asset instance ID of the drop table to pick from</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPickDropsFromDropTableResponse</param>
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


        #region Reports

        /// <summary>
        /// Retrieves the different types of report possible.
        /// These can be changed in the web interface or through the Admin API.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerReportsGetTypesResponse</param>
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
        /// Create a report of a player.
        /// </summary>
        /// <param name="input">The report to upload</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerReportsCreatePlayerResponse</param>
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
        /// Create a report of an asset.
        /// </summary>
        /// <param name="input">The report to upload</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerReportsCreateAssetResponse</param>
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
        /// If any of their UGC has been removed as a result of reports they will be returned in this method.
        /// </summary>
        /// <param name="input">The report to upload</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerReportsGetRemovedAssetsResponse</param>
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

        /// <summary>
        /// Ping the server, contains information about the current time of the server.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPingResponse</param>
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
