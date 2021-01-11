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

    public class LootLockerVerifyTwoFactorAuthenticationRequest
    {

        public int secret;

    }

    public class LootLockerSetupTwoFactorAuthenticationResponse : LootLockerResponse
    {

        public bool success { get; set; }
        public string mfa_token_url { get; set; }

    }
    public class LootLockerVerifyTwoFactorAuthenticationResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string recover_token { get; set; }
    }

    public class LootLockerRemoveTwoFactorAuthenticationResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string error { get; set; }
    }


}

namespace LootLocker.Admin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void SetupTwoFactorAuthentication(Action<LootLockerSetupTwoFactorAuthenticationResponse> onComplete)
        {
            string json = "";

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.setupTwoFactorAuthentication;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerSetupTwoFactorAuthenticationResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerSetupTwoFactorAuthenticationResponse>(serverResponse.text);

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
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }
        public static void VerifyTwoFactorAuthenticationSetup(LootLockerVerifyTwoFactorAuthenticationRequest data, Action<LootLockerVerifyTwoFactorAuthenticationResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.verifyTwoFactorAuthenticationSetup;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerVerifyTwoFactorAuthenticationResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerVerifyTwoFactorAuthenticationResponse>(serverResponse.text);

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
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }
        public static void RemoveTwoFactorAuthentication(LootLockerVerifyTwoFactorAuthenticationRequest data, Action<LootLockerRemoveTwoFactorAuthenticationResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            Debug.Log("Removing 2FA with json: " + json);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.removeTwoFactorAuthentication;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerRemoveTwoFactorAuthenticationResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerRemoveTwoFactorAuthenticationResponse>(serverResponse.text);

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
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }

    }

}