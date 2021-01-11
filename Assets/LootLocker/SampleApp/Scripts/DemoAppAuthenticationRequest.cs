using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Admin;
using Newtonsoft.Json;
using System;
using LootLocker.Requests;
using LootLockerDemoApp;


namespace LootLocker.Admin
{
    public partial class DemoAppAdminRequests
    {
        public static void InitialAuthenticationRequest(LootLockerInitialAuthRequest data, Action<LootLockerAuthResponse> onComplete)
        {

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.initialAuthenticationRequest;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerAuthResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerAuthResponse>(serverResponse.text);

                    if (response.mfa_key == null)
                    {
                        LootLockerConfig.current.UpdateToken(response.auth_token);
                    }

                    response.text = serverResponse.text;

                    LootLockerConfig.current.email = data.email;

                    LootLockerConfig.current.password = data.password;

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

            EndPointClass endPoint = LootLockerEndPoints.current.twoFactorAuthenticationCodeVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerAuthResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerAuthResponse>(serverResponse.text);
                    response.text = serverResponse.text;

                    LootLockerConfig.current.UpdateToken(response.auth_token);

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

            EndPointClass endPoint = LootLockerEndPoints.current.subsequentRequests;

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

        public static void CreatingAGame(LootLockerCreatingAGameRequest data, Action<LootLockerCreatingAGameResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.creatingAGame;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerCreatingAGameResponse response = new LootLockerCreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerCreatingAGameResponse>(serverResponse.text);
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

        //Both this and the previous call share the same response
        public static void GetDetailedInformationAboutAGame(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerCreatingAGameResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getDetailedInformationAboutAGame;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                LootLockerCreatingAGameResponse response = new LootLockerCreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerCreatingAGameResponse>(serverResponse.text);
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

        public static void ListTriggers(LootLockerGetRequest data, Action<LootLockerListTriggersResponse> onComplete)
        {
            string json = "";

            EndPointClass endPoint = LootLockerEndPoints.current.listTriggers;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerListTriggersResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerListTriggersResponse>(serverResponse.text);

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

        public static void CreateTriggers(LootLockerCreateTriggersRequest requestData, LootLockerGetRequest data, Action<LootLockerListTriggersResponse> onComplete)
        {

            string json = "";
            if (requestData == null) return;
            else json = JsonConvert.SerializeObject(requestData);

            EndPointClass endPoint = LootLockerEndPoints.current.createTriggers;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerListTriggersResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerListTriggersResponse>(serverResponse.text);

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