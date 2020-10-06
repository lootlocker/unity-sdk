using LootLocker;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLockerRequests;
using Newtonsoft.Json;
using System.Linq;



namespace LootLockerRequests
{

    public class GetCurrentLoadouttoDefaultCharacterResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public DefaultCharacterLoadout[] loadout { get; set; }

        public string[] GetContexts()
        {
            string[] context = loadout.Select(x => x.asset.context).ToArray();
            return context;
        }

        public Asset[] GetAssets()
        {
            Asset[] context = loadout.Select(x => x.asset).ToArray();
            return context;
        }
    }

    public class DefaultCharacterLoadout
    {
        public int variation_id { get; set; }
        public int instance_id { get; set; }
        public DateTime mounted_at { get; set; }
        public Asset asset { get; set; }
        public Rental rental { get; set; }
    }

    public class Loadouts
    {
        public string variation_id { get; set; }
        public int instance_id { get; set; }
        public DateTime mounted_at { get; set; }
        public Asset asset { get; set; }
    }

    public class UpdateCharacterRequest
    {
        public bool is_default { get; set; }
        public string name { get; set; }
        public string type { get; set; }

    }

    public class EquipByIDRequest
    {
        public int instance_id { get; set; }
    }


    public class EquipByAssetRequest
    {
        public int asset_id { get; set; }
        public int asset_variation_id { get; set; }
    }

    public class CharacterLoadoutResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Loadout[] loadouts { get; set; }
    }

    public class Loadout : IStageData
    {
        public Character character { get; set; }
        public Loadouts[] loadout { get; set; }
    }

    public class Character
    {
        public int id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public bool is_default { get; set; }
    }

    public class CharacterAsset
    {
        public string Asset { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetCharacterLoadout(Action<CharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.characterLoadouts;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void GetOtherPlayersCharacterLoadout(LootLockerGetRequest data, Action<CharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getOtherPlayersCharacterLoadouts;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void UpdateCharacter(LootLockerGetRequest lootLockerGetRequest, UpdateCharacterRequest data, Action<CharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.updateCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void EquipIdAssetToDefaultCharacter(EquipByIDRequest data, Action<CharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.equipIDAssetToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void EquipGlobalAssetToDefaultCharacter(EquipByAssetRequest data, Action<CharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.equipGlobalAssetToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void EquipIdAssetToCharacter(LootLockerGetRequest lootLockerGetRequest, EquipByIDRequest data, Action<CharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.equipIDAssetToCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void EquipGlobalAssetToCharacter(LootLockerGetRequest lootLockerGetRequest, EquipByAssetRequest data, Action<CharacterLoadoutResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.equipGlobalAssetToCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void UnEquipIdAssetToDefaultCharacter(LootLockerGetRequest lootLockerGetRequest, Action<CharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.unEquipIDAssetToDefaultCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void UnEquipIdAssetToCharacter(LootLockerGetRequest lootLockerGetRequest, Action<CharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.unEquipIDAssetToCharacter;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void GetCurrentLoadOutToDefaultCharacter(Action<GetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getCurrentLoadoutToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                GetCurrentLoadouttoDefaultCharacterResponse response = new GetCurrentLoadouttoDefaultCharacterResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetCurrentLoadouttoDefaultCharacterResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);//getEquipableContextToDefaultCharacter
        }

        public static void GetCurrentLoadOutToOtherCharacter(LootLockerGetRequest lootLockerGetRequest, Action<GetCurrentLoadouttoDefaultCharacterResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getOtherPlayersCharacterLoadouts;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                GetCurrentLoadouttoDefaultCharacterResponse response = new GetCurrentLoadouttoDefaultCharacterResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetCurrentLoadouttoDefaultCharacterResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void GetEquipableContextToDefaultCharacter(Action<ContextResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getEquipableContextToDefaultCharacter;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                ContextResponse response = new ContextResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<ContextResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        //public static void GetInventory(Action<InventoryResponse> onComplete)
        //{
        //    EndPointClass endPoint = LootLockerEndPoints.current.getInventory;

        //    ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
        //    {
        //        InventoryResponse response = new InventoryResponse();
        //        if (string.IsNullOrEmpty(serverResponse.Error))
        //        {
        //            response = JsonConvert.DeserializeObject<InventoryResponse>(serverResponse.text);
        //            response.text = serverResponse.text;
        //            onComplete?.Invoke(response);
        //        }
        //        else
        //        {
        //            response.message = serverResponse.message;
        //            response.Error = serverResponse.Error;
        //            onComplete?.Invoke(response);
        //        }
        //    }, true);
        //}
    }
}