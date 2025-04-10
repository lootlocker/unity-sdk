using System;

namespace LootLocker
{
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
