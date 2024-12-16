using System;
using System.Collections.Generic;
using LootLocker.LootLockerEnums;
using UnityEngine;

namespace LootLocker.LootLockerEnums
{
    public enum LootLockerHTTPRequestDataType
    {
        EMPTY = 0,
        JSON = 1,
        WWW_FORM = 2,
        FILE = 3,
    }
}

namespace LootLocker.HTTP
{
    [Serializable]
    public class LootLockerHTTPRequestData
    {
        /// <summary>
        /// The endpoint to send the request to
        /// </summary>
        public string Endpoint { get; set; }
        /// <summary>
        /// The HTTP method to use for the request
        /// </summary>
        public LootLockerHTTPMethod HTTPMethod { get; set; }
        /// <summary>
        /// Which target to use for the request
        /// </summary>
        public LootLockerCallerRole CallerRole { get; set; }
        /// <summary>
        /// The full url with endpoint, target, and query parameters included
        /// </summary>
        public string FormattedURL { get; set; }
        /// <summary>
        /// The content of the request, check content.dataType to see what type of content it is
        /// </summary>
        public LootLockerHTTPRequestContent Content { get; set; }

        /// <summary>
        /// Leave this null if you don't need custom headers
        /// </summary>
        public Dictionary<string, string> ExtraHeaders;

        /// <summary>
        /// Query parameters to append to the end of the request URI
        /// Example: If you include a dictionary with a key of "page" and a value of "42" (as a string) then the url would become "https://mydomain.com/endpoint?page=42"
        /// </summary>
        public Dictionary<string, string> QueryParams;

        /// <summary>
        /// How many times this request has been retried
        /// </summary>
        public int TimesRetried { get; set; }

        /// <summary>
        /// The listeners for handling responses
        /// </summary>
        public List<Action<LootLockerResponse>> Listeners { get; set; }

        /// <summary>
        /// Whether the listeners have been invoked or not
        /// </summary>
        public bool HaveListenersBeenInvoked { get; set; }

        /// <summary>
        /// A generated id for this request, it is a combination of hashes for the endpoint, headers and content
        /// </summary>
        public string RequestId { get; set; }


        /// <summary>
        /// Call all listeners with response
        /// </summary>
        public void CallListenersWithResult(LootLockerResponse response)
        {
            foreach(var listener in Listeners)
            {
                listener?.Invoke(response);
            }
            HaveListenersBeenInvoked = true;
        }

