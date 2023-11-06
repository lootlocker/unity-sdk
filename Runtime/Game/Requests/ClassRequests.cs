#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
#else
using LLlibs.ZeroDepJson;
#endif
using System;
using LootLocker.Requests;
using System.Linq;

namespace LootLocker.Requests 
{
    public class LootLockerGetCurrentLoadoutToDefaultClassResponse : LootLockerResponse 
    {
        public LootLockerDefaultClassLoadout[] loadout { get; set; }

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

    public class LootLockerDefaultClassLoadout 
    {
        public int variation_id { get; set; }
        public int instance_id { get; set; }
        public DateTime mounted_at { get; set; }
        public LootLockerCommonAsset asset { get; set; }
        public LootLockerRental rental { get; set; }
    }

    public class LootLockerListClassTypesResponse : LootLockerResponse
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("character_types")]
#else
        [Json(Name = "character_types")]
#endif
        public LootLockerClassTypes[] class_types { get; set; }
    }

    public class LootLockerClassTypes
    {
        public int id { get; set; }
        public bool is_default { get; set; }
        public string name { get; set; }
        public LootLockerStorage[] storage { get; set; }
    }

    public class LootLockerLoadouts
    {
        public string variation_id { get; set; }
        public int instance_id { get; set; }
        public DateTime mounted_at { get; set; }
        public LootLockerCommonAsset asset { get; set; }
    }

    public class LootLockerCreateClassRequest
    {
        public bool is_default { get; set; }
        public string name { get; set; }
        public string character_type_id { get; set; }
    }

    public class LootLockerUpdateClassRequest
    {
        public bool is_default { get; set; }
        public string name { get; set; }
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

    public class LootLockerClassLoadoutResponse : LootLockerResponse
    {
        public LootLockerClassLoadout[] loadouts { get; set; }

        public LootLockerClass GetClass(string name)
        {
            LootLockerClass lootLockerCharacter = loadouts.FirstOrDefault(x => x.Class.name == name)?.Class;
            return lootLockerCharacter;
        }

        public LootLockerClass[] GetClassess()
        {
            return loadouts.Select(x => x.Class).ToArray();
        }
    }

    public class LootLockerClass
    {
        public int id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string ulid { get; set; }
        public bool is_default { get; set; }
    }


    public class LootLockerClassLoadout
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("character")]
#else
        [Json(Name = "character")]
#endif
        public LootLockerClass Class { get; set; }
        public LootLockerLoadouts[] loadout { get; set; }
    }

    public class EquipAssetToClassLoadoutResponse : LootLockerResponse
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("character")]
#else
        [Json(Name = "character")]
#endif
        public LootLockerClass Class { get; set; }
        public LootLockerLoadouts[] loadout { get; set; }
        public string error { get; set; }
    }

    public class LootLockerClassAsset
    {
        public string Asset { get; set; }
    }

    public class LootLockerPlayerClassListResponse : LootLockerResponse
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("characters")]
#else
        [Json(Name = "characters")]
#endif
        public LootLockerClass[] classes { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void CreateClass(LootLockerCreateClassRequest data, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.createClass;
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerClassLoadoutResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListClassTypes(Action<LootLockerListClassTypesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listClassTypes;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListPlayerClasses(Action<LootLockerPlayerClassListResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listPlayerClasses;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetClassLoadout(Action<LootLockerClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.classLoadouts;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetOtherPlayersClassLoadout(LootLockerGetRequest data, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersClassLoadouts;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateClass(LootLockerGetRequest lootLockerGetRequest, LootLockerUpdateClassRequest data, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerClassLoadoutResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateClass;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipIdAssetToDefaultClass(LootLockerEquipByIDRequest data, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToClassLoadoutResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipIDAssetToDefaultClass;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipGlobalAssetToDefaultClass(LootLockerEquipByAssetRequest data, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToClassLoadoutResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipGlobalAssetToDefaultClass;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipIdAssetToClass(LootLockerGetRequest lootLockerGetRequest, LootLockerEquipByIDRequest data, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToClassLoadoutResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipIDAssetToClass;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipGlobalAssetToClass(LootLockerGetRequest lootLockerGetRequest, LootLockerEquipByAssetRequest data, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToClassLoadoutResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipGlobalAssetToClass;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UnEquipIdAssetToDefaultClass(LootLockerGetRequest lootLockerGetRequest, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.unEquipIDAssetToDefaultClass;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UnEquipIdAssetToClass(LootLockerGetRequest lootLockerGetRequest, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.unEquipIDAssetToClass;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetCurrentLoadoutToDefaultClass(Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getCurrentLoadoutToDefaultClass;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetCurrentLoadoutToOtherClass(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersLoadoutToDefaultClass;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0], lootLockerGetRequest.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetEquipableContextToDefaultClass(Action<LootLockerContextResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getEquipableContextToDefaultClass;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}