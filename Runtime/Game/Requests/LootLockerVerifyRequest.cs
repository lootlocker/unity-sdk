using LootLocker.Newtonsoft.Json;
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

        public LootLockerVerifyRequest(string token)
        {
            this.token = token;
        }
    }

    public class LootLockerVerifyResponse : LootLockerResponse
    {
        public bool success { set; get; }
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

            EndPointClass endPoint = LootLockerEndPoints.current.playerVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerVerifyResponse response = new LootLockerVerifyResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerVerifyResponse>(serverResponse.text);
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, false);
        }

    }
}
