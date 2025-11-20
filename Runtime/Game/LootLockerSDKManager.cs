using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using LootLocker.LootLockerEnums;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine.Networking;
#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using LLlibs.ZeroDepJson;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LootLocker.Requests
{
    public partial class LootLockerSDKManager
    {

        /// <summary>
        /// Stores which platform the player currently has a session for.
        /// </summary>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static string GetCurrentPlatform(string forPlayerWithUlid = null)
        {
            return LootLockerAuthPlatform.GetPlatformRepresentation(GetLastActivePlatform(forPlayerWithUlid)).PlatformString;
        }

        #region Init
        
        static bool Init()
        {
            // Initialize the lifecycle manager which will set up HTTP client
            var _ = LootLockerLifecycleManager.Instance;
            return LootLockerConfig.ValidateSettings();
        }

        /// <summary>
        /// Manually initialize the SDK.
        /// </summary>
        /// <param name="apiKey">Find the Game API-key at https://console.lootlocker.com/settings/api-keys and click on the API-tab</param>
        /// <param name="gameVersion">The current version of the game in the format 1.2.3.4 (the 3 and 4 being optional but recommended)</param>
        /// <param name="domainKey">Extra key needed for some endpoints, can be found by going to https://console.lootlocker.com/settings/api-keys and click on the API-tab</param>
        /// <param name="logLevel">What log level to use for the SDKs internal logging</param>
        /// <returns>True if initialized successfully, false otherwise</returns>
        public static bool Init(string apiKey, string gameVersion, string domainKey, LootLockerLogger.LogLevel logLevel = LootLockerLogger.LogLevel.Info)
        {
            // Create new settings first
            bool configResult = LootLockerConfig.CreateNewSettings(apiKey, gameVersion, domainKey, logLevel);
            if (!configResult)
            {
                return false;
            }
            
            // Reset and reinitialize the lifecycle manager with new settings
            LootLockerLifecycleManager.ResetInstance();
            var _ = LootLockerLifecycleManager.Instance;
            
            return LootLockerLifecycleManager.IsReady;
        }

        static bool LoadConfig()
        {
            return LootLockerConfig.ValidateSettings();
        }

        /// <summary>
        /// Checks if an active session exists.
        /// </summary>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <returns>True if a token is found, false otherwise.</returns>
        private static bool CheckActiveSession(string forPlayerWithUlid = null)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            return !string.IsNullOrEmpty(playerData?.SessionToken);
        }



        /// <summary>
        /// Utility function to check if the sdk has been initialized
        /// </summary>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <returns>True if initialized, false otherwise.</returns>
        public static bool CheckInitialized(bool skipSessionCheck = false, string forPlayerWithUlid = null)
        {
            // Check if lifecycle manager exists and is ready, if not try to initialize
            if (!LootLockerLifecycleManager.IsReady)
            {
                if (!Init())
                {
                    return false;
                }
                
                // Double check that initialization succeeded
                if (!LootLockerLifecycleManager.IsReady)
                {
                    LootLockerLogger.Log("LootLocker services are still initializing. Please try again in a moment or ensure LootLockerConfig.current is properly set.", LootLockerLogger.LogLevel.Warning);
                    return false;
                }
            }

            if (skipSessionCheck)
            {
                return true;
            }

            return CheckActiveSession(forPlayerWithUlid);
        }

#if LOOTLOCKER_ENABLE_HTTP_CONFIGURATION_OVERRIDE
        public static void _OverrideLootLockerHTTPClientConfiguration(int maxRetries, int incrementalBackoffFactor, int initialRetryWaitTime)
        {
            LootLockerHTTPClient.Get().OverrideConfiguration(new LootLockerHTTPClientConfiguration(maxRetries, incrementalBackoffFactor, initialRetryWaitTime));
        }

        public static void _OverrideLootLockerCertificateHandler(CertificateHandler certificateHandler)
        {
            LootLockerHTTPClient.Get().OverrideCertificateHandler(certificateHandler);
        }
#endif


        #endregion

        #region SDK Customization
        #if LOOTLOCKER_ENABLE_OVERRIDABLE_STATE_WRITER
        public static void SetStateWriter(ILootLockerStateWriter stateWriter)
        {
            LootLockerStateData.overrideStateWriter(stateWriter);
        }
        #endif

        /// <summary>
        /// Reset all SDK services and state. 
        /// This will reset all managed services through the lifecycle manager and clear local state.
        /// Call this if you need to completely reinitialize the SDK without restarting the application.
        /// Note: After calling this method, you will need to re-authenticate and reinitialize.
        /// </summary>
        /// <summary>
        /// Reset the entire LootLocker SDK, clearing all services and state.
        /// This will terminate all ongoing requests and reset all cached data.
        /// Call this when switching between different game contexts or during application cleanup.
        /// After calling this method, you'll need to re-initialize the SDK before making API calls.
        /// </summary>
        public static void ResetSDK()
        {
            LootLockerLogger.Log("Resetting LootLocker SDK - all services and state will be cleared", LootLockerLogger.LogLevel.Info);
            
            // Reset the lifecycle manager which will reset all managed services and coordinate with StateData
            LootLockerLifecycleManager.ResetInstance();
            
            LootLockerLogger.Log("LootLocker SDK reset complete", LootLockerLogger.LogLevel.Info);
        }
        #endregion

        #region Multi-User Management

        /// <summary>
        /// Get the information from the stored state for the player with the specified ULID.
        /// </summary>
        /// <returns>The data stored for the specified player. Will be empty if no data is found.</returns>
        public static LootLockerPlayerData GetPlayerDataForPlayerWithUlid(string playerUlid)
        {
            return LootLockerStateData.GetPlayerDataForPlayerWithUlidWithoutChangingState(playerUlid);
        }

        /// <summary>
        /// Get a list of player ULIDs that have been active since game start (or state initialization).
        /// </summary>
        /// <returns>List of player ULIDs that have been active since game start.</returns>
        public static List<string> GetActivePlayerUlids()
        {
            return LootLockerStateData.GetActivePlayerULIDs();
        }

        /// <summary>
        /// Make the state for the player with the specified ULID to be "inactive".
        /// 
        /// This will not delete the state, but it will remove it from the list of active players.
        /// </summary>
        /// <param name="playerUlid">The ULID of the player whose state should be set to inactive.</param>
        public static void SetPlayerUlidToInactive(string playerUlid)
        {
            LootLockerStateData.SetPlayerULIDToInactive(playerUlid);
        }

        /// <summary>
        /// Make the state for all currently active players to be "inactive".
        /// 
        /// This will not delete the state, but it will remove all players from the list of active players.
        /// </summary>
        public static void SetAllPlayersToInactive()
        {
            LootLockerStateData.SetAllPlayersToInactive();
        }

        /// <summary>
        /// Make the state for all currently active players except the specified player to be "inactive".
        /// 
        /// This will not delete the state, but it will remove all players except the specified one from the list of active players.
        /// </summary>
        /// <param name="playerUlid">The ULID of the player to keep active.</param>
        public static void SetAllPlayersToInactiveExceptForPlayer(string playerUlid)
        {
            LootLockerStateData.SetAllPlayersToInactiveExceptForPlayer(playerUlid);
        }

        /// <summary>
        /// Get a list of player ULIDs that there is a stored state for.
        /// This includes both active and inactive players.
        /// </summary>
        /// <returns>List of player ULIDs that have a stored state.</returns>
        public static List<string> GetCachedPlayerUlids()
        {
            return LootLockerStateData.GetCachedPlayerULIDs();
        }

        /// <summary>
        /// Get the ULID of the player state that is used as the default state for calls that have no other player specified.
        /// </summary>
        /// <returns>The ULID of the default player state.</returns>
        public static string GetDefaultPlayerUlid()
        {
            return LootLockerStateData.GetDefaultPlayerULID();
        }

        /// <summary>
        /// Set the player state that is used as the default state for calls that have no other player specified.
        /// </summary>
        /// <param name="playerUlid">The ULID of the player state to set as default.</param>
        /// <returns>True if the default player ULID was set successfully, false otherwise.</returns>
        public static bool SetDefaultPlayerUlid(string playerUlid)
        {
            return LootLockerStateData.SetDefaultPlayerULID(playerUlid);
        }

        /// <summary>
        /// Get the player state for the player with the specified ULID, or the default player state if the supplied player ULID is empty, or an empty state if none of the previous are present.
        /// </summary>
        /// <param name="playerUlid">The ULID of the player whose state should be retrieved.</param>
        /// <returns>The player state for the specified player, or the default player state if the supplied ULID is empty or could not be found, or an empty state if none of the previous are valid.</returns>
        public static LootLockerPlayerData GetSavedStateOrDefaultOrEmptyForPlayer(string playerUlid)
        {
            return LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
        }

        /// <summary>
        /// Remove stored state information for the specified player if present (player will need to re-authenticate).
        /// If the player is the default player, the default player will be set to an empty state.
        /// If the player is not the default player, the state will be removed but the default player will not be changed.
        /// If the player is not found, no action will be taken.
        /// </summary> 
        /// <param name="playerUlid">The ULID of the player whose state should be cleared.</param>
        public static void ClearCacheForPlayer(string playerUlid)
        {
            LootLockerStateData.ClearSavedStateForPlayerWithULID(playerUlid);
        }

        /// <summary>
        /// Remove all stored state information (players will need to re-authenticate).
        /// This will clear all player states, including the default player state.
        /// If you want to clear the state for a specific player, use ClearCacheForPlayer(string playerUlid) instead.
        /// This will also reset the default player to an empty state.
        /// </summary>
        public static void ClearAllPlayerCaches()
        {
            LootLockerStateData.ClearAllSavedStates();
        }

        /// <summary>
        /// Remove all stored state information except for the specified player (players will need to re-authenticate).
        /// This will clear all player states except for the specified player.
        /// If the specified player is the default player, it will remain as the default player.
        /// </summary>
        /// <param name="playerUlid">The ULID of the player to save the cache for.</param>
        public static void ClearAllPlayerCachesExceptForPlayer(string playerUlid)
        {
            LootLockerStateData.ClearAllSavedStatesExceptForPlayer(playerUlid);
        }

        #endregion

        #region Authentication
        /// <summary>
        /// Verify the player's identity with the server and selected platform.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerVerifyResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        [Obsolete("This method is deprecated, please use VerifyPlayerAndStartPlaystationNetworkSession or VerifyPlayerAndStartSteamSession instead.")] // Deprecation date 20250922
        public static void VerifyID(string deviceId, Action<LootLockerVerifyResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerVerifyResponse>(forPlayerWithUlid));
                return;
            }

            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            if (playerData == null || !playerData.Identifier.Equals(deviceId))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerVerifyResponse>($"The provided deviceId did not match the identifier on player with ulid {forPlayerWithUlid}", forPlayerWithUlid));
                return;
            }
            LootLockerVerifyRequest verifyRequest = new LootLockerVerifyRequest(deviceId, playerData.CurrentPlatform.PlatformString);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }

        /// <summary>
        /// Start a Playstation Network session
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// <param name="psnOnlineId">The player's Online ID</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        [Obsolete("This method is deprecated, please use VerifyPlayerAndStartPlaystationNetworkSession instead.")] // Deprecation date 20250922
        public static void StartPlaystationNetworkSession(string psnOnlineId, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.authenticationRequest.endPoint, LootLockerEndPoints.authenticationRequest.httpMethod, 
                    LootLockerJson.SerializeObject(new LootLockerSessionRequest(psnOnlineId, LL_AuthPlatforms.PlayStationNetwork)), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = "",
                            ULID = response.player_ulid,
                            Identifier = psnOnlineId,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.PlayStationNetwork),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                        };
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Start a Playstation Network session. If your token starts with v3, then you should use VerifyPlayerAndStartPlaystationNetworkV3Session instead.
        /// 
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// 
        /// <param name="AuthCode">The authorization code received from PSN after a successful login</param>
        /// <param name="AccountId">The numeric representation of the account id received from PSN after a successful login</param>
        /// <param name="PsnIssuerId">Optional: The PSN issuer id to use when verifying the player towards PSN. If not supplied, will be defaulted to 256=production.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void VerifyPlayerAndStartPlaystationNetworkSession(string AuthCode, long AccountId, Action<LootLockerSessionResponse> onComplete, int PsnIssuerId = 256, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            LootLockerPlaystationNetworkVerificationRequest verificationRequest = new LootLockerPlaystationNetworkVerificationRequest
            {
                token = AuthCode,
                psn_issuer_id = PsnIssuerId
            };

            LootLockerServerRequest.CallAPI(null, LootLockerEndPoints.playerVerification.endPoint, LootLockerEndPoints.playerVerification.httpMethod, LootLockerJson.SerializeObject(verificationRequest), onComplete: (verificationResponse) =>
            {
                if (!verificationResponse.success)
                {
                    onComplete?.Invoke(LootLockerResponseFactory.FailureResponseConversion<LootLockerSessionResponse>(verificationResponse));
                    return;
                }

                LootLockerSessionRequest sessionRequest = new LootLockerSessionRequest(AccountId.ToString(), LL_AuthPlatforms.PlayStationNetwork, Optionals);

                LootLockerServerRequest.CallAPI(null, LootLockerEndPoints.authenticationRequest.endPoint, LootLockerEndPoints.authenticationRequest.httpMethod, LootLockerJson.SerializeObject(sessionRequest), onComplete: (serverResponse) =>
                {
                    var sessionResponse = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                    if (sessionResponse.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = sessionResponse.session_token,
                            RefreshToken = "",
                            ULID = sessionResponse.player_ulid,
                            Identifier = AccountId.ToString(),
                            PublicUID = sessionResponse.public_uid,
                            LegacyID = sessionResponse.player_id,
                            Name = sessionResponse.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.PlayStationNetwork),
                            LastSignIn = DateTime.Now,
                            CreatedAt = sessionResponse.player_created_at,
                            WalletID = sessionResponse.wallet_id,
                            SessionOptionals = Optionals
                        };
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(sessionResponse);
                });
            });
        }

        /// <summary>
        /// Start a Playstation Network session using the v3 version of PSN authentication. If your token starts with v3, then you're using this version.
        /// 
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// 
        /// <param name="AuthCode">The authorization code received from PSN after a successful login</param>
        /// <param name="EnvIssuerId">Optional: The PSN Environment issuer id to use when verifying the player towards PSN. If not supplied, will be defaulted to 256=production.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void VerifyPlayerAndStartPlaystationNetworkV3Session(string AuthCode, Action<LootLockerPlaystationV3SessionResponse> onComplete, int EnvIssuerId = 256, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlaystationV3SessionResponse>(null));
                return;
            }

            LootLockerPlaystationNetworkV3SessionRequest sessionRequest = new LootLockerPlaystationNetworkV3SessionRequest
            {
                auth_code = AuthCode,
                env_iss_id = EnvIssuerId,
                optionals = Optionals
            };

            LootLockerServerRequest.CallAPI(null, LootLockerEndPoints.playstationNetworkv3SessionRequest.endPoint, LootLockerEndPoints.playstationNetworkv3SessionRequest.httpMethod, LootLockerJson.SerializeObject(sessionRequest), onComplete: (serverResponse) =>
            {
                var sessionResponse = LootLockerResponse.Deserialize<LootLockerPlaystationV3SessionResponse>(serverResponse);
                if (sessionResponse.success)
                {
                    var playerData = new LootLockerPlayerData
                    {
                        SessionToken = sessionResponse.session_token,
                        RefreshToken = "",
                        ULID = sessionResponse.player_ulid,
                        Identifier = sessionResponse.player_identifier,
                        PublicUID = sessionResponse.public_uid,
                        LegacyID = sessionResponse.player_id,
                        Name = sessionResponse.player_name,
                        WhiteLabelEmail = "",
                        WhiteLabelToken = "",
                        CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.PlayStationNetwork),
                        LastSignIn = DateTime.Now,
                        CreatedAt = sessionResponse.player_created_at,
                        WalletID = sessionResponse.wallet_id,
                        SessionOptionals = Optionals
                    };
                    LootLockerEventSystem.TriggerSessionStarted(playerData);
                }

                onComplete?.Invoke(sessionResponse);
            });
        }

        /// <summary>
        /// Start an Android Network session
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// <param name="deviceId">The player's Device ID</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartAndroidSession(string deviceId, Action<LootLockerSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.authenticationRequest.endPoint, LootLockerEndPoints.authenticationRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerSessionRequest(deviceId, LL_AuthPlatforms.Android, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = "",
                            ULID = response.player_ulid,
                            Identifier = deviceId,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Android),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Start a Amazon Luna session
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// <param name="amazonLunaGuid">The player's Amazon Luna GUID</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartAmazonLunaSession(string amazonLunaGuid, Action<LootLockerSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.authenticationRequest.endPoint, LootLockerEndPoints.authenticationRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerSessionRequest(amazonLunaGuid, LL_AuthPlatforms.AmazonLuna, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = "",
                            ULID = response.player_ulid,
                            Identifier = amazonLunaGuid,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.AmazonLuna),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false);
        }

        /// <summary>
        /// Start a guest session.
        /// </summary>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGuestSessionResponse</param>
        public static void StartGuestSession(Action<LootLockerGuestSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGuestSessionResponse>(null));
                return;
            }

            string defaultPlayerUlid = LootLockerStateData.GetDefaultPlayerULID();
            if (string.IsNullOrEmpty(defaultPlayerUlid) || LootLockerStateData.GetActivePlayerULIDs().Contains(defaultPlayerUlid))
            {
                // Start a new guest session with a new identifier if there is no default player to use or if that player is already playing
                StartGuestSession(null, onComplete);
                return;
            } 
            else if (LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(defaultPlayerUlid)?.CurrentPlatform.Platform != LL_AuthPlatforms.Guest)
            {
                // Also start a new guest session with a new identifier if the default player is not playing but isn't a guest user
                LootLockerStateData.SetPlayerULIDToInactive(defaultPlayerUlid);
                StartGuestSession(null, onComplete);
                return;
            }

            StartGuestSession(LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(defaultPlayerUlid)?.Identifier, onComplete, Optionals);
        }

        /// <summary>
        /// Start a guest session for an already existing player that has previously had active guest sessions on this device
        /// </summary>
        /// <param name="forPlayerWithUlid">Execute the request for the specified player</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGuestSessionResponse</param>
        public static void StartGuestSessionForPlayer(string forPlayerWithUlid, Action<LootLockerGuestSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGuestSessionResponse>(null));
                return;
            }

            if (!LootLockerStateData.SaveStateExistsForPlayer(forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerGuestSessionResponse>($"No save state exists for player with ulid {forPlayerWithUlid}", forPlayerWithUlid));
                return;
            }

            StartGuestSession(LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid)?.Identifier, onComplete, Optionals ?? LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid)?.SessionOptionals);
        }

        /// <summary>
        /// Start a guest session with an identifier, you can use something like SystemInfo.deviceUniqueIdentifier to tie the account to a device.
        /// </summary>
        /// <param name="identifier">Identifier for the player. Set this to empty if you want an identifier to be generated for you.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGuestSessionResponse</param>
        public static void StartGuestSession(string identifier, Action<LootLockerGuestSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGuestSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.guestSessionRequest.endPoint, LootLockerEndPoints.guestSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerSessionRequest(identifier, LL_AuthPlatforms.Guest, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerGuestSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = "",
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Guest),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Start a steam session. You can read more on how to setup Steam with LootLocker here; https://docs.lootlocker.com/how-to/authentication/steam
        /// </summary>
        /// <param name="ticket">The Steam session ticket received from Steam Authentication</param>
        /// <param name="ticketSize">The size of the Steam session ticket received from Steam Authentication</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void VerifyPlayerAndStartSteamSession(ref byte[] ticket, uint ticketSize, Action<LootLockerSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            var sessionTicket = _SteamSessionTicket(ref ticket, ticketSize);
            LootLockerServerRequest.CallAPI(null, LootLockerEndPoints.steamSessionRequest.endPoint, LootLockerEndPoints.steamSessionRequest.httpMethod, LootLockerJson.SerializeObject(new LootLockerSteamSessionRequest{ steam_ticket = sessionTicket , optionals = Optionals }), onComplete: (serverResponse) => {
                var sessionResponse = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                if (sessionResponse.success)
                {
                    var playerData = new LootLockerPlayerData
                    {
                        SessionToken = sessionResponse.session_token,
                        RefreshToken = "",
                        ULID = sessionResponse.player_ulid,
                        Identifier = "",
                        PublicUID = sessionResponse.public_uid,
                        LegacyID = sessionResponse.player_id,
                        Name = sessionResponse.player_name,
                        WhiteLabelEmail = "",
                        WhiteLabelToken = "",
                        CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Steam),
                        LastSignIn = DateTime.Now,
                        CreatedAt = sessionResponse.player_created_at,
                        WalletID = sessionResponse.wallet_id,
                        SessionOptionals = Optionals
                    };
                    
                    LootLockerEventSystem.TriggerSessionStarted(playerData);
                }

                onComplete?.Invoke(sessionResponse);
            });
        }

        /// <summary>
        /// Start a steam session. You can read more on how to setup Steam with LootLocker here; https://docs.lootlocker.com/how-to/authentication/steam
        /// </summary>
        /// <param name="ticket">The Steam session ticket received from Steam Authentication</param>
        /// <param name="ticketSize">The size of the Steam session ticket received from Steam Authentication</param>
        /// <param name="steamAppId">The steam app id to start this steam session for</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void VerifyPlayerAndStartSteamSessionWithSteamAppId(ref byte[] ticket, uint ticketSize, string steamAppId, Action<LootLockerSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            var sessionTicket = _SteamSessionTicket(ref ticket, ticketSize);
            LootLockerServerRequest.CallAPI(null, LootLockerEndPoints.steamSessionRequest.endPoint, LootLockerEndPoints.steamSessionRequest.httpMethod, LootLockerJson.SerializeObject(new LootLockerSteamSessionWithAppIdRequest { steam_ticket = sessionTicket, steam_app_id = steamAppId, optionals = Optionals }), onComplete: (serverResponse) => {
                var sessionResponse = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                if (sessionResponse.success)
                {
                    var playerData = new LootLockerPlayerData
                    {
                        SessionToken = sessionResponse.session_token,
                        RefreshToken = "",
                        ULID = sessionResponse.player_ulid,
                        Identifier = "",
                        PublicUID = sessionResponse.public_uid,
                        LegacyID = sessionResponse.player_id,
                        Name = sessionResponse.player_name,
                        WhiteLabelEmail = "",
                        WhiteLabelToken = "",
                        CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Steam),
                        LastSignIn = DateTime.Now,
                        CreatedAt = sessionResponse.player_created_at,
                        WalletID = sessionResponse.wallet_id,
                        SessionOptionals = Optionals
                    };
                    
                    LootLockerEventSystem.TriggerSessionStarted(playerData);
                }

                onComplete?.Invoke(sessionResponse);
            });
        }

        /// <summary>
        /// Convert a steam ticket so LootLocker can read it. You can read more on how to setup Steam with LootLocker here; https://docs.lootlocker.com/how-to/authentication/steam
        /// </summary>
        /// <param name="ticket">The Steam session ticket received from Steam Authentication</param>
        /// <param name="ticketSize">The size of the Steam session ticket received from Steam Authentication</param>
        /// <returns>A converted SteamSessionTicket as a string for use with VerifyPlayer.</returns>
        private static string _SteamSessionTicket(ref byte[] ticket, uint ticketSize)
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
        /// Create a new session for a Nintendo Switch user
        /// The Nintendo Switch platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="nsa_id_token">nsa (Nintendo Switch Account) id token as a string</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartNintendoSwitchSession(string nsa_id_token, Action<LootLockerSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.nintendoSwitchSessionRequest.endPoint, LootLockerEndPoints.nintendoSwitchSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerNintendoSwitchSessionRequest(nsa_id_token, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = "",
                            ULID = response.player_ulid,
                            Identifier = "",
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.NintendoSwitch),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Create a new session for a Xbox One user
        /// The Xbox One platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="xbox_user_token">Xbox user token as a string</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of typeLootLockerSessionResponse</param>
        public static void StartXboxOneSession(string xbox_user_token, Action<LootLockerSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.xboxSessionRequest.endPoint, LootLockerEndPoints.xboxSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerXboxOneSessionRequest(xbox_user_token, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = "",
                            ULID = response.player_ulid,
                            Identifier = "",
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.XboxOne),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Start a Game session for a Google User
        /// The Google sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="idToken">The Id Token from google sign in</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartGoogleSession(string idToken, Action<LootLockerGoogleSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGoogleSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.googleSessionRequest.endPoint, LootLockerEndPoints.googleSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerGoogleSignInSessionRequest(idToken, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerGoogleSessionResponse.Deserialize<LootLockerGoogleSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Google),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Start a Game session for a Google User
        /// The Google sign in platform must be enabled in the web console for this to work.
        /// Desired Google platform also must be configured under advanced options in the web console.
        /// </summary>
        /// <param name="idToken">The Id Token from google sign in</param>
        /// <param name="googlePlatform">Google OAuth2 ClientID platform</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void StartGoogleSession(string idToken, GooglePlatform googlePlatform, Action<LootLockerGoogleSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGoogleSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.googleSessionRequest.endPoint, LootLockerEndPoints.googleSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerGoogleSignInWithPlatformSessionRequest(idToken, googlePlatform.ToString(), Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerGoogleSessionResponse.Deserialize<LootLockerGoogleSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Google),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Refresh a previous session signed in with Google.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Google sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RefreshGoogleSession(Action<LootLockerGoogleSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            RefreshGoogleSession(null, onComplete, forPlayerWithUlid, Optionals);
        }

        /// <summary>
        /// Refresh a previous session signed in with Google.
        /// If you do not want to manually handle the refresh token we recommend using the RefreshGoogleSession(Action<LootLockerGoogleSessionResponse> onComplete, string forPlayerWithUlid) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Google sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="refresh_token">Token received in response from StartGoogleSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RefreshGoogleSession(string refresh_token, Action<LootLockerGoogleSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGoogleSessionResponse>(null));
                return;
            }

            if (string.IsNullOrEmpty(refresh_token))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
                if (string.IsNullOrEmpty(playerData?.RefreshToken))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerGoogleSessionResponse>(playerData?.ULID));
                    return;
                }

                refresh_token = playerData.RefreshToken;
            }

            if (Optionals == null)
            {
                Optionals = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid)?.SessionOptionals;
            }

            LootLockerServerRequest.CallAPI(null,
                LootLockerEndPoints.googleSessionRequest.endPoint, LootLockerEndPoints.googleSessionRequest.httpMethod,
                LootLockerJson.SerializeObject(new LootLockerGoogleRefreshSessionRequest(refresh_token, Optionals)),
                (serverResponse) =>
                {
                    var response = LootLockerGoogleSessionResponse.Deserialize<LootLockerGoogleSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Google),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionRefreshed(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }
        
        /// <summary>
        /// Start a Google Play Games Services session.
        /// The Google Play Games sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="authCode">The auth code received from Google Play Games Services authentication.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void StartGooglePlayGamesSession(string authCode, Action<LootLockerGooglePlayGamesSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(null);
                return;
            }
            var request = new Requests.LootLockerGooglePlayGamesSessionRequest(authCode, Optionals);
            LootLockerServerRequest.CallAPI(
                null,
                LootLocker.LootLockerEndPoints.googlePlayGamesSessionRequest.endPoint,
                LootLocker.LootLockerEndPoints.googlePlayGamesSessionRequest.httpMethod,
                LootLockerJson.SerializeObject(request),
                (serverResponse) =>
                {
                    var response = LootLockerGooglePlayGamesSessionResponse.Deserialize<LootLockerGooglePlayGamesSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.GooglePlayGames),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Refresh a previous session signed in with Google Play Games
        /// A response code of 401 (Unauthorized) means the refresh token has expired and you'll need to sign in again
        /// The Google Play Games sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="refreshToken">The refresh token received from a previous GPGS session.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void RefreshGooglePlayGamesSession(string refreshToken, Action<LootLockerGooglePlayGamesSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(null);
                return;
            }
            var request = new Requests.LootLockerGooglePlayGamesRefreshSessionRequest(refreshToken, Optionals);
            LootLockerServerRequest.CallAPI(
                null,
                LootLocker.LootLockerEndPoints.googlePlayGamesRefreshSessionRequest.endPoint,
                LootLocker.LootLockerEndPoints.googlePlayGamesRefreshSessionRequest.httpMethod,
                LootLockerJson.SerializeObject(request),
                (serverResponse) =>
                {
                    var response = LootLockerGooglePlayGamesSessionResponse.Deserialize<LootLockerGooglePlayGamesSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.GooglePlayGames),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionRefreshed(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Refresh a previous session signed in with Google Play Games
        /// A response code of 401 (Unauthorized) means the refresh token has expired and you'll need to sign in again
        /// The Google Play Games sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RefreshGooglePlayGamesSession(Action<LootLockerGooglePlayGamesSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGooglePlayGamesSessionResponse>(null));
                return;
            }
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            if (string.IsNullOrEmpty(playerData?.RefreshToken))
            {
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerGooglePlayGamesSessionResponse>(playerData?.ULID));
                return;
            }

            RefreshGooglePlayGamesSession(playerData.RefreshToken, onComplete, Optionals ?? playerData?.SessionOptionals);
        }

        /// <summary>
        /// Create a new session for Sign in with Apple
        /// The Apple sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="authorization_code">Authorization code, provided by apple</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAppleSessionResponse</param>
        public static void StartAppleSession(string authorization_code, Action<LootLockerAppleSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAppleSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.appleSessionRequest.endPoint, LootLockerEndPoints.appleSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerAppleSignInSessionRequest(authorization_code, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerAppleSessionResponse.Deserialize<LootLockerAppleSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.AppleSignIn),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Refresh a previous session signed in with Apple
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Apple sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAppleSessionResponse</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RefreshAppleSession(Action<LootLockerAppleSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            RefreshAppleSession(null, onComplete, forPlayerWithUlid, Optionals);
        }

        /// <summary>
        /// Refresh a previous session signed in with Apple
        /// If you do not want to manually handle the refresh token we recommend using the RefreshAppleSession(Action<LootLockerAppleSessionResponse> onComplete, string forPlayerWithUlid) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Apple sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="refresh_token">Token received in response from StartAppleSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAppleSessionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        public static void RefreshAppleSession(string refresh_token, Action<LootLockerAppleSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAppleSessionResponse>(null));
                return;
            }

            if (string.IsNullOrEmpty(refresh_token))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
                if (string.IsNullOrEmpty(playerData?.RefreshToken))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerAppleSessionResponse>(playerData?.ULID));
                    return;
                }

                refresh_token = playerData.RefreshToken;
            }

            if (Optionals == null)
            {
                Optionals = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid)?.SessionOptionals;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.appleSessionRequest.endPoint, LootLockerEndPoints.appleSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerAppleRefreshSessionRequest(refresh_token, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerAppleSessionResponse.Deserialize<LootLockerAppleSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.AppleSignIn),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionRefreshed(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
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
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response of type  for handling the response
        public static void StartAppleGameCenterSession(string bundleId, string playerId, string publicKeyUrl, string signature, string salt, long timestamp, Action<LootLockerAppleGameCenterSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAppleGameCenterSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.appleGameCenterSessionRequest.endPoint, LootLockerEndPoints.appleGameCenterSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerAppleGameCenterSessionRequest(bundleId, playerId, publicKeyUrl, signature, salt, timestamp, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerAppleGameCenterSessionResponse.Deserialize<LootLockerAppleGameCenterSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = "",
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.AppleGameCenter),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Refresh a previous session signed in with Apple Game Center
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Apple Game Center sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type  for handling the response of type LootLockerAppleGameCenterSessionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        public static void RefreshAppleGameCenterSession(Action<LootLockerAppleGameCenterSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAppleGameCenterSessionResponse>(null));
                return;
            }

            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            if (string.IsNullOrEmpty(playerData?.RefreshToken))
            {
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerAppleGameCenterSessionResponse>(playerData?.ULID));
                return;
            }

            if (Optionals == null)
            {
                Optionals = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid)?.SessionOptionals;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.appleGameCenterSessionRequest.endPoint, LootLockerEndPoints.appleGameCenterSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerAppleGameCenterRefreshSessionRequest(playerData?.RefreshToken, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerAppleGameCenterSessionResponse.Deserialize<LootLockerAppleGameCenterSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        LootLockerEventSystem.TriggerSessionRefreshed(new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = "",
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.AppleGameCenter),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        });
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Create a new session for an Epic Online Services (EOS) user
        /// The Epic Games platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="id_token">EOS Id Token as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerEpicSessionResponse</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        public static void StartEpicSession(string id_token, Action<LootLockerEpicSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerEpicSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.epicSessionRequest.endPoint, LootLockerEndPoints.epicSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerEpicSessionRequest(id_token, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerEpicSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        var playerData = new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = "",
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Epic),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        };
                        
                        LootLockerEventSystem.TriggerSessionStarted(playerData);
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Refresh a previous session signed in with Epic
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Epic sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerEpicSessionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        public static void RefreshEpicSession(Action<LootLockerEpicSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            RefreshEpicSession(null, onComplete, forPlayerWithUlid, Optionals);
        }

        /// <summary>
        /// Refresh a previous session signed in with Epic
        /// If you do not want to manually handle the refresh token we recommend using the RefreshEpicSession(Action<LootLockerEpicSessionResponse> onComplete, string forPlayerWithUlid) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Epic sign in platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="refresh_token">Token received in response from StartEpicSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerEpicSessionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        public static void RefreshEpicSession(string refresh_token, Action<LootLockerEpicSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerEpicSessionResponse>(null));
                return;
            }

            if (string.IsNullOrEmpty(refresh_token))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
                if (string.IsNullOrEmpty(playerData?.RefreshToken))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerEpicSessionResponse>(playerData?.ULID));
                    return;
                }

                refresh_token = playerData.RefreshToken;
            }

            if (Optionals == null)
            {
                Optionals = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid)?.SessionOptionals;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.epicSessionRequest.endPoint, LootLockerEndPoints.epicSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerEpicRefreshSessionRequest(refresh_token, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerEpicSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        LootLockerEventSystem.TriggerSessionRefreshed(new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = "",
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Epic),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        });
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }
        
        /// <summary>
        /// Start a Meta / Oculus session
        /// The Meta / Oculus platform must be enabled and configured in the web console for this to work.
        /// </summary>
        /// <param name="user_id">User ID as a string</param>
        /// <param name="nonce">Nonce as a string</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">Action to handle the response of type LootLockerMetaSessionResponse</param>
        public static void StartMetaSession(string user_id, string nonce, Action<LootLockerMetaSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerMetaSessionResponse>(null));
                return;
            }

            var sessionRequest = new LootLockerMetaSessionRequest()
            {
                user_id = user_id,
                nonce = nonce,
                optionals = Optionals
            };
            var endPoint = LootLockerEndPoints.metaSessionRequest;

            LootLockerServerRequest.CallAPI(null, endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(sessionRequest), (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerMetaSessionResponse>(serverResponse);
                if (response.success)
                {
                    LootLockerEventSystem.TriggerSessionStarted(new LootLockerPlayerData
                    {
                        SessionToken = response.session_token,
                        RefreshToken = response.refresh_token,
                        ULID = response.player_ulid,
                        Identifier = "",
                        PublicUID = response.public_uid,
                        LegacyID = response.player_id,
                        Name = response.player_name,
                        WhiteLabelEmail = "",
                        WhiteLabelToken = "",
                        CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Meta),
                        LastSignIn = DateTime.Now,
                        CreatedAt = response.player_created_at,
                        WalletID = response.wallet_id,
                        SessionOptionals = Optionals
                    });
                }

                onComplete?.Invoke(response);
            }, false);
        }

        /// <summary>
        /// Refresh a previous Meta / Oculus session
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Meta / Oculus platform must be enabled and configured in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerMetaSessionResponse</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RefreshMetaSession(Action<LootLockerMetaSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            RefreshMetaSession(null, onComplete, forPlayerWithUlid, Optionals);
        }

        /// <summary>
        /// Refresh a previous Meta session
        /// If you do not want to manually handle the refresh token we recommend using the RefreshMetaSession(Action<LootLockerMetaSessionResponse> onComplete, string forPlayerWithUlid) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Meta platform must be enabled and configured in the web console for this to work.
        /// </summary>
        /// <param name="refresh_token">Token received in response from StartMetaSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerMetaSessionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        public static void RefreshMetaSession(string refresh_token, Action<LootLockerMetaSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerMetaSessionResponse>(null));
                return;
            }

            if (string.IsNullOrEmpty(refresh_token))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
                if (string.IsNullOrEmpty(playerData?.RefreshToken))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerMetaSessionResponse>(playerData?.ULID));
                    return;
                }

                refresh_token = playerData.RefreshToken;
            }

            if (Optionals == null)
            {
                Optionals = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid)?.SessionOptionals;
            }

            LootLockerServerRequest.CallAPI(null,
                LootLockerEndPoints.metaSessionRequest.endPoint, LootLockerEndPoints.metaSessionRequest.httpMethod,
                LootLockerJson.SerializeObject(new LootLockerMetaRefreshSessionRequest{ refresh_token = refresh_token, optionals = Optionals }),
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerMetaSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        LootLockerEventSystem.TriggerSessionRefreshed(new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = "",
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Meta),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        });
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// Start a Discord session.
        /// The Discord platform must be enabled and configured in the web console for this to work.
        /// A game can support multiple platforms, but it is recommended that a build only supports one platform.
        /// </summary>
        /// <param name="accessToken">The player's Discord OAuth token</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void StartDiscordSession(string accessToken, Action<LootLockerDiscordSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(null);
                return;
            }

            LootLockerServerRequest.CallAPI(null,
                LootLockerEndPoints.discordSessionRequest.endPoint,
                LootLockerEndPoints.discordSessionRequest.httpMethod,
                LootLockerJson.SerializeObject(new LootLockerDiscordSessionRequest(accessToken, Optionals)),
                (serverResponse) =>
                    {
                        var response = LootLockerResponse.Deserialize<LootLockerDiscordSessionResponse>(serverResponse);
                        if (response.success)
                        {
                            LootLockerEventSystem.TriggerSessionStarted(new LootLockerPlayerData
                            {
                                SessionToken = response.session_token,
                                RefreshToken = response.refresh_token,
                                ULID = response.player_ulid,
                                Identifier = "",
                                PublicUID = response.public_uid,
                                LegacyID = response.player_id,
                                Name = response.player_name,
                                WhiteLabelEmail = "",
                                WhiteLabelToken = "",
                                CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Discord),
                                LastSignIn = DateTime.Now,
                                CreatedAt = response.player_created_at,
                                WalletID = response.wallet_id,
                                SessionOptionals = Optionals
                            });
                        }

                        onComplete?.Invoke(response);
                    }, 
                false
            );
        }

        /// <summary>
        /// Refresh a previous Discord session
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Discord platform must be enabled and configured in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RefreshDiscordSession(Action<LootLockerDiscordSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            if (string.IsNullOrEmpty(playerData?.RefreshToken))
            {
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerDiscordSessionResponse>(playerData?.ULID));
                return;
            }

            if (Optionals == null)
            {
                Optionals = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid)?.SessionOptionals;
            }

            RefreshDiscordSession(playerData.RefreshToken, onComplete, forPlayerWithUlid, Optionals);
        }

        /// <summary>
        /// Refresh a previous Discord session
        /// If you do not want to manually handle the refresh token we recommend using the RefreshDiscordSession(Action<LootLockerDiscordSessionResponse> onComplete, string forPlayerWithUlid) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// The Discord platform must be enabled and configured in the web console for this to work.
        /// </summary>
        /// <param name="refresh_token">Token received in response from StartDiscordSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerDiscordSessionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <param name="Optionals">Optional: Additional session options</param>
        public static void RefreshDiscordSession(string refresh_token, Action<LootLockerDiscordSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDiscordSessionResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(refresh_token))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
                if (string.IsNullOrEmpty(playerData?.RefreshToken))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerDiscordSessionResponse>(playerData?.ULID));
                    return;
                }
                refresh_token = playerData.RefreshToken;
                forPlayerWithUlid = playerData.ULID;
            }

            if (Optionals == null)
            {
                Optionals = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid)?.SessionOptionals;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, 
                LootLockerEndPoints.discordSessionRequest.endPoint, LootLockerEndPoints.discordSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerDiscordRefreshSessionRequest(refresh_token, Optionals)), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerDiscordSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        LootLockerEventSystem.TriggerSessionRefreshed(new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = "",
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Discord),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = Optionals
                        });
                    }

                    onComplete?.Invoke(response);
                }, 
                false
            );
        }

        /// <summary>
        /// End active session (if any exists)
        /// Succeeds if a session was ended or no sessions were active
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        /// <param name="clearLocalState">If set to true all local data about the player will be removed from the device if the session is successfully ended</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void EndSession(Action<LootLockerSessionResponse> onComplete, bool clearLocalState = false, string forPlayerWithUlid = null)
        {
            if (string.IsNullOrEmpty(forPlayerWithUlid))
            {
                forPlayerWithUlid = LootLockerStateData.GetDefaultPlayerULID();
            }
            if (!CheckInitialized(true) || !CheckActiveSession(forPlayerWithUlid))
            {
                onComplete?.Invoke(new LootLockerSessionResponse() { success = true, text = "No active session" });
                return;
            }

            EndPointClass endPoint = LootLockerEndPoints.endingSession;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null,
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        LootLockerEventSystem.TriggerSessionEnded(serverResponse.requestContext.player_ulid, clearLocalState);
                    }

                    onComplete?.Invoke(response);
                }
            );
        }

        /// <summary>
        /// Clears client session data. WARNING: This does not end the session in LootLocker servers.
        /// </summary>
        /// <param name="forPlayerWithUlid">Execute the request for the specified player.</param>
        public static void ClearLocalSession(string forPlayerWithUlid)
        {
            ClearCacheForPlayer(forPlayerWithUlid);
        }
        #endregion

        #region Event System

        /// <summary>
        /// Subscribe to SDK events using the unified event system
        /// </summary>
        /// <typeparam name="T">The event data type</typeparam>
        /// <param name="eventType">The event type to subscribe to</param>
        /// <param name="handler">The event handler</param>
        public static void Subscribe<T>(LootLockerEventType eventType, LootLockerEventHandler<T> handler) where T : LootLockerEventData
        {
            LootLockerEventSystem.Subscribe(eventType, handler);
        }

        /// <summary>
        /// Unsubscribe from SDK events
        /// </summary>
        /// <typeparam name="T">The event data type</typeparam>
        /// <param name="eventType">The event type to unsubscribe from</param>
        /// <param name="handler">The event handler to remove</param>
        public static void Unsubscribe<T>(LootLockerEventType eventType, LootLockerEventHandler<T> handler) where T : LootLockerEventData
        {
            LootLockerEventSystem.Unsubscribe(eventType, handler);
        }

        #endregion

        #region Presence

