using Newtonsoft.Json;
using System;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    public class LootLockerVerifyRequest
    {
        public string key => LootLockerConfig.current.apiKey;
        public string platform => CurrentPlatform.GetString();
        public string token { get; set; }

        public LootLockerVerifyRequest(string token)
        {
            this.token = token;
        }
    }

    public class LootLockerVerifySteamRequest : LootLockerVerifyRequest
    {
        public new string platform => CurrentPlatform.GetPlatformRepresentation(Platforms.Steam).PlatformString;

        public LootLockerVerifySteamRequest(string token) : base(token)
        {
            this.token = token;
        }
    }

    public class LootLockerVerifyResponse : LootLockerResponse
    {
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void Verify(LootLockerVerifyRequest data, Action<LootLockerVerifyResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            json = JsonConvert.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            EndPointClass endPoint = LootLockerEndPoints.playerVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, false);
        }
    }
}