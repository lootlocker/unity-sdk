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

    public class VerifyTwoFactorAuthenticationRequest
    {

        public int secret;

    }

    public class SetupTwoFactorAuthenticationResponse : LootLockerResponse
    {

        public bool success { get; set; }
        public string mfa_token_url { get; set; }

    }
    public class VerifyTwoFactorAuthenticationResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string recover_token { get; set; }
    }

    public class RemoveTwoFactorAuthenticationResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string error { get; set; }
    }


}

namespace LootLockerAdmin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void SetupTwoFactorAuthentication(Action<SetupTwoFactorAuthenticationResponse> onComplete)
        {
            string json = "";

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.setupTwoFactorAuthentication;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new SetupTwoFactorAuthenticationResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<SetupTwoFactorAuthenticationResponse>(serverResponse.text);

                    response.text = serverResponse.text;

                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);
        }
        public static void VerifyTwoFactorAuthenticationSetup(VerifyTwoFactorAuthenticationRequest data, Action<VerifyTwoFactorAuthenticationResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.verifyTwoFactorAuthenticationSetup;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new VerifyTwoFactorAuthenticationResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<VerifyTwoFactorAuthenticationResponse>(serverResponse.text);

                    response.text = serverResponse.text;

                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);
        }
        public static void RemoveTwoFactorAuthentication(VerifyTwoFactorAuthenticationRequest data, Action<RemoveTwoFactorAuthenticationResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            Debug.Log("Removing 2FA with json: " + json);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.removeTwoFactorAuthentication;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new RemoveTwoFactorAuthenticationResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<RemoveTwoFactorAuthenticationResponse>(serverResponse.text);

                    response.text = serverResponse.text;

                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);
        }

    }

}