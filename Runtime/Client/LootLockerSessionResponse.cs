using System;

namespace LootLocker.Requests
{

    [Serializable]
    public class LootLockerLevel_Thresholds
    {
        public int current { get; set; }
        public bool current_is_prestige { get; set; }
        public int? next { get; set; }
        public bool next_is_prestige { get; set; }
    }
    

    [Serializable]
    public class LootLockerSessionResponse : LootLockerResponse
    {
        /// <summary>
        /// The player's name if it has been set by using SetPlayerName().
        /// </summary>
        public string player_name { get; set; }
        /// <summary>
        /// The session token that can now be used to use further LootLocker functionality. We store and use this for you.
        /// </summary>
        public string session_token { get; set; }
        /// <summary>
        /// The player id
        /// </summary>
        public int player_id { get; set; }
        /// <summary>
        /// Whether this player has been seen before (true) or is new (false)
        /// </summary>
        public bool seen_before { get; set; }
        /// <summary>
        /// The last time this player logged in
        /// </summary>
        public DateTime? last_seen { get; set; }
        /// <summary>
        /// The public UID for this player
        /// </summary>
        public string public_uid { get; set; }
        /// <summary>
        /// The player ULID for this player
        /// </summary>
        public string player_ulid { get; set; }
        /// <summary>
        /// The creation time of this player
        /// </summary>
        public DateTime player_created_at { get; set; }
        /// <summary>
        /// Whether this player has new information to check in grants
        /// </summary>
        public bool check_grant_notifications { get; set; }
        /// <summary>
        /// Whether this player has new information to check in deactivations
        /// </summary>
        public bool check_deactivation_notifications { get; set; }
        /// <summary>
        /// Whether this player has new information to check in dlcs
        /// </summary>
        public int[] check_dlcs { get; set; }
        /// <summary>
        /// The current xp of this player
        /// </summary>
        public int xp { get; set; }
        /// <summary>
        /// The current level of this player
        /// </summary>
        public int level { get; set; }
        /// <summary>
        /// The level_thresholds that the level and xp data relates to
        /// </summary>
        public LootLockerLevel_Thresholds level_thresholds { get; set; }
        /// <summary>
        /// The current balance in this account
        /// </summary>
        public int account_balance { get; set; }
        /// <summary>
        /// The id of the wallet for this account
        /// </summary>
        public string wallet_id { get; set; }
        /// <summary>
        /// Any errors that occurred during the request, for example if a player name was supplied in optionals but was invalid.
        /// </summary>
        public string[] errors { get; set; }
    }
}