using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// The status of the remote session leasing process
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
        /// The Game Key configured for the game
        /// </summary>
        public string game_key { get; set; } = LootLockerConfig.current.apiKey;
        /// <summary>
        /// The Game Version configured for the game
        /// </summary>
        public string game_version { get; set; } = LootLockerConfig.current.game_version;
    }

    /// <summary>
    /// </summary>
    public class LootLockerStartRemoteSessionRequest
    {
        /// <summary>
        /// The Game Key configured for the game
        /// </summary>
        public string game_key { get; set; } = LootLockerConfig.current.apiKey;
        /// <summary>
        /// The Game Version configured for the game
        /// </summary>
        public string game_version { get; set; } = LootLockerConfig.current.game_version;
        /// <summary>
        /// The lease code returned with the response when starting a lease process
        /// </summary>
        public string lease_code { get; set; }
        /// <summary>
        /// The nonce returned with the response when starting a lease process
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
        /// The unique code for this leasing process, this is what identifies the leasing process and that is used to interact with it
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// The nonce used to sign usage of the lease code
        /// </summary>
        public string nonce { get; set; }
        /// <summary>
        /// A url with the code and nonce baked in that can be used to immediately start the remote authentication process on the device that uses it
        /// </summary>
        public string redirect_url { get; set; }
        /// <summary>
        /// A QR code representation of the redirect_url encoded in Base64
        /// </summary>
        public string redirect_url_qr_base64 { get; set; }
        /// <summary>
        /// A clean version of the redirect_url without the code visible that you can use in your UI 
        /// </summary>
        public string display_url { get; set; }
        /// <summary>
        /// The status of this lease process
        /// </summary>
        public LootLockerRemoteSessionLeaseStatus status { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerStartRemoteSessionResponse : LootLockerSessionResponse
    {
        /// <summary>
        /// The current status of this lease process. If this is not of the status Authorized, the rest of the fields in this object will be empty.
        /// </summary>
        public LootLockerRemoteSessionLeaseStatus lease_status { get; set; }
        /// <summary>
        /// A refresh token that can be used to refresh the remote session instead of signing in each time the session token expires
        /// </summary>
        public string refresh_token { get; set; }
        /// <summary>
        /// The player identifier of the player
        /// </summary>
        public string player_identifier { get; set; }
    }
}
