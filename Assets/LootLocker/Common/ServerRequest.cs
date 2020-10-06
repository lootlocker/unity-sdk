using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;

//this is common between user and admin
namespace LootLocker
{
    [System.Serializable]
    public enum HTTPMethod
    {
        GET = 0,
        POST = 1,
        DELETE = 2,
        PUT = 3,
        HEAD = 4,
        CREATE = 5,
        OPTIONS = 6,
        PATCH = 7,
        UPLOAD = 8
    }

    /// <summary>
    /// All ServerAPI.SendRequest responses will invoke the callback using an instance of this class for easier handling in client code.
    /// </summary>
    [System.Serializable]
    public class LootLockerResponse
    {
        /// <summary>
        /// TRUE if http error OR server returns an error status
        /// </summary>
        public bool hasError;
        /// <summary>
        /// HTTP Status Code
        /// </summary>
        public int statusCode;
        /// <summary>
        /// Raw text response from the server
        /// <para>If hasError = true, this will contain the error message.</para>
        /// </summary>
        public string text;
        public bool status;
        public string message;
        /// <summary>
        /// Any error that comes from the server
        /// </summary>
        public string Error;
        /// <summary>
        /// inheritdoc added this because unity main thread excuting style cut the calling stack and make the event orphant see also calling multiple events 
        /// of the same type makes use unable to identify each one
        /// </summary>
        public string EventId;

    }

    /// <summary>
    /// Construct a request to send to the server.
    /// </summary>
    [System.Serializable]
    public struct ServerRequest
    {
        public string endpoint;
        public HTTPMethod httpMethod;
        public Dictionary<string, object> payload;
        public string jsonPayload;
        public byte[] upload;
        public bool adminCall;
        public WWWForm form;
        /// <summary>
        /// Leave this null if you don't need custom headers
        /// </summary>
        public Dictionary<string, string> extraHeaders;
        /// <summary>
        /// Query parameters to append to the end of the request URI
        /// <para>Example: If you include a dictionary with a key of "page" and a value of "42" (as a string) then the url would become "https: //mydomain.com/endpoint?page=42"</para>
        /// </summary>
        public Dictionary<string, string> queryParams;
        public int retryCount;

        #region Make ServerRequest and call send (3 functions)
        public static void CallAPI(string endPoint, HTTPMethod httpMethod, Dictionary<string, object> body = null, Action<LootLockerResponse> onComplete = null)
        {
            new ServerRequest(endPoint, httpMethod, body).Send((response) =>
             {
                 onComplete?.Invoke(response);
             });
        }
        public static void CallAPI(string endPoint, HTTPMethod httpMethod, string body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, bool isAdminCall = false)
        {

            Debug.Log("AdminCall: " + isAdminCall);

            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (useAuthToken)
            {
                headers = new Dictionary<string, string>();
                headers.Add(isAdminCall ? "x-auth-token" : "x-session-token", BaseServerAPI.activeConfig.token);
            }

            BaseServerAPI.I.SwitchURL(isAdminCall);

            new ServerRequest(endPoint, httpMethod, body, headers, isAdminCall: isAdminCall).Send((response) =>
            {
                onComplete?.Invoke(response);
            });
        }
        public static void UploadFile(string endPoint, HTTPMethod httpMethod, byte[] file, Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, bool isAdminCall = false)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (useAuthToken)
            {
                headers = new Dictionary<string, string>();

                headers.Add(isAdminCall ? "x-auth-token" : "x-session-token", BaseServerAPI.activeConfig.token);

            }

            BaseServerAPI.I.SwitchURL(isAdminCall);

            new ServerRequest(endPoint, httpMethod, file, body, headers, isAdminCall: true).Send((response) =>
              {
                  onComplete?.Invoke(response);
              });
        }
        #endregion

