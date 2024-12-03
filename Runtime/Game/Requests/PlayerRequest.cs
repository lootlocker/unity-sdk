using System;
using LootLocker.Requests;
#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
#else
using LLlibs.ZeroDepJson;
#endif

namespace LootLocker.Requests
{
    #region DEPRECATED Legacy Player Info
    public class LootLockerGetPlayerInfoResponse : LootLockerResponse
    {
        public int? account_balance { get; set; }
        public int? xp { get; set; }
        public int? level { get; set; }
        public string ulid { get; set; }
        public LootLockerLevel_Thresholds level_thresholds { get; set; }
    }

    [Serializable]
    public class LootLockerOtherPlayerInfoRequest : LootLockerGetRequest
    {
        public LootLockerOtherPlayerInfoRequest(string playerID, string platform = "")
        {
            getRequests.Add(playerID);
            if (platform != "")
            {
                getRequests.Add(platform);
            }
        }
    }
    #endregion


    #region DEPRECATED Legacy Progressions
    [Serializable]
    public class LootLockerXpSubmitResponse : LootLockerResponse
    {
        public LootLockerXp xp { get; set; }
        public LootLockerLevel[] levels { get; set; }
        public bool check_grant_notifications { get; set; }
    }

    [Serializable]
    public class LootLockerXpResponse : LootLockerResponse
    {
        public int? xp { get; set; }
        public int? level { get; set; }
    }

    [Serializable]
    public class LootLockerXp
    {
        public int? previous { get; set; }
        public int? current { get; set; }
    }

    [Serializable]
    public class LootLockerLevel
    {
        public int? level { get; set; }
        public int? xp_threshold { get; set; }
    }
    [Serializable]
    public class LootLockerXpSubmitRequest
    {
        public int? points;

        public LootLockerXpSubmitRequest(int points)
        {
            this.points = points;
        }
    }

    [Serializable]
    public class LootLockerXpRequest : LootLockerGetRequest
    {
        public LootLockerXpRequest()
        {
            getRequests.Clear();
            getRequests.Add(LootLockerConfig.current.deviceID);
            getRequests.Add(CurrentPlatform.GetString());
        }
    }
    #endregion

    //==================================================
    // Data Definitions
    //==================================================

    public class PlatformIDs
    {
        public ulong? steam_id { get; set; }
        public string xbox_id { get; set; }
        public ulong? psn_id { get; set; }
    }

    public class PlayerWith1stPartyPlatformIDs
    {
        public uint player_id { get; set; }
        public string player_public_uid { get; set; }
        public string name { get; set; }
        public string last_active_platform { get; set; }
        public PlatformIDs platform_ids { get; set; }
    }

    public class PlayerNameWithIDs
    {
        public uint player_id { get; set; }
        public string player_public_uid { get; set; }
        public string player_ulid { get; set; }
        public string name { get; set; }
        public string last_active_platform { get; set; }
        public string platform_player_id { get; set; }
    }

    [Serializable]
    public class LootLockerDeactivatedObjects
    {
        public int deactivated_asset_id { get; set; }
        public int replacement_asset_id { get; set; }
        public string reason { get; set; }
    }

    [Serializable]
    public class LootLockerAssetClass
    {
        public string Asset { get; set; }
    }

    [Serializable]
    public class LootLockerRental
    {
        public bool is_rental { get; set; }
        public string time_left { get; set; }
        public string duration { get; set; }
        public string is_active { get; set; }
    }

    public class LootLockerInventory
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public string rental_option_id { get; set; }
        public string acquisition_source { get; set; }
        public DateTime? acquisition_date { get; set; }
        public LootLockerCommonAsset asset { get; set; }
        public LootLockerRental rental { get; set; }


