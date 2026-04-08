using System;
using LootLocker.Requests;
#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
#else
using LLlibs.ZeroDepJson;
#endif

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// First-party platform identifiers (Steam, Xbox, PSN) linked to a player's account.
    /// </summary>
    public class PlatformIDs
    {
        /// <summary>The player's Steam id, or null if not linked.</summary>
        public ulong? steam_id { get; set; }
        /// <summary>The player's Xbox id, or null if not linked.</summary>
        public string xbox_id { get; set; }
        /// <summary>The player's PSN id, or null if not linked.</summary>
        public ulong? psn_id { get; set; }
    }

    /// <summary>
    /// Player information combined with all linked first-party platform identifiers.
    /// </summary>
    public class PlayerWith1stPartyPlatformIDs
    {
        /// <summary>The legacy integer id of the player.</summary>
        public uint player_id { get; set; }
        /// <summary>The public UID of the player.</summary>
        public string player_public_uid { get; set; }
        /// <summary>The display name of the player.</summary>
        public string name { get; set; }
        /// <summary>The platform the player was last active on.</summary>
        public string last_active_platform { get; set; }
        /// <summary>The first-party platform identifiers linked to this player.</summary>
        public PlatformIDs platform_ids { get; set; }
    }

    /// <summary>
    /// Player name and various identifier forms used for player lookup operations.
    /// </summary>
    public class PlayerNameWithIDs
    {
        /// <summary>The legacy integer id of the player.</summary>
        public uint player_id { get; set; }
        /// <summary>The public UID of the player.</summary>
        public string player_public_uid { get; set; }
        /// <summary>The ULID of the player.</summary>
        public string ulid { get; set; }
        /// <summary>The display name of the player.</summary>
        public string name { get; set; }
        /// <summary>The platform the player was last active on.</summary>
        public string last_active_platform { get; set; }
        /// <summary>The platform-specific player identifier on the last active platform.</summary>
        public string platform_player_id { get; set; }
    }

    /// <summary>
    /// A deactivated asset and its replacement, used to notify the player that an asset they own has been retired.
    /// </summary>
    [Serializable]
    public class LootLockerDeactivatedObjects
    {
        /// <summary>The id of the asset that was deactivated.</summary>
        public int deactivated_asset_id { get; set; }
        /// <summary>The id of the replacement asset, if one is available.</summary>
        public int replacement_asset_id { get; set; }
        /// <summary>The reason the asset was deactivated.</summary>
        public string reason { get; set; }
    }

    /// <summary>
    /// A helper class used internally to represent an asset class name.
    /// </summary>
    [Serializable]
    public class LootLockerAssetClass
    {
        /// <summary>The asset class name.</summary>
        public string Asset { get; set; }
    }

    /// <summary>
    /// A simplified view of an inventory item
    /// </summary>
    public class LootLockerSimpleInventoryItem
    {
        /// <summary>
        /// The asset id of the inventory item
        /// </summary>
        public int asset_id { get; set; }
        /// <summary>
        /// The asset ulid of the inventory item
        /// </summary>
        public string asset_ulid { get; set; }
        /// <summary>
        /// The name of the asset
        /// </summary>
        public string asset_name { get; set; }
        /// <summary>
        /// The instance id of the inventory item
        /// </summary>
        public int instance_id { get; set; }
        /// <summary>
        /// The ulid of the inventory item
        /// </summary>
        public string ulid { get; set; }
        /// <summary>
        /// The acquisition source of the inventory item
        /// </summary>
        public string acquisition_source { get; set; }
        /// <summary>
        /// The acquisition date of the inventory item
        /// </summary>
        public DateTime? acquisition_date { get; set; }
        /// <summary>
        /// Metadata entries for this inventory item when requested
        /// </summary>
        public LootLockerMetadataEntry[] metadata { get; set; }
    }

    /// <summary>
    /// A reward object linking an inventory instance to the underlying asset details.
    /// </summary>
    public class LootLockerRewardObject
    {
        /// <summary>The inventory instance id of the reward item.</summary>
        public int instance_id { get; set; }
        /// <summary>The variation id of the reward item, or null for the default variation.</summary>
        public int? variation_id { get; set; }
        /// <summary>The source through which this reward was acquired.</summary>
        public string acquisition_source { get; set; }
        /// <summary>Full asset details for the reward item.</summary>
        public LootLockerCommonAsset asset { get; set; }
    }

    /// <summary>
    /// A set of important information about a player
    /// </summary>
    public class LootLockerPlayerInfo
    {
        /// <summary>
        /// When this player was first created
        /// </summary>
        public DateTime created_at { get; set; }
        /// <summary>
        /// The last time this player logged in
        /// </summary>
        public DateTime last_seen { get; set; }
        /// <summary>
        /// The name of the player expressly configured through a SetPlayerName call
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The public uid of the player. This id is in the form of a UID
        /// </summary>
        public string public_uid { get; set; }
        /// <summary>
        /// The legacy id of the player. This id is in the form of an integer and are sometimes called simply player_id or id
        /// </summary>
        public int legacy_id { get; set; }
        /// <summary>
        /// The id of the player. This id is in the form a ULID and is sometimes called player_ulid or similar
        /// </summary>
        public string id { get; set; }
    }

    //==================================================
    // Request Definitions
    //==================================================
    /// <summary>
    /// Request to set or update the name of the current player.
    /// </summary>
    [Serializable]
    public class PlayerNameRequest
    {
        /// <summary>The new name to assign to the player.</summary>
        public string name { get; set; }
    }

    /// <summary>
    /// Request to look up the first-party platform ids for multiple players identified by their legacy ids or public UIDs.
    /// </summary>
    public class LookupPlayer1stPartyPlatformIDsRequest
    {
        public ulong[] player_ids { get; set; }
        public string[] player_public_uids { get; set; }

        public LookupPlayer1stPartyPlatformIDsRequest()
        {
            player_ids = new ulong[] { };
            player_public_uids = new string[] { };
        }
    }

    /// <summary>
    /// Request to look up basic info for multiple players identified by their ULID, legacy id, or public UID.
    /// </summary>
    public class LootLockerListPlayerInfoRequest
    {
        /// <summary>
        /// A list of ULID ids of players to look up. These ids are in the form of ULIDs and are sometimes called player_ulid or similar
        /// </summary>
        public string[] player_id { get; set; }
        /// <summary>
        /// A list of legacy ids of players to look up. These ids are in the form of integers and are sometimes called simply player_id or id
        /// </summary>
        public int[] player_legacy_id { get; set; }
        /// <summary>
        /// A list of public uids to look up. These ids are in the form of UIDs
        /// </summary>
        public string[] player_public_uid { get; set; }
    }

    /// <summary>
    /// Request to list a player's simplified inventory with the given filters
    /// </summary>
    public class LootLockerListSimplifiedFilters
    {
        /// <summary>
        /// A list of asset ids to filter the inventory items by
        /// </summary>
        public int[] asset_ids { get; set; } = System.Array.Empty<int>();
        /// <summary>
        /// A list of context ids to filter the inventory items by
        /// </summary>
        public int[] context_ids { get; set; } = System.Array.Empty<int>();
    }

    /// <summary>
    /// What metadata to include in simplified inventory responses when requested by the client. If no keys are specified, all metadata will be included when metadata is requested in the includes section of the request
    /// </summary>
    public class LootLockerListSimplifiedInventoryMetadataIncludes
    {
        /// <summary>
        /// A list of metadata keys to include in the response when metadata is requested. If this list is empty, all metadata will be included when metadata is requested in the includes section of the request
        /// </summary>
        public string[] keys { get; set; } = System.Array.Empty<string>();
        /// <summary>
        /// A boolean value indicating whether all metadata should be included in the response when metadata is requested. This will be true if the keys list is empty, and false if the keys list contains any entries. This value is ignored if metadata is not requested in the includes section of the request
        /// </summary>
        public bool all { get { return keys.Length == 0; } set { } }
    }

    /// <summary>
    /// Includes to add extra data to simplified inventory responses
    /// </summary>
    public class LootLockerListSimplifiedInventoryIncludes
    {
        /// <summary>
        /// Whether to include metadata for inventory items in the response. If true, metadata will be included according to the keys specified in the metadata includes section of the request. If false, no metadata will be included for inventory items in the response
        /// </summary>
        public LootLockerListSimplifiedInventoryMetadataIncludes metadata { get; set; } = null;
    }

    /// <summary>
    /// Request to list a player's simplified inventory with the given filters
    /// </summary>
    public class LootLockerListSimplifiedInventoryRequest
    {
        /// <summary>
        /// Includes for the simplified inventory response
        /// </summary>
        public LootLockerListSimplifiedInventoryIncludes includes { get; set; } = new LootLockerListSimplifiedInventoryIncludes();
        /// <summary>
        /// The filters to apply to the inventory listing. If null, no filters will be applied and the full inventory will be returned
        /// </summary>
        public LootLockerListSimplifiedFilters filters { get; set; } = new LootLockerListSimplifiedFilters();
    }

    //==================================================
    // Response Definitions
    //==================================================
    /// <summary>
    /// A base response type for operations that return no payload beyond success or error state.
    /// </summary>
    [Serializable]
    public class LootLockerStandardResponse : LootLockerResponse
    {
    }
    
    /// <summary>
    /// Response containing a list of players and their linked first-party platform identifiers.
    /// </summary>
    [Serializable]
    public class Player1stPartyPlatformIDsLookupResponse : LootLockerResponse
    {
        /// <summary>The list of players with their first-party platform ids.</summary>
        public PlayerWith1stPartyPlatformIDs[] players { get; set; }
    }

    /// <summary>
    /// Response containing a list of players with their names and various identifier forms.
    /// </summary>
    [Serializable]
    public class PlayerNameLookupResponse : LootLockerResponse
    {
        /// <summary>The list of matched players with their names and identifiers.</summary>
        public PlayerNameWithIDs[] players { get; set; }
    }

    /// <summary>
    /// Response containing the current player's display name.
    /// </summary>
    [Serializable]
    public class PlayerNameResponse : LootLockerResponse
    {
        /// <summary>The current display name of the player.</summary>
        public string name { get; set; }
    }

    /// <summary>
    /// Response containing the list of DLC package identifiers owned by the current player.
    /// </summary>
    [Serializable]
    public class LootLockerDlcResponse : LootLockerResponse
    {
        /// <summary>The list of DLC identifiers owned by the player.</summary>
        public string[] dlcs { get; set; }
    }

    /// <summary>
    /// Response containing assets that have been deactivated and their replacements, if any.
    /// </summary>
    [Serializable]
    public class LootLockerDeactivatedAssetsResponse : LootLockerResponse
    {
        /// <summary>The list of deactivated asset entries.</summary>
        public LootLockerDeactivatedObjects[] objects { get; set; }
    }

    /// <summary>
    /// Response containing the current player's currency balance.
    /// </summary>
    [Serializable]
    public class LootLockerBalanceResponse : LootLockerResponse
    {
        /// <summary>The player's current currency balance, or null if unavailable.</summary>
        public int? balance { get; set; }
    }

    /// <summary>
    /// The response class for simplified inventory requests
    /// </summary>
    [Serializable]
    public class LootLockerSimpleInventoryResponse : LootLockerResponse
    {
        /// <summary>
        /// List of simplified inventory items according to the requested filters
        /// </summary>
        public LootLockerSimpleInventoryItem[] items { get; set; }
        /// <summary>
        /// Pagination information for the response
        /// </summary>
        public LootLockerExtendedPagination pagination { get; set; }
    }
    
    /// <summary>
    /// Response containing a list of files attached to the player's account.
    /// </summary>
    public class LootLockerPlayerFilesResponse : LootLockerResponse
    {
        /// <summary>The list of player files.</summary>
        public LootLockerPlayerFile[] items { get; set; }
    }

    /// <summary>
    /// A file stored on a player's account, with download URL, metadata, and expiration information.
    /// </summary>
    public class LootLockerPlayerFile : LootLockerResponse
    {
        /// <summary>The unique identifier of this player file.</summary>
        public int id { get; set; }
        /// <summary>The revision id for this file version.</summary>
        public string revision_id { get; set; }
        /// <summary>The file name.</summary>
        public string name { get; set; }
        /// <summary>The file size in bytes.</summary>
        public int size { get; set; }
        /// <summary>The purpose or category tag for this file.</summary>
        public string purpose { get; set; }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("public")]
