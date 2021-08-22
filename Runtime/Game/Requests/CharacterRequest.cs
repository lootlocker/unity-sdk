using LootLocker;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using Newtonsoft.Json;
using System.Linq;



namespace LootLocker.Requests
{

    public class LootLockerGetCurrentLoadouttoDefaultCharacterResponse : LootLockerResponse
    {
        
        public LootLockerDefaultCharacterLoadout[] loadout { get; set; }

        public string[] GetContexts()
        {
            string[] context = loadout.Select(x => x.asset.context).ToArray();
            return context;
        }

        public LootLockerCommonAsset[] GetAssets()
        {
            LootLockerCommonAsset[] context = loadout.Select(x => x.asset).ToArray();
            return context;
        }
    }


    public class LootLockerListCharacterTypesResponse : LootLockerResponse
    {
        
        public LootLockerCharacter_Types[] character_types { get; set; }
    }

    [Serializable]
    public class LootLockerCharacter_Types
    {
        public int id;
        public bool is_default;
        public string name;
    }


    public class LootLockerDefaultCharacterLoadout
    {
        public int variation_id { get; set; }
        public int instance_id { get; set; }
        public DateTime mounted_at { get; set; }
        public LootLockerCommonAsset asset { get; set; }
        public LootLockerRental rental { get; set; }
    }

    public class LootLockerLoadouts
    {
        public string variation_id { get; set; }
        public int instance_id { get; set; }
        public DateTime mounted_at { get; set; }
        public LootLockerCommonAsset asset { get; set; }
    }

    public class LootLockerUpdateCharacterRequest
    {
        public bool is_default { get; set; }
        public string name { get; set; }
        public string type { get; set; }

    }
    public class LootLockerCreateCharacterRequest
    {
        public bool is_default { get; set; }
        public string name { get; set; }
        public string character_type_id { get; set; }

    }
    public class LootLockerEquipByIDRequest
    {
        public int instance_id { get; set; }
    }


    public class LootLockerEquipByAssetRequest
    {
        public int asset_id { get; set; }
        public int asset_variation_id { get; set; }
    }

    public class LootLockerCharacterLoadoutResponse : LootLockerResponse
    {
        
        public LootLockerLootLockerLoadout[] loadouts { get; set; }

        public LootLockerCharacter GetCharacter(string name)
        {
            LootLockerCharacter lootLockerCharacter = loadouts.FirstOrDefault(x => x.character.name == name)?.character;
            return lootLockerCharacter;
        }

        public LootLockerCharacter[] GetCharacters()
        {
            return loadouts.Select(x => x.character).ToArray();
        }
    }

    public class EquipAssetToCharacterLoadoutResponse : LootLockerResponse
    {
        

        public LootLockerCharacter character;
        public LootLockerLoadouts[] loadout;
    }

    [Serializable]
    public class LootLockerLootLockerLoadout 
    {
        public LootLockerCharacter character;
        public LootLockerLoadouts[] loadout;
    }

    [Serializable]
    public class LootLockerCharacter
    {
        public int id;
        public string type;
        public string name;
        public bool is_default;
    }

    public class LootLockerCharacterAsset
    {
        public string Asset { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void CreateCharacter(LootLockerCreateCharacterRequest data, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.createCharacter;

            string json = (data == null) ? "" : JsonConvert.SerializeObject(data);

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerCharacterLoadoutResponse response = new LootLockerCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void ListCharacterTypes(Action<LootLockerListCharacterTypesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listCharacterTypes;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerListCharacterTypesResponse response = new LootLockerListCharacterTypesResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerListCharacterTypesResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetCharacterLoadout(Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.characterLoadouts;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerCharacterLoadoutResponse response = new LootLockerCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetOtherPlayersCharacterLoadout(LootLockerGetRequest data, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersCharacterLoadouts;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerCharacterLoadoutResponse response = new LootLockerCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void UpdateCharacter(LootLockerGetRequest lootLockerGetRequest, LootLockerUpdateCharacterRequest data, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerCharacterLoadoutResponse response = new LootLockerCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void EquipIdAssetToDefaultCharacter(LootLockerEquipByIDRequest data, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipIDAssetToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                EquipAssetToCharacterLoadoutResponse response = new EquipAssetToCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<EquipAssetToCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void EquipGlobalAssetToDefaultCharacter(LootLockerEquipByAssetRequest data, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipGlobalAssetToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                EquipAssetToCharacterLoadoutResponse response = new EquipAssetToCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<EquipAssetToCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void EquipIdAssetToCharacter(LootLockerGetRequest lootLockerGetRequest, LootLockerEquipByIDRequest data, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipIDAssetToCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                EquipAssetToCharacterLoadoutResponse response = new EquipAssetToCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<EquipAssetToCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void EquipGlobalAssetToCharacter(LootLockerGetRequest lootLockerGetRequest, LootLockerEquipByAssetRequest data, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipGlobalAssetToCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                EquipAssetToCharacterLoadoutResponse response = new EquipAssetToCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<EquipAssetToCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void UnEquipIdAssetToDefaultCharacter(LootLockerGetRequest lootLockerGetRequest, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.unEquipIDAssetToDefaultCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                EquipAssetToCharacterLoadoutResponse response = new EquipAssetToCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<EquipAssetToCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void UnEquipIdAssetToCharacter(LootLockerGetRequest lootLockerGetRequest, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.unEquipIDAssetToCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                EquipAssetToCharacterLoadoutResponse response = new EquipAssetToCharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<EquipAssetToCharacterLoadoutResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetCurrentLoadOutToDefaultCharacter(Action<LootLockerGetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getCurrentLoadoutToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerGetCurrentLoadouttoDefaultCharacterResponse response = new LootLockerGetCurrentLoadouttoDefaultCharacterResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGetCurrentLoadouttoDefaultCharacterResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);//getEquipableContextToDefaultCharacter
        }

        public static void GetCurrentLoadOutToOtherCharacter(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerGetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersCharacterLoadouts;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerGetCurrentLoadouttoDefaultCharacterResponse response = new LootLockerGetCurrentLoadouttoDefaultCharacterResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGetCurrentLoadouttoDefaultCharacterResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetEquipableContextToDefaultCharacter(Action<LootLockerContextResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getEquipableContextToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerContextResponse response = new LootLockerContextResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerContextResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

    }
}