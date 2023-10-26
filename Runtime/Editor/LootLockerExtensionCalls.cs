using System;
using UnityEngine;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && LOOTLOCKER_ENABLE_EXTENSION
using LootLocker.Extension.Requests;
using LootLocker.Extension.Responses;

namespace LootLocker.Extension
{
    public partial class LootLockerAdminManager
    {

        public static void SendAdminRequest(string endPoint, LootLockerHTTPMethod httpMethod, string json, Action<LootLockerResponse> onComplete, bool useAuthToken)
        {
            LootLockerServerRequest.CallAPI(endPoint, httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); },
                useAuthToken,
                callerRole: LootLockerEnums.LootLockerCallerRole.Admin);
        }

        public static void AdminLogin(string email, string password, Action<LoginResponse> onComplete)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)("Email or Password is empty.");
                return;
            }

            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionLogin;

            AdminLoginRequest request = new AdminLoginRequest();
            request.email = email;
            request.password = password;

            string json = LootLockerJson.SerializeObject(request);

            SendAdminRequest(endPoint.endPoint, endPoint.httpMethod, json,
               onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }

        public static void MFAAuthenticate(string authCode, string secret, Action<LoginResponse> onComplete)
        {
            if (string.IsNullOrEmpty(authCode) || string.IsNullOrEmpty(secret))
            {
                Debug.Log("No authentication code found!");
                return;
            }

            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionMFA;

            MfaAdminLoginRequest data = new MfaAdminLoginRequest();
            data.mfa_key = authCode;
            data.secret = secret;

            string json = LootLockerJson.SerializeObject(data);

            SendAdminRequest(endPoint.endPoint, endPoint.httpMethod, json,
            onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }

        public static void GetAllKeys(string game_id, Action<KeysResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionGetAllKeys;
            string getVariable = string.Format(endPoint.endPoint, game_id);

            SendAdminRequest(getVariable, endPoint.httpMethod, "",
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }

        public static void GenerateKey(string game_id, string key_name, string key_environment, Action<KeyResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionCreateKey;

            string getVariable = string.Format(endPoint.endPoint, game_id);

            KeyCreationRequest data = new KeyCreationRequest();
            data.name = key_name;
            data.api_type = key_environment;

            string json = LootLockerJson.SerializeObject(data);

            SendAdminRequest(getVariable, endPoint.httpMethod, json,
            onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }

        public static void GetUserRole(string userId, Action<UserRoleResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionGetUserRole;

            string getVariable = string.Format(endPoint.endPoint, userId);

            SendAdminRequest(getVariable, endPoint.httpMethod, "",
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }
    }
}
#endif