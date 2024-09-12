using LootLocker;
using LootLocker.Requests;
using System;
using Random = UnityEngine.Random;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestLeaderboards
    {

        public static void GetAssetContexts(
            Action<bool, string, LootLockerTestContextResponse> onComplete /*success*/ /*errorMessage*/ /*context*/)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Already signed in
                onComplete?.Invoke(false, "Not logged in", null);
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.getAssetContexts;

            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                var contextResponse = LootLockerResponse.Deserialize<LootLockerTestContextResponse>(serverResponse);
                onComplete?.Invoke(contextResponse.success, contextResponse?.errorData?.message, contextResponse);
            }, true);
        }

        public static void CreateAsset(int contextID, Action<LootLockerTestAssetResponse /*asset*/> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Already signed in
                onComplete?.Invoke(null);
                return;
            }

            var assetRequest = new CreateLootLockerTestAsset
            {
                context_id = contextID,
                name = GetRandomAssetName()
            };

            var endpoint = LootLockerTestConfigurationEndpoints.createAsset;

            string json = LootLockerJson.SerializeObject(assetRequest);

            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                var assetResponse = LootLockerResponse.Deserialize<LootLockerTestAssetResponse>(serverResponse);
                onComplete?.Invoke(assetResponse);
            }, true);
        }

        public static string GetRandomAssetName()
        {
            string[] colors = { "Green", "Blue", "Red", "Black" };
            string[] items = { "Rod", "House", "Wand", "Staff" };

            return colors[Random.Range(0, colors.Length)] + " " + items[Random.Range(0, items.Length)];
        }

        public static void CreateReward(LootLockerRewardRequest request,
            Action<LootLockerRewardResponse> onComplete /*reward*/)
        {
            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.createReward;

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(endPoint.endPoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) =>
                {
                    var reward = LootLockerResponse.Deserialize<LootLockerRewardResponse>(serverResponse);
                    onComplete?.Invoke(reward);
                }, true);
        }
    }

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

        public void UpdateLeaderboard(UpdateLootLockerLeaderboardRequest request, Action<bool, string, LootLockerTestLeaderboard> onComplete /*success*/ /*errorMessage*/ /*leaderboard*/)
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

        public void UpdateLeaderboardSchedule(UpdateLootLockerLeaderboardScheduleRequest request,
            Action<bool, string, LootLockerTestLeaderboard> onComplete /*success*/ /*errorMessage*/ /*leaderboard*/)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Not signed in
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

            LootLockerTestLeaderboards.GetAssetContexts((success, errorMessage, response) =>
            {
                if (!success)
                {
                    onComplete?.Invoke(null);
                }

                LootLockerTestLeaderboards.CreateAsset(response.contexts[0].id, (assetResponse) =>
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

                    LootLockerTestLeaderboards.CreateReward(rewardRequest, (reward) =>
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