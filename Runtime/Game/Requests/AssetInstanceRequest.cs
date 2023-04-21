using LootLocker.Requests;
using System;

namespace LootLocker.Requests
{
    public class LootLockerGetAllKeyValuePairsResponse : LootLockerResponse
    {
        public int streamedObjectCount { get; set; } = 0;
        public LootLockerInstanceStoragePair[] keypairs { get; set; }
    }

    public class LootLockerGetSingleKeyValuePairsResponse : LootLockerResponse
    {
        public int streamedObjectCount { get; set; } = 0;
        public LootLockerInstanceStoragePair keypair { get; set; }
    }

    public class LootLockerInstanceStoragePair
    {
        public int instance_id { get; set; }
        public LootLockerStorage storage { get; set; }
    }

    public class LootLockerStorage
    {
        public int id { get; set; }
        public string key { get; set; }
        public string value { get; set; }
    }


    public class LootLockerAssetDefaultResponse : LootLockerResponse
    {
        public LootLockerStorage[] storage { get; set; }
    }


    public class LootLockerCreateKeyValuePairRequest
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class LootLockerUpdateOneOrMoreKeyValuePairRequest
    {
        public LootLockerCreateKeyValuePairRequest[] storage { get; set; }
    }


    public class LootLockerInspectALootBoxResponse : LootLockerResponse
    {
        public LootLockerContent[] contents { get; set; }
    }

    public class LootLockerContent
    {
        public int asset_id { get; set; }
        public int? asset_variation_id { get; set; }
        public int? asset_rental_option_id { get; set; }
        public int weight { get; set; }
    }

    public class LootLockerOpenLootBoxResponse : LootLockerResponse
    {
        public bool check_grant_notifications { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetAllKeyValuePairs(Action<LootLockerGetAllKeyValuePairsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getAllKeyValuePairs;
            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetAllKeyValuePairsToAnInstance(LootLockerGetRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getAllKeyValuePairsToAnInstance;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetAKeyValuePairById(LootLockerGetRequest data, Action<LootLockerGetSingleKeyValuePairsResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGetSingleKeyValuePairsResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.getAKeyValuePairById;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CreateKeyValuePair(LootLockerGetRequest lootLockerGetRequest, LootLockerCreateKeyValuePairRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAssetDefaultResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.createKeyValuePair;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateOneOrMoreKeyValuePair(LootLockerGetRequest lootLockerGetRequest, LootLockerUpdateOneOrMoreKeyValuePairRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAssetDefaultResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateOneOrMoreKeyValuePair;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateKeyValuePairById(LootLockerGetRequest lootLockerGetRequest, LootLockerCreateKeyValuePairRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAssetDefaultResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateKeyValuePairById;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0], lootLockerGetRequest.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void DeleteKeyValuePair(LootLockerGetRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deleteKeyValuePair;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void InspectALootBox(LootLockerGetRequest data, Action<LootLockerInspectALootBoxResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.inspectALootBox;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void OpenALootBox(LootLockerGetRequest data, Action<LootLockerOpenLootBoxResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.openALootBox;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}