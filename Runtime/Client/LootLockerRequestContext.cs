using System;

namespace LootLocker
{
    /// <summary>
    /// Contains contextual information about a LootLocker API request.
    /// Includes details such as which player the request was made for and the request ID.
    /// </summary>
    public class LootLockerRequestContext
    {
        public LootLockerRequestContext()
        {
            request_time = null;
        }

        public LootLockerRequestContext(string playerUlid)
        {
            player_ulid = playerUlid;
            request_time = null;
        }

        public LootLockerRequestContext(string playerUlid, DateTime? requestTime)
        {
            player_ulid = playerUlid;
            request_time = requestTime;
        }

        /// <summary>
        /// What player this request was made on behalf of
        /// </summary>
        public string player_ulid { get; set; }

        /// <summary>
        /// The time that this request was made
        /// </summary>
        public DateTime? request_time { get; set; }
    }
}
