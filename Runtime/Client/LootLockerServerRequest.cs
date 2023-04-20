using System.Collections.Generic;
using UnityEngine;
using System;
using LootLocker.LootLockerEnums;
#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
#else
using LLlibs.ZeroDepJson;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

//this is common between user and admin
namespace LootLocker
{

    public static class LootLockerJsonSettings
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        public static readonly JsonSerializerSettings Default = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
            Formatting = Formatting.None
        };
#else
        public static readonly JsonOptions Default = new JsonOptions(JsonSerializationOptions.Default & ~JsonSerializationOptions.SkipGetOnly);
#endif
    }

    public static class LootLockerJson
    {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        public static string SerializeObject(object obj)
        {
            return SerializeObject(obj, LootLockerJsonSettings.Default);
        }

        public static string SerializeObject(object obj, JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(obj, settings ?? LootLockerJsonSettings.Default);
        }

        public static T DeserializeObject<T>(string json)
        {
            return DeserializeObject<T>(json, LootLockerJsonSettings.Default);
        }


        public static T DeserializeObject<T>(string json, JsonSerializerSettings settings)
        {
            return JsonConvert.DeserializeObject<T>(json, settings ?? LootLockerJsonSettings.Default);
        }
#else //LOOTLOCKER_USE_NEWTONSOFTJSON
        public static string SerializeObject(object obj)
        {
            return SerializeObject(obj, LootLockerJsonSettings.Default);
        }

        public static string SerializeObject(object obj, JsonOptions options)
        {
            return Json.Serialize(obj, options ?? LootLockerJsonSettings.Default);
        }

        public static T DeserializeObject<T>(string json)
        {
            return DeserializeObject<T>(json, LootLockerJsonSettings.Default);
        }

        public static T DeserializeObject<T>(string json, JsonOptions options)
        {
            return Json.Deserialize<T>(json, options ?? LootLockerJsonSettings.Default);
        }
#endif //LOOTLOCKER_USE_NEWTONSOFTJSON
    }

    [Serializable]
    public enum LootLockerHTTPMethod
    {
        GET = 0,
        POST = 1,
        DELETE = 2,
        PUT = 3,
        HEAD = 4,
        CREATE = 5,
        OPTIONS = 6,
        PATCH = 7,
        UPLOAD_FILE = 8,
        UPDATE_FILE = 9
    }

    /// <summary>
    /// All ServerAPI.SendRequest responses will invoke the callback using an instance of this class for easier handling in client code.
    /// </summary>
    public class LootLockerResponse
    {
        /// <summary>
        /// TRUE if http error OR server returns an error status
        /// </summary>
        public bool hasError { get; set; }

        /// <summary>
        /// HTTP Status Code
        /// </summary>
        public int statusCode { get; set; }

        /// <summary>
        /// Raw text response from the server
        /// <para>If hasError = true, this will contain the error message.</para>
        /// </summary>
        public string text { get; set; }

        public bool success { get; set; }


        public string Error { get; set; }

        /// <summary>
        /// A texture downloaded in the webrequest, if applicable, otherwise this will be null.
        /// </summary>
        public Texture2D texture { get; set; }

        /// <summary>
        /// inheritdoc added this because unity main thread executing style cut the calling stack and make the event orphan see also calling multiple events 
        /// of the same type makes use unable to identify each one
        /// </summary>
        public string EventId { get; set; }

        public static void Deserialize<T>(Action<T> onComplete, LootLockerResponse serverResponse,
#if LOOTLOCKER_USE_NEWTONSOFTJSON
            JsonSerializerSettings options = null
#else //LOOTLOCKER_USE_NEWTONSOFTJSON
            JsonOptions options = null
#endif
            )
            where T : LootLockerResponse, new()
        {
            onComplete?.Invoke(Deserialize<T>(serverResponse, options));
        }

        public static T Deserialize<T>(LootLockerResponse serverResponse,
#if LOOTLOCKER_USE_NEWTONSOFTJSON
            JsonSerializerSettings options = null
#else //LOOTLOCKER_USE_NEWTONSOFTJSON
            JsonOptions options = null
#endif
            )
            where T : LootLockerResponse, new() 
        {
            if (serverResponse == null)
            {
                return new T() { success = false, Error = "Unknown error, please check your internet connection." };
            }
            else if (!string.IsNullOrEmpty(serverResponse.Error))
            {
                return new T() { success = false, Error = serverResponse.Error, statusCode = serverResponse.statusCode };
            }

            var response = LootLockerJson.DeserializeObject<T>(serverResponse.text, options ?? LootLockerJsonSettings.Default) ?? new T();

            response.text = serverResponse.text;
            response.success = serverResponse.success;
            response.Error = serverResponse.Error;
            response.statusCode = serverResponse.statusCode;

            return response;
        }
    }
    public class LootLockerPaginationResponse<TKey>
    {
        public int total { get; set; }
        public TKey next_cursor { get; set; }
        public TKey previous_cursor { get; set; }
    }

    /// <summary>
    /// Convenience factory class for creating some responses that we use often.
    /// </summary>
    public class LootLockerResponseFactory
    {
        /// <summary>
        /// Construct an error response to send to the client.
        /// </summary>
        public static T Error<T>(string errorMessage) where T : LootLockerResponse, new()
        {
            return new T()
            {
                success = false,
                hasError = true,
                Error = errorMessage,
                text = errorMessage
            };
        }

        /// <summary>
        /// Construct an error response specifically when the SDK has not been initialized.
        /// </summary>
        public static T SDKNotInitializedError<T>() where T : LootLockerResponse, new()
        {
            return Error<T>("SDK not initialized");
        }

        /// <summary>
        /// Construct an error response because an unserializable input has been given
        /// </summary>
        public static T InputUnserializableError<T>() where T : LootLockerResponse, new()
        {
            return Error<T>("Method parameter could not be serialized");
        }

        /// <summary>
        /// Construct an error response because the rate limit has been hit
        /// </summary>
        public static T RateLimitExceeded<T>(string method, int secondsLeftOfRateLimit) where T : LootLockerResponse, new()
        {
            return Error<T>(string.Format("Your request to {0} was not sent. You are sending too many requests and are being rate limited for {1} seconds", method, secondsLeftOfRateLimit));
        }
    }



    #region Rate Limiting Support

    public class RateLimiter
    {
        /* -- Configurable constants -- */
        // Tripwire settings, allow for a max total of n requests per x seconds
        protected const int TripWireTimeFrameSeconds = 60;
        protected const int MaxRequestsPerTripWireTimeFrame = 280;
        protected const int SecondsPerBucket = 5; // Needs to evenly divide the time frame

        // Moving average settings, allow for a max average of n requests per x seconds
        protected const float AllowXPercentOfTripWireMaxForMovingAverage = 0.8f; // Moving average threshold (the average number of requests per bucket) is set slightly lower to stop constant abusive call behaviour just under the tripwire limit
        protected const int CountMovingAverageAcrossNTripWireTimeFrames = 3; // Count Moving average across a longer time period

        /* -- Calculated constants -- */
        protected const int BucketsPerTimeFrame = TripWireTimeFrameSeconds / SecondsPerBucket;
        protected const int RateLimitMovingAverageBucketCount = CountMovingAverageAcrossNTripWireTimeFrames * BucketsPerTimeFrame;
        private const int MaxRequestsPerBucketOnMovingAverage = (int)((MaxRequestsPerTripWireTimeFrame * AllowXPercentOfTripWireMaxForMovingAverage) / (BucketsPerTimeFrame)); 


        /* -- Functionality -- */
        protected readonly int[] buckets = new int[RateLimitMovingAverageBucketCount];

        protected int lastBucket = -1;
        private DateTime _lastBucketChangeTime = DateTime.MinValue;
        private int _totalRequestsInBuckets;
        private int _totalRequestsInBucketsInTripWireTimeFrame;

        protected bool isRateLimited = false;
        private DateTime _rateLimitResolvesAt = DateTime.MinValue;

        protected virtual DateTime GetTimeNow()
        {
            return DateTime.Now;
        }

        public int GetSecondsLeftOfRateLimit()
        {
            if (!isRateLimited)
            {
                return 0;
            }
            return (int)Math.Ceiling((_rateLimitResolvesAt - GetTimeNow()).TotalSeconds);
        }
        private int MoveCurrentBucket(DateTime now)
        {
            int moveOverXBuckets = _lastBucketChangeTime == DateTime.MinValue ? 1 : (int)Math.Floor((now - _lastBucketChangeTime).TotalSeconds / SecondsPerBucket);
            if (moveOverXBuckets == 0)
            {
                return lastBucket;
            }

            for (int stepIndex = 1; stepIndex <= moveOverXBuckets; stepIndex++)
            {
                int bucketIndex = (lastBucket + stepIndex) % buckets.Length;
                if (bucketIndex == lastBucket)
                {
                    continue;
                }
                int bucketMovingOutOfTripWireTimeFrame = (bucketIndex - BucketsPerTimeFrame) < 0 ? buckets.Length + (bucketIndex - BucketsPerTimeFrame) : bucketIndex - BucketsPerTimeFrame;
                _totalRequestsInBucketsInTripWireTimeFrame -= buckets[bucketMovingOutOfTripWireTimeFrame]; // Remove the request count from the bucket that is moving out of the time frame from trip wire count
                _totalRequestsInBuckets -= buckets[bucketIndex]; // Remove the count from the bucket we're moving into from the total before emptying it
                buckets[bucketIndex] = 0;
            }

            return (lastBucket + moveOverXBuckets) % buckets.Length; // Step to next bucket and wrap around if necessary;
        }

        public virtual bool AddRequestAndCheckIfRateLimitHit()
        {
            DateTime now = GetTimeNow();
            var currentBucket = MoveCurrentBucket(now);

            if (isRateLimited)
            {
                if (_totalRequestsInBuckets <= 0)
                {
                    isRateLimited = false;
                    _rateLimitResolvesAt = DateTime.MinValue;
                }
            }
            else
            {
                buckets[currentBucket]++; // Increment the current bucket
                _totalRequestsInBuckets++; // Increment the total request count
                _totalRequestsInBucketsInTripWireTimeFrame++; // Increment the request count for the current time frame

                isRateLimited |= _totalRequestsInBucketsInTripWireTimeFrame >= MaxRequestsPerTripWireTimeFrame; // If the request count for the time frame is greater than the max requests per time frame, set isRateLimited to true
                isRateLimited |= _totalRequestsInBuckets / RateLimitMovingAverageBucketCount > MaxRequestsPerBucketOnMovingAverage; // If the average number of requests per bucket is greater than the max requests on moving average, set isRateLimited to true
#if UNITY_EDITOR
                if (_totalRequestsInBucketsInTripWireTimeFrame >= MaxRequestsPerTripWireTimeFrame) LootLockerLogger.GetForLogLevel()("Rate Limit Hit due to Trip Wire, count = " + _totalRequestsInBucketsInTripWireTimeFrame + " out of allowed " + MaxRequestsPerTripWireTimeFrame);
                if (_totalRequestsInBuckets / RateLimitMovingAverageBucketCount > MaxRequestsPerBucketOnMovingAverage) LootLockerLogger.GetForLogLevel()("Rate Limit Hit due to Moving Average, count = " + _totalRequestsInBuckets / RateLimitMovingAverageBucketCount + " out of allowed " + MaxRequestsPerBucketOnMovingAverage);
#endif
                if (isRateLimited)
                {
                    _rateLimitResolvesAt = (now - TimeSpan.FromSeconds(now.Second % SecondsPerBucket)) + TimeSpan.FromSeconds(buckets.Length*SecondsPerBucket);
                }
            }
            if (currentBucket != lastBucket)
            {
                _lastBucketChangeTime = now;
                lastBucket = currentBucket;
            }
            return isRateLimited;
        }

        protected int GetMaxRequestsInSingleBucket()
        {
            int maxRequests = 0;
            foreach (var t in buckets)
            {
                maxRequests = Math.Max(maxRequests, t);
            }

            return maxRequests;
        }

        private static RateLimiter _rateLimiter = null;

        public static RateLimiter Get()
        {
            if (_rateLimiter == null)
            {
                Reset();
            }
            return _rateLimiter;
        }

        public static void Reset()
        {
            _rateLimiter = new RateLimiter();
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Reset RateLimiter due to entering play mode");
            Reset();
        }
#endif
    }
    #endregion

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
        public LootLocker.LootLockerEnums.LootLockerCallerRole adminCall { get; set; }
        public WWWForm form { get; set; }

        /// <summary>
        /// Leave this null if you don't need custom headers
        /// </summary>
        public Dictionary<string, string> extraHeaders;

        /// <summary>
        /// Query parameters to append to the end of the request URI
        /// <para>Example: If you include a dictionary with a key of "page" and a value of "42" (as a string) then the url would become "https: //mydomain.com/endpoint?page=42"</para>
        /// </summary>
        public Dictionary<string, string> queryParams;

        public int retryCount { get; set; }

        #region Make ServerRequest and call send (3 functions)

        public static void CallAPI(string endPoint, LootLockerHTTPMethod httpMethod, string body = null, Action<LootLockerResponse> onComplete = null, bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                onComplete?.Invoke(LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit()));
                return;
            }

