using System;
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
    ///</summary>
    public class LootLockerFollower
    {
        /// <summary>
        /// The id of the player
        ///</summary>
        public string player_id { get; set; }
        /// <summary>
        /// The name (if any has been set) of the player
        ///</summary>
        public string player_name { get; set; }
        /// <summary>
        /// The public uid of the player
        ///</summary>
        public string publicuid { get; set; }
        /// <summary>
        /// When the player's account was created
        ///</summary>
        public DateTime created_at { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerListFollowersResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of the followers for the specified player
        /// </summary>
        public LootLockerFollower[] followers { get; set; }
        /// <summary>
        /// Pagination data for the request
        /// </summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerListFollowingResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of the players that the specified player is following
        /// </summary>
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("followers")]
#else
        [Json(Name = "followers")]
#endif

        public LootLockerFollower[] following { get; set; }
        /// <summary>
        /// Pagination data for the request
        /// </summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerFollowersOperationResponse : LootLockerResponse
    {
        // Empty unless errors occured
    }

}