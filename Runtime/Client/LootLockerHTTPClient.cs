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
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace LootLocker.LootLockerEnums
{
    public enum LootLockerHttpRequestDataType
    {
        EMPTY = 0,
        JSON = 1,
        WWW_FORM = 2,
        FILE = 3,
    }
}

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
            LootLockerHTTPRequestContent content = null;
            if (httpMethod == LootLockerHTTPMethod.GET || httpMethod == LootLockerHTTPMethod.HEAD || httpMethod == LootLockerHTTPMethod.OPTIONS)
            {
                content = new LootLockerHTTPRequestContent();
                if (!string.IsNullOrEmpty(body))
                {
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("Payloads can not be sent in GET, HEAD, or OPTIONS requests. Attempted to send a body to: " + httpMethod.ToString() + " " + endPoint);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(body))
                {
                    body = "{}";
                }
                content = new LootLockerJsonBodyRequestContent(body);
            }

            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                onComplete?.Invoke(LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit()));
                return;
            }

            MakeAndSendRequest(content, endPoint, httpMethod, onComplete, useAuthToken, callerRole, additionalHeaders);
        }

        public static void UploadFile(string endPoint, LootLockerHTTPMethod httpMethod, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            if (file.Length == 0)
            {
#if UNITY_EDITOR
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)("File content is empty, not allowed.");
#endif
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>("File content is empty, not allowed."));
                return;
            }
            LootLockerHTTPRequestContent content = (LootLockerHTTPMethod.PUT == httpMethod && file != null) ?
                    new LootLockerWWWFormRequestContent(file, fileName, fileContentType)
                    : new LootLockerFileRequestContent(file, fileName, body);
            MakeAndSendRequest(content, endPoint, httpMethod, onComplete, useAuthToken, callerRole, additionalHeaders);
        }
        
        public static void UploadFile(EndPointClass endPoint, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            UploadFile(endPoint.endPoint, endPoint.httpMethod, file, fileName, fileContentType, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken, callerRole, additionalHeaders);
        }

        private static void MakeAndSendRequest(LootLockerHTTPRequestContent content, string endPoint, LootLockerHTTPMethod httpMethod, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                onComplete?.Invoke(LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit()));
                return;
            }

#if UNITY_EDITOR
            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Debug)("Caller Type: " + callerRole);
