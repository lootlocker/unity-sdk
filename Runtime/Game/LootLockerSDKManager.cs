using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using LootLocker.LootLockerEnums;
using System.Linq;
using System.Security.Cryptography;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LootLocker.Requests
{
    public partial class LootLockerSDKManager
    {
#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            initialized = false;
        }
#endif

        /// <summary>
        /// Stores which platform the player currently has a session for.
        /// </summary>
        public static string GetCurrentPlatform()
        {
            return CurrentPlatform.GetString();
        }

        #region Init

        static bool initialized;
        static bool Init()
        {
            LootLockerServerApi.Instantiate();
            return LoadConfig();
        }

        /// <summary>
        /// Manually initialize the SDK.
        /// </summary>
        /// <param name="apiKey">Find the Game API-key at https://my.lootlocker.io/settings/game and click on the API-tab</param>
        /// <param name="gameVersion">The current version of the game in the format 1.2.3.4 (the 3 and 4 being optional but recommended)</param>
        /// <param name="domainKey">Extra key needed for some endpoints, can be found by going to https://my.lootlocker.io/settings/game and click on the API-tab</param>
        /// <returns>True if initialized successfully, false otherwise</returns>
        public static bool Init(string apiKey, string gameVersion, string domainKey)
        {
            LootLockerServerApi.Instantiate();
            return LootLockerConfig.CreateNewSettings(apiKey, gameVersion, domainKey);
        }

        static bool LoadConfig()
        {
            initialized = false;
            if (LootLockerConfig.current == null)
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)("SDK could not find settings, please contact support \n You can also set config manually by calling Init(string apiKey, string gameVersion, bool onDevelopmentMode, string domainKey)");
                return false;
            }
            if (string.IsNullOrEmpty(LootLockerConfig.current.apiKey))
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)("API Key has not been set, set it in project settings or manually calling Init(string apiKey, string gameVersion, bool onDevelopmentMode, string domainKey)");
                return false;
            }
            
            initialized = true;
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
        /// Utility function to check if the sdk has been initialized
        /// </summary>
        /// <returns>True if initialized, false otherwise.</returns>
        public static bool CheckInitialized(bool skipSessionCheck = false)
        {
            if (!initialized)
            {
                LootLockerConfig.current.token = "";
                LootLockerConfig.current.refreshToken = "";
                LootLockerConfig.current.deviceID = "";
                if (!Init())
                {
                    return false;
                }
            }

            if (!skipSessionCheck && !CheckActiveSession())
            {
                return false;
            }

            return true;
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
        /// Start a Playstation Network session
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// <param name="psnOnlineId">The player's Online ID</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartPlaystationNetworkSession(string psnOnlineId, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.PlayStationNetwork);

            LootLockerConfig.current.deviceID = psnOnlineId;
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(psnOnlineId);
            LootLockerAPIManager.Session(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Start an Android Network session
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// <param name="deviceId">The player's Device ID</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartAndroidSession(string deviceId, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.Android);

            LootLockerConfig.current.deviceID = deviceId;
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(deviceId);
            LootLockerAPIManager.Session(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Start a Amazon Luna session
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// <param name="amazonLunaGuid">The player's Amazon Luna GUID</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartAmazonLunaSession(string amazonLunaGuid, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.AmazonLuna);

            LootLockerConfig.current.deviceID = amazonLunaGuid;
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(amazonLunaGuid);
            LootLockerAPIManager.Session(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
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

            CurrentPlatform.Set(Platforms.Guest);
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest();
            string existingPlayerID = PlayerPrefs.GetString("LootLockerGuestPlayerID", "");
            if (!string.IsNullOrEmpty(existingPlayerID))
            {
                sessionRequest = new LootLockerSessionRequest(existingPlayerID);
            }

            LootLockerAPIManager.GuestSession(sessionRequest, response =>
            {
                if (response.success)
                {
                    PlayerPrefs.SetString("LootLockerGuestPlayerID", response.player_identifier);
                    PlayerPrefs.Save();
                }
                else
                {
                    CurrentPlatform.Reset();
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
            CurrentPlatform.Set(Platforms.Guest);

            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(identifier);

            LootLockerAPIManager.GuestSession(sessionRequest, response =>
            {
                if (response.success)
                {
                    PlayerPrefs.SetString("LootLockerGuestPlayerID", response.player_identifier);
                    PlayerPrefs.Save();
                }
                else
                {
                    CurrentPlatform.Reset();
                }
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

            CurrentPlatform.Set(Platforms.Steam);
            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(steamId64);
            LootLockerAPIManager.Session(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }

                onComplete(response);
            });
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
            CurrentPlatform.Set(Platforms.NintendoSwitch);
            LootLockerNintendoSwitchSessionRequest sessionRequest = new LootLockerNintendoSwitchSessionRequest(nsa_id_token);
            LootLockerAPIManager.NintendoSwitchSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
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

            CurrentPlatform.Set(Platforms.XboxOne);
            LootLockerXboxOneSessionRequest sessionRequest = new LootLockerXboxOneSessionRequest(xbox_user_token);
            LootLockerAPIManager.XboxOneSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Start a Game session for a Google User
        /// The Google sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="idToken">The Id Token from google sign in</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartGoogleSession(string idToken, Action<LootLockerGoogleSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGoogleSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.Google);

            LootLockerGoogleSignInSessionRequest sessionRequest = new LootLockerGoogleSignInSessionRequest(idToken);
            LootLockerAPIManager.GoogleSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Start a Game session for a Google User
        /// The Google sign in platform must be enabled in the web console for this to work.
        /// Desired Google platform also must be configured under advanced options in the web console.
        /// </summary>
        /// <param name="idToken">The Id Token from google sign in</param>
        /// <param name="googlePlatform">Google OAuth2 ClientID platform</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void StartGoogleSession(string idToken, GooglePlatform googlePlatform, Action<LootLockerGoogleSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGoogleSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.Google);

            var sessionRequest = new LootLockerGoogleSignInWithPlatformSessionRequest(idToken)
            {
                platform = googlePlatform.ToString()
            };

            LootLockerAPIManager.GoogleSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Refresh a previous session signed in with Google.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Google sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void RefreshGoogleSession(Action<LootLockerGoogleSessionResponse> onComplete)
        {
            RefreshGoogleSession("", onComplete);
        }

        /// <summary>
        /// Refresh a previous session signed in with Google.
        /// If you do not want to manually handle the refresh token we recommend using the RefreshGoogleSession(Action<LootLockerGoogleSessionResponse> onComplete) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Google sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="refresh_token">Token received in response from StartGoogleSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void RefreshGoogleSession(string refresh_token, Action<LootLockerGoogleSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGoogleSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.Google);
            LootLockerGoogleRefreshSessionRequest sessionRequest = new LootLockerGoogleRefreshSessionRequest(string.IsNullOrEmpty(refresh_token) ? LootLockerConfig.current.refreshToken : refresh_token);
            LootLockerAPIManager.GoogleSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Create a new session for Sign in with Apple
        /// The Apple sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="authorization_code">Authorization code, provided by apple</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAppleSessionResponse</param>
        public static void StartAppleSession(string authorization_code, Action<LootLockerAppleSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAppleSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.AppleSignIn);
            LootLockerAppleSignInSessionRequest sessionRequest = new LootLockerAppleSignInSessionRequest(authorization_code);
            LootLockerAPIManager.AppleSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Refresh a previous session signed in with Apple
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Apple sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAppleSessionResponse</param>
        public static void RefreshAppleSession(Action<LootLockerAppleSessionResponse> onComplete)
        {
            RefreshAppleSession("", onComplete);
        }

        /// <summary>
        /// Refresh a previous session signed in with Apple
        /// If you do not want to manually handle the refresh token we recommend using the RefreshAppleSession(Action<LootLockerAppleSessionResponse> onComplete) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
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

            CurrentPlatform.Set(Platforms.AppleSignIn);
            LootLockerAppleRefreshSessionRequest sessionRequest = new LootLockerAppleRefreshSessionRequest(string.IsNullOrEmpty(refresh_token) ? LootLockerConfig.current.refreshToken : refresh_token);
            LootLockerAPIManager.AppleSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Create a new session for Sign in with Apple Game Center
        /// The Apple Game Center sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="bundleId">The Apple Game Center bundle id of your app</param>
        /// <param name="playerId">The user's player id in Apple Game Center</param>
        /// <param name="publicKeyUrl">The url of the public key generated from Apple Game Center Identity Verification</param>
        /// <param name="signature">The signature generated from Apple Game Center Identity Verification</param>
        /// <param name="salt">The salt of the signature generated from Apple Game Center Identity Verification</param>
        /// <param name="timestamp">The timestamp of the verification generated from Apple Game Center Identity Verification</param>
        /// <param name="onComplete">onComplete Action for handling the response of type  for handling the response of type LootLockerAppleGameCenterSessionResponse</param>
        public static void StartAppleGameCenterSession(string bundleId, string playerId, string publicKeyUrl, string signature, string salt, long timestamp, Action<LootLockerAppleGameCenterSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAppleGameCenterSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.AppleGameCenter);
            LootLockerAppleGameCenterSessionRequest sessionRequest = new LootLockerAppleGameCenterSessionRequest(bundleId, playerId, publicKeyUrl, signature, salt, timestamp);
            LootLockerAPIManager.AppleGameCenterSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Refresh a previous session signed in with Apple Game Center
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Apple Game Center sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type  for handling the response of type LootLockerAppleGameCenterSessionResponse</param>
        public static void RefreshAppleGameCenterSession(Action<LootLockerAppleGameCenterSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAppleGameCenterSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.AppleGameCenter);
            LootLockerAppleGameCenterRefreshSessionRequest sessionRequest = new LootLockerAppleGameCenterRefreshSessionRequest(LootLockerConfig.current.refreshToken);
            LootLockerAPIManager.AppleGameCenterSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Create a new session for an Epic Online Services (EOS) user
        /// The Epic Games platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="id_token">EOS Id Token as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerEpicSessionResponse</param>
        public static void StartEpicSession(string id_token, Action<LootLockerEpicSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerEpicSessionResponse>());
                return;
            }
            CurrentPlatform.Set(Platforms.Epic);
            LootLockerEpicSessionRequest sessionRequest = new LootLockerEpicSessionRequest(id_token);
            LootLockerAPIManager.EpicSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Refresh a previous session signed in with Epic
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Epic sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerEpicSessionResponse</param>
        public static void RefreshEpicSession(Action<LootLockerEpicSessionResponse> onComplete)
        {
            RefreshEpicSession("", onComplete);
        }

        /// <summary>
        /// Refresh a previous session signed in with Epic
        /// If you do not want to manually handle the refresh token we recommend using the RefreshEpicSession(Action<LootLockerEpicSessionResponse> onComplete) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Epic sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="refresh_token">Token received in response from StartEpicSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerEpicSessionResponse</param>
        public static void RefreshEpicSession(string refresh_token, Action<LootLockerEpicSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerEpicSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.Epic);
            LootLockerEpicRefreshSessionRequest sessionRequest = new LootLockerEpicRefreshSessionRequest(string.IsNullOrEmpty(refresh_token) ? LootLockerConfig.current.refreshToken : refresh_token);
            LootLockerAPIManager.EpicSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }
        
        /// <summary>
        /// Start a Meta / Oculus session
        /// The Meta / Oculus platform must be enabled and configured in the web console for this to work.
        /// </summary>
        /// <param name="user_id">User ID as a string</param>
        /// <param name="nonce">Nonce as a string</param>
        /// <param name="onComplete">Action to handle the response of type LootLockerMetaSessionResponse</param>
        public static void StartMetaSession(string user_id, string nonce, Action<LootLockerMetaSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerMetaSessionResponse>());
                return;
            }
            CurrentPlatform.Set(Platforms.Meta);
            var sessionRequest = new LootLockerMetaSessionRequest()
            {
                user_id = user_id,
                nonce = nonce
            };
            
            var endPoint = LootLockerEndPoints.metaSessionRequest;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(sessionRequest), (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerMetaSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.refreshToken = response.refresh_token;
                LootLockerConfig.current.deviceID = "";
                onComplete?.Invoke(response);
            }, false);
        }

        /// <summary>
        /// Refresh a previous Meta / Oculus session
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Meta / Oculus platform must be enabled and configured in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerMetaSessionResponse</param>
        public static void RefreshMetaSession(Action<LootLockerMetaSessionResponse> onComplete)
        {
            RefreshMetaSession("", onComplete);
        }

        /// <summary>
        /// Refresh a previous Meta session
        /// If you do not want to manually handle the refresh token we recommend using the RefreshMetaSession(Action<LootLockerMetaSessionResponse> onComplete) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Meta platform must be enabled and configured in the web console for this to work.
        /// </summary>
        /// <param name="refresh_token">Token received in response from StartMetaSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerMetaSessionResponse</param>
        public static void RefreshMetaSession(string refresh_token, Action<LootLockerMetaSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerMetaSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.Meta);
            var sessionRequest = new LootLockerMetaRefreshSessionRequest()
            {
                refresh_token = string.IsNullOrEmpty(refresh_token) ? LootLockerConfig.current.refreshToken : refresh_token
            };
            var endPoint = LootLockerEndPoints.metaSessionRequest;
            
            
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(sessionRequest), (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerMetaSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.refreshToken = response.refresh_token;
                LootLockerConfig.current.deviceID = "";
                onComplete?.Invoke(response);
            }, false);
        }

        /// <summary>
        /// End active session (if any exists)
        /// Succeeds if a session was ended or no sessions were active
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void EndSession(Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true) || !CheckActiveSession())
            {
                onComplete?.Invoke(new LootLockerSessionResponse() { success = true, text = "No active session" });
                return;
            }

            LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest();
            LootLockerAPIManager.EndSession(sessionRequest, response =>
            {
                if (response.success)
                {
                    ClearLocalSession();
                }

                onComplete?.Invoke(response);
            });
        }

        /// <summary>
        /// Clears client session data. WARNING: This does not end the session in LootLocker servers.
        /// </summary>
        public static void ClearLocalSession()
        {
            // Clear White Label Login credentials
            if (CurrentPlatform.Get() == Platforms.WhiteLabel)
            {
                PlayerPrefs.DeleteKey("LootLockerWhiteLabelSessionToken");
                PlayerPrefs.DeleteKey("LootLockerWhiteLabelSessionEmail");
            }

            CurrentPlatform.Reset();

            LootLockerConfig.current.token = "";
            LootLockerConfig.current.deviceID = "";
            LootLockerConfig.current.refreshToken = "";
        }
        #endregion

        #region Remote Sessions

        /// <summary>
        /// Start a remote session
        /// If you want to let your local user sign in using another device then you use this method. First you will get the lease information needed to allow a secondary device to authenticate.
        /// While the process is ongoing, the remoteSessionLeaseStatusUpdate action (if one is provided) will be invoked intermittently (about once a second) to update you on the status of the process.
        /// When the process has come to an end (whether successfully or not), the onComplete action will be invoked with the updated information.
        /// </summary>
        /// <param name="remoteSessionLeaseInformation">Will be invoked once to provide the lease information that the secondary device can use to authenticate</param>
        /// <param name="remoteSessionLeaseStatusUpdate">Will be invoked intermittently to update the status lease process</param>
        /// <param name="onComplete">Invoked when the remote session process has run to completion containing either a valid session or information on why the process failed</param>
        /// <param name="pollingIntervalSeconds">Optional: How often to poll the status of the remote session process</param>
        /// <param name="timeOutAfterMinutes">Optional: How long to allow the process to take in it's entirety</param>
        public static Guid StartRemoteSession(Action<LootLockerLeaseRemoteSessionResponse> remoteSessionLeaseInformation, Action<LootLockerRemoteSessionStatusPollingResponse> remoteSessionLeaseStatusUpdate, Action<LootLockerStartRemoteSessionResponse> onComplete, float pollingIntervalSeconds = 1.0f, float timeOutAfterMinutes = 5.0f)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStartRemoteSessionResponse>());
                return Guid.Empty;
            }

            return LootLockerAPIManager.RemoteSessionPoller.StartRemoteSessionWithContinualPolling(remoteSessionLeaseInformation, remoteSessionLeaseStatusUpdate, onComplete, pollingIntervalSeconds, timeOutAfterMinutes);
        }

        /// <summary>
        /// Cancel an ongoing remote session process
        /// </summary>
        /// <param name="guid">The guid of the remote session process that you want to cancel</param>
        public static void CancelRemoteSessionProcess(Guid guid)
        {
            LootLockerAPIManager.RemoteSessionPoller.CancelRemoteSessionProcess(guid);
        }

        /// <summary>
        /// Refresh a previous session signed in remotely.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void RefreshRemoteSession(Action<LootLockerRefreshRemoteSessionResponse> onComplete)
        {
            RefreshRemoteSession("", onComplete);
        }

        /// <summary>
        /// Refresh a previous session signed in remotely.
        /// If you do not want to manually handle the refresh token we recommend using the RefreshRemoteSession(Action<LootLockerRemoteSessionResponse> onComplete) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// </summary>
        /// <param name="refreshToken">Token received in response from StartRemoteSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void RefreshRemoteSession(string refreshToken, Action<LootLockerRefreshRemoteSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerRefreshRemoteSessionResponse>());
                return;
            }

            CurrentPlatform.Set(Platforms.Remote);

            LootLockerRefreshRemoteSessionRequest sessionRequest = new LootLockerRefreshRemoteSessionRequest(string.IsNullOrEmpty(refreshToken) ? LootLockerConfig.current.refreshToken : refreshToken);
            LootLockerAPIManager.RemoteSessionPoller.RefreshRemoteSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
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

            LootLockerAPIManager.WhiteLabelLogin(input, response =>
            {
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
        /// Request verify account email for the user.
        /// White Label platform must be enabled in the web console for this to work.
        /// Account verification must also be enabled.
        /// </summary>
        /// <param name="email">Email of the player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        public static void WhiteLabelRequestVerification(string email, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            LootLockerAPIManager.WhiteLabelRequestAccountVerification(email, onComplete);
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

            CurrentPlatform.Set(Platforms.WhiteLabel);
            LootLockerAPIManager.WhiteLabelSession(sessionRequest, response =>
            {
                if (!response.success)
                {
                    CurrentPlatform.Reset();
                }
                onComplete(response);
            });
        }

        /// <summary>
        /// Log in a White Label user with the given email and password combination, verify user, and start a White Label Session. If that succeeds, then also start a LootLocker Session.
        /// The response is nested. The top properties will give the complete success condition and eventual error data. Nested inside the response you can also find the specific responses of the two composite calls using LoginResponse and SessionResponse respectively
        /// Set remember=true to prolong the session lifetime
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="email">E-mail for an existing user</param>
        /// <param name="password">Password for an existing user</param>
        /// <param name="rememberMe">Set remember=true to prolong the session lifetime</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void WhiteLabelLoginAndStartSession(string email, string password, bool rememberMe, Action<LootLockerWhiteLabelLoginAndStartSessionResponse> onComplete)
        {
            WhiteLabelLogin(email, password, rememberMe, loginResponse =>
            {
                if (!loginResponse.success)
                {
                    onComplete?.Invoke(LootLockerWhiteLabelLoginAndStartSessionResponse.MakeWhiteLabelLoginAndStartSessionResponse(loginResponse, null));
                    return;
                }
                StartWhiteLabelSession(sessionResponse =>
                {
                    onComplete?.Invoke(LootLockerWhiteLabelLoginAndStartSessionResponse.MakeWhiteLabelLoginAndStartSessionResponse(loginResponse, sessionResponse));
                });
            });
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
        /// Get the players XP and Level information from a playerIdentifier. This uses the same platform as the current session.
        /// If you are using multiple platforms (White Label + Steam for example), you must use the GetOtherPlayerInfo(string playerIdentifier, string platform, Action<LootLockerXpResponse> onComplete) method instead.
        /// </summary>
        /// <param name="playerIdentifier">The player identifier for this platform.</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerXpResponse</param>
        public static void GetOtherPlayerInfo(string playerIdentifier, Action<LootLockerXpResponse> onComplete)
        {
            GetOtherPlayerInfo(playerIdentifier, CurrentPlatform.GetString(), onComplete);
        }

        /// <summary>
        /// Get the players XP and Level information from a playerIdentifier.
        /// </summary>
        /// <param name="playerIdentifier">The player identifier for this platform.</param>
        /// <param name="platform">The platform that the user is present on as a string.</param>
        /// <param name="onComplete"></param>
        public static void GetOtherPlayerInfo(string playerIdentifier, string platform, Action<LootLockerXpResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerXpResponse>());
                return;
            }

            if (string.IsNullOrEmpty(platform))
            {
                platform = CurrentPlatform.GetString();
            }

            LootLockerOtherPlayerInfoRequest infoRequest = new LootLockerOtherPlayerInfoRequest(playerIdentifier, platform);
            LootLockerAPIManager.GetOtherPlayerInfo(infoRequest, onComplete);
        }


        /// <summary>
        /// Get the players inventory.
        /// </summary>
        /// <param name="count">Amount of assets to retrieve</param>
        /// <param name="after">The instance ID the list should start from</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInventoryResponse</param>
        public static void GetInventory(int count, int after, Action<LootLockerInventoryResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInventoryResponse>());
                return;
            }

            var endpoint = LootLockerEndPoints.getInventory.endPoint;

            endpoint += "?";
            if (count > 0)
                endpoint += $"count={count}&";
            if (after > 0)
                endpoint += $"after={after}&";


            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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
            GetInventory(-1, -1, onComplete);
        }

        /// <summary>
        /// Get the players inventory.
        /// </summary>
        /// <param name="count">Amount of assets to retrieve</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInventoryResponse</param>
        public static void GetInventory(int count, Action<LootLockerInventoryResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInventoryResponse>());
                return;
            }


            GetInventory(count, -1, onComplete);

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
        /// Get player names of the players from their last active platform by PSN ID's.
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
        /// Get player names of the players from their last active platform by Xbox ID's.
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

            if (CurrentPlatform.Get() == Platforms.Guest)
            {
                if (name.ToLower().Contains("player"))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.Error<PlayerNameResponse>("Setting the Player name to 'Player' is not allowed"));
                    return;

                } else if (name.ToLower().Contains(PlayerPrefs.GetString("LootLockerGuestPlayerID").ToLower()))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.Error<PlayerNameResponse>("Setting the Player name to the Identifier is not allowed"));
                    return;
                }
            }

            PlayerNameRequest data = new PlayerNameRequest();
            data.name = name;

            LootLockerAPIManager.SetPlayerName(data, onComplete);
        }

        /// <summary>
        /// Mark the logged in player for deletion. After 30 days the player will be deleted from the system.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse></param>
        public static void DeletePlayer(Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.deletePlayer.endPoint, LootLockerEndPoints.deletePlayer.httpMethod, null, onComplete:
                (serverResponse) =>
                {
                    if (serverResponse != null && serverResponse.success)
                    {
                        ClearLocalSession();
                    }
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
                });
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

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.getPlayerFiles.endPoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"File error: {e.Message}");
                return;
            }

            LootLockerServerRequest.UploadFile(LootLockerEndPoints.uploadPlayerFile, fileBytes, Path.GetFileName(pathToFile), "multipart/form-data", body,
                onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
                });
        }

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
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"File error: {e.Message}");
                return;
            }

            LootLockerServerRequest.UploadFile(LootLockerEndPoints.uploadPlayerFile, fileBytes, Path.GetFileName(fileStream.Name), "multipart/form-data", body,
                onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
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
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
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

        ///////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Update an existing player file with a new file.
        /// </summary>
        /// <param name="fileId">Id of the file. You can get the ID of files when you upload a file, or with GetAllPlayerFiles()</param>
        /// <param name="pathToFile">Path to the file, example: Application.persistentDataPath + "/" + fileName;</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void UpdatePlayerFile(int fileId, string pathToFile, Action<LootLockerPlayerFile> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>());
                return;
            }

            var fileBytes = new byte[] { };
            try
            {
                fileBytes = File.ReadAllBytes(pathToFile);
            }
            catch (Exception e)
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"File error: {e.Message}");
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.updatePlayerFile.endPoint, fileId);

            LootLockerServerRequest.UploadFile(endpoint, LootLockerEndPoints.updatePlayerFile.httpMethod, fileBytes, Path.GetFileName(pathToFile), "multipart/form-data", new Dictionary<string, string>(),
                onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
                });
        }

        /// <summary>
        /// Update an existing player file with a new file using a Filestream. Can be useful if you want to upload without storing anything on disk.
        /// </summary>
        /// <param name="fileId">Id of the file. You can get the ID of files when you upload a file, or with GetAllPlayerFiles()</param>
        /// <param name="fileStream">Filestream to upload</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void UpdatePlayerFile(int fileId, FileStream fileStream, Action<LootLockerPlayerFile> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>());
                return;
            }

            var fileBytes = new byte[fileStream.Length];
            try
            {
                fileStream.Read(fileBytes, 0, Convert.ToInt32(fileStream.Length));
            }
            catch (Exception e)
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"File error: {e.Message}");
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.updatePlayerFile.endPoint, fileId);

            LootLockerServerRequest.UploadFile(endpoint, LootLockerEndPoints.updatePlayerFile.httpMethod, fileBytes, Path.GetFileName(fileStream.Name), "multipart/form-data", new Dictionary<string, string>(),
                onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
                });
        }

        /// <summary>
        /// Update an existing player file with a new file using a byte array. Can be useful if you want to upload without storing anything on disk.
        /// </summary>
        /// <param name="fileId">Id of the file. You can get the ID of files when you upload a file, or with GetAllPlayerFiles()</param>
        /// <param name="fileBytes">Byte array to upload</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        public static void UpdatePlayerFile(int fileId, byte[] fileBytes, Action<LootLockerPlayerFile> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.updatePlayerFile.endPoint, fileId);

            LootLockerServerRequest.UploadFile(endpoint, LootLockerEndPoints.updatePlayerFile.httpMethod, fileBytes, null, "multipart/form-data", new Dictionary<string, string>(),
                onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
                });
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

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.DELETE, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        #endregion

        #region Player progressions

        /// <summary>
        /// Returns multiple progressions the player is currently on.
        /// </summary>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">Used for pagination, id of the player progression from which the pagination starts from, use the next_cursor and previous_cursor values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedPlayerProgressions</param>
        public static void GetPlayerProgressions(int count, string after, Action<LootLockerPaginatedPlayerProgressionsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedPlayerProgressionsResponse>());
                return;
            }

            var endpoint = LootLockerEndPoints.getAllPlayerProgressions.endPoint;

            endpoint += "?";
            if (count > 0)
                endpoint += $"count={count}&";

            if (!string.IsNullOrEmpty(after))
                endpoint += $"after={after}&";

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progressions the player is currently on.
        /// </summary>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedPlayerProgressions</param>
        public static void GetPlayerProgressions(int count, Action<LootLockerPaginatedPlayerProgressionsResponse> onComplete)
        {
            GetPlayerProgressions(count, null, onComplete);
        }

        /// <summary>
        /// Returns multiple progressions the player is currently on.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedPlayerProgressions</param>
        public static void GetPlayerProgressions(Action<LootLockerPaginatedPlayerProgressionsResponse> onComplete)
        {
            GetPlayerProgressions(-1, null, onComplete);
        }

        /// <summary>
        /// Returns a single progression the player is currently on.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgression</param>
        public static void GetPlayerProgression(string progressionKey, Action<LootLockerPlayerProgressionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerProgressionResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getSinglePlayerProgression.endPoint, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Adds points to a player progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to be added</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgressionWithRewards</param>
        public static void AddPointsToPlayerProgression(string progressionKey, ulong amount, Action<LootLockerPlayerProgressionWithRewardsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerProgressionWithRewardsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.addPointsToPlayerProgression.endPoint, progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Subtracts points from a player progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to be subtracted</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgressionWithRewards</param>
        public static void SubtractPointsFromPlayerProgression(string progressionKey, ulong amount, Action<LootLockerPlayerProgressionWithRewardsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerProgressionWithRewardsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.subtractPointsFromPlayerProgression.endPoint, progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Resets a player progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgressionWithRewards</param>
        public static void ResetPlayerProgression(string progressionKey, Action<LootLockerPlayerProgressionWithRewardsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerProgressionWithRewardsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.resetPlayerProgression.endPoint, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.POST, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Deletes a player progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        public static void DeletePlayerProgression(string progressionKey, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.deletePlayerProgression.endPoint, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.DELETE, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Registers a player progression if it doesn't exist. Same as adding 0 points to a progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgressionWithRewards</param>
        public static void RegisterPlayerProgression(string progressionKey, Action<LootLockerPlayerProgressionWithRewardsResponse> onComplete)
        {
            AddPointsToPlayerProgression(progressionKey, 0, onComplete);
        }

        #endregion

        #region Hero

        /// <summary>
        /// Create a hero with the provided type and name. The hero will be owned by the currently active player.
        /// </summary>
        /// <param name="heroId">The id of the hero</param>
        /// <param name="name">The new name for the hero</param>
        /// <param name="isDefault">Should this hero be the default hero?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        public static void CreateHero(int heroId, string name, bool isDefault, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {

                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>());
                return;
            }
            LootLockerCreateHeroRequest data = new LootLockerCreateHeroRequest();

            data.hero_id = heroId;
            data.name = name;
            data.is_default = isDefault;


            LootLockerAPIManager.CreateHero(data, onComplete);
        }

        /// <summary>
        /// List the heroes with names and character information
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGameHeroResponse</param>
        public static void GetGameHeroes(Action<LootLockerGameHeroResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGameHeroResponse>());
                return;
            }
            LootLockerAPIManager.GetGameHeroes(onComplete);
        }

        /// <summary>
        /// List the heroes that the current player owns
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        public static void ListPlayerHeroes(Action<LootLockerListHeroResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListHeroResponse>());
                return;
            }

            LootLockerAPIManager.ListPlayerHeroes(onComplete);
        }

        /// <summary>
        /// List player that the player with the specified SteamID64 owns
        /// </summary>
        /// <param name="steamID64">Steam id of the requested player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        public static void ListOtherPlayersHeroesBySteamID64(int steamID64, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>());
                return;
            }

            LootLockerAPIManager.ListOtherPlayersHeroesBySteamID64(steamID64, onComplete);
        }

        /// <summary>
        /// Create a hero for the current player with the supplied name from the game hero specified with the supplied hero id, asset variation id, and whether to set as default.
        /// </summary>
        /// <param name="name">The new name for the hero</param>
        /// <param name="heroId">The id of the hero</param>
        /// <param name="assetVariationId">ID of the asset variation to use</param>
        /// <param name="isDefault">Should this hero be the default hero?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        public static void CreateHeroWithVariation(string name, int heroId, int assetVariationId, bool isDefault, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>());
                return;
            }

            LootLockerCreateHeroWithVariationRequest data = new LootLockerCreateHeroWithVariationRequest();

            data.name = name;
            data.hero_id = heroId;
            data.asset_variation_id = assetVariationId;
            data.is_default = isDefault;

            LootLockerAPIManager.CreateHeroWithVariation(data, onComplete);
        }

        /// <summary>
        /// Return information about the requested hero on the current player
        /// </summary>
        /// <param name="heroId">The id of the hero to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        public static void GetHero(int heroId, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>());
                return;
            }

            LootLockerAPIManager.GetHero(heroId, onComplete);
        }

        /// <summary>
        /// Get the default hero for the player with the specified SteamID64
        /// </summary>
        /// <param name="steamId">Steam Id of the requested player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        public static void GetOtherPlayersDefaultHeroBySteamID64(int steamId, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>());
                return;
            }

            LootLockerAPIManager.GetOtherPlayersDefaultHeroBySteamID64(steamId, onComplete);

        }

        /// <summary>
        /// Update the name of the hero with the specified id and/or set it as default for the current player
        /// </summary>
        /// <param name="heroId">Id of the hero</param>
        /// <param name="name">The new name for the hero</param>
        /// <param name="isDefault">Should this hero be the default hero?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        public static void UpdateHero(string heroId, string name, bool isDefault, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>());
                return;
            }

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(heroId);

            LootLockerUpdateHeroRequest data = new LootLockerUpdateHeroRequest();
            data.name = name;
            data.is_default = isDefault;


            LootLockerAPIManager.UpdateHero(lootLockerGetRequest, data, onComplete);
        }

        /// <summary>
        /// Remove the hero with the specified id from the current players list of heroes.
        /// </summary>
        /// <param name="heroID">HeroID Id of the hero</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        public static void DeleteHero(int heroID, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>());
                return;
            }

            LootLockerAPIManager.DeleteHero(heroID, onComplete);
        }

        /// <summary>
        /// List Asset Instances owned by the specified hero
        /// </summary>
        /// <param name="heroID">HeroID Id of the hero</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInventoryResponse</param>
        public static void GetHeroInventory(int heroID, Action<LootLockerInventoryResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInventoryResponse>());
                return;
            }

            LootLockerAPIManager.GetHeroInventory(heroID, onComplete);
        }

        /// <summary>
        /// List the loadout of the specified hero that the current player owns
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        public static void GetHeroLoadout(Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>());
                return;
            }

            LootLockerAPIManager.GetHeroLoadout(onComplete);
        }

        /// <summary>
        /// List the loadout of the specified hero that the another player owns
        /// </summary>
        /// <param name="heroID">HeroID Id of the hero</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        public static void GetOtherPlayersHeroLoadout(int heroID, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>());
                return;
            }

            LootLockerAPIManager.GetOtherPlayersHeroLoadout(heroID, onComplete);
        }

        /// <summary>
        /// Equip the specified Asset Instance to the specified Hero that the current player owns
        /// </summary>
        /// <param name="heroID">Id of the hero</param>
        /// <param name="assetInstanceID">Id of the asset instance to give</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        public static void AddAssetToHeroLoadout(int heroID, int assetInstanceID, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>());
                return;
            }

            LootLockerAddAssetToHeroLoadoutRequest data = new LootLockerAddAssetToHeroLoadoutRequest();

            data.asset_instance_id = assetInstanceID;
            data.hero_id = heroID;


            LootLockerAPIManager.AddAssetToHeroLoadout(data, onComplete);
        }

        /// <summary>
        /// Equip the specified Asset Variation to the specified Hero that the current player owns
        /// </summary>
        /// 
        /// <param name="heroID">Id of the hero</param>
        /// <param name="assetID">Id of the asset</param>
        /// <param name="assetInstanceID">Id of the asset instance to give</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        public static void AddAssetVariationToHeroLoadout(int heroID, int assetID, int assetInstanceID, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>());
                return;
            }

            LootLockerAddAssetVariationToHeroLoadoutRequest data = new LootLockerAddAssetVariationToHeroLoadoutRequest();

            data.hero_id = heroID;
            data.asset_id = assetID;
            data.asset_variation_id = assetInstanceID;

            LootLockerAPIManager.AddAssetVariationToHeroLoadout(data, onComplete);
        }

        /// <summary>
        /// Unequip the specified Asset Instance to the specified Hero that the current player owns
        /// </summary>
        /// 
        /// <param name="assetID">Id of the asset</param>
        /// <param name="heroID">Id of the hero</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        public static void RemoveAssetFromHeroLoadout(string assetID, string heroID, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>());
                return;
            }

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(assetID);
            lootLockerGetRequest.getRequests.Add(heroID);

            LootLockerAPIManager.RemoveAssetFromHeroLoadout(lootLockerGetRequest, onComplete);
        }

        #endregion

        #region Base Classes


        [Obsolete("This function is deprecated and will be removed soon. Please use the function CreateClass() instead")]
        public static void CreateCharacter(string characterTypeId, string newCharacterName, bool isDefault, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            CreateClass(characterTypeId, newCharacterName, isDefault, onComplete);
        }

        /// <summary>
        /// Create a Class with the provided type and name. The Class will be owned by the currently active player.
        /// Use ListClassTypes() to get a list of available Class types for your game.
        /// </summary>
        /// <param name="classTypeID">Use ListClassTypes() to get a list of available class types for your game.</param>
        /// <param name="newClassName">The new name for the class</param>
        /// <param name="isDefault">Should this class be the default class?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        public static void CreateClass(string classTypeID, string newClassName, bool isDefault, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>());
                return;
            }

            LootLockerCreateClassRequest data = new LootLockerCreateClassRequest();

            data.name = newClassName;
            data.is_default = isDefault;
            data.character_type_id = classTypeID;

            LootLockerAPIManager.CreateClass(data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function ListClassTypes() instead")]
        public static void ListCharacterTypes(Action<LootLockerListClassTypesResponse> onComplete)
        {
            ListClassTypes(onComplete);
        }

        /// <summary>
        /// List all available Class types for your game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerListClassTypesResponse</param>
        public static void ListClassTypes(Action<LootLockerListClassTypesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListClassTypesResponse>());
                return;
            }
            LootLockerAPIManager.ListClassTypes(onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function ListPlayerClasses() instead")]
        public static void ListPlayerCharacters(Action<LootLockerPlayerClassListResponse> onComplete)
        {
            ListPlayerClasses(onComplete);
        }

        /// <summary>
        /// Get list of classes to a player
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerClassListResponse</param>
        public static void ListPlayerClasses(Action<LootLockerPlayerClassListResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerClassListResponse>());
                return;
            }
            LootLockerAPIManager.ListPlayerClasses(onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetClassLoadout() instead")]
        public static void GetCharacterLoadout(Action<LootLockerClassLoadoutResponse> onComplete)
        {
            GetClassLoadout(onComplete);
        }

        /// <summary>
        /// Get all class loadouts for your game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        public static void GetClassLoadout(Action<LootLockerClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>());
                return;
            }
            LootLockerAPIManager.GetClassLoadout(onComplete);

        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetOtherPlayersClassLoadout() instead")]
        public static void GetOtherPlayersCharacterLoadout(string player_id, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            GetOtherPlayersCharacterLoadout(player_id, CurrentPlatform.Get(), onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetOtherPlayersClassLoadout() instead")]
        public static void GetOtherPlayersCharacterLoadout(string player_id, Platforms platform, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            GetOtherPlayersClassLoadout(player_id, platform, onComplete);
        }

        /// <summary>
        /// Get a class loadout from a specific player and platform
        /// </summary>
        /// <param name="playerID">ID of the player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        public static void GetOtherPlayersClassLoadout(string playerID, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            GetOtherPlayersClassLoadout(playerID, CurrentPlatform.Get(), onComplete);
        }

        /// <summary>
        /// Get a class loadout from a specific player and platform
        /// </summary>
        /// <param name="playerID">ID of the player</param>
        /// <param name="platform">The platform that the ID of the player is for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        public static void GetOtherPlayersClassLoadout(string playerID, Platforms platform, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();

            data.getRequests.Add(playerID);
            data.getRequests.Add(CurrentPlatform.GetPlatformRepresentation(platform).PlatformString);
            LootLockerAPIManager.GetOtherPlayersClassLoadout(data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function UpdateClass() instead")]
        public static void UpdateCharacter(string characterID, string newCharacterName, bool isDefault, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            UpdateClass(characterID, newCharacterName, isDefault, onComplete);
        }

        /// <summary>
        /// Update information about the class. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="classID">ID of the class</param>
        /// <param name="newClassName">New name for the class</param>
        /// <param name="isDefault">Should the class be the default class?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        public static void UpdateClass(string classID, string newClassName, bool isDefault, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>());
                return;
            }

            LootLockerUpdateClassRequest data = new LootLockerUpdateClassRequest();

            data.name = newClassName;
            data.is_default = isDefault;

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(classID);

            LootLockerAPIManager.UpdateClass(lootLockerGetRequest, data, onComplete);

        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function SetDefaultClass() instead")]
        public static void SetDefaultCharacter(string characterID, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            SetDefaultClass(characterID, onComplete);
        }

        /// <summary>
        /// Set the class with classID as the default class for the currently active player. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="classID">ID of the class</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        public static void SetDefaultClass(string classID, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>());
                return;
            }

            LootLockerUpdateClassRequest data = new LootLockerUpdateClassRequest();

            data.is_default = true;

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(classID);

            LootLockerAPIManager.UpdateClass(lootLockerGetRequest, data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function EquipIdAssetToDefaultClass() instead")]
        public static void EquipIdAssetToDefaultCharacter(string assetInstanceId, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            EquipIdAssetToDefaultClass(assetInstanceId, onComplete);
        }

        /// <summary>
        /// Equip an asset to the players default class.
        /// </summary>
        /// <param name="assetInstanceID">ID of the asset instance to equip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        public static void EquipIdAssetToDefaultClass(string assetInstanceID, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>());
                return;
            }
            LootLockerEquipByIDRequest data = new LootLockerEquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceID);
            LootLockerAPIManager.EquipIdAssetToDefaultClass(data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function EquipGlobalAssetToDefaultClass() instead")]
        public static void EquipGlobalAssetToDefaultCharacter(string assetId, string assetVariationId, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            EquipGlobalAssetToDefaultClass(assetId, assetVariationId, onComplete);
        }

        /// <summary>
        /// Equip a global asset to the players default class.
        /// </summary>
        /// <param name="assetID">ID of the asset instance to equip</param>
        /// <param name="assetVariationID">ID of the asset variation to use</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        public static void EquipGlobalAssetToDefaultClass(string assetID, string assetVariationID, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>());
                return;
            }
            LootLockerEquipByAssetRequest data = new LootLockerEquipByAssetRequest();
            data.asset_id = int.Parse(assetID);
            data.asset_variation_id = int.Parse(assetVariationID);
            LootLockerAPIManager.EquipGlobalAssetToDefaultClass(data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function EquipIdAssetToClass() instead")]
        public static void EquipIdAssetToCharacter(string characterID, string assetInstanceId, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            EquipIdAssetToClass(characterID, assetInstanceId, onComplete);
        }

        /// <summary>
        /// Equip an asset to a specific class. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="classID">ID of the class</param>
        /// <param name="assetInstanceID">ID of the asset instance to equip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToclassLoadoutResponse</param>
        public static void EquipIdAssetToClass(string classID, string assetInstanceID, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>());
                return;
            }
            LootLockerEquipByIDRequest data = new LootLockerEquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceID);

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(classID);
            LootLockerAPIManager.EquipIdAssetToClass(lootLockerGetRequest, data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function EquipGlobalAssetToClass() instead")]
        public static void EquipGlobalAssetToCharacter(string assetId, string assetVariationId, string characterID, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            EquipGlobalAssetToClass(assetId, assetVariationId, characterID, onComplete);
        }

        /// <summary>
        /// Equip a global asset to a specific class. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="assetID">ID of the asset to equip</param>
        /// <param name="assetVariationID">ID of the variation to use</param>
        /// <param name="classID">ID of the class to equip the asset to</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        public static void EquipGlobalAssetToClass(string assetID, string assetVariationID, string classID, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>());
                return;
            }
            LootLockerEquipByAssetRequest data = new LootLockerEquipByAssetRequest();
            data.asset_id = int.Parse(assetID);
            data.asset_variation_id = int.Parse(assetVariationID);
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(classID);
            LootLockerAPIManager.EquipGlobalAssetToClass(lootLockerGetRequest, data, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function UnEquipIdAssetToClass() instead")]
        public static void UnEquipIdAssetToCharacter(string assetId, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            UnEquipIdAssetToClass(assetId, onComplete);
        }

        /// <summary>
        /// Unequip an asset from the players default class.
        /// </summary>
        /// <param name="assetID">ID of the asset to unequip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        public static void UnEquipIdAssetToClass(string assetID, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>());
                return;
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(assetID);
            LootLockerAPIManager.UnEquipIdAssetToClass(lootLockerGetRequest, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function UnEquipIdAssetToClass() instead")]
        public static void UnEquipIdAssetToCharacter(int characterID, int assetInstanceId, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            UnEquipIdAssetToClass(characterID.ToString(), assetInstanceId.ToString(), onComplete);
        }

        /// <summary>
        /// Unequip an asset from a specific class. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="classID">ID of the class to unequip</param>
        /// <param name="assetInstanceID">Asset instance ID of the asset to unequip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        public static void UnEquipIdAssetToClass(string classID, string assetInstanceID, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>());
                return;
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(classID);
            lootLockerGetRequest.getRequests.Add(assetInstanceID);
            LootLockerAPIManager.UnEquipIdAssetToClass(lootLockerGetRequest, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetCurrentLoadoutToDefaultClass() instead")]
        public static void GetCurrentLoadOutToDefaultCharacter(Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            GetCurrentLoadoutToDefaultClass(onComplete);
        }

        /// <summary>
        /// Get the loadout for the players default class.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetCurrentLoadoutToDefaultClassResponse</param>
        public static void GetCurrentLoadoutToDefaultClass(Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCurrentLoadoutToDefaultClassResponse>());
                return;
            }
            LootLockerAPIManager.GetCurrentLoadoutToDefaultClass(onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetCurrentLoadoutToOtherClass() instead")]
        public static void GetCurrentLoadOutToOtherCharacter(string playerID, Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            GetCurrentLoadoutToOtherClass(playerID, onComplete);
        }

        /// <summary>
        /// Get the current loadout for the default class of the specified player on the current platform
        /// </summary>
        /// <param name="playerID">ID of the player to get the loadout for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetCurrentLoadoutToDefaultClassResponse</param>
        public static void GetCurrentLoadoutToOtherClass(string playerID, Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            GetCurrentLoadoutToOtherClass(playerID, CurrentPlatform.Get(), onComplete);

        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetCurrentLoadoutToOtherClass() instead")]
        public static void GetCurrentLoadOutToOtherCharacter(string playerID, Platforms platform, Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            GetCurrentLoadoutToOtherClass(playerID, platform, onComplete);
        }

        /// <summary>
        /// Get the current loadout for the default class of the specified player and platform
        /// </summary>
        /// <param name="playerID">ID of the player to get the loadout for</param>
        /// <param name="platform">The platform that the ID of the player is for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetCurrentLoadoutToDefaultClassResponse</param>
        public static void GetCurrentLoadoutToOtherClass(string playerID, Platforms platform, Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCurrentLoadoutToDefaultClassResponse>());
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(playerID);
            lootLockerGetRequest.getRequests.Add(CurrentPlatform.GetPlatformRepresentation(platform).PlatformString);
            LootLockerAPIManager.GetCurrentLoadoutToOtherClass(lootLockerGetRequest, onComplete);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetEquipableContextToDefaultClass() instead")]
        public static void GetEquipableContextToDefaultCharacter(Action<LootLockerContextResponse> onComplete)
        {
            GetEquipableContextToDefaultClass(onComplete);
        }

        /// <summary>
        /// Get the equippable contexts for the players default class.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerContextResponse</param>
        public static void GetEquipableContextToDefaultClass(Action<LootLockerContextResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerContextResponse>());
                return;
            }
            LootLockerAPIManager.GetEquipableContextToDefaultClass(onComplete);
        }

        #endregion

        #region Character progressions

        /// <summary>
        /// Returns multiple progressions the character is currently on.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">Used for pagination, id of the character progression from which the pagination starts from, use the next_cursor and previous_cursor values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedCharacterProgressions</param>
        public static void GetCharacterProgressions(int characterId, int count, string after, Action<LootLockerPaginatedCharacterProgressionsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedCharacterProgressionsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getAllCharacterProgressions.endPoint, characterId);

            endpoint += "?";
            if (count > 0)
                endpoint += $"count={count}&";

            if (!string.IsNullOrEmpty(after))
                endpoint += $"after={after}&";

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progressions the character is currently on.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedCharacterProgressions</param>
        public static void GetCharacterProgressions(int characterId, int count, Action<LootLockerPaginatedCharacterProgressionsResponse> onComplete)
        {
            GetCharacterProgressions(characterId, count, null, onComplete);
        }

        /// <summary>
        /// Returns multiple progressions the character is currently on.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedCharacterProgressions</param>
        public static void GetCharacterProgressions(int characterId, Action<LootLockerPaginatedCharacterProgressionsResponse> onComplete)
        {
            GetCharacterProgressions(characterId, -1, null, onComplete);
        }

        /// <summary>
        /// Returns a single progression the character is currently on.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterProgression</param>
        public static void GetCharacterProgression(int characterId, string progressionKey, Action<LootLockerCharacterProgressionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterProgressionResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getSingleCharacterProgression.endPoint, characterId, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Adds points to a character progression.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to add</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterProgressionWithRewards</param>
        public static void AddPointsToCharacterProgression(int characterId, string progressionKey, ulong amount, Action<LootLockerCharacterProgressionWithRewardsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterProgressionWithRewardsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.addPointsToCharacterProgression.endPoint, characterId, progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Subtracts points from a character progression.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to subtract</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterProgressionWithRewards</param>
        public static void SubtractPointsFromCharacterProgression(int characterId, string progressionKey, ulong amount, Action<LootLockerCharacterProgressionWithRewardsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterProgressionWithRewardsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.subtractPointsFromCharacterProgression.endPoint, characterId, progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Resets a character progression.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterProgressionWithRewards</param>
        public static void ResetCharacterProgression(int characterId, string progressionKey, Action<LootLockerCharacterProgressionWithRewardsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterProgressionWithRewardsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.resetCharacterProgression.endPoint, characterId, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.POST, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Deletes a character progression.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        public static void DeleteCharacterProgression(int characterId, string progressionKey, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.deleteCharacterProgression.endPoint, characterId, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.DELETE, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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
        /// Get the player storage as a Dictionary<string, string> for the currently active player (key/values).
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStoragResponseDictionary</param>
        public static void GetEntirePersistentStorage(Action<LootLockerGetPersistentStoragResponseDictionary> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStoragResponseDictionary>());
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
        /// Update or create a key/value pair in the player storage for the currently active player.
        /// </summary>
        /// <param name="key">Name of the key</param>
        /// <param name="value">Value of the key</param>
        /// <param name="isPublic">Is the key public?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStoragResponse</param>
        public static void UpdateOrCreateKeyValue(string key, string value, bool isPublic, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStoragResponse>());
                return;
            }
            LootLockerGetPersistentStorageRequest data = new LootLockerGetPersistentStorageRequest();
            data.AddToPayload(new LootLockerPayload { key = key, value = value, is_public = isPublic });
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

        /// <summary>
        /// Grant an Asset to the Player's Inventory.
        /// </summary>
        /// <param name="assetID">The Asset you want to create an Instance of and give to the current player</param>
        public static void GrantAssetToPlayerInventory(int assetID, Action<LootLockerGrantAssetResponse> onComplete)
        {
            GrantAssetToPlayerInventory(assetID, null, null, onComplete);
        }

        /// <summary>
        /// Grant an Asset Instance to the Player's Inventory.
        /// </summary>
        /// <param name="assetID">The Asset you want to create an Instance of and give to the current player</param>
        /// <param name="assetVariationID">The id of the Asset Variation you want to grant</param>
        /// <param name="assetRentalOptionID">The rental option id you want to give the Asset Instance</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGrantAssetResponse</param>
        public static void GrantAssetToPlayerInventory(int assetID, int? assetVariationID, int? assetRentalOptionID, Action<LootLockerGrantAssetResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGrantAssetResponse>());
                return;
            }

            LootLockerGrantAssetRequest data = new LootLockerGrantAssetRequest();
            data.asset_id = assetID;
            data.asset_variation_id = assetVariationID;
            data.asset_rental_option_id = assetRentalOptionID;

            LootLockerAPIManager.GrantAssetToPlayerInventory(data, onComplete);
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

        /// <summary>
        /// Delete an Asset Instance from the current Player's Inventory.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID of the asset instance you want to delete from the current Players Inventory</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        public static void DeleteAssetInstanceFromPlayerInventory(int assetInstanceID, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.DeleteAssetInstanceFromPlayerInventory(data, onComplete);
        }
        #endregion
        
        #region AssetInstance progressions

        /// <summary>
        /// Returns multiple progressions for an asset instance.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">Used for pagination, ID of the asset instance progression from which the pagination starts from, use the next_cursor and previous_cursor values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedAssetInstanceProgressions</param>
        public static void GetAssetInstanceProgressions(int assetInstanceId, int count, string after, Action<LootLockerPaginatedAssetInstanceProgressionsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedAssetInstanceProgressionsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getAllAssetInstanceProgressions.endPoint, assetInstanceId);

            endpoint += "?";
            if (count > 0)
                endpoint += $"count={count}&";

            if (!string.IsNullOrEmpty(after))
                endpoint += $"after={after}&";

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progressions for an asset instance.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedAssetInstanceProgressions</param>
        public static void GetAssetInstanceProgressions(int assetInstanceId, int count, Action<LootLockerPaginatedAssetInstanceProgressionsResponse> onComplete)
        {
            GetAssetInstanceProgressions(assetInstanceId, count, null, onComplete);
        }

        /// <summary>
        /// Returns multiple progressions for an asset instance.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedAssetInstanceProgressions</param>
        public static void GetAssetInstanceProgressions(int assetInstanceId, Action<LootLockerPaginatedAssetInstanceProgressionsResponse> onComplete)
        {
            GetAssetInstanceProgressions(assetInstanceId, -1, null, onComplete);
        }

        /// <summary>
        /// Returns multiple progressions for an asset instance.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetInstanceProgression</param>
        public static void GetAssetInstanceProgression(int assetInstanceId, string progressionKey, Action<LootLockerAssetInstanceProgressionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetInstanceProgressionResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getSingleAssetInstanceProgression.endPoint, assetInstanceId, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Adds points to an asset instance progression.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to add</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetInstanceProgressionWithRewards</param>
        public static void AddPointsToAssetInstanceProgression(int assetInstanceId, string progressionKey, ulong amount, Action<LootLockerAssetInstanceProgressionWithRewardsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetInstanceProgressionWithRewardsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.addPointsToAssetInstanceProgression.endPoint, assetInstanceId, progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });  

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Subtracts points from an asset instance progression.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to subtract</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetInstanceProgressionWithRewards</param>
        public static void SubtractPointsFromAssetInstanceProgression(int assetInstanceId, string progressionKey, ulong amount, Action<LootLockerAssetInstanceProgressionWithRewardsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetInstanceProgressionWithRewardsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.subtractPointsFromAssetInstanceProgression.endPoint, assetInstanceId, progressionKey);
            
            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Resets an asset instance progression.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetInstanceProgressionWithRewards</param>
        public static void ResetAssetInstanceProgression(int assetInstanceId, string progressionKey, Action<LootLockerAssetInstanceProgressionWithRewardsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetInstanceProgressionWithRewardsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.resetAssetInstanceProgression.endPoint, assetInstanceId, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.POST, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        
        /// <summary>
        /// Deletes an asset instance progression.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        public static void DeleteAssetInstanceProgression(int assetInstanceId, string progressionKey, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.deleteAssetInstanceProgression.endPoint, assetInstanceId, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.DELETE, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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

        #region Progressions

        /// <summary>
        /// Returns multiple progressions.
        /// </summary>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">Used for pagination, id of the progression from which the pagination starts from, use the next_cursor and previous_cursor values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressions</param>
        public static void GetProgressions(int count, string after, Action<LootLockerPaginatedProgressionsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedProgressionsResponse>());
                return;
            }

            var endpoint = LootLockerEndPoints.getAllProgressions.endPoint;

            endpoint += "?";
            if (count > 0)
                endpoint += $"count={count}&";

            if (!string.IsNullOrEmpty(after))
                endpoint += $"after={after}&";

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progressions.
        /// </summary>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressions</param>
        public static void GetProgressions(int count, Action<LootLockerPaginatedProgressionsResponse> onComplete)
        {
            GetProgressions(count, null, onComplete);
        }

        /// <summary>
        /// Returns multiple progressions.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressions</param>
        public static void GetProgressions(Action<LootLockerPaginatedProgressionsResponse> onComplete)
        {
            GetProgressions(-1, null, onComplete);
        }

        /// <summary>
        /// Returns a single progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerProgression</param>
        public static void GetProgression(string progressionKey, Action<LootLockerProgressionResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerProgressionResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getSingleProgression.endPoint, progressionKey);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progression tiers for the specified progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">Used for pagination, step of the tier from which the pagination starts from, use the next_cursor and previous_cursor values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressionTiers</param>
        public static void GetProgressionTiers(string progressionKey, int count, ulong? after, Action<LootLockerPaginatedProgressionTiersResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedProgressionTiersResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getProgressionTiers.endPoint, progressionKey);

            endpoint += "?";
            if (count > 0)
                endpoint += $"count={count}&";

            if (after.HasValue && after > 0)
                endpoint += $"after={after}&";

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progression tiers for the specified progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressionTiers</param>
        public static void GetProgressionTiers(string progressionKey, int count, Action<LootLockerPaginatedProgressionTiersResponse> onComplete)
        {
            GetProgressionTiers(progressionKey, count, null, onComplete);
        }

        /// <summary>
        /// Returns multiple progression tiers for the specified progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressionTiers</param>
        public static void GetProgressionTiers(string progressionKey, Action<LootLockerPaginatedProgressionTiersResponse> onComplete)
        {
            GetProgressionTiers(progressionKey, -1, null, onComplete);
        }
        
        /// <summary>
        /// Returns a single progression tier for the specified progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="step">Step of the progression tier that is being fetched</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerProgressionTierResponse</param>
        public static void GetProgressionTier(string progressionKey, ulong step, Action<LootLockerProgressionTierResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerProgressionTierResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getProgressionTier.endPoint, progressionKey, step);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region Missions

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
            string source = LootLockerJson.SerializeObject(finishingPayload) + startingMissionSignature + playerId;
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

        [Obsolete("This function is deprecated and will be removed soon. Please use the function PollOrderStatus(int assetId, Action<LootLockerPurchaseOrderStatus> onComplete) instead")]
        public static void PollOrderStatus(int assetId, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            PollOrderStatus(assetId, (LootLockerPurchaseOrderStatus orderStatus) => onComplete(new LootLockerClassLoadoutResponse
            {
                errorData = orderStatus.errorData,
                statusCode = orderStatus.statusCode,
                success = orderStatus.success,
                text = orderStatus.text
            }));
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
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPurchaseOrderStatus</param>
        public static void PollOrderStatus(int assetId, Action<LootLockerPurchaseOrderStatus> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseOrderStatus>());
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.PollOrderStatus(data, onComplete);
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

        /// <summary>
        /// Purchase one or more catalog items using a specified wallet
        /// </summary>
        /// <param name="walletId">The id of the wallet to use for the purchase</param>
        /// <param name="itemsToPurchase">A list of items to purchase along with the quantity of each item to purchase</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void LootLockerPurchaseCatalogItems(string walletId, LootLockerCatalogItemAndQuantityPair[] itemsToPurchase, Action<LootLockerPurchaseCatalogItemResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseCatalogItemResponse>());
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerPurchaseCatalogItemRequest
            {
                wallet_id = walletId,
                items = itemsToPurchase
            });

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.purchaseCatalogItem.endPoint, LootLockerEndPoints.purchaseCatalogItem.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Apple App Store for the current player
        /// </summary>
        /// <param name="transactionId">The id of the transaction successfully made towards the Apple App Store</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="sandboxed">Optional: Should this redemption be made towards sandbox App Store</param>
        public static void RedeemAppleAppStorePurchaseForPlayer(string transactionId, Action<LootLockerResponse> onComplete, bool sandboxed = false)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemAppleAppStorePurchaseForPlayerRequest()
            {
                transaction_id = transactionId,
                sandboxed = sandboxed
            });

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.redeemAppleAppStorePurchase.endPoint, LootLockerEndPoints.redeemAppleAppStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Apple App Store for a class that the current player owns
        /// </summary>
        /// <param name="transactionId">The id of the transaction successfully made towards the Apple App Store</param>
        /// <param name="classId">The id of the class to redeem this transaction for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="sandboxed">Optional: Should this redemption be made towards sandbox App Store</param>
        public static void RedeemAppleAppStorePurchaseForClass(string transactionId, int classId, Action<LootLockerResponse> onComplete, bool sandboxed = false)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemAppleAppStorePurchaseForClassRequest()
            {
                transaction_id = transactionId,
                class_id = classId,
                sandboxed = sandboxed
            });

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.redeemAppleAppStorePurchase.endPoint, LootLockerEndPoints.redeemAppleAppStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Google Play Store for the current player
        /// </summary>
        /// <param name="productId">The id of the product that this redemption refers to</param>
        /// <param name="purchaseToken">The token from the purchase successfully made towards the Google Play Store</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void RedeemGooglePlayStorePurchaseForPlayer(string productId, string purchaseToken, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemGooglePlayStorePurchaseForPlayerRequest()
            {
                product_id = productId,
                purchase_token = purchaseToken
            });

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.redeemGooglePlayStorePurchase.endPoint, LootLockerEndPoints.redeemGooglePlayStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Google Play Store for a class that the current player owns
        /// </summary>
        /// <param name="productId">The id of the product that this redemption refers to</param>
        /// <param name="purchaseToken">The token from the purchase successfully made towards the Google Play Store</param>
        /// <param name="classId">The id of the class to redeem this purchase for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void RedeemGooglePlayStorePurchaseForClass(string productId, string purchaseToken, int classId, Action<LootLockerResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>());
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemGooglePlayStorePurchaseForClassRequest()
            {
                product_id = productId,
                purchase_token = purchaseToken,
                class_id = classId
            });

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.redeemGooglePlayStorePurchase.endPoint, LootLockerEndPoints.redeemGooglePlayStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region Collectables
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
        /// <param name="leaderboardKey">Key of the leaderboard as a string</param>
        /// <param name="member_id">ID of the player as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetMemberRankResponse</param>
        public static void GetMemberRank(string leaderboardKey, string member_id, Action<LootLockerGetMemberRankResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMemberRankResponse>());
                return;
            }
            LootLockerGetMemberRankRequest lootLockerGetMemberRankRequest = new LootLockerGetMemberRankRequest();

            lootLockerGetMemberRankRequest.leaderboardId = leaderboardKey;
            lootLockerGetMemberRankRequest.member_id = member_id;

            LootLockerAPIManager.GetMemberRank(lootLockerGetMemberRankRequest, onComplete);
        }

        /// <summary>
        /// Get the current ranking for several members on a specific leaderboard.
        /// </summary>
        /// <param name="members">List of members to get as string</param>
        /// <param name="leaderboardKey">Key of the leaderboard as a string</param>
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

        /// <summary>
        /// Get the entries for a specific leaderboard.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
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
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetScoreList(string leaderboardKey, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreList(leaderboardKey, count, -1, onComplete);
        }

        /// <summary>
        /// Get the next entries for a specific leaderboard. Can be called after GetScoreList.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        public static void GetNextScoreList(string leaderboardKey, int count, Action<LootLockerGetScoreListResponse> onComplete)
        {
            GetScoreList(leaderboardKey, count, int.Parse(LootLockerGetScoreListRequest.nextCursor.ToString()), onComplete);
        }

        /// <summary>
        /// Get the previous entries for a specific leaderboard. Can be called after GetScoreList or GetNextScoreList.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
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

        /// <summary>
        /// Submit a score to a leaderboard.
        /// </summary>
        /// <param name="memberId">Can be left blank if it is a player leaderboard, otherwise an identifier for the player</param>
        /// <param name="score">The score to upload</param>
        /// <param name="leaderboardKey">Key of the leaderboard to submit score to</param>
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
        /// <param name="leaderboardKey">Key of the leaderboard to submit score to</param>
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
        /// <param name="AddAssetDetails">Optional:If true, return additional information about the asset</param>
        /// <param name="tag">Optional:Specific tag to use</param>
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
        /// Lock a drop table and return information about the assets that were computed.
        /// </summary>
        /// <param name="tableInstanceId">Asset instance ID of the drop table to compute</param>
        /// <param name="AddAssetDetails">If true, return additional information about the asset</param>
        /// <param name="tag">Specific tag to use</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerComputeAndLockDropTableResponse</param>
        public static void ComputeAndLockDropTable(int tableInstanceId, bool AddAssetDetails, string tag, Action<LootLockerComputeAndLockDropTableResponse> onComplete)
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

        #region Currency
        /// <summary>
        /// Get a list of available currencies for the game
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void ListCurrencies(Action<LootLockerListCurrenciesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListCurrenciesResponse>());
                return;
            }

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.listCurrencies.endPoint, LootLockerEndPoints.listCurrencies.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get a list of the denominations available for a specific currency
        /// </summary>
        /// <param name="currencyCode">The code of the currency to fetch denominations for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void GetCurrencyDenominationsByCode(string currencyCode, Action<LootLockerListDenominationsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListDenominationsResponse>());
                return;
            }

            var endpoint = string.Format(LootLockerEndPoints.getCurrencyDenominationsByCode.endPoint, currencyCode);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerEndPoints.getCurrencyDenominationsByCode.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region Balances
        /// <summary>
        /// Get a list of balances in a specified wallet
        /// </summary>
        /// <param name="walletId">Unique ID of the wallet to get balances for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void ListBalancesInWallet(string walletId, Action<LootLockerListBalancesForWalletResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListBalancesForWalletResponse>());
                return;
            }
            var endpoint = string.Format(LootLockerEndPoints.listBalancesInWallet.endPoint, walletId);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerEndPoints.listBalancesInWallet.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get information about a specified wallet
        /// </summary>
        /// <param name="walletId">Unique ID of the wallet to get information for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void GetWalletByWalletId(string walletId, Action<LootLockerGetWalletResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetWalletResponse>());
                return;
            }
            var endpoint = string.Format(LootLockerEndPoints.getWalletByWalletId.endPoint, walletId);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerEndPoints.getWalletByWalletId.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get information about a wallet for a specified holder
        /// </summary>
        /// <param name="holderUlid">ULID of the holder of the wallet you want to get information for</param>
        /// <param name="holderType">The type of the holder to get the wallet for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void GetWalletByHolderId(string holderUlid, LootLockerWalletHolderTypes holderType, Action<LootLockerGetWalletResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetWalletResponse>());
                return;
            }
            var endpoint = string.Format(LootLockerEndPoints.getWalletByHolderId.endPoint, holderUlid);

            LootLockerServerRequest.CallAPI(endpoint, LootLockerEndPoints.getWalletByHolderId.httpMethod, onComplete:
                (serverResponse) =>
                {
                    var parsedResponse = LootLockerResponse.Deserialize<LootLockerGetWalletResponse>(serverResponse);
                    if (!parsedResponse.success && parsedResponse.statusCode == 404)
                    {
                        LootLockerCreateWalletRequest request = new LootLockerCreateWalletRequest()
                        {
                            holder_id = holderUlid,
                            holder_type = holderType.ToString()
                        };
                        LootLockerServerRequest.CallAPI(LootLockerEndPoints.createWallet.endPoint,
                            LootLockerEndPoints.createWallet.httpMethod, LootLockerJson.SerializeObject(request),
                            createWalletResponse =>
                            {
                                if (createWalletResponse.success)
                                {
                                    LootLockerServerRequest.CallAPI(endpoint,
                                        LootLockerEndPoints.getWalletByHolderId.httpMethod, null,
                                        secondResponse =>
                                        {
                                            LootLockerResponse.Deserialize(onComplete, secondResponse);
                                        });
                                    return;
                                }

                                onComplete?.Invoke(parsedResponse);
                            });
                        return;
                    }

                    onComplete?.Invoke(parsedResponse);
                }
            );
        }

        /// <summary>
        /// Credit (increase) the specified amount of the provided currency to the provided wallet
        /// </summary>
        /// <param name="walletId">Unique ID of the wallet to credit the given amount of the given currency to</param>
        /// <param name="currencyId">Unique ID of the currency to credit</param>
        /// <param name="amount">The amount of the given currency to credit to the given wallet</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void CreditBalanceToWallet(string walletId, string currencyId, string amount, Action<LootLockerCreditWalletResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCreditWalletResponse>());
                return;
            }

            var json = LootLockerJson.SerializeObject(new LootLockerCreditRequest() { amount = amount, currency_id = currencyId, wallet_id = walletId });

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.creditBalanceToWallet.endPoint, LootLockerEndPoints.creditBalanceToWallet.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Debit (decrease) the specified amount of the provided currency to the provided wallet
        /// </summary>
        /// <param name="walletId">Unique ID of the wallet to debit the given amount of the given currency from</param>
        /// <param name="currencyId">Unique ID of the currency to debit</param>
        /// <param name="amount">The amount of the given currency to debit from the given wallet</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void DebitBalanceToWallet(string walletId, string currencyId, string amount, Action<LootLockerDebitWalletResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDebitWalletResponse>());
                return;
            }

            var json = LootLockerJson.SerializeObject(new LootLockerDebitRequest() { amount = amount, currency_id = currencyId, wallet_id = walletId });

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.debitBalanceToWallet.endPoint, LootLockerEndPoints.debitBalanceToWallet.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region Catalog
        /// <summary>
        /// List the catalogs available for the game
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void ListCatalogs(Action<LootLockerListCatalogsResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListCatalogsResponse>());
                return;
            }

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.listCatalogs.endPoint, LootLockerEndPoints.listCatalogs.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// List the items available in a specific catalog
        /// </summary>
        /// <param name="catalogKey">Unique Key of the catalog that you want to get items for</param>
        /// <param name="count">Amount of catalog items to receive. Use null to simply get the default amount.</param>
        /// <param name="after">Used for pagination, this is the cursor to start getting items from. Use null to get items from the beginning. Use the cursor from a previous call to get the next count of items in the list.</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void ListCatalogItems(string catalogKey, int count, string after, Action<LootLockerListCatalogPricesResponse> onComplete)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListCatalogPricesResponse>());
                return;
            }
            var endpoint = string.Format(LootLockerEndPoints.listCatalogItemsByKey.endPoint, catalogKey); 
            
            endpoint += "?";
            if (count > 0)
                endpoint += $"per_page={count}&";

            if (!string.IsNullOrEmpty(after))
                endpoint += $"cursor={after}&";

            LootLockerServerRequest.CallAPI(endpoint, LootLockerEndPoints.listCatalogItemsByKey.httpMethod, onComplete: (serverResponse) => { onComplete?.Invoke(new LootLockerListCatalogPricesResponse(serverResponse)); });
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

        /// <summary>
        /// Get the Platform the user last used. This can be used to know what login method to prompt.
        /// </summary>
        /// <returns>The platform that was last used by the user</returns>
        public static Platforms GetLastActivePlatform()
        {
            if (CurrentPlatform.Get() == Platforms.None)
            {
                return (Platforms)PlayerPrefs.GetInt("LastActivePlatform");
            }
            else
            {
                return CurrentPlatform.Get();
            }
        }


        #endregion
    }

    public class ResponseError
    {
        public bool success { get; set; }
        public string error { get; set; }
        public string[] messages { get; set; }
        public string error_id { get; set; }
    }
}
