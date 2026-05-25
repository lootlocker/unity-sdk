using LootLocker.Requests;

namespace LootLocker
{
    /// <summary>
    /// Details about a player's active ban.
    /// </summary>
    public class LootLockerBanInfo
    {
        /// <summary>
        /// The reason for the ban. One of "manual" or "chargeback".
        /// </summary>
        public string ban_reason { get; set; }

        /// <summary>
        /// The time the ban was issued, as an ISO 8601 timestamp.
        /// </summary>
        public string banned_on { get; set; }

        /// <summary>
        /// The time the ban expires, as an ISO 8601 timestamp.
        /// Null when the ban is permanent; check the <see cref="permanent"/> field to confirm.
        /// </summary>
        public string banned_until { get; set; }

        /// <summary>
        /// True if the ban has no expiry date.
        /// </summary>
        public bool permanent { get; set; }
    }
}

namespace LootLocker.Requests
{
    public class LootLockerBanStatusRequest
    {
        public string game_api_key { get; set; } = LootLockerConfig.current.apiKey;
        public string player_id { get; set; }
    }

    /// <summary>
    /// Response for <see cref="LootLockerSDKManager.GetPlayerBanStatus"/>.
    /// </summary>
    public class LootLockerBanStatusResponse : LootLockerResponse
    {
        /// <summary>
        /// Whether the player is currently banned.
        /// </summary>
        public bool is_banned { get; set; }

        /// <summary>
        /// Details about the active ban. Populated when <see cref="is_banned"/> is true.
        /// </summary>
        public LootLockerBanInfo ban { get; set; }
    }
}
