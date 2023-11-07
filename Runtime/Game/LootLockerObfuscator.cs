using LLlibs.ZeroDepJson;
using System;
using System.Collections.Generic;
using System.Text;

namespace LootLocker
{
    public class LootLockerObfuscator
    {
        private struct ObfuscationDetails
        {
            public string key { get; set; }
            public char replacementChar { get; set; }
            public int visibleCharsFromBeginning { get; set; }
            public int visibleCharsFromEnd { get; set; }
            public bool hideCharactersForShortStrings { get; set; }

            public ObfuscationDetails(string key, char replacementChar = '*', int visibleCharsFromBeginning = 3, int visibleCharsFromEnd = 3, bool hideCharactersForShortStrings = true)
            {
                this.key = key;
                this.replacementChar = replacementChar;
                this.visibleCharsFromBeginning = visibleCharsFromBeginning;
                this.visibleCharsFromEnd = visibleCharsFromEnd;
                this.hideCharactersForShortStrings = hideCharactersForShortStrings;
            }
        }

        private static readonly List<ObfuscationDetails> FieldsToObfuscate = new List<ObfuscationDetails>
        {
            new ObfuscationDetails("game_key", '*', 4, 3, false),
            new ObfuscationDetails("email"),
            new ObfuscationDetails("password", '*', 0, 0),
            new ObfuscationDetails("domain_key"),
            new ObfuscationDetails("session_token"),
            new ObfuscationDetails("token")
        };

        public static string ObfuscateJsonStringForLogging(string json)
        {
#if LOOTLOCKER_USE_NEWTONSOFTJSON
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }
            
            JObject jsonObject;
            try
            {
                jsonObject = JObject.Parse(json);
            }
            catch (JsonReaderException)
            {
                return json;
            }
            ;
            if (jsonObject.HasValues)
            {
                foreach (ObfuscationDetails obfuscationInfo in FieldsToObfuscate)
                {
                    string valueToObfuscate;
                    try
                    {
                        JToken jsonValue;
                        jsonObject.TryGetValue(obfuscationInfo.key, StringComparison.Ordinal, out jsonValue);
                        if (jsonValue == null || (jsonValue.Type != JTokenType.String && jsonValue.Type != JTokenType.Integer))
                            continue;
                        valueToObfuscate = jsonValue.ToString();
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(valueToObfuscate))
                        continue;

                    if (valueToObfuscate.Equals("null", StringComparison.Ordinal))
                        continue;

                    int replaceFrom = 0;
                    int replaceTo = valueToObfuscate.Length;

                    // Deal with short strings
                    if (valueToObfuscate.Length <= obfuscationInfo.visibleCharsFromBeginning + obfuscationInfo.visibleCharsFromEnd)
                    {
                        if (!obfuscationInfo.hideCharactersForShortStrings) // Hide nothing, else hide everything
                            continue;
                    }
                    // Replace in
                    else
                    {
                        replaceFrom += obfuscationInfo.visibleCharsFromBeginning;
                        replaceTo -= obfuscationInfo.visibleCharsFromEnd;
                    }

                    StringBuilder replacement = new StringBuilder();
                    replacement.Append(obfuscationInfo.replacementChar, replaceTo - replaceFrom);
                    StringBuilder obfuscatedValue = new StringBuilder(valueToObfuscate);
                    obfuscatedValue.Remove(replaceFrom, replacement.Length);
                    obfuscatedValue.Insert(replaceFrom, replacement.ToString());
                    jsonObject[obfuscationInfo.key] = obfuscatedValue.ToString();
                }
            }

            return LootLockerJson.SerializeObject(jsonObject);
            
#else //LOOTLOCKER_USE_NEWTONSOFTJSON
            if (string.IsNullOrEmpty(json) || json.Equals("{}"))
            {
                return json;
            }

            Dictionary<string, object> jsonObject = null;
            try
            {
                jsonObject = Json.Deserialize(json) as Dictionary<string, object>;
            }
            catch (JsonException)
            {
                return json;
            }

            if (jsonObject != null && jsonObject.Count > 0)
            {
                foreach (ObfuscationDetails obfuscationInfo in FieldsToObfuscate)
                {
                    string valueToObfuscate;
                    try
                    {
                        if (!jsonObject.ContainsKey(obfuscationInfo.key))
                        {
                            continue;
                        }

                        valueToObfuscate = Json.Serialize(jsonObject[obfuscationInfo.key]);
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(valueToObfuscate))
                        continue;

                    if (valueToObfuscate.Equals("null", StringComparison.Ordinal))
                        continue;

                    int replaceFrom = 0;
                    int replaceTo = valueToObfuscate.Length;

                    // Deal with short strings
                    if (valueToObfuscate.Length <= obfuscationInfo.visibleCharsFromBeginning + obfuscationInfo.visibleCharsFromEnd)
                    {
                        if (!obfuscationInfo.hideCharactersForShortStrings) // Hide nothing, else hide everything
                            continue;
                    }
                    // Replace in
                    else
                    {
                        replaceFrom += obfuscationInfo.visibleCharsFromBeginning;
                        replaceTo -= obfuscationInfo.visibleCharsFromEnd;
                    }

                    StringBuilder replacement = new StringBuilder();
                    replacement.Append(obfuscationInfo.replacementChar, replaceTo - replaceFrom);
                    StringBuilder obfuscatedValue = new StringBuilder(valueToObfuscate);
                    obfuscatedValue.Remove(replaceFrom, replacement.Length);
                    obfuscatedValue.Insert(replaceFrom, replacement.ToString());
                    jsonObject[obfuscationInfo.key] = obfuscatedValue.ToString();
                }
            }

            return LootLockerJson.SerializeObject(jsonObject);
#endif //LOOTLOCKER_USE_NEWTONSOFTJSON
        }
    }
}