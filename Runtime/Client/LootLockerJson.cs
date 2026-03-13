using System;
using System.Collections.Generic;

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

namespace LootLocker
{
    /// <summary>
    /// Generic result structure for streamed JSON responses.
    /// Contains the count and array of parsed objects from a streaming endpoint.
    /// </summary>
    /// <typeparam name="T">The type of objects in the streamed response</typeparam>
    public struct LootLockerStreamedResponse<T>
    {
        public int streamedObjectCount;
        public T[] objects;

        public LootLockerStreamedResponse(int count, T[] objects)
        {
            this.streamedObjectCount = count;
            this.objects = objects;
        }
    }

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
        public static readonly JsonOptions Indented = new JsonOptions((JsonSerializationOptions.Default | JsonSerializationOptions.EnumAsText) & ~JsonSerializationOptions.SkipGetOnly);
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

        public static string SerializeObjectArray(object[] obj)
        {
            return SerializeObjectArray(obj, LootLockerJsonSettings.Default);
        }

        public static string SerializeObjectArray(object[] obj, JsonSerializerSettings settings)
        {
            string jsonArray = "[";
            for (int i = 0; i < obj.Length; i++)
            {
                jsonArray += JsonConvert.SerializeObject(obj[i], settings ?? LootLockerJsonSettings.Default);
                if (i < obj.Length - 1)
                    jsonArray += ",";
            }
            jsonArray += "]";
            return jsonArray;
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

        public static string PrettifyJsonString(string json)
        {
            try
            {
                var parsedJson = DeserializeObject<object>(json);
                var tempSettings = new JsonSerializerSettings
                {
                    ContractResolver = LootLockerJsonSettings.Default.ContractResolver,
                    Converters = LootLockerJsonSettings.Default.Converters,
                    Formatting = Formatting.Indented
                };
                return SerializeObject(parsedJson, tempSettings);
            }
            catch (Exception)
            {
                return json;
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

        public static string SerializeObjectArray(object[] obj)
        {
            return SerializeObjectArray(obj, LootLockerJsonSettings.Default);
        }

        public static string SerializeObjectArray(object[] obj, JsonOptions options)
        {
            string jsonArray = "[";
            for (int i = 0; i < obj.Length; i++)
            {
                jsonArray += Json.Serialize(obj[i], options ?? LootLockerJsonSettings.Default);
                if (i < obj.Length - 1)
                    jsonArray += ",";
            }
            jsonArray += "]";
            return jsonArray;
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
                return output != null || string.IsNullOrEmpty(json) || json.Equals("\"\"") || json.Equals("null");
            }
            catch (Exception)
            {
                output = default(T);
                return false;
            }
        }

        public static Dictionary<string, object>[] DeserializeJsonObjectArrayToDictionaryArray(string json)
        {
            return Json.Deserialize<Dictionary<string, object>[]>(json, LootLockerJsonSettings.Default);
        }

        /// <summary>
        /// Parses a streamed JSON response containing concatenated objects with metadata.
        /// The expected format is: {obj1},{obj2},...,{objN},{"streamedObjectCount":N}
        /// </summary>
        /// <typeparam name="T">The type to deserialize the streamed objects into</typeparam>
        /// <param name="streamedJson">The raw streamed JSON string</param>
        /// <returns>A structured result containing the count and parsed objects</returns>
        public static LootLockerStreamedResponse<T> DeserializeStreamedResponse<T>(string streamedJson)
        {
            if (string.IsNullOrEmpty(streamedJson))
            {
                return new LootLockerStreamedResponse<T>(0, new T[0]);
            }

            try
            {
                // Convert concatenated JSON objects into a valid JSON array format
                string jsonArray = "[" + streamedJson + "]";
                Dictionary<string, object>[] streamedObjects = DeserializeJsonObjectArrayToDictionaryArray(jsonArray);
                
                if (streamedObjects == null || streamedObjects.Length == 0)
                {
                    return new LootLockerStreamedResponse<T>(0, new T[0]);
                }

                // Extract metadata from the last object
                var metadataObject = streamedObjects[streamedObjects.Length - 1];
                int streamedObjectCount = ExtractStreamedObjectCount(metadataObject);
                
                // Parse the actual data objects (excluding the metadata object)
                T[] parsedObjects = ParseStreamedObjects<T>(streamedObjects, streamedObjectCount);
                
                return new LootLockerStreamedResponse<T>(streamedObjectCount, parsedObjects);
            }
            catch (Exception)
            {
                return new LootLockerStreamedResponse<T>(0, new T[0]);
            }
        }

        private static int ExtractStreamedObjectCount(Dictionary<string, object> metadataObject)
        {
            if (metadataObject?.ContainsKey("streamedObjectCount") == true && 
                metadataObject["streamedObjectCount"] != null)
            {
                return Convert.ToInt32(metadataObject["streamedObjectCount"]);
            }
            return 0;
        }

        private static T[] ParseStreamedObjects<T>(Dictionary<string, object>[] streamedObjects, int count)
        {
            var parsedObjects = new List<T>();
            
            int objectsToParse = Math.Min(count, streamedObjects.Length - 1); // Exclude metadata object
            for (int i = 0; i < objectsToParse; i++)
            {
                try
                {
                    string objectJson = SerializeObject(streamedObjects[i]);
                    var parsedObject = DeserializeObject<T>(objectJson);
                    if (parsedObject != null)
                    {
                        parsedObjects.Add(parsedObject);
                    }
                }
                catch (Exception)
                {
                    // Skip malformed objects rather than failing the entire response
                    continue;
                }
            }
            
            return parsedObjects.ToArray();
        }

        public static string PrettifyJsonString(string json)
        {
            try
            {
                var parsedJson = DeserializeObject<object>(json);
                var indentedOptions = LootLockerJsonSettings.Default.Clone();
                indentedOptions.FormattingTab = "  ";
                return Json.SerializeFormatted(parsedJson, indentedOptions);
            }
            catch (Exception)
            {
                return json;
            }
        }
#endif //LOOTLOCKER_USE_NEWTONSOFTJSON
    }
}
