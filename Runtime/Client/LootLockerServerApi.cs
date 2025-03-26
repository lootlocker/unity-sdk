#if !LOOTLOCKER_BETA_HTTP_QUEUE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Text;
using LootLocker.LootLockerEnums;
using UnityEditor;
using LootLocker.Requests;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace LootLocker
{
    public class LootLockerHTTPClient : MonoBehaviour
    {
        private static bool _bTaggedGameObjects = false;
        private static LootLockerHTTPClient _instance;
        private static int _instanceId = 0;
        private const int MaxRetries = 3;
        private int _tries;
        public GameObject HostingGameObject = null;

        public static void Instantiate()
        {
            if (_instance == null)
            {
                var gameObject = new GameObject("LootLockerHTTPClient");
                if (_bTaggedGameObjects)
                {
                    gameObject.tag = "LootLockerHTTPClientGameObject";
                }

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
        private static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
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
                LootLockerLogger.Log("ServerRequest " + request.httpMethod + " URL: " + url, LootLockerLogger.LogLevel.Verbose);
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
                        LootLockerLogger.Log("Exceeded maxTimeOut waiting for a response from " + request.httpMethod + " " + url, LootLockerLogger.LogLevel.Warning);
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
                            LootLockerLogger.Log("JSON Starts with <, info: \n    statusCode: " + response.statusCode + "\n    body: " + response.text, LootLockerLogger.LogLevel.Warning);
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

                    LootLockerLogger.Log(response.errorData?.ToString(), LootLockerLogger.LogLevel.Error);
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
                LootLockerLogger.Log("Unity Web request failed, request to " +
                    request.endpoint + " completed in " +
                    (Time.time - startTime).ToString("n4") +
                    " secs.\nWeb Request Error: " + unityWebRequestError, LootLockerLogger.LogLevel.Verbose);
                return;
            }

            try
            {
                LootLockerLogger.Log("Server Response: " +
                    statusCode + " " +
                    request.endpoint + " completed in " +
                    (Time.time - startTime).ToString("n4") +
                    " secs.\nResponse: " +
                    LootLockerObfuscator
                        .ObfuscateJsonStringForLogging(responseBody), LootLockerLogger.LogLevel.Verbose);
            }
            catch
            {
                LootLockerLogger.Log(request.httpMethod.ToString(), LootLockerLogger.LogLevel.Error);
                LootLockerLogger.Log(request.endpoint, LootLockerLogger.LogLevel.Error);
                LootLockerLogger.Log(LootLockerObfuscator.ObfuscateJsonStringForLogging(responseBody), LootLockerLogger.LogLevel.Error);
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
                    LootLockerLogger.Log($"Token has expired, please refresh it", LootLockerLogger.LogLevel.Warning);
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
                    LootLockerLogger.Log($"Token has expired, please refresh it", LootLockerLogger.LogLevel.Warning);
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
                    LootLockerLogger.Log($"Token has expired, please refresh it", LootLockerLogger.LogLevel.Warning);
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
                    LootLockerLogger.Log($"Token has expired, please refresh it", LootLockerLogger.LogLevel.Warning);
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
                    LootLockerLogger.Log($"Token has expired, please refresh it", LootLockerLogger.LogLevel.Warning);
                    onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                    return;
                }
                case Platforms.NintendoSwitch:
                case Platforms.Steam:
                {
                    LootLockerLogger.Log($"Token has expired and token refresh is not supported for {CurrentPlatform.GetFriendlyString()}", LootLockerLogger.LogLevel.Warning);
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
                    LootLockerLogger.Log($"Token refresh for platform {CurrentPlatform.GetFriendlyString()} not supported", LootLockerLogger.LogLevel.Error);
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
                LootLockerLogger.Log("Session refresh failed");
                onComplete?.Invoke(LootLockerResponseFactory.TokenExpiredError<LootLockerResponse>());
                return;
            }

            if (cachedRequest.retryCount >= 4)
            {
                LootLockerLogger.Log("Session refresh failed");
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

                    if (request.payload == null && request.upload != null)
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
                        //LootLockerLogger.Log(url); // The url is wrong in some cases
                        webRequest.uploadHandler = new UploadHandlerRaw(formSections);
                        webRequest.uploadHandler.contentType = contentType;
                        webRequest.useHttpContinue = false;

                        // webRequest.method = "POST";
                        webRequest.method = UnityWebRequest.kHttpVerbPOST;
                    }
                    else
                    {
                        string json = (request.payload != null && request.payload.Count > 0) ? LootLockerJson.SerializeObject(request.payload) : request.jsonPayload;
                        LootLockerLogger.Log("REQUEST BODY = " + LootLockerObfuscator.ObfuscateJsonStringForLogging(json), LootLockerLogger.LogLevel.Verbose);
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(string.IsNullOrEmpty(json) ? "{}" : json);
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
