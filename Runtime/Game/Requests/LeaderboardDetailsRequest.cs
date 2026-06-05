using System;
using System.Net;
using LootLocker.Requests;
using UnityEngine;

namespace LootLocker.Requests
{
    /// <summary>
    /// Response containing the full configuration details of a specific leaderboard, including its schedule and rewards.
    /// </summary>
    public class LootLockerLeaderboardDetailResponse : LootLockerResponse
    {
        /// <summary>
        /// The date the Leaderboard was created.
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// The date the Leaderboard was last updated.
        /// </summary>
        public string updated_at { get; set; }
        /// <summary>
        /// The Leaderboards Key.
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// The direction of the Leaderboard (Ascending / Descending).
        /// </summary>
        public string direction_method { get; set; }
        /// <summary>
        /// The name of the Leaderboard.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The type of the Leaderboard (Player / Generic).
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// Will the score be overwritten even if it was less than the original score.
        /// </summary>
        public bool overwrite_score_on_submit { get; set; }
        /// <summary>
        /// Does the Leaderboard have metadata.
        /// </summary>
        public bool has_metadata { get; set; }
        /// <summary>
        /// Whether this leaderboard allows manual resets to be requested via the server API.
        /// </summary>
        public bool allow_manual_resets { get; set; }
        /// <summary>
        /// Schedule of the Leaderboard.
        /// </summary>
        public LootLockerLeaderboardSchedule schedule { get; set; }
        /// <summary>
        /// A List of rewards tied to the Leaderboard.
        /// </summary>
        public LootLockerLeaderboardReward[] rewards { get; set; }
        /// <summary>
        /// Pending manual resets that have been requested but not yet processed, ordered by scheduled_for ascending.
        /// Only populated when allow_manual_resets is true.
        /// </summary>
        public LootLockerLeaderboardUpcomingReset[] upcoming_resets { get; set; }
        /// <summary>
        /// The ulid for this leaderboard
        /// </summary>
        public string ulid { get; set; }
    }

    /// <summary>
    /// A pending manual reset for a leaderboard that has been requested but not yet processed.
    /// </summary>
    public class LootLockerLeaderboardUpcomingReset
    {
        /// <summary>
        /// The unique ID of this manual reset request.
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// An optional human-readable name for this reset.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The UTC time at which this reset is scheduled to be processed (ISO 8601).
        /// </summary>
        public string scheduled_for { get; set; }
    }

    /// <summary>
    /// A currency reward attached to a leaderboard, describing the currency and amount to grant when the reward is triggered.
    /// </summary>
    public class LootLockerLeaderboardRewardCurrency
    {
        /// <summary>
        /// The date the Currency reward was created.
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// The date the Currency reward was last updated.
        /// </summary>
        public string updated_at { get; set; }
        /// <summary>
        /// The amount of Currency to be rewarded.
        /// </summary>
        public string amount { get; set; }
        /// <summary>
        /// The details on the Currency.
        /// </summary>
        public LootLockerLeaderboardRewardCurrencyDetails details { get; set; }
        /// <summary>
        /// The ID of a reward.
        /// </summary>
        public string reward_id { get; set; }
        /// <summary>
        /// The ID of the Currency.
        /// </summary>
        public string currency_id { get; set; }
    }

    /// <summary>
    /// Details about the currency associated with a leaderboard currency reward.
    /// </summary>
    public class LootLockerLeaderboardRewardCurrencyDetails
    {
        /// <summary>
        /// The name of the Currency.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The code of the Currency.
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// The amount of the Currency.
        /// </summary>
        public string amount { get; set; }
        /// <summary>
        /// The ID of the Currency.
        /// </summary>
        public string id { get; set; }
    }

    /// <summary>
    /// A progression points reward attached to a leaderboard, granting points to a specific progression when triggered.
    /// </summary>
    public class LootLockerLeaderboardRewardProgression
    {
        /// <summary>
        /// The date the Progression Points reward was created.
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// The date the Progression Points was last updated.
        /// </summary>
        public string updated_at { get; set; }
        /// <summary>
        /// The details of the Progression.
        /// </summary>
        public LootLockerLeaderboardRewardProgressionDetails details { get; set; }
        /// <summary>
        /// The amount of Progression Points to be rewarded.
        /// </summary>
        public int amount { get; set; }
        /// <summary>
        /// The ID of the Progression.
        /// </summary>
        public string progression_id { get; set; }
        /// <summary>
        /// The ID of the reward.
        /// </summary>
        public string reward_id { get; set; }
    }

