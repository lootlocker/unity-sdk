using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using System;
using System.Collections.Generic;

#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json.Linq;
#endif

//==================================================
// Enum Definitions
//==================================================
namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// Possible metadata sources
    /// </summary>
    public enum LootLockerMetadataSources
    {
        reward = 0,
        leaderboard = 1,
        catalog_item = 2,
        progression = 3,
    };

    /// <summary>
    /// Possible metadata types
    /// </summary>
    public enum LootLockerMetadataTypes
    {
        String = 0,
        Number = 1,
        Bool = 2,
        Json = 3,
        Base64 = 4,
    };
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================
    /// <summary>
    ///</summary>
    public class LootLockerMetadataBase64Value
    {
        /// <summary>
        /// The type of content that the base64 string encodes. Could be for example "image/jpeg" if it is a base64 encoded jpeg, or "application/x-redacted" if loading of files has been disabled
        ///</summary>
        public string content_type { get; set; }
        /// <summary>
        /// The encoded content in the form of a Base64 String. If this is unexpectedly empty, check if Content_type is set to "application/x-redacted". If it is, then the request for metadata was made with the ignoreFiles parameter set to true
        ///</summary>
        public string content { get; set; }

    }

    /// <summary>
    ///</summary>
    public class LootLockerMetadataEntry
    {
        /// <summary>
        /// The value of the metadata in string format (unparsed). To use this as the type specified by the Type field, parse it using the corresponding GetValueAs<Type> method in C++ or using the LootLockerMetadataValueParser node in Blueprints.
        ///</summary>
        public string value { get; set; }
        /// <summary>
        /// The metadata key
        ///</summary>
        public string key { get; set; }
        /// <summary>
        /// The type of value this metadata contains. Use this to parse the value.
        ///</summary>
        public LootLockerMetadataTypes type { get; set; }
        /// <summary>
        /// List of tags applied to this metadata
        ///</summary>
        public string[] tags { get; set; }

        /// <summary>
        /// Get the value as a String. Returns true if value could be parsed in which case output contains the string value untouched, returns false if parsing failed.
        ///</summary>
        public bool GetValueAsString(out string output)
        {
            output = value;
            return true;
        }
        /// <summary>
        /// Get the value as a double. Returns true if value could be parsed in which case output contains the double, returns false if parsing failed which can happen if the value is not numeric, the conversion under or overflows, or the string value precision is larger than can be dealt within a double.
        ///</summary>
        public bool GetValueAsDouble(out double output)
        {
            return double.TryParse(value, out output);
        }
        /// <summary>
        /// Get the value as an integer. Returns true if value could be parsed in which case output contains the int, returns false if parsing failed which can happen if the value is not numeric or the conversion under or overflows
        ///</summary>
        public bool GetValueAsInteger(out int output)
        {
            return int.TryParse(value, out output);
        }
        /// <summary>
        /// Get the value as a boolean. Returns true if value could be parsed in which case output contains the bool, returns false if parsing failed which can happen if the string is not a convertible to a boolean.
        ///</summary>
        public bool GetValueAsBool(out bool output)
        {
            return bool.TryParse(value, out output);
        }

        /// <summary>
        /// Get the value as the specified type. Returns true if value could be parsed in which case output contains the parsed object, returns false if parsing failed which can happen if the value is not a valid json object string convertible to the specified object.
        ///</summary>
        public bool GetValueAsType<T>(out T output)
        {
            return LootLockerJson.TryDeserializeObject<T>(value, out output);
        }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
        /// <summary>
        /// Get the value as a Json Object. Returns true if value could be parsed in which case output contains the Json Object, returns false if parsing failed which can happen if the value is not a valid json object string.
        ///</summary>
        public bool GetValueAsJson(out JObject output)
        {
            return LootLockerJson.TryDeserializeObject<JObject>(value, out output);
        }

        /// <summary>
        /// Get the value as a Json Array. Returns true if value could be parsed in which case output contains the Json Array, returns false if parsing failed which can happen if the value is not a valid json array string.
        ///</summary>
        public bool GetValueAsJsonArray(out object[] output)
        {
            return LootLockerJson.TryDeserializeObject<object[]>(value, out output);
        }
#else

        /// <summary>
        /// Get the value as a Json Object. Returns true if value could be parsed in which case output contains the Json Object, returns false if parsing failed which can happen if the value is not a valid json object string.
        ///</summary>
        public bool GetValueAsJson(out Dictionary<string, object> output)
        {
            return LootLockerJson.TryDeserializeObject<Dictionary<string, object>>(value, out output);
        }

        /// <summary>
        /// Get the value as a Json Array. Returns true if value could be parsed in which case output contains the Json Array, returns false if parsing failed which can happen if the value is not a valid json array string.
        ///</summary>
        public bool GetValueAsJsonArray(out object[] output)
        {
            return LootLockerJson.TryDeserializeObject<object[]>(value, out output);
        }
#endif
        /// <summary>
        /// Get the value as a LootLockerMetadataBase64Value object. Returns true if value could be parsed in which case output contains the FLootLockerMetadataBase64Value, returns false if parsing failed.
        ///</summary>
        public bool GetValueAsBase64(out LootLockerMetadataBase64Value output)
        {
            return LootLockerJson.TryDeserializeObject<LootLockerMetadataBase64Value>(value, out output);
        }
    }

    //==================================================
    // Request Definitions
    //==================================================

    //==================================================
    // Response Definitions
    //==================================================
    /// <summary>
    /// </summary>
    public class LootLockerListMetadataResponse : LootLockerResponse
    {
        /// <summary>
        /// List of metadata entries on this page of metadata
        /// </summary>
        public LootLockerMetadataEntry[] entries { get; set; }
        /// <summary>
        /// Pagination data for this set of metadata entries
        /// </summary>
        public LootLockerExtendedPagination pagination { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerGetMetadataResponse : LootLockerResponse
    {
        /// <summary>
        /// The requested metadata entry
        /// </summary>
        public LootLockerMetadataEntry entry { get; set; }
    };
}

//==================================================
// API Class Definition
//==================================================

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void ListMetadata(LootLockerMetadataSources Source, string SourceID, int Page, int PerPage, string[] Tags, bool ignoreFiles, Action<LootLockerListMetadataResponse> onComplete)
        {
            string formattedEndpoint = string.Format(LootLockerEndPoints.listMetadata.endPoint, Source.ToString(), SourceID);

            string queryParams = "";
            if (Page > 0) queryParams += $"page={Page}&";
            if (PerPage > 0) queryParams += $"per_page={PerPage}&";
            if (Tags?.Length > 0) {
                string TagList = "";
                int index = 0;
                foreach (string tag in Tags)
                {
                    if (index >= 0)
                    {
                        TagList += ",";
                    }
                    TagList += tag;
                    ++index;
                }
                queryParams += $"tags={TagList}&";
            }
            if (ignoreFiles) queryParams += $"ignoreFiles=true";

            if (!string.IsNullOrEmpty(queryParams)) queryParams = $"?{queryParams}";

            LootLockerServerRequest.CallAPI(formattedEndpoint, LootLockerEndPoints.listMetadata.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
        public static void GetMetadata(LootLockerMetadataSources Source, string SourceID, string Key, bool ignoreFiles, Action<LootLockerGetMetadataResponse> onComplete)
        {
            string formattedEndpoint = string.Format(LootLockerEndPoints.listMetadata.endPoint, Source.ToString(), SourceID);

            string queryParams = "";
            if (!string.IsNullOrEmpty(Key)) queryParams += $"key={Key}&";
            if (ignoreFiles) queryParams += $"ignoreFiles=true";

            if (!string.IsNullOrEmpty(queryParams)) queryParams = $"?{queryParams}";

            LootLockerServerRequest.CallAPI(formattedEndpoint, LootLockerEndPoints.listMetadata.httpMethod, onComplete: 
                (serverResponse) => {
                    var ListResponse = LootLockerResponse.Deserialize<LootLockerListMetadataResponse>(serverResponse);
                    LootLockerGetMetadataResponse SingleMetadataResponse = new()
                    {
                        success = ListResponse.success,
                        statusCode = ListResponse.statusCode,
                        text = ListResponse.text,
                        EventId = ListResponse.EventId,
                        errorData = ListResponse.errorData,
                        entry = ListResponse.entries != null ? ListResponse.entries.Length > 0 ? ListResponse.entries[0] : null : null
                    };
                    onComplete(SingleMetadataResponse);
                }
            );
        }
    }
}
