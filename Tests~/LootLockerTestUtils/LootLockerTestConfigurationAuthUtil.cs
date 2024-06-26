using LootLocker;
using System;
using System.Collections.Generic;

namespace LootLockerTestConfigurationUtils
{
    public class AuthUtil
    {
        public class LoginRequest
        {
            public string email { get; set; }
            public string password { get; set; }
        }

        public class LoginResponse : LootLockerResponse
        {

            //public string mfa_key { get; set; }
            public string auth_token { get; set; }
            //public User user { get; set; }
        }

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

        public class SignupRequest
        {
            public string organisation { get; set; }
            public string name { get; set; }
            public string email { get; set; }
            public string password { get; set; }
        }
        public class Game
        {
            public int id { get; set; }
            public bool is_demo { get; set; }
            public string name { get; set; }
            public string badge_url { get; set; }
            public string logo_url { get; set; }
            public DevelopmentGame development { get; set; }
            public string organisation_name { get; set; }
        }

        public class DevelopmentGame
        {
            public int id { get; set; }

        }

        public class Organisation
        {
            public int id { get; set; }
            public string name { get; set; }
            public Game[] games { get; set; }
        }

        public class User
        {
            public int id { get; set; }
            public string name { get; set; }
            public long signed_up { get; set; }
            public Organisation[] organisations { get; set; }
        }

        public class SignupResponse : LootLockerResponse
        {
            public string auth_token { get; set; }
            public User user { get; set; }
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
}
