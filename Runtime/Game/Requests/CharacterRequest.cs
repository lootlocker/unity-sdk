﻿using System;
using LootLocker.Requests;
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
        public LootLockerStorage [] storage;
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
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerCharacterLoadoutResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListCharacterTypes(Action<LootLockerListCharacterTypesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listCharacterTypes;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetCharacterLoadout(Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.characterLoadouts;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetOtherPlayersCharacterLoadout(LootLockerGetRequest data, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersCharacterLoadouts;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateCharacter(LootLockerGetRequest lootLockerGetRequest, LootLockerUpdateCharacterRequest data, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerCharacterLoadoutResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipIdAssetToDefaultCharacter(LootLockerEquipByIDRequest data, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToCharacterLoadoutResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipIDAssetToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipGlobalAssetToDefaultCharacter(LootLockerEquipByAssetRequest data, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToCharacterLoadoutResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipGlobalAssetToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipIdAssetToCharacter(LootLockerGetRequest lootLockerGetRequest, LootLockerEquipByIDRequest data, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToCharacterLoadoutResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipIDAssetToCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipGlobalAssetToCharacter(LootLockerGetRequest lootLockerGetRequest, LootLockerEquipByAssetRequest data, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToCharacterLoadoutResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipGlobalAssetToCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UnEquipIdAssetToDefaultCharacter(LootLockerGetRequest lootLockerGetRequest, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.unEquipIDAssetToDefaultCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UnEquipIdAssetToCharacter(LootLockerGetRequest lootLockerGetRequest, Action<EquipAssetToCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.unEquipIDAssetToCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetCurrentLoadOutToDefaultCharacter(Action<LootLockerGetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getCurrentLoadoutToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetCurrentLoadOutToOtherCharacter(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerGetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersCharacterLoadouts;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetEquipableContextToDefaultCharacter(Action<LootLockerContextResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getEquipableContextToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}