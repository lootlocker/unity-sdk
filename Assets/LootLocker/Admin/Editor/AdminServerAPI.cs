using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using System;
using LootLockerAdminRequests;
using LootLockerAdmin;
using Unity.EditorCoroutines.Editor;

namespace LootLockerAdmin
{
    /// <summary>
    /// made for admin, relay on editing coroutines
    /// </summary>
    public class AdminServerAPI : BaseServerAPI
    {
        public new static AdminServerAPI I;

        public static void Init()
        {
            I = new AdminServerAPI();

            BaseServerAPI.Init(I);

            I.StartCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless;
        }

        protected override void RefreshTokenAndCompleteCall(ServerRequest cacheServerRequest, Action<LootLockerResponse> OnServerResponse)
        {
            var authRequest = new InitialAuthRequest();
            authRequest.email = activeConfig.email;
            authRequest.password = activeConfig.password;

            LootLockerAPIManagerAdmin.InitialAuthenticationRequest(authRequest, (response) =>
            {
                if (response.success)
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("x-auth-token", activeConfig.token);
                    cacheServerRequest.extraHeaders = headers;
                    if (cacheServerRequest.retryCount < 4)
                    {
                        SendRequest(cacheServerRequest, OnServerResponse);
                        cacheServerRequest.retryCount++;
                    }
                    else
                    {
                        LootLockerSDKAdminManager.DebugMessage("Admin token refresh failed");
                        LootLockerResponse res = new LootLockerResponse();
                        res.statusCode = 401;
                        res.Error = "Admin token Expired";
                        res.hasError = true;
                        OnServerResponse?.Invoke(res);
                    }
                }
                else
                {
                    LootLockerSDKAdminManager.DebugMessage("Admin token refresh failed", true);
                    LootLockerResponse res = new LootLockerResponse();
                    res.statusCode = 401;
                    res.Error = "Admin token Expired";
                    res.hasError = true;
                    OnServerResponse?.Invoke(res);
                }
            });
        }
    }
}