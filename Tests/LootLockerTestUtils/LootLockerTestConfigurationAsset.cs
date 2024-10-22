using LootLocker;
using System;
using Random = UnityEngine.Random;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestAssets
    {
        public static void GetAssetContexts(Action<bool, string, LootLockerTestContextResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
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

        public static void CreateAsset(int contextID, Action<LootLockerTestAssetResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
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
            string[] colors = { "Green", "Blue", "Red", "Black", "Yellow", "Orange", "Purple", "Indigo", "Clear", "White", "Magenta", "Marine", "Crimson", "Teal" };
            string[] items = { "Rod", "House", "Wand", "Staff", "Car", "Sword", "Shield", "Gun", "Shovel", "Boomstick", "Rifle", "Hut", "Boat", "Bicycle", "Wheelchair" };

            return colors[Random.Range(0, colors.Length)] + " " + items[Random.Range(0, items.Length)];
        }

        public static void CreateReward(LootLockerRewardRequest request, Action<LootLockerRewardResponse> onComplete)
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

    public class LootLockerTestAssetResponse : LootLockerResponse
    {
        public LootLockerTestAsset asset { get; set; }
    }

    public class LootLockerTestAsset
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string ulid { get; set; }
        public string name { get; set; }
    }

    public class LootLockerRewardResponse : LootLockerResponse
    {
        public string id { get; set; }
    }

    public class LootLockerRewardRequest
    {
        public string entity_id { get; set; }
        public string entity_kind { get; set; }
    }

    public class LootLockerTestContextResponse : LootLockerResponse
    {
        public LootLockerTestContext[] contexts { get; set; }
    }

    public class LootLockerTestContext
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string name { get; set; }
    }

    public class CreateLootLockerTestAsset
    {
        public int context_id { get; set; }
        public string name { get; set; }
    }
}
