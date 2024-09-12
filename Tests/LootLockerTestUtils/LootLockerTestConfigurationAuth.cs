using LootLocker;
using System;

namespace LootLockerTestConfigurationUtils
{
    public class Auth
    {
        public static void Login(string email, string password, Action<LoginResponse> onComplete)
        {
            if (!string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Already signed in
                onComplete?.Invoke(new LoginResponse { auth_token = LootLockerConfig.current.adminToken });
                return;
            }

            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.LoginEndpoint;

            LoginRequest request = new LoginRequest();
            request.email = email;
            request.password = password;

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(endPoint.endPoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) =>
                {
                    var loginResponse = LootLockerResponse.Deserialize<LoginResponse>(serverResponse);
                    if (loginResponse != null && loginResponse.success)
                    {
                        LootLockerConfig.current.adminToken = loginResponse.auth_token;
                    }
                    onComplete?.Invoke(loginResponse);
                }, true);
        }

        public static void Signup(string email, string password, string name, string organization, Action<SignupResponse> onComplete)
        {
            if (!string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Already signed in
                onComplete?.Invoke(new SignupResponse{ auth_token = LootLockerConfig.current.adminToken });
                return;
            }

            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.SignupEndpoint;

            SignupRequest request = new SignupRequest
            {
                email = email,
                password = password,
                organisation = organization,
                name = name
            };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(endPoint.endPoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) =>
                {
                    var loginResponse = LootLockerResponse.Deserialize<SignupResponse>(serverResponse);
                    if (loginResponse != null && loginResponse.success)
                    {
                        LootLockerConfig.current.adminToken = loginResponse.auth_token;
                    }
                    onComplete?.Invoke(loginResponse);
                }, true);
        }
    }
    public class LoginRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class LoginResponse : LootLockerResponse
    {

        //public string mfa_key { get; set; }
        public string auth_token { get; set; }
        public LootLockerTestUser user { get; set; }
    }

    public class SignupRequest
    {
        public string organisation { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string password { get; set; }
    }

    public class SignupResponse : LootLockerResponse
    {
        public string auth_token { get; set; }
        public LootLockerTestUser user { get; set; }
    }
}
