using System;
#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
#else
using LLlibs.ZeroDepJson;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LootLocker
{
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
        /// Context for the request
        /// </summary>
        public LootLockerRequestContext requestContext { get; set; }

        /// <summary>
        /// If this request was not a success, this structure holds all the information needed to identify the problem
        /// </summary>
        public LootLockerErrorData errorData { get; set; }

        /// <summary>
        /// inheritdoc added this because unity main thread executing style cut the calling stack and make the event orphan see also calling multiple events 
        /// of the same type makes use unable to identify each one
        /// </summary>
        public string EventId { get; set; } = Guid.NewGuid().ToString();

#if LOOTLOCKER_BETA_ENABLE_ERROR_REPORTING
        /// <summary>
        /// Sends a report about a failed request to be viewable in the LootLocker dashboard.
        /// This is intended to be used in the case where a request fails and you want to send the details of that failure to LootLocker for debugging and tracking purposes. 
        /// It will not work for successful requests, unauthorized requests, or requests that were throttled due to too many requests. 
        /// The request must have failed with a response from the server containing details about the failure in order for this method to work. 
        /// If the request failed without a response from the server, or if the response from the server could not be deserialized properly, then this method will not be able to send a report.
        /// </summary>
        /// <param name="userDescription">A description provided by the user about the failure.</param>
        /// <param name="onComplete">A callback to be invoked when the report has been sent.</param>
        public void ReportFailure(string userDescription, Action<LootLockerResponse> onComplete)
        {
            LootLocker.Requests.LootLockerSDKManager.SendLootLockerErrorReport(userDescription, this, onComplete);
        }
#endif

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
                return LootLockerResponseFactory.ClientError<T>("Unknown error, please check your internet connection.", null);
            }
            else if (serverResponse.errorData != null)
            {
                return new T() { success = false, errorData = serverResponse.errorData, statusCode = serverResponse.statusCode, text = serverResponse.text, requestContext = serverResponse.requestContext };
            }

            var response = LootLockerJson.DeserializeObject<T>(serverResponse.text, options ?? LootLockerJsonSettings.Default) ?? new T();

            response.text = serverResponse.text;
            response.success = serverResponse.success;
            response.errorData = serverResponse.errorData;
            response.requestContext = serverResponse.requestContext;
            response.statusCode = serverResponse.statusCode;
            response.EventId = serverResponse.EventId;

            return response;
        }
    }

    /// <summary>
    /// Convenience factory class for creating some responses that we use often.
    /// </summary>
    public class LootLockerResponseFactory
    {
        /// <summary>
        /// Construct a success response
        /// </summary>
        public static T Success<T>(int statusCode, string responseBody, string forPlayerWithUlid, DateTime? requestTime = null, string requestId = null) where T : LootLockerResponse, new()
        {
            return new T()
            {
                success = true,
                text = responseBody,
                statusCode = statusCode,
                errorData = null,
                requestContext = new LootLockerRequestContext(forPlayerWithUlid, requestTime, requestId)
            };
        }

        /// <summary>
        /// Construct a failure response
        /// </summary>
        public static T Failure<T>(int statusCode, string responseBody, string forPlayerWithUlid, DateTime? requestTime = null, string requestId = null) where T : LootLockerResponse, new()
        {
            return new T()
            {
                success = false,
                text = responseBody,
                statusCode = statusCode,
                errorData = null,
                requestContext = new LootLockerRequestContext(forPlayerWithUlid, requestTime, requestId)
            };
        }

        /// <summary>
        /// Construct a failure response of type T from an existing failure response of any type
        /// </summary>
        public static T FailureResponseConversion<T>(LootLockerResponse failureResponse) where T : LootLockerResponse, new()
        {
            return new T()
            {
                success = failureResponse.success,
                text = failureResponse.text,
                statusCode = failureResponse.statusCode,
                errorData = failureResponse.errorData,
                requestContext = failureResponse.requestContext
            };
        }

        /// <summary>
        /// Construct an error response from a network request to send to the client.
        /// </summary>
        public static T NetworkError<T>(string errorMessage, int httpStatusCode, string forPlayerWithUlid, DateTime? requestTime = null, string requestId = null) where T : LootLockerResponse, new()
        {
            return new T()
            {
                success = false,
                text = "{ \"message\": \"" + errorMessage + "\"}",
                statusCode = httpStatusCode,
                errorData = new LootLockerErrorData(httpStatusCode, errorMessage),
                requestContext = new LootLockerRequestContext(forPlayerWithUlid, requestTime, requestId)
            };
        }

        /// <summary>
        /// Construct an error response from a client side error to send to the client.
        /// </summary>
        public static T ClientError<T>(string errorMessage, string forPlayerWithUlid, DateTime? requestTime = null, string requestId = null) where T : LootLockerResponse, new()
        {
            return new T()
            {
                success = false,
                text = "{ \"message\": \"" + errorMessage + "\"}",
                statusCode = 0,
                errorData = new LootLockerErrorData
                {
                    message = errorMessage,
                },
                requestContext = new LootLockerRequestContext(forPlayerWithUlid, requestTime, requestId)
            };
        }

        /// <summary>
        /// Construct an error response for token expiration.
        /// </summary>
        public static T TokenExpiredError<T>(string forPlayerWithUlid, DateTime? requestTime = null, string requestId = null) where T : LootLockerResponse, new()
        {
            return NetworkError<T>("Token Expired", 401, forPlayerWithUlid, requestTime, requestId);
        }

        /// <summary>
        /// Construct an error response for the request being timed out client side
        /// </summary>
        public static T RequestTimeOut<T>(string forPlayerWithUlid, DateTime? requestTime = null, string requestId = null) where T : LootLockerResponse, new()
        {
            return NetworkError<T>("The request has timed out", 408, forPlayerWithUlid, requestTime, requestId);
        }

        /// <summary>
        /// Construct an error response specifically when the SDK has not been initialized.
        /// </summary>
        public static T SDKNotInitializedError<T>(string forPlayerWithUlid, DateTime? requestTime = null, string requestId = null) where T : LootLockerResponse, new()
        {
            return ClientError<T>("The game client has not been initialized, please start a session to call this method", forPlayerWithUlid, requestTime, requestId);
        }

        /// <summary>
        /// Construct an error response because an unserializable input has been given
        /// </summary>
        public static T InputUnserializableError<T>(string forPlayerWithUlid, DateTime? requestTime = null, string requestId = null) where T : LootLockerResponse, new()
        {
            return ClientError<T>("Method parameter could not be serialized", forPlayerWithUlid, requestTime, requestId);
        }

        /// <summary>
        /// Construct an error response because the rate limit has been hit
        /// </summary>
        public static T RateLimitExceeded<T>(string method, int secondsLeftOfRateLimit, string forPlayerWithUlid, DateTime? requestTime = null, string requestId = null) where T : LootLockerResponse, new()
        {
            var error = ClientError<T>($"Your request to {method} was not sent. You are sending too many requests and are being rate limited for {secondsLeftOfRateLimit} seconds", forPlayerWithUlid, requestTime, requestId);
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
            response.statusCode = 200;
            response.success = true;
            return response;
        }
    }
}