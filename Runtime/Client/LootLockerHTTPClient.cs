#if !LOOTLOCKER_LEGACY_HTTP_STACK
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

        public static void CallAPI(string forPlayerWithUlid, string endPoint, LootLockerHTTPMethod httpMethod, string body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            if (httpMethod == LootLockerHTTPMethod.GET || httpMethod == LootLockerHTTPMethod.HEAD || httpMethod == LootLockerHTTPMethod.OPTIONS)
            {
                if (!string.IsNullOrEmpty(body))
                {
                    LootLockerLogger.Log("Payloads can not be sent in GET, HEAD, or OPTIONS requests. Attempted to send a body to: " + httpMethod.ToString() + " " + endPoint, LootLockerLogger.LogLevel.Warning);
                }
                LootLockerHTTPClient.Get().ScheduleRequest(LootLockerHTTPRequestData.MakeNoContentRequest(forPlayerWithUlid, endPoint, httpMethod, onComplete, useAuthToken, callerRole, additionalHeaders, null));
            }
            else
            {
                LootLockerHTTPClient.Get().ScheduleRequest(LootLockerHTTPRequestData.MakeJsonRequest(forPlayerWithUlid, endPoint, httpMethod, body, onComplete, useAuthToken, callerRole, additionalHeaders, null));
            }
        }

        public static void UploadFile(string forPlayerWithUlid, string endPoint, LootLockerHTTPMethod httpMethod, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            if (file == null || file.Length == 0)
            {
                LootLockerLogger.Log("File content is empty, not allowed.", LootLockerLogger.LogLevel.Error);
                onComplete(LootLockerResponseFactory.ClientError<LootLockerResponse>("File content is empty, not allowed.", forPlayerWithUlid, DateTime.Now));
                return;
            }

            LootLockerHTTPClient.Get().ScheduleRequest(LootLockerHTTPRequestData.MakeFileRequest(forPlayerWithUlid, endPoint, httpMethod, file, fileName, fileContentType, body, onComplete, useAuthToken, callerRole, additionalHeaders, null));
        }
        
        public static void UploadFile(string forPlayerWithUlid, EndPointClass endPoint, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            UploadFile(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, file, fileName, fileContentType, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken, callerRole, additionalHeaders);
        }

        #endregion
    }

    public class LootLockerHTTPClientConfiguration
    {
        /*
         * The max number of times each failed request will be retried before errors are returned
         */
        public int MaxRetries = 5;
        /*
         * The multiplicative factor applied for each back off iteration. Example: If InitialRetryWaitTimeInMs is 50ms and the IncrementalBackoffFactor is 2 then the first retry will happen after 50ms, the second 100ms after that, the third 200ms after that and so on)
         */
        public int IncrementalBackoffFactor = 2;
        /*
         * The time to wait before retrying the request the first time. Example: If InitialRetryWaitTimeInMs is 50ms and the IncrementalBackoffFactor is 2 then the first retry will happen after 50ms, the second 100ms after that, the third 200ms after that and so on)
         */
        public int InitialRetryWaitTimeInMs = 50;
        /*
         * The maximum number of requests allowed to be in progress at the same time
         */
        public int MaxOngoingRequests = 50;
        /*
         * The threshold of number of requests outstanding to use for warning about the building queue
         */
        public int ChokeWarningThreshold = 500;
        /*
         * Whether to deny incoming requests when the HTTP client is already handling too many requests
         */
        public bool DenyIncomingRequestsWhenBackedUp = true;

        public LootLockerHTTPClientConfiguration()
        {
            MaxRetries = 5;
            IncrementalBackoffFactor = 2;
            InitialRetryWaitTimeInMs = 50;
            MaxOngoingRequests = 50;
            ChokeWarningThreshold = 500;
            DenyIncomingRequestsWhenBackedUp = true;
        }

        public LootLockerHTTPClientConfiguration(int maxRetries, int incrementalBackoffFactor, int initialRetryWaitTime)
        {
            MaxRetries = maxRetries;
            IncrementalBackoffFactor = incrementalBackoffFactor;
            InitialRetryWaitTimeInMs = initialRetryWaitTime;
            MaxOngoingRequests = 50;
            ChokeWarningThreshold = 500;
            DenyIncomingRequestsWhenBackedUp = true;
        }
    }

    #if UNITY_EDITOR
    [ExecuteInEditMode]
    #endif
    public class LootLockerHTTPClient : MonoBehaviour
    {
        #region Configuration

        private static LootLockerHTTPClientConfiguration configuration = new LootLockerHTTPClientConfiguration();
        private static CertificateHandler certificateHandler = null;

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
        private GameObject _hostingGameObject = null;

        public static void Instantiate()
        {
            if (!_instance)
            {
                var gameObject = new GameObject("LootLockerHTTPClient");

                _instance = gameObject.AddComponent<LootLockerHTTPClient>();
                _instanceId = _instance.GetInstanceID();
                _instance._hostingGameObject = gameObject;
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
                if (serverApi && _instanceId != serverApi.GetInstanceID() && serverApi._hostingGameObject)
                {
#if UNITY_EDITOR
                    DestroyImmediate(serverApi._hostingGameObject);
#else
                    Destroy(serverApi._hostingGameObject);
#endif
                }
            }
            yield return null;
        }

        public static void ResetInstance()
        {
            if (!_instance) return;
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
            if (!_instance)
            {
                Instantiate();
            }
            return _instance;
        }

        public void OverrideConfiguration(LootLockerHTTPClientConfiguration configuration)
        {
            if (configuration != null)
            {
                LootLockerHTTPClient.configuration = configuration;
            }
        }

        public void OverrideCertificateHandler(CertificateHandler certificateHandler)
        {
            LootLockerHTTPClient.certificateHandler = certificateHandler;
        }
        #endregion

        private Dictionary<string, LootLockerHTTPExecutionQueueItem> HTTPExecutionQueue = new Dictionary<string, LootLockerHTTPExecutionQueueItem>();
        private List<string> CompletedRequestIDs = new List<string>();
        private UniqueList<string> ExecutionItemsNeedingRefresh = new UniqueList<string>();
        private List<string> OngoingIdsToCleanUp = new List<string>();

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
            // Process the execution queue
            foreach (var executionItem in HTTPExecutionQueue.Values)
            {
                // Skip completed requests
                if (executionItem.Done)
                {
                    if (!CompletedRequestIDs.Contains(executionItem.RequestData.RequestId))
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
                    if (executionItem.RetryAfter != null && executionItem.RetryAfter > DateTime.Now)
                    {
                        // Wait for retry
                        continue;
                    }

                    if (CurrentlyOngoingRequests.Count >= configuration.MaxOngoingRequests)
                    {
                        // Wait for some requests to finish before scheduling more requests
                        continue;
                    }

                    CreateAndSendRequest(executionItem);
                    continue;
                }

                // Process ongoing
                var Result = ProcessOngoingRequest(executionItem);

                if (Result == HTTPExecutionQueueProcessingResult.NeedsSessionRefresh)
                {
                    //Bulk handle session refreshes at the end
                    ExecutionItemsNeedingRefresh.AddUnique(executionItem.RequestData.RequestId);
                    continue;
                }
                else if (Result == HTTPExecutionQueueProcessingResult.WaitForNextTick || Result == HTTPExecutionQueueProcessingResult.None)
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
                        if (executionItem == null)
                {
                            ExecutionItemsNeedingRefresh.Remove(executionItemId);
                                    continue;
                                }
                        else if (executionItem.IsWaitingForSessionRefresh)
                                {
                            // Already waiting for session refresh
                                    continue;
                                }
                        CurrentlyOngoingRequests.Remove(executionItem.RequestData.RequestId);
                        executionItem.IsWaitingForSessionRefresh = true;
                        executionItem.RequestData.TimesRetried++;
                        // Unsetting web request fields will make the execution queue retry it
                        executionItem.AbortRequest();
                        
                        StartCoroutine(RefreshSession(executionItem.RequestData.ForPlayerWithUlid, executionItemId, HandleSessionRefreshResult));
                    }
                }
            }

            if((HTTPExecutionQueue.Count - CurrentlyOngoingRequests.Count) > configuration.ChokeWarningThreshold)
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
                            CallListenersAndMarkDone(completedRequest, completedRequest.Response);
                        }
                        else if (completedRequest.WebRequest != null)
                        {
                            if (WebRequestSucceeded(completedRequest.WebRequest))
                            {
                                CallListenersAndMarkDone(completedRequest, LootLockerResponseFactory.Success<LootLockerResponse>((int)completedRequest.WebRequest.responseCode, completedRequest.WebRequest.downloadHandler.text, completedRequest.RequestData.ForPlayerWithUlid));
                            }
                            else
                            {
                                CallListenersAndMarkDone(completedRequest, ExtractFailureResponseFromExecutionItem(completedRequest));
                            }
                        }
                        else
                        {
                            CallListenersAndMarkDone(completedRequest, LootLockerResponseFactory.ClientError<LootLockerResponse>("Request completed but no response was present", completedRequest.RequestData.ForPlayerWithUlid));
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
            
            OngoingIdsToCleanUp.Clear();
            foreach (string OngoingId in CurrentlyOngoingRequests.Keys)
            {
                if (!HTTPExecutionQueue.TryGetValue(OngoingId, out var executionQueueItem) || executionQueueItem.Done)
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

            if (configuration.DenyIncomingRequestsWhenBackedUp && (HTTPExecutionQueue.Count - CurrentlyOngoingRequests.Count) > configuration.ChokeWarningThreshold)
            {
                // Execution queue is backed up, deny request
                request.CallListenersWithResult(LootLockerResponseFactory.ClientError<LootLockerResponse>("Request was denied because there are currently too many requests in queue", request.ForPlayerWithUlid, request.RequestStartTime));
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
                CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(executionItem.RequestData.Endpoint, RateLimiter.Get().GetSecondsLeftOfRateLimit(), executionItem.RequestData.ForPlayerWithUlid));
                return false;
            }

            executionItem.RequestStartTime = Time.time;

            executionItem.WebRequest = CreateWebRequest(executionItem.RequestData);
            if (executionItem.WebRequest == null)
            {
                CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.ClientError<LootLockerResponse>($"Call to {executionItem.RequestData.Endpoint} failed because Unity Web Request could not be created", executionItem.RequestData.ForPlayerWithUlid));
                return false;
            }

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

            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(executionItem.RequestData.ForPlayerWithUlid);

            if (ShouldRetryRequest(executionItem.WebRequest.responseCode, executionItem.RequestData.TimesRetried, playerData == null ? LL_AuthPlatforms.None : playerData.CurrentPlatform.Platform) && !(executionItem.WebRequest.responseCode == 401 && !IsAuthorizedRequest(executionItem)))
            {
                if (ShouldRefreshSession(executionItem.WebRequest.responseCode, playerData == null ? LL_AuthPlatforms.None : playerData.CurrentPlatform.Platform) && (CanRefreshUsingRefreshToken(executionItem.RequestData) || CanStartNewSessionUsingCachedAuthData(executionItem.RequestData.ForPlayerWithUlid)))
                {
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
                        CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.Success<LootLockerResponse>((int)executionItem.WebRequest.responseCode, executionItem.WebRequest.downloadHandler.text, executionItem.RequestData.ForPlayerWithUlid));
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
                                LootLockerResponse response = LootLockerResponseFactory.Failure<LootLockerResponse>((int)executionItem.WebRequest.responseCode, executionItem.WebRequest.downloadHandler.text, executionItem.RequestData.ForPlayerWithUlid);
                                response.errorData = ExtractErrorData(response);
                                if (response.errorData != null)
                                {
                                    response.errorData.retry_after_seconds = RetryAfterHeader;
                                }

                                CallListenersAndMarkDone(executionItem, response);
                                return;
                            }
                            executionItem.RetryAfter = DateTime.Now.AddSeconds(RetryAfterHeader);
                        }
                        else
                        {
                            // Incremental backoff
                            executionItem.RetryAfter = DateTime.Now.AddMilliseconds(configuration.InitialRetryWaitTimeInMs + (configuration.InitialRetryWaitTimeInMs * executionItem.RequestData.TimesRetried*configuration.IncrementalBackoffFactor));
                        }
                        executionItem.RequestData.TimesRetried++;

                        // Unsetting web request fields will make the execution queue retry it
                        executionItem.AbortRequest();

                        CurrentlyOngoingRequests.Remove(executionItem.RequestData.RequestId);
                        return;
                    }
                case HTTPExecutionQueueProcessingResult.Completed_TimedOut:
                    {
                        CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.RequestTimeOut<LootLockerResponse>(executionItem.RequestData.ForPlayerWithUlid));
                    }
                    break;
                case HTTPExecutionQueueProcessingResult.Completed_Failed:
                    {
                        LootLockerResponse response = ExtractFailureResponseFromExecutionItem(executionItem);
                        CallListenersAndMarkDone(executionItem, response);
                    }
                    break;
            }
        }

        private void CallListenersAndMarkDone(LootLockerHTTPExecutionQueueItem executionItem, LootLockerResponse response)
        {
            // Log HTTP request/response for the log viewer
            try
            {
                var requestData = executionItem.RequestData;
                var webRequest = executionItem.WebRequest;
                var logEntry = new LootLockerLogger.LootLockerHttpLogEntry
                {
                    Method = requestData.HTTPMethod.ToString(),
                    Url = requestData.FormattedURL,
                    RequestHeaders = requestData.ExtraHeaders,
                    RequestBody = requestData.Content?.dataType == LootLocker.LootLockerEnums.LootLockerHTTPRequestDataType.JSON ? ((LootLockerJsonBodyRequestContent)requestData.Content)?.jsonBody : null,
                    StatusCode = response.statusCode,
                    ResponseHeaders = webRequest?.GetResponseHeaders(),
                    Response = response,
                    DurationSeconds = Time.time - executionItem.RequestStartTime,
                    Timestamp = DateTime.Now
                };
                LootLockerLogger.LogHttpRequestResponse(logEntry);
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Failed to log HTTP request: {ex}", LootLockerLogger.LogLevel.Warning);
            }
            CurrentlyOngoingRequests.Remove(executionItem.RequestData.RequestId);
            executionItem.IsWaitingForSessionRefresh = false;
            executionItem.Done = true;
            response.requestContext = new LootLockerRequestContext(executionItem.RequestData.ForPlayerWithUlid, executionItem.RequestData.RequestStartTime);
            executionItem.Response = response;
            executionItem.RequestData.CallListenersWithResult(response);
            CompletedRequestIDs.Add(executionItem.RequestData.RequestId);
        }

        private IEnumerator RefreshSession(string refreshForPlayerUlid, string forExecutionItemId, Action<LootLockerSessionResponse, string, string> onSessionRefreshedCallback)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(refreshForPlayerUlid);
            if (playerData == null)
            {
                LootLockerLogger.Log($"No stored player data for player with ulid {refreshForPlayerUlid}. Can't refresh session.", LootLockerLogger.LogLevel.Warning);
                onSessionRefreshedCallback?.Invoke(LootLockerResponseFactory.Failure<LootLockerSessionResponse>(401, $"No stored player data for player with ulid {refreshForPlayerUlid}. Can't refresh session.", refreshForPlayerUlid), refreshForPlayerUlid, forExecutionItemId);
                yield break;
            }

            LootLockerSessionResponse newSessionResponse = null;
            bool callCompleted = false;
            switch (playerData.CurrentPlatform.Platform)
            {
                case LL_AuthPlatforms.Guest:
                    {
                        LootLockerSDKManager.StartGuestSessionForPlayer(refreshForPlayerUlid, response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case LL_AuthPlatforms.WhiteLabel:
                    {
                        LootLockerSDKManager.StartWhiteLabelSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        }, playerData.ULID);
                    }
                    break;
                case LL_AuthPlatforms.AppleGameCenter:
                    {
                        LootLockerSDKManager.RefreshAppleGameCenterSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        }, refreshForPlayerUlid);
                    }
                    break;
                case LL_AuthPlatforms.AppleSignIn:
                    {
                        LootLockerSDKManager.RefreshAppleSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        }, refreshForPlayerUlid);
                    }
                    break;
                case LL_AuthPlatforms.Epic:
                    {
                        LootLockerSDKManager.RefreshEpicSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        }, refreshForPlayerUlid);
                    }
                    break;
                case LL_AuthPlatforms.Google:
                    {
                        LootLockerSDKManager.RefreshGoogleSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        }, refreshForPlayerUlid);
                    }
                    break;
                case LL_AuthPlatforms.Remote:
                    {
                        LootLockerSDKManager.RefreshRemoteSession(response =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        }, refreshForPlayerUlid);
                    }
                    break;
                case LL_AuthPlatforms.PlayStationNetwork:
                case LL_AuthPlatforms.XboxOne:
                case LL_AuthPlatforms.AmazonLuna:
                    {
                        LootLockerSDKManager.StartAmazonLunaSession(playerData.Identifier, (response) =>
                        {
                            newSessionResponse = response;
                            callCompleted = true;
                        });
                    }
                    break;
                case LL_AuthPlatforms.NintendoSwitch:
                case LL_AuthPlatforms.Steam:
                    {
                        LootLockerLogger.Log($"Token has expired and token refresh is not supported for {playerData.CurrentPlatform.PlatformFriendlyString}", LootLockerLogger.LogLevel.Warning);
                        newSessionResponse =
                            LootLockerResponseFactory
                                .TokenExpiredError<LootLockerSessionResponse>(refreshForPlayerUlid);
                        callCompleted = true;
                        break;
                    }
                case LL_AuthPlatforms.None:
                default:
                    {
                        LootLockerLogger.Log($"Token refresh for platform {playerData.CurrentPlatform.PlatformFriendlyString} not supported", LootLockerLogger.LogLevel.Error);
                        newSessionResponse =
                            LootLockerResponseFactory
                                .TokenExpiredError<LootLockerSessionResponse>(refreshForPlayerUlid);
                        callCompleted = true;
                        break;
                    }
            }
            yield return new WaitUntil(() => callCompleted);
            onSessionRefreshedCallback?.Invoke(newSessionResponse, refreshForPlayerUlid, forExecutionItemId);
        }

        private void HandleSessionRefreshResult(LootLockerResponse newSessionResponse, string forPlayerWithUlid, string forExecutionItemId)
        {
            if (HTTPExecutionQueue.TryGetValue(forExecutionItemId, out var executionItem))
            {
                if (!executionItem.RequestData.ForPlayerWithUlid.Equals(forPlayerWithUlid))
                {
                    // This refresh callback was not for this user
                    LootLockerLogger.Log($"Session refresh callback ulid {forPlayerWithUlid} does not match the execution item ulid {executionItem.RequestData.ForPlayerWithUlid}. Ignoring.", LootLockerLogger.LogLevel.Error);
                    return;
                }
                var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(executionItem.RequestData.ForPlayerWithUlid);
                string tokenBeforeRefresh = executionItem.RequestData.ExtraHeaders["x-session-token"];
                string tokenAfterRefresh = playerData?.SessionToken;
                if (string.IsNullOrEmpty(tokenAfterRefresh) || tokenBeforeRefresh.Equals(playerData.SessionToken))
                {
                    // Session refresh failed so abort call chain
                    CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>(executionItem.RequestData.ForPlayerWithUlid));
                    return;
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
                    executionItem.RequestData.ExtraHeaders["x-session-token"] = tokenAfterRefresh;
                }

                // Mark request as ready for continuation
                ExecutionItemsNeedingRefresh.Remove(forExecutionItemId);
                executionItem.IsWaitingForSessionRefresh = false;
            }
        }

        #region Session Refresh Helper Methods

        private static bool ShouldRetryRequest(long statusCode, int timesRetried, LL_AuthPlatforms platform)
        {
            return (statusCode == 401 || statusCode == 403 || statusCode == 502 || statusCode == 500 || statusCode == 503) && LootLockerConfig.current.allowTokenRefresh && platform != LL_AuthPlatforms.Steam && timesRetried < configuration.MaxRetries;
        }

        private static bool ShouldRefreshSession(long statusCode, LL_AuthPlatforms platform)
        {
            return (statusCode == 401 || statusCode == 403) && LootLockerConfig.current.allowTokenRefresh && platform != LL_AuthPlatforms.Steam;
        }

        private static bool IsAuthorizedRequest(LootLockerHTTPExecutionQueueItem request)
        {
            return !string.IsNullOrEmpty(request.WebRequest?.GetRequestHeader("x-session-token")) || !string.IsNullOrEmpty(request.WebRequest?.GetRequestHeader("x-auth-token"));
        }

        private static bool CanRefreshUsingRefreshToken(LootLockerHTTPRequestData cachedRequest)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(cachedRequest.ForPlayerWithUlid);
            if (!LootLockerAuthPlatformSettings.PlatformsWithRefreshTokens.Contains(playerData == null ? LL_AuthPlatforms.None : playerData.CurrentPlatform.Platform))
            {
                return false;
            }
            // The failed request isn't a refresh session request but we have a refresh token stored, so try to refresh the session automatically before failing
            string json = cachedRequest.Content.dataType == LootLockerHTTPRequestDataType.JSON ? ((LootLockerJsonBodyRequestContent)cachedRequest.Content).jsonBody : null;
            return (string.IsNullOrEmpty(json) || !json.Contains("refresh_token")) && !string.IsNullOrEmpty(LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(cachedRequest.ForPlayerWithUlid)?.RefreshToken);
        }

        private static bool CanStartNewSessionUsingCachedAuthData(string forPlayerWithUlid)
        {
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
            if (playerData == null)
            {
                return false;
            }

            if (!LootLockerAuthPlatformSettings.PlatformsWithStoredAuthData.Contains(playerData.CurrentPlatform.Platform))
            {
                return false;
            }

            if (playerData.CurrentPlatform.Platform == LL_AuthPlatforms.WhiteLabel 
                && !string.IsNullOrEmpty(playerData.WhiteLabelEmail) 
                && !string.IsNullOrEmpty(playerData.WhiteLabelToken))
            {
                return true;
            }
           
           
            return !string.IsNullOrEmpty(playerData.Identifier);
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
            LootLockerResponse response = LootLockerResponseFactory.Failure<LootLockerResponse>((int)executionItem.WebRequest.responseCode, executionItem.WebRequest.downloadHandler.text, executionItem.RequestData.ForPlayerWithUlid);
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
                        request.CallListenersWithResult(LootLockerResponseFactory.ClientError<LootLockerResponse>("File request without file content", request.ForPlayerWithUlid, request.RequestStartTime));
                        return null;
                    }
                    webRequest = UnityWebRequest.Post(request.FormattedURL, ((LootLockerFileRequestContent)request.Content).fileForm);
                    if (request.HTTPMethod == LootLockerHTTPMethod.UPDATE_FILE)
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
                        byte[] bytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(((LootLockerJsonBodyRequestContent)request.Content).jsonBody) ? "{}" : ((LootLockerJsonBodyRequestContent)request.Content).jsonBody);
                        webRequest = UnityWebRequest.Put(request.FormattedURL, bytes);
                        webRequest.method = request.HTTPMethod.ToString();
                    }
                    break;
                default:
                    request.CallListenersWithResult(LootLockerResponseFactory.ClientError<LootLockerResponse>("Unsupported HTTP Method", request.ForPlayerWithUlid, request.RequestStartTime));
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

            if (certificateHandler != null)
            {
                webRequest.certificateHandler = certificateHandler;
            }

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
        #endregion
    }
}
#endif
