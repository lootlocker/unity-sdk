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

        public static void ListTriggers(LootLockerGetRequest data, Action<ListTriggersResponse> onComplete)
        {
            string json = "";

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.listTriggers;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new ListTriggersResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<ListTriggersResponse>(serverResponse.text);

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
            }, useAuthToken: true, callerRole: enums.CallerRole.Admin);

        }

        public static void CreateTriggers(CreateTriggersRequest requestData, LootLockerGetRequest data, Action<ListTriggersResponse> onComplete)
        {

            string json = "";
            if (requestData == null) return;
            else json = JsonConvert.SerializeObject(requestData);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.createTriggers;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new ListTriggersResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<ListTriggersResponse>(serverResponse.text);

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
            }, useAuthToken: true, callerRole: enums.CallerRole.Admin);

        }

        //public static void VerifyTwoFactorAuthenticationSetup(VerifyTwoFactorAuthenticationRequest data, Action<VerifyTwoFactorAuthenticationResponse> onComplete)
        //{
        //    string json = "";
        //    if (data == null) return;
        //    else json = JsonConvert.SerializeObject(data);

        //    EndPointClass endPoint = LootLockerEndPointsAdmin.current.verifyTwoFactorAuthenticationSetup;

        //    ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
        //    {
        //        var response = new VerifyTwoFactorAuthenticationResponse();
        //        if (string.IsNullOrEmpty(serverResponse.Error))
        //        {
        //            response = JsonConvert.DeserializeObject<VerifyTwoFactorAuthenticationResponse>(serverResponse.text);

        //            response.text = serverResponse.text;

        //            onComplete?.Invoke(response);
        //        }
        //        else
        //        {
        //            response.text = serverResponse.text;
        //            response.message = serverResponse.message;
        //            response.Error = serverResponse.Error;
        //            onComplete?.Invoke(response);
        //        }
        //    }, useAuthToken: true, callerRole: enums.CallerRole.Admin);
        //}
        //public static void RemoveTwoFactorAuthentication(VerifyTwoFactorAuthenticationRequest data, Action<RemoveTwoFactorAuthenticationResponse> onComplete)
        //{
        //    string json = "";
        //    if (data == null) return;
        //    else json = JsonConvert.SerializeObject(data);

        //    Debug.Log("Removing 2FA with json: " + json);

        //    EndPointClass endPoint = LootLockerEndPointsAdmin.current.removeTwoFactorAuthentication;

        //    ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
        //    {
        //        var response = new RemoveTwoFactorAuthenticationResponse();
        //        if (string.IsNullOrEmpty(serverResponse.Error))
        //        {
        //            response = JsonConvert.DeserializeObject<RemoveTwoFactorAuthenticationResponse>(serverResponse.text);

        //            response.text = serverResponse.text;

        //            onComplete?.Invoke(response);
        //        }
        //        else
        //        {
        //            response.text = serverResponse.text;
        //            response.message = serverResponse.message;
        //            response.Error = serverResponse.Error;
        //            onComplete?.Invoke(response);
        //        }
        //    }, useAuthToken: true, callerRole: enums.CallerRole.Admin);
        //}

    }

}