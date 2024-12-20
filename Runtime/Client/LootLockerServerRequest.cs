using System.Collections.Generic;
using UnityEngine;
using System;
using LootLocker.LootLockerEnums;
#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
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
            Converters = {new StringEnumConverter()},
            Formatting = Formatting.None
        };
#else
        public static readonly JsonOptions Default = new JsonOptions((JsonSerializationOptions.Default | JsonSerializationOptions.EnumAsText) & ~JsonSerializationOptions.SkipGetOnly);
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

        public static bool TryDeserializeObject<T>(string json, out T output)
        {
            return TryDeserializeObject<T>(json, LootLockerJsonSettings.Default, out output);
        }

        public static bool TryDeserializeObject<T>(string json, JsonSerializerSettings options, out T output)
        {
            try
            {
                output = JsonConvert.DeserializeObject<T>(json, options ?? LootLockerJsonSettings.Default);
                return true;
            }
            catch (Exception)
            {
                output = default(T);
                return false;
            }
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

        public static bool TryDeserializeObject<T>(string json, out T output)
        {
            return TryDeserializeObject<T>(json, LootLockerJsonSettings.Default, out output);
        }

        public static bool TryDeserializeObject<T>(string json, JsonOptions options, out T output)
        {
            try
            {
                output = Json.Deserialize<T>(json, options ?? LootLockerJsonSettings.Default);
                return true;
            } catch (Exception)
            {
                output = default(T);
                return false;
            }
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

    public class LootLockerErrorData
    {
        public LootLockerErrorData(int httpStatusCode, string errorMessage)
        {
            code = $"HTTP{httpStatusCode}";
            doc_url = $"https://developer.mozilla.org/docs/Web/HTTP/Status/{httpStatusCode}";
            message = errorMessage;
        }

        public LootLockerErrorData() { }

        /// <summary>
        /// A descriptive code identifying the error.
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// A link to further documentation on the error.
        /// </summary>
        public string doc_url { get; set; }

        /// <summary>
        /// A unique identifier of the request to use in contact with support.
        /// </summary>
        public string request_id { get; set; }

        /// <summary>
        /// A unique identifier for tracing the request through LootLocker systems, use this in contact with support.
        /// </summary>
        public string trace_id { get; set; }

        /// <summary>
        /// If the request was not a success this property will hold any error messages
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// If the request was rate limited (status code 429) or the servers were temporarily unavailable (status code 503) you can use this value to determine how many seconds to wait before retrying
        /// </summary>
        public int? retry_after_seconds { get; set; } = null;

        /// <summary>
        /// An easy way of debugging LootLockerErrorData class, example: Debug.Log(onComplete.errorData);
        /// </summary>
        /// <returns>string used to debug errors</returns>
        public override string ToString()
        {
            // Empty error, make sure we print something
            if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(trace_id) && string.IsNullOrEmpty(request_id))
            {
                return $"An unexpected LootLocker error without error data occurred. Please try again later.\n If the issue persists, please contact LootLocker support.";
            }

            //Print the most important info first
            string prettyError = $"LootLocker Error: \"{message ?? ""}\"";

            // Look for intermittent, non user errors
            if (!string.IsNullOrEmpty(code) && code.StartsWith("HTTP5"))
            {
                prettyError +=
                    $"\nTry again later. If the issue persists, please contact LootLocker support and provide the following error details:\n trace ID - \"{trace_id ?? ""}\",\n request ID - \"{request_id ?? ""}\",\n message - \"{message ?? ""}\".";
                if (!string.IsNullOrEmpty(doc_url))
                {
                    prettyError += $"\nFor more information, see {doc_url} (error code was \"{code}\").";
                }
            }
            // Print user errors
            else
            {
                prettyError +=
                    $"\nThere was a problem with your request. The error message provides information on the problem and will help you fix it.";
                if (!string.IsNullOrEmpty(doc_url ?? ""))
                {
                    prettyError += $"\nFor more information, see {doc_url ?? ""} (error code was \"{code ?? ""}\").";
                }

                prettyError +=
                    $"\nIf you are unable to fix the issue, contact LootLocker support and provide the following error details:";
                if (!string.IsNullOrEmpty(trace_id ?? ""))
                {
                    prettyError += $"\n     trace ID - \"{trace_id}\"";
                }
                if (!string.IsNullOrEmpty(request_id))
                {
                    prettyError += $"\n     request ID - \"{request_id}\"";
                }

                prettyError += $"\n     message - \"{message ?? ""}\".";
            }
            return prettyError;
        }
    }

    /// <summary>
    /// All ServerAPI.SendRequest responses will invoke the callback using an instance of this class for easier handling in client code.
    /// </summary>
    public class LootLockerResponse
    {
        /// <summary>
        /// HTTP Status Code
        /// </summary>
        public int statusCode { get; set; }

        /// <summary>
        /// Whether this request was a success
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// Raw text/http body from the server response
        /// </summary>
        public string text { get; set; }

        /// <summary>
        /// If this request was not a success, this structure holds all the information needed to identify the problem
        /// </summary>
        public LootLockerErrorData errorData { get; set; }

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
                return LootLockerResponseFactory.ClientError<T>("Unknown error, please check your internet connection.");
            }
            else if (serverResponse.errorData != null)
            {
                return new T() { success = false, errorData = serverResponse.errorData, statusCode = serverResponse.statusCode, text = serverResponse.text };
            }

            var response = LootLockerJson.DeserializeObject<T>(serverResponse.text, options ?? LootLockerJsonSettings.Default) ?? new T();

            response.text = serverResponse.text;
            response.success = serverResponse.success;
            response.errorData = serverResponse.errorData;
            response.statusCode = serverResponse.statusCode;

            return response;
        }
    }

    public class LootLockerPaginationResponse<TKey>
    {
        /// <summary>
        /// The total available items in this list
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// The cursor that points to the next item in the list. Use this in subsequent requests to get additional items from the list.
        /// </summary>
        public TKey next_cursor { get; set; }
        /// <summary>
        /// The cursor that points to the first item in this batch of items.
        /// </summary>
        public TKey previous_cursor { get; set; }
    }

    public class LootLockerExtendedPaginationError
    {
        /// <summary>
        /// Which field in the pagination that this error relates to
        /// </summary>
        public string field { get; set; }
        /// <summary>
        /// The error message in question
        /// </summary>
        public string message { get; set; }
    }

    public class LootLockerExtendedPagination
    {
        /// <summary>
        /// How many entries in total exists in the paginated list
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// How many entries (counting from the beginning of the paginated list) from the first entry that the current page starts at
        /// </summary>
        public int offset { get; set; }
        /// <summary>
        /// Number of entries on each page
        /// </summary>
        public int per_page { get; set; }
        /// <summary>
        /// The page index to use for fetching the last page of entries
        /// </summary>
        public int last_page { get; set; }
        /// <summary>
        /// The page index used for fetching this page of entries
        /// </summary>
        public int current_page { get; set; }
        /// <summary>
        /// The page index to use for fetching the page of entries immediately succeeding this page of entries
        /// </summary>
        public int? next_page { get; set; }
        /// <summary>
        /// The page index to use for fetching the page of entries immediately preceding this page of entries
        /// </summary>
        public int? prev_page { get; set; }
        /// <summary>
        /// List of pagination errors (if any). These are errors specifically related to the pagination of the entry set.
        /// </summary>
        public LootLockerExtendedPaginationError[] errors { get; set; }
    }

    /// <summary>
    /// Convenience factory class for creating some responses that we use often.
    /// </summary>
    public class LootLockerResponseFactory
    {
        /// <summary>
        /// Construct an error response from a network request to send to the client.
        /// </summary>
        public static T NetworkError<T>(string errorMessage, int httpStatusCode) where T : LootLockerResponse, new()
        {
            return new T()
            {
                success = false,
                text = "{ \"message\": \"" + errorMessage + "\"}",
                statusCode = httpStatusCode,
                errorData = new LootLockerErrorData(httpStatusCode, errorMessage)
            };
        }

        /// <summary>
        /// Construct an error response from a client side error to send to the client.
        /// </summary>
        public static T ClientError<T>(string errorMessage) where T : LootLockerResponse, new()
        {
            return new T()
            {
                success = false,
                text = "{ \"message\": \"" + errorMessage + "\"}",
                statusCode = 0,
                errorData = new LootLockerErrorData
                {
                    message = errorMessage,
                }
            };
        }

        /// <summary>
        /// Construct an error response for token expiration.
        /// </summary>
        public static T TokenExpiredError<T>() where T : LootLockerResponse, new()
        {
            return NetworkError<T>("Token Expired", 401);
        }

        /// <summary>
        /// Construct an error response specifically when the SDK has not been initialized.
        /// </summary>
        public static T SDKNotInitializedError<T>() where T : LootLockerResponse, new()
        {
            return ClientError<T>("The LootLocker SDK has not been initialized, please start a session to call this method");
        }

        /// <summary>
        /// Construct an error response because an unserializable input has been given
        /// </summary>
        public static T InputUnserializableError<T>() where T : LootLockerResponse, new()
        {
            return ClientError<T>("Method parameter could not be serialized");
        }

        /// <summary>
        /// Construct an error response because the rate limit has been hit
        /// </summary>
        public static T RateLimitExceeded<T>(string method, int secondsLeftOfRateLimit) where T : LootLockerResponse, new()
        {
            var error = ClientError<T>($"Your request to {method} was not sent. You are sending too many requests and are being rate limited for {secondsLeftOfRateLimit} seconds");
            error.errorData.retry_after_seconds = secondsLeftOfRateLimit;
            return error;
        }

        /// <summary>
        /// Construct a default constructed successful response of the specified type
        /// </summary>
        public static T EmptySuccess<T>() where T : LootLockerResponse, new()
        {
            T response = new T();
            response.text = LootLockerJson.SerializeObject(response);
            return response;
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
            //Disable local ratelimiter when not targeting production
            if (!LootLockerConfig.IsTargetingProductionEnvironment())
            {
                return false;
            }

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
                onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerResponse>("LootLocker domain key must be set in settings"));

                return;
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("domain-key", LootLockerConfig.current.domainKey);

            if(LootLockerConfig.current.apiKey.StartsWith("dev_"))
            {
                headers.Add("is-development", "true");
            }
            
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
            this.callerRole = callerRole;
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
            this.callerRole = callerRole;
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
}
