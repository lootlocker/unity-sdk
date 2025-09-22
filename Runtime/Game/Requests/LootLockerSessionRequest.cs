using System;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    public class LootLockerSteamSessionRequest
    {
        public string game_api_key { get; set; } = LootLockerConfig.current.apiKey;
        public string game_version { get; set; } = LootLockerConfig.current.game_version;
        public string steam_ticket { get; set; }
    }

    public class LootLockerPlaystationNetworkVerificationRequest
    {
        public string key { get; set; } = LootLockerConfig.current.apiKey;
        public string platform { get; set; } = "psn";
        public string token { get; set; }
        public int psn_issuer_id { get; set; } = 256; // Default to production
    }

    public class LootLockerPlaystationNetworkV3SessionRequest
    {
        public string game_api_key { get; set; } = LootLockerConfig.current.apiKey;
        public string game_version => LootLockerConfig.current.game_version;
        public string auth_code { get; set; }
        public int env_iss_id { get; set; } = 256; // Default to production
    }

    public class LootLockerSteamSessionWithAppIdRequest : LootLockerSteamSessionRequest
    {
        public string steam_app_id { get; set; }
    }

    [Serializable]
    public class LootLockerSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string platform { get; set; }
        public string player_identifier { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerSessionRequest()
        {
            player_identifier = "";
            platform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.None).PlatformString;
        }

        public LootLockerSessionRequest(string playerIdentifier, LL_AuthPlatforms forPlatform)
        {
            player_identifier = playerIdentifier;
            platform = LootLockerAuthPlatform.GetPlatformRepresentation(forPlatform).PlatformString;
        }
    }

    [Serializable]
    public class LootLockerWhiteLabelSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string email { get; set; }
        public string token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerWhiteLabelSessionRequest(string email)
        {
            this.email = email;
            this.token = null;
        }

        public LootLockerWhiteLabelSessionRequest()
        {
            this.email = null;
            this.token = null;
        }
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
    }

    public class LootLockerGuestSessionResponse : LootLockerSessionResponse
    {
        /// <summary>
        /// The unique player identifier for this account
        /// </summary>
        public string player_identifier { get; set; }
    }

    [Serializable]
    public class LootLockerGoogleSessionResponse : LootLockerSessionResponse
    {
        public string player_identifier { get; set; }
        public string refresh_token { get; set; }
    }

    [Serializable]
    public class LootLockerGooglePlayGamesSessionResponse : LootLockerSessionResponse
    {
        public string player_identifier { get; set; }
        public string refresh_token { get; set; }
    }

    [Serializable]
    public class LootLockerAppleSessionResponse : LootLockerSessionResponse
    {
        public string player_identifier { get; set; }
        public string refresh_token { get; set; }
    }

    [System.Serializable]
    public class LootLockerAppleGameCenterSessionResponse : LootLockerSessionResponse
    {
        public string refresh_token { get; set; }
    }

    [System.Serializable]
    public class LootLockerEpicSessionResponse : LootLockerSessionResponse
    {
        public string refresh_token { get; set; }
    }
    
    public class LootLockerMetaSessionResponse : LootLockerSessionResponse
    {
        public string refresh_token { get; set; }
    }
    
    public class LootLockerPlaystationV3SessionResponse : LootLockerSessionResponse
    {
        public string player_identifier { get; set; }
    }

    [Serializable]
    public class LootLockerLevel_Thresholds
    {
        public int current { get; set; }
        public bool current_is_prestige { get; set; }
        public int? next { get; set; }
        public bool next_is_prestige { get; set; }
    }

    [Serializable]
    public class LootLockerNintendoSwitchSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string nsa_id_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerNintendoSwitchSessionRequest(string nsa_id_token)
        {
            this.nsa_id_token = nsa_id_token;
        }
    }

    [Serializable]
    public class LootLockerEpicSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string id_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerEpicSessionRequest(string id_token)
        {
            this.id_token = id_token;
        }
    }

    [Serializable]
    public class LootLockerEpicRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string refresh_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerEpicRefreshSessionRequest(string refresh_token)
        {
            this.refresh_token = refresh_token;
        }
    }
    
    [Serializable]
    public class LootLockerMetaSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string game_version => LootLockerConfig.current.game_version;
        
        public string user_id { get; set; }

        public string nonce { get; set; }
    }
    
    [Serializable]
    public class LootLockerMetaRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string refresh_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;
    }

    public class LootLockerXboxOneSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string xbox_user_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerXboxOneSessionRequest(string xbox_user_token)
        {
            this.xbox_user_token = xbox_user_token;
        }
    }

    public class LootLockerGoogleSignInSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string id_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerGoogleSignInSessionRequest(string id_token)
        {
            this.id_token = id_token;
        }
    }
    
    public enum GooglePlatform
    {
        web, android, ios, desktop
    }

    public class LootLockerGoogleSignInWithPlatformSessionRequest : LootLockerGoogleSignInSessionRequest
    {
        public string platform { get; set; }

        public LootLockerGoogleSignInWithPlatformSessionRequest(string id_token, string platform) : base(id_token)
        {
            this.platform = platform;
        }
    }

    public class LootLockerGoogleRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string refresh_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerGoogleRefreshSessionRequest(string refresh_token)
        {
            this.refresh_token = refresh_token;
        }
    }
    public class LootLockerGooglePlayGamesSessionRequest
    {
        public string game_api_key => LootLockerConfig.current.apiKey;
        public string auth_code { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerGooglePlayGamesSessionRequest(string authCode)
        {
            this.auth_code = authCode;
        }
    }

    public class LootLockerGooglePlayGamesRefreshSessionRequest
    {
        public string game_api_key => LootLockerConfig.current.apiKey;
        public string refresh_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerGooglePlayGamesRefreshSessionRequest(string refreshToken)
        {
            this.refresh_token = refreshToken;
        }
    }

    public class LootLockerAppleSignInSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string apple_authorization_code { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerAppleSignInSessionRequest(string apple_authorization_code)
        {
            this.apple_authorization_code = apple_authorization_code;
        }
    }

    public class LootLockerAppleRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string refresh_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerAppleRefreshSessionRequest(string refresh_token)
        {
            this.refresh_token = refresh_token;
        }
    }

    public class LootLockerAppleGameCenterSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string game_version => LootLockerConfig.current.game_version;
        public string bundle_id { get; private set; }
        public string player_id { get; private set; }
        public string public_key_url { get; private set; }
        public string signature { get; private set; }
        public string salt { get; private set; }
        public long timestamp { get; private set; }

        public LootLockerAppleGameCenterSessionRequest(string bundleId, string playerId, string publicKeyUrl, string signature, string salt, long timestamp)
        {
            this.bundle_id = bundleId;
            this.player_id = playerId;
            this.public_key_url = publicKeyUrl;
            this.signature = signature;
            this.salt = salt;
            this.timestamp = timestamp;
        }
    }

    public class LootLockerAppleGameCenterRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string game_version => LootLockerConfig.current.game_version;
        public string refresh_token { get; private set; }

        public LootLockerAppleGameCenterRefreshSessionRequest(string refreshToken)
        {
            this.refresh_token = refreshToken;
        }
    }
    public class LootLockerDiscordSessionRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string access_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerDiscordSessionRequest(string accessToken)
        {
            this.access_token = accessToken;
        }
    }

    public class LootLockerDiscordRefreshSessionRequest
    {
        public string game_key => LootLockerConfig.current.apiKey;
        public string refresh_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerDiscordRefreshSessionRequest(string refreshToken)
        {
            this.refresh_token = refreshToken;
        }
    }

    [Serializable]
    public class LootLockerDiscordSessionResponse : LootLockerSessionResponse
    {
        public string refresh_token { get; set; }
        public string player_identifier { get; set; }
    }
}