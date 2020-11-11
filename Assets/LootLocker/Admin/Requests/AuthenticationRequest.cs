using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerAdmin;
using LootLockerAdminRequests;
using Newtonsoft.Json;
using System;

namespace LootLockerAdminRequests
{
    

}

namespace LootLockerAdmin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void InitialAuthenticationRequest(InitialAuthRequest data, Action<AuthResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.initialAuthenticationRequest;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new AuthResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<AuthResponse>(serverResponse.text);

                    if (response.mfa_key == null)
                    {
                        LootLockerAdminConfig.current.UpdateToken(response.auth_token, "");
                    }

                    response.text = serverResponse.text;

                    LootLockerAdminConfig.current.email = data.email;

                    LootLockerAdminConfig.current.password = data.password;

                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: false, callerRole: enums.CallerRole.Admin);
        }

        public static void TwoFactorAuthVerification(TwoFactorAuthVerficationRequest data, Action<AuthResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.twoFactorAuthenticationCodeVerification;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new AuthResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<AuthResponse>(serverResponse.text);
                    response.text = serverResponse.text;

                    LootLockerAdminConfig.current.UpdateToken(response.auth_token, "");

                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: false, callerRole: enums.CallerRole.Admin);
        }

        public static void SubsequentRequests(Action<SubsequentRequestsResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.subsequentRequests;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) =>
            {
                SubsequentRequestsResponse response = new SubsequentRequestsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<SubsequentRequestsResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: enums.CallerRole.Admin);
        }

    }

}