#endif
            Dictionary<string, string> headers = InitializeHeadersWithSessionToken(callerRole, useAuthToken);

            if (LootLockerConfig.current != null)
                headers.Add(LootLockerConfig.current.dateVersion.key, LootLockerConfig.current.dateVersion.value);

            if (additionalHeaders != null)
            {
                foreach (var additionalHeader in additionalHeaders)
                {
                    headers.Add(additionalHeader.Key, additionalHeader.Value);
                }
            }

            LootLockerHTTPRequestData requestData = new LootLockerHTTPRequestData
            {
                TimesRetried = 0,
                Endpoint = endPoint,
                HTTPMethod = httpMethod,
                ExtraHeaders = headers != null && headers.Count == 0 ? null : headers, // Force extra headers to null if empty dictionary was supplied
                QueryParams = null,
                CallerRole = callerRole,
                Content = content
            };

            LootLockerHTTPClient.Get().SendRequest(requestData, (response) => { onComplete?.Invoke(response); });
        }

        private static Dictionary<string, string> InitializeHeadersWithSessionToken(LootLockerCallerRole callerRole, bool useAuthToken)
        {
            var headers = new Dictionary<string, string>();
            if (useAuthToken)
            {
                if (callerRole == LootLockerCallerRole.Admin)
                {
#if UNITY_EDITOR
                    if (!string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
                    {
                        headers.Add("x-auth-token", LootLockerConfig.current.adminToken);
                    }
#endif
                }
                else if (!string.IsNullOrEmpty(LootLockerConfig.current.token))
                {
                    headers.Add("x-session-token", LootLockerConfig.current.token);
                }
            }
            return headers;
        }

        #endregion
    }

    public class LootLockerHTTPClient : MonoBehaviour
    {
        private const int MaxRetries = 3;

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

        void Update()
        {
        }

        public void SendRequest(LootLockerHTTPRequestData request, Action<LootLockerResponse> OnServerResponse = null)
        {
            StartCoroutine(coroutine(request, OnServerResponse));
        }

        protected IEnumerator coroutine(LootLockerHTTPRequestData request, Action<LootLockerResponse> OnServerResponse = null)
        {
            //Always wait 1 frame before starting any request to the server to make sure the requester code has exited the main thread.
            yield return null;

            //Build the URL that we will hit based on the specified endpoint, query params, etc
            string url = BuildUrl(request.Endpoint, request.QueryParams, request.CallerRole);
#if UNITY_EDITOR
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("ServerRequest " + request.HTTPMethod + " URL: " + url);
#endif
            using (UnityWebRequest webRequest = CreateWebRequest(url, request))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                float startTime = Time.time;
                bool timedOut = false;

                UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = webRequest.SendWebRequest();
                yield return new WaitUntil(() =>
                {
                    if (unityWebRequestAsyncOperation == null)
                    {
                        return true;
                    }

                    timedOut = !unityWebRequestAsyncOperation.isDone && Time.time - startTime >= LootLockerConfig.current.clientSideRequestTimeOut;

                    return timedOut || unityWebRequestAsyncOperation.isDone;

                });

                if (!webRequest.isDone && timedOut)
                {
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("Exceeded maxTimeOut waiting for a response from " + request.HTTPMethod + " " + url);
                    OnServerResponse?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>(request.Endpoint + " timed out."));
                    yield break;
                }


                LogResponse(request, webRequest.responseCode, webRequest.downloadHandler.text, startTime, webRequest.error);

                if (WebRequestSucceeded(webRequest))
                {
                    OnServerResponse?.Invoke(new LootLockerResponse
                    {
                        statusCode = (int)webRequest.responseCode,
                        success = true,
                        text = webRequest.downloadHandler.text,
                        errorData = null
                    });
                    yield break;
                }

                if (ShouldRetryRequest(webRequest.responseCode, request.TimesRetried))
                {
                    request.TimesRetried++;
                    RefreshTokenAndCompleteCall(request, (value) => { request.TimesRetried = 0; OnServerResponse?.Invoke(value); });
                    yield break;
                }

                request.TimesRetried = 0;
                LootLockerResponse response = new LootLockerResponse
                {
                    statusCode = (int)webRequest.responseCode,
                    success = false,
                    text = webRequest.downloadHandler.text
                };

                try
                {
                    response.errorData = LootLockerJson.DeserializeObject<LootLockerErrorData>(webRequest.downloadHandler.text);
                }
                catch (Exception)
                {
                    if (webRequest.downloadHandler.text.StartsWith("<"))
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("JSON Starts with <, info: \n    statusCode: " + response.statusCode + "\n    body: " + response.text);
                    }
                    response.errorData = null;
                }
                // Error data was not parseable, populate with what we know
                if (response.errorData == null)
                {
                    response.errorData = new LootLockerErrorData((int)webRequest.responseCode, webRequest.downloadHandler.text);
                }

                string RetryAfterHeader = webRequest.GetResponseHeader("Retry-After");
                if (!string.IsNullOrEmpty(RetryAfterHeader))
                {
                    response.errorData.retry_after_seconds = Int32.Parse(RetryAfterHeader);
                }

                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(response.errorData.ToString());
                OnServerResponse?.Invoke(response);
            }
        }

        #region Private Methods

        private static bool ShouldRetryRequest(long statusCode, int timesRetried)
        {
            return (statusCode == 401 || statusCode == 403 || statusCode == 502 || statusCode == 500 || statusCode == 503) && LootLockerConfig.current.allowTokenRefresh && CurrentPlatform.Get() != Platforms.Steam && timesRetried < MaxRetries;
        }

        private static void LogResponse(LootLockerHTTPRequestData request, long statusCode, string responseBody, float startTime, string unityWebRequestError)
        {
            if (statusCode == 0 && string.IsNullOrEmpty(responseBody) && !string.IsNullOrEmpty(unityWebRequestError))
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Unity Web request failed, request to " +
                    request.Endpoint + " completed in " +
                    (Time.time - startTime).ToString("n4") +
                    " secs.\nWeb Request Error: " + unityWebRequestError);
                return;
            }

            try
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Server Response: " +
                    statusCode + " " +
                    request.Endpoint + " completed in " +
                    (Time.time - startTime).ToString("n4") +
                    " secs.\nResponse: " +
                    LootLockerObfuscator
                        .ObfuscateJsonStringForLogging(responseBody));
            }
            catch
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.HTTPMethod.ToString());
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.Endpoint);
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(LootLockerObfuscator.ObfuscateJsonStringForLogging(responseBody));
            }
        }

        private static string GetUrl(LootLockerCallerRole callerRole)
        {
            switch (callerRole)
            {
                case LootLockerCallerRole.Admin:
                    return LootLockerConfig.current.adminUrl;
                case LootLockerCallerRole.User:
                    return LootLockerConfig.current.userUrl;
                case LootLockerCallerRole.Player:
                    return LootLockerConfig.current.playerUrl;
                case LootLockerCallerRole.Base:
                    return LootLockerConfig.current.baseUrl;
                default:
                    return LootLockerConfig.current.url;
            }
        }

        private bool WebRequestSucceeded(UnityWebRequest webRequest)
        {
            return !
#if UNITY_2020_1_OR_NEWER
            (webRequest.result == UnityWebRequest.Result.ProtocolError || webRequest.result == UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(webRequest.error));
#else
            (webRequest.isHttpError || webRequest.isNetworkError || !string.IsNullOrEmpty(webRequest.error));
#endif
        }

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

        private void RefreshTokenAndCompleteCall(LootLockerHTTPRequestData cachedRequest, Action<LootLockerResponse> onComplete)
        {
            switch (CurrentPlatform.Get())
            {
                case Platforms.Guest:
                    {
                        LootLockerSDKManager.StartGuestSession(response =>
                        {
                            CompleteCall(cachedRequest, response, onComplete);
                        });
                        return;
                    }
                case Platforms.WhiteLabel:
                    {
                        LootLockerSDKManager.StartWhiteLabelSession(response =>
                        {
                            CompleteCall(cachedRequest, response, onComplete);
                        });
                        return;
                    }
                case Platforms.AppleGameCenter:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshAppleGameCenterSession(response =>
                            {
                                CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.AppleSignIn:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshAppleSession(response =>
                            {
                                CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.Epic:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshEpicSession(response =>
                            {
                                CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.Google:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshGoogleSession(response =>
                            {
                                CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.Remote:
                    {
                        if (ShouldRefreshUsingRefreshToken(cachedRequest))
                        {
                            LootLockerSDKManager.RefreshRemoteSession(response =>
                            {
                                CompleteCall(cachedRequest, response, onComplete);
                            });
                            return;
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.NintendoSwitch:
                case Platforms.Steam:
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired and token refresh is not supported for {CurrentPlatform.GetFriendlyString()}");
                        onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                        return;
                    }
                case Platforms.PlayStationNetwork:
                case Platforms.XboxOne:
                case Platforms.AmazonLuna:
                    {
                        var sessionRequest = new LootLockerSessionRequest(LootLockerConfig.current.deviceID);
                        LootLockerAPIManager.Session(sessionRequest, (response) =>
                        {
                            CompleteCall(cachedRequest, response, onComplete);
                        });
                        return;
                    }
                case Platforms.None:
                default:
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"Token refresh for platform {CurrentPlatform.GetFriendlyString()} not supported");
                        onComplete?.Invoke(LootLockerResponseFactory.NetworkError<LootLockerResponse>($"Token refresh for platform {CurrentPlatform.GetFriendlyString()} not supported", 401));
                        return;
                    }
            }
        }

        private static bool ShouldRefreshUsingRefreshToken(LootLockerHTTPRequestData cachedRequest)
        {
            // The failed request isn't a refresh session request but we have a refresh token stored, so try to refresh the session automatically before failing
            string json = cachedRequest.Content.dataType == LootLockerHttpRequestDataType.JSON ? ((LootLockerJsonBodyRequestContent)cachedRequest.Content).jsonBody : null;
            return (string.IsNullOrEmpty(json) || !json.Contains("refresh_token")) && !string.IsNullOrEmpty(LootLockerConfig.current.refreshToken);
        }

        private void CompleteCall(LootLockerHTTPRequestData cachedRequest, LootLockerSessionResponse sessionRefreshResponse, Action<LootLockerResponse> onComplete)
        {
            if (!sessionRefreshResponse.success)
            {
                LootLockerLogger.GetForLogLevel()("Session refresh failed");
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                return;
            }

            if (cachedRequest.TimesRetried >= 4)
            {
                LootLockerLogger.GetForLogLevel()("Session refresh failed");
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                return;
            }

            cachedRequest.ExtraHeaders["x-session-token"] = LootLockerConfig.current.token;
            SendRequest(cachedRequest, onComplete);
            cachedRequest.TimesRetried++;
        }

        private UnityWebRequest CreateWebRequest(string url, LootLockerHTTPRequestData request)
        {
            UnityWebRequest webRequest = null;
            switch (request.HTTPMethod)
            {
                case LootLockerHTTPMethod.UPLOAD_FILE:
                    if(request.Content.dataType == LootLockerHttpRequestDataType.FILE)
                    {
                        webRequest = UnityWebRequest.Post(url, ((LootLockerFileRequestContent)request.Content).fileForm);
                    }
                    break;
                case LootLockerHTTPMethod.UPDATE_FILE:
                    if (request.Content.dataType == LootLockerHttpRequestDataType.FILE)
                    {
                        webRequest = UnityWebRequest.Post(url, ((LootLockerFileRequestContent)request.Content).fileForm);
                    }
                    // Workaround for UnityWebRequest with PUT HTTP verb not having form fields
                    webRequest.method = UnityWebRequest.kHttpVerbPUT;
                    break;
                case LootLockerHTTPMethod.POST:
                case LootLockerHTTPMethod.PATCH:
                // Defaults are fine for PUT
                case LootLockerHTTPMethod.PUT:

                    if (request.Content.dataType == LootLockerHttpRequestDataType.WWW_FORM)
                    {
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
                        webRequest = new UnityWebRequest();
                        webRequest.SetRequestHeader("Content-Type", "multipart/form-data; boundary=--");
                        webRequest.uri = new Uri(url);
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)(url);//the url is wrong in some cases
                        webRequest.uploadHandler = new UploadHandlerRaw(formSections);
                        webRequest.uploadHandler.contentType = contentType;
                        webRequest.useHttpContinue = false;

                        // webRequest.method = "POST";
                        webRequest.method = UnityWebRequest.kHttpVerbPOST;
                    }
                    else
                    {
#if UNITY_EDITOR
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("REQUEST BODY = " + LootLockerObfuscator.ObfuscateJsonStringForLogging(((LootLockerJsonBodyRequestContent)request.Content).jsonBody));
#endif
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(string.IsNullOrEmpty(((LootLockerJsonBodyRequestContent)request.Content).jsonBody) ? "{}" : ((LootLockerJsonBodyRequestContent)request.Content).jsonBody);
                        webRequest = UnityWebRequest.Put(url, bytes);
                        webRequest.method = request.HTTPMethod.ToString();
                    }

                    break;

                case LootLockerHTTPMethod.OPTIONS:
                case LootLockerHTTPMethod.HEAD:
                case LootLockerHTTPMethod.GET:
                    // Defaults are fine for GET
                    webRequest = UnityWebRequest.Get(url);
                    webRequest.method = request.HTTPMethod.ToString();
                    break;

                case LootLockerHTTPMethod.DELETE:
                    // Defaults are fine for DELETE
                    webRequest = UnityWebRequest.Delete(url);
                    break;
                default:
                    throw new System.Exception("Invalid HTTP Method");
            }

            if (BaseHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in BaseHeaders)
                {
                    if (pair.Key == "Content-Type" && request.Content.dataType != LootLockerHttpRequestDataType.JSON) continue;

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

            return webRequest;
        }

        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams = null, LootLockerCallerRole callerRole = LootLockerCallerRole.User)
        {
            string ep = endpoint.StartsWith("/") ? endpoint.Trim() : "/" + endpoint.Trim();

            return (GetUrl(callerRole) + ep + GetQueryParameterStringFromDictionary(queryParams)).Trim();
        }

        public string GetQueryParameterStringFromDictionary(Dictionary<string, string> queryDict)
        {
            if (queryDict == null || queryDict.Count == 0) return string.Empty;

            string query = "?";

            foreach (KeyValuePair<string, string> pair in queryDict)
            {
                if (query.Length > 1)
                    query += "&";

                query += pair.Key + "=" + pair.Value;
            }

            return query;
        }
        #endregion
    }
}
#endif
