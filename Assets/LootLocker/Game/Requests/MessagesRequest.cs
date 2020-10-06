using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;
using Newtonsoft.Json;
using System;

namespace LootLockerRequests
{

    #region GetMessages

    public class GetMessagesResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public GMMessage[] messages { get; set; }
    }

    public class GMMessage:IStageData
    {
        public string title { get; set; }
        public string published_at { get; set; }
        public string body { get; set; }
        public string summary { get; set; }
        public string category { get; set; }
        public bool alert { get; set; }
        public bool _new { get; set; }
        public string action { get; set; }
        public string image { get; set; }
    }

    #endregion

}

namespace LootLocker
{

    public partial class LootLockerAPIManager
    {

        public static void GetMessages(Action<GetMessagesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getMessages;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) =>
            {
                GetMessagesResponse response = new GetMessagesResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetMessagesResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

    }

}