#if LOOTLOCKER_ENABLE_PRESENCE
        /// <summary>
        /// Force start the Presence WebSocket connection manually. 
        /// This will override the automatic presence management and manually establish a connection.
        /// Use this when you need precise control over presence connections, otherwise let the SDK auto-manage.
        /// </summary>
        /// <param name="onComplete">Callback indicating whether the connection and authentication succeeded</param>
        /// <param name="forPlayerWithUlid">Optional: Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ForceStartPresenceConnection(
            LootLockerPresenceCallback onComplete = null,
            string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(false, "SDK not initialized");
                return;
            }

            // Connect with simple completion callback
            LootLockerPresenceManager.ConnectPresence(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Force stop the Presence WebSocket connection manually.
        /// This will override the automatic presence management and manually disconnect.
        /// Use this when you need precise control over presence connections, otherwise let the SDK auto-manage.
        /// </summary>
        /// <param name="onComplete">Optional callback indicating whether the disconnection succeeded</param>
        /// <param name="forPlayerWithUlid">Optional: Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ForceStopPresenceConnection(
            LootLockerPresenceCallback onComplete = null,
            string forPlayerWithUlid = null)
        {
            LootLockerPresenceManager.DisconnectPresence(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Force stop all Presence WebSocket connections manually.
        /// This will override the automatic presence management and disconnect all active connections.
        /// Use this when you need to immediately disconnect all presence connections.
        /// </summary>
        public static void ForceStopAllPresenceConnections()
        {
            LootLockerPresenceManager.DisconnectAll();
        }

        /// <summary>
        /// Get a list of player ULIDs that currently have active Presence connections
        /// </summary>
        /// <returns>Collection of player ULIDs that have active presence connections</returns>
        public static IEnumerable<string> ListPresenceConnections()
        {
            return LootLockerPresenceManager.ActiveClientUlids;
        }

        /// <summary>
        /// Update the player's presence status
        /// </summary>
        /// <param name="status">The status to set (e.g., "online", "in_game", "away")</param>
        /// <param name="metadata">Optional metadata to include with the status</param>
        /// <param name="onComplete">Callback for the result of the operation</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdatePresenceStatus(string status, Dictionary<string, string> metadata = null, Action<bool> onComplete = null, string forPlayerWithUlid = null)
        {
            LootLockerPresenceManager.UpdatePresenceStatus(status, metadata, forPlayerWithUlid, (success, error) => {
                onComplete?.Invoke(success);
            });
        }

        /// <summary>
        /// Get the current Presence connection state for a specific player
        /// </summary>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <returns>The current connection state</returns>
        public static LootLockerPresenceConnectionState GetPresenceConnectionState(string forPlayerWithUlid = null)
        {
            return LootLockerPresenceManager.GetPresenceConnectionState(forPlayerWithUlid);
        }

        /// <summary>
        /// Check if Presence is connected and authenticated for a specific player
        /// </summary>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <returns>True if connected and active, false otherwise</returns>
        public static bool IsPresenceConnected(string forPlayerWithUlid = null)
        {
            return LootLockerPresenceManager.IsPresenceConnected(forPlayerWithUlid);
        }

        /// <summary>
        /// Get statistics about the Presence connection for a specific player
        /// </summary>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <returns>Connection statistics</returns>
        public static LootLockerPresenceConnectionStats GetPresenceConnectionStats(string forPlayerWithUlid)
        {
            return LootLockerPresenceManager.GetPresenceConnectionStats(forPlayerWithUlid);
        }

        /// <summary>
        /// Get the last status that was sent for a specific player
        /// </summary>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <returns>The last sent status string, or null if no client is found or no status has been sent</returns>
        public static string GetCurrentPresenceStatus(string forPlayerWithUlid = null)
        {
            return LootLockerPresenceManager.GetLastSentStatus(forPlayerWithUlid);
        }

        /// <summary>
        /// Enable or disable the entire Presence system
        /// </summary>
        /// <param name="enabled">Whether to enable presence</param>
        public static void SetPresenceEnabled(bool enabled)
        {
            if(LootLockerPresenceManager.IsEnabled && !enabled)
            {
                LootLockerPresenceManager.DisconnectAll();
            }
            LootLockerPresenceManager.IsEnabled = enabled;
        }

        /// <summary>
        /// Check if presence system is currently enabled
        /// </summary>
        /// <returns>True if enabled, false otherwise</returns>
        public static bool IsPresenceEnabled()
        {
            return LootLockerPresenceManager.IsEnabled;
        }

        /// <summary>
        /// Enable or disable automatic presence connection when sessions start
        /// </summary>
        /// <param name="enabled">Whether to auto-connect presence</param>
        public static void SetPresenceAutoConnectEnabled(bool enabled)
        {
            LootLockerPresenceManager.AutoConnectEnabled = enabled;
        }

        /// <summary>
        /// Check if automatic presence connections are enabled
        /// </summary>
        /// <returns>True if auto-connect is enabled, false otherwise</returns>
        public static bool IsPresenceAutoConnectEnabled()
        {
            return LootLockerPresenceManager.AutoConnectEnabled;
        }

        /// <summary>
        /// Enable or disable automatic presence disconnection when the application loses focus or is paused.
        /// When enabled, presence connections will automatically disconnect when the app goes to background
        /// and reconnect when it returns to foreground. Useful for saving battery on mobile or managing resources.
        /// </summary>
        /// <param name="enabled">True to enable auto-disconnect on focus change, false to disable</param>
        public static void SetPresenceAutoDisconnectOnFocusChangeEnabled(bool enabled)
        {
            LootLockerPresenceManager.AutoDisconnectOnFocusChange = enabled;
        }

        /// <summary>
        /// Check if automatic presence disconnection on focus change is enabled
        /// </summary>
        /// <returns>True if auto-disconnect on focus change is enabled, false otherwise</returns>
        public static bool IsPresenceAutoDisconnectOnFocusChangeEnabled()
        {
            return LootLockerPresenceManager.AutoDisconnectOnFocusChange;
        }
#endif

        #endregion

        #region Connected Accounts
        /// <summary>
        /// List identity providers (like Apple, Google, etc.) that are connected to the currently logged in account
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListConnectedAccounts(Action<LootLockerListConnectedAccountsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListConnectedAccountsResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.listConnectedAccounts.endPoint, LootLockerEndPoints.listConnectedAccounts.httpMethod, null, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Disconnect account from the currently logged in account
        ///
        /// Use this to disconnect an account (like a Google or Apple account) that can be used to start sessions for this LootLocker account so that it is no longer allowed to do that
        /// IMPORTANT: If you are using multiple users, be very sure to pass in the correct `forPlayerWithUlid` parameter as that will be the account that the provider is disconnected from
        /// </summary>
        /// <param name="accountToDisconnect">What account to disconnect from this LootLocker Account</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DisconnectAccount(LootLockerAccountProvider accountToDisconnect, Action<LootLockerAccountConnectedResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAccountConnectedResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.disconnectAccount.WithPathParameter(accountToDisconnect);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.disconnectAccount.httpMethod, null, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Connect a Google Account to the currently logged in LootLocker account allowing that google account to start sessions for this player
        /// IMPORTANT: If you are using multiple users, be very sure to pass in the correct `forPlayerWithUlid` parameter as that will be the account that the Google account is linked into
        /// </summary>
        /// <param name="idToken">The Id Token from google sign in</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ConnectGoogleAccount(string idToken, Action<LootLockerAccountConnectedResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAccountConnectedResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.connectProviderToAccount.WithPathParameter("google");

            string data = LootLockerJson.SerializeObject(new LootLockerConnectGoogleProviderToAccountRequest{id_token = idToken });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.connectProviderToAccount.httpMethod, data, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Connect a Google Account (with a Google Platform specified) to the currently logged in LootLocker account allowing that google account to start sessions for this player
        /// IMPORTANT: If you are using multiple users, be very sure to pass in the correct `forPlayerWithUlid` parameter as that will be the account that the Google account is linked into
        /// </summary>
        /// <param name="idToken">The Id Token from google sign in</param>
        /// <param name="platform">Google OAuth2 ClientID platform</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ConnectGoogleAccount(string idToken, GoogleAccountProviderPlatform platform, Action<LootLockerAccountConnectedResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAccountConnectedResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.connectProviderToAccount.WithPathParameter("google");

            string data = LootLockerJson.SerializeObject(new LootLockerConnectGoogleProviderToAccountWithPlatformRequest() { id_token = idToken, platform = platform });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.connectProviderToAccount.httpMethod, data, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Connect an Apple Account (authorized by Rest Sign In) to the currently logged in LootLocker account allowing that google account to start sessions for this player
        /// IMPORTANT: If you are using multiple users, be very sure to pass in the correct `forPlayerWithUlid` parameter as that will be the account that the Apple account is linked into
        /// </summary>
        /// <param name="authorizationCode">Authorization code, provided by apple during Sign In</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ConnectAppleAccountByRestSignIn(string authorizationCode, Action<LootLockerAccountConnectedResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAccountConnectedResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.connectProviderToAccount.WithPathParameter("apple-rest");

            string data = LootLockerJson.SerializeObject(new LootLockerConnectAppleRestProviderToAccountRequest() { authorization_code = authorizationCode });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.connectProviderToAccount.httpMethod, data, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Connect a Twitch Account to the currently logged in LootLocker account allowing that Twitch account to start sessions for this player
        /// IMPORTANT: If you are using multiple users, be very sure to pass in the correct `forPlayerWithUlid` parameter as that will be the account that the Twitch account is linked into
        /// </summary>
        /// <param name="authorizationCode">The Authorization Code from Twitch sign in</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ConnectTwitchAccount(string authorizationCode, Action<LootLockerAccountConnectedResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAccountConnectedResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.connectProviderToAccount.WithPathParameter("twitch");

            string data = LootLockerJson.SerializeObject(new LootLockerConnectTwitchProviderToAccountRequest() { authorization_code = authorizationCode });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.connectProviderToAccount.httpMethod, data, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Connect an Epic Account to the currently logged in LootLocker account allowing that Epic account to start sessions for this player
        /// IMPORTANT: If you are using multiple users, be very sure to pass in the correct `forPlayerWithUlid` parameter as that will be the account that the Epic account is linked into
        /// </summary>
        /// <param name="token">The Token from Epic sign in</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ConnectEpicAccount(string Token, Action<LootLockerAccountConnectedResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAccountConnectedResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.connectProviderToAccount.WithPathParameter("epic");

            string data = LootLockerJson.SerializeObject(new LootLockerConnectEpicProviderToAccountRequest() { token = Token });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.connectProviderToAccount.httpMethod, data, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Connect a Playstation Account to the currently logged in LootLocker account allowing that Playstation account to start sessions for this player
        /// IMPORTANT: If you are using multiple users, be very sure to pass in the correct `forPlayerWithUlid` parameter as that will be the account that the Playstation account is linked into
        /// </summary>
        /// <param name="environment">The environment for the playstation account (dev, qa, prod)</param>
        /// <param name="code">The code from playstation sign in</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ConnectPlaystationAccount(string environment, string code, Action<LootLockerAccountConnectedResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAccountConnectedResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.connectProviderToAccount.WithPathParameter("playstation");

            string data = LootLockerJson.SerializeObject(new LootLockerConnectPlaystationProviderToAccountRequest() { environment = environment, code = code });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.connectProviderToAccount.httpMethod, data, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Connect an Discord Account to the currently logged in LootLocker account allowing that Discord account to start sessions for this player
        /// IMPORTANT: If you are using multiple users, be very sure to pass in the correct `forPlayerWithUlid` parameter as that will be the account that the Discord account is linked into
        /// </summary>
        /// <param name="token">The Token from Discord sign in</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ConnectDiscordAccount(string token, Action<LootLockerAccountConnectedResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAccountConnectedResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.connectProviderToAccount.WithPathParameter("discord");

            string data = LootLockerJson.SerializeObject(new LootLockerConnectDiscordProviderToAccountRequest() { token = token });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.connectProviderToAccount.httpMethod, data, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Connect an identity provider (authorized using a remote link session) to the currently logged in LootLocker account allowing that authentication method to start sessions for this player
        /// IMPORTANT: If you are using multiple users, be very sure to pass in the correct `forPlayerWithUlid` parameter as that will be the account that the Remote Session account is linked into
        /// </summary>
        /// <param name="Code">The lease code returned with the response when starting a lease process. Note that the process must have concluded successfully first.</param>
        /// <param name="Nonce">The nonce returned with the response when starting a lease process. Note that the process must have concluded successfully first.</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ConnectRemoteSessionAccount(string Code, string Nonce, Action<LootLockerAccountConnectedResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAccountConnectedResponse>(forPlayerWithUlid));
                return;
            }
            
            string data = LootLockerJson.SerializeObject(new LootLockerConnectRemoteSessionToAccountRequest() { Code = Code, Nonce = Nonce });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.attachRemoteSessionToAccount.endPoint, LootLockerEndPoints.attachRemoteSessionToAccount.httpMethod, data, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// This endpoint lets you transfer identity providers between two players, provided you have a valid session for both.
        /// The designated identity providers will be transferred FROM the player designated by the `FromPlayerWithUlid` parameter and TO the player designated by the `ToPlayerWithUlid` parameter.
        /// If any of the providers can not be transferred the whole operation will fail and NO identity providers will be transferred.
        /// IMPORTANT: This is a destructive action.Once an identity provider has been transferred they will allow authentication for the target player and no longer authenticate for the source player.
        /// This can leave the source player without means of authentication and thus unusable from the game.
   	    /// 
        /// ** Limitations**
        /// - You can not move an identity provider that the source player does not have
        /// - You can not move an identity provider to a player that already has an account from said identity provider associated with their account.
        /// - You can not move an identity provider which isn't active in your game settings.
        /// </summary>
        /// <param name="FromPlayerWithUlid">The ULID of an authenticated player that you wish to move identity providers FROM</param>
        /// <param name="ToPlayerWithUlid">The ULID of an authenticated player that you wish to move identity providers TO</param>
        /// <param name="ProvidersToTransfer">Which identity providers you wish to transfer</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void TransferIdentityProvidersBetweenAccounts(string FromPlayerWithUlid, string ToPlayerWithUlid, List<LootLockerAccountProvider> ProvidersToTransfer, Action<LootLockerListConnectedAccountsResponse> onComplete)
        {
            if (!CheckInitialized(false, FromPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListConnectedAccountsResponse>(FromPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(FromPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerListConnectedAccountsResponse>("No ulid provided for source player", FromPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(ToPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerListConnectedAccountsResponse>("No ulid provided for target player", ToPlayerWithUlid));
                return;
            }

            var fromPlayer = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(FromPlayerWithUlid);
            if (string.IsNullOrEmpty(fromPlayer?.SessionToken))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerListConnectedAccountsResponse>("No valid session token found for source player", FromPlayerWithUlid));
                return;
            }

            var toPlayer = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(ToPlayerWithUlid);
            if (string.IsNullOrEmpty(toPlayer?.SessionToken))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerListConnectedAccountsResponse>("No valid session token found for target player", ToPlayerWithUlid));
                return;
            }

            if (ProvidersToTransfer.Count == 0)
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerListConnectedAccountsResponse>("No providers submitted", FromPlayerWithUlid));
                return;
            }

            string data = LootLockerJson.SerializeObject(new LootLockerTransferProvidersBetweenAccountsRequest() { Source_token = fromPlayer.SessionToken, Target_token = toPlayer.SessionToken, Identity_providers = ProvidersToTransfer.ToArray() });

            LootLockerServerRequest.CallAPI(FromPlayerWithUlid, LootLockerEndPoints.transferProvidersBetweenAccountsEndpoint.endPoint, LootLockerEndPoints.transferProvidersBetweenAccountsEndpoint.httpMethod, data, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
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
        /// <param name="timeOutAfterMinutes">Optional: How long to allow the process to take in its entirety</param>
        public static Guid StartRemoteSession(
            Action<LootLockerLeaseRemoteSessionResponse> remoteSessionLeaseInformation,
            Action<LootLockerRemoteSessionStatusPollingResponse> remoteSessionLeaseStatusUpdate,
            Action<LootLockerStartRemoteSessionResponse> onComplete,
            float pollingIntervalSeconds = 1.0f,
            float timeOutAfterMinutes = 5.0f)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStartRemoteSessionResponse>(null));
                return Guid.Empty;
            }

            return LootLockerAPIManager.RemoteSessionPoller.StartRemoteSessionWithContinualPolling(
                LootLockerRemoteSessionLeaseIntent.login,
                remoteSessionLeaseInformation,
                remoteSessionLeaseStatusUpdate,
                onComplete,
                pollingIntervalSeconds,
                timeOutAfterMinutes
            );
        }

        /// <summary>
        /// Start a remote session for linking
        /// If you want to let your local user sign in using another device then you use this method. First you will get the lease information needed to allow a secondary device to authenticate.
        /// While the process is ongoing, the remoteSessionLeaseStatusUpdate action (if one is provided) will be invoked intermittently (about once a second) to update you on the status of the process.
        /// When the process has come to an end (whether successfully or not), the onComplete action will be invoked with the updated information.
        /// </summary>
        /// <param name="forPlayerWithUlid">Execute the request for the specified player (the player that you intend to link the remote account into)</param>
        /// <param name="remoteSessionLeaseInformation">Will be invoked once to provide the lease information that the secondary device can use to authenticate</param>
        /// <param name="remoteSessionLeaseStatusUpdate">Will be invoked intermittently to update the status lease process</param>
        /// <param name="onComplete">Invoked when the remote session process has run to completion containing either a valid session or information on why the process failed</param>
        /// <param name="pollingIntervalSeconds">Optional: How often to poll the status of the remote session process</param>
        /// <param name="timeOutAfterMinutes">Optional: How long to allow the process to take in its entirety</param>
        public static Guid StartRemoteSessionForLinking(
            string forPlayerWithUlid,
            Action<LootLockerLeaseRemoteSessionResponse> remoteSessionLeaseInformation,
            Action<LootLockerRemoteSessionStatusPollingResponse> remoteSessionLeaseStatusUpdate,
            Action<LootLockerStartRemoteSessionResponse> onComplete,
            float pollingIntervalSeconds = 1.0f,
            float timeOutAfterMinutes = 5.0f)
        {
            if (!CheckInitialized())
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStartRemoteSessionResponse>(forPlayerWithUlid));
                return Guid.Empty;
            }

            return LootLockerAPIManager.RemoteSessionPoller.StartRemoteSessionWithContinualPolling(
                LootLockerRemoteSessionLeaseIntent.link,
                remoteSessionLeaseInformation,
                remoteSessionLeaseStatusUpdate,
                onComplete,
                pollingIntervalSeconds,
                timeOutAfterMinutes,
                forPlayerWithUlid
            );
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RefreshRemoteSession(Action<LootLockerRefreshRemoteSessionResponse> onComplete, string forPlayerWithUlid = null)
        {
            RefreshRemoteSession(null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Refresh a previous session signed in remotely.
        /// If you do not want to manually handle the refresh token we recommend using the RefreshRemoteSession(Action<LootLockerRemoteSessionResponse> onComplete, string forPlayerWithUlid) method.
        /// A response code of 400 (Bad request) could mean that the refresh token has expired and you'll need to sign in again
        /// </summary>
        /// <param name="refreshToken">Token received in response from StartRemoteSession request</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RefreshRemoteSession(string refreshToken, Action<LootLockerRefreshRemoteSessionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerRefreshRemoteSessionResponse>(null));
                return;
            }

            if (string.IsNullOrEmpty(refreshToken))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
                if (string.IsNullOrEmpty(playerData?.RefreshToken))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerRefreshRemoteSessionResponse>(playerData?.ULID));
                    return;
                }

                refreshToken = playerData.RefreshToken;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.startRemoteSession.endPoint, LootLockerEndPoints.startRemoteSession.httpMethod, 
                LootLockerJson.SerializeObject(new LootLockerRefreshRemoteSessionRequest(refreshToken)),
                (LootLockerResponse serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerRefreshRemoteSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        LootLockerEventSystem.TriggerSessionRefreshed(new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Remote),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                        });
                    }

                    onComplete?.Invoke(response);
                },
                false);
        }
        #endregion

        #region White Label

        private static Dictionary<string /*email*/, string /*token*/> _wllProcessesDictionary = new Dictionary<string, string>();

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
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerWhiteLabelLoginResponse>(null));
                return;
            }

            if (_wllProcessesDictionary.ContainsKey(email))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerWhiteLabelLoginResponse>($"White Label login already in progress for email {email}", null));

                return;
            }

            _wllProcessesDictionary.Add(email, null);

            LootLockerWhiteLabelUserRequest input = new LootLockerWhiteLabelUserRequest
            {
                email = email,
                password = password,
                remember = remember
            };

            LootLockerAPIManager.WhiteLabelLogin(input, response =>
            {
                if (response.success)
                {
                    _wllProcessesDictionary[input.email] = response.SessionToken;
                }
                else
                {
                    _wllProcessesDictionary.Remove(input.email);
                }

                onComplete?.Invoke(response);
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
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerWhiteLabelSignupResponse>(null));
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
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(null));
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
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(null));
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
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(null));
                return;
            }

            LootLockerAPIManager.WhiteLabelRequestAccountVerification(email, onComplete);
        }

        /// <summary>
        /// Checks for a stored session and if that session is valid.
        /// Depending on response of this method the developer can either start a session using the token, or show a login form.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action bool that returns true if a White Label session exists </param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CheckWhiteLabelSession(Action<bool> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(false);
                return;
            }

            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            string existingSessionEmail = playerData?.WhiteLabelEmail;
            string existingSessionToken = playerData?.WhiteLabelToken;
            if (string.IsNullOrEmpty(existingSessionToken) || string.IsNullOrEmpty(existingSessionEmail))
            {
                onComplete?.Invoke(false);
                return;
            }

            VerifyWhiteLabelSession(existingSessionEmail, existingSessionToken, onComplete);
        }

        /// <summary>
        /// Checks for a stored session and if that session is valid.
        /// Depending on response of this method the developer can either start a session using the token, or show a login form.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="email">The email to check for a valid white label session</param>
        /// <param name="onComplete">onComplete Action bool that returns true if a White Label session exists </param>
        public static void CheckWhiteLabelSession(string email, Action<bool> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(false);
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                onComplete?.Invoke(false);
                return;
            }

            string playerUlid = LootLockerStateData.GetPlayerUlidFromWLEmail(email);
            string token = null;
            if (!string.IsNullOrEmpty(playerUlid))
            {
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);
                token = playerData?.WhiteLabelToken;
            }
            else
            {
                _wllProcessesDictionary.TryGetValue(email, out token);
            }

            string existingSessionEmail = email;
            string existingSessionToken = token;
            if (string.IsNullOrEmpty(existingSessionToken))
            {
                onComplete?.Invoke(false);
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
                onComplete?.Invoke(false);
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
                onComplete?.Invoke(response.success);
            });
        }

        /// <summary>
        /// Start a LootLocker Session using the cached White Label token and email if any exist
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <param name="Optionals">Optional parameters for the session start request</param>
        public static void StartWhiteLabelSession(Action<LootLockerSessionResponse> onComplete, string forPlayerWithUlid = null, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            string email = null;
            string token = null;
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            if (playerData == null || string.IsNullOrEmpty(playerData.WhiteLabelEmail))
            {
                if (_wllProcessesDictionary.Count == 0)
                {
                    onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerSessionResponse>("No cached white label data found, please start a session explicitly using WhiteLabelLoginAndStartSession", forPlayerWithUlid));
                    return;
                }
                var pair = _wllProcessesDictionary.ToList()[0];
                email = pair.Key;
                token = pair.Value;
            }
            else
            {
                email = playerData.WhiteLabelEmail;
                token = playerData.WhiteLabelToken;
            }

            if (string.IsNullOrEmpty(token))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerSessionResponse>($"No valid white label token found for {email}", forPlayerWithUlid));
                return;
            }

            if (Optionals == null)
            {
                Optionals = playerData?.SessionOptionals;
            }

            StartWhiteLabelSession(new LootLockerWhiteLabelSessionRequest() { email = email, token = token, optionals = Optionals }, onComplete);
        }

        /// <summary>
        /// Start a LootLocker Session using the cached White Label token for the specified email if it exist
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="email">The email of the White Label user to start a WL session for</param>
        /// <param name="Optionals">Optional parameters for the session start request</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartWhiteLabelSession(string email, Action<LootLockerSessionResponse> onComplete, LootLockerSessionOptionals Optionals = null)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                StartWhiteLabelSession(onComplete);
                return;
            }

            string token = null;
            if (!_wllProcessesDictionary.ContainsKey(email))
            {
                string playerUlidInStateData = LootLockerStateData.GetPlayerUlidFromWLEmail(email);
                if (string.IsNullOrEmpty(playerUlidInStateData))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerSessionResponse>($"No White Label data stored for {email}", null));
                    return;
                }
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlidInStateData);

                token = playerData.WhiteLabelToken;
                if(Optionals == null)
                {
                    Optionals = playerData?.SessionOptionals;
                }
            }
            else
            {
                token = _wllProcessesDictionary[email];
            }


            if (string.IsNullOrEmpty(token))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerSessionResponse>($"No White Label token stored for {email}", null));
                return;
            }

            StartWhiteLabelSession(new LootLockerWhiteLabelSessionRequest() { email = email, token = token, optionals = Optionals }, onComplete);
        }

        /// <summary>
        /// Start a LootLocker Session using the provided White Label request.
        /// White Label platform must be enabled in the web console for this to work.
        /// </summary>
        /// <param name="sessionRequest">A White Label Session Request with inner values already set</param>
        /// <param name="Optionals">Optional parameters for the session start request</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSessionResponse</param>
        public static void StartWhiteLabelSession(LootLockerWhiteLabelSessionRequest sessionRequest, Action<LootLockerSessionResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSessionResponse>(null));
                return;
            }

            LootLockerServerRequest.CallAPI(null, 
                LootLockerEndPoints.whiteLabelLoginSessionRequest.endPoint, LootLockerEndPoints.whiteLabelLoginSessionRequest.httpMethod, 
                LootLockerJson.SerializeObject(sessionRequest), 
                (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                    if (response.success)
                    {
                        LootLockerEventSystem.TriggerSessionStarted(new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = "",
                            ULID = response.player_ulid,
                            Identifier = "",
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = sessionRequest.email,
                            WhiteLabelToken = sessionRequest.token,
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.WhiteLabel),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                            SessionOptionals = sessionRequest.optionals
                        });
                        _wllProcessesDictionary.Remove(sessionRequest.email);
                    }

                    onComplete?.Invoke(response);
                }, 
                false);
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
                StartWhiteLabelSession(email, sessionResponse =>
                {
                    onComplete?.Invoke(LootLockerWhiteLabelLoginAndStartSessionResponse.MakeWhiteLabelLoginAndStartSessionResponse(loginResponse, sessionResponse));
                });
            });
        }

        #endregion

        #region Player
        /// <summary>
        /// Get information about the currently logged in player such as name and different ids to use for subsequent calls to LootLocker methods
        /// </summary>
        /// <param name="onComplete">Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCurrentPlayerInfo(Action<LootLockerGetCurrentPlayerInfoResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCurrentPlayerInfoResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.getInfoFromSession;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// List information for one or more other players
        /// </summary>
        /// <param name="playerIdsToLookUp">A list of ULID ids of players to look up. These ids are in the form of ULIDs and are sometimes called player_ulid or similar</param>
        /// <param name="playerLegacyIdsToLookUp">A list of legacy ids of players to look up. These ids are in the form of integers and are sometimes called simply player_id or id</param>
        /// <param name="playerPublicUidsToLookUp">A list of public uids to look up. These ids are in the form of UIDs.</param>
        /// <param name="onComplete">Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListPlayerInfo(string[] playerIdsToLookUp, int[] playerLegacyIdsToLookUp, string[] playerPublicUidsToLookUp, Action<LootLockerListPlayerInfoResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListPlayerInfoResponse>(forPlayerWithUlid));
                return;
            }
            if(playerIdsToLookUp.Length == 0 && playerLegacyIdsToLookUp.Length == 0 && playerPublicUidsToLookUp.Length == 0)
            {
                // Nothing to do, early out
                onComplete?.Invoke(LootLockerResponseFactory.EmptySuccess<LootLockerListPlayerInfoResponse>());
                return;
            }

            var endPoint = LootLockerEndPoints.listPlayerInfo;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint,
                endPoint.httpMethod,
                LootLockerJson.SerializeObject(new LootLockerListPlayerInfoRequest {
                    player_id = playerIdsToLookUp,
                    player_legacy_id = playerLegacyIdsToLookUp,
                    player_public_uid = playerPublicUidsToLookUp
                }),
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get the players inventory.
        /// </summary>
        /// <param name="count">Amount of assets to retrieve</param>
        /// <param name="after">The instance ID the list should start from</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInventoryResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetInventory(int count, int after, Action<LootLockerInventoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInventoryResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getInventory.endPoint;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("count", count);
            if (after > 0)
                queryParams.Add("after", after);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get the players inventory.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInventoryResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetInventory(Action<LootLockerInventoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInventoryResponse>(forPlayerWithUlid));
                return;
            }
            GetInventory(-1, -1, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get the players inventory.
        /// </summary>
        /// <param name="count">Amount of assets to retrieve</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInventoryResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetInventory(int count, Action<LootLockerInventoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInventoryResponse>(forPlayerWithUlid));
                return;
            }


            GetInventory(count, -1, onComplete, forPlayerWithUlid);

        }

        /// <summary>
        /// List player inventory with default parameters (no filters, first page, default page size).
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListPlayerInventoryWithDefaultParameters(Action<LootLockerSimpleInventoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListPlayerInventory(new LootLockerListSimplifiedInventoryRequest(), 100, 1, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List player inventory with minimal response data. Due to looking up less data, this endpoint is significantly faster than GetInventory.
        /// </summary>
        /// <param name="request">Request object containing any filters to apply to the inventory listing.</param>
        /// <param name="perPage">Optional : Number of items per page.</param>
        /// <param name="page">Optional : Page number to retrieve.</param>
        public static void ListPlayerInventory(LootLockerListSimplifiedInventoryRequest request, int perPage, int page, Action<LootLockerSimpleInventoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSimpleInventoryResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.listSimplifiedInventory;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            queryParams.Add("per_page", perPage > 0 ? perPage : 100);
            queryParams.Add("page", page > 0 ? page : 1);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint + queryParams.Build(), endPoint.httpMethod, LootLockerJson.SerializeObject(request), onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// List character inventory with default parameters (no filters, first page, default page size).
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListCharacterInventoryWithDefaultParameters(Action<LootLockerSimpleInventoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListCharacterInventory(new LootLockerListSimplifiedInventoryRequest(), 0, 100, 1, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List character inventory with minimal response data. Due to looking up less data, this endpoint is significantly faster than GetInventory.
        /// </summary>
        /// <param name="request">Request object containing any filters to apply to the inventory listing.</param>
        /// <param name="characterId">Optional : Filter inventory by character ID.</param>
        /// <param name="perPage">Optional : Number of items per page.</param>
        /// <param name="page">Optional : Page number to retrieve.</param>
        public static void ListCharacterInventory(LootLockerListSimplifiedInventoryRequest request, int characterId, int perPage, int page, Action<LootLockerSimpleInventoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSimpleInventoryResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.listSimplifiedInventory;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (characterId > 0)
                queryParams.Add("character_id", characterId);
            queryParams.Add("per_page", perPage > 0 ? perPage : 100);
            queryParams.Add("page", page > 0 ? page : 1);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint + queryParams.Build(), endPoint.httpMethod, LootLockerJson.SerializeObject(request), onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get the amount of credits/currency that the player has.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerBalanceResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetBalance(Action<LootLockerBalanceResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerBalanceResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.getCurrencyBalance;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get assets that have been given to the currently logged in player since the last time this endpoint was called.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerAssetNotificationsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetNotification(Action<LootLockerPlayerAssetNotificationsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerAssetNotificationsResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.playerAssetNotifications;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get asset deactivations for the currently logged in player since the last time this endpoint was called.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerDeactivatedAssetsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetDeactivatedAssetNotification(Action<LootLockerDeactivatedAssetsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDeactivatedAssetsResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.playerAssetDeactivationNotification;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Initiate DLC Migration for the player: https://docs.lootlocker.com/background/live-ops-tools#dlc-migration
        /// 5 minutes after calling this endpoint you should issue a call to the Player Asset Notifications call to get the results of the migrated DLC, if any. If you only want the ID's of the assets you can also use  GetDLCMigrated(Action<LootLockerDlcResponse> onComplete).
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerDlcResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void InitiateDLCMigration(Action<LootLockerDlcResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDlcResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.initiateDlcMigration;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get a list of DLC's migrated for the player. This response will only list the asset-ID's of the migrated DLC, if you want more information about the assets, use GetAssetNotification(Action<LootLockerPlayerAssetNotificationsResponse> onComplete) instead.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerDlcResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetDLCMigrated(Action<LootLockerDlcResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDlcResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.getDlcMigration;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Set the players profile to be private. This means that their inventory will not be displayed publicly on Steam and other places.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerStandardResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SetProfilePrivate(Action<LootLockerStandardResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStandardResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.setProfilePrivate;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Set the players profile to public. This means that their inventory will be displayed publicly on Steam and other places.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerStandardResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SetProfilePublic(Action<LootLockerStandardResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStandardResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.setProfilePublic;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get the logged in players name.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetPlayerName(Action<PlayerNameResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameResponse>(forPlayerWithUlid));
                return;
            }
            var endPoint = LootLockerEndPoints.getPlayerName;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Set the logged in players name. Max length of a name is 255 characters.
        /// </summary>
        /// <param name="name">The name to set to the currently logged in player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SetPlayerName(string name, Action<PlayerNameResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameResponse>(forPlayerWithUlid));
                return;
            }

            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);

            if (playerData != null && playerData.CurrentPlatform.Platform == LL_AuthPlatforms.Guest)
            {
                if (name.ToLower().Contains("player"))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.ClientError<PlayerNameResponse>("Setting the Player name to 'Player' is not allowed", forPlayerWithUlid));
                    return;

                }
                else if (name.ToLower().Contains(playerData.Identifier.ToLower()))
                {
                    onComplete?.Invoke(LootLockerResponseFactory.ClientError<PlayerNameResponse>("Setting the Player name to the Identifier is not allowed", forPlayerWithUlid));
                    return;
                }
            }

            PlayerNameRequest data = new PlayerNameRequest();
            data.name = name;
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<PlayerNameResponse>(forPlayerWithUlid));
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            var endPoint = LootLockerEndPoints.setPlayerName;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get players 1st party platform ID's from the provided list of playerID's.
        /// </summary>
        /// <param name="playerIds">A list of multiple player ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type Player1stPartyPlatformIDsLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayer1stPartyPlatformIds(ulong[] playerIds, Action<Player1stPartyPlatformIDsLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<Player1stPartyPlatformIDsLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayer1stPartyPlatformIDs(forPlayerWithUlid, new LookupPlayer1stPartyPlatformIDsRequest()
            {
                player_ids = playerIds
            }, onComplete);
        }

        /// <summary>
        /// Get players 1st party platform ID's from the provided list of playerID's.
        /// </summary>
        /// <param name="playerPublicUIds">A list of multiple player public UID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type Player1stPartyPlatformIDsLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayer1stPartyPlatformIds(string[] playerPublicUIds, Action<Player1stPartyPlatformIDsLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<Player1stPartyPlatformIDsLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayer1stPartyPlatformIDs(forPlayerWithUlid, new LookupPlayer1stPartyPlatformIDsRequest()
            {
                player_public_uids = playerPublicUIds
            }, onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by playerID's.
        /// </summary>
        /// <param name="playerIds">A list of multiple player ID's<</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByPlayerIds(ulong[] playerIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            List<string> stringPlayerIds = new List<string>();
            foreach (ulong id in playerIds)
            {
                stringPlayerIds.Add(id.ToString());
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "player_public_uid", stringPlayerIds.ToArray(), onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by public playerID's.
        /// </summary>
        /// <param name="playerPublicUIds">A list of multiple player public UID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByPlayerPublicUIds(string[] playerPublicUIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "player_public_uid", playerPublicUIds, onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by public playerID's.
        /// </summary>
        /// <param name="playerUlids">A list of player ulids</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByPlayerUlids(string[] playerUlids, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "player_ulid", playerUlids, onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by public playerID's.
        /// </summary>
        /// <param name="guestLoginIds">A list of guest login ids</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByGuestLoginIds(string[] guestLoginIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "player_guest_login_id", guestLoginIds, onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by public playerID's.
        /// </summary>
        /// <param name="playerNames">A list of player names</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByPlayerNames(string[] playerNames, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "player_name", playerNames, onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by steam ID's
        /// </summary>
        /// <param name="steamIds">A list of multiple player Steam ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesBySteamIds(ulong[] steamIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            List<string> stringSteamIds = new List<string>();
            foreach (ulong id in steamIds)
            {
                stringSteamIds.Add(id.ToString());
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "steam_id", stringSteamIds.ToArray(), onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by Steam ID's
        /// </summary>
        /// <param name="steamIds">A list of multiple player Steam ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesBySteamIds(string[] steamIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "steam_id", steamIds, onComplete);
        }

        /// <summary>
        ///Get player names and important ids of a set of players from their last active platform by PSN ID's
        /// </summary>
        /// <param name="psnIds">A list of multiple player PSN ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByPSNIds(ulong[] psnIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            List<string> stringPsnIds = new List<string>();
            foreach (ulong id in psnIds)
            {
                stringPsnIds.Add(id.ToString());
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "psn_id", stringPsnIds.ToArray(), onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by PSN ID's
        /// </summary>
        /// <param name="psnIds">A list of multiple player PSN ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByPSNIds(string[] psnIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "psn_id", psnIds, onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by Xbox ID's
        /// </summary>
        /// <param name="xboxIds">A list of multiple player XBOX ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByXboxIds(string[] xboxIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "xbox_id", xboxIds, onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by Epic Games ID's
        /// </summary>
        /// <param name="epicGamesIds">A list of multiple player Epic Games ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByEpicGamesIds(string[] epicGamesIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "epic_games_id", epicGamesIds, onComplete);
        }

        /// <summary>
        /// Get player names and important ids of a set of players from their last active platform by Google Play Games ID's
        /// </summary>
        /// <param name="googlePlayGamesIds">A list of multiple player Google Play Games ID's</param>
        /// <param name="onComplete">onComplete Action for handling the response of type PlayerNameLookupResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LookupPlayerNamesByGooglePlayGamesIds(string[] googlePlayGamesIds, Action<PlayerNameLookupResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<PlayerNameLookupResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.LookupPlayerNames(forPlayerWithUlid, "google_play_games_id", googlePlayGamesIds, onComplete);
        }

        /// <summary>
        /// Mark the logged in player for deletion. After 30 days the player will be deleted from the system.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse></param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeletePlayer(Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.deletePlayer.endPoint, LootLockerEndPoints.deletePlayer.httpMethod, null, onComplete: (serverResponse) =>
            {
                if (serverResponse != null && serverResponse.success)
                {
                    ClearLocalSession(serverResponse.requestContext.player_ulid);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetPlayerFile(int fileId, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getSingleplayerFile.WithPathParameter(fileId);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns all the files that your currently active player own.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFilesResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllPlayerFiles(Action<LootLockerPlayerFilesResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFilesResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.getPlayerFiles.endPoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns all public files that the player with the provided playerID owns.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFilesResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllPlayerFiles(int playerId, Action<LootLockerPlayerFilesResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFilesResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getPlayerFilesByPlayerId.WithPathParameter(playerId);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Upload a file with the provided name and content. The file will be owned by the currently active player.
        /// </summary>
        /// <param name="pathToFile">Path to the file, example: Application.persistentDataPath + "/" + fileName;</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="isPublic">Should this file be viewable by other players?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UploadPlayerFile(string pathToFile, string filePurpose, bool isPublic, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>(forPlayerWithUlid));
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
                LootLockerLogger.Log($"File error: {e.Message}", LootLockerLogger.LogLevel.Error);
                return;
            }

            LootLockerServerRequest.UploadFile(forPlayerWithUlid, LootLockerEndPoints.uploadPlayerFile, fileBytes, Path.GetFileName(pathToFile), "multipart/form-data", body,
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UploadPlayerFile(string pathToFile, string filePurpose, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            UploadPlayerFile(pathToFile, filePurpose, false, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Upload a file using a Filestream. The file will be owned by the currently active player.
        /// </summary>
        /// <param name="fileStream">Filestream to upload</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="isPublic">Should this file be viewable by other players?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UploadPlayerFile(FileStream fileStream, string filePurpose, bool isPublic, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>(forPlayerWithUlid));
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
                LootLockerLogger.Log($"File error: {e.Message}", LootLockerLogger.LogLevel.Error);
                return;
            }

            LootLockerServerRequest.UploadFile(forPlayerWithUlid, LootLockerEndPoints.uploadPlayerFile, fileBytes, Path.GetFileName(fileStream.Name), "multipart/form-data", body,
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UploadPlayerFile(FileStream fileStream, string filePurpose, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            UploadPlayerFile(fileStream, filePurpose, false, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Upload a file using a byte array. Can be useful if you want to upload without storing anything on disk. The file will be owned by the currently active player.
        /// </summary>
        /// <param name="fileBytes">Byte array to upload</param>
        /// <param name="fileName">Name of the file on LootLocker</param>
        /// <param name="filePurpose">Purpose of the file, example: savefile/config</param>
        /// <param name="isPublic">Should this file be viewable by other players?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UploadPlayerFile(byte[] fileBytes, string fileName, string filePurpose, bool isPublic, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>(forPlayerWithUlid));
                return;
            }

            var body = new Dictionary<string, string>()
            {
                { "purpose", filePurpose },
                { "public", isPublic.ToString().ToLower() }
            };

            LootLockerServerRequest.UploadFile(forPlayerWithUlid, LootLockerEndPoints.uploadPlayerFile, fileBytes, Path.GetFileName(fileName), "multipart/form-data", body,
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UploadPlayerFile(byte[] fileBytes, string fileName, string filePurpose, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            UploadPlayerFile(fileBytes, fileName, filePurpose, false, onComplete, forPlayerWithUlid);
        }

        ///////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Update an existing player file with a new file.
        /// </summary>
        /// <param name="fileId">Id of the file. You can get the ID of files when you upload a file, or with GetAllPlayerFiles()</param>
        /// <param name="pathToFile">Path to the file, example: Application.persistentDataPath + "/" + fileName;</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerFile</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdatePlayerFile(int fileId, string pathToFile, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>(forPlayerWithUlid));
                return;
            }

            var fileBytes = new byte[] { };
            try
            {
                fileBytes = File.ReadAllBytes(pathToFile);
            }
            catch (Exception e)
            {
                LootLockerLogger.Log($"File error: {e.Message}", LootLockerLogger.LogLevel.Error);
                return;
            }

            var endpoint = LootLockerEndPoints.updatePlayerFile.WithPathParameter(fileId);

            LootLockerServerRequest.UploadFile(forPlayerWithUlid, endpoint, LootLockerEndPoints.updatePlayerFile.httpMethod, fileBytes, Path.GetFileName(pathToFile), "multipart/form-data", new Dictionary<string, string>(),
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdatePlayerFile(int fileId, FileStream fileStream, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>(forPlayerWithUlid));
                return;
            }

            var fileBytes = new byte[fileStream.Length];
            try
            {
                fileStream.Read(fileBytes, 0, Convert.ToInt32(fileStream.Length));
            }
            catch (Exception e)
            {
                LootLockerLogger.Log($"File error: {e.Message}", LootLockerLogger.LogLevel.Error);
                return;
            }

            var endpoint = LootLockerEndPoints.updatePlayerFile.WithPathParameter(fileId);

            LootLockerServerRequest.UploadFile(forPlayerWithUlid, endpoint, LootLockerEndPoints.updatePlayerFile.httpMethod, fileBytes, Path.GetFileName(fileStream.Name), "multipart/form-data", new Dictionary<string, string>(),
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdatePlayerFile(int fileId, byte[] fileBytes, Action<LootLockerPlayerFile> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerFile>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.updatePlayerFile.WithPathParameter(fileId);

            LootLockerServerRequest.UploadFile(forPlayerWithUlid, endpoint, LootLockerEndPoints.updatePlayerFile.httpMethod, fileBytes, null, "multipart/form-data", new Dictionary<string, string>(),
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeletePlayerFile(int fileId, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.deletePlayerFile.WithPathParameter(fileId);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.DELETE, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        #endregion

        #region Player progressions

        /// <summary>
        /// Returns multiple progressions the player is currently on.
        /// </summary>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">Used for pagination, id of the player progression from which the pagination starts from, use the next_cursor and previous_cursor values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedPlayerProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetPlayerProgressions(int count, string after, Action<LootLockerPaginatedPlayerProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedPlayerProgressionsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getAllPlayerProgressions.endPoint;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("count", count);
            if (!string.IsNullOrEmpty(after))
                queryParams.Add("after", after);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progressions the player is currently on.
        /// </summary>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedPlayerProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetPlayerProgressions(int count, Action<LootLockerPaginatedPlayerProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetPlayerProgressions(count, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns multiple progressions the player is currently on.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedPlayerProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetPlayerProgressions(Action<LootLockerPaginatedPlayerProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetPlayerProgressions(-1, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns a single progression the player is currently on.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgression</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetPlayerProgression(string progressionKey, Action<LootLockerPlayerProgressionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerProgressionResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getSinglePlayerProgression.WithPathParameter(progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Adds points to a player progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to be added</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void AddPointsToPlayerProgression(string progressionKey, ulong amount, Action<LootLockerPlayerProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerProgressionWithRewardsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.addPointsToPlayerProgression.WithPathParameter(progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Subtracts points from a player progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to be subtracted</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SubtractPointsFromPlayerProgression(string progressionKey, ulong amount, Action<LootLockerPlayerProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerProgressionWithRewardsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.subtractPointsFromPlayerProgression.WithPathParameter(progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Resets a player progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ResetPlayerProgression(string progressionKey, Action<LootLockerPlayerProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerProgressionWithRewardsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.resetPlayerProgression.WithPathParameter(progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.POST, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Deletes a player progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeletePlayerProgression(string progressionKey, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.deletePlayerProgression.WithPathParameter(progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.DELETE, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Registers a player progression if it doesn't exist. Same as adding 0 points to a progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RegisterPlayerProgression(string progressionKey, Action<LootLockerPlayerProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            AddPointsToPlayerProgression(progressionKey, 0, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns multiple progressions that the specified player is currently on.
        /// </summary>
        /// <param name="playerUlid">The ulid of the player you wish to look up</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">Used for pagination, id of the player progression from which the pagination starts from, use the next_cursor and previous_cursor values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedPlayerProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersProgressions(string playerUlid, int count, string after, Action<LootLockerPaginatedPlayerProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedPlayerProgressionsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getAllOtherPlayersProgressions.WithPathParameter(playerUlid);

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("count", count);
            if (!string.IsNullOrEmpty(after))
                queryParams.Add("after", after);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progressions that the specified player is currently on.
        /// </summary>
        /// <param name="playerUlid">The ulid of the player you wish to look up</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedPlayerProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersProgressions(string playerUlid, int count, Action<LootLockerPaginatedPlayerProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetOtherPlayersProgressions(playerUlid, count, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns multiple progressions that the specified player is currently on.
        /// </summary>
        /// <param name="playerUlid">The ulid of the player you wish to look up</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedPlayerProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersProgressions(string playerUlid, Action<LootLockerPaginatedPlayerProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetOtherPlayersProgressions(playerUlid, -1, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns a single progression that the specified player is currently on.
        /// </summary>
        /// <param name="playerUlid">The ulid of the player you wish to look up</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerProgression</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersProgression(string playerUlid, string progressionKey, Action<LootLockerPlayerProgressionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerProgressionResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getSingleOtherPlayersProgression.WithPathParameters(progressionKey, playerUlid);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CreateHero(int heroId, string name, bool isDefault, Action<LootLockerHeroLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {

                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerCreateHeroRequest data = new LootLockerCreateHeroRequest();

            data.hero_id = heroId;
            data.name = name;
            data.is_default = isDefault;


            LootLockerAPIManager.CreateHero(data, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List the heroes with names and character information
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGameHeroResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetGameHeroes(Action<LootLockerGameHeroResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGameHeroResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetGameHeroes(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// List the heroes that the current player owns
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListPlayerHeroes(Action<LootLockerListHeroResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListHeroResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.ListPlayerHeroes(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// List player that the player with the specified SteamID64 owns
        /// </summary>
        /// <param name="steamID64">Steam id of the requested player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListOtherPlayersHeroesBySteamID64(int steamID64, Action<LootLockerPlayerHeroResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.ListOtherPlayersHeroesBySteamID64(forPlayerWithUlid, steamID64, onComplete);
        }

        /// <summary>
        /// Create a hero for the current player with the supplied name from the game hero specified with the supplied hero id, asset variation id, and whether to set as default.
        /// </summary>
        /// <param name="name">The new name for the hero</param>
        /// <param name="heroId">The id of the hero</param>
        /// <param name="assetVariationId">ID of the asset variation to use</param>
        /// <param name="isDefault">Should this hero be the default hero?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CreateHeroWithVariation(string name, int heroId, int assetVariationId, bool isDefault, Action<LootLockerHeroLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerCreateHeroWithVariationRequest data = new LootLockerCreateHeroWithVariationRequest();

            data.name = name;
            data.hero_id = heroId;
            data.asset_variation_id = assetVariationId;
            data.is_default = isDefault;

            LootLockerAPIManager.CreateHeroWithVariation(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Return information about the requested hero on the current player
        /// </summary>
        /// <param name="heroId">The id of the hero to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetHero(int heroId, Action<LootLockerPlayerHeroResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.GetHero(forPlayerWithUlid, heroId, onComplete);
        }

        /// <summary>
        /// Get the default hero for the player with the specified SteamID64
        /// </summary>
        /// <param name="steamId">Steam Id of the requested player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersDefaultHeroBySteamID64(int steamId, Action<LootLockerPlayerHeroResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.GetOtherPlayersDefaultHeroBySteamID64(forPlayerWithUlid, steamId, onComplete);

        }

        /// <summary>
        /// Update the name of the hero with the specified id and/or set it as default for the current player
        /// </summary>
        /// <param name="heroId">Id of the hero</param>
        /// <param name="name">The new name for the hero</param>
        /// <param name="isDefault">Should this hero be the default hero?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdateHero(string heroId, string name, bool isDefault, Action<LootLockerPlayerHeroResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(heroId);

            LootLockerUpdateHeroRequest data = new LootLockerUpdateHeroRequest();
            data.name = name;
            data.is_default = isDefault;


            LootLockerAPIManager.UpdateHero(forPlayerWithUlid, lootLockerGetRequest, data, onComplete);
        }

        /// <summary>
        /// Remove the hero with the specified id from the current players list of heroes.
        /// </summary>
        /// <param name="heroID">HeroID Id of the hero</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerHeroResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeleteHero(int heroID, Action<LootLockerPlayerHeroResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerHeroResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.DeleteHero(forPlayerWithUlid, heroID, onComplete);
        }

        /// <summary>
        /// List Asset Instances owned by the specified hero
        /// </summary>
        /// <param name="heroID">HeroID Id of the hero</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInventoryResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetHeroInventory(int heroID, Action<LootLockerInventoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInventoryResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.GetHeroInventory(forPlayerWithUlid, heroID, onComplete);
        }

        /// <summary>
        /// List the loadout of the specified hero that the current player owns
        /// </summary>
        /// <param name="heroID">HeroID Id of the hero</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetHeroLoadout(int HeroID, Action<LootLockerHeroLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.GetHeroLoadout(forPlayerWithUlid, HeroID, onComplete);
        }

        /// <summary>
        /// List the loadout of the specified hero that the another player owns
        /// </summary>
        /// <param name="heroID">HeroID Id of the hero</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersHeroLoadout(int heroID, Action<LootLockerHeroLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.GetOtherPlayersHeroLoadout(forPlayerWithUlid, heroID, onComplete);
        }

        /// <summary>
        /// Equip the specified Asset Instance to the specified Hero that the current player owns
        /// </summary>
        /// <param name="heroID">Id of the hero</param>
        /// <param name="assetInstanceID">Id of the asset instance to give</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void AddAssetToHeroLoadout(int heroID, int assetInstanceID, Action<LootLockerHeroLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAddAssetToHeroLoadoutRequest data = new LootLockerAddAssetToHeroLoadoutRequest();

            data.asset_instance_id = assetInstanceID;
            data.hero_id = heroID;


            LootLockerAPIManager.AddAssetToHeroLoadout(forPlayerWithUlid, heroID, data, onComplete);
        }

        /// <summary>
        /// Equip the specified Asset Variation to the specified Hero that the current player owns
        /// </summary>
        /// 
        /// <param name="heroID">Id of the hero</param>
        /// <param name="assetID">Id of the asset</param>
        /// <param name="assetInstanceID">Id of the asset instance to give</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void AddAssetVariationToHeroLoadout(int heroID, int assetID, int assetInstanceID, Action<LootLockerHeroLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAddAssetVariationToHeroLoadoutRequest data = new LootLockerAddAssetVariationToHeroLoadoutRequest();

            data.hero_id = heroID;
            data.asset_id = assetID;
            data.asset_variation_id = assetInstanceID;

            LootLockerAPIManager.AddAssetVariationToHeroLoadout(forPlayerWithUlid, heroID, data, onComplete);
        }

        /// <summary>
        /// Unequip the specified Asset Instance to the specified Hero that the current player owns
        /// </summary>
        /// 
        /// <param name="assetID">Id of the asset</param>
        /// <param name="heroID">Id of the hero</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerHeroLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RemoveAssetFromHeroLoadout(int assetID, int heroID, Action<LootLockerHeroLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerHeroLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.RemoveAssetFromHeroLoadout(forPlayerWithUlid, heroID, assetID, onComplete);
        }

        #endregion

        #region Base Classes
        /// <summary>
        /// Create a Class with the provided type and name. The Class will be owned by the currently active player.
        /// Use ListClassTypes() to get a list of available Class types for your game.
        /// </summary>
        /// <param name="classTypeID">Use ListClassTypes() to get a list of available class types for your game.</param>
        /// <param name="newClassName">The new name for the class</param>
        /// <param name="isDefault">Should this class be the default class?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CreateClass(string classTypeID, string newClassName, bool isDefault, Action<LootLockerClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerCreateClassRequest data = new LootLockerCreateClassRequest();

            data.name = newClassName;
            data.is_default = isDefault;
            data.character_type_id = classTypeID;

            LootLockerAPIManager.CreateClass(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Delete a Class with the provided classId. The Class will be removed from the currently active player.
        /// </summary>
        /// <param name="classId">The id of the class you want to delete</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeleteClass(int classId, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.DeleteClass(forPlayerWithUlid, classId, onComplete);
        }

        /// <summary>
        /// List all available Class types for your game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerListClassTypesResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListClassTypes(Action<LootLockerListClassTypesResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListClassTypesResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.ListClassTypes(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Get list of classes to a player
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPlayerClassListResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListPlayerClasses(Action<LootLockerPlayerClassListResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPlayerClassListResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.ListPlayerClasses(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Get all class loadouts for your game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetClassLoadout(Action<LootLockerClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetClassLoadout(forPlayerWithUlid, onComplete);

        }

        /// <summary>
        /// Get a class loadout from a specific player and platform
        /// </summary>
        /// <param name="playerID">ID of the player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersClassLoadout(string playerID, Action<LootLockerClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            GetOtherPlayersClassLoadout(playerID, playerData == null ? LL_AuthPlatforms.None : playerData.CurrentPlatform.Platform, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get a class loadout from a specific player and platform
        /// </summary>
        /// <param name="playerID">ID of the player</param>
        /// <param name="platform">The platform that the ID of the player is for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersClassLoadout(string playerID, LL_AuthPlatforms platform, Action<LootLockerClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();

            data.getRequests.Add(playerID);
            data.getRequests.Add(LootLockerAuthPlatform.GetPlatformRepresentation(platform).PlatformString);
            LootLockerAPIManager.GetOtherPlayersClassLoadout(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Get a class loadout from a specific player by the player's UID
        /// </summary>
        /// <param name="playerUid">The UID of the player</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersClassLoadoutByUid(string playerUid, Action<LootLockerClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            LootLockerAPIManager.GetOtherPlayersClassLoadoutByUid(forPlayerWithUlid, playerUid, onComplete);
        }

        /// <summary>
        /// Update information about the class. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="classID">ID of the class</param>
        /// <param name="newClassName">New name for the class</param>
        /// <param name="isDefault">Should the class be the default class?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdateClass(string classID, string newClassName, bool isDefault, Action<LootLockerClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerUpdateClassRequest data = new LootLockerUpdateClassRequest();

            data.name = newClassName;
            data.is_default = isDefault;

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(classID);

            LootLockerAPIManager.UpdateClass(forPlayerWithUlid, lootLockerGetRequest, data, onComplete);
        }

        /// <summary>
        /// Set the class with classID as the default class for the currently active player. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="classID">ID of the class</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SetDefaultClass(string classID, Action<LootLockerClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerUpdateClassRequest data = new LootLockerUpdateClassRequest();

            data.is_default = true;

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(classID);

            LootLockerAPIManager.UpdateClass(forPlayerWithUlid, lootLockerGetRequest, data, onComplete);
        }

        /// <summary>
        /// Equip an asset to the players default class.
        /// </summary>
        /// <param name="assetInstanceID">ID of the asset instance to equip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void EquipIdAssetToDefaultClass(string assetInstanceID, Action<EquipAssetToClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerEquipByIDRequest data = new LootLockerEquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceID);
            LootLockerAPIManager.EquipIdAssetToDefaultClass(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Equip a global asset to the players default class.
        /// </summary>
        /// <param name="assetID">ID of the asset instance to equip</param>
        /// <param name="assetVariationID">ID of the asset variation to use</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void EquipGlobalAssetToDefaultClass(string assetID, string assetVariationID, Action<EquipAssetToClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerEquipByAssetRequest data = new LootLockerEquipByAssetRequest();
            data.asset_id = int.Parse(assetID);
            data.asset_variation_id = int.Parse(assetVariationID);
            LootLockerAPIManager.EquipGlobalAssetToDefaultClass(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Equip an asset to a specific class. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="classID">ID of the class</param>
        /// <param name="assetInstanceID">ID of the asset instance to equip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToclassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void EquipIdAssetToClass(string classID, string assetInstanceID, Action<EquipAssetToClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerEquipByIDRequest data = new LootLockerEquipByIDRequest();
            data.instance_id = int.Parse(assetInstanceID);

            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(classID);
            LootLockerAPIManager.EquipIdAssetToClass(forPlayerWithUlid, lootLockerGetRequest, data, onComplete);
        }

        /// <summary>
        /// Equip a global asset to a specific class. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="assetID">ID of the asset to equip</param>
        /// <param name="assetVariationID">ID of the variation to use</param>
        /// <param name="classID">ID of the class to equip the asset to</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void EquipGlobalAssetToClass(string assetID, string assetVariationID, string classID, Action<EquipAssetToClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerEquipByAssetRequest data = new LootLockerEquipByAssetRequest();
            data.asset_id = int.Parse(assetID);
            data.asset_variation_id = int.Parse(assetVariationID);
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(classID);
            LootLockerAPIManager.EquipGlobalAssetToClass(forPlayerWithUlid, lootLockerGetRequest, data, onComplete);
        }

        /// <summary>
        /// Unequip an asset from the players default class.
        /// </summary>
        /// <param name="assetInstanceID">Asset instance ID of the asset to unequip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UnEquipIdAssetFromDefaultClass(string assetInstanceID, Action<EquipAssetToClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            lootLockerGetRequest.getRequests.Add(assetInstanceID);
            LootLockerAPIManager.UnEquipIdAssetToDefaultClass(forPlayerWithUlid, lootLockerGetRequest, onComplete);
        }

        /// <summary>
        /// Unequip an asset from a specific class. The class must be owned by the currently active player.
        /// </summary>
        /// <param name="classID">ID of the class to unequip</param>
        /// <param name="assetInstanceID">Asset instance ID of the asset to unequip</param>
        /// <param name="onComplete">onComplete Action for handling the response of type EquipAssetToClassLoadoutResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UnEquipIdAssetToClass(string classID, string assetInstanceID, Action<EquipAssetToClassLoadoutResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(classID);
            lootLockerGetRequest.getRequests.Add(assetInstanceID);
            LootLockerAPIManager.UnEquipIdAssetToClass(forPlayerWithUlid, lootLockerGetRequest, onComplete);
        }

        /// <summary>
        /// Get the loadout for the players default class.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetCurrentLoadoutToDefaultClassResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCurrentLoadoutToDefaultClass(Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCurrentLoadoutToDefaultClassResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetCurrentLoadoutToDefaultClass(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Get the current loadout for the default class of the specified player on the current platform
        /// </summary>
        /// <param name="playerID">ID of the player to get the loadout for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetCurrentLoadoutToDefaultClassResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCurrentLoadoutToOtherClass(string playerID, Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete, string forPlayerWithUlid = null)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            GetCurrentLoadoutToOtherClass(playerID, playerData == null ? LL_AuthPlatforms.None : playerData.CurrentPlatform.Platform, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get the current loadout for the default class of the specified player and platform
        /// </summary>
        /// <param name="playerID">ID of the player to get the loadout for</param>
        /// <param name="platform">The platform that the ID of the player is for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetCurrentLoadoutToDefaultClassResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCurrentLoadoutToOtherClass(string playerID, LL_AuthPlatforms platform, Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCurrentLoadoutToDefaultClassResponse>(forPlayerWithUlid));
            }
            LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();
            lootLockerGetRequest.getRequests.Add(playerID);
            lootLockerGetRequest.getRequests.Add(LootLockerAuthPlatform.GetPlatformRepresentation(platform).PlatformString);
            LootLockerAPIManager.GetCurrentLoadoutToOtherClass(forPlayerWithUlid, lootLockerGetRequest, onComplete);
        }

        /// <summary>
        /// Get the equippable contexts for the players default class.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerContextResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetEquipableContextToDefaultClass(Action<LootLockerContextResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerContextResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetEquipableContextToDefaultClass(forPlayerWithUlid, onComplete);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCharacterProgressions(int characterId, int count, string after, Action<LootLockerPaginatedCharacterProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedCharacterProgressionsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getAllCharacterProgressions.WithPathParameter(characterId);

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("count", count);
            if (!string.IsNullOrEmpty(after))
                queryParams.Add("after", after);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progressions the character is currently on.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedCharacterProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCharacterProgressions(int characterId, int count, Action<LootLockerPaginatedCharacterProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetCharacterProgressions(characterId, count, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns multiple progressions the character is currently on.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedCharacterProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCharacterProgressions(int characterId, Action<LootLockerPaginatedCharacterProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetCharacterProgressions(characterId, -1, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns a single progression the character is currently on.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterProgression</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCharacterProgression(int characterId, string progressionKey, Action<LootLockerCharacterProgressionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterProgressionResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getSingleCharacterProgression.WithPathParameters(characterId, progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Adds points to a character progression.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to add</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void AddPointsToCharacterProgression(int characterId, string progressionKey, ulong amount, Action<LootLockerCharacterProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterProgressionWithRewardsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.addPointsToCharacterProgression.WithPathParameters(characterId, progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Subtracts points from a character progression.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to subtract</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SubtractPointsFromCharacterProgression(int characterId, string progressionKey, ulong amount, Action<LootLockerCharacterProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterProgressionWithRewardsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.subtractPointsFromCharacterProgression.WithPathParameters(characterId, progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Resets a character progression.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCharacterProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ResetCharacterProgression(int characterId, string progressionKey, Action<LootLockerCharacterProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCharacterProgressionWithRewardsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.resetCharacterProgression.WithPathParameters(characterId, progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.POST, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Deletes a character progression.
        /// </summary>
        /// <param name="characterId">Id of the character</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeleteCharacterProgression(int characterId, string progressionKey, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.deleteCharacterProgression.WithPathParameters(characterId, progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.DELETE, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region PlayerStorage
        /// <summary>
        /// Get the player storage for the currently active player (key/values).
        /// Note: The Player Metadata feature will over time replace Player Persistent Storage.
        /// If you are not already deeply integrated with the Player Persistent Storage in your game, consider moving to Player Metadata.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStorageResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetEntirePersistentStorage(Action<LootLockerGetPersistentStorageResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStorageResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetEntirePersistentStorage(forPlayerWithUlid, onComplete);
        }
        /// <summary>
        /// Get the player storage as a Dictionary<string, string> for the currently active player (key/values).
        /// Note: The Player Metadata feature will over time replace Player Persistent Storage.
        /// If you are not already deeply integrated with the Player Persistent Storage in your game, consider moving to Player Metadata.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStorageResponseDictionary</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetEntirePersistentStorage(Action<LootLockerGetPersistentStorageResponseDictionary> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStorageResponseDictionary>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetEntirePersistentStorage(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Get a specific key from the player storage for the currently active player.
        /// Note: The Player Metadata feature will over time replace Player Persistent Storage.
        /// If you are not already deeply integrated with the Player Persistent Storage in your game, consider moving to Player Metadata.
        /// </summary>
        /// <param name="key">Name of the key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentSingle</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetSingleKeyPersistentStorage(string key, Action<LootLockerGetPersistentSingle> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentSingle>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(key);
            LootLockerAPIManager.GetSingleKeyPersistentStorage(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Update or create a key/value pair in the player storage for the currently active player.
        /// Note: The Player Metadata feature will over time replace Player Persistent Storage.
        /// If you are not already deeply integrated with the Player Persistent Storage in your game, consider moving to Player Metadata.
        /// </summary>
        /// <param name="key">Name of the key</param>
        /// <param name="value">Value of the key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStorageResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdateOrCreateKeyValue(string key, string value, Action<LootLockerGetPersistentStorageResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStorageResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetPersistentStorageRequest data = new LootLockerGetPersistentStorageRequest();
            data.AddToPayload(new LootLockerPayload { key = key, value = value });
            LootLockerAPIManager.UpdateOrCreateKeyValue(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Update or create a key/value pair in the player storage for the currently active player.
        /// Note: The Player Metadata feature will over time replace Player Persistent Storage.
        /// If you are not already deeply integrated with the Player Persistent Storage in your game, consider moving to Player Metadata.
        /// </summary>
        /// <param name="key">Name of the key</param>
        /// <param name="value">Value of the key</param>
        /// <param name="isPublic">Is the key public?</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStorageResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdateOrCreateKeyValue(string key, string value, bool isPublic, Action<LootLockerGetPersistentStorageResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStorageResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetPersistentStorageRequest data = new LootLockerGetPersistentStorageRequest();
            data.AddToPayload(new LootLockerPayload { key = key, value = value, is_public = isPublic });
            LootLockerAPIManager.UpdateOrCreateKeyValue(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Update or create multiple key/value pairs in the player storage for the currently active player.
        /// Note: The Player Metadata feature will over time replace Player Persistent Storage.
        /// If you are not already deeply integrated with the Player Persistent Storage in your game, consider moving to Player Metadata.
        /// </summary>
        /// <param name="data">A LootLockerGetPersistentStorageRequest with multiple keys</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStorageResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdateOrCreateKeyValue(LootLockerGetPersistentStorageRequest data, Action<LootLockerGetPersistentStorageResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStorageResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.UpdateOrCreateKeyValue(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Delete a key from the player storage for the currently active player.
        /// Note: The Player Metadata feature will over time replace Player Persistent Storage.
        /// If you are not already deeply integrated with the Player Persistent Storage in your game, consider moving to Player Metadata.
        /// </summary>
        /// <param name="keyToDelete">The key/value key(name) to delete</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStorageResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeleteKeyValue(string keyToDelete, Action<LootLockerGetPersistentStorageResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStorageResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(keyToDelete);
            LootLockerAPIManager.DeleteKeyValue(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Get the public player storage(key/values) for a specific player.
        /// Note: The Player Metadata feature will over time replace Player Persistent Storage.
        /// If you are not already deeply integrated with the Player Persistent Storage in your game, consider moving to Player Metadata.
        /// </summary>
        /// <param name="otherPlayerId">The ID of the player to retrieve the public ley/values for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetPersistentStorageResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetOtherPlayersPublicKeyValuePairs(string otherPlayerId, Action<LootLockerGetPersistentStorageResponse> onComplete, string forPlayerWithUlid = null)
        {

            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetPersistentStorageResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(otherPlayerId);
            LootLockerAPIManager.GetOtherPlayersPublicKeyValuePairs(forPlayerWithUlid, data, onComplete);
        }
        #endregion

        #region Assets
        /// <summary>
        /// Get the available contexts for the game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerContextResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetContext(Action<LootLockerContextResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerContextResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetContext(forPlayerWithUlid, onComplete);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetsOriginal(int assetCount, Action<LootLockerAssetResponse> onComplete, int? idOfLastAsset = null, List<LootLocker.LootLockerEnums.AssetFilter> filter = null, bool includeUGC = false, Dictionary<string, string> assetFilters = null, int UGCCreatorPlayerID = 0, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetAssetsOriginal(forPlayerWithUlid, onComplete, assetCount, idOfLastAsset, filter, includeUGC, assetFilters, UGCCreatorPlayerID);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetListWithCount(int assetCount, Action<LootLockerAssetResponse> onComplete, List<LootLocker.LootLockerEnums.AssetFilter> filter = null, bool includeUGC = false, Dictionary<string, string> assetFilters = null, int UGCCreatorPlayerID = 0, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetAssetsOriginal(forPlayerWithUlid, (response) =>
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetNextList(int assetCount, Action<LootLockerAssetResponse> onComplete, List<LootLocker.LootLockerEnums.AssetFilter> filter = null, bool includeUGC = false, Dictionary<string, string> assetFilters = null, int UGCCreatorPlayerID = 0, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.GetAssetsOriginal(forPlayerWithUlid, (response) =>
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

        /// <summary>
        /// Get information about a specific asset.
        /// </summary>
        /// <param name="assetId">The ID of the asset that you want information about</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSingleAssetResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetInformation(int assetId, Action<LootLockerSingleAssetResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetAssetById(assetId, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get information about a specific asset.
        /// </summary>
        /// <param name="assetId">The ID of the asset that you want information about</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSingleAssetResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetById(int assetId, Action<LootLockerSingleAssetResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSingleAssetResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();

            data.getRequests.Add(assetId.ToString());

            // Using GetAssetByID in the background
            LootLockerAPIManager.GetAssetById(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// List the current players favorite assets.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerFavouritesListResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFavouriteAssets(Action<LootLockerFavouritesListResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFavouritesListResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.ListFavouriteAssets(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Add an asset to the current players favorite assets.
        /// </summary>
        /// <param name="assetId">The ID of the asset to add to favourites</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void AddFavouriteAsset(string assetId, Action<LootLockerAssetResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.AddFavouriteAsset(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Remove an asset from the current players favorite assets.
        /// </summary>
        /// <param name="assetId">The ID of the asset to remove from favourites</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RemoveFavouriteAsset(string assetId, Action<LootLockerAssetResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId);
            LootLockerAPIManager.RemoveFavouriteAsset(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Get multiple assets by their IDs.
        /// </summary>
        /// <param name="assetIdsToRetrieve">A list of multiple assets to retrieve</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetsById(string[] assetIdsToRetrieve, Action<LootLockerAssetResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();

            for (int i = 0; i < assetIdsToRetrieve.Length; i++)
                data.getRequests.Add(assetIdsToRetrieve[i]);

            LootLockerAPIManager.GetAssetsById(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Grant an Asset to the Player's Inventory.
        /// </summary>
        /// <param name="assetID">The Asset you want to create an Instance of and give to the current player</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GrantAssetToPlayerInventory(int assetID, Action<LootLockerGrantAssetResponse> onComplete, string forPlayerWithUlid = null)
        {
            GrantAssetToPlayerInventory(assetID, null, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Grant an Asset Instance to the Player's Inventory.
        /// </summary>
        /// <param name="assetID">The Asset you want to create an Instance of and give to the current player</param>
        /// <param name="assetVariationID">The id of the Asset Variation you want to grant</param>
        /// <param name="assetRentalOptionID">The rental option id you want to give the Asset Instance</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGrantAssetResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GrantAssetToPlayerInventory(int assetID, int? assetVariationID, int? assetRentalOptionID, Action<LootLockerGrantAssetResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGrantAssetResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerGrantAssetRequest data = new LootLockerGrantAssetRequest();
            data.asset_id = assetID;
            data.asset_variation_id = assetVariationID;
            data.asset_rental_option_id = assetRentalOptionID;

            LootLockerAPIManager.GrantAssetToPlayerInventory(forPlayerWithUlid, data, onComplete);
        }

        #endregion
        /// <summary>
        /// List assets with default parameters (no filters, first page, default page size).
        /// </summary>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional: Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListAssetsWithDefaultParameters(Action<LootLockerListAssetsResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListAssets(new LootLockerListAssetsRequest(), onComplete, forPlayerWithUlid: forPlayerWithUlid);
        }

        /// <summary>
        /// List assets with configurable response data. Use this to limit the fields returned in the response and improve performance.
        /// </summary>
        /// <param name="Request">Request object with settings on what fields to include, exclude, and what assets to filter</param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="PerPage">(Optional) Used together with Page to apply pagination to this Request. PerPage designates how many notifications are considered a "page". Set to 0 to not use this filter.</param>
        /// <param name="Page">(Optional) Used together with PerPage to apply pagination to this Request. Page designates which "page" of items to fetch. Set to 0 to not use this filter.</param>
        /// <param name="orderBy">(Optional) Order the list by a specific field. Default is unordered.</param>
        /// <param name="orderDirection">(Optional) Order the list in ascending or descending order. Default is unordered.</param>
        /// <param name="forPlayerWithUlid">Optional: Execute the Request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListAssets(LootLockerListAssetsRequest Request, Action<LootLockerListAssetsResponse> onComplete, int PerPage = 0, int Page = 0, OrderAssetListBy orderBy = OrderAssetListBy.none, OrderAssetListDirection orderDirection = OrderAssetListDirection.none, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListAssetsResponse>(forPlayerWithUlid));
                return;
            }

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (Page > 0)
                queryParams.Add("page", Page.ToString());
            if (PerPage > 0)
                queryParams.Add("per_page", PerPage.ToString());
            if (orderBy != OrderAssetListBy.none)
                queryParams.Add("order_by", orderBy.ToString());
            if (orderDirection != OrderAssetListDirection.none)
                queryParams.Add("order_direction", orderDirection.ToString());

            string endPoint = LootLockerEndPoints.ListAssets.endPoint + queryParams.Build();

            string body = LootLockerJson.SerializeObject(Request);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, LootLockerEndPoints.ListAssets.httpMethod, body,
                (response) =>
                {
                    var parsedResponse = LootLockerResponse.Deserialize<LootLockerListAssetsResponse>(response);
                    onComplete?.Invoke(parsedResponse);
                });
        }

        #region AssetInstance
        /// <summary>
        /// Get all key/value pairs for all asset instances.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllKeyValuePairsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllKeyValuePairsForAssetInstances(Action<LootLockerGetAllKeyValuePairsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetAllKeyValuePairsResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetAllKeyValuePairs(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Get all key/value pairs for a specific asset instance.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to get the key/value-pairs for</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllKeyValuePairsToAnInstance(int assetInstanceID, Action<LootLockerAssetDefaultResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.GetAllKeyValuePairsToAnInstance(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Get a specific key/value pair for a specific asset instance.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to get the key/value-pairs for</param>
        /// <param name="keyValueID">The ID of the key-value to get. Can be obtained when creating a new key/value-pair or with GetAllKeyValuePairsToAnInstance()</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetSingleKeyValuePairsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAKeyValuePairByIdForAssetInstances(int assetInstanceID, int keyValueID, Action<LootLockerGetSingleKeyValuePairsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetSingleKeyValuePairsResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            data.getRequests.Add(keyValueID.ToString());
            LootLockerAPIManager.GetAKeyValuePairById(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Create a new key/value pair for a specific asset instance.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to create the key/value for</param>
        /// <param name="key">Key(name)</param>
        /// <param name="value">The value of the key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CreateKeyValuePairForAssetInstances(int assetInstanceID, string key, string value, Action<LootLockerAssetDefaultResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerCreateKeyValuePairRequest createKeyValuePairRequest = new LootLockerCreateKeyValuePairRequest();
            createKeyValuePairRequest.key = key;
            createKeyValuePairRequest.value = value;
            LootLockerAPIManager.CreateKeyValuePair(forPlayerWithUlid, data, createKeyValuePairRequest, onComplete);
        }

        /// <summary>
        /// Update a specific key/value pair for a specific asset instance. Data is provided as key/value pairs.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to create the key/value for</param>
        /// <param name="data">A Dictionary<string, string> for multiple key/values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdateOneOrMoreKeyValuePairForAssetInstances(int assetInstanceID, Dictionary<string, string> data, Action<LootLockerAssetDefaultResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>(forPlayerWithUlid));
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
            LootLockerAPIManager.UpdateOneOrMoreKeyValuePair(forPlayerWithUlid, request, createKeyValuePairRequest, onComplete);
        }
        /// <summary>
        /// Update a specific key/value pair for a specific asset instance by key.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to update the key/value for</param>
        /// <param name="key">Name of the key to update</param>
        /// <param name="value">The new value of the key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdateKeyValuePairForAssetInstances(int assetInstanceID, string key, string value, Action<LootLockerAssetDefaultResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest request = new LootLockerGetRequest();
            request.getRequests.Add(assetInstanceID.ToString());
            LootLockerUpdateOneOrMoreKeyValuePairRequest createKeyValuePairRequest = new LootLockerUpdateOneOrMoreKeyValuePairRequest();
            List<LootLockerCreateKeyValuePairRequest> temp = new List<LootLockerCreateKeyValuePairRequest>();
            temp.Add(new LootLockerCreateKeyValuePairRequest { key = key, value = value });
            createKeyValuePairRequest.storage = temp.ToArray();
            LootLockerAPIManager.UpdateOneOrMoreKeyValuePair(forPlayerWithUlid, request, createKeyValuePairRequest, onComplete);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdateKeyValuePairByIdForAssetInstances(int assetInstanceID, int keyValueID, string value, string key, Action<LootLockerAssetDefaultResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>(forPlayerWithUlid));
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
            LootLockerAPIManager.UpdateKeyValuePairById(forPlayerWithUlid, data, createKeyValuePairRequest, onComplete);
        }

        /// <summary>
        /// Delete a specific key/value pair for a specific asset instance by key/value-id.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID to delete the key/value for</param>
        /// <param name="keyValueID">ID of the key/value, can be obtained when creating the key or by using GetAllKeyValuePairsToAnInstance()</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetDefaultResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeleteKeyValuePairForAssetInstances(int assetInstanceID, int keyValueID, Action<LootLockerAssetDefaultResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetDefaultResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            data.getRequests.Add(keyValueID.ToString());
            LootLockerAPIManager.DeleteKeyValuePair(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Get the drop rates for a loot box asset instance.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID of the loot box</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerInspectALootBoxResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void InspectALootBoxForAssetInstances(int assetInstanceID, Action<LootLockerInspectALootBoxResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInspectALootBoxResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.InspectALootBox(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Open a loot box asset instance. The loot box will be consumed and the contents will be added to the player's inventory.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID of the loot box</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerOpenLootBoxResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void OpenALootBoxForAssetInstances(int assetInstanceID, Action<LootLockerOpenLootBoxResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerOpenLootBoxResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.OpenALootBox(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Delete an Asset Instance from the current Player's Inventory.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID of the asset instance you want to delete from the current Players Inventory</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeleteAssetInstanceFromPlayerInventory(int assetInstanceID, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.DeleteAssetInstanceFromPlayerInventory(forPlayerWithUlid, data, onComplete);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetInstanceProgressions(int assetInstanceId, int count, string after, Action<LootLockerPaginatedAssetInstanceProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedAssetInstanceProgressionsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getAllAssetInstanceProgressions.WithPathParameter(assetInstanceId);

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("count", count);
            if (!string.IsNullOrEmpty(after))
                queryParams.Add("after", after);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progressions for an asset instance.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedAssetInstanceProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetInstanceProgressions(int assetInstanceId, int count, Action<LootLockerPaginatedAssetInstanceProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetAssetInstanceProgressions(assetInstanceId, count, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns multiple progressions for an asset instance.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedAssetInstanceProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetInstanceProgressions(int assetInstanceId, Action<LootLockerPaginatedAssetInstanceProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetAssetInstanceProgressions(assetInstanceId, -1, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns multiple progressions for an asset instance.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetInstanceProgression</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAssetInstanceProgression(int assetInstanceId, string progressionKey, Action<LootLockerAssetInstanceProgressionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetInstanceProgressionResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getSingleAssetInstanceProgression.WithPathParameters(assetInstanceId, progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Adds points to an asset instance progression.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to add</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetInstanceProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void AddPointsToAssetInstanceProgression(int assetInstanceId, string progressionKey, ulong amount, Action<LootLockerAssetInstanceProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetInstanceProgressionWithRewardsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.addPointsToAssetInstanceProgression.WithPathParameters(assetInstanceId, progressionKey);

            var body = LootLockerJson.SerializeObject(new { amount });  

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Subtracts points from an asset instance progression.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="amount">Amount of points to subtract</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetInstanceProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SubtractPointsFromAssetInstanceProgression(int assetInstanceId, string progressionKey, ulong amount, Action<LootLockerAssetInstanceProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetInstanceProgressionWithRewardsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.subtractPointsFromAssetInstanceProgression.WithPathParameters(assetInstanceId, progressionKey);
            
            var body = LootLockerJson.SerializeObject(new { amount });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.POST, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Resets an asset instance progression.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerAssetInstanceProgressionWithRewards</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ResetAssetInstanceProgression(int assetInstanceId, string progressionKey, Action<LootLockerAssetInstanceProgressionWithRewardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerAssetInstanceProgressionWithRewardsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.resetAssetInstanceProgression.WithPathParameters(assetInstanceId, progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.POST, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        
        /// <summary>
        /// Deletes an asset instance progression.
        /// </summary>
        /// <param name="assetInstanceId">ID of the asset instance</param>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeleteAssetInstanceProgression(int assetInstanceId, string progressionKey, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.deleteAssetInstanceProgression.WithPathParameters(assetInstanceId, progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.DELETE, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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
        /// <param name="complete">(Optional) Whether this asset is complete, if set to true this asset candidate will become an asset and can not be edited anymore</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CreatingAnAssetCandidate(string name, Action<LootLockerUserGenerateContentResponse> onComplete,
            Dictionary<string, string> kv_storage = null, Dictionary<string, string> filters = null,
            Dictionary<string, string> data_entities = null, int context_id = -1, bool complete = false, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>(forPlayerWithUlid));
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
                context_id = context_id
            };

            LootLockerCreatingOrUpdatingAnAssetCandidateRequest data = new LootLockerCreatingOrUpdatingAnAssetCandidateRequest
            {
                data = assetData,
                completed = complete
            };

            LootLockerAPIManager.CreatingAnAssetCandidate(forPlayerWithUlid, data, onComplete);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UpdatingAnAssetCandidate(int assetId, bool isCompleted, Action<LootLockerUserGenerateContentResponse> onComplete,
            string name = null, Dictionary<string, string> kv_storage = null, Dictionary<string, string> filters = null,
            Dictionary<string, string> data_entities = null, int context_id = -1, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>(forPlayerWithUlid));
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

            LootLockerAPIManager.UpdatingAnAssetCandidate(forPlayerWithUlid, data, getRequest, onComplete);
        }

        /// <summary>
        /// Delete an asset candidate.
        /// </summary>
        /// <param name="assetCandidateID">ID of the asset candidate to delete</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerUserGenerateContentResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeletingAnAssetCandidate(int assetCandidateID, Action<LootLockerUserGenerateContentResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetCandidateID.ToString());
            LootLockerAPIManager.DeletingAnAssetCandidate(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Get information about a single asset candidate.
        /// </summary>
        /// <param name="assetCandidateID">The ID of the asset candidate to receive information about</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerUserGenerateContentResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GettingASingleAssetCandidate(int assetCandidateID, Action<LootLockerUserGenerateContentResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetCandidateID.ToString());
            LootLockerAPIManager.GettingASingleAssetCandidate(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Get all asset candidates for the current player.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerListingAssetCandidatesResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListingAssetCandidates(Action<LootLockerListingAssetCandidatesResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListingAssetCandidatesResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.ListingAssetCandidates(forPlayerWithUlid, onComplete);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void AddingFilesToAssetCandidates(int assetCandidateID, string filePath, string fileName,
            FilePurpose filePurpose, Action<LootLockerUserGenerateContentResponse> onComplete, string fileContentType = null, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>(forPlayerWithUlid));
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

            LootLockerAPIManager.AddingFilesToAssetCandidates(forPlayerWithUlid, data, getRequest, onComplete);
        }

        /// <summary>
        /// Remove a file from an asset candidate.
        /// </summary>
        /// <param name="assetCandidateID">ID of the asset instance to remove the file from</param>
        /// <param name="fileId">ID of the file to remove</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerUserGenerateContentResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RemovingFilesFromAssetCandidates(int assetCandidateID, int fileId, Action<LootLockerUserGenerateContentResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerUserGenerateContentResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetCandidateID.ToString());
            data.getRequests.Add(fileId.ToString());

            LootLockerAPIManager.RemovingFilesFromAssetCandidates(forPlayerWithUlid, data, onComplete);
        }
        #endregion

        #region Progressions

        /// <summary>
        /// Returns multiple progressions.
        /// </summary>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">Used for pagination, id of the progression from which the pagination starts from, use the next_cursor and previous_cursor values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetProgressions(int count, string after, Action<LootLockerPaginatedProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedProgressionsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getAllProgressions.endPoint;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("count", count);
            if (!string.IsNullOrEmpty(after))
                queryParams.Add("after", after);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progressions.
        /// </summary>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetProgressions(int count, Action<LootLockerPaginatedProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetProgressions(count, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns multiple progressions.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressions</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetProgressions(Action<LootLockerPaginatedProgressionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetProgressions(-1, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns a single progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerProgression</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetProgression(string progressionKey, Action<LootLockerProgressionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerProgressionResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getSingleProgression.WithPathParameter(progressionKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progression tiers for the specified progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">Used for pagination, step of the tier from which the pagination starts from, use the next_cursor and previous_cursor values</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressionTiers</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetProgressionTiers(string progressionKey, int count, ulong? after, Action<LootLockerPaginatedProgressionTiersResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPaginatedProgressionTiersResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getProgressionTiers.WithPathParameter(progressionKey);

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("count", count);
            if (after.HasValue && after > 0)
                queryParams.Add("after", after ?? 0);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Returns multiple progression tiers for the specified progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressionTiers</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetProgressionTiers(string progressionKey, int count, Action<LootLockerPaginatedProgressionTiersResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetProgressionTiers(progressionKey, count, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns multiple progression tiers for the specified progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPaginatedProgressionTiers</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetProgressionTiers(string progressionKey, Action<LootLockerPaginatedProgressionTiersResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetProgressionTiers(progressionKey, -1, null, onComplete, forPlayerWithUlid);
        }
        
        /// <summary>
        /// Returns a single progression tier for the specified progression.
        /// </summary>
        /// <param name="progressionKey">Progression key</param>
        /// <param name="step">Step of the progression tier that is being fetched</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerProgressionTierResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetProgressionTier(string progressionKey, ulong step, Action<LootLockerProgressionTierResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerProgressionTierResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getProgressionTier.WithPathParameters(progressionKey, step);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerHTTPMethod.GET, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region Missions

        /// <summary>
        /// Get all available missions for the current game. Missions are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Missions here; https://docs.lootlocker.com/background/game-systems#missions
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGettingAllMissionsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllMissions(Action<LootLockerGetAllMissionsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetAllMissionsResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetAllMissions(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Get information about a single mission. Missions are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Missions here; https://docs.lootlocker.com/background/game-systems#missions
        /// </summary>
        /// <param name="missionId">The ID of the mission to get information about</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGettingASingleMissionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetMission(int missionId, Action<LootLockerGetMissionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMissionResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.GetMission(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Start a mission for the current player. Missions are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Missions here; https://docs.lootlocker.com/background/game-systems#missions
        /// </summary>
        /// <param name="missionId">The ID of the mission to start</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerStartingAMissionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void StartMission(int missionId, Action<LootLockerStartMissionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerStartMissionResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(missionId.ToString());
            LootLockerAPIManager.StartMission(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Finish a mission for the current player. Missions are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Missions here; https://docs.lootlocker.com/background/game-systems#missions
        /// </summary>
        /// <param name="missionId">The ID of the mission to start</param>
        /// <param name="startingMissionSignature">Mission signature is received when starting a mission</param>
        /// <param name="playerId">ID of the current player</param>
        /// <param name="finishingPayload">A LootLockerFinishingPayload with variables for how the mission was completed</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerFinishingAMissionResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void FinishMission(int missionId, string startingMissionSignature, string playerId,
            LootLockerFinishingPayload finishingPayload, Action<LootLockerFinishMissionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFinishMissionResponse>(forPlayerWithUlid));
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
            LootLockerAPIManager.FinishMission(forPlayerWithUlid, data, onComplete);
        }
        #endregion

        #region Maps
        /// <summary>
        /// Get all available maps for the current game. Maps are created with the Admin API https://ref.lootlocker.com/admin-api/#introduction together with data from your game. You can read more about Maps here; https://docs.lootlocker.com/background/game-systems#maps
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerMapsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllMaps(Action<LootLockerMapsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerMapsResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetAllMaps(forPlayerWithUlid, onComplete);
        }
        #endregion

        #region Purchasing
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void PollOrderStatus(int assetId, Action<LootLockerPurchaseOrderStatus> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseOrderStatus>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetId.ToString());
            LootLockerAPIManager.PollOrderStatus(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Activate a rental asset. This will grant the asset to the player and start the rental timer on the server.
        /// </summary>
        /// <param name="assetInstanceID">The asset instance ID of the asset to activate</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerActivateARentalAssetResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ActivateRentalAsset(int assetInstanceID, Action<LootLockerActivateRentalAssetResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerActivateRentalAssetResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(assetInstanceID.ToString());
            LootLockerAPIManager.ActivateRentalAsset(forPlayerWithUlid, data, onComplete);
        }

        /// <summary>
        /// Purchase one catalog item using a specified wallet
        /// </summary>
        /// <param name="walletID">The id of the wallet to use for the purchase</param>
        /// <param name="itemID">The id of the item that you want to purchase</param>
        /// <param name="quantity">The amount that you want to purchase the item </param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LootLockerPurchaseSingleCatalogItem(string walletID, string itemID, int quantity, Action<LootLockerPurchaseCatalogItemResponse> onComplete, string forPlayerWithUlid = null)
        {
            LootLockerCatalogItemAndQuantityPair item = new LootLockerCatalogItemAndQuantityPair();

            item.catalog_listing_id = itemID;
            item.quantity = quantity;

            LootLockerCatalogItemAndQuantityPair[] items = { item };

            LootLockerPurchaseCatalogItems(walletID, items, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Purchase one or more catalog items using a specified wallet
        /// </summary>
        /// <param name="walletId">The id of the wallet to use for the purchase</param>
        /// <param name="itemsToPurchase">A list of items to purchase along with the quantity of each item to purchase</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void LootLockerPurchaseCatalogItems(string walletId, LootLockerCatalogItemAndQuantityPair[] itemsToPurchase, Action<LootLockerPurchaseCatalogItemResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPurchaseCatalogItemResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerPurchaseCatalogItemRequest
            {
                wallet_id = walletId,
                items = itemsToPurchase
            });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.purchaseCatalogItem.endPoint, LootLockerEndPoints.purchaseCatalogItem.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Apple App Store for the current player
        /// </summary>
        /// <param name="transactionId">The id of the transaction successfully made towards the Apple App Store</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="sandboxed">Optional: Should this redemption be made towards sandbox App Store</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RedeemAppleAppStorePurchaseForPlayer(string transactionId, Action<LootLockerResponse> onComplete, bool sandboxed = false, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemAppleAppStorePurchaseForPlayerRequest()
            {
                transaction_id = transactionId,
                sandboxed = sandboxed
            });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.redeemAppleAppStorePurchase.endPoint, LootLockerEndPoints.redeemAppleAppStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Apple App Store for a class that the current player owns
        /// </summary>
        /// <param name="transactionId">The id of the transaction successfully made towards the Apple App Store</param>
        /// <param name="classId">The id of the class to redeem this transaction for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="sandboxed">Optional: Should this redemption be made towards sandbox App Store</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RedeemAppleAppStorePurchaseForClass(string transactionId, int classId, Action<LootLockerResponse> onComplete, bool sandboxed = false, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemAppleAppStorePurchaseForClassRequest()
            {
                transaction_id = transactionId,
                class_id = classId,
                sandboxed = sandboxed
            });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.redeemAppleAppStorePurchase.endPoint, LootLockerEndPoints.redeemAppleAppStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Google Play Store for the current player
        /// </summary>
        /// <param name="productId">The id of the product that this redemption refers to</param>
        /// <param name="purchaseToken">The token from the purchase successfully made towards the Google Play Store</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RedeemGooglePlayStorePurchaseForPlayer(string productId, string purchaseToken, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemGooglePlayStorePurchaseForPlayerRequest()
            {
                product_id = productId,
                purchase_token = purchaseToken
            });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.redeemGooglePlayStorePurchase.endPoint, LootLockerEndPoints.redeemGooglePlayStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Google Play Store for a class that the current player owns
        /// </summary>
        /// <param name="productId">The id of the product that this redemption refers to</param>
        /// <param name="purchaseToken">The token from the purchase successfully made towards the Google Play Store</param>
        /// <param name="classId">The id of the class to redeem this purchase for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RedeemGooglePlayStorePurchaseForClass(string productId, string purchaseToken, int classId, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemGooglePlayStorePurchaseForClassRequest()
            {
                product_id = productId,
                purchase_token = purchaseToken,
                class_id = classId
            });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.redeemGooglePlayStorePurchase.endPoint, LootLockerEndPoints.redeemGooglePlayStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Epic Store for the current player
        /// </summary>
        /// <param name="accountId">The Epic account id of the account that this purchase was made for</param>
        /// <param name="bearerToken">This is the token from epic used to allow the LootLocker backend to verify ownership of the specified entitlements. This is sometimes referred to as the Server Auth Ticket or Auth Token depending on your Epic integration.</param>
        /// <param name="entitlementIds">The ids of the purchased entitlements that you wish to redeem</param>
        /// <param name="sandboxId">The Sandbox Id configured for the game making the purchase (this is the sandbox id from your epic online service configuration)</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional: Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RedeemEpicStorePurchaseForPlayer(string accountId, string bearerToken, List<string> entitlementIds, string sandboxId, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemEpicStorePurchaseForPlayerRequest()
            {
                account_id = accountId,
                bearer_token = bearerToken,
                entitlement_ids = entitlementIds,
                sandbox_id = sandboxId
            });
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.redeemEpicStorePurchase.endPoint, LootLockerEndPoints.redeemEpicStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Epic Store for a class that the current player owns
        /// </summary>
        /// <param name="accountId">The Epic account id of the account that this purchase was made for</param>
        /// <param name="bearerToken">This is the token from epic used to allow the LootLocker backend to verify ownership of the specified entitlements. This is sometimes referred to as the Server Auth Ticket or Auth Token depending on your Epic integration.</param>
        /// <param name="entitlementIds">The ids of the purchased entitlements that you wish to redeem</param>
        /// <param name="sandboxId">The Sandbox Id configured for the game making the purchase (this is the sandbox id from your epic online service configuration)</param>
        /// <param name="classId">The id of the class to redeem this purchase for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional: Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RedeemEpicStorePurchaseForClass(string accountId, string bearerToken, List<string> entitlementIds, string sandboxId, int classId, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerRedeemEpicStorePurchaseForClassRequest()
            {
                account_id = accountId,
                bearer_token = bearerToken,
                entitlement_ids = entitlementIds,
                class_id = classId,
                sandbox_id = sandboxId
            });
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.redeemEpicStorePurchase.endPoint, LootLockerEndPoints.redeemEpicStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

#if LOOTLOCKER_BETA_PLAYSTATION_IAP
        /// <summary>
        /// Redeem a purchase that was made successfully towards the Playstation Store for the current player
        /// </summary>
        /// <param name="transaction_id">The transaction id from the PlayStation Store of the purchase to redeem </param>
        /// <param name="auth_code">The authorization code from the PlayStation Store of the purchase to redeem </param>
        /// <param name="entitlement_label">The entitlement label configured in the NP service for the entitlement that this redemption relates to </param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="service_label">Optional: The NP service label. </param>
        /// <param name="service_name">Optional: The abreviation of the service name of the ASM service ID service that was used when configuring the serviceIds. Possible Values: pssdc, cce. Default Value: pssdc </param>
        /// <param name="environment">Optional: The id of the environment you wish to make the request against. Allowed values: 1, 8, 256 </param>
        /// <param name="use_count">Optional: The use count for this redemption </param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RedeemPlaystationStorePurchaseForPlayer(string transaction_id, string auth_code, string entitlement_label, Action<LootLockerResponse> onComplete, string service_label = "", string service_name = "", int environment = -1, int use_count = -1, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            Dictionary<string, object> bodyDict = new Dictionary<string, object>
            {
                { "transaction_id", transaction_id },
                { "auth_code", auth_code },
                { "entitlement_label", entitlement_label }
            };
            if (!string.IsNullOrEmpty(service_label))
            {
                bodyDict.Add("service_label", service_label);
            }
            if (!string.IsNullOrEmpty(service_name))
            {
                bodyDict.Add("service_name", service_name);
            }
            if (environment != -1)
            {
                bodyDict.Add("environment", environment);
            }
            if (use_count != -1)
            {
                bodyDict.Add("use_count", use_count);
            }
            var body = LootLockerJson.SerializeObject(bodyDict);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.redeemPlayStationStorePurchase.endPoint, LootLockerEndPoints.redeemPlayStationStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Redeem a purchase that was made successfully towards the Playstation Store for a class that the current player owns
        /// </summary>
        /// <param name="transaction_id">The transaction id from the PlayStation Store of the purchase to redeem </param>
        /// <param name="auth_code">The authorization code from the PlayStation Store of the purchase to redeem </param>
        /// <param name="entitlement_label">The entitlement label configured in the NP service for the entitlement that this redemption relates to </param>
        /// <param name="classId">The id of the class to redeem this purchase for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="service_label">Optional: The NP service label. </param>
        /// <param name="service_name">Optional: The abreviation of the service name of the ASM service ID service that was used when configuring the serviceIds. Possible Values: pssdc, cce. Default Value: pssdc </param>
        /// <param name="environment">Optional: The id of the environment you wish to make the request against. Allowed values: 1, 8, 256 </param>
        /// <param name="use_count">Optional: The use count for this redemption </param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void RedeemPlaystationStorePurchaseForClass(string transaction_id, string auth_code, string entitlement_label, int classId, Action<LootLockerResponse> onComplete, string service_label = "", string service_name = "", int environment = -1, int use_count = -1, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            
            Dictionary<string, object> bodyDict = new Dictionary<string, object>
            {
                { "transaction_id", transaction_id },
                { "auth_code", auth_code },
                { "entitlement_label", entitlement_label },
                { "character_id", classId }
            };
            if (!string.IsNullOrEmpty(service_label))
            {
                bodyDict.Add("service_label", service_label);
            }
            if (!string.IsNullOrEmpty(service_name))
            {
                bodyDict.Add("service_name", service_name);
            }
            if (environment != -1)
            {
                bodyDict.Add("environment", environment);
            }
            if (use_count != -1)
            {
                bodyDict.Add("use_count", use_count);
            }
            var body = LootLockerJson.SerializeObject(bodyDict);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.redeemPlayStationStorePurchase.endPoint, LootLockerEndPoints.redeemPlayStationStorePurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
#endif

        /// <summary>
        /// Begin a Steam purchase with the given settings that when finalized will redeem the specified catalog item
        /// 
        /// Steam in-app purchases need to be configured for this to work
        /// Steam in-app purchases works slightly different from other platforms, you begin a purchase with this call which initiates it in Steams backend
        /// While your app is waiting for the user to finalize that purchase you can use QuerySteamPurchaseRedemptionStatus to get the status, when that tells you that the purchase is Approved you can finalize the purchase using FinalizeSteamPurchaseRedemption
        /// </summary>
        /// <param name="steamId">Id of the Steam User that is making the purchase</param>
        /// <param name="currency">The currency to use for the purchase</param>
        /// <param name="language">The language to use for the purchase</param>
        /// <param name="catalogItemId">The LootLocker Catalog Item Id for the item you wish to purchase</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void BeginSteamPurchaseRedemption(string steamId, string currency, string language, string catalogItemId, Action<LootLockerBeginSteamPurchaseRedemptionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerBeginSteamPurchaseRedemptionResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerBeginSteamPurchaseRedemptionRequest()
            {
                steam_id = steamId,
                currency = currency,
                language = language,
                catalog_item_id = catalogItemId
            });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.beginSteamPurchaseRedemption.endPoint, LootLockerEndPoints.beginSteamPurchaseRedemption.httpMethod, body, onComplete: (serverResponse) =>
            {
                var parsedResponse = LootLockerResponse.Deserialize<LootLockerBeginSteamPurchaseRedemptionResponse>(serverResponse);
                if (!parsedResponse.success)
                {
                    onComplete?.Invoke(parsedResponse);
                    return;
                }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
                    JObject jsonObject;
                    try
                    {
                        jsonObject = JObject.Parse(serverResponse.text);
                    }
                    catch (JsonReaderException)
                    {
                        onComplete?.Invoke(parsedResponse);
                        return;
                    }
                    if (jsonObject != null && jsonObject.TryGetValue("success", StringComparison.OrdinalIgnoreCase, out var successObj))
                    {
                        if (successObj.ToObject(typeof(bool)) is bool isSuccess)
                        {
                            parsedResponse.isSuccess = isSuccess;
                        }
                    }
#else
                Dictionary<string, object> jsonObject = null;
                try
                {
                    jsonObject = Json.Deserialize(serverResponse.text) as Dictionary<string, object>;
                }
                catch (JsonException)
                {
                    onComplete?.Invoke(parsedResponse);
                    return;
                }
                if (jsonObject != null && jsonObject.TryGetValue("success", out var successObj))
                {
                    if (successObj is bool isSuccess)
                    {
                        parsedResponse.isSuccess = isSuccess;
                    }
                }
#endif
                onComplete?.Invoke(parsedResponse);
            });
        }

        /// <summary>
        /// Begin a Steam purchase with the given settings that when finalized will redeem the specified catalog item for the specified class
        /// 
        /// Steam in-app purchases need to be configured for this to work
        /// Steam in-app purchases works slightly different from other platforms, you begin a purchase with this call which initiates it in Steams backend
        /// While your app is waiting for the user to finalize that purchase you can use QuerySteamPurchaseRedemptionStatus to get the status, when that tells you that the purchase is Approved you can finalize the purchase using FinalizeSteamPurchaseRedemption
        /// </summary>
        /// <param name="classId">Id of the class to make the purchase for</param>
        /// <param name="steamId">Id of the Steam User that is making the purchase</param>
        /// <param name="currency">The currency to use for the purchase</param>
        /// <param name="language">The language to use for the purchase</param>
        /// <param name="catalogItemId">The LootLocker Catalog Item Id for the item you wish to purchase</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void BeginSteamPurchaseRedemptionForClass(int classId, string steamId, string currency, string language, string catalogItemId, Action<LootLockerBeginSteamPurchaseRedemptionResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerBeginSteamPurchaseRedemptionResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerBeginSteamPurchaseRedemptionForClassRequest()
            {
                class_id = classId,
                steam_id = steamId,
                currency = currency,
                language = language,
                catalog_item_id = catalogItemId
            });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.beginSteamPurchaseRedemption.endPoint, LootLockerEndPoints.beginSteamPurchaseRedemption.httpMethod, body, onComplete: (serverResponse) =>
            {
                var parsedResponse = LootLockerResponse.Deserialize<LootLockerBeginSteamPurchaseRedemptionResponse>(serverResponse);
                if (!parsedResponse.success)
                {
                    onComplete?.Invoke(parsedResponse);
                    return;
                }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
                    JObject jsonObject;
                    try
                    {
                        jsonObject = JObject.Parse(serverResponse.text);
                    }
                    catch (JsonReaderException)
                    {
                        onComplete?.Invoke(parsedResponse);
                        return;
                    }
                    if (jsonObject != null && jsonObject.TryGetValue("success", StringComparison.OrdinalIgnoreCase, out var successObj))
                    {
                        if (successObj.ToObject(typeof(bool)) is bool isSuccess)
                        {
                            parsedResponse.isSuccess = isSuccess;
                        }
                    }
#else
                Dictionary<string, object> jsonObject = null;
                try
                {
                    jsonObject = Json.Deserialize(serverResponse.text) as Dictionary<string, object>;
                }
                catch (JsonException)
                {
                    onComplete?.Invoke(parsedResponse);
                    return;
                }
                if (jsonObject != null && jsonObject.TryGetValue("success", out var successObj))
                {
                    if (successObj is bool isSuccess)
                    {
                        parsedResponse.isSuccess = isSuccess;
                    }
                }
#endif
                onComplete?.Invoke(parsedResponse);
            });
        }

        /// <summary>
        /// Check the Steam Purchase status for a given entitlement
        /// 
        /// Use this to check the status of an ongoing purchase to know when it's ready to finalize or has been aborted
        /// or use this to get information for a completed purchase
        /// </summary>
        /// <param name="entitlementId">The id of the entitlement to check the status for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void QuerySteamPurchaseRedemption(string entitlementId, Action<LootLockerQuerySteamPurchaseRedemptionStatusResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerQuerySteamPurchaseRedemptionStatusResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerQuerySteamPurchaseRedemptionStatusRequest()
            {
                entitlement_id = entitlementId
            });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.querySteamPurchaseRedemptionStatus.endPoint, LootLockerEndPoints.querySteamPurchaseRedemptionStatus.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Finalize a started Steam Purchase and subsequently redeem the catalog items that the entitlement refers to
        /// 
        /// The steam purchase needs to be in status Approved for this call to work
        /// </summary>
        /// <param name="entitlementId">The id of the entitlement to finalize the purchase for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void FinalizeSteamPurchaseRedemption(string entitlementId, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            var body = LootLockerJson.SerializeObject(new LootLockerFinalizeSteamPurchaseRedemptionRequest()
            {
                entitlement_id = entitlementId
            });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.finalizeSteamPurchaseRedemption.endPoint, LootLockerEndPoints.finalizeSteamPurchaseRedemption.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

#endregion

        #region Collectables
        /// <summary>
        /// Get all collectables for the game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGettingCollectablesResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCollectables(Action<LootLockerGetCollectablesResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetCollectablesResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetCollectables(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Collect a collectable item. This will grant the collectable to the player.
        /// </summary>
        /// <param name="slug">A string representing what was collected, example; Carsdriven.Bugs.Dune</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerCollectingAnItemResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CollectItem(string slug, Action<LootLockerCollectItemResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCollectItemResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerCollectingAnItemRequest data = new LootLockerCollectingAnItemRequest();
            data.slug = slug;
            LootLockerAPIManager.CollectItem(forPlayerWithUlid, data, onComplete);
        }

        #endregion

        #region Messages

        /// <summary>
        /// Get the current messages.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetMessagesResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetMessages(Action<LootLockerGetMessagesResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMessagesResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.GetMessages(forPlayerWithUlid, onComplete);
        }

        #endregion

        #region Triggers

        /// <summary>
        /// Invoke a set of triggers by key
        ///
        /// Note that the response contains two lists:
        /// - One listing the keys of the triggers that were successfully executed
        /// - One listing the triggers that failed as well as the reason they did so
        ///
        /// This means that the request can "succeed" but still contain triggers that failed. So make sure to check the inner results.
        /// </summary>
        /// <param name="KeysToInvoke">List of keys of the triggers to invoke</param>
        /// <param name="onComplete">onComplete Action for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void InvokeTriggersByKey(string[] KeysToInvoke, Action<LootLockerInvokeTriggersByKeyResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerInvokeTriggersByKeyResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.InvokeTriggers.endPoint, LootLockerEndPoints.InvokeTriggers.httpMethod, LootLockerJson.SerializeObject(new LootLockerInvokeTriggersByKeyRequest { Keys = KeysToInvoke }), onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        #endregion

        #region Leaderboard
        /// <summary>
        /// List leaderboards with details on each leaderboard
        /// </summary>
        /// <param name="count">How many leaderboards to get in one request</param>
        /// <param name="after">Return leaderboards after this specified index</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetMemberRankResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListLeaderboards(int count, int after, Action<LootLockerListLeaderboardsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListLeaderboardsResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.listLeaderboards.endPoint;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("count", count);
            if (after > 0)
                queryParams.Add("after", after);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.listLeaderboards.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get the current ranking for a specific player on a leaderboard.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard as a string</param>
        /// <param name="member_id">ID of the player as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetMemberRankResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetMemberRank(string leaderboardKey, string member_id, Action<LootLockerGetMemberRankResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMemberRankResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetMemberRankRequest lootLockerGetMemberRankRequest = new LootLockerGetMemberRankRequest();

            lootLockerGetMemberRankRequest.leaderboardId = leaderboardKey;
            lootLockerGetMemberRankRequest.member_id = member_id;

            LootLockerAPIManager.GetMemberRank(forPlayerWithUlid, lootLockerGetMemberRankRequest, onComplete);
        }

        /// <summary>
        /// Get the current ranking for several members on a specific leaderboard.
        /// </summary>
        /// <param name="members">List of members to get as string</param>
        /// <param name="leaderboardKey">Key of the leaderboard as a string</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetByListOfMembersResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetByListOfMembers(string[] members, string leaderboardKey, Action<LootLockerGetByListOfMembersResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetByListOfMembersResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetByListMembersRequest request = new LootLockerGetByListMembersRequest();

            request.members = members;

            LootLockerAPIManager.GetByListOfMembers(forPlayerWithUlid, request, leaderboardKey, onComplete);
        }

        /// <summary>
        /// Get the current ranking for a member on all available leaderboards.
        /// </summary>
        /// <param name="member_id">The ID of the player to check</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="after">How many extra rows after the returned position</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllMemberRanksResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllMemberRanksMain(int member_id, int count, int after, Action<LootLockerGetAllMemberRanksResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetAllMemberRanksResponse>(forPlayerWithUlid));
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
            LootLockerAPIManager.GetAllMemberRanks(forPlayerWithUlid, request, callback);
        }

        /// <summary>
        /// Get the current ranking for a member on all available leaderboards.
        /// </summary>
        /// <param name="member_id">The ID of the player to check</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllMemberRanksResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllMemberRanks(int member_id, int count, Action<LootLockerGetAllMemberRanksResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetAllMemberRanksMain(member_id, count, -1, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get the next current rankings for a member on all available leaderboards.
        /// </summary>
        /// <param name="member_id">The ID of the player to check</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllMemberRanksResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllMemberRanksNext(int member_id, int count, Action<LootLockerGetAllMemberRanksResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetAllMemberRanksMain(member_id, count, int.Parse(LootLockerGetAllMemberRanksRequest.nextCursor.ToString()), onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get the previous ranking for a member on all available leaderboards.
        /// </summary>
        /// <param name="member_id">The ID of the player to check</param>
        /// <param name="count">Amount of entries to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetAllMemberRanksResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllMemberRanksPrev(int member_id, int count, Action<LootLockerGetAllMemberRanksResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetAllMemberRanksMain(member_id, count, int.Parse(LootLockerGetAllMemberRanksRequest.prevCursor.ToString()), onComplete, forPlayerWithUlid);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetAllMemberRanksOriginal(int member_id, int count, int after, Action<LootLockerGetAllMemberRanksResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetAllMemberRanksResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerGetAllMemberRanksRequest request = new LootLockerGetAllMemberRanksRequest();
            request.member_id = member_id;
            request.count = count;
            request.after = after > 0 ? after.ToString() : null;

            LootLockerAPIManager.GetAllMemberRanks(forPlayerWithUlid, request, onComplete);
        }

        /// <summary>
        /// Get the entries for a specific leaderboard.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="after">How many after the last entry to receive</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetScoreList(string leaderboardKey, int count, int after, Action<LootLockerGetScoreListResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetScoreListResponse>(forPlayerWithUlid));
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
            LootLockerAPIManager.GetScoreList(forPlayerWithUlid, request, callback);
        }

        /// <summary>
        /// Get the entries for a specific leaderboard.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetScoreList(string leaderboardKey, int count, Action<LootLockerGetScoreListResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetScoreList(leaderboardKey, count, -1, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get the next entries for a specific leaderboard. Can be called after GetScoreList.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetNextScoreList(string leaderboardKey, int count, Action<LootLockerGetScoreListResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetScoreList(leaderboardKey, count, int.Parse(LootLockerGetScoreListRequest.nextCursor.ToString()), onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get the previous entries for a specific leaderboard. Can be called after GetScoreList or GetNextScoreList.
        /// </summary>
        /// <param name="leaderboardKey">Key of the leaderboard to get entries for</param>
        /// <param name="count">How many entries to get</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerGetScoreListResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetPrevScoreList(string leaderboardKey, int count, Action<LootLockerGetScoreListResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetScoreList(leaderboardKey, count, int.Parse(LootLockerGetScoreListRequest.prevCursor.ToString()), onComplete, forPlayerWithUlid);
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SubmitScore(string memberId, int score, string leaderboardKey, Action<LootLockerSubmitScoreResponse> onComplete, string forPlayerWithUlid = null)
        {
            SubmitScore(memberId, score, leaderboardKey, "", onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Submit a score to a leaderboard with additional metadata.
        /// </summary>
        /// <param name="memberId">Can be left blank if it is a player leaderboard, otherwise an identifier for the player</param>
        /// <param name="score">The score to upload</param>
        /// <param name="leaderboardKey">Key of the leaderboard to submit score to</param>
        /// <param name="metadata">Additional metadata to add to the score</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSubmitScoreResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SubmitScore(string memberId, int score, string leaderboardKey, string metadata, Action<LootLockerSubmitScoreResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSubmitScoreResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerSubmitScoreRequest request = new LootLockerSubmitScoreRequest();
            request.member_id = memberId;
            request.score = score;
            if (!string.IsNullOrEmpty(metadata))
                request.metadata = metadata;

            LootLockerAPIManager.SubmitScore(forPlayerWithUlid, request, leaderboardKey, onComplete);
        }

        /// <summary>
        /// Query a leaderboard for which rank a specific score would achieve. Does not submit the score but returns the projected rank.
        /// </summary>
        /// <param name="score">The score to use for the query</param>
        /// <param name="leaderboardKey">Key of the leaderboard to submit score to</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSubmitScoreResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void QueryScore(int score, string leaderboardKey, Action<LootLockerSubmitScoreResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSubmitScoreResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerQueryScoreRequest request = new LootLockerQueryScoreRequest();
            request.score = score;

            EndPointClass requestEndPoint = LootLockerEndPoints.queryScore;

            string json = LootLockerJson.SerializeObject(request);

            string endPoint = requestEndPoint.WithPathParameter(leaderboardKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Increment an existing score on a leaderboard by the given amount.
        /// </summary>
        /// <param name="memberId">Can be left blank if it is a player leaderboard, otherwise this is the identifier you wish to use for this score</param>
        /// <param name="amount">The amount with which to increment the current score on the given leaderboard (can be positive or negative)</param>
        /// <param name="leaderboardKey">Key of the leaderboard to submit score to</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerSubmitScoreResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void IncrementScore(string memberId, int amount, string leaderboardKey, Action<LootLockerSubmitScoreResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSubmitScoreResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerIncrementScoreRequest request = new LootLockerIncrementScoreRequest();
            request.member_id = memberId;
            request.amount = amount;

            EndPointClass requestEndPoint = LootLockerEndPoints.incrementScore;

            string json = LootLockerJson.SerializeObject(request);

            string endPoint = requestEndPoint.WithPathParameter(leaderboardKey);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// List the archived versions of a leaderboard, containing past rewards, ranks, etc.
        /// </summary>
        /// <param name="leaderboard_key">Key of the Leaderboard</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerLeaderboardArchiveResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListLeaderboardArchive(string leaderboard_key, Action<LootLockerLeaderboardArchiveResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerLeaderboardArchiveResponse>(forPlayerWithUlid));
                return;
            }

            EndPointClass endPoint = LootLockerEndPoints.listLeaderboardArchive;
            string tempEndpoint = endPoint.WithPathParameter(leaderboard_key);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, tempEndpoint, endPoint.httpMethod, null, ((serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }));
        }

        /// <summary>
        /// Get the details of a Leaderboard Archive, containing past rewards, ranks, etc
        /// </summary>
        /// <param name="key"> Key of the json archive to read</param>
        /// <param name="onComplete"><onComplete Action for handling the response of type LootLockerLeaderboardArchiveDetailsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetLeaderboardArchive(string key, Action<LootLockerLeaderboardArchiveDetailsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetLeaderboardArchive(key, -1, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get the details of a Leaderboard Archive, containing past rewards, ranks, etc
        /// </summary>
        /// <param name="key"> Key of the json archive to read</param>
        /// <param name="count"> Amount of entries to read </param>
        /// <param name="onComplete"><onComplete Action for handling the response of type LootLockerLeaderboardArchiveDetailsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetLeaderboardArchive(string key, int count, Action<LootLockerLeaderboardArchiveDetailsResponse> onComplete, string forPlayerWithUlid = null)
        {
            GetLeaderboardArchive(key, count, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get the details of a Leaderboard Archive, containing past rewards, ranks, etc
        /// </summary>
        /// <param name="key"> Key of the json archive to read</param>
        /// <param name="count"> Amount of entries to read </param>
        /// <param name="after"> Return after specified index </param>
        /// <param name="onComplete"><onComplete Action for handling the response of type LootLockerLeaderboardArchiveDetailsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetLeaderboardArchive(string key, int count, string after, Action<LootLockerLeaderboardArchiveDetailsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerLeaderboardArchiveDetailsResponse>(forPlayerWithUlid));
                return;
            }
            
            EndPointClass endPoint = LootLockerEndPoints.getLeaderboardArchive;

            string tempEndpoint = endPoint.WithPathParameter(key);

            if (count > 0)
                tempEndpoint += $"count={count}&";

            if (!string.IsNullOrEmpty(after))
                tempEndpoint += $"after={after}&";


            LootLockerServerRequest.CallAPI(forPlayerWithUlid, tempEndpoint, endPoint.httpMethod, null, ((serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }));        
        }

        /// <summary>
        /// Get data on a leaderboard, check rewards and when it will reset and the last reset time.
        /// </summary>
        /// <param name="leaderboard_key">Key of the leaderboard to get data from</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerLeaderboardDetailResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetLeaderboardData(string leaderboard_key, Action<LootLockerLeaderboardDetailResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerLeaderboardDetailResponse>(forPlayerWithUlid));
                return;
            }

            EndPointClass endPoint = LootLockerEndPoints.getLeaderboardData;
            string formatedEndPoint = endPoint.WithPathParameter(leaderboard_key);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formatedEndPoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region Drop Tables

        /// <summary>
        /// Lock a drop table and return information about the assets that were computed.
        /// </summary>
        /// <param name="tableInstanceId">Asset instance ID of the drop table to compute</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerComputeAndLockDropTableResponse</param>
        /// <param name="AddAssetDetails">Optional:If true, return additional information about the asset</param>
        /// <param name="tag">Optional:Specific tag to use</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ComputeAndLockDropTable(int tableInstanceId, Action<LootLockerComputeAndLockDropTableResponse> onComplete, bool AddAssetDetails = false, string tag = "", string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerComputeAndLockDropTableResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.ComputeAndLockDropTable(forPlayerWithUlid, tableInstanceId, onComplete, AddAssetDetails, tag);
        }

        /// <summary>
        /// Lock a drop table and return information about the assets that were computed.
        /// </summary>
        /// <param name="tableInstanceId">Asset instance ID of the drop table to compute</param>
        /// <param name="AddAssetDetails">If true, return additional information about the asset</param>
        /// <param name="tag">Specific tag to use</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerComputeAndLockDropTableResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ComputeAndLockDropTable(int tableInstanceId, bool AddAssetDetails, string tag, Action<LootLockerComputeAndLockDropTableResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerComputeAndLockDropTableResponse>(forPlayerWithUlid));
                return;
            }
            LootLockerAPIManager.ComputeAndLockDropTable(forPlayerWithUlid, tableInstanceId, onComplete, AddAssetDetails, tag);
        }

        /// <summary>
        /// Send a list of id's from a ComputeAndLockDropTable()-call to grant the assets to the player.
        /// </summary>
        /// <param name="picks">A list of the ID's of the picks to choose</param>
        /// <param name="tableInstanceId">Asset instance ID of the drop table to pick from</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPickDropsFromDropTableResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void PickDropsFromDropTable(int[] picks, int tableInstanceId, Action<LootLockerPickDropsFromDropTableResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPickDropsFromDropTableResponse>(forPlayerWithUlid));
                return;
            }
            PickDropsFromDropTableRequest data = new PickDropsFromDropTableRequest();
            data.picks = picks;

            LootLockerAPIManager.PickDropsFromDropTable(forPlayerWithUlid, data, tableInstanceId, onComplete);
        }
#endregion //Drop Tables

        #region Reports

        /// <summary>
        /// Retrieves the different types of report possible.
        /// These can be changed in the web interface or through the Admin API.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerReportsGetTypesResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetReportTypes(Action<LootLockerReportsGetTypesResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReportsGetTypesResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.GetReportTypes(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        /// Create a report of a player.
        /// </summary>
        /// <param name="input">The report to upload</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerReportsCreatePlayerResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CreatePlayerReport(ReportsCreatePlayerRequest input, Action<LootLockerReportsCreatePlayerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReportsCreatePlayerResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.CreatePlayerReport(forPlayerWithUlid, input, onComplete);
        }

        /// <summary>
        /// Create a report of an asset.
        /// </summary>
        /// <param name="input">The report to upload</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerReportsCreateAssetResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CreateAssetReport(ReportsCreateAssetRequest input, Action<LootLockerReportsCreateAssetResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReportsCreateAssetResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.CreateAssetReport(forPlayerWithUlid, input, onComplete);
        }

        /// <summary>
        /// Get removed UGC for the current player. 
        /// If any of their UGC has been removed as a result of reports they will be returned in this method.
        /// </summary>
        /// <param name="input">The report to upload</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerReportsGetRemovedAssetsResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetRemovedUGCForPlayer(GetRemovedUGCForPlayerInput input, Action<LootLockerReportsGetRemovedAssetsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReportsGetRemovedAssetsResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.GetRemovedUGCForPlayer(forPlayerWithUlid, input, onComplete);
        }

        #endregion

        #region Feedback

        /// <summary>
        /// Returns a list of categories to be used for giving feedback about a certain player.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type ListLootLockerFeedbackCategoryResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListPlayerFeedbackCategories(Action<ListLootLockerFeedbackCategoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListFeedbackCategories(LootLockerFeedbackTypes.player, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns a list of categories to be used for giving feedback about the game.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type ListLootLockerFeedbackCategoryResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListGameFeedbackCategories(Action<ListLootLockerFeedbackCategoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListFeedbackCategories(LootLockerFeedbackTypes.game, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Returns a list of categories to be used for giving feedback about a certain ugc asset.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type ListLootLockerFeedbackCategoryResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListUGCFeedbackCategories(Action<ListLootLockerFeedbackCategoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListFeedbackCategories(LootLockerFeedbackTypes.ugc, onComplete, forPlayerWithUlid);
        }

        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        private static void ListFeedbackCategories(LootLockerFeedbackTypes type, Action<ListLootLockerFeedbackCategoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<ListLootLockerFeedbackCategoryResponse>(forPlayerWithUlid));
                return;
            }

            EndPointClass endPoint = LootLockerEndPoints.listFeedbackCategories;

            var formattedEndPoint = endPoint.WithPathParameter(type.ToString());

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, endPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Sends a feedback with the given data, will return 204 upon successful request.
        /// </summary>
        /// <param name="ulid">Ulid of who you're giving feedback about</param>
        /// <param name="description">Reason behind the report</param>
        /// <param name="category_id">A unique identifier of what catagory the report should belong under</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SendPlayerFeedback(string ulid, string description, string category_id, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            SendFeedback(LootLockerFeedbackTypes.player, ulid, description, category_id, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Sends a feedback with the given data, will return 204 upon successful request.
        /// </summary>
        /// <param name="description">Reason behind the report</param>
        /// <param name="category_id">A unique identifier of what catagory the report should belong under</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SendGameFeedback(string description, string category_id, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            SendFeedback(LootLockerFeedbackTypes.game, "", description, category_id, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Sends a feedback with the given data, will return 204 upon successful request.
        /// </summary>
        /// <param name="ulid">Ulid of which asset you're giving feedback about</param>
        /// <param name="description">Reason behind the report</param>
        /// <param name="category_id">A unique identifier of what catagory the report should belong under</param>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SendUGCFeedback(string ulid, string description, string category_id, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            SendFeedback(LootLockerFeedbackTypes.ugc, ulid, description, category_id, onComplete, forPlayerWithUlid);
        }

        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        private static void SendFeedback(LootLockerFeedbackTypes type, string ulid, string description, string category_id, Action<LootLockerResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }
            EndPointClass endPoint = LootLockerEndPoints.createFeedbackEntry;

            var request = new LootLockerFeedbackRequest
            {
                entity = type,
                entity_id = ulid,
                description = description,
                category_id = category_id
            };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region Friends
        /// <summary>
        /// List friends for the currently logged in player with default pagination
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFriends(Action<LootLockerListFriendsResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListFriendsPaginated(0, 0, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List friends for the currently logged in player
        /// </summary>
        /// <param name="PerPage">The number of results to return per page</param>
        /// <param name="Page">The page number to return</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFriendsPaginated(int PerPage, int Page, Action<LootLockerListFriendsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListFriendsResponse>(forPlayerWithUlid));
                return;
            }

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (Page > 0)
                queryParams.Add("page", Page.ToString());
            if (PerPage > 0)
                queryParams.Add("per_page", PerPage.ToString());

            string endpointWithParams = LootLockerEndPoints.listFriends.endPoint + queryParams.ToString();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpointWithParams, LootLockerEndPoints.listFriends.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get a specific friend of the currently logged in player
        /// </summary>
        /// <param name="friendPlayerULID">The ULID of the player for whom to get friend information</param>
        /// <param name="onComplete">Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional: Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetFriend(string friendPlayerULID, Action<LootLockerGetFriendResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetFriendResponse>(forPlayerWithUlid));
                return;
            }

            var formattedEndPoint = LootLockerEndPoints.getFriend.WithPathParameter(friendPlayerULID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.getFriend.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// List incoming friend requests for the currently logged in player (friend requests made by others for this player) with default pagination
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListIncomingFriendRequests(Action<LootLockerListIncomingFriendRequestsResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListIncomingFriendRequestsPaginated(0, 0, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List incoming friend requests for the currently logged in player with pagination (friend requests made by others for this player)
        /// </summary>
        /// <param name="PerPage">The number of results to return per page</param>
        /// <param name="Page">The page number to retrieve</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>

        public static void ListIncomingFriendRequestsPaginated(int PerPage, int Page, Action<LootLockerListIncomingFriendRequestsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListIncomingFriendRequestsResponse>(forPlayerWithUlid));
                return;
            }

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (Page > 0)
                queryParams.Add("page", Page.ToString());
            if (PerPage > 0)
                queryParams.Add("per_page", PerPage.ToString());

            string endpointWithParams = LootLockerEndPoints.listIncomingFriendReqeusts.endPoint + queryParams.ToString();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpointWithParams, LootLockerEndPoints.listIncomingFriendReqeusts.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// List outgoing friend requests for the currently logged in player (friend requests made by this player)
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListOutgoingFriendRequests(Action<LootLockerListOutgoingFriendRequestsResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListOutGoingFriendRequestsPaginated(0, 0, onComplete, forPlayerWithUlid);
        }


        /// <summary>
        /// List outgoing friend requests for the currently logged in player with pagination (friend requests made by this player)
        /// </summary>
        /// <param name="PerPage">The number of results to return per page</param>
        /// <param name="Page">The page number to retrieve</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListOutGoingFriendRequestsPaginated(int PerPage, int Page, Action<LootLockerListOutgoingFriendRequestsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListOutgoingFriendRequestsResponse>(forPlayerWithUlid));
                return;
            }

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (Page > 0)
                queryParams.Add("page", Page.ToString());
            if (PerPage > 0)
                queryParams.Add("per_page", PerPage.ToString());

            string endpointWithParams = LootLockerEndPoints.listOutgoingFriendRequests.endPoint + queryParams.ToString();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpointWithParams, LootLockerEndPoints.listOutgoingFriendRequests.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Send a friend request to the specified player for the currently logged in player
        /// </summary>
        /// <param name="playerID">The id of the player to send the friend request to</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void SendFriendRequest(string playerID, Action<LootLockerFriendsOperationResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFriendsOperationResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(playerID))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerFriendsOperationResponse>("A player id needs to be provided for this method", forPlayerWithUlid));
            }

            var formattedEndPoint = LootLockerEndPoints.sendFriendRequest.WithPathParameter(playerID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.sendFriendRequest.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Cancel the outgoing friend request made to the specified player by the currently logged in player
        /// </summary>
        /// <param name="playerID">The id of the player to cancel the friend request for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CancelFriendRequest(string playerID, Action<LootLockerFriendsOperationResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFriendsOperationResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(playerID))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerFriendsOperationResponse>("A player id needs to be provided for this method", forPlayerWithUlid));
            }

            var formattedEndPoint = LootLockerEndPoints.cancelOutgoingFriendRequest.WithPathParameter(playerID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.cancelOutgoingFriendRequest.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Accept the incoming friend request from the specified player
        /// </summary>
        /// <param name="playerID">The id of the player that sent the friend request you wish to accept</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void AcceptFriendRequest(string playerID, Action<LootLockerFriendsOperationResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFriendsOperationResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(playerID))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerFriendsOperationResponse>("A player id needs to be provided for this method", forPlayerWithUlid));
            }

            var formattedEndPoint = LootLockerEndPoints.acceptFriendRequest.WithPathParameter(playerID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.acceptFriendRequest.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Decline the incoming friend request from the specified player
        /// </summary>
        /// <param name="playerID">The id of the player that sent the friend request you wish to decline</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeclineFriendRequest(string playerID, Action<LootLockerFriendsOperationResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFriendsOperationResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(playerID))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerFriendsOperationResponse>("A player id needs to be provided for this method", forPlayerWithUlid));
            }

            var formattedEndPoint = LootLockerEndPoints.declineFriendRequest.WithPathParameter(playerID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.declineFriendRequest.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// List the players (if any) that are blocked by the currently logged in player with default pagination
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListBlockedPlayers(Action<LootLockerListBlockedPlayersResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListBlockedPlayersPaginated(0, 0, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List the players (if any) that are blocked by the currently logged in player
        /// </summary>
        /// <param name="PerPage">The number of results to return per page</param>
        /// <param name="Page">The page number to retrieve</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListBlockedPlayersPaginated(int PerPage, int Page, Action<LootLockerListBlockedPlayersResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListBlockedPlayersResponse>(forPlayerWithUlid));
                return;
            }

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (Page > 0)
                queryParams.Add("page", Page.ToString());
            if (PerPage > 0)
                queryParams.Add("per_page", PerPage.ToString());

            string endpointWithParams = LootLockerEndPoints.listOutgoingFriendRequests.endPoint + queryParams.ToString();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpointWithParams, LootLockerEndPoints.listBlockedPlayers.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Block the specified player (adding them to the currently logged in players block list and removing them the friend list)
        /// </summary>
        /// <param name="playerID">The id of the player to block</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void BlockPlayer(string playerID, Action<LootLockerFriendsOperationResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFriendsOperationResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(playerID))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerFriendsOperationResponse>("A player id needs to be provided for this method", forPlayerWithUlid));
            }

            var formattedEndPoint = LootLockerEndPoints.blockPlayer.WithPathParameter(playerID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.blockPlayer.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Unblock the specified player (remove from the currently logged in players block list)
        /// </summary>
        /// <param name="playerID">The id of the player to unblock</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UnblockPlayer(string playerID, Action<LootLockerFriendsOperationResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFriendsOperationResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(playerID))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerFriendsOperationResponse>("A player id needs to be provided for this method", forPlayerWithUlid));
            }

            var formattedEndPoint = LootLockerEndPoints.unblockPlayer.WithPathParameter(playerID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.unblockPlayer.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Remove the specified player from the currently logged in player's friends list
        /// </summary>
        /// <param name="playerID">The id of the player to delete from the friends list</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DeleteFriend(string playerID, Action<LootLockerFriendsOperationResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFriendsOperationResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(playerID))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerFriendsOperationResponse>("A player id needs to be provided for this method", forPlayerWithUlid));
            }

            var formattedEndPoint = LootLockerEndPoints.deleteFriend.WithPathParameter(playerID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.deleteFriend.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        #endregion

        #region Followers
        /// <summary>
        /// List followers of the currently logged in player with default pagination
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFollowers(Action<LootLockerListFollowersResponse> onComplete, string forPlayerWithUlid = null)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            ListFollowers(playerData.PublicUID, onComplete, forPlayerWithUlid);
        }
        /// <summary>
        /// List followers of the currently logged in player
        /// </summary>
        /// <param name="Cursor">Used for pagination, if null or empty string it will return the first page. `next_cursor` will be included in the response if there are more pages.</param>
        /// <param name="Count">The number of results to return counting from the cursor</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFollowersPaginated(string Cursor, int Count, Action<LootLockerListFollowersResponse> onComplete, string forPlayerWithUlid = null)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            ListFollowersPaginated(playerData.PublicUID, Cursor, Count, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List followers that the specified player has with default pagination
        /// </summary>
        /// <param name="playerPublicUID">The public UID of the player whose followers to list</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFollowers(string playerPublicUID, Action<LootLockerListFollowersResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListFollowersPaginated(playerPublicUID, null, 0, onComplete, forPlayerWithUlid);
        }
        
        /// <summary>
        /// List followers that the specified player has
        /// </summary>
        /// <param name="playerPublicUID">The public UID of the player whose followers to list</param>
        /// <param name="Cursor">Used for pagination, if null or empty string it will return the first page. `next_cursor` will be included in the response if there are more pages.</param>
        /// <param name="Count">The number of results to return counting from the cursor</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFollowersPaginated(string playerPublicUID, string Cursor, int Count, Action<LootLockerListFollowersResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListFollowersResponse>(forPlayerWithUlid));
                return;
            }

            var formattedEndPoint = LootLockerEndPoints.listFollowers.WithPathParameter(playerPublicUID);

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (!string.IsNullOrEmpty(Cursor))
                queryParams.Add("cursor", Cursor);
            if (Count > 0)
                queryParams.Add("per_page", Count.ToString());
            formattedEndPoint += queryParams.ToString();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.listFollowers.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        
        /// <summary>
        /// List what players the currently logged in player is following with default pagination
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFollowing(Action<LootLockerListFollowingResponse> onComplete, string forPlayerWithUlid = null)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            ListFollowing(playerData.PublicUID, onComplete, forPlayerWithUlid);
        }
        
        /// <summary>
        /// List what players the currently logged in player is following
        /// </summary>
        /// <param name="Cursor">Used for pagination, if null or empty string it will return the first page. `next_cursor` will be included in the response if there are more pages.</param>
        /// <param name="Count">The number of results to return counting from the cursor</
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFollowingPaginated(string Cursor, int Count, Action<LootLockerListFollowingResponse> onComplete, string forPlayerWithUlid = null)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            ListFollowingPaginated(playerData.PublicUID, Cursor, Count, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List players that the specified player is following with default pagination
        /// </summary>
        /// <param name="playerPublicUID">The public UID of the player for which to list following players</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFollowing(string playerPublicUID, Action<LootLockerListFollowingResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListFollowingPaginated(playerPublicUID, null, 0, onComplete, forPlayerWithUlid);
        }
        
        /// <summary>
        /// List players that the specified player is following
        /// </summary>
        /// <param name="playerPublicUID">The public UID of the player for which to list following players</param>
        /// <param name="Cursor">Used for pagination, if null or empty string it will return the first page. `next_cursor` will be included in the response if there are more pages.</param>
        /// <param name="Count">The number of results to return counting from the cursor</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListFollowingPaginated(string playerPublicUID, string Cursor, int Count, Action<LootLockerListFollowingResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListFollowingResponse>(forPlayerWithUlid));
                return;
            }

            var formattedEndPoint = LootLockerEndPoints.listFollowing.WithPathParameter(playerPublicUID);

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (!string.IsNullOrEmpty(Cursor))
                queryParams.Add("cursor", Cursor);
            if (Count > 0)
                queryParams.Add("per_page", Count.ToString());
            formattedEndPoint += queryParams.ToString();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.listFollowing.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Follow the specified player
        /// </summary>
        /// <param name="playerPublicUID">The public uid of the player to follow</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void FollowPlayer(string playerPublicUID, Action<LootLockerFollowersOperationResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFollowersOperationResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(playerPublicUID))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerFollowersOperationResponse>("A player public UID needs to be provided for this method", forPlayerWithUlid));
            }

            var formattedEndPoint = LootLockerEndPoints.followPlayer.WithPathParameter(playerPublicUID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.followPlayer.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Unfollow the specified player
        /// </summary>
        /// <param name="playerPublicUID">The public uid of the player to unfollow</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void UnfollowPlayer(string playerPublicUID, Action<LootLockerFollowersOperationResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerFollowersOperationResponse>(forPlayerWithUlid));
                return;
            }

            if (string.IsNullOrEmpty(playerPublicUID))
            {
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerFollowersOperationResponse>("A player public UID needs to be provided for this method", forPlayerWithUlid));
            }

            var formattedEndPoint = LootLockerEndPoints.unfollowPlayer.WithPathParameter(playerPublicUID);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, formattedEndPoint, LootLockerEndPoints.unfollowPlayer.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        #endregion

        #region Currency
        /// <summary>
        /// Get a list of available currencies for the game
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListCurrencies(Action<LootLockerListCurrenciesResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListCurrenciesResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.listCurrencies.endPoint, LootLockerEndPoints.listCurrencies.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get details about the specified currency
        /// </summary>
        /// <param name="currencyCode">The code of the currency to get details for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCurrencyDetails(string currencyCode, Action<GetLootLockerCurrencyDetailsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<GetLootLockerCurrencyDetailsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getCurrencyDetails.WithPathParameter(currencyCode);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.getCurrencyDetails.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get a list of the denominations available for a specific currency
        /// </summary>
        /// <param name="currencyCode">The code of the currency to fetch denominations for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetCurrencyDenominationsByCode(string currencyCode, Action<LootLockerListDenominationsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListDenominationsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.getCurrencyDenominationsByCode.WithPathParameter(currencyCode);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.getCurrencyDenominationsByCode.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region Balances
        /// <summary>
        /// Get a list of balances in a specified wallet
        /// </summary>
        /// <param name="walletId">Unique ID of the wallet to get balances for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListBalancesInWallet(string walletId, Action<LootLockerListBalancesForWalletResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListBalancesForWalletResponse>(forPlayerWithUlid));
                return;
            }
            var endpoint = LootLockerEndPoints.listBalancesInWallet.WithPathParameter(walletId);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.listBalancesInWallet.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get information about a specified wallet
        /// </summary>
        /// <param name="walletId">Unique ID of the wallet to get information for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetWalletByWalletId(string walletId, Action<LootLockerGetWalletResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetWalletResponse>(forPlayerWithUlid));
                return;
            }
            var endpoint = LootLockerEndPoints.getWalletByWalletId.WithPathParameter(walletId);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.getWalletByWalletId.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Get information about a wallet for a specified holder
        /// </summary>
        /// <param name="holderUlid">ULID of the holder of the wallet you want to get information for</param>
        /// <param name="holderType">The type of the holder to get the wallet for</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetWalletByHolderId(string holderUlid, LootLockerWalletHolderTypes holderType, Action<LootLockerGetWalletResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetWalletResponse>(forPlayerWithUlid));
                return;
            }
            var endpoint = LootLockerEndPoints.getWalletByHolderId.WithPathParameter(holderUlid);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.getWalletByHolderId.httpMethod, onComplete: (serverResponse) =>
                {
                    var parsedResponse = LootLockerResponse.Deserialize<LootLockerGetWalletResponse>(serverResponse);
                    if (!parsedResponse.success && parsedResponse.statusCode == 404)
                    {
                        LootLockerCreateWalletRequest request = new LootLockerCreateWalletRequest()
                        {
                            holder_id = holderUlid,
                            holder_type = holderType.ToString()
                        };
                        LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.createWallet.endPoint,
                            LootLockerEndPoints.createWallet.httpMethod, LootLockerJson.SerializeObject(request),
                            createWalletResponse =>
                            {
                                if (createWalletResponse.success)
                                {
                                    LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint,
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
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void CreditBalanceToWallet(string walletId, string currencyId, string amount, Action<LootLockerCreditWalletResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerCreditWalletResponse>(forPlayerWithUlid));
                return;
            }

            var json = LootLockerJson.SerializeObject(new LootLockerCreditRequest() { amount = amount, currency_id = currencyId, wallet_id = walletId });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.creditBalanceToWallet.endPoint, LootLockerEndPoints.creditBalanceToWallet.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Debit (decrease) the specified amount of the provided currency to the provided wallet
        /// </summary>
        /// <param name="walletId">Unique ID of the wallet to debit the given amount of the given currency from</param>
        /// <param name="currencyId">Unique ID of the currency to debit</param>
        /// <param name="amount">The amount of the given currency to debit from the given wallet</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void DebitBalanceToWallet(string walletId, string currencyId, string amount, Action<LootLockerDebitWalletResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerDebitWalletResponse>(forPlayerWithUlid));
                return;
            }

            var json = LootLockerJson.SerializeObject(new LootLockerDebitRequest() { amount = amount, currency_id = currencyId, wallet_id = walletId });

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.debitBalanceToWallet.endPoint, LootLockerEndPoints.debitBalanceToWallet.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        #endregion

        #region Catalog
        /// <summary>
        /// List the catalogs available for the game
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListCatalogs(Action<LootLockerListCatalogsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListCatalogsResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.listCatalogs.endPoint, LootLockerEndPoints.listCatalogs.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// List the items available in a specific catalog
        /// </summary>
        /// <param name="catalogKey">Unique Key of the catalog that you want to get items for</param>
        /// <param name="count">Amount of catalog items to receive. Use null to simply get the default amount.</param>
        /// <param name="after">Used for pagination, this is the cursor to start getting items from. Use null to get items from the beginning. Use the cursor from a previous call to get the next count of items in the list.</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        [Obsolete("This method is deprecated, please use ListCatalogItems(string catalogKey, int PerPage, int Page, Action<LootLockerListCatalogPricesV2Response> onComplete, string forPlayerWithUlid = null) instead.")] // Deprecation date 20251016
        public static void ListCatalogItems(string catalogKey, int count, string after, Action<LootLockerListCatalogPricesResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListCatalogPricesResponse>(forPlayerWithUlid));
                return;
            }
            var endpoint = LootLockerEndPoints.deprecatedListCatalogItemsByKey.WithPathParameter(catalogKey);

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("per_page", count);
            if (!string.IsNullOrEmpty(after))
                queryParams.Add("cursor", after);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.deprecatedListCatalogItemsByKey.httpMethod, onComplete: (serverResponse) => { onComplete?.Invoke(new LootLockerListCatalogPricesResponse(serverResponse)); });
        }

        /// <summary>
        /// List the items available in a specific catalog
        /// </summary>
        /// <param name="catalogKey">Unique Key of the catalog that you want to get items for</param>
        /// <param name="PerPage">The number of results to return per page</param>
        /// <param name="Page">The page number to retrieve</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListCatalogItems(string catalogKey, int PerPage, int Page, Action<LootLockerListCatalogPricesV2Response> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListCatalogPricesV2Response>(forPlayerWithUlid));
                return;
            }
            var endpoint = LootLockerEndPoints.listCatalogItemsByKey.WithPathParameter(catalogKey);

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (PerPage > 0)
                queryParams.Add("per_page", PerPage);
            if (Page > 0)
                queryParams.Add("page", Page);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.listCatalogItemsByKey.httpMethod, onComplete: (serverResponse) => { onComplete?.Invoke(new LootLockerListCatalogPricesV2Response(serverResponse)); });
        }
        #endregion

        #region Entitlements
        /// <summary>
        /// List this player's historical entitlements
        /// Use this to retrieve information on entitlements the player has received regardless of their origin (for example as an effect of progression, purchases, or leaderboard rewards)
        /// </summary>
        /// <param name="count">Optional: Amount of historical entries to fetch</param>
        /// <param name="after">Optional: Used for pagination, this is the cursor to start getting entries from. Use null or use an overload without the parameter to get entries from the beginning. Use the cursor from a previous call to get the next count of entries in the list.</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListEntitlements(int count, string after, Action<LootLockerEntitlementHistoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerEntitlementHistoryResponse>(forPlayerWithUlid));
                return;
            }

            string endpoint = LootLockerEndPoints.listEntitlementHistory.endPoint;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (count > 0)
                queryParams.Add("per_page", count);
            if (!string.IsNullOrEmpty(after))
                queryParams.Add("cursor", after);

            endpoint += queryParams.Build();

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.listEntitlementHistory.httpMethod, onComplete: (serverResponse) => {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
                LootLockerResponse.Deserialize(onComplete, serverResponse);
#else
                LootLockerResponse.Deserialize(onComplete, serverResponse, new JsonOptions(JsonSerializationOptions.UseXmlIgnore) );
#endif
            });
        }

        /// <summary>
        /// List this player's historical entitlements
        /// Use this to retrieve information on entitlements the player has received regardless of their origin (for example as an effect of progression, purchases, or leaderboard rewards)
        /// </summary>
        /// <param name="count">Optional: Amount of historical entries to fetch</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListEntitlements(int count, Action<LootLockerEntitlementHistoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListEntitlements(count, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List this player's historical entitlements
        /// Use this to retrieve information on entitlements the player has received regardless of their origin (for example as an effect of progression, purchases, or leaderboard rewards)
        /// </summary>
        /// <param name="after">Optional: Used for pagination, this is the cursor to start getting entries from. Use null or use an overload without the parameter to get entries from the beginning. Use the cursor from a previous call to get the next count of entries in the list.</param>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListEntitlements(string after, Action<LootLockerEntitlementHistoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListEntitlements(-1, after, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// List this player's historical entitlements
        /// Use this to retrieve information on entitlements the player has received regardless of their origin (for example as an effect of progression, purchases, or leaderboard rewards)
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListEntitlements(Action<LootLockerEntitlementHistoryResponse> onComplete, string forPlayerWithUlid = null)
        {
            ListEntitlements(-1, null, onComplete, forPlayerWithUlid);
        }

        /// <summary>
        /// Get a single entitlement, with information about its current status
        /// Use this to retrieve information on entitlements the player has received regardless of their origin (for example as an effect of progression, purchases, or leaderboard rewards)
        /// </summary>
        /// <param name="entitlementId"></param>
        /// <param name="onComplete"></param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetEntitlement(string entitlementId, Action<LootLockerSingleEntitlementResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerSingleEntitlementResponse>(forPlayerWithUlid));
                return;
            }
            
            var endpoint = LootLockerEndPoints.getSingleEntitlement.WithPathParameter(entitlementId);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.getSingleEntitlement.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

