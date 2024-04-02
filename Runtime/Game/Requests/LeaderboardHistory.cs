using System;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    public class LootLockerLeaderboardHistoryResponse : LootLockerResponse
    {
        /// <summary>
        /// A List of past Leaderboards.
        /// </summary>
        public LootLockerLeaderboardArchive[] archives { get; set; }
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
    public class LootLockerLeaderboardHistoryDetailsResponse : LootLockerResponse
    {
        /// <summary>
        /// Pagination.
        /// </summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }
        /// <summary>
        /// A list of players and details from the archived Leaderboard.
        /// </summary>
        public LootLockerLeaderboardHistoryDetails[] items { get; set; }
    }
    public class LootLockerLeaderboardHistoryDetails
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
}
