using System;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================
    public class LootLockerLeaderboardDetails
    {
        /// <summary>
        /// The date the Leaderboard was created.
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// The date the Leaderboard was last updated.
        /// </summary>
        public string updated_at { get; set; }
        /// <summary>
        /// The Leaderboards Key.
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// The Ulid of the leaderboard.
        /// </summary>
        public string ulid { get; set; }
        /// <summary>
        /// The direction of the Leaderboard (Ascending / Descending).
        /// </summary>
        public string direction_method { get; set; }
        /// <summary>
        /// The name of the Leaderboard.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The type of the Leaderboard (Player / Generic).
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// Will the score be overwritten even if it was less than the original score.
        /// </summary>
        public bool overwrite_score_on_submit { get; set; }
        /// <summary>
        /// Does the Leaderboard have metadata.
        /// </summary>
        public bool has_metadata { get; set; }
        /// <summary>
        /// Schedule of the Leaderboard.
        /// </summary>
        public LootLockerLeaderboardSchedule schedule { get; set; }
        /// <summary>
        /// A List of rewards tied to the Leaderboard.
        /// </summary>
        public LootLockerLeaderboardReward[] rewards { get; set; }
    }

    public class LootLockerPlayer
    {
        public int id { get; set; }
        public string public_uid { get; set; }
        public string name { get; set; }
        public string ulid { get; set; }
    }

    public class LootLockerLeaderboardMember
    {
        public string member_id { get; set; }
        public int rank { get; set; }
        public int score { get; set; }
        public LootLockerPlayer player { get; set; }
        public string metadata { get; set; }
    }

    public class LootLockerLeaderboard
    {
        public LootLockerLeaderboardMember rank { get; set; }
        public int leaderboard_id { get; set; }
        public string leaderboard_key { get; set; }
        public string ulid { get; set; }
    }
    
    public class LootLockerLeaderboardArchive
    {
        /// <summary>
        /// The date when the archived Leaderboard was modified.
        /// </summary>
        public string last_modified { get; set; }
        /// <summary>
        /// The type of content (application/json).
        /// </summary>
        public string content_type { get; set; }
        /// <summary>
        /// The Key which is used to identify a json body of an old Leaderboard.
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// Length of the archived Leaderboard.
        /// </summary>
        public int content_length { get; set; }
    }

    public class LootLockerLeaderboardArchiveDetails
    {
        /// <summary>
        /// The Player on the archived Leaderboard.
        /// </summary>
        public LootLockerLeaderBoardPlayer player { get; set; }
        /// <summary>
        /// Metadata if any was supplied.
        /// </summary>
        public string metadata { get; set; }
        /// <summary>
        /// The Player's member ID on the Archived Leaderboard.
        /// </summary>
        public string member_id { get; set; }
        /// <summary>
        /// The Player's rank on the archived Leaderboard.
        /// </summary>
        public int rank { get; set; }
        /// <summary>
        /// The Player's Score on the archived Leaderboard.
        /// </summary>
        public int score { get; set; }
    }

    public class LootLockerPagination
    {
        public int total { get; set; }
        public int? next_cursor { get; set; }
        public int? previous_cursor { get; set; }
        public bool allowNext { get; set; }
        public bool allowPrev { get; set; }
    }

    public class LootLockerLeaderBoardPlayer
    {
        /// <summary>
        /// The name of the Player.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The Public UID of the Player.
        /// </summary>
        public string public_uid { get; set; }
        /// <summary>
        /// The ID of the Player.
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// The ULID of the Player.
        /// </summary>
        public string player_ulid { get; set; }

    }


    //==================================================
    // Request Definitions
    //==================================================
    public class LootLockerGetRequests
    {
        public int count { get; set; }
        public string after { get; set; }
    }

    public class LootLockerSubmitScoreRequest
    {
        public string member_id { get; set; }
        public int score { get; set; }
        public string metadata { get; set; }
    }

    [Serializable]
    public class LootLockerLeaderboardArchiveRequest
    {
        /// <summary>
        /// The identifying Key of an archived leaderboard.
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// Count of entries to read.
        /// </summary>
        public int count { get; set; }
        /// <summary>
        /// After specified index.
        /// </summary>
        public string after { get; set; }
    }

    public class LootLockerGetMemberRankRequest
    {
        public string leaderboardId { get; set; }
        public string member_id { get; set; }
    }

    public class LootLockerGetScoreListRequest : LootLockerGetRequests
    {
        public string leaderboardKey { get; set; }
        public static int? nextCursor { get; set; }
        public static int? prevCursor { get; set; }

        public static void Reset()
        {
            nextCursor = 0;
            prevCursor = 0;
        }
    }

    public class LootLockerGetAllMemberRanksRequest : LootLockerGetRequests
    {
        public int member_id { get; set; }
        public static int? nextCursor { get; set; }
        public static int? prevCursor { get; set; }

        public static void Reset()
        {
            nextCursor = 0;
            prevCursor = 0;
        }
    }

    public class LootLockerGetByListMembersRequest
    {
        public string[] members { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    // <summary>
    // </summary>
    public class LootLockerListLeaderboardsResponse : LootLockerResponse
    {
        /// <summary>
        /// Pagination data to use for subsequent requests
        /// </summary>
        public LootLockerPaginationResponse<int> pagination { get; set; }

        // <summary>
        // List of details for the requested leaderboards
        // </summary>
        public LootLockerLeaderboardDetails[] items { get; set; }
    }

    public class LootLockerGetMemberRankResponse : LootLockerResponse
    {
        // we are doing thisfor legacy reasons, since it is no longer being set on the backend
        public string member_id { get; set; }
        public int rank { get; set; }
        public int score { get; set; }
        public LootLockerPlayer player { get; set; }
        public string metadata { get; set; }
    }


    public class LootLockerGetByListOfMembersResponse : LootLockerResponse
    {
        public LootLockerLeaderboardMember[] members { get; set; }
    }

    public class LootLockerGetScoreListResponse : LootLockerResponse
    {
        public LootLockerPagination pagination { get; set; }
        public LootLockerLeaderboardMember[] items { get; set; }
    }

    public class LootLockerGetAllMemberRanksResponse : LootLockerResponse
    {
        public LootLockerLeaderboard[] leaderboards { get; set; }
        public LootLockerPagination pagination { get; set; }
    }

    public class LootLockerSubmitScoreResponse : LootLockerResponse
    {
        public string member_id { get; set; }
        public int rank { get; set; }
        public int score { get; set; }
        public string metadata { get; set; }
    }

    public class LootLockerLeaderboardArchiveResponse : LootLockerResponse
    {
        /// <summary>
        /// A List of past Leaderboards.
        /// </summary>
        public LootLockerLeaderboardArchive[] archives { get; set; }
    }

    public class LootLockerLeaderboardArchiveDetailsResponse : LootLockerResponse
    {
        /// <summary>
        /// Pagination.
        /// </summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }
        /// <summary>
        /// A list of players and details from the archived Leaderboard.
        /// </summary>
        public LootLockerLeaderboardArchiveDetails[] items { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetMemberRank(LootLockerGetMemberRankRequest data, Action<LootLockerGetMemberRankResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getMemberRank;
            string tempEndpoint = string.Format(endPoint.endPoint, data.leaderboardId, data.member_id);
            LootLockerServerRequest.CallAPI(tempEndpoint, endPoint.httpMethod, null, ((serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }));
        }

        public static void GetByListOfMembers(LootLockerGetByListMembersRequest data, string id, Action<LootLockerGetByListOfMembersResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.getByListOfMembers;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGetByListOfMembersResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            string endPoint = string.Format(requestEndPoint.endPoint, id);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetAllMemberRanks(LootLockerGetAllMemberRanksRequest getRequests, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.getAllMemberRanks;

            string tempEndpoint = requestEndPoint.endPoint;
            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.member_id, getRequests.count);

            if (!string.IsNullOrEmpty(getRequests.after))
            {
                tempEndpoint = requestEndPoint.endPoint + "&after={2}";
                endPoint = string.Format(tempEndpoint, getRequests.member_id, getRequests.count, int.Parse(getRequests.after));
            }

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetScoreList(LootLockerGetScoreListRequest getRequests, Action<LootLockerGetScoreListResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.getScoreList;

            string tempEndpoint = requestEndPoint.endPoint;
            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.leaderboardKey, getRequests.count);

            if (!string.IsNullOrEmpty(getRequests.after))
            {
                tempEndpoint = requestEndPoint.endPoint + "&after={2}";

                endPoint = string.Format(tempEndpoint, getRequests.leaderboardKey, getRequests.count, int.Parse(getRequests.after));
            }

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void SubmitScore(LootLockerSubmitScoreRequest data, string id, Action<LootLockerSubmitScoreResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.submitScore;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerSubmitScoreResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            string endPoint = string.Format(requestEndPoint.endPoint, id);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}