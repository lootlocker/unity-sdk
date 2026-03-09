using System;
using LootLocker;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestCatalog
    {
        public static void ToggleCatalogItemPurchasable(string catalogItemId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }
            var endpoint = LootLockerTestConfigurationEndpoints.toggleCatalogItemPurchasable;
            string formattedEndpoint = string.Format(endpoint.endPoint, catalogItemId);
            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, null, serverResponse =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }

        public static void CreateCatalog(string name, string key, Action<LootLockerTestCreateCatalogResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }
            var request = new LootLockerTestCreateCatalogRequest
            {
                name = name,
                key = key,
                game_id = LootLockerAdminRequest.ActiveGameId
            };
            var endpoint = LootLockerTestConfigurationEndpoints.createCatalog;
            string json = LootLockerJson.SerializeObject(request);
            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, serverResponse =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerTestCreateCatalogResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }

        public static void CreateCatalogItem(string catalogId, string entityId, string entityKind, Action<LootLockerTestCatalogItemResponse> onComplete, string amount = null)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }
            string metadataKey = entityKind switch
            {
                "currency" => "purchased_amount",
                "progression_points" => "progression_points_amount",
                _ => null
            };
            var request = new LootLockerTestCreateCatalogItemRequest
            {
                game_id = LootLockerAdminRequest.ActiveGameId,
                catalog_id = catalogId,
                entity_id = entityId,
                entity_kind = entityKind,
                metadata = (!string.IsNullOrEmpty(amount) && metadataKey != null)
                    ? new[] { new LootLockerTestCatalogItemMetadata { key = metadataKey, value = amount } }
                    : null
            };
            var endpoint = LootLockerTestConfigurationEndpoints.createCatalogItem;
            string json = LootLockerJson.SerializeObject(request);
            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, serverResponse =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerTestCatalogItemResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }

        public static void AddPriceToCatalogItem(string catalogItemId, string currencyId, string amount, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }
            var request = new LootLockerTestAddPriceRequest
            {
                amount = amount,
                game_id = LootLockerAdminRequest.ActiveGameId,
                currency_id = currencyId,
                catalog_item_id = catalogItemId
            };
            var endpoint = LootLockerTestConfigurationEndpoints.addPriceToCatalogItem;
            string json = LootLockerJson.SerializeObject(request);
            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, serverResponse =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }

        public static void PublishCatalog(string catalogId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }
            var endpoint = LootLockerTestConfigurationEndpoints.publishCatalog;
            string formattedEndpoint = string.Format(endpoint.endPoint, catalogId);
            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, null, serverResponse =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }
    }

    public class LootLockerTestCreateCatalogRequest
    {
        public string name { get; set; }
        public string key { get; set; }
        public int game_id { get; set; }
    }

    public class LootLockerTestCreateCatalogResponse : LootLockerResponse
    {
        public string id { get; set; }
        public string name { get; set; }
        public string key { get; set; }
    }

    public class LootLockerTestCatalogItemMetadata
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class LootLockerTestCreateCatalogItemRequest
    {
        public int game_id { get; set; }
        public string catalog_id { get; set; }
        public string entity_id { get; set; }
        public string entity_kind { get; set; }
        public LootLockerTestCatalogItemMetadata[] metadata { get; set; }
    }

    public class LootLockerTestCatalogItemResponse : LootLockerResponse
    {
        public string catalog_item_id { get; set; }
    }

    public class LootLockerTestAddPriceRequest
    {
        public string amount { get; set; }
        public int game_id { get; set; }
        public string currency_id { get; set; }
        public string catalog_item_id { get; set; }
    }
}