#endregion

        #region Metadata
        /// <summary>
        /// List Metadata for the specified source with default pagination
        /// </summary>
        /// <param name="Source"> The source type for which to request metadata</param>
        /// <param name="SourceID"> The specific source id for which to request metadata</param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="IgnoreFiles"> Base64 values will be set to content_type "application/x-redacted" and the content will be an empty String. Use this to avoid accidentally fetching large data files.</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListMetadata(LootLockerMetadataSources Source, string SourceID, Action<LootLockerListMetadataResponse> onComplete, bool IgnoreFiles = false, string forPlayerWithUlid = null)
        {
            ListMetadata(Source, SourceID, 0, 0, onComplete, IgnoreFiles, forPlayerWithUlid);
        }

        /// <summary>
        /// List the requested page of Metadata for the specified source with the specified pagination
        /// </summary>
        /// <param name="Source"> The source type for which to request metadata</param>
        /// <param name="SourceID"> The specific source id for which to request metadata</param>
        /// <param name="Page"> Used together with PerPage to apply pagination to this request. Page designates which "page" of items to fetch</param>
        /// <param name="PerPage"> Used together with Page to apply pagination to this request.PerPage designates how many items are considered a "page"</param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="IgnoreFiles"> Base64 values will be set to content_type "application/x-redacted" and the content will be an empty String. Use this to avoid accidentally fetching large data files.</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListMetadata(LootLockerMetadataSources Source, string SourceID, int Page, int PerPage, Action<LootLockerListMetadataResponse> onComplete, bool IgnoreFiles = false, string forPlayerWithUlid = null)
        {
            ListMetadataWithTags(Source, SourceID, null, Page, PerPage, onComplete, IgnoreFiles, forPlayerWithUlid);
        }

        /// <summary>
        /// List Metadata for the specified source that has all of the provided tags, use default pagination
        /// </summary>
        /// <param name="Source"> The source type for which to request metadata</param>
        /// <param name="SourceID"> The specific source id for which to request metadata</param>
        /// <param name="Tags"> The tags that the requested metadata should have, only metadata matching *all of* the given tags will be returned </param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="IgnoreFiles"> Base64 values will be set to content_type "application/x-redacted" and the content will be an empty String. Use this to avoid accidentally fetching large data files.</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListMetadataWithTags(LootLockerMetadataSources Source, string SourceID, string[] Tags, Action<LootLockerListMetadataResponse> onComplete, bool IgnoreFiles = false, string forPlayerWithUlid = null)
        {
            ListMetadataWithTags(Source, SourceID, Tags, 0, 0, onComplete, IgnoreFiles, forPlayerWithUlid);
        }

        /// <summary>
        /// List the requested page of Metadata for the specified source that has all of the provided tags and paginate according to the supplied pagination settings
        /// </summary>
        /// <param name="Source"> The source type for which to request metadata</param>
        /// <param name="SourceID"> The specific source id for which to request metadata</param>
        /// <param name="Tags"> The tags that the requested metadata should have, only metadata matching *all of* the given tags will be returned </param>
        /// <param name="Page"> Used together with PerPage to apply pagination to this request.Page designates which "page" of items to fetch</param>
        /// <param name="PerPage"> Used together with Page to apply pagination to this request.PerPage designates how many items are considered a "page"</param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="IgnoreFiles"> Base64 values will be set to content_type "application/x-redacted" and the content will be an empty String. Use this to avoid accidentally fetching large data files.</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListMetadataWithTags(LootLockerMetadataSources Source, string SourceID, string[] Tags, int Page, int PerPage, Action<LootLockerListMetadataResponse> onComplete, bool IgnoreFiles = false, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListMetadataResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.ListMetadata(forPlayerWithUlid, Source, SourceID, Page, PerPage, null, Tags, IgnoreFiles, onComplete);
        }

        /// <summary>
        /// Get Metadata for the specified source with the given key
        /// </summary>
        /// <param name="Source"> The source type for which to request metadata</param>
        /// <param name="SourceID"> The specific source id for which to request metadata, note that if the source is self then this too should be set to "self"</param>
        /// <param name="Key"> The key of the metadata to fetch, use this to fetch metadata for a specific key for the specified source.</param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="IgnoreFiles"> Optional: Base64 values will be set to content_type "application/x-redacted" and the content will be an empty String. Use this to avoid accidentally fetching large data files.</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetMetadata(LootLockerMetadataSources Source, string SourceID, string Key, Action<LootLockerGetMetadataResponse> onComplete, bool IgnoreFiles=false, string forPlayerWithUlid = null)
        {
            LootLockerAPIManager.ListMetadata(forPlayerWithUlid, Source, SourceID, 0, 0, Key, null, IgnoreFiles, (ListResponse) =>
            {
                onComplete?.Invoke(new LootLockerGetMetadataResponse()
                {
                    success = ListResponse.success,
                    statusCode = ListResponse.statusCode,
                    text = ListResponse.text,
                    EventId = ListResponse.EventId,
                    errorData = ListResponse.errorData,
                    entry = ListResponse.entries != null ? ListResponse.entries.Length > 0 ? ListResponse.entries[0] : null : null
                });
            });
        }

        /// <summary>
        /// List the requested page of Metadata for the specified source that has all of the provided tags and paginate according to the supplied pagination settings
        /// </summary>
        /// <param name="SourcesAndKeysToGet"> The combination of sources to get keys for, and the keys to get for those sources </param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="IgnoreFiles"> Optional: Base64 values will be set to content_type "application/x-redacted" and the content will be an empty String. Use this to avoid accidentally fetching large data files.</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void GetMultisourceMetadata(LootLockerMetadataSourceAndKeys[] SourcesAndKeysToGet, Action<LootLockerGetMultisourceMetadataResponse> onComplete, bool IgnoreFiles = false, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGetMultisourceMetadataResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.GetMultisourceMetadata(forPlayerWithUlid, SourcesAndKeysToGet, IgnoreFiles, onComplete);
        }

        /// <summary>
        /// Perform the specified metadata operations for the specified source
        /// Note that a subset of the specified operations can fail without the full request failing. Make sure to check the errors array in the response.
        /// </summary>
        /// <param name="Source"> The source type that the source id refers to </param>
        /// <param name="SourceID"> The specific source id for which to set metadata, note that if the source is self then this too should be set to "self" </param>
        /// <param name="OperationsToPerform"> List of operations to perform for the given source </param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void PerformMetadataOperations(LootLockerMetadataSources Source, string SourceID, List<LootLockerMetadataOperation> OperationsToPerform, Action<LootLockerMetadataOperationsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerMetadataOperationsResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.PerformMetadataOperations(forPlayerWithUlid, Source, SourceID, OperationsToPerform, onComplete);
        }
        #endregion

        #region Notifications

        /// <summary>
        /// List notifications without filters and with default pagination settings
        /// </summary>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListNotificationsWithDefaultParameters(Action<LootLockerListNotificationsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListNotificationsResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.ListNotifications.endPoint, LootLockerEndPoints.ListNotifications.httpMethod, null,
                (response) =>
                {

                    LootLockerListNotificationsResponse parsedResponse = LootLockerResponse.Deserialize<LootLockerListNotificationsResponse>(response);
                    if (parsedResponse != null && parsedResponse.Notifications != null)
                    {
                        foreach (var notification in parsedResponse.Notifications)
                        {
                            notification.Content.ContextAsDictionary = new Dictionary<string, string>();
                            foreach (var contextEntry in notification.Content.Context)
                            {
                                notification.Content.ContextAsDictionary.Add(contextEntry.Key, contextEntry.Value);
                            }
                        }
                    }
                    onComplete?.Invoke(parsedResponse);
                });
        }

        /// <summary>
        /// List notifications according to specified filters and with pagination settings
        /// </summary>
        /// <param name="ShowRead">When set to true, only read notifications will be returned, when set to false only unread notifications will be returned.</param>
        /// <param name="WithPriority">(Optional) Return only notifications with the specified priority. Set to null to not use this filter.</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <param name="OfType">(Optional) Return only notifications with the specified type. Use static defines in LootLockerStaticStrings.LootLockerNotificationTypes to know what you strings you can use. Set to "" or null to not use this filter.</param, string forPlayerWithUlid = null>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <param name="WithSource">(Optional) Return only notifications with the specified source. Use static defines in LootLockerStaticStrings.LootLockerNotificationSources to know what you strings you can use. Set to "" or null to not use this filter.</param, string forPlayerWithUlid = null>
        /// <param name="PerPage">(Optional) Used together with Page to apply pagination to this request. PerPage designates how many notifications are considered a "page". Set to 0 to not use this filter.</param>
        /// <param name="Page">(Optional) Used together with PerPage to apply pagination to this request. Page designates which "page" of items to fetch. Set to 0 to not use this filter.</param>
        /// <param name="onComplete"></param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListNotifications(bool ShowRead, LootLockerNotificationPriority? WithPriority, string OfType, string WithSource, int PerPage, int Page, Action<LootLockerListNotificationsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListNotificationsResponse>(forPlayerWithUlid));
                return;
            }

            string queryParams = "";
            if(WithPriority != null) { queryParams = $"priority={WithPriority?.ToString()}&"; }
            if (ShowRead) queryParams += $"read=true&"; else queryParams += $"read=false&";
            if (Page > 0) queryParams += $"page={Page}&";
            if (PerPage > 0) queryParams += $"per_page={PerPage}&";
            if (!string.IsNullOrEmpty(OfType)) queryParams += $"notification_type={OfType}&";
            if (!string.IsNullOrEmpty(WithSource)) queryParams += $"source={WithSource}&";

            string endPoint = LootLockerEndPoints.ListNotifications.endPoint;
            if (!string.IsNullOrEmpty(queryParams))
            {
                endPoint = $"{endPoint}?{queryParams}";
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, LootLockerEndPoints.ListNotifications.httpMethod, null,
                (response) =>
                {
                    LootLockerListNotificationsResponse parsedResponse = LootLockerResponse.Deserialize<LootLockerListNotificationsResponse>(response);
                    if (parsedResponse != null && parsedResponse.Notifications != null)
                    {
                        parsedResponse.PopulateConvenienceStructures();
                    }
                    onComplete?.Invoke(parsedResponse);
                });
        }

        /// <summary>
        /// Mark all unread notifications as read
        ///
        /// Warning: This will mark ALL unread notifications as read, so if you have listed notifications but due to filters and/or pagination not pulled all of them you may have unviewed unread notifications
        /// </summary>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void MarkAllNotificationsAsRead(Action<LootLockerReadNotificationsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReadNotificationsResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.ReadAllNotifications.endPoint, LootLockerEndPoints.ReadAllNotifications.httpMethod, null, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// Mark the specified notifications as read
        /// </summary>
        /// <param name="NotificationIds">List of ids of notifications to mark as read</param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void MarkNotificationsAsRead(string[] NotificationIds, Action<LootLockerReadNotificationsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerReadNotificationsResponse>(forPlayerWithUlid));
                return;
            }

            if (NotificationIds == null || NotificationIds.Length == 0)
            {
                onComplete?.Invoke(new LootLockerReadNotificationsResponse(){errorData = null, EventId = "", statusCode = 204, success = true, text = "{}"});
                return;
            }
            
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.ReadNotifications.endPoint, LootLockerEndPoints.ReadNotifications.httpMethod, LootLockerJson.SerializeObject(new LootLockerReadNotificationsRequest{ Notifications = NotificationIds }), (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }
        #endregion

        #region Broadcasts
        /// <summary>
        /// List broadcasts for this game with default localisation and limit
        /// </summary>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListTopBroadcasts(Action<LootLockerListBroadcastsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListBroadcastsResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.ListBroadcasts.endPoint, LootLockerEndPoints.ListBroadcasts.httpMethod, null, (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// List broadcasts for this game with specified localisation and default limit
        /// </summary>
        /// <param name="languages">Array of language codes to filter the broadcasts by. Language codes are typically ISO 639-1 codes (e.g. "en", "fr", "es") with regional variations (e.g. "en-US", "fr-FR"), but can also be custom defined by the game developer.</param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListTopBroadcastsLocalized(string[] languages, Action<LootLockerListBroadcastsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListBroadcastsResponse>(forPlayerWithUlid));
                return;
            }

            string acceptLanguages = "";
            if (languages != null && languages.Length > 0)
            {
                acceptLanguages = string.Join(",", languages);
            }
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(acceptLanguages))
            {
                headers.Add("Accept-Language", acceptLanguages);
            }
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.ListBroadcasts.endPoint, LootLockerEndPoints.ListBroadcasts.httpMethod, null, additionalHeaders: headers, onComplete: (response) => { LootLockerResponse.Deserialize(onComplete, response); });
        }

        /// <summary>
        /// List broadcasts for this game
        /// </summary>
        /// <param name="languages">Array of language codes to filter the broadcasts by. Language codes are typically ISO 639-1 codes (e.g. "en", "fr", "es") with regional variations (e.g. "en-US", "fr-FR"), but can also be custom defined by the game developer.</param>
        /// <param name="limit">Limit the number of broadcasts returned.</param>
        /// <param name="onComplete">Delegate for handling the server response</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void ListBroadcasts(string[] languages, int limit, Action<LootLockerListBroadcastsResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerListBroadcastsResponse>(forPlayerWithUlid));
                return;
            }

            var endpoint = LootLockerEndPoints.ListBroadcasts.endPoint;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (limit > 0)
                queryParams.Add("limit", limit);

            endpoint += queryParams.Build();

            string acceptLanguages = "";
            if (languages != null && languages.Length > 0)
            {
                acceptLanguages = string.Join(",", languages);
            }
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(acceptLanguages))
            {
                headers.Add("Accept-Language", acceptLanguages);
            }
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endpoint, LootLockerEndPoints.ListBroadcasts.httpMethod, null, additionalHeaders: headers, onComplete: (response) => {
                var internalResponse = LootLockerResponse.Deserialize<__LootLockerInternalListBroadcastsResponse>(response);
                onComplete?.Invoke(new LootLockerListBroadcastsResponse(internalResponse));
            });
        }
        #endregion

        #region Misc

        /// <summary>
        /// Ping the server, contains information about the current time of the server.
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response of type LootLockerPingResponse</param>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        public static void Ping(Action<LootLockerPingResponse> onComplete, string forPlayerWithUlid = null)
        {
            if (!CheckInitialized(false, forPlayerWithUlid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerPingResponse>(forPlayerWithUlid));
                return;
            }

            LootLockerAPIManager.Ping(forPlayerWithUlid, onComplete);
        }

        /// <summary>
        ///  Get meta information about the game
        /// </summary>
        /// <param name="onComplete">onComplete Action for handling the response</param>
        public static void GetGameInfo(Action<LootLockerGameInfoResponse> onComplete)
        {
            if (!CheckInitialized(true))
            {
                onComplete?.Invoke(LootLockerResponseFactory.SDKNotInitializedError<LootLockerGameInfoResponse>(""));
                return;
            }

            LootLockerAPIManager.GetGameInfo(onComplete);
        }

        /// <summary>
        /// Get the Platform the user last used. This can be used to know what login method to prompt.
        /// </summary>
        /// <param name="forPlayerWithUlid">Optional : Execute the request for the specified player. If not supplied, the default player will be used.</param>
        /// <returns>The platform that was last used by the user</returns>
        public static LL_AuthPlatforms GetLastActivePlatform(string forPlayerWithUlid = null)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            if (playerData == null)
            {
                return LL_AuthPlatforms.None;
            }

            return playerData.CurrentPlatform.Platform;
        }

        #endregion
    }
}
