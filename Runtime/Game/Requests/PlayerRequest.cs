using Newtonsoft.Json;
using System;
using LootLocker.Requests;

namespace LootLocker.Requests
{
    public class LootLockerGetPlayerInfoResponse : LootLockerResponse
    {
        public int? account_balance { get; set; }
        public int? xp { get; set; }
        public int? level { get; set; }
        public LootLockerLevel_Thresholds level_thresholds { get; set; }
    }

    [System.Serializable]
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

    [System.Serializable]
    public class LootLockerStandardResponse : LootLockerResponse
    {
    }

    [System.Serializable]
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
    
    [System.Serializable]
    public class Player1stPartyPlatformIDsLookupResponse : LootLockerResponse
    {
        public PlayerWith1stPartyPlatformIDs[] players { get; set; }
    }

    public class PlayerWith1stPartyPlatformIDs
    {
        public uint player_id { get; set; }
        public string player_public_uid { get; set; }
        public string name { get; set; }
        public string last_active_platform { get; set; }
        public PlatformIDs platform_ids { get; set; }
    }
    
    public class PlatformIDs
    {
        public ulong? steam_id { get; set; }
        public string xbox_id { get; set; }
        public ulong? psn_id { get; set; }
    }

    [System.Serializable]
    public class PlayerNameLookupResponse : LootLockerResponse
    {
        public PlayerNameWithIDs[] players { get; set; }
    }

    public class PlayerNameWithIDs : LootLockerResponse
    {
        public uint player_id { get; set; }
        public string player_public_uid { get; set; }
        public string name { get; set; }
        public string last_active_platform { get; set; }
        public string platform_player_id { get; set; }
    }

    [System.Serializable]
    public class PlayerNameResponse : LootLockerResponse
    {
        public string name { get; set; }
    }

    [System.Serializable]
    public class LootLockerDlcResponse : LootLockerResponse
    {
        public string[] dlcs { get; set; }
    }

    [System.Serializable]
    public class LootLockerDeactivatedAssetsResponse : LootLockerResponse
    {
        public LootLockerDeactivatedObjects[] objects { get; set; }
    }

    [System.Serializable]
    public class LootLockerDeactivatedObjects
    {
        public int deactivated_asset_id { get; set; }
        public int replacement_asset_id { get; set; }
        public string reason { get; set; }
    }


    [System.Serializable]
    public class LootLockerBalanceResponse : LootLockerResponse
    {
        public int? balance { get; set; }
    }

    [System.Serializable]
    public class LootLockerXpSubmitResponse : LootLockerResponse
    {
        public LootLockerXp xp { get; set; }
        public LootLockerLevel[] levels { get; set; }
        public bool check_grant_notifications { get; set; }
    }

    [System.Serializable]
    public class LootLockerXpResponse : LootLockerResponse
    {
        public int? xp { get; set; }
        public int? level { get; set; }
    }

    [System.Serializable]
    public class LootLockerXp
    {
        public int? previous { get; set; }
        public int? current { get; set; }
    }

    [System.Serializable]
    public class LootLockerLevel
    {
        public int? level { get; set; }
        public int? xp_threshold { get; set; }
    }

    [System.Serializable]
    public class LootLockerInventoryResponse : LootLockerResponse
    {
        public LootLockerInventory[] inventory;
    }

    public class LootLockerInventory
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public string rental_option_id { get; set; }
        public string acquisition_source { get; set; }
        public DateTime acquisition_date { get; set; }
        public LootLockerCommonAsset asset { get; set; }
        public LootLockerRental rental { get; set; }


        public float balance;
    }
    
    public class LootLockerPlayerFileRequest
    {
        public string purpose { get; set; }
        public string path_to_file { get; set; }
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
        [JsonProperty("public")]
        public bool is_public { get; set; }
        public string url { get; set; }
        public DateTime url_expires_at { get; set; }
        public DateTime created_at { get; set; }
    }

    [System.Serializable]
    public class LootLockerAssetClass
    {
        public string Asset { get; set; }
    }

    [System.Serializable]
    public class LootLockerRental
    {
        public bool is_rental { get; set; }
        public string time_left { get; set; }
        public string duration { get; set; }
        public string is_active { get; set; }
    }

    [System.Serializable]
    public class LootLockerXpSubmitRequest
    {
        public int? points;

        public LootLockerXpSubmitRequest(int points)
        {
            this.points = points;
        }
    }

    [System.Serializable]
    public class LootLockerXpRequest : LootLockerGetRequest
    {
        public LootLockerXpRequest()
        {
            getRequests.Clear();
            getRequests.Add(LootLockerConfig.current.deviceID);
            getRequests.Add(CurrentPlatform.GetString());
        }
    }

    public class LootLockerPlayerAssetNotificationsResponse : LootLockerResponse
    {
        public LootLockerRewardObject[] objects { get; set; }
    }

    public class LootLockerRewardObject
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public string acquisition_source { get; set; }
        public LootLockerCommonAsset asset { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetPlayerInfo(Action<LootLockerGetPlayerInfoResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getPlayerInfo;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }
        public static void GetOtherPlayerInfo(LootLockerOtherPlayerInfoRequest data, Action<LootLockerXpResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getXpAndLevel;
            var getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }
        public static void GetInventory(Action<LootLockerInventoryResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getInventory;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void GetBalance(Action<LootLockerBalanceResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getCurrencyBalance;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void SubmitXp(LootLockerXpSubmitRequest data, Action<LootLockerXpSubmitResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            var endPoint = LootLockerEndPoints.submitXp;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void GetXpAndLevel(LootLockerGetRequest data, Action<LootLockerXpResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getXpAndLevel;

            var getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void GetPlayerAssetNotification(Action<LootLockerPlayerAssetNotificationsResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.playerAssetNotifications;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void GetDeactivatedAssetNotification(Action<LootLockerDeactivatedAssetsResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.playerAssetDeactivationNotification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void InitiateDLCMigration(Action<LootLockerDlcResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.initiateDlcMigration;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void GetDLCMigrated(Action<LootLockerDlcResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getDlcMigration;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void SetProfilePrivate(Action<LootLockerStandardResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.setProfilePrivate;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void SetProfilePublic(Action<LootLockerStandardResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.setProfilePublic;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void GetPlayerName(Action<PlayerNameResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getPlayerName;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

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

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
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

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void SetPlayerName(PlayerNameRequest data, Action<PlayerNameResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            var endPoint = LootLockerEndPoints.setPlayerName;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }
    }
}