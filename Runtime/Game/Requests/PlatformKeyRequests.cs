namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// Information about the campaign associated with a platform key
    /// </summary>
    public class LootLockerPlatformKeyCampaign
    {
        /// <summary>
        /// The name of the campaign that issued this key
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The platform this key is for (e.g. "steam", "discord")
        /// </summary>
        public string platform { get; set; }
    };

    /// <summary>
    /// A platform key redeemed by the player
    /// </summary>
    public class LootLockerPlatformKey
    {
        /// <summary>
        /// Information about the campaign that issued this key
        /// </summary>
        public LootLockerPlatformKeyCampaign campaign { get; set; }
        /// <summary>
        /// The redeemed key value
        /// </summary>
        public string key { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response containing all platform keys redeemed by the authenticated player.
    /// </summary>
    public class LootLockerListPlatformKeysResponse : LootLockerResponse
    {
        /// <summary>
        /// List of platform keys redeemed by the player
        /// </summary>
        public LootLockerPlatformKey[] platform_keys { get; set; }
    };

}
