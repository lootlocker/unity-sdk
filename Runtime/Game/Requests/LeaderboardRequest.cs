using System;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// Details about a leaderboard's configuration, schedule, and rewards.
    /// </summary>
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

    /// <summary>
    /// Basic player information as returned in leaderboard entries.
    /// </summary>
    public class LootLockerPlayer
    {
        /// <summary>The legacy integer id of the player.</summary>
        public int id { get; set; }
        /// <summary>The public UID of the player.</summary>
        public string public_uid { get; set; }
        /// <summary>The display name of the player.</summary>
        public string name { get; set; }
        /// <summary>The ULID of the player.</summary>
        public string ulid { get; set; }
    }

    /// <summary>
    /// A single member's entry on a leaderboard, including their rank, score, metadata, and player information.
    /// </summary>
    public class LootLockerLeaderboardMember
    {
        /// <summary>The member id used to identify this entry on a generic leaderboard.</summary>
        public string member_id { get; set; }
        /// <summary>The rank of this member on the leaderboard.</summary>
        public int rank { get; set; }
        /// <summary>The score of this member.</summary>
        public int score { get; set; }
        /// <summary>Player information for this member, if the leaderboard is a player leaderboard.</summary>
        public LootLockerPlayer player { get; set; }
        /// <summary>Optional metadata string attached to this score submission.</summary>
        public string metadata { get; set; }
    }

    /// <summary>
    /// A player's rank entry across a specific leaderboard, linking the rank details to the leaderboard identifier.
    /// </summary>
    public class LootLockerLeaderboard
    {
        /// <summary>The rank details for the player on this leaderboard.</summary>
        public LootLockerLeaderboardMember rank { get; set; }
        /// <summary>The legacy integer id of the leaderboard.</summary>
        public int leaderboard_id { get; set; }
        /// <summary>The key used to identify this leaderboard.</summary>
        public string leaderboard_key { get; set; }
        /// <summary>The ULID of this leaderboard.</summary>
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

    /// <summary>
    /// Pagination state for leaderboard cursor-based pagination.
    /// </summary>
    public class LootLockerPagination
    {
        /// <summary>The total number of entries on the leaderboard.</summary>
        public int total { get; set; }
        /// <summary>The cursor value for the next page of results, or null if on the last page.</summary>
        public int? next_cursor { get; set; }
        /// <summary>The cursor value for the previous page of results, or null if on the first page.</summary>
        public int? previous_cursor { get; set; }
        /// <summary>Whether there is a next page of results available.</summary>
        public bool allowNext { get; set; }
        /// <summary>Whether there is a previous page of results available.</summary>
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

    /// <summary>
    /// Pagination parameters for requesting a range of leaderboard entries.
    /// </summary>
    public class LootLockerGetRequests
    {
        /// <summary>The maximum number of entries to return per page.</summary>
        public int count { get; set; }
        /// <summary>A cursor value specifying the entry after which to begin returning results.</summary>
        public string after { get; set; }
    }

    /// <summary>
    /// Request to submit or update a score on a leaderboard.
    /// </summary>
    public class LootLockerSubmitScoreRequest
    {
        /// <summary>The member id to attribute this score to on a generic leaderboard.</summary>
        public string member_id { get; set; }
        /// <summary>The score value to submit.</summary>
        public int score { get; set; }
        /// <summary>Optional metadata string to attach to this score submission.</summary>
        public string metadata { get; set; }
    }

    /// <summary>
    /// Request to query the theoretical rank a given score would achieve on a leaderboard.
    /// </summary>
    public class LootLockerQueryScoreRequest
    {
        /// <summary>The score value to query the rank for.</summary>
        public int score { get; set; }
    }

    /// <summary>
    /// Request to increment a member's score on a leaderboard by the given amount.
    /// </summary>
    public class LootLockerIncrementScoreRequest
    {
        /// <summary>The member id of the entry to increment.</summary>
        public string member_id { get; set; }
        /// <summary>The amount by which to increment the score.</summary>
        public int amount { get; set; }
    }

    /// <summary>
    /// Request to retrieve entries from an archived (past) leaderboard.
    /// </summary>
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

    /// <summary>
    /// Request to fetch the rank of a specific member on a leaderboard.
    /// </summary>
    public class LootLockerGetMemberRankRequest
    {
        /// <summary>The key or id of the leaderboard to query.</summary>
        public string leaderboardId { get; set; }
        /// <summary>The member id to find the rank for.</summary>
        public string member_id { get; set; }
    }

    /// <summary>
    /// Request to retrieve a paginated score list for a specific leaderboard, with cursor-based pagination state.
    /// </summary>
    public class LootLockerGetScoreListRequest : LootLockerGetRequests
    {
        /// <summary>The key of the leaderboard to retrieve scores from.</summary>
        public string leaderboardKey { get; set; }
        /// <summary>The cursor for the next page of results.</summary>
        public static int? nextCursor { get; set; }
        /// <summary>The cursor for the previous page of results.</summary>
        public static int? prevCursor { get; set; }

        /// <summary>Resets the pagination cursors to the start of the leaderboard.</summary>
        public static void Reset()
        {
            nextCursor = 0;
            prevCursor = 0;
        }
    }

    /// <summary>
    /// Request to retrieve leaderboard ranks for all leaderboards a specific member appears on.
    /// </summary>
    public class LootLockerGetAllMemberRanksRequest : LootLockerGetRequests
    {
        /// <summary>The legacy integer id of the member to look up.</summary>
        public int member_id { get; set; }
        /// <summary>The cursor for the next page of results.</summary>
        public static int? nextCursor { get; set; }
        /// <summary>The cursor for the previous page of results.</summary>
        public static int? prevCursor { get; set; }

        /// <summary>Resets the pagination cursors.</summary>
        public static void Reset()
        {
            nextCursor = 0;
            prevCursor = 0;
        }
    }

    /// <summary>
    /// Request to retrieve leaderboard scores for a list of specific member ids.
    /// </summary>
    public class LootLockerGetByListMembersRequest
    {
        /// <summary>The member ids to retrieve scores for.</summary>
        public string[] members { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response containing a paginated list of leaderboard definitions.
    /// </summary>
    public class LootLockerListLeaderboardsResponse : LootLockerResponse
    {
        /// <summary>
        /// Pagination data to use for subsequent requests
        /// </summary>
        public LootLockerPaginationResponse<int> pagination { get; set; }

        /// <summary>
        /// List of details for the requested leaderboards
        /// </summary>
        public LootLockerLeaderboardDetails[] items { get; set; }
    }

    /// <summary>
    /// Response containing the rank and score for a specific member on a leaderboard.
    /// </summary>
    public class LootLockerGetMemberRankResponse : LootLockerResponse
    {
        /// <summary>The member id for this entry (set for legacy compatibility).</summary>
        public string member_id { get; set; }
        /// <summary>The rank of this member on the leaderboard.</summary>
        public int rank { get; set; }
        /// <summary>The score of this member.</summary>
        public int score { get; set; }
        /// <summary>Player information for this member, if the leaderboard is a player leaderboard.</summary>
        public LootLockerPlayer player { get; set; }
        /// <summary>Optional metadata string attached to this score submission.</summary>
        public string metadata { get; set; }
    }


    /// <summary>
    /// Response containing the leaderboard entries for a list of specific member ids.
    /// </summary>
    public class LootLockerGetByListOfMembersResponse : LootLockerResponse
    {
        /// <summary>The leaderboard entries for the requested members.</summary>
        public LootLockerLeaderboardMember[] members { get; set; }
    }

    /// <summary>
    /// Response containing a paginated list of scores for a specific leaderboard.
    /// </summary>
    public class LootLockerGetScoreListResponse : LootLockerResponse
    {
        /// <summary>Pagination data for this request.</summary>
        public LootLockerPagination pagination { get; set; }
        /// <summary>The leaderboard entries returned for this page.</summary>
        public LootLockerLeaderboardMember[] items { get; set; }
    }

    /// <summary>
    /// Response containing all leaderboard ranks for a specific member across all leaderboards.
    /// </summary>
    public class LootLockerGetAllMemberRanksResponse : LootLockerResponse
    {
        /// <summary>The leaderboard rank entries for the requested member.</summary>
        public LootLockerLeaderboard[] leaderboards { get; set; }
        /// <summary>Pagination data for this request.</summary>
        public LootLockerPagination pagination { get; set; }
    }

    /// <summary>
    /// Response returned after submitting a score to a leaderboard, showing the resulting rank.
    /// </summary>
    public class LootLockerSubmitScoreResponse : LootLockerResponse
    {
        /// <summary>The member id of the entry that was submitted or updated.</summary>
        public string member_id { get; set; }
        /// <summary>The resulting rank of the member after the score was submitted.</summary>
        public int rank { get; set; }
        /// <summary>The submitted score value.</summary>
        public int score { get; set; }
        /// <summary>The optional metadata string attached to this submission.</summary>
        public string metadata { get; set; }
    }

    /// <summary>
    /// Response containing a list of archived (past) leaderboard snapshots.
    /// </summary>
    public class LootLockerLeaderboardArchiveResponse : LootLockerResponse
    {
        /// <summary>
        /// A List of past Leaderboards.
        /// </summary>
        public LootLockerLeaderboardArchive[] archives { get; set; }
    }

    /// <summary>
    /// Response containing the paginated entries from a specific archived leaderboard snapshot.
    /// </summary>
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
        public static void GetMemberRank(string forPlayerWithUlid, LootLockerGetMemberRankRequest data, Action<LootLockerGetMemberRankResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getMemberRank;
            string tempEndpoint = endPoint.WithPathParameters(data.leaderboardId, data.member_id);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, tempEndpoint, endPoint.httpMethod, null, ((serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }));
        }

        public static void GetByListOfMembers(string forPlayerWithUlid, LootLockerGetByListMembersRequest data, string id, Action<LootLockerGetByListOfMembersResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.getByListOfMembers;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGetByListOfMembersResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            string endPoint = requestEndPoint.WithPathParameter(id);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetAllMemberRanks(string forPlayerWithUlid, LootLockerGetAllMemberRanksRequest getRequests, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.getAllMemberRanks;

            string tempEndpoint = requestEndPoint.endPoint;
            string endPoint = requestEndPoint.WithPathParameters(getRequests.member_id, getRequests.count);

            if (!string.IsNullOrEmpty(getRequests.after))
            {
                tempEndpoint = requestEndPoint.endPoint + "&after={2}";
                endPoint = string.Format(tempEndpoint, getRequests.member_id, getRequests.count, int.Parse(getRequests.after));
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetScoreList(string forPlayerWithUlid, LootLockerGetScoreListRequest getRequests, Action<LootLockerGetScoreListResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.getScoreList;

            string tempEndpoint = requestEndPoint.endPoint;
            string endPoint = requestEndPoint.WithPathParameters(getRequests.leaderboardKey, getRequests.count);

            if (!string.IsNullOrEmpty(getRequests.after))
            {
                tempEndpoint = requestEndPoint.endPoint + "&after={2}";

                endPoint = string.Format(tempEndpoint, getRequests.leaderboardKey, getRequests.count, int.Parse(getRequests.after));
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void SubmitScore(string forPlayerWithUlid, LootLockerSubmitScoreRequest data, string id, Action<LootLockerSubmitScoreResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.submitScore;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerSubmitScoreResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            string endPoint = requestEndPoint.WithPathParameter(id);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}