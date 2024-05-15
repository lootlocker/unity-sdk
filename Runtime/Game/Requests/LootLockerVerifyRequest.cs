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

    public class LootLockerVerifySteamWithAppIdRequest : LootLockerVerifySteamRequest
    {
        public int active_steam_app_id { get; set; }

        public LootLockerVerifySteamWithAppIdRequest(string token, int appId) : base(token)
        {
            this.token = token;
            this.active_steam_app_id = appId;
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
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerVerifyResponse>());
            	return;
            }
            string json = LootLockerJson.SerializeObject(data);
            EndPointClass endPoint = LootLockerEndPoints.playerVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, false);
        }
        public static void VerifyWithAppId(string token, int appId, Action<LootLockerVerifyResponse> onComplete)
        {
            if (string.IsNullOrEmpty(token))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerVerifyResponse>());
                return;
            }
            EndPointClass endPoint = LootLockerEndPoints.playerVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(new LootLockerVerifySteamWithAppIdRequest(token, appId)), (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, false);
        }
    }
}