    /// <summary>
    /// Details about the progression associated with a leaderboard progression points reward.
    /// </summary>
    public class LootLockerLeaderboardRewardProgressionDetails
    {
        /// <summary>
        /// The key of the Progression.
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// The name of the Progression.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The amount of Progression Points to be rewarded.
        /// </summary>
        public int amount { get; set; }
        /// <summary>
        /// The ID of the Progression.
        /// </summary>
        public string id { get; set; }
    }

    /// <summary>
    /// A progression reset reward attached to a leaderboard, resetting a specific progression when triggered.
    /// </summary>
    public class LootLockerLeaderboardRewardProgressionReset
    {
        /// <summary>
        /// The date the Progression Reset reward was created.
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// The date the Progression Reset reward was last updated.
        /// </summary>
        public string updated_at { get; set; }
        /// <summary>
        /// The details of the Progression reward.
        /// </summary>
        public LootLockerLeaderboardRewardProgressionResetDetails details { get; set; }
        /// <summary>
        /// The ID of the Progression.
        /// </summary>
        public string progression_id { get; set; }
        /// <summary>
        /// The ID of the reward.
        /// </summary>
        public string reward_id { get; set; }
    }

    /// <summary>
    /// Details about the progression associated with a leaderboard progression reset reward.
    /// </summary>
    public class LootLockerLeaderboardRewardProgressionResetDetails
    {
        /// <summary>
        /// The key of the Progression.
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// The name of the Progression.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The ID of the Progression.
        /// </summary>
        public string id { get; set; }
    }

    /// <summary>
    /// An asset reward attached to a leaderboard, granting a specific asset (or variation or rental) when triggered.
    /// </summary>
    public class LootLockerLeaderboardRewardAsset
    {
        /// <summary>
        /// The date the Asset reward was created.
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// The date the Asset reward was last updated.
        /// </summary>
        public string updated_at { get; set; }
        /// <summary>
        /// The details on the Asset.
        /// </summary>
        public LootLockerLeaderboardRewardAssetDetails details { get; set; }
        /// <summary>
        /// The Asset variation ID, will be null if its not a variation.
        /// </summary>
        public int? asset_variation_id { get; set; }
        /// <summary>
        /// The Asset rental option ID, will be null if its not a rental.
        /// </summary>
        public int? asset_rental_option_id { get; set; }
        /// <summary>
        /// The ID of the Asset.
        /// </summary>
        public int asset_id { get; set; }
        /// <summary>
        /// The ID of the reward.
        /// </summary>
        public string reward_id { get; set; }
        /// <summary>
        /// The ULID of the Asset.
        /// </summary>
        public string asset_ulid { get; set; }
    }

    /// <summary>
    /// Details about the asset associated with a leaderboard asset reward.
    /// </summary>
    public class LootLockerLeaderboardRewardAssetDetails
    {
        /// <summary>
        /// The name of the Asset.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The url to the thumbnail, will be null if its not set in the LootLocker console.
        /// </summary>
        public string thumbnail { get; set; }
        /// <summary>
        /// The name of the Variation Asset, will be null if its not a Variation Asset.
        /// </summary>
        public string variation_name { get; set; }
        /// <summary>
        /// The name of the Rental Asset, will be null if its not a Variation Asset.
        /// </summary>
        public string rental_option_name { get; set; }
        /// <summary>
        /// The ID of the Variation, will be null if its not a Variation Asset.
        /// </summary>
        public int? variation_id { get; set; }
        /// <summary>
        /// The ID of the rental option, will be null if its not a Rental Asset.
        /// </summary>
        public int? rental_option_id { get; set; }
        /// <summary>
        /// The ID of the Asset.
        /// </summary>
        public int legacy_id { get; set; }
        /// <summary>
        /// the ULID of the Asset.
        /// </summary>
        public string id { get; set; }
    }

