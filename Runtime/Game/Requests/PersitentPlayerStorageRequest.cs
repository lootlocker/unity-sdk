﻿using System.Collections.Generic;
using LootLocker.Requests;
using System;

namespace LootLocker.Requests
{
    public class LootLockerGetPersistentStorageResponse : LootLockerResponse
    {
        public virtual LootLockerPayload[] payload { get; set; }
    }

    public class LootLockerGetPersistentStorageResponseDictionary : LootLockerResponse
    {
        public virtual Dictionary<string, string> payload { get; set; }
    }

    public class LootLockerGetPersistentStorageRequest
    {
        public List<LootLockerPayload> payload { get; set; } = new List<LootLockerPayload>();

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
        public string key { get; set; }
        public string value { get; set; }
        public int order { get; set; }
        public bool is_public { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetEntirePersistentStorage(string forPlayerWithUlid, Action<LootLockerGetPersistentStorageResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getEntirePersistentStorage;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetEntirePersistentStorage(string forPlayerWithUlid, Action<LootLockerGetPersistentStorageResponseDictionary> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getEntirePersistentStorage;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetSingleKeyPersistentStorage(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerGetPersistentSingle> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getSingleKeyFromPersistentStorage;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateOrCreateKeyValue(string forPlayerWithUlid, LootLockerGetPersistentStorageRequest data, Action<LootLockerGetPersistentStorageResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGetPersistentStorageResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateOrCreateKeyValue;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                serverResponse.text = serverResponse.text.Replace("\"public\"", "\"is_public\"");
                LootLockerResponse.Deserialize(onComplete, serverResponse);
            });
        }

        public static void DeleteKeyValue(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerGetPersistentStorageResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deleteKeyValue;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetOtherPlayersPublicKeyValuePairs(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerGetPersistentStorageResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersPublicKeyValuePairs;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}