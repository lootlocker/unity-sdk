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
        /// TODO: Document
        /// </summary>
        public string created_at { get; set; }

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string reward_kind { get; set; }

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string reward_id { get; set; }

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string catalog_id { get; set; }

        /// <summary>
        /// TODO: Document
        /// </summary>
        public bool purchasable { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerEntitlementHistoryMetadata
    {

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string key { get; set; }

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string value { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerEntitlementHistoryReward
    {

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string created_at { get; set; }

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string entitlement_id { get; set; }

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// TODO: Document
        /// </summary>
        public string reward_id { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerEntitlementHistoryResponse : LootLockerResponse
    {

        /// <summary>
        /// TODO: Document
        /// </summary>
        public LootLockerEntitlementListing[] listings { get; set; }
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }
    }
    
    /// <summary>
    /// </summary>
    public class LootLockerEntitlementListing
    {
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string created_at { get; set; }
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string id { get; set; }
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public LootLockerEntitlementHistoryItem[] items { get; set; }
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public LootLockerEntitlementHistoryMetadata[] metadata { get; set; }
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string player_id { get; set; }
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public LootLockerEntitlementHistoryReward[] rewards { get; set; }
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string status { get; set; }
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string store { get; set; }
        
        /// <summary>
        /// TODO: Document
        /// </summary>
        public string type { get; set; }
    }
}
