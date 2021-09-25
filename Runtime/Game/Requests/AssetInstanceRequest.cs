using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace LootLocker.Requests
{
    public class LootLockerGetAllKeyValuePairsResponse : LootLockerResponse
    {
        public int streamedObjectCount = 0;
        public LootLockerInstanceStoragePair[] keypairs;
    }

    public class LootLockerGetSingleKeyValuePairsResponse : LootLockerResponse
    {
        public int streamedObjectCount = 0;
        public LootLockerInstanceStoragePair keypair;
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

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerGetAllKeyValuePairsResponse response = new LootLockerGetAllKeyValuePairsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGetAllKeyValuePairsResponse>(serverResponse.text);

                // LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.statusCode = serverResponse.statusCode;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);

            }, true);
        }

        public static void GetAllKeyValuePairsToAnInstance(LootLockerGetRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getAllKeyValuePairsToAnInstance;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerAssetDefaultResponse response = new LootLockerAssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerAssetDefaultResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetAKeyValuePairById(LootLockerGetRequest data, Action<LootLockerGetSingleKeyValuePairsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getAKeyValuePairById;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                LootLockerGetSingleKeyValuePairsResponse response = new LootLockerGetSingleKeyValuePairsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGetSingleKeyValuePairsResponse>(serverResponse.text);

                //   LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void CreateKeyValuePair(LootLockerGetRequest lootLockerGetRequest, LootLockerCreateKeyValuePairRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.createKeyValuePair;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                LootLockerAssetDefaultResponse response = new LootLockerAssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    if (string.IsNullOrEmpty(serverResponse.Error))
                        response = JsonConvert.DeserializeObject<LootLockerAssetDefaultResponse>(serverResponse.text);

                    //     LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                    response.text = serverResponse.text;
                    response.success = serverResponse.success;
                    response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void UpdateOneOrMoreKeyValuePair(LootLockerGetRequest lootLockerGetRequest, LootLockerUpdateOneOrMoreKeyValuePairRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.updateOneOrMoreKeyValuePair;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                LootLockerAssetDefaultResponse response = new LootLockerAssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerAssetDefaultResponse>(serverResponse.text);

                //  LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void UpdateKeyValuePairById(LootLockerGetRequest lootLockerGetRequest, LootLockerCreateKeyValuePairRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.updateKeyValuePairById;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0], lootLockerGetRequest.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                LootLockerAssetDefaultResponse response = new LootLockerAssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerAssetDefaultResponse>(serverResponse.text);

                //  LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void DeleteKeyValuePair(LootLockerGetRequest data, Action<LootLockerAssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deleteKeyValuePair;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerAssetDefaultResponse response = new LootLockerAssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerAssetDefaultResponse>(serverResponse.text);

                // LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void InspectALootBox(LootLockerGetRequest data, Action<LootLockerInspectALootBoxResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.inspectALootBox;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerInspectALootBoxResponse response = new LootLockerInspectALootBoxResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerInspectALootBoxResponse>(serverResponse.text);

                //    LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void OpenALootBox(LootLockerGetRequest data, Action<LootLockerOpenLootBoxResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.openALootBox;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerOpenLootBoxResponse response = new LootLockerOpenLootBoxResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerOpenLootBoxResponse>(serverResponse.text);

                //  LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }
    }
}