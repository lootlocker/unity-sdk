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
    public class LootLockerLeaderboardHistoryDetailsResponse : LootLockerResponse
    {
        public LootLockerPaginationResponse<string> pagination { get; set; }
        public LootLockerLeaderboardHistoryDetails[] items { get; set; }
    }
    public class LootLockerLeaderboardHistoryDetails
    {
        public LootLockerLeaderBoardPlayer player { get; set; }
        public string metadata { get; set; }
        public string member_id { get; set; }
        public int rank { get; set; }
        public int score { get; set; }

    }
    public class LootLockerLeaderBoardPlayer
    {
        public string name { get; set; }
        public string public_uid { get; set; }
        public int id { get; set; }
        public string player_ulid { get; set; }

    }
    [Serializable]
    public class LootLockerLeaderboardArchiveRequest
    {
        public string key { get; set; }
        public int count { get; set; }
        public string after { get; set; }

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
        public static void GetLeaderboardArchive(string key, int count, string after, Action<LootLockerLeaderboardHistoryDetailsResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPoints.getLeaderboardArchive;

            endPoint.endPoint += $"?key={key}&";
            if (count > 0)
                endPoint.endPoint += $"count={count}&";

            if (!string.IsNullOrEmpty(after))
                endPoint.endPoint += $"after={after}&";


            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, ((serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }));

        }
    }
}