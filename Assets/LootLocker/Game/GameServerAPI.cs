using System;
using System.Collections;
using System.Collections.Generic;
using LootLocker;
using LootLockerRequests;
using UnityEngine;

/// <summary>
/// made for user, relay on playtime coroutines
/// </summary>
public class GameServerAPI : BaseServerAPI
{
    public new static GameServerAPI I;
    ServerManager ServerManager;

    public static void Init(ServerManager serverManager)
    {
        I = new GameServerAPI();

        BaseServerAPI.Init(I);

        I.ServerManager = serverManager;

        I.StartCoroutine = I.ServerManager.StartCoroutine;
    }

    protected override void RefreshTokenAndCompleteCall(ServerRequest cacheServerRequest, Action<LootLockerResponse> OnServerResponse)
    {
        var sessionRequest = new SessionRequest(activeConfig.deviceID);

        LootLockerAPIManager.Session(sessionRequest, (response) =>
        {
            if (response.success)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("x-session-token", activeConfig.token);
                cacheServerRequest.extraHeaders = headers;
                if (cacheServerRequest.retryCount < 4)
                {
                    SendRequest(cacheServerRequest, OnServerResponse);
                    cacheServerRequest.retryCount++;
                }
                else
                {
                    Debug.LogError("Session refresh failed");
                    LootLockerResponse res = new LootLockerResponse();
                    res.statusCode = 401;
                    res.Error = "Token Expired";
                    res.hasError = true;
                    OnServerResponse?.Invoke(res);
                }
            }
            else
            {
                Debug.LogError("Session refresh failed");
                LootLockerResponse res = new LootLockerResponse();
                res.statusCode = 401;
                res.Error = "Token Expired";
                res.hasError = true;
                OnServerResponse?.Invoke(res);
            }
        });
    }
}
