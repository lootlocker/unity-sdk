namespace LootLocker
{
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
}
