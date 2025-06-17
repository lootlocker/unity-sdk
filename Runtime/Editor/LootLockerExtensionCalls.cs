using System;
using UnityEngine;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER
using LootLocker.Extension.Requests;
using LootLocker.Extension.Responses;

namespace LootLocker.Extension
{    public partial class LootLockerAdminManager
    {
        private static LootLockerLogger.LogLevel? savedLogLevel = null;
        private static int activeAdminRequestCount = 0;
        
        public static void SendAdminRequest(string endPoint, LootLockerHTTPMethod httpMethod, string json, Action<LootLockerResponse> onComplete, bool useAuthToken)
        {
            // Only save the log level if this is the first admin request
            if (activeAdminRequestCount == 0)
            {
                savedLogLevel = LootLockerConfig.current.logLevel;
                LootLockerConfig.current.logLevel = LootLockerLogger.LogLevel.None;
            }
            activeAdminRequestCount++;

            try
            {
                LootLockerServerRequest.CallAPI(null, endPoint, httpMethod, json, onComplete: (serverResponse) => { 
                    // Always restore log level, regardless of success or failure
                    try 
                    {
                        LootLockerResponse.Deserialize(onComplete, serverResponse); 
                    } finally 
                    {
                        activeAdminRequestCount--;
                        // Only restore log level when all admin requests are complete
                        if (activeAdminRequestCount == 0 && savedLogLevel.HasValue)
                        {
                            LootLockerConfig.current.logLevel = savedLogLevel.Value;
                            savedLogLevel = null;
#if UNITY_EDITOR
                            UnityEditor.EditorUtility.SetDirty(LootLockerConfig.current);
#endif
                        }
                    }
                },
                    useAuthToken: useAuthToken,
                    callerRole: LootLockerEnums.LootLockerCallerRole.Admin);
            } catch
            {
                // If CallAPI itself throws an exception, restore the log level
                activeAdminRequestCount--;
                if (activeAdminRequestCount == 0 && savedLogLevel.HasValue)
                {
                    LootLockerConfig.current.logLevel = savedLogLevel.Value;
                    savedLogLevel = null;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(LootLockerConfig.current);
#endif
                }
                throw; // Re-throw the exception
            }
        }

        public static void AdminLogin(string email, string password, Action<LoginResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionLogin;

            AdminLoginRequest request = new AdminLoginRequest();
            request.email = email;
            request.password = password;

            string json = LootLockerJson.SerializeObject(request);

            SendAdminRequest(endPoint.endPoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }

        public static void GetGameDomainKey(int game_id, Action<GameResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionGetGameInformation;

            string getVariable = endPoint.WithPathParameter(game_id);

            SendAdminRequest(getVariable, endPoint.httpMethod, "",
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

        public static void GetAllKeys(int game_id, Action<KeysResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionGetAllKeys;
            string getVariable = endPoint.WithPathParameter(game_id);

            SendAdminRequest(getVariable, endPoint.httpMethod, "",
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }

        public static void GenerateKey(int game_id, string key_name, string key_environment, Action<KeyResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionCreateKey;

            string getVariable = endPoint.WithPathParameter(game_id);

            KeyCreationRequest data = new KeyCreationRequest();
            data.name = key_name;
            data.api_type = key_environment;

            string json = LootLockerJson.SerializeObject(data);

            SendAdminRequest(getVariable, endPoint.httpMethod, json,
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }

        public static void GetUserInformation(Action<LoginResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionUserInformation;

            SendAdminRequest(endPoint.endPoint, endPoint.httpMethod, "",
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }

        public static void GetUserRole(string userId, Action<UserRoleResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerAdminEndPoints.adminExtensionGetUserRole;

            string getVariable = endPoint.WithPathParameter(userId);

            SendAdminRequest(getVariable, endPoint.httpMethod, "",
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, true);
        }
    }
}
#endif