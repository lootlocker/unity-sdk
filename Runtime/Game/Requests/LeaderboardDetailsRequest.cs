using System;
using System.Net;
using LootLocker.Requests;
using UnityEngine;

namespace LootLocker.Requests
{
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
        /// Schedule of the Leaderboard.
        /// </summary>
        public LootLockerLeaderboardSchedule schedule { get; set; }
        /// <summary>
        /// A List of rewards tied to the Leaderboard.
        /// </summary>
        public LootLockerLeaderboardReward[] rewards { get; set; }
        /// <summary>
        /// The ulid for this leaderboard
        /// </summary>
        public string ulid { get; set; }
    }

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