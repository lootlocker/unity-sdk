using LootLocker;
using System;
using System.Collections.Generic;

namespace LootLockerTestConfigurationUtils
{
    public class ApiKey
    {
        public class CreateApiKeyRequest
        {
            public string name { get; set; }
            public string api_type { get; set; } = "game";
        }

        public class CreateApiKeyResponse : LootLockerResponse
        {
            public string api_key { get; set; }
        }

        public static void CreateKey(string keyName, Action<CreateApiKeyResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Not signed in
                onComplete?.Invoke(new CreateApiKeyResponse { success = false, errorData = new LootLockerErrorData{message = "Not logged in"}});
                return;
            }

            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.CreateKey;

            CreateApiKeyRequest request = new CreateApiKeyRequest { name = keyName };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(endPoint.endPoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) => LootLockerResponse.Deserialize(onComplete, serverResponse), true);
        }
    }
}
