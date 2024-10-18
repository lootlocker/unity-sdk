using LootLocker;
using System;
using LootLocker.Requests;

namespace LootLockerTestConfigurationUtils
{
    public class LootLockerTestLeaderboard : LootLockerResponse
    {
        public int id { get; set; }
        public string key { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string direction_method { get; set; }
        public bool enable_game_api_writes { get; set; }
        public bool overwrite_score_on_submit { get; set; }
        public bool has_metadata { get; set; }
        public string type { get; set; }

        public void UpdateLeaderboard(UpdateLootLockerLeaderboardRequest request, Action<bool, string, LootLockerTestLeaderboard> onComplete)
        {
            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.updateLeaderboard;
            var formattedEndpoint = string.Format(endPoint.endPoint, id);

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(formattedEndpoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) =>
                {
                    var leaderboardResponse = LootLockerResponse.Deserialize<LootLockerTestLeaderboard>(serverResponse);
                    onComplete?.Invoke(leaderboardResponse.success, leaderboardResponse?.errorData?.message, leaderboardResponse);
                }, true);
        }

        public void UpdateLeaderboardSchedule(UpdateLootLockerLeaderboardScheduleRequest request, Action<bool, string, LootLockerTestLeaderboard> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(false, "Not logged in", null);
                return;
            }
            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.updateLeaderboardSchedule;
            var formattedEndpoint = string.Format(endPoint.endPoint, id);

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(formattedEndpoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) =>
                {
                    var leaderboardResponse = LootLockerResponse.Deserialize<LootLockerTestLeaderboard>(serverResponse);
                    onComplete?.Invoke(leaderboardResponse.success, leaderboardResponse?.errorData?.message, leaderboardResponse);
                }, true);
        }

        public void AddLeaderboardReward(Action<LootLockerLeaderboardDetailResponse> onComplete /*leaderboard*/)
        {

            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Not signed in
                onComplete?.Invoke(null);
                return;
            }

            LootLockerTestAssets.GetAssetContexts((success, errorMessage, response) =>
            {
                if (!success)
                {
                    onComplete?.Invoke(null);
                }

                LootLockerTestAssets.CreateAsset(response.contexts[0].id, (assetResponse) =>
                {
                    if (!assetResponse.success)
                    {
                        onComplete?.Invoke(null);
                    }

                    var rewardRequest = new LootLockerRewardRequest
                    {
                        entity_id = assetResponse.asset.ulid,
                        entity_kind = "asset"
                    };

                    LootLockerTestAssets.CreateReward(rewardRequest, (reward) =>
                    {
                        if (!reward.success)
                        {
                            return;
                        }
                        var request = new AddLootLockerLeaderboardRewardRequest
                        {
                            reward_id = reward.id,
                            reward_kind = rewardRequest.entity_kind,
                            predicates = new LootLockerLeaderboardRewardPredicates[1]
                        };

                        request.predicates[0] = new LootLockerLeaderboardRewardPredicates
                        {
                            type = "between",
                            args = new LootLockerLeaderboardRewardPredicatesArgs
                            {
                                min = 1,
                                max = 1,
                                method = "by_rank",
                                direction = "asc"
                            }
                        };

                        //Add asset reward into Leaderboard
                        EndPointClass endPoint = LootLockerTestConfigurationEndpoints.addLeaderboardReward;
                        var formattedEndpoint = string.Format(endPoint.endPoint, id);

                        string json = LootLockerJson.SerializeObject(request);

                        LootLockerAdminRequest.Send(formattedEndpoint, endPoint.httpMethod, json,
                            onComplete: (serverResponse) =>
                            {
                                var leaderboardResponse = LootLockerResponse.Deserialize<LootLockerLeaderboardDetailResponse>(serverResponse);
                                onComplete?.Invoke(leaderboardResponse);
                            }, true);
                    });
                });
            });
        }

    }

    public class UpdateLootLockerLeaderboardRequest
    {
        public string name { get; set; }
        public string key { get; set; }
        public string type { get; set; }
        public string direction_method { get; set; }
        public bool enable_game_api_writes { get; set; } = true;
        public bool overwrite_score_on_submit { get; set; } = false;
    }

    public class CreateLootLockerLeaderboardRequest
    {
        public string name { get; set; }
        public string key { get; set; }
        public string direction_method { get; set; }
        public bool enable_game_api_writes { get; set; }
        public bool overwrite_score_on_submit { get; set; }
        public bool has_metadata { get; set; }
        public string type { get; set; }
    }

    public class UpdateLootLockerLeaderboardScheduleRequest
    {
        public string cron_expression { get; set; }
    }

    public class AddLootLockerLeaderboardRewardRequest
    {
        public string reward_id { get; set; }
        public string reward_kind { get; set; }
        public LootLockerLeaderboardRewardPredicates[] predicates { get; set; }
    }

    public enum LootLockerLeaderboardSortDirection
    {
        ascending,
        descending,
    }

}