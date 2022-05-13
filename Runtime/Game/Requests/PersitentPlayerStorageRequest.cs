using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using Newtonsoft.Json;
using System;

namespace LootLocker.Requests
{
    public class LootLockerGetPersistentStoragResponse : LootLockerResponse
    {
        public virtual LootLockerPayload[] payload { get; set; }
    }

    public class LootLockerGetPersistentStorageRequest
    {
        public List<LootLockerPayload> payload = new List<LootLockerPayload>();

        public void AddToPayload(LootLockerPayload newPayload)
        {
            newPayload.order = payload.Count + 1;
            payload.Add(newPayload);
        }
    }

    public class LootLockerGetPersistentSingle : LootLockerResponse

    {
        public LootLockerPayload payload { get; set; }
    }

    [Serializable]
    public class LootLockerPayload
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
        public static void GetEntirePersistentStorage(Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getEntirePersistentStorage;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void GetSingleKeyPersistentStorage(LootLockerGetRequest data, Action<LootLockerGetPersistentSingle> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getSingleKeyFromPersitenctStorage;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void UpdateOrCreateKeyValue(LootLockerGetPersistentStorageRequest data, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateOrCreateKeyValue;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void DeleteKeyValue(LootLockerGetRequest data, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deleteKeyValue;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void GetOtherPlayersPublicKeyValuePairs(LootLockerGetRequest data, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersPublicKeyValuePairs;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }
    }
}