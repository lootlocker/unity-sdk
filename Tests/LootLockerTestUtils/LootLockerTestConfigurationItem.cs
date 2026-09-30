using System;
using LootLocker;
using Random = UnityEngine.Random;

namespace LootLockerTestConfigurationUtils
{
    /// <summary>
    /// Admin API helpers for setting up item templates and player inventory items in tests.
    /// </summary>
    /// <remarks>
    /// These helpers deliberately mirror the go-backend admin routes rather than the published
    /// ApiDog spec, which is stale for this feature. Notable differences from the spec:
    /// the list endpoint returns <c>templates</c> (not <c>items</c>), grant returns <c>id</c>
    /// (not <c>inventory_id</c>), consume requires a body and returns a count (not a bool),
    /// and the player inventory listing uses <c>template_name</c> and has no <c>deletable</c>.
    /// </remarks>
    public static class LootLockerTestItems
    {
        public static string GetRandomItemTemplateName()
        {
            string[] colors = { "Green", "Blue", "Red", "Black", "Yellow", "Orange", "Purple", "Indigo", "Clear", "White", "Magenta", "Marine", "Crimson", "Teal" };
            string[] items = { "Rod", "House", "Wand", "Staff", "Car", "Sword", "Shield", "Gun", "Shovel", "Boomstick", "Rifle", "Hut", "Boat", "Bicycle", "Wheelchair" };

            return colors[Random.Range(0, colors.Length)] + " " + items[Random.Range(0, items.Length)];
        }

        /// <summary>
        /// Create an item template. Audiences are set inline by the create endpoint, which is
        /// required for the template to be visible to the player through the game API.
        /// </summary>
        public static void CreateItemTemplate(string name, string itemType, bool consumable, bool deletable, string[] audiences, Action<LootLockerTestItemTemplateResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerTestItemTemplateResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var request = new LootLockerTestCreateItemTemplateRequest
            {
                name = name,
                item_type = itemType,
                consumable = consumable,
                deletable = deletable,
                audiences = audiences
            };

            var endpoint = LootLockerTestConfigurationEndpoints.createItemTemplate;
            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerTestItemTemplateResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }

        /// <summary>
        /// Grant an item template to a player. Returns the id of the created inventory item.
        /// </summary>
        public static void GrantItemToPlayer(int playerId, string itemTemplateId, int count, Action<LootLockerTestGrantItemResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerTestGrantItemResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var request = new LootLockerTestGrantItemRequest
            {
                player_id = playerId,
                item_template_id = itemTemplateId,
                count = count
            };

            var endpoint = LootLockerTestConfigurationEndpoints.grantItemTemplate;
            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerTestGrantItemResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }

        public static void SetItemTemplateAudiences(string itemTemplateId, string[] audiences, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.setItemTemplateAudiences;
            string formattedEndpoint = string.Format(endpoint.endPoint, itemTemplateId);
            string json = LootLockerJson.SerializeObject(new LootLockerTestSetItemTemplateAudiencesRequest { audiences = audiences });

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, json, onComplete, true);
        }

        public static void ListPlayerInventoryItemsAdmin(int playerId, Action<LootLockerTestAdminListPlayerInventoryItemsResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerTestAdminListPlayerInventoryItemsResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.adminListPlayerInventoryItems;
            string formattedEndpoint = string.Format(endpoint.endPoint, playerId);

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerTestAdminListPlayerInventoryItemsResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }

        /// <summary>
        /// Consume items through the admin API. The endpoint requires a body carrying the
        /// player id; <paramref name="count"/> of 0 lets the backend apply its default of 1.
        /// </summary>
        public static void ConsumePlayerInventoryItemAdmin(int playerId, string inventoryId, int count, Action<LootLockerTestAdminConsumeItemResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerTestAdminConsumeItemResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.adminConsumePlayerInventoryItem;
            string formattedEndpoint = string.Format(endpoint.endPoint, playerId, inventoryId);
            string json = LootLockerJson.SerializeObject(new LootLockerTestAdminConsumeItemRequest { player_id = playerId, count = count });

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerTestAdminConsumeItemResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }

        public static void DeletePlayerInventoryItemAdmin(int playerId, string inventoryId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.adminDeletePlayerInventoryItem;
            string formattedEndpoint = string.Format(endpoint.endPoint, playerId, inventoryId);

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, null, onComplete, true);
        }

        public static void SplitPlayerInventoryItemStackAdmin(int playerId, string inventoryId, int count, Action<LootLockerTestSplitItemStackResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerTestSplitItemStackResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.adminSplitPlayerInventoryItemStack;
            string formattedEndpoint = string.Format(endpoint.endPoint, playerId, inventoryId);
            string json = LootLockerJson.SerializeObject(new LootLockerTestSplitItemStackRequest { count = count });

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerTestSplitItemStackResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }

        public static void MergePlayerInventoryItemStacksAdmin(int playerId, string sourceInventoryId, string targetInventoryId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.adminMergePlayerInventoryItemStacks;
            string formattedEndpoint = string.Format(endpoint.endPoint, playerId);
            string json = LootLockerJson.SerializeObject(new LootLockerTestMergeItemStacksRequest
            {
                source_inventory_id = sourceInventoryId,
                target_inventory_id = targetInventoryId
            });

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, json, onComplete, true);
        }
    }

    public class LootLockerTestCreateItemTemplateRequest
    {
        public string name { get; set; }
        public string item_type { get; set; }
        public bool consumable { get; set; }
        public bool deletable { get; set; }
        public string[] audiences { get; set; }
    }

    public class LootLockerTestItemTemplateResponse : LootLockerResponse
    {
        public string id { get; set; }
    }

    public class LootLockerTestGrantItemRequest
    {
        public int player_id { get; set; }
        public string item_template_id { get; set; }
        public int count { get; set; }
    }

    public class LootLockerTestGrantItemResponse : LootLockerResponse
    {
        public string id { get; set; }
    }

    public class LootLockerTestSetItemTemplateAudiencesRequest
    {
        public string[] audiences { get; set; }
    }

    public class LootLockerTestAdminConsumeItemRequest
    {
        public int player_id { get; set; }
        public int count { get; set; }
    }

    public class LootLockerTestAdminConsumeItemResponse : LootLockerResponse
    {
        public int consumed { get; set; }
    }

    public class LootLockerTestSplitItemStackRequest
    {
        public int count { get; set; }
    }

    public class LootLockerTestSplitItemStackResponse : LootLockerResponse
    {
        public string id { get; set; }
    }

    public class LootLockerTestMergeItemStacksRequest
    {
        public string source_inventory_id { get; set; }
        public string target_inventory_id { get; set; }
    }

    public class LootLockerTestAdminListPlayerInventoryItemsResponse : LootLockerResponse
    {
        public LootLockerTestAdminPlayerItem[] items { get; set; }
    }

    /// <summary>
    /// An item as returned by the admin player inventory listing. Note that this shape differs
    /// from the game API item: the template name is exposed as <c>template_name</c> and there is
    /// no <c>deletable</c> field.
    /// </summary>
    public class LootLockerTestAdminPlayerItem
    {
        public string id { get; set; }
        public int player_id { get; set; }
        public string item_template_id { get; set; }
        public string template_name { get; set; }
        public string item_type { get; set; }
        public bool consumable { get; set; }
        public int count { get; set; }
        public string source { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
    }
}
