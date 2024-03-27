using System;
using System.Net;
using LootLocker.Requests;
using UnityEngine;

namespace LootLocker.Requests
{
    public class LootLockerLeaderboardDetailResponse : LootLockerResponse
    {
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string key { get; set; }
        public string direction_method { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public int id { get; set; }
        public int game_id { get; set; }
        public bool enable_game_api_writes { get; set; }
        public bool overwrite_score_on_submit { get; set; }
        public bool has_metadata { get; set; }
        public LootLockerLeaderboardSchedule schedule { get; set; }

        public LootLockerLeaderboardReward[] rewards { get; set; }

    }

    public class LootLockerLeaderboardRewardCurrency
    {
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string amount { get; set; }
        public LootLockerLeaderboardRewardCurrencyDetails details { get; set; }
        public string reward_id { get; set; }
        public string currency_id { get; set; }
    }

    public class LootLockerLeaderboardRewardCurrencyDetails
    {
        public string name { get; set; }
        public string code { get; set; }
        public string amount { get; set; }
        public string id { get; set; }
    }

    public class LootLockerLeaderboardRewardProgression
    {
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public LootLockerLeaderboardRewardPorgressionDetails details { get; set; }
        public int amount { get; set; }
        public string progression_id { get; set; }
        public string reward_id { get; set; }

    }

    public class LootLockerLeaderboardRewardPorgressionDetails
    {
        public string key { get; set; }
        public string name { get; set; }
        public int amount { get; set; }
        public string id { get; set; }
    }

    public class LootLockerLeaderboardRewardProgressionReset
    {
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public LootLockerLeaderboardRewardProgressionResetDetails details { get; set; }
        public string progression_id { get; set; }
        public string reward_id { get; set; }
    }
    public class LootLockerLeaderboardRewardProgressionResetDetails
    {
        public string key { get; set; }
        public string name { get; set; }
        public string id { get; set; }
    }

    public class LootLockerLeaderboardRewardAsset
    {
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public LootLockerLeaderboardRewardAssetDetails details { get; set; }
        public string asset_variation_id { get; set; }
        public string asset_rental_option_id { get; set; }
        public int asset_id { get; set; }
        public string reward_id { get; set; }
        public string asset_ulid { get; set; }

    }

    public class LootLockerLeaderboardRewardAssetDetails
    {
        public string name { get; set; }
        public string thumbnail { get; set; }
        public string variation_name { get; set; }
        public string rental_option_name { get; set; }
        public int? variation_id { get; set; }
        public int? rental_option_id { get; set; }
        public int legacy_id { get; set; }
        public string id { get; set; }
    }

    public class LootLockerLeaderboardReward
    {
        public string reward_kind { get; set; }
        public LootLockerLeaderboardRewardPredicates predicates { get; set; }
        public LootLockerLeaderboardRewardCurrency currency { get; set; }
        public LootLockerLeaderboardRewardProgressionReset progression_reset { get; set; }
        public LootLockerLeaderboardRewardProgression progression_points { get; set; }
        public LootLockerLeaderboardRewardAsset asset { get; set; }

    }

    public class LootLockerLeaderboardRewardPredicates
    {
        public string id { get; set; }
        public string type { get; set; }
        public LootLockerLeaderboardRewardPredicatesArgs args { get; set; }

    }

    public class LootLockerLeaderboardRewardPredicatesArgs
    {
        public int max { get; set; }
        public int min { get; set; }
        public string method { get; set; }
        public string direction { get; set; }

    }

    public class LootLockerLeaderboardSchedule
    {
        public string cron_expression { get; set; }
        public string next_run { get; set; }
        public string last_run { get; set; }
        public string[] schedule { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetLeaderboardData(string leaderboard_key, Action<LootLockerLeaderboardDetailResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getLeaderboardData;
            string formatedEndPoint = string.Format(endPoint.endPoint, leaderboard_key);
            LootLockerServerRequest.CallAPI(formatedEndPoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}