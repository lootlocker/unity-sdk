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
    /// <summary>
    /// Response containing the currently equipped loadout for the default class.
    /// </summary>
    public class LootLockerGetCurrentLoadoutToDefaultClassResponse : LootLockerResponse 
    {
        /// <summary>The loadout items currently equipped on the default class.</summary>
        public LootLockerDefaultClassLoadout[] loadout { get; set; }

        /// <summary>Returns the context names of all equipped assets.</summary>
        public string[] GetContexts()
        {
            string[] context = loadout.Select(x => x.asset.context).ToArray();
            return context;
        }

        /// <summary>Returns the asset objects of all equipped items.</summary>
        public LootLockerCommonAsset[] GetAssets()
        {
            LootLockerCommonAsset[] context = loadout.Select(x => x.asset).ToArray();
            return context;
        }
    }

    /// <summary>
    /// A single loadout slot on the default class, combining asset details with equip metadata.
    /// </summary>
    public class LootLockerDefaultClassLoadout 
    {
        /// <summary>The id of the asset variation that is equipped in this slot.</summary>
        public int variation_id { get; set; }
        /// <summary>The inventory instance id of the equipped asset.</summary>
        public int instance_id { get; set; }
        /// <summary>When this item was equipped.</summary>
        public DateTime mounted_at { get; set; }
        /// <summary>Full asset details for the equipped item.</summary>
        public LootLockerCommonAsset asset { get; set; }
        /// <summary>Rental status for the equipped item, if it was acquired as a rental.</summary>
        public LootLockerRental rental { get; set; }
    }

    /// <summary>
    /// A default loadout entry for a class type, pairing an asset with its expected id and variation.
    /// </summary>
    public class LootLockerClassTypeDefaultLoadout 
    {
        /// <summary>The id of the asset that is part of the default loadout.</summary>
        public int asset_id { get; set; }
        /// <summary>The variation id of the asset to use in the default loadout.</summary>
        public int asset_variation_id { get; set; }
        /// <summary>Full asset details.</summary>
        public LootLockerCommonAsset asset { get; set; }
    }

    /// <summary>
    /// Response containing a list of available class types for this game.
    /// </summary>
    public class LootLockerListClassTypesResponse : LootLockerResponse
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("character_types")]
#else
        [Json(Name = "character_types")]
