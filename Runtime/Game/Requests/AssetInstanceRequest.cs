﻿using LootLocker.Requests;
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
        public static void GetAllKeyValuePairs(string forPlayerWithUlid, Action<LootLockerGetAllKeyValuePairsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getAllKeyValuePairs;
            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetAllKeyValuePairsToAnInstance(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getAllKeyValuePairsToAnInstance;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetAKeyValuePairById(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerGetSingleKeyValuePairsResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGetSingleKeyValuePairsResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.getAKeyValuePairById;

            string getVariable = endPoint.WithPathParameters(data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CreateKeyValuePair(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, LootLockerCreateKeyValuePairRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAssetDefaultResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.createKeyValuePair;

            string getVariable = endPoint.WithPathParameter(lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateOneOrMoreKeyValuePair(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, LootLockerUpdateOneOrMoreKeyValuePairRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAssetDefaultResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateOneOrMoreKeyValuePair;

            string getVariable = endPoint.WithPathParameter(lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateKeyValuePairById(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, LootLockerCreateKeyValuePairRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAssetDefaultResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateKeyValuePairById;

            string getVariable = endPoint.WithPathParameters(lootLockerGetRequest.getRequests[0], lootLockerGetRequest.getRequests[1]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void DeleteKeyValuePair(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deleteKeyValuePair;

            string getVariable = endPoint.WithPathParameters(data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void InspectALootBox(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerInspectALootBoxResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.inspectALootBox;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void OpenALootBox(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerOpenLootBoxResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.openALootBox;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void DeleteAssetInstanceFromPlayerInventory(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deleteAssetInstanceFromPlayerInventory;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

    }
}