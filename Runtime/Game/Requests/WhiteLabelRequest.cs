using System;
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
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelLogin;
            
            if(input == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerWhiteLabelLoginResponse>());
            	return;
            }
            
            string json = LootLockerJson.SerializeObject(input);

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void WhiteLabelVerifySession(LootLockerWhiteLabelVerifySessionRequest input, Action<LootLockerWhiteLabelVerifySessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelVerifySession;
            
            if(input == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerWhiteLabelVerifySessionResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(input);

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void WhiteLabelSignUp(LootLockerWhiteLabelUserRequest input, Action<LootLockerWhiteLabelSignupResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelSignUp;

            if (input == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerWhiteLabelSignupResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(input);

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void WhiteLabelRequestPasswordReset(string email, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestPasswordReset;

            var json = LootLockerJson.SerializeObject(new { email });
            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void WhiteLabelRequestAccountVerification(int userID, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestAccountVerification;

            var json = LootLockerJson.SerializeObject(new { user_id = userID });
            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void WhiteLabelRequestAccountVerification(string email, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestAccountVerification;

            var json = LootLockerJson.SerializeObject(new { email = email });
            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, onComplete);
        }
    }
}
