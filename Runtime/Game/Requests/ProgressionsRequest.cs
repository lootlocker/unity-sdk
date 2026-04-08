using System;
using System.Collections.Generic;

namespace LootLocker.Requests
{
    /// <summary>
    /// Response containing details about a single progression definition.
    /// </summary>
    public class LootLockerProgressionResponse : LootLockerResponse
    {
        /// <summary>The unique identifier of the progression.</summary>
        public string id { get; set; }
        /// <summary>The key used to identify this progression in the API.</summary>
        public string key { get; set; }
        /// <summary>The human-readable name of the progression.</summary>
        public string name { get; set; }
        /// <summary>Whether this progression is currently active.</summary>
        public bool active { get; set; }
    }
    
    /// <summary>
    /// Response containing a paginated list of progression definitions.
    /// </summary>
    public class LootLockerPaginatedProgressionsResponse : LootLockerResponse
    {
        /// <summary>Pagination data for iterating through the full list.</summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }
        /// <summary>The list of progression definitions returned by this page.</summary>
        public List<LootLockerProgression> items { get; set; }

        /// <summary>
        /// A single progression definition within a paginated progressions list.
        /// </summary>
        public class LootLockerProgression
        {
            /// <summary>The unique identifier of the progression.</summary>
            public string id { get; set; }
            /// <summary>The key used to identify this progression in the API.</summary>
            public string key { get; set; }
            /// <summary>The human-readable name of the progression.</summary>
            public string name { get; set; }
            /// <summary>Whether this progression is currently active.</summary>
            public bool active { get; set; }
        }
    }
    
    /// <summary>
    /// Response containing the current progression status for a player on a specific progression.
    /// </summary>
    public class LootLockerPlayerProgressionResponse : LootLockerResponse
    {
        /// <summary>The unique identifier of this player progression record.</summary>
        public string id { get; set; }
        /// <summary>The key of the progression this record belongs to.</summary>
        public string progression_key { get; set; }
        /// <summary>The id of the progression this record belongs to.</summary>
        public string progression_id { get; set; }
        /// <summary>The name of the progression this record belongs to.</summary>
        public string progression_name { get; set; }
        /// <summary>The current level (step) the player has reached in this progression.</summary>
        public ulong step { get; set; }
        /// <summary>The total number of points accumulated in this progression.</summary>
        public ulong points { get; set; }
        /// <summary>The points threshold for the current level.</summary>
        public ulong previous_threshold { get; set; }
        /// <summary>The points threshold for the next level, or null if the player is at the maximum level.</summary>
        public ulong? next_threshold { get; set; }
        /// <summary>When the player last levelled up in this progression, or null if they have never levelled up.</summary>
        public DateTime? last_level_up { get; set; }
    }
    
    /// <summary>
    /// Response containing a paginated list of a player's progression statuses across multiple progressions.
    /// </summary>
    public class LootLockerPaginatedPlayerProgressionsResponse : LootLockerResponse
    {
        /// <summary>Pagination data for iterating through the full list.</summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }
        /// <summary>The list of player progression statuses returned by this page.</summary>
        public List<LootLockerPlayerProgression> items { get; set; }

        /// <summary>
        /// A single player progression status within a paginated player progressions list.
        /// </summary>
        public class LootLockerPlayerProgression
        {
            /// <summary>The unique identifier of this player progression record.</summary>
            public string id { get; set; }
            /// <summary>The key of the progression this record belongs to.</summary>
            public string progression_key { get; set; }
            /// <summary>The id of the progression this record belongs to.</summary>
            public string progression_id { get; set; }
            /// <summary>The name of the progression this record belongs to.</summary>
            public string progression_name { get; set; }
            /// <summary>The current level (step) the player has reached in this progression.</summary>
            public ulong step { get; set; }
            /// <summary>The total number of points accumulated in this progression.</summary>
            public ulong points { get; set; }
            /// <summary>The points threshold for the current level.</summary>
            public ulong previous_threshold { get; set; }
            /// <summary>The points threshold for the next level, or null if the player is at the maximum level.</summary>
            public ulong? next_threshold { get; set; }
            /// <summary>When the player last levelled up in this progression, or null if they have never levelled up.</summary>
            public DateTime? last_level_up { get; set; }
        }
    }

