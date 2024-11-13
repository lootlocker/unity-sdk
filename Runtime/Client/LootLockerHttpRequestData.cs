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
    public struct LootLockerHTTPRequestData
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
        /// The callback action for handling responses
        /// </summary>
        public Action<LootLockerResponse> ResponseCallback { get; set; }
    }

    public class LootLockerHTTPRequestContent
    {
        public LootLockerHTTPRequestContent(LootLockerHTTPRequestDataType type = LootLockerHTTPRequestDataType.EMPTY)
        {
            this.dataType = type;
        }
        public LootLockerHTTPRequestDataType dataType { get; set; }
    }

    public class LootLockerJsonBodyRequestContent : LootLockerHTTPRequestContent
    {
        public LootLockerJsonBodyRequestContent(string jsonBody) : base(LootLockerHTTPRequestDataType.JSON)
        {
            this.jsonBody = jsonBody;
        }
        public string jsonBody { get; set; }
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
    }
}
