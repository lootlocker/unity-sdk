using System;
using LootLocker;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestCurrency
    {
        public static void EnableCurrencyGameWrites(string currencyId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.enableCurrencyGameWrites;
            string formattedEndpoint = string.Format(endpoint.endPoint, currencyId);

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, null, serverResponse =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }

        public static void CreateCurrency(string name, string code, Action<LootLockerTestCreateCurrencyResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var request = new LootLockerTestCreateCurrencyRequest
            {
                name = name,
                code = code,
                game_id = LootLockerAdminRequest.ActiveGameId,
                initial_denomination_name = "base"
            };

            var endpoint = LootLockerTestConfigurationEndpoints.createCurrency;
            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, serverResponse =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerTestCreateCurrencyResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }

        public static void CreateCurrencyDenomination(string currencyId, string name, int value, Action<LootLockerTestDenominationResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var request = new LootLockerTestCreateDenominationRequest { name = name, value = value };
            var endpoint = LootLockerTestConfigurationEndpoints.createCurrencyDenomination;
            string formattedEndpoint = string.Format(endpoint.endPoint, currencyId);
            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, json, serverResponse =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerTestDenominationResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }
    }

    public class LootLockerTestCreateCurrencyRequest
    {
        public string name { get; set; }
        public string code { get; set; }
        public int game_id { get; set; }
        public string initial_denomination_name { get; set; }
    }

    public class LootLockerTestCreateCurrencyResponse : LootLockerResponse
    {
        public string id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public bool game_api_writes_enabled { get; set; }
    }

    public class LootLockerTestCreateDenominationRequest
    {
        public string name { get; set; }
        public int value { get; set; }
    }

    public class LootLockerTestDenominationResponse : LootLockerResponse
    {
        public string id { get; set; }
        public string name { get; set; }
        public int value { get; set; }
        public string currency { get; set; }
        public string created_at { get; set; }
    }
}