#if UNITY_EDITOR
            LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)("Caller Type: " + callerRole);
#endif

            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (useAuthToken)
            {
                headers = new Dictionary<string, string>();
                if (callerRole == LootLocker.LootLockerEnums.LootLockerCallerRole.Admin && !string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
                {
                    headers.Add("x-auth-token", LootLockerConfig.current.adminToken);
                }
                else if (callerRole != LootLocker.LootLockerEnums.LootLockerCallerRole.Admin && !string.IsNullOrEmpty(LootLockerConfig.current.token))
                {
                    headers.Add("x-session-token", LootLockerConfig.current.token);
                }
            }

            if (LootLockerConfig.current != null)
                headers.Add(LootLockerConfig.current.dateVersion.key, LootLockerConfig.current.dateVersion.value);

            LootLockerBaseServerAPI.I.SwitchURL(callerRole);

            new LootLockerServerRequest(endPoint, httpMethod, body, headers, callerRole: callerRole).Send((response) => { onComplete?.Invoke(response); });
        }

        public static void CallDomainAuthAPI(string endPoint, LootLockerHTTPMethod httpMethod, string body = null, Action<LootLockerResponse> onComplete = null)
        {
            if (RateLimiter.Get().AddRequestAndCheckIfRateLimitHit())
            {
                onComplete?.Invoke(LootLockerResponseFactory.RateLimitExceeded<LootLockerResponse>(endPoint, RateLimiter.Get().GetSecondsLeftOfRateLimit()));
                return;
            }
            
            if (LootLockerConfig.current.domainKey.ToString().Length == 0)
            {
#if UNITY_EDITOR
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Error)("LootLocker domain key must be set in settings");
#endif
                onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("LootLocker domain key must be set in settings"));

                return;
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("domain-key", LootLockerConfig.current.domainKey);

            if((!LootLockerConfig.current.IsPrefixedApiKey() && LootLockerConfig.current.developmentMode) || LootLockerConfig.current.apiKey.StartsWith("dev_"))
            {
                headers.Add("is-development", "true");
            }

            LootLockerBaseServerAPI.I.SwitchURL(LootLockerCallerRole.Base);

            new LootLockerServerRequest(endPoint, httpMethod, body, headers, callerRole: LootLockerCallerRole.Base).Send((response) => { onComplete?.Invoke(response); });
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
                onComplete?.Invoke(LootLockerResponseFactory.Error<LootLockerResponse>("File content is empty, not allowed."));
                return;
            }
            if (useAuthToken)
            {
                headers = new Dictionary<string, string>();

                headers.Add(callerRole == LootLockerCallerRole.Admin ? "x-auth-token" : "x-session-token", LootLockerConfig.current.token);
            }

            LootLockerBaseServerAPI.I.SwitchURL(callerRole);

            new LootLockerServerRequest(endPoint, httpMethod, file, fileName, fileContentType, body, headers, callerRole: callerRole).Send((response) => { onComplete?.Invoke(response); });
        }
        
        public static void UploadFile(EndPointClass endPoint, byte[] file, string fileName = "file", string fileContentType = "text/plain", Dictionary<string, string> body = null, Action<LootLockerResponse> onComplete = null,
            bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            UploadFile(endPoint.endPoint, endPoint.httpMethod, file, fileName, fileContentType, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken, callerRole);
        }

        #endregion

        #region ServerRequest constructor

        public LootLockerServerRequest(string endpoint, LootLockerHTTPMethod httpMethod = LootLockerHTTPMethod.GET, byte[] upload = null, string uploadName = null, string uploadType = null, Dictionary<string, string> body = null,
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
            this.adminCall = callerRole;
            this.form = new WWWForm();

            foreach (var kvp in body)
            {
                this.form.AddField(kvp.Key, kvp.Value);
            }

            this.form.AddBinaryData("file", upload, uploadName);

            bool isNonPayloadMethod = (this.httpMethod == LootLockerHTTPMethod.GET || this.httpMethod == LootLockerHTTPMethod.HEAD || this.httpMethod == LootLockerHTTPMethod.OPTIONS);

            if (this.payload != null && isNonPayloadMethod)
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }

        public LootLockerServerRequest(string endpoint, LootLockerHTTPMethod httpMethod = LootLockerHTTPMethod.GET, Dictionary<string, object> payload = null, Dictionary<string, string> extraHeaders = null, Dictionary<string, string> queryParams = null,
            bool useAuthToken = true, LootLocker.LootLockerEnums.LootLockerCallerRole callerRole = LootLocker.LootLockerEnums.LootLockerCallerRole.User)
        {
            this.retryCount = 0;
            this.endpoint = endpoint;
            this.httpMethod = httpMethod;
            this.payload = payload != null && payload.Count == 0 ? null : payload; //Force payload to null if an empty dictionary was supplied
            this.upload = null;
            this.uploadName = null;
            this.uploadType = null;
            this.jsonPayload = null;
            this.extraHeaders = extraHeaders != null && extraHeaders.Count == 0 ? null : extraHeaders; // Force extra headers to null if empty dictionary was supplied
            this.queryParams = queryParams != null && queryParams.Count == 0 ? null : queryParams;
            this.adminCall = callerRole;
            bool isNonPayloadMethod = (this.httpMethod == LootLockerHTTPMethod.GET || this.httpMethod == LootLockerHTTPMethod.HEAD || this.httpMethod == LootLockerHTTPMethod.OPTIONS);
            this.form = null;
            if (this.payload != null && isNonPayloadMethod)
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Warning)("Payloads should not be sent in GET, HEAD, OPTIONS, requests. Attempted to send a payload to: " + this.httpMethod.ToString() + " " + this.endpoint);
            }
        }

        public LootLockerServerRequest(string endpoint, LootLockerHTTPMethod httpMethod = LootLockerHTTPMethod.GET, string payload = null, Dictionary<string, string> extraHeaders = null, Dictionary<string, string> queryParams = null, bool useAuthToken = true,
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
            this.adminCall = callerRole;
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
            LootLockerBaseServerAPI.I.SendRequest(this, (response) => { OnServerResponse?.Invoke(response); });
        }
    }
}