    /// <summary>
    /// Response containing the updated player progression status along with any tiers awarded as a result of the points change.
    /// </summary>
    public class LootLockerPlayerProgressionWithRewardsResponse : LootLockerPlayerProgressionResponse
    {
        /// <summary>List of progression tiers awarded to the player as a result of the points change.</summary>
        public List<LootLockerAwardedTier> awarded_tiers { get; set; }
    }

    /// <summary>
    /// Response containing the current progression status for a character on a specific progression.
    /// </summary>
    public class LootLockerCharacterProgressionResponse : LootLockerResponse
    {
        /// <summary>The unique identifier of this character progression record.</summary>
        public string id { get; set; }
        /// <summary>The key of the progression this record belongs to.</summary>
        public string progression_key { get; set; }
        /// <summary>The id of the progression this record belongs to.</summary>
        public string progression_id { get; set; }
        /// <summary>The name of the progression this record belongs to.</summary>
        public string progression_name { get; set; }
        /// <summary>The current level (step) the character has reached in this progression.</summary>
        public ulong step { get; set; }
        /// <summary>The total number of points accumulated in this progression.</summary>
        public ulong points { get; set; }
        /// <summary>The points threshold for the current level.</summary>
        public ulong previous_threshold { get; set; }
        /// <summary>The points threshold for the next level, or null if the character is at the maximum level.</summary>
        public ulong? next_threshold { get; set; }
        /// <summary>When the character last levelled up in this progression, or null if they have never levelled up.</summary>
        public DateTime? last_level_up { get; set; }
    }
    
    /// <summary>
    /// Response containing a paginated list of a character's progression statuses across multiple progressions.
    /// </summary>
    public class LootLockerPaginatedCharacterProgressionsResponse : LootLockerResponse
    {
        /// <summary>Pagination data for iterating through the full list.</summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }
        public List<LootLockerCharacterProgression> items { get; set; }

        /// <summary>
        /// A single character progression status within a paginated character progressions list.
        /// </summary>
        public class LootLockerCharacterProgression
        {
            /// <summary>The unique identifier of this character progression record.</summary>
            public string id { get; set; }
            /// <summary>The key of the progression this record belongs to.</summary>
            public string progression_key { get; set; }
            /// <summary>The id of the progression this record belongs to.</summary>
            public string progression_id { get; set; }
            /// <summary>The name of the progression this record belongs to.</summary>
            public string progression_name { get; set; }
            /// <summary>The current level (step) the character has reached in this progression.</summary>
            public ulong step { get; set; }
            /// <summary>The total number of points accumulated in this progression.</summary>
            public ulong points { get; set; }
            /// <summary>The points threshold for the current level.</summary>
            public ulong previous_threshold { get; set; }
            /// <summary>The points threshold for the next level, or null if the character is at the maximum level.</summary>
            public ulong? next_threshold { get; set; }
            /// <summary>When the character last levelled up in this progression, or null if they have never levelled up.</summary>
            public DateTime? last_level_up { get; set; }
        }
    }

    /// <summary>
    /// Response containing the updated character progression status along with any tiers awarded as a result of the points change.
    /// </summary>
    public class LootLockerCharacterProgressionWithRewardsResponse : LootLockerCharacterProgressionResponse
    {
        /// <summary>List of progression tiers awarded to the character as a result of the points change.</summary>
        public List<LootLockerAwardedTier> awarded_tiers { get; set; }
    }
    
