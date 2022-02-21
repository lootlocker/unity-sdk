using Newtonsoft.Json;
using System;
using LootLocker.Requests;
using Newtonsoft.Json.Serialization;

namespace LootLocker.Requests
{
    public class LootLockerPingResponse : LootLockerResponse
    {
        public string date { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void Ping(Action<LootLockerPingResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.ping;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, ((serverResponse) =>
            {
                LootLockerPingResponse response = new LootLockerPingResponse();
                if (string.IsNullOrEmpty(serverResponse.Error) && serverResponse.text != null)
                {
                    DefaultContractResolver contractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };

                    response = JsonConvert.DeserializeObject<LootLockerPingResponse>(serverResponse.text, new JsonSerializerSettings
                    {
                        ContractResolver = contractResolver,
                        Formatting = Formatting.Indented
                    });

                    if (response == null)
                    {
                        response = LootLockerResponseFactory.Error<LootLockerPingResponse>("error deserializing server response");
                        onComplete?.Invoke(response);
                        return;
                    }
                }

                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }));
        }
    }
}
