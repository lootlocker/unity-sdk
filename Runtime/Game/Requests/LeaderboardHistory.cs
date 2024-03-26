using System;
using LootLocker.Requests;


namespace LootLocker.Requests
{

    public class LootLockerLeaderboardHistoryResponse : LootLockerResponse
    {
        public LootLockerLeaderboardArchive[] archives { get; set; }
    }

    public class LootLockerLeaderboardArchive
    {
        public string last_modified { get; set; }
        public string content_type { get; set; }
        public string key { get; set; }
        public int content_length { get; set; }
    }


}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void ListLeaderboardArchive(string leaderboard_key, Action<LootLockerLeaderboardHistoryResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listLeaderboardArchive;
            string tempEndpoint = string.Format(endPoint.endPoint, leaderboard_key);
            LootLockerServerRequest.CallAPI(tempEndpoint, endPoint.httpMethod, null, ((serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }));
        }
    }
}