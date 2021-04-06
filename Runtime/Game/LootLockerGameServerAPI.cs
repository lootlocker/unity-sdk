using System;
using System.Collections;
using System.Collections.Generic;
using LootLocker;
using LootLocker.Requests;
using UnityEngine;

namespace LootLocker
{
    /// <summary>
    /// made for user, relay on playtime coroutines
    /// </summary>
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
            if (LootLockerConfig.current.platform == LootLockerConfig.platformType.Steam)
            {
                LootLockerSDKManager.DebugMessage("Token has expired, And token refresh not supported in Steam calls", true);
                LootLockerResponse res = new LootLockerResponse();
                res.statusCode = 401;
                res.Error = "Token Expired";
                res.hasError = true;
                OnServerResponse?.Invoke(res);
                return;
            }

            var sessionRequest = new LootLockerSessionRequest(LootLockerConfig.current.deviceID);

            LootLockerAPIManager.Session(sessionRequest, (response) =>
            {
                if (response.success)
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("x-session-token", LootLockerConfig.current.token);
                    cacheServerRequest.extraHeaders = headers;
                    if (cacheServerRequest.retryCount < 4)
                    {
                        SendRequest(cacheServerRequest, OnServerResponse);
                        cacheServerRequest.retryCount++;
                    }
                    else
                    {
                        LootLockerSDKManager.DebugMessage("Session refresh failed", true);
                        LootLockerResponse res = new LootLockerResponse();
                        res.statusCode = 401;
                        res.Error = "Token Expired";
                        res.hasError = true;
                        OnServerResponse?.Invoke(res);
                    }
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("Session refresh failed", true);
                    LootLockerResponse res = new LootLockerResponse();
                    res.statusCode = 401;
                    res.Error = "Token Expired";
                    res.hasError = true;
                    OnServerResponse?.Invoke(res);
                }
            });
        }
    }
}