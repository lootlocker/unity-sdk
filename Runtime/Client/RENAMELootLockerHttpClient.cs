#if !LOOTLOCKER_USE_LEGACY_HTTP
using System.Collections.Generic;
using UnityEngine;
using System;
using LootLocker.LootLockerEnums;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using LootLocker.Requests;
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
        public string endpoint { get; set; }
        public LootLockerHTTPMethod httpMethod { get; set; }
        public string jsonPayload { get; set; }
        public byte[] upload { get; set; }
        public string uploadName { get; set; }
        public string uploadType { get; set; }
        public LootLockerCallerRole callerRole { get; set; }
        public WWWForm form { get; set; }

        /// <summary>
        /// Leave this null if you don't need custom headers
        /// </summary>
        public Dictionary<string, string> extraHeaders;

        /// <summary>
        /// Query parameters to append to the end of the request URI
        /// Example: If you include a dictionary with a key of "page" and a value of "42" (as a string) then the url would become "https://mydomain.com/endpoint?page=42"
        /// </summary>
        public Dictionary<string, string> queryParams;

        public int retryCount { get; set; }

        #region Make ServerRequest and call send (3 functions)

        public static void CallAPI(string endPoint, LootLockerHTTPMethod httpMethod, string body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, Dictionary<string, string> additionalHeaders = null)
        {
            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                onComplete?.Invoke(LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit()));
                return;
            }

#if UNITY_EDITOR
            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Debug)("Caller Type: " + callerRole);
