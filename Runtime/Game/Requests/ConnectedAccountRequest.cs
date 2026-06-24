using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// Account providers possible to use for connected accounts
    /// </summary>
    public enum LootLockerAccountProvider
    {
        guest = 0,
        google = 1,
        apple = 2,
        steam = 3,
        epic_games = 4,
        credentials = 5, // White Label Login
        nintendo = 6,
        xbox = 7,
        playstation = 8,
        twitch = 9,
        discord = 10
    }

    /// <summary>
    /// Google OAuth2 Client platform
    /// </summary>
    public enum GoogleAccountProviderPlatform
    {
        web, android, ios, desktop
    }
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================
    
    /// <summary>
    /// An account provider linked to the current player's LootLocker account.
    /// </summary>
    public class LootLockerConnectedAccountProvider
    {
        /// <summary>
        /// The account provider
        /// </summary>
        public LootLockerAccountProvider provider { get; set; }
        /// <summary>
        /// Decorated name of this provider to use for displaying
        /// </summary>
        public string provider_name { get; set; }
    }

    //==================================================
    // Request Definitions
    //==================================================
    
    /// <summary>
    /// Request to link a Google account to the current player's LootLocker account using an id token.
    /// </summary>
    public class LootLockerConnectGoogleProviderToAccountRequest
    {
        /// <summary>
        /// The Id Token from google sign in
        /// </summary>
        public string id_token { get; set; }
    }

    /// <summary>
    /// Request to link a Google account to the current player's LootLocker account, specifying the OAuth2 client platform.
    /// </summary>
    public class LootLockerConnectGoogleProviderToAccountWithPlatformRequest
    {
        /// <summary>
        /// The Id Token from google sign in
        /// </summary>
        public string id_token { get; set; }
        /// <summary>
        /// Google OAuth2 ClientID platform
        /// </summary>
        public GoogleAccountProviderPlatform platform { get; set; }
    }

    /// <summary>
    /// Request to link a remote session to the current player's LootLocker account using a lease code and nonce.
    /// </summary>
    public class LootLockerConnectRemoteSessionToAccountRequest
    {
        /// <summary>
        /// The unique code for this leasing process, this is what identifies the leasing process and that is used to interact with it
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// The nonce used to sign usage of the lease code
        /// </summary>
        public string Nonce { get; set; }
    }

    /// <summary>
    /// Request to transfer connected account providers from one LootLocker player account to another.
    /// </summary>
    public class LootLockerTransferProvidersBetweenAccountsRequest
    {
        /// <summary>
        /// Session token belonging to the player to move platforms FROM
        /// </summary>
        public string Source_token { get; set; }
        /// <summary>
        /// Session token belonging to the player to move platforms TO
        /// </summary>
        public string Target_token { get; set; }
        /// <summary>
        /// List of identity providers to move FROM the account authenticated by the source token TO the account authenticated by the target token
        /// </summary>
        public LootLockerAccountProvider[] Identity_providers { get; set; }
    }

    /// <summary>
    /// Request to link an Apple account to the current player's LootLocker account using an authorization code.
    /// </summary>
    public class LootLockerConnectAppleRestProviderToAccountRequest
    {
        /// <summary>
        /// Authorization code, provided by apple during Sign In
        /// </summary>
        public string authorization_code { get; set; }
    }
    
    /// <summary>
    /// Request to link an Epic Games account to the current player's LootLocker account.
    /// </summary>
    public class LootLockerConnectEpicProviderToAccountRequest
    {
        /// <summary>
        /// The Token from epic sign in
        /// </summary>
        public string token { get; set; }
    }
    
    /// <summary>
    /// Request to link a PlayStation account to the current player's LootLocker account.
    /// </summary>
    public class LootLockerConnectPlaystationProviderToAccountRequest
    {
        /// <summary>
        /// The environment for the playstation account (dev, qa, prod)
        /// </summary>
        public string environment { get; set; }
        /// <summary>
        /// The code from playstation sign in
        /// </summary>
        public string code { get; set; }
    }
    
    /// <summary>
    /// Request to link a Discord account to the current player's LootLocker account.
    /// </summary>
    public class LootLockerConnectDiscordProviderToAccountRequest
    {
        /// <summary>
        /// The Token from discord sign in
        /// </summary>
        public string token { get; set; }
    }
    
    /// <summary>
    /// Request to link a Twitch account to the current player's LootLocker account using an authorization code.
    /// </summary>
    public class LootLockerConnectTwitchProviderToAccountRequest
    {
        /// <summary>
        /// The Authorization Code from Twitch sign in
        /// </summary>
        public string authorization_code { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response returned after successfully connecting an account provider to the current player's LootLocker account.
    /// </summary>
    public class LootLockerAccountConnectedResponse : LootLockerResponse
    {
        /// <summary>
        /// The connected account provider
        /// </summary>
        public LootLockerConnectedAccountProvider connected_account { get; set; }
    }

    /// <summary>
    /// Response containing all account providers connected to the current player's LootLocker account.
    /// </summary>
    public class LootLockerListConnectedAccountsResponse : LootLockerResponse
    {
        /// <summary>
        /// List of the accounts connected (allowed to start sessions for) to this LootLocker account
        /// </summary>
        public LootLockerConnectedAccountProvider[] connected_accounts { get; set; }
    }


}