    /// <summary>
    /// A reward attached to a leaderboard. The reward kind determines which reward type field is populated.
    /// </summary>
    public class LootLockerLeaderboardReward
    {
        /// <summary>
        /// The kind of reward, (asset / currency / group / progression points / progression reset).
        /// </summary>
        public string reward_kind { get; set; }
        /// <summary>
        /// The Predicates of the reward.
        /// </summary>
        public LootLockerLeaderboardRewardPredicates[] predicates { get; set; }
        /// <summary>
        /// The currency reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerLeaderboardRewardCurrency currency { get; set; }
        /// <summary>
        /// The Progression Reset reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerLeaderboardRewardProgressionReset progression_reset { get; set; }
        /// <summary>
        /// The Progression Points reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerLeaderboardRewardProgression progression_points { get; set; }
        /// <summary>
        /// The Asset reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerLeaderboardRewardAsset asset { get; set; }
        /// <summary>
        /// The Group reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerLeaderboardRewardGroup group { get; set; }
    }

    /// <summary>
    /// Rank range predicates determining which players receive a specific leaderboard reward.
    /// </summary>
    public class LootLockerLeaderboardRewardPredicates
    {
        /// <summary>
        /// The ID of the reward predicate.
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The type of reward predicate.
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// The details on predicate.
        /// </summary>
        public LootLockerLeaderboardRewardPredicatesArgs args { get; set; }
    }

    /// <summary>
    /// The rank range arguments for a leaderboard reward predicate, defining the min/max rank boundaries and method.
    /// </summary>
    public class LootLockerLeaderboardRewardPredicatesArgs
    {
        /// <summary>
        /// Max predicate to reward.
        /// </summary>
        public int max { get; set; }
        /// <summary>
        /// Min predicate to reward.
        /// </summary>
        public int min { get; set; }
        /// <summary>
        /// The reward method (by_rank / by_percent).
        /// </summary>
        public string method { get; set; }
        /// <summary>
        /// The direction of the predicate (asc / desc).
        /// </summary>
        public string direction { get; set; }
    }

    /// <summary>
    /// A group reward attached to a leaderboard, bundling multiple rewards of different types into a single named group.
    /// </summary>
    public class LootLockerLeaderboardRewardGroup
    {
        /// <summary>
        /// The date the Group reward was created.
        /// </summary>
        public string created_at { get; set; }
        
        /// <summary>
        /// The name of the Group.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The description of the Group.
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// Associations for the Group reward.
        /// </summary>
        public LootLockerLeaderboardGroupRewardAssociations[] associations { get; set; }

        /// <summary>
        /// The ID of the reward.
        /// </summary>
        public string reward_id { get; set; }
    }


    /// <summary>
    /// A single reward entry within a leaderboard reward group, specifying the kind and the associated reward details.
    /// </summary>
    public class LootLockerLeaderboardGroupRewardAssociations
    {
        /// <summary>
        /// The kind of reward, (asset / currency / group / progression points / progression reset).
        /// </summary>
        public string kind { get; set; }

        /// <summary>
        /// The details on the Asset.
        /// </summary>
        public LootLockerLeaderboardRewardAssetDetails asset { get; set; }

        /// <summary>
        /// The details on the Currency.
        /// </summary>
        public LootLockerLeaderboardRewardCurrencyDetails currency { get; set; }

        /// <summary>
        /// The Progression Points reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerLeaderboardRewardProgression progression_points { get; set; }

        /// <summary>
        /// The Progression Reset reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerLeaderboardRewardProgressionReset progression_reset { get; set; }

    }

    /// <summary>
    /// The reset schedule configuration for a leaderboard, including a cron expression and the next and last reset dates.
    /// </summary>
    public class LootLockerLeaderboardSchedule
    {
        /// <summary>
        /// Cron expression used to define the scheduling.
        /// </summary>
        public string cron_expression { get; set; }
        /// <summary>
        /// The date when the next Leaderboard reset wil happen.
        /// </summary>
        public string next_run { get; set; }
        /// <summary>
        /// The date when the last Leaderboard reset happened.
        /// </summary>
        public string last_run { get; set; }
        /// <summary>
        /// A list of all the schedules that has ran.
        /// </summary>
        public string[] schedule { get; set; }
    }
}