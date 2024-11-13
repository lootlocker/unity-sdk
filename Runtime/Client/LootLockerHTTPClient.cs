#if !LOOTLOCKER_USE_LEGACY_HTTP
using System.Collections.Generic;
using UnityEngine;
using System;
using LootLocker.LootLockerEnums;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using LootLocker.Requests;
using LootLocker.HTTP;
using UnityEditor.PackageManager.Requests;
using System.Threading;
using System.Net;
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
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("Payloads can not be sent in GET, HEAD, or OPTIONS requests. Attempted to send a body to: " + httpMethod.ToString() + " " + endPoint);
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
#if UNITY_EDITOR
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)("File content is empty, not allowed.");
#endif
                onComplete(LootLockerResponseFactory.ClientError<LootLockerResponse>("File content is empty, not allowed."));
                return;
            }
            LootLockerHTTPRequestContent content = (LootLockerHTTPMethod.PUT == httpMethod) ?
                    new LootLockerWWWFormRequestContent(file, fileName, fileContentType)
                    : new LootLockerFileRequestContent(file, fileName, body);

            LootLockerHTTPClient.Get().ScheduleRequest(LootLockerHTTPRequestData.MakeFileRequest(endPoint, httpMethod, file, fileName, fileContentType, body, onComplete, useAuthToken, callerRole, additionalHeaders, null));
        }
        
        public static void UploadFile(EndPointClass endPoint, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            UploadFile(endPoint.endPoint, endPoint.httpMethod, file, fileName, fileContentType, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken, callerRole, additionalHeaders);
        }

        #endregion
    }

    public class LootLockerHTTPClient : MonoBehaviour
    {
        #region Configuration
        private const int MaxRetries = 3;

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

        void Update()
        {
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
                    CreateAndSendRequest(executionItem);
                    continue;
                }

                // Process ongoing
                ProcessOngoingRequest(executionItem); //TODO: Handle result

            }
        }

        private void LateUpdate()
        {
            // Do Cleanup
            foreach (var CompletedRequestID in CompletedRequestIDs)
            {
                if(HTTPExecutionQueue.ContainsKey(CompletedRequestID))
                {
                    HTTPExecutionQueue.TryGetValue(CompletedRequestID, out var completedRequest);

                    if(!completedRequest.Done)
                    {
                        continue;
                    }

                    if(!completedRequest.RequestData.HaveListenersBeenInvoked)
                    {
                        completedRequest.RequestData.CallListenersWithResult(completedRequest.Response);
                    }

                    HTTPExecutionQueue.Remove(CompletedRequestID);
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
        }

        public void ScheduleRequest(LootLockerHTTPRequestData request)
        {
            StartCoroutine(_ScheduleRequest(request));
            //StartCoroutine(SendRequestCoroutine(request));
        }

        private IEnumerator _ScheduleRequest(LootLockerHTTPRequestData request)
        {
            //Always wait 1 frame before starting any request to the server to make sure the requester code has exited the main thread.
            yield return null;

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

#if UNITY_EDITOR
            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("ServerRequest " + executionItem.RequestData.HTTPMethod + " URL: " + executionItem.RequestData.FormattedURL);
#endif

            UnityWebRequest webRequest = CreateWebRequest(executionItem.RequestData);
            if (webRequest == null)
            {
                CallListenersAndMarkDone(executionItem, LootLockerResponseFactory.ClientError<LootLockerResponse>($"Call to {executionItem.RequestData.Endpoint} failed because Unity Web Request could not be created"));
                return false;
            }

            executionItem.RequestStartTime = Time.time;

            executionItem.WebRequest = webRequest;
            executionItem.AsyncOperation = executionItem.WebRequest.SendWebRequest();
            return true;
        }

        private bool ProcessOngoingRequest(LootLockerHTTPExecutionQueueItem executionItem)
        {
            //TODO: Give Results
            if (executionItem.AsyncOperation == null)
            {
                return true;
            }

            bool timedOut = !executionItem.AsyncOperation.isDone && (Time.time - executionItem.RequestStartTime) >= LootLockerConfig.current.clientSideRequestTimeOut;
            if(timedOut)
            {
                return true;
            }

            // Not timed out and not done, nothing to do
            if(!executionItem.AsyncOperation.isDone)
            {
                return false;
            }

            /* ############################################
               Request is done, from now on handling result
             //############################################*/

            LogResponse(executionItem.RequestData, executionItem.WebRequest.responseCode, executionItem.WebRequest.downloadHandler.text, executionItem.RequestStartTime, executionItem.WebRequest.error);

            // ##### Web request success handling ####
            if (WebRequestSucceeded(executionItem.WebRequest))
            {
                CallListenersAndMarkDone(executionItem, new LootLockerResponse
                {
                    statusCode = (int)executionItem.WebRequest.responseCode,
                    success = true,
                    text = executionItem.WebRequest.downloadHandler.text,
                    errorData = null,
                    EventId = Guid.NewGuid().ToString()
                });
                return true;
            }

            if (ShouldRetryRequest(executionItem.WebRequest.responseCode, executionItem.RequestData.TimesRetried))
            {
                // ##### Web request failed but should be retried ####
                executionItem.RequestData.TimesRetried++;

                if (ShouldRefreshSession(executionItem.WebRequest.responseCode) && (CanRefreshUsingRefreshToken(executionItem.RequestData) || CanStartNewSessionUsingCachedData()))
                {
                    executionItem.IsWaitingForSessionRefresh = true;
                    string tokenBeforeRefresh = LootLockerConfig.current.token;
                    StartCoroutine(RefreshSession(newSessionResponse =>
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
                    }));
                }

                // Unsetting web request fields will make the execution queue retry it
                executionItem.AsyncOperation = null;
                executionItem.WebRequest = null;
                return false;
            }

            // ##### Web request did not succeed and should not be retried = failed ####
            LootLockerResponse response = new LootLockerResponse
            {
                statusCode = (int)executionItem.WebRequest.responseCode,
                success = false,
                text = executionItem.WebRequest.downloadHandler.text
            };

            try
            {
                response.errorData = LootLockerJson.DeserializeObject<LootLockerErrorData>(executionItem.WebRequest.downloadHandler.text);
            }
            catch (Exception)
            {
                if (executionItem.WebRequest.downloadHandler.text.StartsWith("<"))
                {
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("JSON Starts with <, info: \n    statusCode: " + response.statusCode + "\n    body: " + response.text);
                }
                response.errorData = null;
            }
            // Error data was not parseable, populate with what we know
            if (response.errorData == null)
            {
                response.errorData = new LootLockerErrorData((int)executionItem.WebRequest.responseCode, executionItem.WebRequest.downloadHandler.text);
            }

            string RetryAfterHeader = executionItem.WebRequest.GetResponseHeader("Retry-After");
            if (!string.IsNullOrEmpty(RetryAfterHeader))
            {
                response.errorData.retry_after_seconds = int.Parse(RetryAfterHeader);
            }

            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(response.errorData.ToString());
            CallListenersAndMarkDone(executionItem, response);
            return true;
        }

        private void CallListenersAndMarkDone(LootLockerHTTPExecutionQueueItem executionItem, LootLockerResponse response)
        {
            executionItem.RequestData.CallListenersWithResult(response);
            CompletedRequestIDs.Add(executionItem.RequestData.RequestId);
            executionItem.Done = true;
            executionItem.IsWaitingForSessionRefresh = false;
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
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired and token refresh is not supported for {CurrentPlatform.GetFriendlyString()}");
                        yield break;
                    }
                case Platforms.None:
                default:
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"Token refresh for platform {CurrentPlatform.GetFriendlyString()} not supported");
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
#if UNITY_EDITOR
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("REQUEST BODY = " + LootLockerObfuscator.ObfuscateJsonStringForLogging(((LootLockerJsonBodyRequestContent)request.Content).jsonBody));
#endif
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
        private static void LogResponse(LootLockerHTTPRequestData request, long statusCode, string responseBody, float startTime, string unityWebRequestError)
        {
            if (statusCode == 0 && string.IsNullOrEmpty(responseBody) && !string.IsNullOrEmpty(unityWebRequestError))
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Unity Web request failed, request to " +
                    request.FormattedURL + " completed in " +
                    (Time.time - startTime).ToString("n4") +
                    " secs.\nWeb Request Error: " + unityWebRequestError);
                return;
            }

            try
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Server Response: " +
                    statusCode + " " +
                    request.FormattedURL + " completed in " +
                    (Time.time - startTime).ToString("n4") +
                    " secs.\nResponse: " +
                    LootLockerObfuscator
                        .ObfuscateJsonStringForLogging(responseBody));
            }
            catch
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.HTTPMethod.ToString());
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.FormattedURL);
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(LootLockerObfuscator.ObfuscateJsonStringForLogging(responseBody));
            }
        }
        #endregion
    }
}
#endif
