using System;
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
            }
            catch (Exception)
            {
                output = default(T);
                return false;
            }
        }
#endif //LOOTLOCKER_USE_NEWTONSOFTJSON
    }
}
