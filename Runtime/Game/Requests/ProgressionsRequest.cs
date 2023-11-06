using System;
using System.Collections.Generic;

namespace LootLocker.Requests
{
    public class LootLockerProgressionResponse : LootLockerResponse
    {
        public string id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
    }
    
    public class LootLockerPaginatedProgressionsResponse : LootLockerResponse
    {
        public LootLockerPaginationResponse<string> pagination { get; set; }
        public List<LootLockerProgression> items { get; set; }
        
        public class LootLockerProgression
        {
            public string id { get; set; }
            public string key { get; set; }
            public string name { get; set; }
            public bool active { get; set; }
        }
    }
    
    public class LootLockerPlayerProgressionResponse : LootLockerResponse
    {
        public string id { get; set; }
        public string progression_key { get; set; }
        public string progression_name { get; set; }
        public ulong step { get; set; }
        public ulong points { get; set; }
        public ulong previous_threshold { get; set; }
        public ulong? next_threshold { get; set; }
        public DateTime? last_level_up { get; set; }
    }
    
    public class LootLockerPaginatedPlayerProgressionsResponse : LootLockerResponse
    {
        public LootLockerPaginationResponse<string> pagination { get; set; }
        public List<LootLockerPlayerProgression> items { get; set; }
        
        public class LootLockerPlayerProgression
        {
            public string id { get; set; }
            public string progression_key { get; set; }
            public string progression_name { get; set; }
            public ulong step { get; set; }
            public ulong points { get; set; }
            public ulong previous_threshold { get; set; }
            public ulong? next_threshold { get; set; }
            public DateTime? last_level_up { get; set; }
        }
    }

    public class LootLockerPlayerProgressionWithRewardsResponse : LootLockerPlayerProgressionResponse
    {
        public List<LootLockerAwardedTier> awarded_tiers { get; set; }
    }

    public class LootLockerCharacterProgressionResponse : LootLockerResponse
    {
        public string id { get; set; }
        public string progression_key { get; set; }
        public string progression_name { get; set; }
        public ulong step { get; set; }
        public ulong points { get; set; }
        public ulong previous_threshold { get; set; }
        public ulong? next_threshold { get; set; }
        public DateTime? last_level_up { get; set; }
    }
    
    public class LootLockerPaginatedCharacterProgressionsResponse : LootLockerResponse
    {
        public LootLockerPaginationResponse<string> pagination { get; set; }
        public List<LootLockerCharacterProgression> items { get; set; }
        
        public class LootLockerCharacterProgression
        {
            public string id { get; set; }
            public string progression_key { get; set; }
            public string progression_name { get; set; }
            public ulong step { get; set; }
            public ulong points { get; set; }
            public ulong previous_threshold { get; set; }
            public ulong? next_threshold { get; set; }
            public DateTime? last_level_up { get; set; }
        }
    }

    public class LootLockerCharacterProgressionWithRewardsResponse : LootLockerCharacterProgressionResponse
    {
        public List<LootLockerAwardedTier> awarded_tiers { get; set; }
    }
    
    public class LootLockerAssetInstanceProgressionResponse : LootLockerResponse
    {
        public string id { get; set; }
        public string progression_key { get; set; }
        public string progression_name { get; set; }
        public ulong step { get; set; }
        public ulong points { get; set; }
        public ulong previous_threshold { get; set; }
        public ulong? next_threshold { get; set; }
        public DateTime? last_level_up { get; set; }
    }

    public class LootLockerPaginatedAssetInstanceProgressionsResponse : LootLockerResponse
    {
        public LootLockerPaginationResponse<string> pagination { get; set; }
        public List<LootLockerAssetInstanceProgression> items { get; set; }
    
        public class LootLockerAssetInstanceProgression
        {
            public string id { get; set; }
            public string progression_key { get; set; }
            public string progression_name { get; set; }
            public ulong step { get; set; }
            public ulong points { get; set; }
            public ulong previous_threshold { get; set; }
            public ulong? next_threshold { get; set; }
            public DateTime? last_level_up { get; set; }
        }
    }

    public class LootLockerAssetInstanceProgressionWithRewardsResponse : LootLockerAssetInstanceProgressionResponse
    {
        public List<LootLockerAwardedTier> awarded_tiers { get; set; }
    }

    public class LootLockerAwardedTier
    {
        public ulong step { get; set; }
        public ulong points_threshold { get; set; }
        public LootLockerRewards rewards { get; set; }
    }
    
    public class LootLockerAssetReward
    {
        public int asset_id { get; set; }
        public int? asset_variation_id { get; set; }
        public int? asset_rental_option_id { get; set; }
    }

    public class LootLockerProgressionPointsReward
    {
        public string progression_id { get; set; }
        public string progression_key { get; set; }
        public string progression_name { get; set; }
        public ulong amount { get; set; }
    }

    public class LootLockerProgressionResetReward
    {
        public string progression_id { get; set; }
        public string progression_key { get; set; }
        public string progression_name { get; set; }
    }

    public class LootLockerCurrencyReward
    {
        public string currency_name { get; set; }
        public string currency_code { get; set; }
        public string amount { get; set; }
    }

    public class LootLockerRewards
    {
        public List<LootLockerProgressionPointsReward> progression_points_rewards { get; set; }
        public List<LootLockerProgressionResetReward> progression_reset_rewards { get; set; }
        public List<LootLockerAssetReward> asset_rewards { get; set; }
        public List<LootLockerCurrencyReward> currency_rewards { get; set; }
    }

    public class LootLockerPaginatedProgressionTiersResponse : LootLockerResponse
    {
        public LootLockerPaginationResponse<ulong?> pagination { get; set; }
        public List<LootLockerProgressionTier> items { get; set; }
        
        public class LootLockerProgressionTier
        {
            public ulong step { get; set; }
            public ulong points_threshold { get; set; }
            public LootLockerRewards rewards { get; set; }
        }
    }
    
    public class LootLockerProgressionTierResponse : LootLockerResponse
    {
        public ulong step { get; set; }
        public ulong points_threshold { get; set; }
        public LootLockerRewards rewards { get; set; }
    }
}