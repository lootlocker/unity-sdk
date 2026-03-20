using System;
using LootLocker.Requests;

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================
    /// <summary>
    /// Information about the currently configured game in LootLocker.
    /// </summary>
    public class LootLockerGameInfo
    {
        // The title ID of the game (uniquely identifies the game in LootLocker)
        public string title_id { get; set; }
        // The environment ID of the game (identifies which environment instance of the title this game refers to in LootLocker)
        public string environment_id { get; set; }
        // The id of the game (uniquely identifies the game in LootLocker)
        public int game_id { get; set; }
        // The name of the game as configured in LootLocker
        public string name { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================
    public class LootLockerPingResponse : LootLockerResponse
    {
        public string date { get; set; }
    }

    /// <summary>
    /// Represents a response from the LootLocker API containing game information.
    /// </summary>
    public class LootLockerGameInfoResponse : LootLockerResponse
    {
        public LootLockerGameInfo info { get; set; }
    }

    //==================================================
    // Request Definitions
    //==================================================
    /// <summary>
    /// Represents a request to get game information from the LootLocker API.
    /// </summary>
    [Serializable]
    public class LootLockerGameInfoRequest
    {
        public string api_key { get; set; }
    };

    /// <summary>
    /// Represents a request to get game version information from the LootLocker API.
    /// </summary>
    [Serializable]
    public class LootLockerGameVersionRequest
    {
        public string api_key { get; set; }
    };

    //==================================================
    // Data Definitions (Game Version)
    //==================================================
    /// <summary>
    /// Represents the current version details for a game in LootLocker.
    /// </summary>
    public class LootLockerGameVersionCurrentVersion
    {
        // The numerical version number of the current version
        public int numerical_version { get; set; }
        // The ULID version identifier of the current version
        public string version_id { get; set; }
        // The human-readable name of the current version
        public string version_name { get; set; }
    }

    /// <summary>
    /// Represents an entry in the list of game versions for a game in LootLocker.
    /// </summary>
    public class LootLockerGameVersionEntry
    {
        // The numerical version number of this version
        public int numerical_version { get; set; }
        // The version identifier of this version
        public string version_id { get; set; }
        // The human-readable name of this version
        public string version_name { get; set; }
        // The version identifier for the version preceding this one
        public string previous_version_id { get; set; }
        // The version identifier for the version following this one
        public string next_version_id { get; set; }
    }

    //==================================================
    // Response Definitions (Game Version)
    //==================================================
    /// <summary>
    /// Represents a response from the LootLocker API containing game version information.
    /// </summary>
    public class LootLockerGameVersionResponse : LootLockerResponse
    {
        // The current version of the game
        public LootLockerGameVersionCurrentVersion current_version { get; set; }
        // All versions of the game
        public LootLockerGameVersionEntry[] versions { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void Ping(string forPlayerWithUlid, Action<LootLockerPingResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.ping;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetGameInfo(Action<LootLockerGameInfoResponse> onComplete)
        {
            string body = LootLockerJson.SerializeObject(new LootLockerGameInfoRequest { api_key = LootLockerConfig.current.apiKey });
            LootLockerServerRequest.CallAPI("", LootLockerEndPoints.gameInfo.endPoint, LootLockerEndPoints.gameInfo.httpMethod, body, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        public static void GetGameVersion(Action<LootLockerGameVersionResponse> onComplete)
        {
            string body = LootLockerJson.SerializeObject(new LootLockerGameVersionRequest { api_key = LootLockerConfig.current.apiKey });
            LootLockerServerRequest.CallAPI("", LootLockerEndPoints.gameVersion.endPoint, LootLockerEndPoints.gameVersion.httpMethod, body, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false);
        }
    }
}