    /// <summary>
    /// Response containing the current progression status for an asset instance on a specific progression.
    /// </summary>
    public class LootLockerAssetInstanceProgressionResponse : LootLockerResponse
    {
        /// <summary>The unique identifier of this asset instance progression record.</summary>
        public string id { get; set; }
        /// <summary>The key of the progression this record belongs to.</summary>
        public string progression_key { get; set; }
        /// <summary>The id of the progression this record belongs to.</summary>
        public string progression_id { get; set; }
        /// <summary>The name of the progression this record belongs to.</summary>
        public string progression_name { get; set; }
        /// <summary>The current level (step) the asset instance has reached in this progression.</summary>
        public ulong step { get; set; }
        /// <summary>The total number of points accumulated in this progression.</summary>
        public ulong points { get; set; }
        /// <summary>The points threshold for the current level.</summary>
        public ulong previous_threshold { get; set; }
        /// <summary>The points threshold for the next level, or null if the asset instance is at the maximum level.</summary>
        public ulong? next_threshold { get; set; }
        /// <summary>When the asset instance last levelled up in this progression, or null if it has never levelled up.</summary>
        public DateTime? last_level_up { get; set; }
    }

    /// <summary>
    /// Response containing a paginated list of an asset instance's progression statuses across multiple progressions.
    /// </summary>
    public class LootLockerPaginatedAssetInstanceProgressionsResponse : LootLockerResponse
    {
        /// <summary>Pagination data for iterating through the full list.</summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }
        /// <summary>The list of asset instance progression statuses returned by this page.</summary>
        public List<LootLockerAssetInstanceProgression> items { get; set; }
    
        /// <summary>
        /// A single asset instance progression status within a paginated asset instance progressions list.
        /// </summary>
        public class LootLockerAssetInstanceProgression
        {
            /// <summary>The unique identifier of this asset instance progression record.</summary>
            public string id { get; set; }
            /// <summary>The key of the progression this record belongs to.</summary>
            public string progression_key { get; set; }
            /// <summary>The id of the progression this record belongs to.</summary>
            public string progression_id { get; set; }
            /// <summary>The name of the progression this record belongs to.</summary>
            public string progression_name { get; set; }
            /// <summary>The current level (step) the asset instance has reached in this progression.</summary>
            public ulong step { get; set; }
            /// <summary>The total number of points accumulated in this progression.</summary>
            public ulong points { get; set; }
            /// <summary>The points threshold for the current level.</summary>
            public ulong previous_threshold { get; set; }
            /// <summary>The points threshold for the next level, or null if the asset instance is at the maximum level.</summary>
            public ulong? next_threshold { get; set; }
            /// <summary>When the asset instance last levelled up in this progression, or null if it has never levelled up.</summary>
            public DateTime? last_level_up { get; set; }
        }
    }

    /// <summary>
    /// Response containing the updated asset instance progression status along with any tiers awarded as a result of the points change.
    /// </summary>
    public class LootLockerAssetInstanceProgressionWithRewardsResponse : LootLockerAssetInstanceProgressionResponse
    {
        /// <summary>List of progression tiers awarded to the asset instance as a result of the points change.</summary>
        public List<LootLockerAwardedTier> awarded_tiers { get; set; }
    }

    /// <summary>
    /// A progression tier that was awarded to a player, character, or asset instance as a result of earning enough points.
    /// </summary>
    public class LootLockerAwardedTier
    {
        /// <summary>The level number (step) of the tier that was awarded.</summary>
        public ulong step { get; set; }
        /// <summary>The cumulative points threshold required to reach this tier.</summary>
        public ulong points_threshold { get; set; }
        /// <summary>The rewards granted when this tier was unlocked.</summary>
        public LootLockerRewards rewards { get; set; }
    }
    
    /// <summary>
    /// An asset reward attached to a progression tier, describing which asset (and optionally which variation or rental option) is granted.
    /// </summary>
    public class LootLockerAssetReward
    {
        /// <summary>The id of the asset to grant.</summary>
        public int asset_id { get; set; }
        /// <summary>The id of the asset variation to grant, or null for the default variation.</summary>
        public int? asset_variation_id { get; set; }
        /// <summary>The id of the rental option to grant, or null if the asset is not rented.</summary>
        public int? asset_rental_option_id { get; set; }
    }

