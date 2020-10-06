using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace LootLockerRequests
{
    public class GetAllKeyValuePairsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int streamedObjectCount = 0;
        public InstanceStoragePair[] keypairs;
    }

    public class InstanceStoragePair
    {
        public int instance_id { get; set; }
        public Storage storage { get; set; }
    }

    public class Storage
    {
        public int id { get; set; }
        public string key { get; set; }
        public string value { get; set; }
    }


    public class AssetDefaultResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Storage[] storage { get; set; }
    }


    public class CreateKeyValuePairRequest
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class UpdateOneOrMoreKeyValuePairRequest
    {
        public CreateKeyValuePairRequest[] storage { get; set; }
    }


    public class InspectALootBoxResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Content[] contents { get; set; }
    }

    public class Content
    {
        public int asset_id { get; set; }
        public int? asset_variation_id { get; set; }
        public int? asset_rental_option_id { get; set; }
        public int weight { get; set; }
    }

    public class OpenLootBoxResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public bool check_grant_notifications { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetAllKeyValuePairs(Action<GetAllKeyValuePairsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getAllKeyValuePairs;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                GetAllKeyValuePairsResponse response = new GetAllKeyValuePairsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<GetAllKeyValuePairsResponse>(serverResponse.text);
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

        public static void GetAllKeyValuePairsToAnInstance(LootLockerGetRequest data, Action<AssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getAllKeyValuePairsToAnInstance;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                AssetDefaultResponse response = new AssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetDefaultResponse>(serverResponse.text);
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

        public static void GetAKeyValuePairById(LootLockerGetRequest data, Action<AssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getAKeyValuePairById;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                AssetDefaultResponse response = new AssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetDefaultResponse>(serverResponse.text);
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

        public static void CreateKeyValuePair(LootLockerGetRequest lootLockerGetRequest, CreateKeyValuePairRequest data, Action<AssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.createKeyValuePair;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                AssetDefaultResponse response = new AssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetDefaultResponse>(serverResponse.text);
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

        public static void UpdateOneOrMoreKeyValuePair(LootLockerGetRequest lootLockerGetRequest, UpdateOneOrMoreKeyValuePairRequest data, Action<AssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.updateOneOrMoreKeyValuePair;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                AssetDefaultResponse response = new AssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetDefaultResponse>(serverResponse.text);
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

        public static void UpdateKeyValuePairById(LootLockerGetRequest lootLockerGetRequest, CreateKeyValuePairRequest data, Action<AssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.updateKeyValuePairById;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0], lootLockerGetRequest.getRequests[1]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                AssetDefaultResponse response = new AssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetDefaultResponse>(serverResponse.text);
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

        public static void DeleteKeyValuePair(LootLockerGetRequest data, Action<AssetDefaultResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.deleteKeyValuePair;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                AssetDefaultResponse response = new AssetDefaultResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetDefaultResponse>(serverResponse.text);
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

        public static void InspectALootBox(LootLockerGetRequest data, Action<InspectALootBoxResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.deleteKeyValuePair;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                InspectALootBoxResponse response = new InspectALootBoxResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<InspectALootBoxResponse>(serverResponse.text);
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

        public static void OpenALootBox(LootLockerGetRequest data, Action<OpenLootBoxResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.deleteKeyValuePair;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                OpenLootBoxResponse response = new OpenLootBoxResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<OpenLootBoxResponse>(serverResponse.text);
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