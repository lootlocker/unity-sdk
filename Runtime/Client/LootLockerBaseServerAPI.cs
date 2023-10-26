using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Net;
#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using LLlibs.ZeroDepJson;
#endif
using LootLocker.Requests;

namespace LootLocker.LootLockerEnums
{
    public enum LootLockerCallerRole { User, Admin, Player, Base };
}

namespace LootLocker
{
    public abstract class LootLockerBaseServerAPI
    {
        public static LootLockerBaseServerAPI I;
        public static void Init(LootLockerBaseServerAPI childType)
        {
            I = childType;
        }

        protected Func<IEnumerator, System.Object> StartCoroutine;
        int maxRetry = 3;
        int tries = 0;
        /// <summary>
        /// This would be something like "www.mydomain.com" or "api.mydomain.com". But you could also directly supply the IPv4 address of the server to speed the calls up a little bit by bypassing DNS Lookup
        /// </summary>
        //public static string SERVER_URL = "http://localhost:5051/api";
        public static string SERVER_URL;
        static LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User;
        public void SwitchURL(LootLocker.LootLockerEnums.LootLockerCallerRole mainCallerRole)
        {
            switch (mainCallerRole)
            {
                case LootLocker.LootLockerEnums.LootLockerCallerRole.Admin:
                    SERVER_URL = LootLockerConfig.current.adminUrl;
                    break;
                case LootLocker.LootLockerEnums.LootLockerCallerRole.User:
                    SERVER_URL = LootLockerConfig.current.userUrl;
                    break;
                case LootLocker.LootLockerEnums.LootLockerCallerRole.Player:
                    SERVER_URL = LootLockerConfig.current.playerUrl;
                    break;
                case LootLocker.LootLockerEnums.LootLockerCallerRole.Base:
                    SERVER_URL = LootLockerConfig.current.baseUrl;
                    break;
                default:
                    SERVER_URL = LootLockerConfig.current.url;
                    break;
            }
            callerRole = mainCallerRole;
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

        public void SendRequest(LootLockerServerRequest request, System.Action<LootLockerResponse> OnServerResponse = null)
        {
            StartCoroutine(coroutine());
            IEnumerator coroutine()
            {
                //Always wait 1 frame before starting any request to the server to make sure the requesters code has exited the main thread.
                yield return null;

                //Build the URL that we will hit based on the specified endpoint, query params, etc
                string url = BuildURL(request.endpoint, request.queryParams);
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
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Server Response: " + webRequest.responseCode + " " + request.endpoint + " completed in " + (Time.time - startTime).ToString("n4") + " secs.\nResponse: " + ObfuscateJsonStringForLogging(webRequest.downloadHandler.text));
                    }
                    catch
                    {
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.httpMethod.ToString());
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(request.endpoint);
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(ObfuscateJsonStringForLogging(webRequest.downloadHandler.text));
                    }

                    if(WebRequestSucceeded(webRequest))
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
                        if ((webRequest.responseCode == 401 || webRequest.responseCode == 403) && LootLockerConfig.current.allowTokenRefresh && CurrentPlatform.Get() != Platforms.Steam && tries < maxRetry) 
                        {
                            tries++;
                            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("Refreshing Token, since we could not find one. If you do not want this please turn off in the LootLocker config settings");
                            RefreshTokenAndCompleteCall(request,(value)=> { tries = 0; OnServerResponse?.Invoke(value); });
                        }
                        else
                        {
                            tries = 0;
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
                                errorData.message += " " + ObfuscateJsonStringForLogging(webRequest.downloadHandler.text);
                            }
                            response.errorData = errorData;
                            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)(response.errorData.message + (!string.IsNullOrEmpty(response.errorData.doc_url) ? " -- " + response.errorData.doc_url : ""));
                            OnServerResponse?.Invoke(response);
                        }
                    }

                }
            }
        }

        protected static Dictionary<string, string> baseHeaders = new Dictionary<string, string>() {

            { "Accept", "application/json; charset=UTF-8" },
            { "Content-Type", "application/json; charset=UTF-8" },
            { "Access-Control-Allow-Credentials", "true" },
            { "Access-Control-Allow-Headers", "Accept, X-Access-Token, X-Application-Name, X-Request-Sent-Time" },
            { "Access-Control-Allow-Methods", "GET, POST, DELETE, PUT, OPTIONS, HEAD" },
            { "Access-Control-Allow-Origin", "*" },
            { "User-Instance-Identifier", System.Guid.NewGuid().ToString() }
        };

        protected event System.Action<ServerError> OnError;

        protected struct ServerError
        {
            public HttpStatusCode status { get; set; }
            public string text { get; set; }
        }

        protected void BroadcastError(ServerError error)
        {
            OnError?.Invoke(error);
        }

        protected abstract void RefreshTokenAndCompleteCall(LootLockerServerRequest cacheServerRequest, System.Action<LootLockerResponse> OnServerResponse);

        protected void DownloadTexture2D(string url, System.Action<Texture2D> OnComplete = null)
        {
            StartCoroutine(DoDownloadTexture2D(url, OnComplete));
        }

        protected IEnumerator DoDownloadTexture2D(string url, System.Action<Texture2D> OnComplete = null)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {

                www.SetRequestHeader("Access-Control-Allow-Headers", "Accept, X-Access-Token, X-Application-Name, X-Request-Sent-Time");
                www.SetRequestHeader("Access-Control-Allow-Credentials", "true");
                www.SetRequestHeader("Access-Control-Allow-Methods", "GET, POST, DELETE, PUT, OPTIONS, HEAD");
                www.SetRequestHeader("Access-Control-Allow-Origin", "*");

                yield return www.SendWebRequest();

                Texture2D texture = DownloadHandlerTexture.GetContent(www);

                if (texture == null)
                {
                    LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)("Texture download failed for: " + url);
                }

                OnComplete?.Invoke(texture);
            }
        }

        UnityWebRequest CreateWebRequest(string url, LootLockerServerRequest request)
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
                        LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("REQUEST BODY = " + ObfuscateJsonStringForLogging(json));
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

        private struct ObfuscationDetails
        {
            public string key { get; set; }
            public char replacementChar { get; set; }
            public int visibleCharsFromBeginning { get; set; }
            public int visibleCharsFromEnd { get; set; }
            public bool hideCharactersForShortStrings { get; set; }

            public ObfuscationDetails(string key, char replacementChar = '*', int visibleCharsFromBeginning = 3, int visibleCharsFromEnd = 3, bool hideCharactersForShortStrings = true)
            {
                this.key = key;
                this.replacementChar = replacementChar;
                this.visibleCharsFromBeginning = visibleCharsFromBeginning;
                this.visibleCharsFromEnd = visibleCharsFromEnd;
                this.hideCharactersForShortStrings = hideCharactersForShortStrings;
            }
        }

        static readonly List<ObfuscationDetails> FieldsToObfuscate = new List<ObfuscationDetails>
        {
            new ObfuscationDetails("game_key", '*', 4, 3, false),
            new ObfuscationDetails("email"),
            new ObfuscationDetails("password", '*', 0, 0),
            new ObfuscationDetails("domain_key"),
            new ObfuscationDetails("session_token"),
            new ObfuscationDetails("token")
        };

        private static string ObfuscateJsonStringForLogging(string json)
        {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }
            
            JObject jsonObject;
            try
            {
                jsonObject = JObject.Parse(json);
            }
            catch (JsonReaderException)
            {
                return json;
            }
            ;
            if (jsonObject.HasValues)
            {
                foreach (ObfuscationDetails obfuscationInfo in FieldsToObfuscate)
                {
                    string valueToObfuscate;
                    try
                    {
                        JToken jsonValue;
                        jsonObject.TryGetValue(obfuscationInfo.key, StringComparison.Ordinal, out jsonValue);
                        if (jsonValue == null || (jsonValue.Type != JTokenType.String && jsonValue.Type != JTokenType.Integer))
                            continue;
                        valueToObfuscate = jsonValue.ToString();
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(valueToObfuscate))
                        continue;

                    if (valueToObfuscate.Equals("null", StringComparison.Ordinal))
                        continue;

                    int replaceFrom = 0;
                    int replaceTo = valueToObfuscate.Length;

                    // Deal with short strings
                    if (valueToObfuscate.Length <= obfuscationInfo.visibleCharsFromBeginning + obfuscationInfo.visibleCharsFromEnd)
                    {
                        if (!obfuscationInfo.hideCharactersForShortStrings) // Hide nothing, else hide everything
                            continue;
                    }
                    // Replace in
                    else
                    {
                        replaceFrom += obfuscationInfo.visibleCharsFromBeginning;
                        replaceTo -= obfuscationInfo.visibleCharsFromEnd;
                    }

                    StringBuilder replacement = new StringBuilder();
                    replacement.Append(obfuscationInfo.replacementChar, replaceTo - replaceFrom);
                    StringBuilder obfuscatedValue = new StringBuilder(valueToObfuscate);
                    obfuscatedValue.Remove(replaceFrom, replacement.Length);
                    obfuscatedValue.Insert(replaceFrom, replacement.ToString());
                    jsonObject[obfuscationInfo.key] = obfuscatedValue.ToString();
                }
            }

            return LootLockerJson.SerializeObject(jsonObject);
            