#endif

            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (useAuthToken)
            {
                headers = new Dictionary<string, string>();
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

            if (LootLockerConfig.current != null)
                headers.Add(LootLockerConfig.current.dateVersion.key, LootLockerConfig.current.dateVersion.value);

            if (additionalHeaders != null)
            {
                foreach (var additionalHeader in additionalHeaders)
                {
                    headers.Add(additionalHeader.Key, additionalHeader.Value);
                }
            }

            new LootLockerServerRequest(endPoint, httpMethod, body, headers, callerRole: callerRole).Send((response) => { onComplete?.Invoke(response); });
        }

        public static void UploadFile(string endPoint, LootLockerHTTPMethod httpMethod, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                onComplete?.Invoke(LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit()));
                return;
            }
            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (file.Length == 0)
            {
#if UNITY_EDITOR
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)("File content is empty, not allowed.");
#endif
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>("File content is empty, not allowed."));
                return;
            }
            if (useAuthToken)
            {
                headers = new Dictionary<string, string>();

                headers.Add(callerRole == LootLockerCallerRole.Admin ? "x-auth-token" : "x-session-token", LootLockerConfig.current.token);
            }
            
            new LootLockerServerRequest(endPoint, httpMethod, file, fileName, fileContentType, body, headers, callerRole: callerRole).Send((response) => { onComplete?.Invoke(response); });
        }
        
        public static void UploadFile(EndPointClass endPoint, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null,
            bool useAuthToken = true, LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            UploadFile(endPoint.endPoint, endPoint.httpMethod, file, fileName, fileContentType, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken, callerRole);
        }

        #endregion

        #region ServerRequest constructor

        public LootLockerServerRequest(string endpoint, LootLockerHTTPMethod httpMethod = LootLockerHTTPMethod.GET, byte[] upload = null, string uploadName = null, string uploadType = null, Dictionary<string, string> body = null,
            Dictionary<string, string> extraHeaders = null, LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.upload = upload;
            this.uploadName = uploadName;
            this.uploadType = uploadType;
            this.jsonPayload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = null;
            this.callerRole = callerRole;
            this.form = new WWWForm();

            foreach (var kvp in body)
            {
                this.form.AddField(kvp.Key, kvp.Value);
            }

            this.form.AddBinaryData("file", upload, uploadName);

            bool isNonPayloadMethod = (this.httpMethod == LootLockerHTTPMethod.GET || this.httpMethod == LootLockerHTTPMethod.HEAD || this.httpMethod == LootLockerHTTPMethod.OPTIONS);
        }

        public LootLockerServerRequest(string endpoint, LootLockerHTTPMethod httpMethod = LootLockerHTTPMethod.GET, string payload = null, Dictionary<string, string> extraHeaders = null, Dictionary<string, string> queryParams = null,
            LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.jsonPayload = payload;
            this.upload = null;
            this.uploadName = null;
            this.uploadType = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = queryParams != null && queryParams.Count == 0 ? null : queryParams;
            this.callerRole = callerRole;
            bool isNonPayloadMethod = (this.httpMethod == LootLockerHTTPMethod.GET || this.httpMethod == LootLockerHTTPMethod.HEAD || this.httpMethod == LootLockerHTTPMethod.OPTIONS);
            this.form = null;
            if (!string.IsNullOrEmpty(jsonPayload) && isNonPayloadMethod)
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }

        #endregion

        /// <summary>
        /// just debug and call ServerAPI.SendRequest which takes the current ServerRequest and pass this response
        /// </summary>
        public void Send(System.Action<LootLockerResponse> OnServerResponse)
        {
            LootLockerServerApi.SendRequest(this, (response) => { OnServerResponse?.Invoke(response); });
        }
    }

    public class LootLockerServerApi : MonoBehaviour
    {
        private static bool _bTaggedGameObjects = false;
        private static LootLockerServerApi _instance;
        private static int _instanceId = 0;
        private const int MaxRetries = 3;
        private int _tries;
        public GameObject HostingGameObject = null;

        public static void Instantiate()
        {
            if (_instance == null)
            {
                var gameObject = new GameObject("LootLockerServerApi");
                if (_bTaggedGameObjects)
                {
                    gameObject.tag = "LootLockerServerApiGameObject";
                }

                _instance = gameObject.AddComponent<LootLockerServerApi>();
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
            LootLockerServerApi[] serverApis = GameObject.FindObjectsByType<LootLockerServerApi>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            LootLockerServerApi[] serverApis = GameObject.FindObjectsOfType<LootLockerServerApi>();
#endif
            foreach (LootLockerServerApi serverApi in serverApis)
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

        void Update()
        {
        }

        public static void SendRequest(LootLockerServerRequest request, Action<LootLockerResponse> OnServerResponse = null)
        {
            if (_instance == null)
            {
                Instantiate();
            }

            _instance._SendRequest(request, OnServerResponse);
        }

        private void _SendRequest(LootLockerServerRequest request, Action<LootLockerResponse> OnServerResponse = null)
        {
            StartCoroutine(coroutine());
            IEnumerator coroutine()
            {
                //Always wait 1 frame before starting any request to the server to make sure the requester code has exited the main thread.
                yield return null;

                //Build the URL that we will hit based on the specified endpoint, query params, etc
                string url = BuildUrl(request.endpoint, request.queryParams, request.callerRole);
#if UNITY_EDITOR
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("ServerRequest " + request.httpMethod + " URL: " + url);
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
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("Exceeded maxTimeOut waiting for a response from " + request.httpMethod + " " + url);
                        OnServerResponse?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>(request.endpoint + " timed out."));
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

                    if (ShouldRetryRequest(webRequest.responseCode, _tries))
                    {
                        _tries++;
                        RefreshTokenAndCompleteCall(request, (value) => { _tries = 0; OnServerResponse?.Invoke(value); });
                        yield break;
                    }

                    _tries = 0;
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
        }

        #region Private Methods

        private static bool ShouldRetryRequest(long statusCode, int timesRetried)
        {
            return (statusCode == 401 || statusCode == 403 || statusCode == 502 || statusCode == 500 || statusCode == 503) && LootLockerConfig.current.allowTokenRefresh && CurrentPlatform.Get() != Platforms.Steam && timesRetried < MaxRetries;
        }

        private static void LogResponse(LootLockerServerRequest request, long statusCode, string responseBody, float startTime, string unityWebRequestError)
        {
            if (statusCode == 0 && string.IsNullOrEmpty(responseBody) && !string.IsNullOrEmpty(unityWebRequestError))
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Unity Web request failed, request to " +
                    request.endpoint + " completed in " +
                    (Time.time - startTime).ToString("n4") +
                    " secs.\nWeb Request Error: " + unityWebRequestError);
                return;
            }

            try
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Server Response: " +
                    statusCode + " " +
                    request.endpoint + " completed in " +
                    (Time.time - startTime).ToString("n4") +
                    " secs.\nResponse: " +
                    LootLockerObfuscator
                        .ObfuscateJsonStringForLogging(responseBody));
            }
            catch
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.httpMethod.ToString());
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.endpoint);
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

        private void RefreshTokenAndCompleteCall(LootLockerServerRequest cachedRequest, Action<LootLockerResponse> onComplete)
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

        private static bool ShouldRefreshUsingRefreshToken(LootLockerServerRequest cachedRequest)
        {
            // The failed request isn't a refresh session request but we have a refresh token stored, so try to refresh the session automatically before failing
            return (string.IsNullOrEmpty(cachedRequest.jsonPayload) || !cachedRequest.jsonPayload.Contains("refresh_token")) && !string.IsNullOrEmpty(LootLockerConfig.current.refreshToken);
        }

        private void CompleteCall(LootLockerServerRequest cachedRequest, LootLockerSessionResponse sessionRefreshResponse, Action<LootLockerResponse> onComplete)
        {
            if (!sessionRefreshResponse.success)
            {
                LootLockerLogger.GetForLogLevel()("Session refresh failed");
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                return;
            }

            if (cachedRequest.retryCount >= 4)
            {
                LootLockerLogger.GetForLogLevel()("Session refresh failed");
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                return;
            }

            cachedRequest.extraHeaders["x-session-token"] = LootLockerConfig.current.token;
            SendRequest(cachedRequest, onComplete);
            cachedRequest.retryCount++;
        }

        private UnityWebRequest CreateWebRequest(string url, LootLockerServerRequest request)
        {
            UnityWebRequest webRequest;
            switch (request.httpMethod)
            {
                case LootLockerHTTPMethod.UPLOAD_FILE:
                    webRequest = UnityWebRequest.Post(url, request.form);
                    break;
                case LootLockerHTTPMethod.UPDATE_FILE:
                    // Workaround for UnityWebRequest with PUT HTTP verb not having form fields
                    webRequest = UnityWebRequest.Post(url, request.form);
                    webRequest.method = UnityWebRequest.kHttpVerbPUT;
                    break;
                case LootLockerHTTPMethod.POST:
                case LootLockerHTTPMethod.PATCH:
                // Defaults are fine for PUT
                case LootLockerHTTPMethod.PUT:

                    if (request.upload != null)
                    {
                        List<IMultipartFormSection> form = new List<IMultipartFormSection>
                        {
                            new MultipartFormFileSection(request.uploadName, request.upload, System.DateTime.Now.ToString(), request.uploadType)
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
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("REQUEST BODY = " + LootLockerObfuscator.ObfuscateJsonStringForLogging(request.jsonPayload));
#endif
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(string.IsNullOrEmpty(request.jsonPayload) ? "{}" : request.jsonPayload);
                        webRequest = UnityWebRequest.Put(url, bytes);
                        webRequest.method = request.httpMethod.ToString();
                    }

                    break;

                case LootLockerHTTPMethod.OPTIONS:
                case LootLockerHTTPMethod.HEAD:
                case LootLockerHTTPMethod.GET:
                    // Defaults are fine for GET
                    webRequest = UnityWebRequest.Get(url);
                    webRequest.method = request.httpMethod.ToString();
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
                    if (pair.Key == "Content-Type" && request.upload != null) continue;

                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            if (!string.IsNullOrEmpty(LootLockerConfig.current?.sdk_version))
            {
                webRequest.SetRequestHeader("LL-SDK-Version", LootLockerConfig.current.sdk_version);
            }

            if (request.extraHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in request.extraHeaders)
                {
                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            return webRequest;
        }

        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams = null, LootLockerCallerRole callerRole = LootLockerCallerRole.User)
        {
            string ep = endpoint.StartsWith("/") ? endpoint.Trim() : "/" + endpoint.Trim();

            return (GetUrl(callerRole) + ep + GetQueryStringFromDictionary(queryParams)).Trim();
        }

        private string GetQueryStringFromDictionary(Dictionary<string, string> queryDict)
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
