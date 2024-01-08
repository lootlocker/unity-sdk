using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// TODO: Document
    /// </summary>
    public enum LootLockerAccountProvider
    {
        guest = 0,
        google = 1,
        apple = 2,
    }

    /// <summary>
    /// TODO: Document
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
    /// </summary>
    public class LootLockerConnectedAccountProvider
    {
        /// <summary>
        /// TODO: Document
        /// </summary>
        public LootLockerAccountProvider provider { get; set; }
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string provider_name { get; set; }
    }

    //==================================================
    // Request Definitions
    //==================================================
    
    /// <summary>
    /// </summary>
    public class LootLockerConnectGoogleProviderToAccountRequest
    {
        /// <summary>
        /// The Id Token from google sign in
        /// </summary>
        public string id_token { get; set; }
    }

    /// <summary>
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
    /// </summary>
    public class LootLockerConnectAppleRestProviderToAccountRequest
    {
        /// <summary>
        /// Authorization code, provided by apple during Sign In
        /// </summary>
        public string authorization_code { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerAccountConnectedResponse : LootLockerResponse
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

    /// <summary>
    /// </summary>
    public class LootLockerListConnectedAccountsResponse : LootLockerResponse
    {
        /// <summary>
        /// List of the accounts connected (allowed to start sessions for) to this LootLocker account
        /// </summary>
        public LootLockerConnectedAccountProvider[] connected_accounts { get; set; }
    }


}