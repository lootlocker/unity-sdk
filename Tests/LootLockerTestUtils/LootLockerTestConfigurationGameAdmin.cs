using System;
using LootLocker;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestGameAdmin
    {
        public static void SetGameConfig(string configKey, string json, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.setGameConfig;
            string formattedEndpoint = string.Format(endpoint.endPoint, configKey);

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, json, serverResponse =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }

        public static void AdminCreditBalance(string walletId, string currencyId, string amount, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var request = new LootLockerTestAdminCreditRequest
            {
                amount = amount,
                wallet_id = walletId,
                currency_id = currencyId
            };

            var endpoint = LootLockerTestConfigurationEndpoints.adminCreditBalance;
            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, serverResponse =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }
    }

    public class LootLockerTestAdminCreditRequest
    {
        public string amount { get; set; }
        public string wallet_id { get; set; }
        public string currency_id { get; set; }
    }
}
