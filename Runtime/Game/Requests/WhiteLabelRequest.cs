using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using LootLocker.LootLockerEnums;
using Newtonsoft.Json.Serialization;

namespace LootLocker.Requests
{
    public class LootLockerWhiteLabelUserRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class LootLockerWhiteLabelSignupResponse : LootLockerResponse
    {
        public int ID { get; set; }
        public int GameID { get; set; }
        public string Email { get; set; }
        public string CreatedAt { get; set; }
        #nullable enable
        public string? UpdatedAt { get; set; }
        #nullable enable
        public string? DeletedAt { get; set; }
        #nullable enable
        public string? ValidatedAt { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void WhiteLabelSignUp(LootLockerWhiteLabelUserRequest input, Action<LootLockerWhiteLabelSignupResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelSignUp;

            string json = "";
            if (input == null) {
                return;
            }
            else
            { 
                json = JsonConvert.SerializeObject(input);
            }

            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, ((serverResponse) =>
            {
                LootLockerWhiteLabelSignupResponse? response = new LootLockerWhiteLabelSignupResponse();
                if (string.IsNullOrEmpty(serverResponse.Error) && serverResponse.text != null)
                {
                    DefaultContractResolver contractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    };

                    response = JsonConvert.DeserializeObject<LootLockerWhiteLabelSignupResponse>(serverResponse.text, new JsonSerializerSettings
                    {
                        ContractResolver = contractResolver,
                        Formatting = Formatting.Indented
                    });

                    if (response == null)
                    {
                        response = LootLockerResponseFactory.Error<LootLockerWhiteLabelSignupResponse>("error deserializing server response");
                        onComplete?.Invoke(response);
                        return;
                    }
                }

                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }));
        }

        public static void WhiteLabelRequestPasswordReset(string email, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestPasswordReset;

            var json = JsonConvert.SerializeObject(new { email });
            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerResponse? response = new LootLockerResponse
                {
                    text = serverResponse.text,
                    success = serverResponse.success,
                    Error = serverResponse.Error,
                    statusCode = serverResponse.statusCode
                };

                onComplete?.Invoke(response);
            });
        }

        public static void WhiteLabelRequestAccountVerification(int userID, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelRequestAccountVerification;

            var json = JsonConvert.SerializeObject(new { user_id = userID });
            LootLockerServerRequest.CallDomainAuthAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerResponse? response = new LootLockerResponse
                {
                    text = serverResponse.text,
                    success = serverResponse.success,
                    Error = serverResponse.Error,
                    statusCode = serverResponse.statusCode
                };

                onComplete?.Invoke(response);
            });
        }
    }
}
