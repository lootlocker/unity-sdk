using System;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;
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
        currency = 4,
        player = 5,
        self = 6,
        asset = 7,
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

    /// <summary>
    /// Possible metadata actions
    /// </summary>
    public enum LootLockerMetadataActions
    {
        create = 0,
        update = 1,
        delete = 2
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
        /// The value of the metadata in base object format (unparsed). To use this as the type specified by the Type field, parse it using the corresponding TryGetValueAs<Type> method in C++ or using the LootLockerMetadataValueParser node in Blueprints.
        ///</summary>
        public object value { get; set; }
        /// <summary>
        /// The metadata key
        ///</summary>
        public string key { get; set; }
        /// <summary>
        /// The type of value this metadata contains. Use this to know how to parse the value.
        ///</summary>
        public LootLockerMetadataTypes type { get; set; }
        /// <summary>
        /// List of tags applied to this metadata entry
        ///</summary>
        public string[] tags { get; set; }
        /// <summary>
        /// The access level set for this metadata entry. Valid values are game_api.read, game_api.write and player.read (only applicable for player metadata and means that the metadata entry is readable for players except the owner), though no values are required.
        /// Note that different sources can allow or disallow a subset of these values.
        /// </summary>
        public string[] access { get; set; }
        /// <summary>
        /// Get the value as a String. Returns true if value could be parsed in which case output contains the value in string format, returns false if parsing failed.
        ///</summary>
        public bool TryGetValueAsString(out string output)
        {
            output = value.ToString();
            return true;
        }
        /// <summary>
        /// Get the value as a double. Returns true if value could be parsed in which case output contains the value in double format, returns false if parsing failed which can happen if the value is not numeric, the conversion under or overflows, or the string value precision is larger than can be dealt within a double.
        ///</summary>
        public bool TryGetValueAsDouble(out double output)
        {
            try
            {
                string doubleAsString = value.ToString();
                return double.TryParse(doubleAsString, out output);
            }
            catch (InvalidCastException)
            {
                output = 0.0;
                return false;
            }
        }
        /// <summary>
        /// Get the value as an integer. Returns true if value could be parsed in which case output contains the value in integer format, returns false if parsing failed which can happen if the value is not numeric or the conversion under or overflows
        ///</summary>
        public bool TryGetValueAsInteger(out int output)
        {
            try
            {
                string intAsString = value.ToString();
                return int.TryParse(intAsString, out output);
            }
            catch (InvalidCastException)
            {
                output = 0;
                return false;
            }
        }
        /// <summary>
        /// Get the value as a boolean. Returns true if value could be parsed in which case output contains the value in boolean format, returns false if parsing failed which can happen if the string is not a convertible to a boolean.
        ///</summary>
        public bool TryGetValueAsBool(out bool output)
        {
            try
            {
                string boolAsString = value.ToString();
                return bool.TryParse(boolAsString, out output);
            }
            catch (InvalidCastException)
            {
                output = false;
                return false;
            }
        }

        /// <summary>
        /// Get the value as the specified type. Returns true if value could be parsed in which case output contains the parsed object, returns false if parsing failed which can happen if the value is not a valid json object string convertible to the specified object.
        ///</summary>
        public bool TryGetValueAsType<T>(out T output)
        {
            return LootLockerJson.TryDeserializeObject<T>(LootLockerJson.SerializeObject(value), out output);
        }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
        /// <summary>
        /// Get the value as a Json Object. Returns true if value could be parsed in which case output contains the value in Json Object format, returns false if parsing failed which can happen if the value is not a valid json object string.
        ///</summary>
        public bool TryGetValueAsJson(out JObject output)
        {
            return TryGetValueAsType(out output);
        }

        /// <summary>
        /// Get the value as a Json Array. Returns true if value could be parsed in which case output contains the value in Json Array format, returns false if parsing failed which can happen if the value is not a valid json array string.
        ///</summary>
        public bool TryGetValueAsJsonArray(out JArray output)
        {
            output = JArray.Parse(value.ToString());
            return output != null;
        }
#else

        /// <summary>
        /// Get the value as a Json Object (a dictionary of string keys to object values). Returns true if value could be parsed in which case output contains the value in Json Object format, returns false if parsing failed which can happen if the value is not a valid json object string.
        ///</summary>
        public bool TryGetValueAsJson(out Dictionary<string, object> output)
        {
            return TryGetValueAsType(out output) && output != null;
        }

        /// <summary>
        /// Get the value as a Json Array. Returns true if value could be parsed in which case output contains the value in Json Array format, returns false if parsing failed which can happen if the value is not a valid json array string.
        ///</summary>
        public bool TryGetValueAsJsonArray(out object[] output)
        {
            if (value.GetType() == typeof(object[]))
            {
                output = (object[])value;
                return true;
            }
            return LootLockerJson.TryDeserializeObject<object[]>(value.ToString(), out output);
        }
#endif
        /// <summary>
        /// Get the value as a LootLockerMetadataBase64Value object. Returns true if value could be parsed in which case output contains the FLootLockerMetadataBase64Value, returns false if parsing failed.
        ///</summary>
        public bool TryGetValueAsBase64(out LootLockerMetadataBase64Value output)
        {
            return TryGetValueAsType(out output);
        }
    }

    /// <summary>
    /// </summary>
    public class LootLockerMetadataSourceAndKeys
    {
        /// <summary>
        /// The type of source that the source id refers to
        /// </summary>
        public LootLockerMetadataSources source { get; set; }
        /// <summary>
        /// The id of the specific source that the set operation was taken on
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// A list of keys existing on the specified source
        /// </summary>
        public string[] keys { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerMetadataSourceAndEntries
    {
        /// <summary>
        /// The type of source that the source id refers to
        /// </summary>
        public LootLockerMetadataSources source { get; set; }
        /// <summary>
        /// The id of the specific source that the set operation was taken on
        /// </summary>
        public string source_id { get; set; }
        /// <summary>
        /// A list of keys existing on the specified source
        /// </summary>
        public LootLockerMetadataEntry[] entries { get; set; }
    }

    public class LootLockerMetadataOperationErrorKeyTypePair
    {
        /// <summary>
        /// The metadata key that the operation error refers to
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// The type of value that the set operation was for
        /// </summary>
        public LootLockerMetadataTypes type { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerMetadataOperationError
    {
        /// <summary>
        /// The type of action that this metadata operation was
        /// </summary>
        public LootLockerMetadataActions action { get; set; }
        /// <summary>
        /// The error message describing why this metadata set operation failed
        /// </summary>
        public string error { get; set; }
        /// <summary>
        /// The key and type of value that the operation was for
        /// </summary>
        public LootLockerMetadataOperationErrorKeyTypePair entry { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerMetadataOperation : LootLockerMetadataEntry
    {
        /// <summary>
        /// The type of action to perform for this metadata operation
        /// </summary>
        public LootLockerMetadataActions action { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerInternalMetadataOperationWithStringEnums : LootLockerMetadataOperation
    {
        public LootLockerInternalMetadataOperationWithStringEnums(LootLockerMetadataOperation other)
        {
            this.key = other.key;
            this.type = other.type.ToString().ToLower();
            this.value = other.value;
            this.tags = other.tags;
            this.access = other.access;
            this.action = other.action.ToString().ToLower();
        }

        public new string type { get; set; }
        public new string action { get; set; }


    }

    //==================================================
    // Request Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerGetMultisourceMetadataRequest
    {
        /// <summary>
        /// The source & key combos to get
        /// </summary>
        public LootLockerMetadataSourceAndKeys[] sources { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerInternalMetadataOperationRequest
    {
        /// <summary>
        /// Whether or not this operation is for metadata on the current player
        /// </summary>
        public bool self { get; set; }
        /// <summary>
        /// The type of source that the source id refers to
        /// </summary>
        public string source { get; set; }
        /// <summary>
        /// The id of the specific source that the set operation was taken on, note that if source is set to self then this should also be set to "self"
        /// </summary>
        public string source_id { get; set; }
        /// <summary>
        /// List of operations to perform for the given source
        /// </summary>
        public LootLockerInternalMetadataOperationWithStringEnums[] entries { get; set; }
    }

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

    /// <summary>
    /// </summary>
    public class LootLockerGetMultisourceMetadataResponse : LootLockerResponse
    {
        /// <summary>
        /// The requested sources with the requested entries for each source
        /// </summary>
        public LootLockerMetadataSourceAndEntries[] Metadata { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerMetadataOperationsResponse : LootLockerResponse
    {
        /// <summary>
        /// The type of source that the source id refers to
        /// </summary>
        public LootLockerMetadataSources source { get; set; }
        /// <summary>
        /// The id of the specific source that the set operation was taken on, note that if source is set to self then this will also be set to "self"
        /// </summary>
        public string source_id { get; set; }
        /// <summary>
        /// A list of errors (if any) that occurred when executing the provided metadata actions
        /// </summary>
        public LootLockerMetadataOperationError[] errors { get; set; }
    }
}

//==================================================
// API Class Definition
//==================================================

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void ListMetadata(LootLockerMetadataSources Source, string SourceID, int Page, int PerPage, string Key, string[] Tags, bool ignoreFiles, Action<LootLockerListMetadataResponse> onComplete)
        {
            if (Source == LootLockerMetadataSources.self)
            {
                SourceID = "self";
            }
            string formattedEndpoint = string.Format(LootLockerEndPoints.listMetadata.endPoint, Source.ToString(), SourceID);

            string queryParams = "";
            if (Page > 0) queryParams += $"page={Page}&";
            if (PerPage > 0) queryParams += $"per_page={PerPage}&";
            if (!string.IsNullOrEmpty(Key)) queryParams += $"key={Key}&";
            if (Tags?.Length > 0) {
                foreach (string tag in Tags)
                {
                    queryParams += $"tags={tag}&";
                }
            }
            if (ignoreFiles) { queryParams += $"ignore_files=true"; } else { queryParams += $"ignore_files=false"; }

            if (!string.IsNullOrEmpty(queryParams))
            {
                queryParams = $"?{queryParams}";
                formattedEndpoint += queryParams;
            }

            LootLockerServerRequest.CallAPI(formattedEndpoint, LootLockerEndPoints.listMetadata.httpMethod, onComplete:
                (serverResponse) =>
                {
                    var parsedResponse = LootLockerResponse.Deserialize<LootLockerListMetadataResponse>(serverResponse);
                    if(parsedResponse.entries == null)
                    {
                        parsedResponse.entries = new LootLockerMetadataEntry[] {};
                    }
                    onComplete?.Invoke(parsedResponse);
                });
        }

        public static void GetMultisourceMetadata(LootLockerMetadataSourceAndKeys[] SourcesAndKeysToGet, bool ignoreFiles, Action<LootLockerGetMultisourceMetadataResponse> onComplete)
        {
            if (SourcesAndKeysToGet == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGetMultisourceMetadataResponse>());
                return;
            }
            string endpoint = LootLockerEndPoints.getMultisourceMetadata.endPoint;

            string queryParams = "";
            if (ignoreFiles) { queryParams += $"ignore_files=true"; } else { queryParams += $"ignore_files=false"; }

            if (!string.IsNullOrEmpty(queryParams))
            {
                queryParams = $"?{queryParams}";
                endpoint += queryParams;
            }

            foreach (LootLockerMetadataSourceAndKeys sourcePair in SourcesAndKeysToGet)
            {
                if(sourcePair.source == LootLockerMetadataSources.self)
                {
                    sourcePair.id = "self";
                }
            }

            LootLockerGetMultisourceMetadataRequest request = new LootLockerGetMultisourceMetadataRequest { sources = SourcesAndKeysToGet };

            string json = LootLockerJson.SerializeObject(request);
            LootLockerServerRequest.CallAPI(endpoint, LootLockerEndPoints.getMultisourceMetadata.httpMethod, json, onComplete:
                (serverResponse) =>
                {
                    LootLockerResponse.Deserialize<LootLockerGetMultisourceMetadataResponse>(onComplete, serverResponse);
                });
        }

        public static void PerformMetadataOperations(LootLockerMetadataSources Source, string SourceID, List<LootLockerMetadataOperation> OperationsToPerform, Action<LootLockerMetadataOperationsResponse> onComplete)
        {
            if (string.IsNullOrEmpty(SourceID) || OperationsToPerform.Count == 0)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerMetadataOperationsResponse>());
                return;
            }

            List<LootLockerInternalMetadataOperationWithStringEnums> entries = new List<LootLockerInternalMetadataOperationWithStringEnums>();
            foreach(var op in OperationsToPerform)
            {
                entries.Add(new LootLockerInternalMetadataOperationWithStringEnums(op));
            }

            LootLockerInternalMetadataOperationRequest request = new LootLockerInternalMetadataOperationRequest
            {
                self = Source == LootLockerMetadataSources.self,
                source = Source == LootLockerMetadataSources.self ? null : Source.ToString().ToLower(),
                source_id = Source == LootLockerMetadataSources.self ? null : SourceID,
                entries = entries.ToArray()
            };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerServerRequest.CallAPI(LootLockerEndPoints.metadataOperations.endPoint, LootLockerEndPoints.metadataOperations.httpMethod, json, onComplete:
                (serverResponse) =>
                {
                    LootLockerResponse.Deserialize<LootLockerMetadataOperationsResponse>(onComplete, serverResponse);
                });
        }
    }
}
    