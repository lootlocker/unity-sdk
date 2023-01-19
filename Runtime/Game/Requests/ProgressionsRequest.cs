using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace LootLocker.Requests
{
    public class LootLockerProgression : LootLockerResponse
    {
        public string id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
    }
    
    public class LootLockerPaginatedProgressions : LootLockerResponse
    {
        public LootLockerPagination pagination { get; set; }
        public List<LootLockerProgression> items { get; set; }
        
        public class LootLockerProgression
        {
            public string id { get; set; }
            public string key { get; set; }
            public string name { get; set; }
            public bool active { get; set; }
        }
    }
    
    public class LootLockerPlayerProgression : LootLockerResponse
    {
        public string id { get; set; }
        public string progression_key { get; set; }
        public string progression_name { get; set; }
        public int step { get; set; }
        public int points { get; set; }
        public int previous_threshold { get; set; }
        public int next_threshold { get; set; }
        public DateTime? last_level_up { get; set; }
    }
    
    public class LootLockerPaginatedPlayerProgressions : LootLockerResponse
    {
        public LootLockerPagination pagination { get; set; }
        public List<LootLockerPlayerProgression> items { get; set; }
        
        public class LootLockerPlayerProgression
        {
            public string id { get; set; }
            public string progression_key { get; set; }
            public string progression_name { get; set; }
            public int step { get; set; }
            public int points { get; set; }
            public int previous_threshold { get; set; }
            public int next_threshold { get; set; }
            public DateTime? last_level_up { get; set; }
        }
    }

    public class LootLockerPlayerProgressionWithRewards : LootLockerPlayerProgression
    {
        public List<LootLockerAwardedTier> awarded_tiers { get; set; }
    }

    public class LootLockerCharacterProgression : LootLockerResponse
    {
        public string id { get; set; }
        public string progression_key { get; set; }
        public string progression_name { get; set; }
        public int step { get; set; }
        public int points { get; set; }
        public int previous_threshold { get; set; }
        public int next_threshold { get; set; }
        public DateTime? last_level_up { get; set; }
    }
    
    public class LootLockerPaginatedCharacterProgressions : LootLockerResponse
    {
        public LootLockerPagination pagination { get; set; }
        public List<LootLockerCharacterProgression> items { get; set; }
        
        public class LootLockerCharacterProgression
        {
            public string id { get; set; }
            public string progression_key { get; set; }
            public string progression_name { get; set; }
            public int step { get; set; }
            public int points { get; set; }
            public int previous_threshold { get; set; }
            public int next_threshold { get; set; }
            public DateTime? last_level_up { get; set; }
        }
    }

    public class LootLockerCharacterProgressionWithRewards : LootLockerPlayerProgression
    {
        public List<LootLockerAwardedTier> awarded_tiers { get; set; }
    }

    public class LootLockerAwardedTier
    {
        public int step { get; set; }
        public int points_threshold { get; set; }
        public LootLockerRewards rewards { get; set; }
    }
    
    public class LootLockerAssetReward
    {
        public int asset_id { get; set; }
        public object asset_variation_id { get; set; }
        public object asset_rental_option_id { get; set; }
    }

    public class LootLockerProgressionPointsReward
    {
        public string progression_id { get; set; }
        public string progression_key { get; set; }
        public string progression_name { get; set; }
        public int amount { get; set; }
    }

    public class LootLockerProgressionResetReward
    {
        public string progression_id { get; set; }
        public string progression_key { get; set; }
        public string progression_name { get; set; }
    }

    public class LootLockerRewards
    {
        public List<LootLockerProgressionPointsReward> progression_points_rewards { get; set; }
        public List<LootLockerProgressionResetReward> progression_reset_rewards { get; set; }
        public List<LootLockerAssetReward> asset_rewards { get; set; }
    }

    public class LootLockerProgressionTier : LootLockerResponse
    {
        public string id { get; set; }
        public int step { get; set; }
        public int points_threshold { get; set; }
        public LootLockerRewards rewards { get; set; }
    }
    
    public class LootLockerPaginatedProgressionTiers : LootLockerResponse
    {
        public LootLockerPagination pagination { get; set; }
        public List<LootLockerProgressionTier> items { get; set; }
        
        public class LootLockerProgressionTier
        {
            public string id { get; set; }
            public int step { get; set; }
            public int points_threshold { get; set; }
            public LootLockerRewards rewards { get; set; }
        }
    }

    public abstract class LLResponse
    {
        
    }
}