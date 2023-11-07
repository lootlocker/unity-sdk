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

        public static LootLockerServerApi GetInstance()
        {
            if (_instance == null)
            {
                _instance = new GameObject("LootLockerServerApi").AddComponent<LootLockerServerApi>();

                if (Application.isPlaying)
                    DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null)
            {
                DontDestroyOnLoad(_instance.gameObject);
            }
        }

        private static void DestroyInstance()
        {
            if (_instance == null) return;
            Destroy(_instance.gameObject);
            _instance = null;
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            DestroyInstance();
        }
#endif

        public static void SendRequest(LootLockerServerRequest request, Action<LootLockerResponse> OnServerResponse = null)
        {
            GetInstance()._SendRequest(request, OnServerResponse);
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

                    try
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Server Response: " + webRequest.responseCode + " " + request.endpoint + " completed in " + (Time.time - startTime).ToString("n4") + " secs.\nResponse: " + LootLockerObfuscator.ObfuscateJsonStringForLogging(webRequest.downloadHandler.text));
                    }
                    catch
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.httpMethod.ToString());
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.endpoint);
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(LootLockerObfuscator.ObfuscateJsonStringForLogging(webRequest.downloadHandler.text));
                    }

                    if (WebRequestSucceeded(webRequest))
                    {
                        LootLockerResponse response = new LootLockerResponse();
                        response.statusCode = (int)webRequest.responseCode;
                        response.success = true;
                        response.text = webRequest.downloadHandler.text;
                        response.errorData = null;
                        OnServerResponse?.Invoke(response);
                    }
                    else
                    {
                        if ((webRequest.responseCode == 401 || webRequest.responseCode == 403) && LootLockerConfig.current.allowTokenRefresh && CurrentPlatform.Get() != Platforms.Steam && _tries < MaxRetries)
                        {
                            _tries++;
                            RefreshTokenAndCompleteCall(request, (value) => { _tries = 0; OnServerResponse?.Invoke(value); });
                        }
                        else
                        {
                            _tries = 0;
                            LootLockerResponse response = new LootLockerResponse();
                            response.statusCode = (int)webRequest.responseCode;
                            response.success = false;
                            response.text = webRequest.downloadHandler.text;

                            LootLockerErrorData errorData = LootLockerJson.DeserializeObject<LootLockerErrorData>(webRequest.downloadHandler.text);
                            // Check if the error uses the "old" error style, not the "new" (https://docs.lootlocker.com/reference/error-codes)
                            if (errorData == null || string.IsNullOrEmpty(errorData.code))
                            {
                                errorData = new LootLockerErrorData();
                                switch (webRequest.responseCode)
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
                                        errorData.message = "Method Not Allowed -- The selected http method is invalid for this resource.";
                                        errorData.code = "method_not_allowed";
                                        break;
                                    case 406:
                                        errorData.message = "Not Acceptable -- Purchasing is disabled.";
                                        errorData.code = "not_acceptable";
                                        break;
                                    case 409:
                                        errorData.message = "Conflict -- Your state is most likely not aligned with the servers.";
                                        errorData.code = "conflict";
                                        break;
                                    case 429:
                                        errorData.message = "Too Many Requests -- You're being limited for sending too many requests too quickly.";
                                        errorData.code = "too_many_requests";
                                        break;
                                    case 500:
                                        errorData.message = "Internal Server Error -- We had a problem with our server. Try again later.";
                                        errorData.code = "internal_server_error";
                                        break;
                                    case 503:
                                        errorData.message = "Service Unavailable -- We're either offline for maintenance, or an error that should be solvable by calling again later was triggered.";
                                        errorData.code = "service_unavailable";
                                        break;
                                    default:
                                        errorData.message = "Unknown error.";
                                        break;
                                }
                                errorData.message += " " + LootLockerObfuscator.ObfuscateJsonStringForLogging(webRequest.downloadHandler.text);
                            }
                            response.errorData = errorData;
                            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(response.errorData.message + (!string.IsNullOrEmpty(response.errorData.doc_url) ? " -- " + response.errorData.doc_url : ""));
                            OnServerResponse?.Invoke(response);
                        }
                    }

                }
            }
        }
