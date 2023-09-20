using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using LLlibs.ZeroDepJson;
using LootLocker.Extension.Requests;
using LootLocker.Requests;
using UnityEditor.VersionControl;
using UnityEngine;

namespace LootLocker.Admin
{
    public partial class LootLockerAdminManager
    {
                
        public static void AdminLogin(string email, string password, Action<LoginResponse> onComplete)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                Debug.Log("Input is invalid!");
                return;
            }

            AdminLoginRequest request = new AdminLoginRequest();
            request.email = email;
            request.password = password;

            AdminLogin(request, onComplete);
        }

        public static void AdminLogin(AdminLoginRequest data, Action<LoginResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.adminExtensionLogin;

            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, false,
                callerRole: LootLockerEnums.LootLockerCallerRole.Admin);
        }


        public static void MFAAuthenticate(string authCode, string secret, Action<LoginResponse> onComplete)
        {
            if (string.IsNullOrEmpty(authCode) || string.IsNullOrEmpty(secret))
            {
                Debug.Log("Input is invalid!");
                return;
            }

            MfaAdminLoginRequest data = new MfaAdminLoginRequest();
            data.mfa_key = authCode;
            data.secret = secret;

            MFAAuthenticate(data, onComplete);
        }

        public static void MFAAuthenticate(MfaAdminLoginRequest data, Action<LoginResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.adminExtensionMFA;

            string json = LootLockerJson.SerializeObject(data);
            
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, 
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse);},
                callerRole: LootLockerEnums.LootLockerCallerRole.Admin);

        }


        public static void GetAllKeys(string game_id, Action<KeysResponse> onComplete)
        {
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(game_id);

            GetAllKeys(data, onComplete);
        }

        public static void GetAllKeys(LootLockerGetRequest lootLockerGetRequest, Action<KeysResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.adminExtensionGetAllKeys;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", 
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse);},
                callerRole: LootLockerEnums.LootLockerCallerRole.Admin);


        }

        public static void GenerateKey(string game_id, string key_name, string key_environment, Action<Key> onComplete)
        {
            LootLockerGetRequest request = new LootLockerGetRequest();
            request.getRequests.Add(game_id);

            KeyCreationRequest data = new KeyCreationRequest();
            data.name = key_name;
            data.api_type = key_environment;


            GenerateKey(data, request, onComplete);
        }

        public static void GenerateKey(KeyCreationRequest data, LootLockerGetRequest lootLockerGetRequest, Action<Key> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.adminExtensionCreateKey;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json,
                onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); },
                callerRole: LootLockerEnums.LootLockerCallerRole.Admin);
        }


        //Roles
        public static void GetUserRole(string userId, Action<UserRoleResponse> onComplete)
        {
            LootLockerGetRequest data = new LootLockerGetRequest();
            data.getRequests.Add(userId);

            GetUserRole(data, onComplete);
        }

        public static void GetUserRole(LootLockerGetRequest lootLockerGetRequest, Action<UserRoleResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.adminExtensionGetUserRole;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "",
            onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); },
            callerRole: LootLockerEnums.LootLockerCallerRole.Admin);

        }

    }

}