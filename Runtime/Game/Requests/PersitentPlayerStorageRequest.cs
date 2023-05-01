using System.Collections.Generic;
using LootLocker.Requests;
using System;

namespace LootLocker.Requests
{
    public class LootLockerGetPersistentStoragResponse : LootLockerResponse
    {
        public virtual LootLockerPayload[] payload { get; set; }
    }
    public class LootLockerGetPersistentStoragResponseDictionary : LootLockerResponse
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
        public static void GetEntirePersistentStorage(Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getEntirePersistentStorage;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetEntirePersistentStorage(Action<LootLockerGetPersistentStoragResponseDictionary> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getEntirePersistentStorage;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetSingleKeyPersistentStorage(LootLockerGetRequest data, Action<LootLockerGetPersistentSingle> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getSingleKeyFromPersitenctStorage;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateOrCreateKeyValue(LootLockerGetPersistentStorageRequest data, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGetPersistentStoragResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateOrCreateKeyValue;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void DeleteKeyValue(LootLockerGetRequest data, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deleteKeyValue;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetOtherPlayersPublicKeyValuePairs(LootLockerGetRequest data, Action<LootLockerGetPersistentStoragResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersPublicKeyValuePairs;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}