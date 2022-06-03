using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    public class LootLockerVerifyRequest
    {
        public string key => LootLockerConfig.current.apiKey.ToString();
        public string platform => LootLockerConfig.current.platform.ToString();
        public string token { get; set; }
        public bool development_mode => LootLockerConfig.current.developmentMode;

        public LootLockerVerifyRequest(string token)
        {
            this.token = token;
        }
    }

    public class LootLockerVerifySteamRequest : LootLockerVerifyRequest
    {
        public new string platform => "Steam";

        public LootLockerVerifySteamRequest(string token) : base(token)
        {
            this.token = token;
        }
    }

    public class LootLockerVerifyResponse : LootLockerResponse
    {
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void Verify(LootLockerVerifyRequest data, Action<LootLockerVerifyResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.playerVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, false);
        }
    }
}