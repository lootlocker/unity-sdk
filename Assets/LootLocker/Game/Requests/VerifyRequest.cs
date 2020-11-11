using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;


namespace LootLockerRequests
{
    public class VerifyRequest
    {
        public string key => LootLockerConfig.current.apiKey.ToString();
        public string platform => LootLockerConfig.current.platform.ToString();
        public string token { get; set; }

        public VerifyRequest(string token)
        {
            this.token = token;
        }
    }

    public class VerifyResponse : LootLockerResponse
    {
        public bool success { set; get; }
    }
}

namespace LootLocker
{

 

    public partial class LootLockerAPIManager
    {
        public static void Verify(VerifyRequest data, Action<VerifyResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.playerVerification;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                VerifyResponse response = new VerifyResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<VerifyResponse>(serverResponse.text);
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
