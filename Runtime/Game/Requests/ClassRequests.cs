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

    public class LootLockerPlayerClassListResponse : LootLockerResponse
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("items")]
#else
        [Json(Name = "items")]
#endif
        public LootLockerClass[] classes { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void CreateClass(string forPlayerWithUlid, LootLockerCreateClassRequest data, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.createClass;
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListClassTypes(string forPlayerWithUlid, Action<LootLockerListClassTypesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listClassTypes;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListPlayerClasses(string forPlayerWithUlid, Action<LootLockerPlayerClassListResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listPlayerClasses;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetClassLoadout(string forPlayerWithUlid, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.classLoadouts;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetOtherPlayersClassLoadout(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersClassLoadouts;

            string getVariable = endPoint.WithPathParameters(data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateClass(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, LootLockerUpdateClassRequest data, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.updateClass;

            string getVariable = endPoint.WithPathParameter(lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipIdAssetToDefaultClass(string forPlayerWithUlid, LootLockerEquipByIDRequest data, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipIDAssetToDefaultClass;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipGlobalAssetToDefaultClass(string forPlayerWithUlid, LootLockerEquipByAssetRequest data, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipGlobalAssetToDefaultClass;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipIdAssetToClass(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, LootLockerEquipByIDRequest data, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipIDAssetToClass;

            string getVariable = endPoint.WithPathParameter(lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipGlobalAssetToClass(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, LootLockerEquipByAssetRequest data, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<EquipAssetToClassLoadoutResponse>(forPlayerWithUlid));
                return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.equipGlobalAssetToClass;

            string getVariable = endPoint.WithPathParameter(lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UnEquipIdAssetToDefaultClass(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.unEquipIDAssetToDefaultClass;

            string getVariable = endPoint.WithPathParameter(lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UnEquipIdAssetToClass(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, Action<EquipAssetToClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.unEquipIDAssetToClass;

            string getVariable = endPoint.WithPathParameters(lootLockerGetRequest.getRequests[0], lootLockerGetRequest.getRequests[1]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetCurrentLoadoutToDefaultClass(string forPlayerWithUlid, Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getCurrentLoadoutToDefaultClass;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetCurrentLoadoutToOtherClass(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, Action<LootLockerGetCurrentLoadoutToDefaultClassResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersLoadoutToDefaultClass;

            string getVariable = endPoint.WithPathParameters(lootLockerGetRequest.getRequests[0], lootLockerGetRequest.getRequests[1]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetEquipableContextToDefaultClass(string forPlayerWithUlid, Action<LootLockerContextResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getEquipableContextToDefaultClass;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}