using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Admin;
using LootLocker.Admin.Requests;
using Newtonsoft.Json;
using System;

namespace LootLocker.Admin.Requests
{
    

}

namespace LootLocker.Admin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void InitialAuthenticationRequest(LootLockerInitialAuthRequest data, Action<LootLockerAuthResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.initialAuthenticationRequest;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerAuthResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerAuthResponse>(serverResponse.text);

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
            }, useAuthToken: false, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }

        public static void TwoFactorAuthVerification(LootLockerTwoFactorAuthVerficationRequest data, Action<LootLockerAuthResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.twoFactorAuthenticationCodeVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerAuthResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerAuthResponse>(serverResponse.text);
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
            }, useAuthToken: false, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }

        public static void SubsequentRequests(Action<LootLockerSubsequentRequestsResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.subsequentRequests;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerSubsequentRequestsResponse response = new LootLockerSubsequentRequestsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerSubsequentRequestsResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }

    }

}