#else
        [Json(Name = "public")]
#endif
        /// <summary>Whether this file is publicly accessible.</summary>
        public bool is_public { get; set; }
        /// <summary>The signed URL to download this file.</summary>
        public string url { get; set; }
        /// <summary>When the signed URL expires.</summary>
        public DateTime url_expires_at { get; set; }
        /// <summary>When this file was created.</summary>
        public DateTime created_at { get; set; }
    }

    /// <summary>
    /// Response containing asset reward notifications for the current player.
    /// </summary>
    public class LootLockerPlayerAssetNotificationsResponse : LootLockerResponse
    {
        /// <summary>The list of asset reward objects the player has been notified about.</summary>
        public LootLockerRewardObject[] objects { get; set; }
    }

    /// <summary>
    /// Response containing key information about the currently logged-in player.
    /// </summary>
    public class LootLockerGetCurrentPlayerInfoResponse : LootLockerResponse
    {
        /// <summary>
        /// Important player information for the currently logged in player
        /// </summary>
        public LootLockerPlayerInfo info { get; set; }
    }

    /// <summary>
    /// Response containing key information about one or more looked-up players.
    /// </summary>
    public class LootLockerListPlayerInfoResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of important player information for the successfully looked up players
        /// </summary>
        public LootLockerPlayerInfo[] info { get; set; }
    }
}

//==================================================
// API Class Definition
//==================================================
namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void LookupPlayerNames(string forPlayerWithUlid, string idType, string[] identifiers, Action<PlayerNameLookupResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.lookupPlayerNames;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            foreach (string identifier in identifiers)
            {
                queryParams.Add(idType, identifier);
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint + queryParams.Build(), endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        
        public static void LookupPlayer1stPartyPlatformIDs(string forPlayerWithUlid, LookupPlayer1stPartyPlatformIDsRequest lookupPlayer1stPartyPlatformIDsRequest, Action<Player1stPartyPlatformIDsLookupResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.lookupPlayer1stPartyPlatformIDs;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();

            foreach (var playerID in lookupPlayer1stPartyPlatformIDsRequest.player_ids)
            {
                queryParams.Add("player_id", playerID);
            }

            foreach (var playerPublicUID in lookupPlayer1stPartyPlatformIDsRequest.player_public_uids)
            {
                queryParams.Add("player_public_uid", playerPublicUID);
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint + queryParams.Build(), endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}