#region Private Methods

        private static string GetUrl(LootLocker.LootLockerEnums.LootLockerCallerRole callerRole)
        {
            switch (callerRole)
            {
                case LootLocker.LootLockerEnums.LootLockerCallerRole.Admin:
                    return LootLockerConfig.current.adminUrl;
                case LootLocker.LootLockerEnums.LootLockerCallerRole.User:
                    return LootLockerConfig.current.userUrl;
                case LootLocker.LootLockerEnums.LootLockerCallerRole.Player:
                    return LootLockerConfig.current.playerUrl;
                case LootLocker.LootLockerEnums.LootLockerCallerRole.Base:
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

        private static Dictionary<string, string> baseHeaders = new Dictionary<string, string>() {

            { "Accept", "application/json; charset=UTF-8" },
            { "Content-Type", "application/json; charset=UTF-8" },
            { "Access-Control-Allow-Credentials", "true" },
            { "Access-Control-Allow-Headers", "Accept, X-Access-Token, X-Application-Name, X-Request-Sent-Time" },
            { "Access-Control-Allow-Methods", "GET, POST, DELETE, PUT, OPTIONS, HEAD" },
            { "Access-Control-Allow-Origin", "*" },
            { "User-Instance-Identifier", System.Guid.NewGuid().ToString() }
        };

        private void RefreshTokenAndCompleteCall(LootLockerServerRequest cacheServerRequest, Action<LootLockerResponse> OnServerResponse)
        {
            switch (CurrentPlatform.Get())
            {
                case Platforms.Guest:
                    {
                        LootLockerSDKManager.StartGuestSession(response =>
                        {
                            CompleteCall(cacheServerRequest, OnServerResponse, response);
                        });
                        return;
                    }
                case Platforms.WhiteLabel:
                    {
                        LootLockerSDKManager.StartWhiteLabelSession(response =>
                        {
                            CompleteCall(cacheServerRequest, OnServerResponse, response);
                        });
                        return;
                    }
                case Platforms.AppleGameCenter:
                case Platforms.AppleSignIn:
                case Platforms.Epic:
                case Platforms.Google:
                    {
                        // The failed request isn't a refresh session request but we have a refresh token stored, so try to refresh the session automatically before failing
                        if (!cacheServerRequest.jsonPayload.Contains("refresh_token") && !string.IsNullOrEmpty(LootLockerConfig.current.refreshToken))
                        {
                            switch (CurrentPlatform.Get())
                            {
                                case Platforms.AppleGameCenter:
                                    LootLockerSDKManager.RefreshAppleGameCenterSession(response =>
                                    {
                                        CompleteCall(cacheServerRequest, OnServerResponse, response);
                                    });
                                    return;
                                case Platforms.AppleSignIn:
                                    LootLockerSDKManager.RefreshAppleSession(response =>
                                    {
                                        CompleteCall(cacheServerRequest, OnServerResponse, response);
                                    });
                                    return;
                                case Platforms.Epic:
                                    LootLockerSDKManager.RefreshEpicSession(response =>
                                    {
                                        CompleteCall(cacheServerRequest, OnServerResponse, response);
                                    });
                                    return;
                                case Platforms.Google:
                                    LootLockerSDKManager.RefreshGoogleSession(response =>
                                    {
                                        CompleteCall(cacheServerRequest, OnServerResponse, response);
                                    });
                                    return;
                            }
                        }
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired, please refresh it");
                        OnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
                        return;
                    }
                case Platforms.NintendoSwitch:
                case Platforms.Steam:
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)($"Token has expired and token refresh is not supported for {CurrentPlatform.GetFriendlyString()}");
                        OnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
                        return;
                    }
                case Platforms.PlayStationNetwork:
                case Platforms.XboxOne:
                case Platforms.AmazonLuna:
                    {
                        var sessionRequest = new LootLockerSessionRequest(LootLockerConfig.current.deviceID);
                        LootLockerAPIManager.Session(sessionRequest, (response) =>
                        {
                            CompleteCall(cacheServerRequest, OnServerResponse, response);
                        });
                        break;
                    }
                case Platforms.None:
                default:
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)($"Platform {CurrentPlatform.GetFriendlyString()} not supported");
                        OnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>($"Platform {CurrentPlatform.GetFriendlyString()} not supported", 401));
                        return;
                    }
            }
        }

        private void CompleteCall(LootLockerServerRequest newcacheServerRequest, Action<LootLockerResponse> newOnServerResponse, LootLockerSessionResponse response)
        {
            if (response.success)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("x-session-token", LootLockerConfig.current.token);
                newcacheServerRequest.extraHeaders = headers;
                if (newcacheServerRequest.retryCount < 4)
                {
                    _SendRequest(newcacheServerRequest, newOnServerResponse);
                    newcacheServerRequest.retryCount++;
                }
                else
                {
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Info)("Session refresh failed");
                    newOnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
                }
            }
            else
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Info)("Session refresh failed");
                LootLockerResponse res = new LootLockerResponse();
                newOnServerResponse?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("Token Expired", 401));
            }
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

            if (baseHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in baseHeaders)
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

