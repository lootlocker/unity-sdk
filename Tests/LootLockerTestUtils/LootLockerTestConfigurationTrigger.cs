using LootLocker;
using System;

namespace LootLockerTestConfigurationUtils
{
    public class LootLockerTestTrigger
    {
        public string key { get; set; }
        public string name { get; set; }
        public int limit { get; set; }
        public string reward_id { get; set; }
    }

    public static class LootLockerTestConfigurationTrigger
    {
        public static void CreateTrigger(string key, string name, int limit, string rewardId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var request = new LootLockerTestTrigger
            {
                key = key,
                name = name,
                limit = limit,
                reward_id = rewardId
            };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(LootLockerTestConfigurationEndpoints.createTrigger.endPoint, LootLockerTestConfigurationEndpoints.createTrigger.httpMethod, json, onComplete: (serverResponse) =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }
    }
}
