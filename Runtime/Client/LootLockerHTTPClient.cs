#if LOOTLOCKER_BETA_HTTP_QUEUE
using System.Collections.Generic;
using UnityEngine;
using System;
using LootLocker.LootLockerEnums;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using LootLocker.Requests;
using LootLocker.HTTP;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace LootLocker
{
    /// <summary>
    /// Construct a request to send to the server.
    /// </summary>
    [Serializable]
    public struct LootLockerServerRequest
    {
        #region Make ServerRequest and call send (3 functions)

        public static void CallAPI(string endPoint, LootLockerHTTPMethod httpMethod, string body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            if (httpMethod == LootLockerHTTPMethod.GET || httpMethod == LootLockerHTTPMethod.HEAD || httpMethod == LootLockerHTTPMethod.OPTIONS)
            {
                if (!string.IsNullOrEmpty(body))
                {
                    LootLockerLogger.Log("Payloads can not be sent in GET, HEAD, or OPTIONS requests. Attempted to send a body to: " + httpMethod.ToString() + " " + endPoint, LootLockerLogger.LogLevel.Warning);
                }
                LootLockerHTTPClient.Get().ScheduleRequest(LootLockerHTTPRequestData.MakeNoContentRequest(endPoint, httpMethod, onComplete, useAuthToken, callerRole, additionalHeaders, null));
            }
            else
            {
                LootLockerHTTPClient.Get().ScheduleRequest(LootLockerHTTPRequestData.MakeJsonRequest(endPoint, httpMethod, body, onComplete, useAuthToken, callerRole, additionalHeaders, null));
            }
        }

        public static void UploadFile(string endPoint, LootLockerHTTPMethod httpMethod, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            if (file == null || file.Length == 0)
            {
                LootLockerLogger.Log("File content is empty, not allowed.", LootLockerLogger.LogLevel.Error);
                onComplete(LootLockerResponseFactory.ClientError<LootLockerResponse>("File content is empty, not allowed."));
                return;
            }

            LootLockerHTTPClient.Get().ScheduleRequest(LootLockerHTTPRequestData.MakeFileRequest(endPoint, httpMethod, file, fileName, fileContentType, body, onComplete, useAuthToken, callerRole, additionalHeaders, null));
        }
        
        public static void UploadFile(EndPointClass endPoint, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            UploadFile(endPoint.endPoint, endPoint.httpMethod, file, fileName, fileContentType, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken, callerRole, additionalHeaders);
        }

        #endregion
    }

    #if UNITY_EDITOR
    [ExecuteInEditMode]
    #endif
    public class LootLockerHTTPClient : MonoBehaviour
    {
        #region Configuration
        private const int MaxRetries = 5;
        private const int IncrementalBackoffFactor = 2;
        private const int InitialRetryWaitTimeInMs = 50;
        private const int MaxOngoingRequests = 50;
        private const int ChokeWarningThreshold = 500;
        private const bool DenyIncomingRequestsWhenBackedUp = true;
        private Dictionary<string, bool> CurrentlyOngoingRequests =  new Dictionary<string, bool>();

        private static readonly Dictionary<string, string> BaseHeaders = new Dictionary<string, string>
        {
            { "Accept", "application/json; charset=UTF-8" },
            { "Content-Type", "application/json; charset=UTF-8" },
            { "Access-Control-Allow-Credentials", "true" },
            { "Access-Control-Allow-Headers", "Accept, X-Access-Token, X-Application-Name, X-Request-Sent-Time" },
            { "Access-Control-Allow-Methods", "GET, POST, DELETE, PUT, OPTIONS, HEAD" },
            { "Access-Control-Allow-Origin", "*" },
            { "LL-Instance-Identifier", System.Guid.NewGuid().ToString() }
        };
        #endregion

        #region Instance Handling
        private static LootLockerHTTPClient _instance;
        private static int _instanceId = 0;
        public GameObject HostingGameObject = null;

        public static void Instantiate()
        {
            if (_instance == null)
            {
                var gameObject = new GameObject("LootLockerHTTPClient");

                _instance = gameObject.AddComponent<LootLockerHTTPClient>();
                _instanceId = _instance.GetInstanceID();
                _instance.HostingGameObject = gameObject;
                _instance.StartCoroutine(CleanUpOldInstances());
                if (Application.isPlaying)
                    DontDestroyOnLoad(_instance.gameObject);
            }
        }

        public static IEnumerator CleanUpOldInstances()
        {
#if UNITY_2020_1_OR_NEWER
            LootLockerHTTPClient[] serverApis = GameObject.FindObjectsByType<LootLockerHTTPClient>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            LootLockerHTTPClient[] serverApis = GameObject.FindObjectsOfType<LootLockerHTTPClient>();
#endif
            foreach (LootLockerHTTPClient serverApi in serverApis)
            {
                if (serverApi != null && _instanceId != serverApi.GetInstanceID() && serverApi.HostingGameObject != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(serverApi.HostingGameObject);
#else
                    Destroy(serverApi.HostingGameObject);
#endif
                }
            }
            yield return null;
        }

        public static void ResetInstance()
        {
            if (_instance == null) return;
#if UNITY_EDITOR
            DestroyImmediate(_instance.gameObject);
#else
            Destroy(_instance.gameObject);
#endif
            _instance = null;
            _instanceId = 0;
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            ResetInstance();
        }
#endif

        public static LootLockerHTTPClient Get()
        {
            if (_instance == null)
            {
                Instantiate();
            }
            return _instance;
        }
        #endregion

        private Dictionary<string, LootLockerHTTPExecutionQueueItem> HTTPExecutionQueue = new Dictionary<string, LootLockerHTTPExecutionQueueItem>();
        private List<string> CompletedRequestIDs = new List<string>();

        private void OnDestroy()
        {
            foreach(var executionItem in HTTPExecutionQueue.Values)
            {
                if(executionItem != null && executionItem.WebRequest != null)
                {
                    executionItem.Dispose();
                }
            }
        }

        void Update()
        {
            List<string> ExecutionItemsNeedingRefresh = new List<string>();
            foreach(var executionItem in HTTPExecutionQueue.Values)
            {
                // Skip completed requests
                if(executionItem.Done)
                {
                    if(!CompletedRequestIDs.Contains(executionItem.RequestData.RequestId))
                    {
                        CompletedRequestIDs.Add(executionItem.RequestData.RequestId);
                    }
                    continue;
                }

                // Skip requests that are waiting for session refresh
                if (executionItem.IsWaitingForSessionRefresh)
                {
                    continue;
                }

                // Send unsent
                if (executionItem.AsyncOperation == null && executionItem.WebRequest == null)
                {
                    if(executionItem.RetryAfter != null && executionItem.RetryAfter > DateTime.Now)
                    {
                        // Wait for retry
                        continue;
                    }

                    if (CurrentlyOngoingRequests.Count >= MaxOngoingRequests)
                    {
                        // Wait for some requests to finish before scheduling more requests
                        continue;
                    }

                    CreateAndSendRequest(executionItem);
                    continue;
                }

                // Process ongoing
                var Result = ProcessOngoingRequest(executionItem);

                if(Result == HTTPExecutionQueueProcessingResult.NeedsSessionRefresh)
                {
                    //Bulk handle session refreshes at the end
                    ExecutionItemsNeedingRefresh.Add(executionItem.RequestData.RequestId);
                    continue;
                }
                else if(Result == HTTPExecutionQueueProcessingResult.WaitForNextTick || Result == HTTPExecutionQueueProcessingResult.None)
                {
                    // Nothing to handle, simply continue
                    continue;
                }

                HandleRequestResult(executionItem, Result);                
            }

            // Bulk session refresh requests
            if (ExecutionItemsNeedingRefresh.Count > 0)
            {
                foreach (string executionItemId in ExecutionItemsNeedingRefresh)
                {
                    if (HTTPExecutionQueue.TryGetValue(executionItemId, out var executionItem))
                    {
                        CurrentlyOngoingRequests.Remove(executionItem.RequestData.RequestId);
                        executionItem.IsWaitingForSessionRefresh = true;
                        executionItem.RequestData.TimesRetried++;

                        // Unsetting web request fields will make the execution queue retry it
                        executionItem.AbortRequest();
                    }
                }

                string tokenBeforeRefresh = LootLockerConfig.current.token;
                StartCoroutine(RefreshSession(newSessionResponse =>
                {
                    foreach (string executionItemId in ExecutionItemsNeedingRefresh)
                    {
                        if (HTTPExecutionQueue.TryGetValue(executionItemId, out var executionItem))
                        {
                            if (tokenBeforeRefresh.Equals(LootLockerConfig.current.token))
                            {
                                // Session refresh failed so abort call chain
                                CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                            }

                            // Session refresh worked so update the session token header
                            if (executionItem.RequestData.CallerRole == LootLockerCallerRole.Admin)
                            {
#if UNITY_EDITOR
                                executionItem.RequestData.ExtraHeaders["x-auth-token"] = LootLockerConfig.current.adminToken;
#endif
                            }
                            else
                            {
                                executionItem.RequestData.ExtraHeaders["x-session-token"] = LootLockerConfig.current.token;
                            }

                            // Mark request as ready for continuation
                            executionItem.IsWaitingForSessionRefresh = false;
                        }
                    }
                }));
            }

            if((HTTPExecutionQueue.Count - CurrentlyOngoingRequests.Count) > ChokeWarningThreshold)
            {
                LootLockerLogger.Log($"LootLocker HTTP Execution Queue is overloaded. Requests currently waiting for execution: '{(HTTPExecutionQueue.Count - CurrentlyOngoingRequests.Count)}'", LootLockerLogger.LogLevel.Warning);
            }
        }

        private void LateUpdate()
        {
            // Do Cleanup
            foreach (var CompletedRequestID in CompletedRequestIDs)
            {
                if(HTTPExecutionQueue.TryGetValue(CompletedRequestID, out var completedRequest))
                {
                    if(!completedRequest.Done)
                    {
                        continue;
                    }

                    if(!completedRequest.RequestData.HaveListenersBeenInvoked)
                    {
                        if(completedRequest.Response != null)
                        {
                            completedRequest.RequestData.CallListenersWithResult(completedRequest.Response);
                        }
                        else if (completedRequest.WebRequest != null)
                        {
                            if (WebRequestSucceeded(completedRequest.WebRequest))
                            {
                                completedRequest.RequestData.CallListenersWithResult(LootLockerResponseFactory.Success<LootLockerResponse>((int)completedRequest.WebRequest.responseCode, completedRequest.WebRequest.downloadHandler.text));
                            }
                            else
                            {
                                completedRequest.RequestData.CallListenersWithResult(ExtractFailureResponseFromExecutionItem(completedRequest));
                            }
                        }
                        else
                        {
                            completedRequest.RequestData.CallListenersWithResult(LootLockerResponseFactory.ClientError<LootLockerResponse>("Request completed but no response was present"));
                        }
                    }

                    HTTPExecutionQueue.Remove(CompletedRequestID);
                    completedRequest.Dispose();
                }
            }
            CompletedRequestIDs.Clear();

            foreach (var ExecutionItem in HTTPExecutionQueue.Values)
            {
                // Find stragglers
                if (ExecutionItem.Done)
                {
                    CompletedRequestIDs.Add(ExecutionItem.RequestData.RequestId);
                }
            }

            List<string> OngoingIdsToCleanUp = new List<string>();
            foreach(string OngoingId in CurrentlyOngoingRequests.Keys)
            {
                if(!HTTPExecutionQueue.TryGetValue(OngoingId, out var executionQueueItem) || executionQueueItem.Done)
                {
                    OngoingIdsToCleanUp.Add(OngoingId);
                }
            }
            foreach(string CompletedId in OngoingIdsToCleanUp)
            {
                CurrentlyOngoingRequests.Remove(CompletedId);
            }
        }

        public void ScheduleRequest(LootLockerHTTPRequestData request)
        {
            StartCoroutine(_ScheduleRequest(request));
        }

        private IEnumerator _ScheduleRequest(LootLockerHTTPRequestData request)
        {
            //Always wait 1 frame before starting any request to the server to make sure the requester code has exited the main thread.
            yield return null;

            if (DenyIncomingRequestsWhenBackedUp && (HTTPExecutionQueue.Count - CurrentlyOngoingRequests.Count) > ChokeWarningThreshold)
            {
                // Execution queue is backed up, deny request
                request.CallListenersWithResult(LootLockerResponseFactory.ClientError<LootLockerResponse>("Request was denied because there are currently too many requests in queue"));
                yield break;
            }

            if (HTTPExecutionQueue.TryGetValue(request.RequestId, out var executionQueueItem))
            {
                executionQueueItem.RequestData.Listeners.AddRange(request.Listeners);
                yield break;
            }
            HTTPExecutionQueue.Add(request.RequestId, new LootLockerHTTPExecutionQueueItem { RequestData = request });
        }

        private bool CreateAndSendRequest(LootLockerHTTPExecutionQueueItem executionItem)
        {
            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(executionItem.RequestData.Endpoint, RateLimiter.Get().GetSecondsLeftOfRateLimit()));
                return false;
            }

            LootLockerLogger.Log("ServerRequest " + executionItem.RequestData.HTTPMethod + " URL: " + executionItem.RequestData.FormattedURL, LootLockerLogger.LogLevel.Verbose);

            UnityWebRequest webRequest = CreateWebRequest(executionItem.RequestData);
            if (webRequest == null)
            {
                CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.ClientError<LootLockerResponse>($"Call to {executionItem.RequestData.Endpoint} failed because Unity Web Request could not be created"));
                return false;
            }

            executionItem.RequestStartTime = Time.time;

            executionItem.WebRequest = webRequest;
            executionItem.AsyncOperation = executionItem.WebRequest.SendWebRequest();
            CurrentlyOngoingRequests.Add(executionItem.RequestData.RequestId, true);
            return true;
        }

        private HTTPExecutionQueueProcessingResult ProcessOngoingRequest(LootLockerHTTPExecutionQueueItem executionItem)
        {
            if (executionItem.AsyncOperation == null)
            {
                return HTTPExecutionQueueProcessingResult.WaitForNextTick;
            }

            bool timedOut = !executionItem.AsyncOperation.isDone && (Time.time - executionItem.RequestStartTime) >= LootLockerConfig.current.clientSideRequestTimeOut;
            if(timedOut)
            {
                return HTTPExecutionQueueProcessingResult.Completed_TimedOut;
            }

            // Not timed out and not done, nothing to do
            if(!executionItem.AsyncOperation.isDone)
            {
                return HTTPExecutionQueueProcessingResult.WaitForNextTick;
            }

            if (WebRequestSucceeded(executionItem.WebRequest))
            {
                return HTTPExecutionQueueProcessingResult.Completed_Success;
            }

            if (ShouldRetryRequest(executionItem.WebRequest.responseCode, executionItem.RequestData.TimesRetried) && !(executionItem.WebRequest.responseCode == 401 && !IsAuthorizedRequest(executionItem)))
            {
                if (ShouldRefreshSession(executionItem.WebRequest.responseCode) && (CanRefreshUsingRefreshToken(executionItem.RequestData) || CanStartNewSessionUsingCachedData()))
                {
                    executionItem.IsWaitingForSessionRefresh = true;
                    return HTTPExecutionQueueProcessingResult.NeedsSessionRefresh;
                }
                return HTTPExecutionQueueProcessingResult.ShouldBeRetried;
            }

            
            return HTTPExecutionQueueProcessingResult.Completed_Failed;
        }

        private void HandleRequestResult(LootLockerHTTPExecutionQueueItem executionItem, HTTPExecutionQueueProcessingResult result)
        {
            switch(result)
            {
                case HTTPExecutionQueueProcessingResult.None:
                case HTTPExecutionQueueProcessingResult.WaitForNextTick:
                case HTTPExecutionQueueProcessingResult.NeedsSessionRefresh:
                default:
                    {
                        // Should be handled outside this method, nothing to do
                        return;
                    }
                case HTTPExecutionQueueProcessingResult.Completed_Success:
                    {
                        CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.Success<LootLockerResponse>((int)executionItem.WebRequest.responseCode, executionItem.WebRequest.downloadHandler.text));
                    }
                    break;
                case HTTPExecutionQueueProcessingResult.ShouldBeRetried:
                    {
                        int RetryAfterHeader = ExtractRetryAfterFromHeader(executionItem);
                        if (RetryAfterHeader > 0)
                        {
                            // If the retry after header suggests to retry after we'd have timed out the request then handle it as a failure
                            if (executionItem.RequestStartTime + RetryAfterHeader > LootLockerConfig.current.clientSideRequestTimeOut)
                            {
                                LootLockerResponse response = LootLockerResponseFactory.Failure<LootLockerResponse>((int)executionItem.WebRequest.responseCode, executionItem.WebRequest.downloadHandler.text);
                                response.errorData = ExtractErrorData(response);
                                if (response.errorData != null)
                                {
                                    response.errorData.retry_after_seconds = RetryAfterHeader;
                                }

                                LootLockerLogger.Log(response.errorData.ToString(), LootLockerLogger.LogLevel.Error);
                                CallListenersAndMarkDone(executionItem, response);
                                return;
                            }
                            executionItem.RetryAfter = DateTime.Now.AddSeconds(RetryAfterHeader);
                        }
                        else
                        {
                            // Incremental backoff
                            executionItem.RetryAfter = DateTime.Now.AddMilliseconds(InitialRetryWaitTimeInMs + (InitialRetryWaitTimeInMs * executionItem.RequestData.TimesRetried*IncrementalBackoffFactor));
                        }
                        executionItem.RequestData.TimesRetried++;

                        // Unsetting web request fields will make the execution queue retry it
                        executionItem.AbortRequest();

                        CurrentlyOngoingRequests.Remove(executionItem.RequestData.RequestId);
                        return;
                    }
                case HTTPExecutionQueueProcessingResult.Completed_TimedOut:
                    {
                        CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.RequestTimeOut<LootLockerResponse>());
                    }
                    break;
                case HTTPExecutionQueueProcessingResult.Completed_Failed:
                    {
                        LootLockerResponse response = ExtractFailureResponseFromExecutionItem(executionItem);

                        LootLockerLogger.Log(response.errorData.ToString(), LootLockerLogger.LogLevel.Error);
                        CallListenersAndMarkDone(executionItem, response);
                    }
                    break;
            }

            LogResponse(executionItem);
        }

        private void CallListenersAndMarkDone(LootLockerHTTPExecutionQueueItem executionItem, LootLockerResponse response)
        {
            CurrentlyOngoingRequests.Remove(executionItem.RequestData.RequestId);
            executionItem.IsWaitingForSessionRefresh = false;
            executionItem.Done = true;
            executionItem.Response = response;
            executionItem.RequestData.CallListenersWithResult(response);
            CompletedRequestIDs.Add(executionItem.RequestData.RequestId);
        }

        private IEnumerator RefreshSession(Action<LootLockerSessionResponse> onSessionRefreshedCallback)
        {
            LootLockerSessionResponse newSessionResponse = null;
            bool callCompleted = false;
            switch (CurrentPlatform.Get())
            {
                case Platforms.Guest:
                    {
                        LootLockerSDKManager.StartGuestSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case Platforms.WhiteLabel:
                    {
                        LootLockerSDKManager.StartWhiteLabelSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case Platforms.AppleGameCenter:
                    {
                        LootLockerSDKManager.RefreshAppleGameCenterSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case Platforms.AppleSignIn:
                    {
                        LootLockerSDKManager.RefreshAppleSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case Platforms.Epic:
                    {
                        LootLockerSDKManager.RefreshEpicSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case Platforms.Google:
                    {
                        LootLockerSDKManager.RefreshGoogleSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case Platforms.Remote:
                    {
                        LootLockerSDKManager.RefreshRemoteSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case Platforms.PlayStationNetwork:
                case Platforms.XboxOne:
                case Platforms.AmazonLuna:
                    {
                        LootLockerAPIManager.Session(new LootLockerSessionRequest(LootLockerConfig.current.deviceID), (response) =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case Platforms.NintendoSwitch:
                case Platforms.Steam:
                    {
                        LootLockerLogger.Log($"Token has expired and token refresh is not supported for {CurrentPlatform.GetFriendlyString()}", LootLockerLogger.LogLevel.Warning);
                        yield break;
                    }
                case Platforms.None:
                default:
                    {
                        LootLockerLogger.Log($"Token refresh for platform {CurrentPlatform.GetFriendlyString()} not supported", LootLockerLogger.LogLevel.Error);
                        yield break;
                    }
            }
            yield return new WaitUntil(() => callCompleted);
            onSessionRefreshedCallback?.Invoke(newSessionResponse);
        }

        #region Session Refresh Helper Methods

        private static bool ShouldRetryRequest(long statusCode, int timesRetried)
        {
            return (statusCode == 401 || statusCode == 403 || statusCode == 502 || statusCode == 500 || statusCode == 503) && LootLockerConfig.current.allowTokenRefresh && CurrentPlatform.Get() != Platforms.Steam && timesRetried < MaxRetries;
        }

        private static bool ShouldRefreshSession(long statusCode)
        {
            return (statusCode == 401 || statusCode == 403) && LootLockerConfig.current.allowTokenRefresh && CurrentPlatform.Get() != Platforms.Steam;
        }

        private static bool IsAuthorizedRequest(LootLockerHTTPExecutionQueueItem request)
        {
            return !string.IsNullOrEmpty(request.WebRequest?.GetRequestHeader("x-session-token")) || !string.IsNullOrEmpty(request.WebRequest?.GetRequestHeader("x-auth-token"));
        }

        private static bool CanRefreshUsingRefreshToken(LootLockerHTTPRequestData cachedRequest)
        {
            if (!LootLockerPlatformSettings.PlatformsWithRefreshTokens.Contains(CurrentPlatform.Get()))
            {
                return false;
            }
            // The failed request isn't a refresh session request but we have a refresh token stored, so try to refresh the session automatically before failing
            string json = cachedRequest.Content.dataType == LootLockerHTTPRequestDataType.JSON ? ((LootLockerJsonBodyRequestContent)cachedRequest.Content).jsonBody : null;
            return (string.IsNullOrEmpty(json) || !json.Contains("refresh_token")) && !string.IsNullOrEmpty(LootLockerConfig.current.refreshToken);
        }

        private static bool CanStartNewSessionUsingCachedData()
        {
            if (!LootLockerPlatformSettings.PlatformsWithStoredAuthData.Contains(CurrentPlatform.Get()))
            {
                return false;
            }
            if (CurrentPlatform.Get() == Platforms.Guest)
            {
                return true;
            }
            else if (CurrentPlatform.Get() == Platforms.WhiteLabel && !string.IsNullOrEmpty(PlayerPrefs.GetString("LootLockerWhiteLabelSessionToken", "")) && !string.IsNullOrEmpty(PlayerPrefs.GetString("LootLockerWhiteLabelSessionEmail", "")))
            {
                return true;
            }
            else
            {
                return !string.IsNullOrEmpty(LootLockerConfig.current.deviceID);
            }
        }
        #endregion

        #region Web Request Helper Methods
        private bool WebRequestSucceeded(UnityWebRequest webRequest)
        {
            return !
#if UNITY_2020_1_OR_NEWER
            (webRequest.result == UnityWebRequest.Result.ProtocolError || webRequest.result == UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(webRequest.error));
#else
            (webRequest.isHttpError || webRequest.isNetworkError || !string.IsNullOrEmpty(webRequest.error));
#endif
        }

        private LootLockerResponse ExtractFailureResponseFromExecutionItem(LootLockerHTTPExecutionQueueItem executionItem)
        {
            LootLockerResponse response = LootLockerResponseFactory.Failure<LootLockerResponse>((int)executionItem.WebRequest.responseCode, executionItem.WebRequest.downloadHandler.text);
            response.errorData = ExtractErrorData(response);
            if (response.errorData != null)
            {
                response.errorData.retry_after_seconds = ExtractRetryAfterFromHeader(executionItem);
            }
            return response;
        }

        private UnityWebRequest CreateWebRequest(LootLockerHTTPRequestData request)
        {
            UnityWebRequest webRequest = null;
            switch (request.HTTPMethod)
            {
                case LootLockerHTTPMethod.OPTIONS:
                case LootLockerHTTPMethod.HEAD:
                case LootLockerHTTPMethod.GET:
                    webRequest = UnityWebRequest.Get(request.FormattedURL);
                    webRequest.method = request.HTTPMethod.ToString();
                    break;

                case LootLockerHTTPMethod.DELETE:
                    webRequest = UnityWebRequest.Delete(request.FormattedURL);
                    break;
                case LootLockerHTTPMethod.UPLOAD_FILE:
                case LootLockerHTTPMethod.UPDATE_FILE:
                    if (request.Content.dataType != LootLockerHTTPRequestDataType.FILE)
                    {
                        request.CallListenersWithResult(LootLockerResponseFactory.ClientError<LootLockerResponse>("File request without file content"));
                        return null;
                    }
                    webRequest = UnityWebRequest.Post(request.FormattedURL, ((LootLockerFileRequestContent)request.Content).fileForm);
                    if(request.HTTPMethod == LootLockerHTTPMethod.UPDATE_FILE)
                    {
                        // Workaround for UnityWebRequest with PUT HTTP verb not having form fields
                        webRequest.method = UnityWebRequest.kHttpVerbPUT;
                    }
                    break;
                case LootLockerHTTPMethod.POST:
                case LootLockerHTTPMethod.PATCH:
                case LootLockerHTTPMethod.PUT:
                    if (request.Content.dataType == LootLockerHTTPRequestDataType.WWW_FORM)
                    {
                        webRequest = MakeWWWFormWebRequest(request);
                    }
                    else
                    {
                        LootLockerLogger.Log("REQUEST BODY = " + LootLockerObfuscator.ObfuscateJsonStringForLogging(((LootLockerJsonBodyRequestContent)request.Content).jsonBody), LootLockerLogger.LogLevel.Verbose);
                        byte[] bytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(((LootLockerJsonBodyRequestContent)request.Content).jsonBody) ? "{}" : ((LootLockerJsonBodyRequestContent)request.Content).jsonBody);
                        webRequest = UnityWebRequest.Put(request.FormattedURL, bytes);
                        webRequest.method = request.HTTPMethod.ToString();
                    }
                    break;
                default:
                    request.CallListenersWithResult(LootLockerResponseFactory.ClientError<LootLockerResponse>("Unsupported HTTP Method"));
                    return webRequest;
            }

            if (BaseHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in BaseHeaders)
                {
                    if (pair.Key == "Content-Type" && request.Content.dataType != LootLockerHTTPRequestDataType.JSON) continue;

                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            if (!string.IsNullOrEmpty(LootLockerConfig.current?.sdk_version))
            {
                webRequest.SetRequestHeader("LL-SDK-Version", LootLockerConfig.current.sdk_version);
            }

            if (request.ExtraHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in request.ExtraHeaders)
                {
                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            webRequest.downloadHandler = new DownloadHandlerBuffer();
            return webRequest;
        }

        private static UnityWebRequest MakeWWWFormWebRequest(LootLockerHTTPRequestData request)
        {
            UnityWebRequest webRequest = new UnityWebRequest();
            var content = (LootLockerWWWFormRequestContent)request.Content;
            List<IMultipartFormSection> form = new List<IMultipartFormSection>
                        {
                            new MultipartFormFileSection(content.name, content.content, System.DateTime.Now.ToString(), content.type)
                        };

            // generate a boundary then convert the form to byte[]
            byte[] boundary = UnityWebRequest.GenerateBoundary();
            byte[] formSections = UnityWebRequest.SerializeFormSections(form, boundary);
            // Set the content type - NO QUOTES around the boundary
            string contentType = String.Concat("multipart/form-data; boundary=--", Encoding.UTF8.GetString(boundary));

            // Make my request object and add the raw text. Set anything else you need here
            webRequest.SetRequestHeader("Content-Type", "multipart/form-data; boundary=--");
            webRequest.uri = new Uri(request.FormattedURL);
            webRequest.uploadHandler = new UploadHandlerRaw(formSections);
            webRequest.uploadHandler.contentType = contentType;
            webRequest.useHttpContinue = false;

            // webRequest.method = "POST";
            webRequest.method = UnityWebRequest.kHttpVerbPOST;
            return webRequest;
        }
        #endregion

        #region Misc Helper Methods

        private static int ExtractRetryAfterFromHeader(LootLockerHTTPExecutionQueueItem executionItem)
        {
            int retryAfterSeconds = -1;
            string RetryAfterHeader = executionItem.WebRequest.GetResponseHeader("Retry-After");
            if (!string.IsNullOrEmpty(RetryAfterHeader))
            {
                retryAfterSeconds = int.Parse(RetryAfterHeader);
            }
            return retryAfterSeconds;
        }

        private static LootLockerErrorData ExtractErrorData(LootLockerResponse response)
        {
            LootLockerErrorData errorData = null;
            try
            {
                errorData = LootLockerJson.DeserializeObject<LootLockerErrorData>(response.text);
            }
            catch (Exception)
            {
                if (response.text.StartsWith("<"))
                {
                    LootLockerLogger.Log("Non Json Response body (starts with <), info: \n    statusCode: " + response.statusCode + "\n    body: " + response.text, LootLockerLogger.LogLevel.Warning);
                }
                errorData = null;
            }
            // Error data was not parseable, populate with what we know
            if (errorData == null)
            {
                errorData = new LootLockerErrorData(response.statusCode, response.text);
            }
            return errorData;
        }

        private static void LogResponse(LootLockerHTTPExecutionQueueItem executedItem)
        {
            if(!executedItem.Done)
            {
                return;
            }
            if (executedItem.WebRequest.responseCode == 0 && string.IsNullOrEmpty(executedItem.WebRequest.downloadHandler.text) && !string.IsNullOrEmpty(executedItem.WebRequest.error))
            {
                LootLockerLogger.Log("Unity Web request failed, request to " +
                    executedItem.RequestData.FormattedURL + " completed in " +
                    (Time.time - executedItem.RequestStartTime).ToString("n4") +
                    " secs.\nWeb Request Error: " + executedItem.WebRequest.error, LootLockerLogger.LogLevel.Verbose);
                return;
            }

            try
            {
                LootLockerLogger.Log("Server Response: " +
                    executedItem.WebRequest.responseCode + " " +
                    executedItem.RequestData.FormattedURL + " completed in " +
                    (Time.time - executedItem.RequestStartTime).ToString("n4") +
                    " secs.\nResponse: " +
                    LootLockerObfuscator
                        .ObfuscateJsonStringForLogging(executedItem.WebRequest.downloadHandler.text), LootLockerLogger.LogLevel.Verbose);
            }
            catch
            {
                LootLockerLogger.Log(executedItem.RequestData.HTTPMethod.ToString(), LootLockerLogger.LogLevel.Error);
                LootLockerLogger.Log(executedItem.RequestData.FormattedURL, LootLockerLogger.LogLevel.Error);
                LootLockerLogger.Log(LootLockerObfuscator.ObfuscateJsonStringForLogging(executedItem.WebRequest.downloadHandler.text), LootLockerLogger.LogLevel.Error);
            }
        }
        #endregion
    }
}
#endif
