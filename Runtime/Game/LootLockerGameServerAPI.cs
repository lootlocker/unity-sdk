using System;
using System.Collections;
using System.Collections.Generic;
using LootLocker;
using LootLocker.Requests;
using UnityEngine;

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
            string platform = LootLockerSDKManager.GetCurrentPlatform();

            if (platform == Platforms.Steam)
            {
                LootLockerSDKManager.DebugMessage("Token has expired and token refresh is not supported for Steam", true);
                LootLockerResponse res = new LootLockerResponse();
                res.statusCode = 401;
                res.Error = "Token Expired";
                res.hasError = true;
                OnServerResponse?.Invoke(res);
                return;
            }

            if (platform == Platforms.NintendoSwitch)
            {
                LootLockerSDKManager.DebugMessage("Token has expired and token refresh is not supported for Nintendo Switch", true);
                LootLockerResponse res = new LootLockerResponse();
                res.statusCode = 401;
                res.Error = "Token Expired";
                res.hasError = true;
                OnServerResponse?.Invoke(res);
                return;
            }

            if (platform == Platforms.Guest)
            {
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    CompleteCall(cacheServerRequest, OnServerResponse, response);
                });
                return;
            } else if (platform == Platforms.WhiteLabel)
            {
                LootLockerSDKManager.StartWhiteLabelSession(response =>
                {
                    CompleteCall(cacheServerRequest, OnServerResponse, response);
                });

                return;
            } else {
                var sessionRequest = new LootLockerSessionRequest(LootLockerConfig.current.deviceID);
                LootLockerAPIManager.Session(sessionRequest, (response) =>
                {
                    CompleteCall(cacheServerRequest, OnServerResponse, response);
                });
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
                        LootLockerSDKManager.DebugMessage("Session refresh failed", true);
                        LootLockerResponse res = new LootLockerResponse();
                        res.statusCode = 401;
                        res.Error = "Token Expired";
                        res.hasError = true;
                        newOnServerResponse?.Invoke(res);
                    }
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("Session refresh failed", true);
                    LootLockerResponse res = new LootLockerResponse();
                    res.statusCode = 401;
                    res.Error = "Token Expired";
                    res.hasError = true;
                    newOnServerResponse?.Invoke(res);
                }
            }
        }
    }
}