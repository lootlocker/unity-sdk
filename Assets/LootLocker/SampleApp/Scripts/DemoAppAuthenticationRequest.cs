using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerAdmin;
using Newtonsoft.Json;
using System;
using LootLockerRequests;
using LootLockerDemoApp;


namespace LootLockerAdmin
{
    public partial class DemoAppAdminRequests
    {
        public static void InitialAuthenticationRequest(InitialAuthRequest data, Action<AuthResponse> onComplete)
        {

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.initialAuthenticationRequest;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new AuthResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<AuthResponse>(serverResponse.text);

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
            }, useAuthToken: false, callerRole: LootLockerEnums.CallerRole.Admin);
        }

        public static void TwoFactorAuthVerification(TwoFactorAuthVerficationRequest data, Action<AuthResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.twoFactorAuthenticationCodeVerification;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = new AuthResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<AuthResponse>(serverResponse.text);
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
            }, useAuthToken: false, callerRole: LootLockerEnums.CallerRole.Admin);
        }

        public static void SubsequentRequests(Action<SubsequentRequestsResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPoints.current.subsequentRequests;

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
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);
        }

        public static void CreatingAGame(CreatingAGameRequest data, Action<CreatingAGameResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.creatingAGame;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                CreatingAGameResponse response = new CreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CreatingAGameResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);
        }

        //Both this and the previous call share the same response
        public static void GetDetailedInformationAboutAGame(LootLockerGetRequest lootLockerGetRequest, Action<CreatingAGameResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getDetailedInformationAboutAGame;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                CreatingAGameResponse response = new CreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CreatingAGameResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);

        }

        public static void ListTriggers(LootLockerGetRequest data, Action<ListTriggersResponse> onComplete)
        {
            string json = "";

            EndPointClass endPoint = LootLockerEndPoints.current.listTriggers;

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
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);

        }

        public static void CreateTriggers(CreateTriggersRequest requestData, LootLockerGetRequest data, Action<ListTriggersResponse> onComplete)
        {

            string json = "";
            if (requestData == null) return;
            else json = JsonConvert.SerializeObject(requestData);

            EndPointClass endPoint = LootLockerEndPoints.current.createTriggers;

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
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);

        }

    }

}