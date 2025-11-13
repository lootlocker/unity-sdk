using LootLocker;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void ActivateAsset(int asset_id, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.activateAsset;
            var formattedEndpoint = string.Format(endpoint.endPoint, asset_id);

            string json = LootLockerJson.SerializeObject(new LootLockerTestActivateAssetRequest());

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, json, onComplete, true);
        }

        public static void AddDataEntityToAsset(int asset_id, string name, string data, Action<LootLockerTestDataEntityResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var dataEntityRequest = new LootLockerTestDataEntityRequest
            {
                asset_id = asset_id,
                name = name,
                data = data
            };

            var endpoint = LootLockerTestConfigurationEndpoints.addDataEntityToAsset;

            string json = LootLockerJson.SerializeObject(dataEntityRequest);

            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                var assetResponse = LootLockerResponse.Deserialize<LootLockerTestDataEntityResponse>(serverResponse);
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

        public static void AddMetadataToAsset(string assetUlid, string key, string value, Action<LootLockerTestMetadata.LootLockerTestMetadataOperationsResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            LootLockerTestMetadata.PerformMetadataOperations(LootLockerTestMetadata.LootLockerTestMetadataSources.asset, assetUlid, new List<LootLockerTestMetadata.LootLockerTestMetadataOperation>
            {
                new LootLockerTestMetadata.LootLockerTestMetadataOperation
                {
                    action = LootLockerTestMetadata.LootLockerTestMetadataActions.upsert,
                    key = key,
                    value = value,
                    type = LootLockerTestMetadata.LootLockerTestMetadataTypes.String,
                    tags = new[] { "test", "asset", "metadata" },
                    access = new [] {"game_api.read"},

                }
            }, onComplete);
        }

        public static void UpdateAsset(string assetJson, int assetId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var formatted = string.Format(LootLockerTestConfigurationEndpoints.updateAsset.endPoint, assetId);

            LootLockerAdminRequest.Send(formatted, LootLockerTestConfigurationEndpoints.updateAsset.httpMethod, assetJson, onComplete: (updateResponse) =>
            {
                onComplete?.Invoke(updateResponse);
            }, true);
        }

        public static void BulkEditFiltersOnAssets(LootLockerTestAssetFiltersEdit[] edits, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.bulkEditAssetFilters;

            string json = LootLockerJson.SerializeObjectArray(edits);

            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerResponse>(serverResponse);
                onComplete?.Invoke(response);
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

    public class LootLockerTestAssetContext
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class LootLockerTestCompleteAsset : LootLocker.Requests.LootLockerCommonAsset
    {
        public bool global { get; set; }
        public int context_id { get; set; }
        public LootLockerTestAssetContext[] contexts { get; set; }
    }

    public class LootLockerTestSingleAssetResponse : LootLockerResponse
    {
        public LootLockerTestCompleteAsset asset { get; set; }
    }

    public class LootLockerTestActivateAssetRequest
    {
        public bool live_and_dev { get; set; } = true;
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

    public class LootLockerTestDataEntityRequest
    {
        public int asset_id { get; set; }
        public string name { get; set; }
        public string data { get; set; }
    }

    public class LootLockerTestContextResponse : LootLockerResponse
    {
        public LootLockerTestContext[] contexts { get; set; }
    }

    public class LootLockerTestDataEntityResponse : LootLockerResponse
    {
        public int id { get; set; }
        public string name { get; set; }
        public string data { get; set; }
        public string updated_at { get; set; }
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

    public class LootLockerTestAssetFiltersEdit
    {
        public class Filter
        {
            public string key { get; set; }
            public string value { get; set; }
        }

        public string action { get; set; } = "set";
        public int[] asset_ids { get; set; }

        public List<Filter> filters { get; set; } = new List<Filter>();
    }
}
