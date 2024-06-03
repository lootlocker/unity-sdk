using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// An enum with the supported stores that can generate entitlements
    /// </summary>
    public enum LootLockerEntitlementHistoryListingStore
    {
        None = 0,
        Apple_app_store = 1,
        Google_play_store = 2,
        Steam_store = 3,
        Playstation_network = 4,
        Nintendo_eshop = 5,
        Lootlocker = 6
    };

    /// <summary>
    /// Status of the entitlement
    /// </summary>
    public enum LootLockerEntitlementHistoryListingStatus
    {
        None = 0,
        Active = 1,
        Pending = 2,
        Expired = 3,
        Canceled = 4,
        Refunded = 5
    };

    /// <summary>
    /// Status of the entitlement
    /// </summary>
    public enum LootLockerEntitlementHistoryListingType
    {
        Undefined = 0,
        One_time_purchase = 1,
        Leaderboard_reward = 2,
        Subscription = 3
    };
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerEntitlementHistoryItem
    {

        /// <summary>
        /// When this item was created
        /// </summary>
        public string Created_at { get; set; }

        /// <summary>
        /// What kind of reward this item is
        /// </summary>
        public string Reward_kind { get; set; }

        /// <summary>
        /// The unique identifier of this specific item
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The id of the reward this item is in
        /// </summary>
        public string Reward_id { get; set; }

        /// <summary>
        /// The id of the catalog item that this item is in
        /// </summary>
        public string Catalog_id { get; set; }

        /// <summary>
        /// Whether this item is purchasable
        /// </summary>
        public bool Purchasable { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerEntitlementHistoryMetadata
    {

        /// <summary>
        /// The key of this pair, describes what the value is
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The value of this pair, contains the information of the metadata
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerEntitlementHistoryReward
    {

        /// <summary>
        /// When this reward was created
        /// </summary>
        public string Created_at { get; set; }

        /// <summary>
        /// The id of this entitlement
        /// </summary>
        public string Entitlement_id { get; set; }

        /// <summary>
        /// The unique identifier of this reward
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerEntitlementListing
    {

        /// <summary>
        /// When this entitlement listing was created
        /// </summary>
        public string Created_at { get; set; }

        /// <summary>
        /// The unique identifier of this entitlement listing
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// List of items in this entitlement (items are related to the catalog) 
        /// </summary>
        public LootLockerEntitlementHistoryItem[] Items { get; set; }

        /// <summary>
        /// List of rewards in this entitlement (these are rewards from systems such as leaderboards, progressions, etc.) 
        /// </summary>
        public LootLockerEntitlementHistoryReward[] Rewards { get; set; }

        /// <summary>
        /// Metadata related to this entitlement listing
        /// This array consists of key value pairs and contains various pieces of information about the entitlement, such as information from third party stores etc.
        /// </summary>
        public LootLockerEntitlementHistoryMetadata[] Metadata { get; set; }

        /// <summary>
        /// The status of this entitlement listing
        /// </summary>
        public LootLockerEntitlementHistoryListingStatus Status { get; set; } = LootLockerEntitlementHistoryListingStatus.None;

        /// <summary>
        /// Which store (if any) that this entitlement listing relates to
        /// </summary>
        public LootLockerEntitlementHistoryListingStore Store { get; set; } = LootLockerEntitlementHistoryListingStore.None;

        /// <summary>
        /// Which type this entitlement listing is
        /// </summary>
        public LootLockerEntitlementHistoryListingType Type { get; set; } = LootLockerEntitlementHistoryListingType.Undefined;
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerEntitlementHistoryResponse : LootLockerResponse
    {

        /// <summary>
        /// List of entitlement history entries
        /// </summary>
        public LootLockerEntitlementListing[] Listings { get; set; }

        /// <summary>
        /// Pagination data to use for subsequent requests
        /// </summary>
        public LootLockerPaginationResponse<string> Pagination { get; set; }
    }

    /// <summary>
    /// Response body of a single entitlement, which contains status and more information
    /// </summary>
    public class LootLockerSingleEntitlementResponse : LootLockerResponse
    {
        /// <summary>
        /// When this entitlement listing was created
        /// </summary>
        public string Created_at { get; set; }
        /// <summary>
        /// The type this entitlement is example is a one time purchase
        /// </summary>
        public LootLockerEntitlementHistoryListingType Type { get; set; } = LootLockerEntitlementHistoryListingType.Undefined;
        /// <summary>
        /// The status of the entitlement, (pending, active, canceled)
        /// </summary>
        public LootLockerEntitlementHistoryListingStatus Status { get; set; } = LootLockerEntitlementHistoryListingStatus.None;
        /// <summary>
        /// The store connected to the entitlement
        /// </summary>
        public LootLockerEntitlementHistoryListingStore Store { get; set; } = LootLockerEntitlementHistoryListingStore.None;
        /// <summary>
        /// An array of the items connected to this entitlement
        /// </summary>
        public LootLockerEntitlementHistoryItem[] Items { get; set; }
        /// <summary>
        /// Metadata of the entitlement
        /// </summary>
        public LootLockerEntitlementHistoryMetadata[] Metadata { get; set; }

    }
}