        public float balance { get; set; }
    }

    public class LootLockerRewardObject
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public string acquisition_source { get; set; }
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
    [Serializable]
    public class PlayerNameRequest
    {
        public string name { get; set; }
    }

    public class LookupPlayerNamesRequest
    {
        public ulong[] player_ids { get; set; }
        public string[] player_public_uids { get; set; }
        public ulong[] steam_ids { get; set; }
        public ulong[] psn_ids { get; set; }
        public string[] xbox_ids { get; set; }

        public LookupPlayerNamesRequest()
        {
            player_ids = new ulong[] { };
            player_public_uids = new string[] { };
            steam_ids = new ulong[] { };
            psn_ids = new ulong[] { };
            xbox_ids = new string[] { };
        }
    }

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

    public class LootLockerPlayerFileRequest
    {
        public string purpose { get; set; }
        public string path_to_file { get; set; }
    }

    /// <summary>
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

    //==================================================
    // Response Definitions
    //==================================================
    [Serializable]
    public class LootLockerStandardResponse : LootLockerResponse
    {
    }
    
    [Serializable]
    public class Player1stPartyPlatformIDsLookupResponse : LootLockerResponse
    {
        public PlayerWith1stPartyPlatformIDs[] players { get; set; }
    }

    [Serializable]
    public class PlayerNameLookupResponse : LootLockerResponse
    {
        public PlayerNameWithIDs[] players { get; set; }
    }

    [Serializable]
    public class PlayerNameResponse : LootLockerResponse
    {
        public string name { get; set; }
    }

    [Serializable]
    public class LootLockerDlcResponse : LootLockerResponse
    {
        public string[] dlcs { get; set; }
    }

    [Serializable]
    public class LootLockerDeactivatedAssetsResponse : LootLockerResponse
    {
        public LootLockerDeactivatedObjects[] objects { get; set; }
    }

    [Serializable]
    public class LootLockerBalanceResponse : LootLockerResponse
    {
        public int? balance { get; set; }
    }

    [Serializable]
    public class LootLockerInventoryResponse : LootLockerResponse
    {
        public LootLockerInventory[] inventory { get; set; }
    }
    
    public class LootLockerPlayerFilesResponse : LootLockerResponse
    {
        public LootLockerPlayerFile[] items { get; set; }
    }

    public class LootLockerPlayerFile : LootLockerResponse
    {
        public int id { get; set; }
        public string revision_id { get; set; }
        public string name { get; set; }
        public int size { get; set; }
        public string purpose { get; set; }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("public")]
#else
        [Json(Name = "public")]
#endif
        public bool is_public { get; set; }
        public string url { get; set; }
        public DateTime url_expires_at { get; set; }
        public DateTime created_at { get; set; }
    }

    public class LootLockerPlayerAssetNotificationsResponse : LootLockerResponse
    {
        public LootLockerRewardObject[] objects { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerGetCurrentPlayerInfoResponse : LootLockerResponse
    {
        /// <summary>
        /// Important player information for the currently logged in player
        /// </summary>
        public LootLockerPlayerInfo info { get; set; }
    }

    /// <summary>
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
        public static void LookupPlayerNames(LookupPlayerNamesRequest lookupPlayerNamesRequest, Action<PlayerNameLookupResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.lookupPlayerNames;

            var getVariable = endPoint.endPoint + "?";

            foreach (var playerID in lookupPlayerNamesRequest.player_ids)
            {
                getVariable += $"player_id={playerID}&";
            }

            foreach (var playerPublicUID in lookupPlayerNamesRequest.player_public_uids)
            {
                getVariable += $"player_public_uid={playerPublicUID}&";
            }

            foreach (var steamID in lookupPlayerNamesRequest.steam_ids)
            {
                getVariable += $"steam_id={steamID}&";
            }

            foreach (var psnID in lookupPlayerNamesRequest.psn_ids)
            {
                getVariable += $"psn_id={psnID}&";
            }

            foreach (var xboxID in lookupPlayerNamesRequest.xbox_ids)
            {
                getVariable += $"xbox_id={xboxID}&";
            }

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        
        public static void LookupPlayer1stPartyPlatformIDs(LookupPlayer1stPartyPlatformIDsRequest lookupPlayer1stPartyPlatformIDsRequest, Action<Player1stPartyPlatformIDsLookupResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.lookupPlayer1stPartyPlatformIDs;

            var getVariable = endPoint.endPoint + "?";

            foreach (var playerID in lookupPlayer1stPartyPlatformIDsRequest.player_ids)
            {
                getVariable += $"player_id={playerID}&";
            }

            foreach (var playerPublicUID in lookupPlayer1stPartyPlatformIDsRequest.player_public_uids)
            {
                getVariable += $"player_public_uid={playerPublicUID}&";
            }

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}