#endif
        /// <summary>The list of class types available for this game, deserialized from the "character_types" JSON field.</summary>
        public LootLockerClassTypes[] class_types { get; set; }
    }

    /// <summary>
    /// Describes a class type, including its identifier, name, default storage, and default loadout.
    /// </summary>
    public class LootLockerClassTypes
    {
        /// <summary>The unique integer id of this class type.</summary>
        public int id { get; set; }
        /// <summary>Whether this is the default class type.</summary>
        public bool is_default { get; set; }
        /// <summary>The display name of this class type.</summary>
        public string name { get; set; }
        /// <summary>Key-value storage entries attached to this class type.</summary>
        public LootLockerStorage[] storage { get; set; }
        /// <summary>The default loadout assets associated with this class type.</summary>
        public LootLockerClassTypeDefaultLoadout[] default_loadout { get; set; }
    }

    /// <summary>
    /// A single item in a class's equipped loadout, pairing an asset with its equip slot metadata.
    /// </summary>
    public class LootLockerLoadouts
    {
        /// <summary>The id of the asset variation equipped in this slot.</summary>
        public string variation_id { get; set; }
        /// <summary>The inventory instance id of the equipped asset.</summary>
        public int instance_id { get; set; }
        /// <summary>When this item was equipped.</summary>
        public DateTime mounted_at { get; set; }
        /// <summary>Full asset details for the equipped item.</summary>
        public LootLockerCommonAsset asset { get; set; }
    }

    /// <summary>
    /// Request to create a new class for the current player.
    /// </summary>
    public class LootLockerCreateClassRequest
    {
        /// <summary>Whether the newly created class should be set as the player's default class.</summary>
        public bool is_default { get; set; }
        /// <summary>The display name for the new class.</summary>
        public string name { get; set; }
        /// <summary>The id of the class type to use for the new class.</summary>
        public string character_type_id { get; set; }
    }

    /// <summary>
    /// Request to update an existing class for the current player.
    /// </summary>
    public class LootLockerUpdateClassRequest
    {
        /// <summary>Whether to set this class as the player's default class.</summary>
        public bool is_default { get; set; }
        /// <summary>The new display name for the class.</summary>
        public string name { get; set; }
    }

    /// <summary>
    /// Request to equip an asset to a class by its inventory instance id.
    /// </summary>
    public class LootLockerEquipByIDRequest
    {
        /// <summary>The inventory instance id of the asset to equip.</summary>
        public int instance_id { get; set; }
    }

    /// <summary>
    /// Request to equip an asset to a class by specifying the asset id and variation.
    /// </summary>
    public class LootLockerEquipByAssetRequest
    {
        /// <summary>The id of the asset to equip.</summary>
        public int asset_id { get; set; }
        /// <summary>The id of the asset variation to equip.</summary>
        public int asset_variation_id { get; set; }
    }

    /// <summary>
    /// Response containing the current loadout for one or more classes belonging to the player.
    /// </summary>
    public class LootLockerClassLoadoutResponse : LootLockerResponse
    {
        /// <summary>The list of class loadout entries returned by the endpoint.</summary>
        public LootLockerClassLoadout[] loadouts { get; set; }

        /// <summary>Returns the class object matching the given name, or null if not found.</summary>
        public LootLockerClass GetClass(string name)
        {
            LootLockerClass lootLockerCharacter = loadouts.FirstOrDefault(x => x.Class.name == name)?.Class;
            return lootLockerCharacter;
        }

        /// <summary>Returns all class objects from the loadout entries.</summary>
        public LootLockerClass[] GetClassess()
        {
            return loadouts.Select(x => x.Class).ToArray();
        }
    }

    /// <summary>
    /// Identifies a class belonging to a player, including its type and whether it is the default.
    /// </summary>
    public class LootLockerClass
    {
        /// <summary>The unique integer id of this class.</summary>
        public int id { get; set; }
        /// <summary>The class type name.</summary>
        public string type { get; set; }
        /// <summary>The display name of this class.</summary>
        public string name { get; set; }
        /// <summary>The ULID of this class.</summary>
        public string ulid { get; set; }
        /// <summary>Whether this is the player's default class.</summary>
        public bool is_default { get; set; }
    }


    /// <summary>
    /// A class and its associated equipped loadout items, as returned from class loadout endpoints.
    /// </summary>
    public class LootLockerClassLoadout
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("character")]
#else
        [Json(Name = "character")]
#endif
        /// <summary>The class associated with this loadout entry.</summary>
        public LootLockerClass Class { get; set; }
        /// <summary>The list of equipped loadout items for this class.</summary>
        public LootLockerLoadouts[] loadout { get; set; }
    }

    /// <summary>
    /// Response containing the class that received the equipped asset along with its updated loadout.
    /// </summary>
    public class EquipAssetToClassLoadoutResponse : LootLockerResponse
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("character")]
#else
        [Json(Name = "character")]
#endif
        /// <summary>The class that had the asset equipped.</summary>
        public LootLockerClass Class { get; set; }
        /// <summary>The updated list of equipped loadout items for this class.</summary>
        public LootLockerLoadouts[] loadout { get; set; }
        /// <summary>An error message, if the equip operation failed.</summary>
        public string error { get; set; }
    }

    /// <summary>
    /// Response containing a list of all classes belonging to the current player.
    /// </summary>
    public class LootLockerPlayerClassListResponse : LootLockerResponse
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("items")]
#else
        [Json(Name = "items")]
#endif
        /// <summary>The list of classes belonging to the current player.</summary>
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

        public static void DeleteClass(string forPlayerWithUlid, int classId, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deleteClass;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.WithPathParameter(classId), endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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

        public static void GetOtherPlayersClassLoadoutByUid(string forPlayerWithUlid, string playerUid, Action<LootLockerClassLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersClassLoadoutsByUid;

            string parameterizedEndpoint = endPoint.WithPathParameter(playerUid);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, parameterizedEndpoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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