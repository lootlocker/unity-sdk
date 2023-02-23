using Newtonsoft.Json;
using System;
using LootLocker.Requests;
using Newtonsoft.Json.Serialization;

namespace LootLocker.Requests
{
    public class LootLockerWhiteLabelUserRequest
    {
        public string email { get; set; }
        public string password { get; set; }
        public bool remember { get; set; }
    }

    public class LootLockerWhiteLabelVerifySessionRequest
    {
        public string email { get; set; }
        public string token { get; set; }
    }

    public class LootLockerWhiteLabelVerifySessionResponse : LootLockerResponse
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }

    public class LootLockerWhiteLabelSignupResponse : LootLockerResponse
    {
        public int ID { get; set; }

        [Obsolete("GameID is deprecated and will be removed soon")]
        public int GameID { get; set; }
        public string Email { get; set; }
        public string CreatedAt { get; set; }

        public string UpdatedAt { get; set; }

        public string DeletedAt { get; set; }

        public string VerifiedAt { get; set; }
    }

    public class LootLockerWhiteLabelLoginResponse : LootLockerWhiteLabelSignupResponse
    {
        public string SessionToken { get; set; }
    }

    [System.Serializable]
    public class LootLockerWhiteLabelLoginAndStartSessionResponse : LootLockerResponse
    {
        public LootLockerWhiteLabelLoginResponse LoginResponse { get; set; }
        public LootLockerSessionResponse SessionResponse { get; set; }

        public static LootLockerWhiteLabelLoginAndStartSessionResponse MakeWhiteLabelLoginAndStartSessionResponse(
            LootLockerWhiteLabelLoginResponse loginResponse, LootLockerSessionResponse sessionResponse)
        {
            if (loginResponse == null && sessionResponse == null)
            {
                return new LootLockerWhiteLabelLoginAndStartSessionResponse();
            }

            return new LootLockerWhiteLabelLoginAndStartSessionResponse
            {
                statusCode = sessionResponse?.statusCode ?? loginResponse.statusCode,
                success = sessionResponse?.success ?? loginResponse.success,
                Error = sessionResponse?.Error ?? loginResponse?.Error,
                EventId = sessionResponse?.EventId ?? loginResponse?.EventId,
                hasError = sessionResponse?.hasError ?? loginResponse.hasError,
                text = sessionResponse?.text ?? loginResponse?.text,
                texture = sessionResponse?.texture ?? loginResponse?.texture,
                LoginResponse = loginResponse,
                SessionResponse = sessionResponse
            };
        }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {

        public static void WhiteLabelLogin(LootLockerWhiteLabelUserRequest input, Action<LootLockerWhiteLabelLoginResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelLogin;

            string json = "";
            if (input == null)
            {
                return;
            }
            else
            {
                json = JsonConvert.SerializeObject(input);
            }

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, ((serverResponse) =>
            {
                LootLockerWhiteLabelLoginResponse response = new LootLockerWhiteLabelLoginResponse();
                if (string.IsNullOrEmpty(serverResponse.Error) && serverResponse.text != null)
                {
                    DefaultContractResolver contractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };

                    response = JsonConvert.DeserializeObject<LootLockerWhiteLabelLoginResponse>(serverResponse.text, new JsonSerializerSettings
                    {
                        ContractResolver = contractResolver,
                        Formatting = Formatting.Indented
                    });

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse, LootLockerJsonSettings.Indented); });
        }

        public static void WhiteLabelVerifySession(LootLockerWhiteLabelVerifySessionRequest input, Action<LootLockerWhiteLabelVerifySessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelVerifySession;

            string json = "";
            if (input == null)
            {
                return;
            }
            else
            {
                json = JsonConvert.SerializeObject(input);
            }

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, ((serverResponse) =>
            {
                LootLockerWhiteLabelVerifySessionResponse response = new LootLockerWhiteLabelVerifySessionResponse();
                if (string.IsNullOrEmpty(serverResponse.Error) && serverResponse.text != null)
                {
                    DefaultContractResolver contractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse, LootLockerJsonSettings.Indented); });
        }

        public static void WhiteLabelSignUp(LootLockerWhiteLabelUserRequest input, Action<LootLockerWhiteLabelSignupResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelSignUp;

            string json = "";
            if (input == null) {
                return;
            }
            else
            { 
                json = JsonConvert.SerializeObject(input);
            }

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, ((serverResponse) =>
            {
                LootLockerWhiteLabelSignupResponse response = new LootLockerWhiteLabelSignupResponse();
                if (string.IsNullOrEmpty(serverResponse.Error) && serverResponse.text != null)
                {
                    DefaultContractResolver contractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };

                    response = JsonConvert.DeserializeObject<LootLockerWhiteLabelSignupResponse>(serverResponse.text, new JsonSerializerSettings
                    {
                        ContractResolver = contractResolver,
                        Formatting = Formatting.Indented
                    });

                    if (response == null)
                    {
                        response = LootLockerResponseFactory.Error<LootLockerWhiteLabelSignupResponse>("error deserializing server response");
                        onComplete?.Invoke(response);
                        return;
                    }
                }

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse, LootLockerJsonSettings.Indented); });
        }

        public static void WhiteLabelRequestPasswordReset(string email, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestPasswordReset;

            var json = JsonConvert.SerializeObject(new { email });
            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void WhiteLabelRequestAccountVerification(int userID, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestAccountVerification;

            var json = JsonConvert.SerializeObject(new { user_id = userID });
            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}