        public override bool Equals(object obj)
        {
            if(obj != null && obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((LootLockerHTTPRequestData)obj);
        }

        public bool Equals(LootLockerHTTPRequestData other)
        {
            return other != null && other.RequestId.Equals(RequestId);
        }

        public override int GetHashCode()
        {
            return RequestId.GetHashCode();
        }

        #region Factory Methods
        public static LootLockerHTTPRequestData MakeFileRequest(string endPoint, LootLockerHTTPMethod httpMethod, byte[] file, string fileName, string fileContentType, Dictionary<string, string> body, Action<LootLockerResponse> onComplete, bool useAuthToken, LootLockerCallerRole callerRole, Dictionary<string, string> additionalHeaders, Dictionary<string, string> queryParams)
        {
            LootLockerHTTPRequestContent content = null;
            if (LootLockerHTTPMethod.PUT == httpMethod)
            {
                content = new LootLockerWWWFormRequestContent(file, fileName, fileContentType);
            }
            else
            {
                content = new LootLockerFileRequestContent(file, fileName, body);
            }
            return _MakeRequestDataWithContent(
                    content,
                    endPoint,
                    httpMethod,
                    onComplete,
                    useAuthToken,
                    callerRole,
                    additionalHeaders,
                    queryParams);
        }

        public static LootLockerHTTPRequestData MakeJsonRequest(string endPoint, LootLockerHTTPMethod httpMethod, string body, Action<LootLockerResponse> onComplete, bool useAuthToken, LootLockerCallerRole callerRole, Dictionary<string, string> additionalHeaders, Dictionary<string, string> queryParams)
        {
            return _MakeRequestDataWithContent(new LootLockerJsonBodyRequestContent(string.IsNullOrEmpty(body) ? "{}" : body), endPoint, httpMethod, onComplete, useAuthToken, callerRole, additionalHeaders, queryParams);
        }

        public static LootLockerHTTPRequestData MakeNoContentRequest(string endPoint, LootLockerHTTPMethod httpMethod, Action<LootLockerResponse> onComplete, bool useAuthToken, LootLockerCallerRole callerRole, Dictionary<string, string> additionalHeaders, Dictionary<string, string> queryParams)
        {
            return _MakeRequestDataWithContent(new LootLockerHTTPRequestContent(), endPoint, httpMethod, onComplete, useAuthToken, callerRole, additionalHeaders, queryParams);
        }

        private static LootLockerHTTPRequestData _MakeRequestDataWithContent(LootLockerHTTPRequestContent content, string endPoint, LootLockerHTTPMethod httpMethod, Action<LootLockerResponse> onComplete, bool useAuthToken, LootLockerCallerRole callerRole, Dictionary<string, string> additionalHeaders, Dictionary<string, string> queryParams)
        {
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
            if (headers != null && headers.Count == 0)
            {
                headers = null; // Force extra headers to null if empty dictionary was supplied
            }
            string headersString = "";
            foreach(var header in headers)
            {
                headersString += $"|{header.Key}:{header.Value}";
            }

            string formattedUrl = BuildUrl(endPoint, queryParams, callerRole);
            string requestId = $"{formattedUrl}-h{headersString.GetHashCode()}-c{content.GetHashCode()}-{Guid.NewGuid()}";

            return new LootLockerHTTPRequestData
            {
                TimesRetried = 0,
                Endpoint = endPoint,
                HTTPMethod = httpMethod,
                ExtraHeaders = headers,
                QueryParams = queryParams,
                CallerRole = callerRole,
                Content = content,
                Listeners = new List<Action<LootLockerResponse>> { onComplete },
                HaveListenersBeenInvoked = false,
                FormattedURL = formattedUrl,
                RequestId = requestId
            };
        }
        #endregion

        #region Helper Methods
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

        private static string BuildUrl(string endpoint, Dictionary<string, string> queryParams, LootLockerCallerRole callerRole)
        {
            string trimmedEndpoint = endpoint.StartsWith("/") ? endpoint.Trim() : "/" + endpoint.Trim();
            string urlBase;
            switch (callerRole)
            {
                case LootLockerCallerRole.Admin:
                    urlBase = LootLockerConfig.current.adminUrl;
                    break;
                case LootLockerCallerRole.User:
                    urlBase = LootLockerConfig.current.userUrl;
                    break;
                case LootLockerCallerRole.Player:
                    urlBase = LootLockerConfig.current.playerUrl;
                    break;
                case LootLockerCallerRole.Base:
                    urlBase = LootLockerConfig.current.baseUrl;
                    break;
                default:
                    urlBase = LootLockerConfig.current.url;
                    break;
            }

            return (urlBase + trimmedEndpoint + GetQueryParameterStringFromDictionary(queryParams)).Trim();
        }

        public static string GetQueryParameterStringFromDictionary(Dictionary<string, string> queryDict)
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

    public class LootLockerHTTPRequestContent
    {
        public LootLockerHTTPRequestContent(LootLockerHTTPRequestDataType type = LootLockerHTTPRequestDataType.EMPTY)
        {
            this.dataType = type;
        }
        public LootLockerHTTPRequestDataType dataType { get; set; }

        public override int GetHashCode()
        {
#if UNITY_2021_3_OR_NEWER
            return HashCode.Combine(dataType, string.Empty.GetHashCode());
#else
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ((int)dataType);
                hash = hash * 31 + string.Empty.GetHashCode();
                return hash;
            }
#endif
        }
    }

    public class LootLockerJsonBodyRequestContent : LootLockerHTTPRequestContent
    {
        public LootLockerJsonBodyRequestContent(string jsonBody) : base(LootLockerHTTPRequestDataType.JSON)
        {
            this.jsonBody = jsonBody;
        }
        public string jsonBody { get; set; }

        public override int GetHashCode()
        {
#if UNITY_2021_3_OR_NEWER
            return HashCode.Combine(dataType, jsonBody.GetHashCode());
#else
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ((int)dataType);
                hash = hash * 31 + jsonBody.GetHashCode();
                return hash;
            }
#endif
        }
    }

    public class LootLockerWWWFormRequestContent : LootLockerHTTPRequestContent
    {
        public LootLockerWWWFormRequestContent(byte[] content, string name, string type) : base(LootLockerHTTPRequestDataType.WWW_FORM)
        {
            this.content = content;
            this.name = name;
            this.type = type;
        }
        public byte[] content { get; set; }
        public string name { get; set; }
        public string type { get; set; }

        public override int GetHashCode()
        {
#if UNITY_2021_3_OR_NEWER
            return HashCode.Combine(dataType, content.GetHashCode(), name.GetHashCode(), type.GetHashCode());
#else
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ((int)dataType);
                hash = hash * 31 + content.GetHashCode();
                hash = hash * 31 + name.GetHashCode();
                hash = hash * 31 + type.GetHashCode();
                return hash;
            }
#endif
        }
    }

    public class LootLockerFileRequestContent : LootLockerHTTPRequestContent
    {
        public LootLockerFileRequestContent(byte[] content, string name, Dictionary<string, string> formFields) : base(LootLockerHTTPRequestDataType.FILE)
        {
            this.fileForm = new WWWForm();

            foreach (var kvp in formFields)
            {
                this.fileForm.AddField(kvp.Key, kvp.Value);
            }

            this.fileForm.AddBinaryData("file", content, name);
        }
        public WWWForm fileForm { get; set; }

        public override int GetHashCode()
        {
#if UNITY_2021_3_OR_NEWER
            return HashCode.Combine(dataType, fileForm.GetHashCode());
#else
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ((int)dataType);
                hash = hash * 31 + fileForm.GetHashCode();
                return hash;
            }
#endif
        }
    }
}
