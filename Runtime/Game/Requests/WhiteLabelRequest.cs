using System;
using System.Collections.Generic;
using LootLocker.Requests;

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

    [Serializable]
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
                errorData = sessionResponse?.errorData ?? loginResponse?.errorData,
                EventId = sessionResponse?.EventId ?? loginResponse?.EventId,
                text = sessionResponse?.text ?? loginResponse?.text,
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
            if(input == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerWhiteLabelLoginResponse>(null));
            	return;
            }

            if (LootLockerConfig.current.domainKey.Length == 0)
            {
                LootLockerLogger.Log("Domain key must be set in settings", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerWhiteLabelLoginResponse>("Domain key must be set in settings", null));

                return;
            }

            string json = LootLockerJson.SerializeObject(input);
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelLogin;
            LootLockerServerRequest.CallAPI(null, endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerResponse.Deserialize(onComplete, serverResponse);
            }, useAuthToken: false, callerRole: endPoint.callerRole, additionalHeaders: GetDomainHeaders());
        }

        public static void WhiteLabelVerifySession(LootLockerWhiteLabelVerifySessionRequest input, Action<LootLockerWhiteLabelVerifySessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelVerifySession;
            
            if(input == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerWhiteLabelVerifySessionResponse>(null));
            	return;
            }

            if (LootLockerConfig.current.domainKey.Length == 0)
            {
                LootLockerLogger.Log("Domain key must be set in settings", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerWhiteLabelVerifySessionResponse>("Domain key must be set in settings", null));

                return;
            }

            string json = LootLockerJson.SerializeObject(input);

            LootLockerServerRequest.CallAPI(null, endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false, callerRole: endPoint.callerRole, additionalHeaders: GetDomainHeaders());
        }

        public static void WhiteLabelSignUp(LootLockerWhiteLabelUserRequest input, Action<LootLockerWhiteLabelSignupResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelSignUp;

            if (input == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerWhiteLabelSignupResponse>(null));
                return;
            }

            if (LootLockerConfig.current.domainKey.Length == 0)
            {
                LootLockerLogger.Log("Domain key must be set in settings", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerWhiteLabelSignupResponse>("Domain key must be set in settings", null));

                return;
            }

            string json = LootLockerJson.SerializeObject(input);

            LootLockerServerRequest.CallAPI(null, endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false, callerRole: endPoint.callerRole, additionalHeaders: GetDomainHeaders());
        }

        public static void WhiteLabelRequestPasswordReset(string email, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestPasswordReset;

            if (LootLockerConfig.current.domainKey.Length == 0)
            {
                LootLockerLogger.Log("Domain key must be set in settings", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>("Domain key must be set in settings", null));

                return;
            }

            var json = LootLockerJson.SerializeObject(new { email });
            LootLockerServerRequest.CallAPI(null, endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false, callerRole: endPoint.callerRole, additionalHeaders: GetDomainHeaders());
        }

        public static void WhiteLabelRequestAccountVerification(int userID, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestAccountVerification;

            if (LootLockerConfig.current.domainKey.Length == 0)
            {
                LootLockerLogger.Log("Domain key must be set in settings", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>("Domain key must be set in settings", null));

                return;
            }

            var json = LootLockerJson.SerializeObject(new { user_id = userID });
            LootLockerServerRequest.CallAPI(null, endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false, callerRole: endPoint.callerRole, additionalHeaders: GetDomainHeaders());
        }

        public static void WhiteLabelRequestAccountVerification(string email, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestAccountVerification;

            if (LootLockerConfig.current.domainKey.Length == 0)
            {
                LootLockerLogger.Log("Domain key must be set in settings", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>("Domain key must be set in settings", null));

                return;
            }

            var json = LootLockerJson.SerializeObject(new { email = email });
            LootLockerServerRequest.CallAPI(null, endPoint.endPoint, endPoint.httpMethod, json, onComplete, useAuthToken: false, callerRole: endPoint.callerRole, additionalHeaders: GetDomainHeaders());
        }

        public static Dictionary<string, string> GetDomainHeaders()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("domain-key", LootLockerConfig.current.domainKey);

            if (LootLockerConfig.current.apiKey.StartsWith("dev_"))
            {
                headers.Add("is-development", "true");
            }
            return headers;
        }
    }
}
