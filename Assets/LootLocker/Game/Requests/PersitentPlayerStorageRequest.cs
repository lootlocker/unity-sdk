using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;
using Newtonsoft.Json;
using System;

namespace LootLockerRequests
{
    public class GetPersistentStoragResponse : LootLockerResponse, IStageData
    {
        public bool success { get; set; }
        public virtual Payload[] payload { get; set; }
    }

    public class GetPersistentStoragRequest
    {
        public List<Payload> payload = new List<Payload>();

        public void AddToPayload(Payload newPayload)
        {
            newPayload.order = payload.Count + 1;
            payload.Add(newPayload);
        }
    }

    public class GetPersistentSingle : LootLockerResponse

    {
        public bool success { get; set; }
        public Payload payload { get; set; }
    }
    [Serializable]
    public class Payload
    {
        public string key;
        public string value;
        public int order;
        public bool is_public;
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetEntirePersistentStorage(Action<GetPersistentStoragResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getEntirePersistentStorage;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                GetPersistentStoragResponse response = new GetPersistentStoragResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetPersistentStoragResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void GetSingleKeyPersistentStorage(Action<GetPersistentSingle> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getSingleKeyFromPersitenctStorage;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                GetPersistentSingle response = new GetPersistentSingle();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetPersistentSingle>(serverResponse.text);
                    response.text = serverResponse.text;
                    response.success = true;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void UpdateOrCreateKeyValue(GetPersistentStoragRequest data, Action<GetPersistentStoragResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.updateOrCreateKeyValue;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                GetPersistentStoragResponse response = new GetPersistentStoragResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetPersistentStoragResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void DeleteKeyValue(LootLockerGetRequest data, Action<GetPersistentStoragResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.deleteKeyValue;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                GetPersistentStoragResponse response = new GetPersistentStoragResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetPersistentStoragResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void GetOtherPlayersPublicKeyValuePairs(LootLockerGetRequest data, Action<GetPersistentStoragResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getOtherPlayersPublicKeyValuePairs;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                GetPersistentStoragResponse response = new GetPersistentStoragResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetPersistentStoragResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

    }
}
