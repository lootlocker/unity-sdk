using System;
using LootLocker.Requests;

namespace LootLocker.Requests
{
    /// <summary>
    /// Internal DTO mapping LootLockerFailedRequestReport to snake_case field names for the error-report API.
    /// </summary>
    class LootLockerErrorReportBody
    {
        /// <summary>
        /// A description of the error provided by the user, useful for debugging and support. Optional but recommended when submitting error reports to help with troubleshooting.
        /// </summary>
        public string user_description { get; set; }
        /// <summary>
        /// The id of the request on the client side
        /// </summary>
        public string client_request_id { get; set; }
        /// <summary>
        /// The id of the request on the server side
        /// </summary>
        public string server_request_id { get; set; }
        /// <summary>
        /// The id of the trace on the server side, useful for debugging with support
        /// </summary>
        public string trace_id { get; set; }
        /// <summary>
        /// The status code of the response
        /// </summary>
        public int status_code { get; set; }
        /// <summary>
        /// The error message provided by the server
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// The endpoint that was called
        /// </summary>
        public string endpoint { get; set; }
        /// <summary>
        /// The HTTP method used for the request (e.g. GET, POST, etc.)
        /// </summary>
        public string http_method { get; set; }
        /// <summary>
        /// The body of the response
        /// </summary>
        public string response_body { get; set; }
        /// <summary>
        /// The headers of the response
        /// </summary>
        public string[] response_headers { get; set; }
        /// <summary>
        /// The body of the request
        /// </summary>
        public string request_body { get; set; }
        /// <summary>
        /// The headers of the request
        /// </summary>
        public string[] request_headers { get; set; }
        /// <summary>
        /// The number of times this request was retried
        /// </summary>
        public int retry_attempts { get; set; }
        /// <summary>
        /// The duration of the request's round trip in seconds
        /// </summary>
        public float request_duration_seconds { get; set; }
        /// <summary>
        /// The timestamp of when the error occurred on the server
        /// </summary>
        public string server_timestamp { get; set; }
        /// <summary>
        /// The timestamp of when the error occurred on the client
        /// </summary>
        public string client_timestamp { get; set; }
        /// <summary>
        /// The player's ULID
        /// </summary>
        public string player_ulid { get; set; }
        /// <summary>
        /// The version of the LootLocker SDK used
        /// </summary>
        public string sdk_version { get; set; }
    }
}