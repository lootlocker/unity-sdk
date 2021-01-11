using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using System;
using LootLocker.Admin.Requests;
using LootLocker.Admin;
using Unity.EditorCoroutines.Editor;

namespace LootLocker.Admin
{
    /// <summary>
    /// made for admin, relay on editing coroutines
    /// </summary>
    public class AdminServerAPI : LootLockerBaseServerAPI
    {
        public new static AdminServerAPI I;

        public static void Init()
        {
            I = new AdminServerAPI();

            LootLockerBaseServerAPI.Init(I);

            I.StartCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless;
        }

        protected override void RefreshTokenAndCompleteCall(LootLockerServerRequest cacheServerRequest, Action<LootLockerResponse> OnServerResponse)
        {
            var authRequest = new LootLockerInitialAuthRequest();
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