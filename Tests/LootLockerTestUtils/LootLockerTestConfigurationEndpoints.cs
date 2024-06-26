using System;
using LootLocker;
using UnityEngine;


namespace LootLockerTestConfigurationUtils
{
    #region HTTP Interface
    public class LootLockerAdminRequest
    {
        public static void Send(string endPoint, LootLockerHTTPMethod httpMethod, string json, Action<LootLockerResponse> onComplete, bool useAuthToken)
        {
            LootLockerConfig.DebugLevel debugLevelSavedState = LootLockerConfig.current.currentDebugLevel;
            LootLockerConfig.current.currentDebugLevel = LootLockerConfig.DebugLevel.AllAsNormal;

            LootLockerServerRequest.CallAPI(endPoint, httpMethod, json, onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
                    LootLockerConfig.current.currentDebugLevel = debugLevelSavedState;
                },
                useAuthToken,
                callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);

        }
    }
    #endregion



    #region Endpoints
    public class LootLockerTestConfigurationEndpoints
    {
        [Header("LootLocker Admin API Authentication")]
        public static EndPointClass LoginEndpoint = new EndPointClass("v1/session", LootLockerHTTPMethod.POST);
        public static EndPointClass SignupEndpoint = new EndPointClass("v1/signup", LootLockerHTTPMethod.POST);

        [Header("LootLocker Admin API Game Operations")]
        public static EndPointClass CreateGame = new EndPointClass("v1/game", LootLockerHTTPMethod.POST);
        public static EndPointClass DeleteGame = new EndPointClass("v1/game/{0}", LootLockerHTTPMethod.DELETE);

        [Header("LootLocker Admin API Key Operations")]
        public static EndPointClass CreateKey = new EndPointClass("game/{0}/api_keys", LootLockerHTTPMethod.POST);

        [Header("LootLocker Admin API Platform Operations")]
        public static EndPointClass UpdatePlatform = new EndPointClass("game/{0}/platforms/{1}", LootLockerHTTPMethod.PUT);
    }
    #endregion
}