#else //LOOTLOCKER_USE_NEWTONSOFTJSON
            if (string.IsNullOrEmpty(json) || json.Equals("{}"))
            {
                return json;
            }

            Dictionary<string, object> jsonObject = null;
            try
            {
                jsonObject = Json.Deserialize(json) as Dictionary<string, object>;
            }
            catch (JsonException)
            {
                return json;
            }

            if (jsonObject != null && jsonObject.Count > 0)
            {
                foreach (ObfuscationDetails obfuscationInfo in FieldsToObfuscate)
                {
                    string valueToObfuscate;
                    try
                    {
                        if (!jsonObject.ContainsKey(obfuscationInfo.key))
                        {
                            continue;
                        }

                        valueToObfuscate = Json.Serialize(jsonObject[obfuscationInfo.key]);
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(valueToObfuscate))
                        continue;

                    if (valueToObfuscate.Equals("null", StringComparison.Ordinal))
                        continue;

                    int replaceFrom = 0;
                    int replaceTo = valueToObfuscate.Length;

                    // Deal with short strings
                    if (valueToObfuscate.Length <= obfuscationInfo.visibleCharsFromBeginning + obfuscationInfo.visibleCharsFromEnd)
                    {
                        if (!obfuscationInfo.hideCharactersForShortStrings) // Hide nothing, else hide everything
                            continue;
                    }
                    // Replace in
                    else
                    {
                        replaceFrom += obfuscationInfo.visibleCharsFromBeginning;
                        replaceTo -= obfuscationInfo.visibleCharsFromEnd;
                    }

                    StringBuilder replacement = new StringBuilder();
                    replacement.Append(obfuscationInfo.replacementChar, replaceTo - replaceFrom);
                    StringBuilder obfuscatedValue = new StringBuilder(valueToObfuscate);
                    obfuscatedValue.Remove(replaceFrom, replacement.Length);
                    obfuscatedValue.Insert(replaceFrom, replacement.ToString());
                    jsonObject[obfuscationInfo.key] = obfuscatedValue.ToString();
                }
            }

            return LootLockerJson.SerializeObject(jsonObject);
#endif //LOOTLOCKER_USE_NEWTONSOFTJSON
        }

        string BuildURL(string endpoint, Dictionary<string, string> queryParams = null)
        {
            string ep = endpoint.StartsWith("/") ? endpoint.Trim() : "/" + endpoint.Trim();

            return (SERVER_URL + ep + GetQueryStringFromDictionary(queryParams)).Trim();
        }

        string GetQueryStringFromDictionary(Dictionary<string, string> queryDict)
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
    }
}

