using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// TODO: Document
    /// </summary>
    public enum LootLockerRemoteSessionLeaseStatus
    {
        Created = 0,
        Claimed = 1,
        Verified = 2,
        Authorized = 3
    };
}

namespace LootLocker.Requests
{

    //==================================================
    // Request Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerLeaseRemoteSessionRequest
    {
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string game_key { get; set; } = LootLockerConfig.current.apiKey;
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string game_version { get; set; } = LootLockerConfig.current.game_version;
    }

    /// <summary>
    /// </summary>
    public class LootLockerStartRemoteSessionRequest
    {
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string game_key { get; set; } = LootLockerConfig.current.apiKey;
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string game_version { get; set; } = LootLockerConfig.current.game_version;
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string lease_code { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string nonce { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerLeaseRemoteSessionResponse : LootLockerResponse
    {
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string nonce { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string redirect_url { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string redirect_url_qr_base64 { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string display_url { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string ip { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerStartRemoteSessionResponse : LootLockerSessionResponse
    {
        /// <summary>
        /// TODO: Document
        /// </summary>
        public LootLockerRemoteSessionLeaseStatus lease_status { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string refresh_token { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string player_identifier { get; set; }
    }
}
