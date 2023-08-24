using System;
using System.Collections.Generic;
using LootLocker.Requests;

namespace LootLocker
{
    public class LootLockerGameServerAPI : LootLockerBaseServerAPI
    {
        public new static LootLockerGameServerAPI I;
        LootLockerServerManager ServerManager;

        public static void Init(LootLockerServerManager serverManager)
        {
            I = new LootLockerGameServerAPI();

            LootLockerBaseServerAPI.Init(I);

            I.ServerManager = serverManager;

            I.StartCoroutine = I.ServerManager.StartCoroutine;
        }

        protected override void RefreshTokenAndCompleteCall(LootLockerServerRequest cacheServerRequest, Action<LootLockerResponse> OnServerResponse)
        {
            switch (CurrentPlatform.Get())
            {
                case Platforms.Guest:
                {
                    LootLockerSDKManager.StartGuestSession(response =>
                    {
                        CompleteCall(cacheServerRequest, OnServerResponse, response);
                    });
                    return;
                }
                case Platforms.WhiteLabel:
                {
                    LootLockerSDKManager.StartWhiteLabelSession(response =>
                    {
                        CompleteCall(cacheServerRequest, OnServerResponse, response);
                    });
                    return;
                }
                case Platforms.AppleGameCenter:
                case Platforms.AppleSignIn:
                case Platforms.Epic:
                case Platforms.Google:
                {
                    // The failed request isn't a refresh session request but we have a refresh token stored, so try to refresh the session automatically before failing
                    if (!cacheServerRequest.jsonPayload.Contains("refresh_token") && !string.IsNullOrEmpty(LootLockerConfig.current.refreshToken))
                    {
                        switch (CurrentPlatform.Get())
                        {
                            case Platforms.AppleGameCenter:
                                LootLockerSDKManager.RefreshAppleGameCenterSession(response =>
                                {
                                    CompleteCall(cacheServerRequest, OnServerResponse, response);
                                });
                                return;
                            case Platforms.AppleSignIn:
                                LootLockerSDKManager.RefreshAppleSession(response =>
                                {
                                    CompleteCall(cacheServerRequest, OnServerResponse, response);
                                });
                                return;
                            case Platforms.Epic:
                                LootLockerSDKManager.RefreshEpicSession(response =>
                                {
                                    CompleteCall(cacheServerRequest, OnServerResponse, response);
                                });
                                return;
                            case Platforms.Google:
                                LootLockerSDKManager.RefreshGoogleSession(response =>
                                {
                                    CompleteCall(cacheServerRequest, OnServerResponse, response);
                                });
                                return;
                        }
                    }
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                    OnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
                    return;
                }
                case Platforms.NintendoSwitch:
                case Platforms.Steam:
                {
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired and token refresh is not supported for {CurrentPlatform.GetFriendlyString()}");
                    OnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
                    return;
                }
                case Platforms.PlayStationNetwork:
                case Platforms.XboxOne:
                case Platforms.AmazonLuna:
                {
                    var sessionRequest = new LootLockerSessionRequest(LootLockerConfig.current.deviceID);
                    LootLockerAPIManager.Session(sessionRequest, (response) =>
                    {
                        CompleteCall(cacheServerRequest, OnServerResponse, response);
                    });
                    break;
                }
                case Platforms.None:
                default:
                {
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"Platform {CurrentPlatform.GetFriendlyString()} not supported");
                    OnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>($"Platform {CurrentPlatform.GetFriendlyString()} not supported", 401));
                    return;
                }
            }
        }

        void CompleteCall(LootLockerServerRequest newcacheServerRequest, Action<LootLockerResponse> newOnServerResponse, LootLockerSessionResponse response)
        {
            if (response.success)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("x-session-token", LootLockerConfig.current.token);
                newcacheServerRequest.extraHeaders = headers;
                if (newcacheServerRequest.retryCount < 4)
                {
                    SendRequest(newcacheServerRequest, newOnServerResponse);
                    newcacheServerRequest.retryCount++;
                }
                else
                {
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Info)("Session refresh failed");
                    newOnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
                }
            }
            else
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Info)("Session refresh failed");
                LootLockerResponse res = new LootLockerResponse();
                newOnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
            }
        }
    }
}