    /// <summary>
    /// A progression points reward attached to a progression tier, describing how many points are added to another progression when this tier is unlocked.
    /// </summary>
    public class LootLockerProgressionPointsReward
    {
        /// <summary>The id of the progression to which points will be added.</summary>
        public string progression_id { get; set; }
        /// <summary>The key of the progression to which points will be added.</summary>
        public string progression_key { get; set; }
        /// <summary>The name of the progression to which points will be added.</summary>
        public string progression_name { get; set; }
        /// <summary>The number of points to add to the target progression.</summary>
        public ulong amount { get; set; }
    }

    /// <summary>
    /// A progression reset reward attached to a progression tier, causing another progression to be reset when this tier is unlocked.
    /// </summary>
    public class LootLockerProgressionResetReward
    {
        /// <summary>The id of the progression that will be reset.</summary>
        public string progression_id { get; set; }
        /// <summary>The key of the progression that will be reset.</summary>
        public string progression_key { get; set; }
        /// <summary>The name of the progression that will be reset.</summary>
        public string progression_name { get; set; }
    }

    /// <summary>
    /// A currency reward attached to a progression tier, describing how much of a given currency is granted when this tier is unlocked.
    /// </summary>
    public class LootLockerCurrencyReward
    {
        /// <summary>The human-readable name of the currency.</summary>
        public string currency_name { get; set; }
        /// <summary>The short code used to identify the currency.</summary>
        public string currency_code { get; set; }
        /// <summary>The amount of currency to grant, as a string to preserve precision.</summary>
        public string amount { get; set; }
    }

    /// <summary>
    /// The full set of rewards attached to a progression tier, across all reward types.
    /// </summary>
    public class LootLockerRewards
    {
        /// <summary>Rewards that grant points to another progression.</summary>
        public List<LootLockerProgressionPointsReward> progression_points_rewards { get; set; }
        /// <summary>Rewards that reset another progression.</summary>
        public List<LootLockerProgressionResetReward> progression_reset_rewards { get; set; }
        /// <summary>Rewards that grant an asset to the player's inventory.</summary>
        public List<LootLockerAssetReward> asset_rewards { get; set; }
        /// <summary>Rewards that grant an amount of currency to the player.</summary>
        public List<LootLockerCurrencyReward> currency_rewards { get; set; }
    }

    /// <summary>
    /// Response containing a paginated list of tiers for a specific progression.
    /// </summary>
    public class LootLockerPaginatedProgressionTiersResponse : LootLockerResponse
    {
        /// <summary>Pagination data for iterating through the full list.</summary>
        public LootLockerPaginationResponse<ulong?> pagination { get; set; }
        /// <summary>The list of progression tiers returned by this page.</summary>
        public List<LootLockerProgressionTier> items { get; set; }
        
        /// <summary>
        /// A single tier definition within a paginated progression tiers list.
        /// </summary>
        public class LootLockerProgressionTier
        {
            /// <summary>The level number (step) of this tier.</summary>
            public ulong step { get; set; }
            /// <summary>The cumulative points threshold required to reach this tier.</summary>
            public ulong points_threshold { get; set; }
            /// <summary>The rewards granted when this tier is unlocked.</summary>
            public LootLockerRewards rewards { get; set; }
        }
    }
    
    /// <summary>
    /// Response containing details about a single progression tier.
    /// </summary>
    public class LootLockerProgressionTierResponse : LootLockerResponse
    {
        /// <summary>The level number (step) of this tier.</summary>
        public ulong step { get; set; }
        /// <summary>The cumulative points threshold required to reach this tier.</summary>
        public ulong points_threshold { get; set; }
        /// <summary>The rewards granted when this tier is unlocked.</summary>
        public LootLockerRewards rewards { get; set; }
    }
}