        #region ServerRequest constructor
        public ServerRequest(string endpoint, HTTPMethod httpMethod = HTTPMethod.GET, byte[] upload = null, Dictionary<string, string> body = null, Dictionary<string, string> extraHeaders = null, bool useAuthToken = true, bool isAdminCall = false, bool isFileUpload = true)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.payload = null;
            this.upload = upload;
            this.jsonPayload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = null;
            this.adminCall = isAdminCall;
            this.form = new WWWForm();

            foreach (var kvp in body)
            {
                this.form.AddField(kvp.Key, kvp.Value);
            }

            this.form.AddBinaryData("file", upload, "file");

            bool isNonPayloadMethod = (this.httpMethod == HTTPMethod.GET || this.httpMethod == HTTPMethod.HEAD || this.httpMethod == HTTPMethod.OPTIONS);

            if (this.payload != null && isNonPayloadMethod)
            {
                Debug.LogWarning("WARNING: Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }
        public ServerRequest(string endpoint, HTTPMethod httpMethod = HTTPMethod.GET, byte[] upload = null, string body = null, Dictionary<string, string> extraHeaders = null, Dictionary<string, string> queryParams = null, bool useAuthToken = true, bool isAdminCall = false)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.payload = null;
            this.upload = upload;
            this.jsonPayload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = queryParams != null && queryParams.Count == 0 ? null : queryParams;
            this.adminCall = isAdminCall;
            this.form = null;
            bool isNonPayloadMethod = (this.httpMethod == HTTPMethod.GET || this.httpMethod == HTTPMethod.HEAD || this.httpMethod == HTTPMethod.OPTIONS);

            if (this.payload != null && isNonPayloadMethod)
            {
                Debug.LogWarning("WARNING: Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }
        public ServerRequest(string endpoint, HTTPMethod httpMethod = HTTPMethod.GET, Dictionary<string, object> payload = null, Dictionary<string, string> extraHeaders = null, Dictionary<string, string> queryParams = null, bool useAuthToken = true, bool isAdminCall = false)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.payload = payload != null && payload.Count == 0 ? null : payload; //Force payload to null if an empty dictionary was supplied
            this.upload = null;
            this.jsonPayload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = queryParams != null && queryParams.Count == 0 ? null : queryParams;
            this.adminCall = isAdminCall;
            bool isNonPayloadMethod = (this.httpMethod == HTTPMethod.GET || this.httpMethod == HTTPMethod.HEAD || this.httpMethod == HTTPMethod.OPTIONS);
            this.form = null;
            if (this.payload != null && isNonPayloadMethod)
            {
                Debug.LogWarning("WARNING: Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }
        public ServerRequest(string endpoint, HTTPMethod httpMethod = HTTPMethod.GET, string payload = null, Dictionary<string, string> extraHeaders = null, Dictionary<string, string> queryParams = null, bool useAuthToken = true, bool isAdminCall = false)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.jsonPayload = payload; //Force payload to null if an empty dictionary was supplied
            this.upload = null;
            this.payload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = queryParams != null && queryParams.Count == 0 ? null : queryParams;
            this.adminCall = isAdminCall;
            bool isNonPayloadMethod = (this.httpMethod == HTTPMethod.GET || this.httpMethod == HTTPMethod.HEAD || this.httpMethod == HTTPMethod.OPTIONS);
            this.form = null;
            if (string.IsNullOrEmpty(jsonPayload))
            {
                Debug.LogWarning("WARNING: Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }
        #endregion

        /// <summary>
        /// just debug and call ServerAPI.SendRequest which takes the current ServerRequest and pass this response
        /// </summary>
        public void Send(System.Action<LootLockerResponse> OnServerResponse)
        {
            Debug.Log("Sending Request: " + httpMethod.ToString() + " " + endpoint + " -- queryParams: " + queryParams?.Count);
            BaseServerAPI.I.SendRequest(this, (response) =>
            {
                OnServerResponse?.Invoke(response);
            });
        }
    }
}
