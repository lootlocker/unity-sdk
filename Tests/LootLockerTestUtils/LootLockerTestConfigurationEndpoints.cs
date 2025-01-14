using System;
using System.Collections;
using LootLocker;
using LootLocker.LootLockerEnums;
using UnityEngine;


namespace LootLockerTestConfigurationUtils
{
    #region HTTP Interface
    public class LootLockerAdminRequest
    {
        public static int ActiveGameId;
        private static int _retries = 0;
        private static readonly int MaxRetries = 10;

        public static void Send(string endPoint, LootLockerHTTPMethod httpMethod, string json, Action<LootLockerResponse> onComplete, bool useAuthToken)
        {
            if (_retries > MaxRetries)
            {
                _retries = 0;
                onComplete?.Invoke(new LootLockerResponse{statusCode = 0, success = false, text = "Request exceeded the allowed number of retries", errorData = new LootLockerErrorData{ message = "Request exceeded the allowed number of retries" } });
                return;
            }
            LootLockerLogger.LogLevel logLevelSavedState = LootLockerConfig.current.logLevel;
            LootLockerConfig.current.logLevel = LootLockerLogger.LogLevel.Verbose;
            LootLockerConfig.current.logErrorsAsWarnings = true;
            
            endPoint = endPoint.Replace("#GAMEID#", ActiveGameId.ToString());

            LootLockerServerRequest.CallAPI(endPoint, httpMethod, json, onComplete: (serverResponse) =>
                {
                    LootLockerResponse.Deserialize(onComplete, serverResponse);
                    if (!serverResponse.success && serverResponse.errorData.retry_after_seconds > 0)
                    {
                        LootLockerCIRetry go = new GameObject().AddComponent<LootLockerCIRetry>();
                        _retries++;
                        go.Retry(serverResponse.errorData.retry_after_seconds.Value, endPoint, httpMethod, json, onComplete, useAuthToken);
                    }
                    LootLockerConfig.current.logLevel = logLevelSavedState;
                },
                useAuthToken,
                callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }



    }

    public class LootLockerCIRetry : MonoBehaviour
    { 
        public void Retry(int retryAfter, string endPoint, LootLockerHTTPMethod httpMethod, string json,
            Action<LootLockerResponse> onComplete, bool useAuthToken)
        { 
            StartCoroutine(RetrySendAfter(retryAfter, endPoint, httpMethod, json, onComplete, useAuthToken));
        }
        private IEnumerator RetrySendAfter(int retryAfter, string endPoint, LootLockerHTTPMethod httpMethod,
            string json, Action<LootLockerResponse> onComplete, bool useAuthToken)
        {
            yield return new WaitForSeconds(retryAfter);
            LootLockerAdminRequest.Send(endPoint, httpMethod, json, onComplete, useAuthToken);
            Destroy(this.gameObject);
        }
    }

    #endregion



    #region Endpoints
    public class LootLockerTestConfigurationEndpoints
    {
        [Header("LootLocker Admin API Authentication")]
        public static EndPointClass LoginEndpoint = new EndPointClass("v1/session", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);
        public static EndPointClass SignupEndpoint = new EndPointClass("v1/signup", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);

        [Header("LootLocker Admin API Game Operations")]
        public static EndPointClass CreateGame = new EndPointClass("v1/game", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);
        public static EndPointClass DeleteGame = new EndPointClass("v1/game/#GAMEID#", LootLockerHTTPMethod.DELETE, LootLockerCallerRole.Admin);

        [Header("LootLocker Admin API Key Operations")]
        public static EndPointClass CreateKey = new EndPointClass("game/#GAMEID#/api_keys", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);

        [Header("LootLocker Admin API Platform Operations")]
        public static EndPointClass UpdatePlatform = new EndPointClass("game/#GAMEID#/platforms/{0}", LootLockerHTTPMethod.PUT, LootLockerCallerRole.Admin);

        [Header("LootLocker Admin API Leaderboard Operations")]
        public static EndPointClass createLeaderboard = new EndPointClass("game/#GAMEID#/leaderboards", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);
        public static EndPointClass updateLeaderboard = new EndPointClass("game/#GAMEID#/leaderboards/{0}", LootLockerHTTPMethod.PUT, LootLockerCallerRole.Admin);
        public static EndPointClass updateLeaderboardSchedule = new EndPointClass("game/#GAMEID#/leaderboard/{0}/schedule", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);
        public static EndPointClass addLeaderboardReward = new EndPointClass("game/#GAMEID#/leaderboard/{0}/reward", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);

        [Header("LootLocker Admin API Asset Operations")]
        public static EndPointClass getAssetContexts = new EndPointClass("/v1/game/#GAMEID#/assets/contexts", LootLockerHTTPMethod.GET, LootLockerCallerRole.Admin);
        public static EndPointClass createAsset = new EndPointClass("/v1/game/#GAMEID#/asset", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);
        public static EndPointClass createReward = new EndPointClass("game/#GAMEID#/reward", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);

        [Header("LootLocker Admin API Trigger Operations")]
        public static EndPointClass createTrigger = new EndPointClass("game/#GAMEID#/triggers/cozy-crusader/v1", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);
    }
    #endregion
}
