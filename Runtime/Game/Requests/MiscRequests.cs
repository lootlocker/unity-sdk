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
    }
}
