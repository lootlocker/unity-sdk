using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Net;
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
                LootLockerSDKManager.DebugMessage("ServerRequest URL: " + url);
#endif

                using (UnityWebRequest webRequest = CreateWebRequest(url, request))
                {
                    webRequest.downloadHandler = new DownloadHandlerBuffer();

                    float startTime = Time.time;
                    float maxTimeOut = 5f;

                    yield return webRequest.SendWebRequest();
                    while (!webRequest.isDone)
                    {
                        yield return null;
                        if (Time.time - startTime >= maxTimeOut)
                        {
                            LootLockerSDKManager.DebugMessage("ERROR: Exceeded maxTimeOut waiting for a response from " + request.httpMethod.ToString() + " " + url);
                            OnServerResponse?.Invoke(new LootLockerResponse() { hasError = true, statusCode = 408, Error = "{\"error\": \"" + request.endpoint + " Timed out.\"}" });
                            yield break;
                        }
                    }

                    if (!webRequest.isDone)
                    {
                        OnServerResponse?.Invoke(new LootLockerResponse() { hasError = true, statusCode = 408, Error = "{\"error\": \"" + request.endpoint + " Timed out.\"}" });
                        yield break;
                    }

                    try
                    {
                        LootLockerSDKManager.DebugMessage("Server Response: " + request.httpMethod + " " + request.endpoint + " completed in " + (Time.time - startTime).ToString("n4") + " secs.\nResponse: " + webRequest.downloadHandler.text);
                    }
                    catch
                    {
                        LootLockerSDKManager.DebugMessage(request.httpMethod.ToString(), true);
                        LootLockerSDKManager.DebugMessage(request.endpoint, true);
                        LootLockerSDKManager.DebugMessage(webRequest.downloadHandler.text, true);
                    }

                    LootLockerResponse response = new LootLockerResponse();
                    response.statusCode = (int)webRequest.responseCode;
#if UNITY_2020_1_OR_NEWER
                    if (webRequest.result == UnityWebRequest.Result.ProtocolError || webRequest.result == UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(webRequest.error))
#else
                    if (webRequest.isHttpError || webRequest.isNetworkError || !string.IsNullOrEmpty(webRequest.error))
#endif

                    {
                        switch (webRequest.responseCode)
                        {
                            case 200:
                                response.Error = "";
                                break;
                            case 400:
                                response.Error = "Bad Request -- Your request has an error";
                                break;
                            case 402:
                                response.Error = "Payment Required -- Payment failed. Insufficient funds, etc.";
                                break;
                            case 401:
                                response.Error = "Unauthroized -- Your session_token is invalid";
                                break;
                            case 403:
                                response.Error = "Forbidden -- You do not have access";
                                break;
                            case 404:
                                response.Error = "Not Found";
                                break;
                            case 405:
                                response.Error = "Method Not Allowed";
                                break;
                            case 406:
                                response.Error = "Not Acceptable -- Purchasing is disabled";
                                break;
                            case 409:
                                response.Error = "Conflict -- Your state is most likely not aligned with the servers.";
                                break;
                            case 429:
                                response.Error = "Too Many Requests -- You're being limited for sending too many requests too quickly.";
                                break;
                            case 500:
                                response.Error = "Internal Server Error -- We had a problem with our server. Try again later.";
                                break;
                            case 503:
                                response.Error = "Service Unavailable -- We're either offline for maintenance, or an error that should be solvable by calling again later was triggered.";
                                break;
                        }

                        bool isSteam = LootLockerSDKManager.GetCurrentPlatform() == "steam";
                        if ((webRequest.responseCode == 401 || webRequest.responseCode == 403) && LootLockerConfig.current.allowTokenRefresh && !isSteam && tries < maxRetry) 
                        {
                            tries++;
                            LootLockerSDKManager.DebugMessage("Refreshing Token, Since we could not find one. If you do not want this please turn off in the lootlocker config settings");
                            RefreshTokenAndCompleteCall(request,(value)=> { tries = 0; OnServerResponse?.Invoke(value); });
                        }
                        else
                        {
                            tries = 0;
                            response.Error += " " + webRequest.downloadHandler.text;
                            response.text = webRequest.downloadHandler.text;

                            response.success = false;
                            response.hasError = true;
                            response.text = webRequest.downloadHandler.text;
                            OnServerResponse?.Invoke(response);
                        }

                    }
                    else
                    {
                        response.success = true;
                        response.hasError = false;
                        response.text = webRequest.downloadHandler.text;
                        OnServerResponse?.Invoke(response);
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
            { "Access-Control-Allow-Origin", "*" }
        };

        protected event System.Action<ServerError> OnError;

        protected struct ServerError
        {
            public HttpStatusCode status;
            public string text;
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
                    LootLockerSDKManager.DebugMessage("Texture download failed for: " + url, true);
                }

                OnComplete?.Invoke(texture);
            }
        }

        UnityWebRequest CreateWebRequest(string url, LootLockerServerRequest request)
        {
            UnityWebRequest webRequest;
            switch (request.httpMethod)
            {
                case LootLockerHTTPMethod.UPLOAD:
                    webRequest = UnityWebRequest.Post(url, request.form);
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

                        //Debug.LogError("Content type Set: " + contentType);
                        // Make my request object and add the raw body. Set anything else you need here
                        webRequest = new UnityWebRequest();
                        webRequest.SetRequestHeader("Content-Type", "multipart/form-data; boundary=--");
                        webRequest.uri = new Uri(url);
                        Debug.Log(url);//the url is wrong in some cases
                        webRequest.uploadHandler = new UploadHandlerRaw(formSections);
                        webRequest.uploadHandler.contentType = contentType;
                        webRequest.useHttpContinue = false;

                        // webRequest.method = "POST";
                        webRequest.method = UnityWebRequest.kHttpVerbPOST;
                    }
                    else
                    {
                        string json = (request.payload != null && request.payload.Count > 0) ? JsonConvert.SerializeObject(request.payload) : request.jsonPayload;
#if UNITY_EDITOR
                        LootLockerSDKManager.DebugMessage("REQUEST BODY = " + json);
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

            if (request.extraHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in request.extraHeaders)
                {
                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            return webRequest;
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

