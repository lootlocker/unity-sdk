#if LOOTLOCKER_LEGACY_HTTP_STACK
using System.Collections.Generic;
using UnityEngine;
using System;
using LootLocker.LootLockerEnums;

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
        public Dictionary<string, object> payload { get; set; }
        public string jsonPayload { get; set; }
        public byte[] upload { get; set; }
        public string uploadName { get; set; }
        public string uploadType { get; set; }
        public LootLockerCallerRole callerRole { get; set; }
        public WWWForm form { get; set; }
        public string forPlayerWithUlid { get; set; }
        public DateTime requestStartTime { get; set; }

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

        public static void CallAPI(string forPlayerWithUlid, string endPoint, LootLockerHTTPMethod httpMethod,
            string body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true,
            LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User,
            Dictionary<string, string> additionalHeaders = null)
        {
            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                onComplete?.Invoke(LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit(), forPlayerWithUlid, DateTime.Now));
                return;
            }

            if (useAuthToken && string.IsNullOrEmpty(forPlayerWithUlid))
            {
                forPlayerWithUlid = LootLockerStateData.GetDefaultPlayerULID();
            }

            LootLockerLogger.Log("Caller Type: " + callerRole, LootLockerLogger.LogLevel.Debug);

            Dictionary<string, string> headers = new Dictionary<string, string>();

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
                else
                {
                    var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
                    if (playerData != null && !string.IsNullOrEmpty(playerData.SessionToken))
                    {
                        headers.Add("x-session-token", playerData.SessionToken);
                    }
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

            new LootLockerServerRequest(forPlayerWithUlid, endPoint, httpMethod, body, headers, callerRole: callerRole).Send((response) => { onComplete?.Invoke(response); });
        }

        public static void UploadFile(string forPlayerWithUlid, string endPoint, LootLockerHTTPMethod httpMethod, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                onComplete?.Invoke(LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit(), forPlayerWithUlid, DateTime.Now));
                return;
            }
            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (file.Length == 0)
            {
                LootLockerLogger.Log("File content is empty, not allowed.", LootLockerLogger.LogLevel.Error);
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>("File content is empty, not allowed.", forPlayerWithUlid));
                return;
            }
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
                else
                {
                    var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(forPlayerWithUlid);
                    if (playerData != null && !string.IsNullOrEmpty(playerData.SessionToken))
                    {
                        headers.Add("x-session-token", playerData.SessionToken);
                    }
                }
            }
            
            new LootLockerServerRequest(forPlayerWithUlid, endPoint, httpMethod, file, fileName, fileContentType, body, headers, callerRole: callerRole).Send((response) => { onComplete?.Invoke(response); });
        }
        
        public static void UploadFile(string forPlayerWithUlid, EndPointClass endPoint, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null,
            bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            UploadFile(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, file, fileName, fileContentType, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken, callerRole);
        }

        #endregion

        #region ServerRequest constructor

        public LootLockerServerRequest(string forPlayerWithUlid, string endpoint, LootLockerHTTPMethod httpMethod = LootLockerHTTPMethod.GET, byte[] upload = null, string uploadName = null, string uploadType = null, Dictionary<string, string> body = null,
            Dictionary<string, string> extraHeaders = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User, bool isFileUpload = true)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.payload = null;
            this.upload = upload;
            this.uploadName = uploadName;
            this.uploadType = uploadType;
            this.jsonPayload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = null;
            this.callerRole = callerRole;
            this.form = new WWWForm();
            this.forPlayerWithUlid = forPlayerWithUlid;
            this.requestStartTime = DateTime.Now;

            foreach (var kvp in body)
            {
                this.form.AddField(kvp.Key, kvp.Value);
            }

            this.form.AddBinaryData("file", upload, uploadName);

            bool isNonPayloadMethod = (this.httpMethod == LootLockerHTTPMethod.GET || this.httpMethod == LootLockerHTTPMethod.HEAD || this.httpMethod == LootLockerHTTPMethod.OPTIONS);

            if (this.payload != null && isNonPayloadMethod)
            {
                LootLockerLogger.Log("Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint, LootLockerLogger.LogLevel.Warning);
            }
        }

        public LootLockerServerRequest(string forPlayerWithUlid, string endpoint, LootLockerHTTPMethod httpMethod = LootLockerHTTPMethod.GET, string payload = null, Dictionary<string, string> extraHeaders = null, Dictionary<string, string> queryParams = null, bool useAuthToken = true,
            LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.jsonPayload = payload;
            this.upload = null;
            this.uploadName = null;
            this.uploadType = null;
            this.payload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = queryParams != null && queryParams.Count == 0 ? null : queryParams;
            this.callerRole = callerRole;
            bool isNonPayloadMethod = (this.httpMethod == LootLockerHTTPMethod.GET || this.httpMethod == LootLockerHTTPMethod.HEAD || this.httpMethod == LootLockerHTTPMethod.OPTIONS);
            this.form = null;
            this.forPlayerWithUlid = forPlayerWithUlid;
            this.requestStartTime = DateTime.Now;
            if (!string.IsNullOrEmpty(jsonPayload) && isNonPayloadMethod)
            {
                LootLockerLogger.Log("Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint, LootLockerLogger.LogLevel.Warning);
            }
        }

        #endregion

        /// <summary>
        /// just debug and call ServerAPI.SendRequest which takes the current ServerRequest and pass this response
        /// </summary>
        public void Send(System.Action<LootLockerResponse> OnServerResponse)
        {
            LootLockerHTTPClient.SendRequest(this, (response) => { OnServerResponse?.Invoke(response); });
        }
    }
}
#endif
