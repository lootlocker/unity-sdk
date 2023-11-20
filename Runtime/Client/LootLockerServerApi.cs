using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using LootLocker.LootLockerEnums;
using UnityEditor;
using LootLocker.Requests;

namespace LootLocker.LootLockerEnums
{
    public enum LootLockerCallerRole { User, Admin, Player, Base };
}

namespace LootLocker
{
    public class LootLockerServerApi : MonoBehaviour
    {
        private static LootLockerServerApi _instance;
        private const int MaxRetries = 3;
        private int _tries;

        public static void Instantiate()
        {
            if (_instance == null)
            {
                _instance = new GameObject("LootLockerServerApi").AddComponent<LootLockerServerApi>();

                if (Application.isPlaying)
                    DontDestroyOnLoad(_instance.gameObject);
            }
        }

        public static void Reset()
        {
            if (_instance == null) return;
#if UNITY_EDITOR
            DestroyImmediate(_instance.gameObject);
#else
            Destroy(_instance.gameObject);
#endif
            _instance = null;
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            Reset();
        }
#endif

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
                        OnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>(request.endpoint + " Timed out.", 408));
                        yield break;
                    }

                    LogResponse(request, webRequest.responseCode, webRequest.downloadHandler.text, startTime);

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

                    LootLockerErrorData errorData =
                        LootLockerJson.DeserializeObject<LootLockerErrorData>(webRequest.downloadHandler.text);
                    if (ErrorIsPreErrorCodes(errorData))
                    {
                        errorData = ExpandErrorInformation(webRequest.responseCode, webRequest.downloadHandler.text);
                    }

                    response.errorData = errorData;
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(response.errorData.message +
                        (!string.IsNullOrEmpty(response.errorData.doc_url) ? " -- " + response.errorData.doc_url : ""));
                    OnServerResponse?.Invoke(response);

                }
            }
        }

#region Private Methods

        private static LootLockerErrorData ExpandErrorInformation(long statusCode, string responseBody)
        {
            var errorData = new LootLockerErrorData();
            switch (statusCode)
            {
                case 400:
                    errorData.message = "Bad Request -- Your request has an error.";
                    errorData.code = "bad_request";
                    break;
                case 401:
                    errorData.message = "Unauthorized -- Your session_token is invalid.";
                    errorData.code = "unauthorized";
                    break;
                case 402:
                    errorData.message = "Payment Required -- Payment failed. Insufficient funds, etc.";
                    errorData.code = "payment_required";
                    break;
                case 403:
                    errorData.message = "Forbidden -- You do not have access to this resource.";
                    errorData.code = "forbidden";
                    break;
                case 404:
                    errorData.message = "Not Found -- The requested resource could not be found.";
                    errorData.code = "not_found";
                    break;
                case 405:
                    errorData.message =
                        "Method Not Allowed -- The selected http method is invalid for this resource.";
                    errorData.code = "method_not_allowed";
                    break;
                case 406:
                    errorData.message = "Not Acceptable -- Purchasing is disabled.";
                    errorData.code = "not_acceptable";
                    break;
                case 409:
                    errorData.message =
                        "Conflict -- Your state is most likely not aligned with the servers.";
                    errorData.code = "conflict";
                    break;
                case 429:
                    errorData.message =
                        "Too Many Requests -- You're being limited for sending too many requests too quickly.";
                    errorData.code = "too_many_requests";
                    break;
                case 500:
                    errorData.message =
                        "Internal Server Error -- We had a problem with our server. Try again later.";
                    errorData.code = "internal_server_error";
                    break;
                case 503:
                    errorData.message =
                        "Service Unavailable -- We're either offline for maintenance, or an error that should be solvable by calling again later was triggered.";
                    errorData.code = "service_unavailable";
                    break;
                default:
                    errorData.message = "Unknown error.";
                    break;
            }

            errorData.message +=
                " " + LootLockerObfuscator.ObfuscateJsonStringForLogging(responseBody);
            return errorData;
        }

        private static bool ErrorIsPreErrorCodes(LootLockerErrorData errorData)
        {
            // Check if the error uses the "old" error style, not the "new" (https://docs.lootlocker.com/reference/error-codes)
            return errorData == null || string.IsNullOrEmpty(errorData.code);
        }

        private static bool ShouldRetryRequest(long statusCode, int timesRetried)
        {
            return (statusCode == 401 || statusCode == 403) && LootLockerConfig.current.allowTokenRefresh && CurrentPlatform.Get() != Platforms.Steam && timesRetried < MaxRetries;
        }

        private static void LogResponse(LootLockerServerRequest request, long statusCode, string responseBody, float startTime)
        {
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
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(
                    LootLockerObfuscator.ObfuscateJsonStringForLogging(responseBody));
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
            { "User-Instance-Identifier", System.Guid.NewGuid().ToString() }
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
                    onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
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
                    onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
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
                    onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
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
                    onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
                    return;
                }
                case Platforms.NintendoSwitch:
                case Platforms.Steam:
                {
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired and token refresh is not supported for {CurrentPlatform.GetFriendlyString()}");
                    onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
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
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"Platform {CurrentPlatform.GetFriendlyString()} not supported");
                    onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>($"Platform {CurrentPlatform.GetFriendlyString()} not supported", 401));
                    return;
                }
            }
        }

        private static bool ShouldRefreshUsingRefreshToken(LootLockerServerRequest cachedRequest)
        {
            // The failed request isn't a refresh session request but we have a refresh token stored, so try to refresh the session automatically before failing
            return !cachedRequest.jsonPayload.Contains("refresh_token") && !string.IsNullOrEmpty(LootLockerConfig.current.refreshToken);
        }

        private void CompleteCall(LootLockerServerRequest cachedRequest, LootLockerSessionResponse sessionRefreshResponse, Action<LootLockerResponse> onComplete)
        {
            if (!sessionRefreshResponse.success)
            {
                LootLockerLogger.GetForLogLevel()("Session refresh failed");
                onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
                return;
            }

            if (cachedRequest.retryCount >= 4)
            {
                LootLockerLogger.GetForLogLevel()("Session refresh failed");
                onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
                return;
            }

            cachedRequest.extraHeaders["x-session-token"] = LootLockerConfig.current.token;
            _SendRequest(cachedRequest, onComplete);
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
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)(url);//the url is wrong in some cases
                        webRequest.uploadHandler = new UploadHandlerRaw(formSections);
                        webRequest.uploadHandler.contentType = contentType;
                        webRequest.useHttpContinue = false;

                        // webRequest.method = "POST";
                        webRequest.method = UnityWebRequest.kHttpVerbPOST;
                    }
                    else
                    {
                        string json = (request.payload != null && request.payload.Count > 0) ? LootLockerJson.SerializeObject(request.payload) : request.jsonPayload;
#if UNITY_EDITOR
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("REQUEST BODY = " + LootLockerObfuscator.ObfuscateJsonStringForLogging(json));
#endif
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
                webRequest.SetRequestHeader("SDK-Version", LootLockerConfig.current.sdk_version);
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

