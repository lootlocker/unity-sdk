using System;
using System.Collections.Generic;
using System.Text;
#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using LLlibs.ZeroDepJson;
#endif

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
            new ObfuscationDetails("game_api_key", '*', 4, 3, false),
            new ObfuscationDetails("api_key", '*', 4, 3, false),
            new ObfuscationDetails("email"),
            new ObfuscationDetails("password", '*', 0, 0),
            new ObfuscationDetails("domain_key"),
            new ObfuscationDetails("session_token"),
            new ObfuscationDetails("token"),
            // Auth & session credentials
            new ObfuscationDetails("steam_ticket"),
            new ObfuscationDetails("id_token"),
            new ObfuscationDetails("auth_code"),
            new ObfuscationDetails("apple_authorization_code"),
            new ObfuscationDetails("xbox_user_token"),
            new ObfuscationDetails("nsa_id_token"),
            new ObfuscationDetails("access_token"),
            new ObfuscationDetails("refresh_token"),
            new ObfuscationDetails("nonce"),
            new ObfuscationDetails("signature"),
            new ObfuscationDetails("salt"),
            new ObfuscationDetails("source_token"),
            new ObfuscationDetails("target_token"),
            // Purchase / IAP credentials
            new ObfuscationDetails("purchase_token"),
            new ObfuscationDetails("bearer_token"),
            new ObfuscationDetails("user_collections_id"),
            new ObfuscationDetails("service_ticket"),
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

        /// <summary>
        /// Redacts signed download URLs from a JSON body before it is logged.
        ///
        /// Player file responses carry a short-lived signed URL in "url". That URL is a bearer
        /// credential: anyone holding it can download the file until it expires, so it must never
        /// reach logs or the log UI. An object is treated as carrying a signed download URL when it
        /// has "url_expires_at" or the player file marker "requires_download_link". The marker is
        /// needed because the file revision payload carries a signed "url" without an expiry field.
        /// Scoping on those markers leaves ordinary URLs in other responses intact.
        /// </summary>
        public static string RedactSignedDownloadUrlsForLogging(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
            JToken token;
            try
            {
                token = JToken.Parse(json);
            }
            catch (JsonReaderException)
            {
                return json;
            }

            bool redacted = false;
            RedactSignedDownloadUrlsInToken(token, ref redacted);
            return redacted ? token.ToString(Formatting.None) : json;
#else //LOOTLOCKER_USE_NEWTONSOFTJSON
            object parsed;
            try
            {
                parsed = Json.Deserialize(json);
            }
            catch (JsonException)
            {
                return json;
            }

            bool redacted = false;
            RedactSignedDownloadUrlsInValue(parsed, ref redacted);
            return redacted ? LootLockerJson.SerializeObject(parsed) : json;
#endif //LOOTLOCKER_USE_NEWTONSOFTJSON
        }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
        private static void RedactSignedDownloadUrlsInToken(JToken token, ref bool redacted)
        {
            if (token == null)
            {
                return;
            }

            if (token.Type == JTokenType.Object)
            {
                JObject obj = (JObject)token;
                foreach (JProperty property in obj.Properties())
                {
                    RedactSignedDownloadUrlsInToken(property.Value, ref redacted);
                }

                // Only redact "url" on objects that carry a signed download URL marker, which leaves
                // ordinary URLs in other responses intact.
                bool carriesSignedDownloadUrl = obj["url_expires_at"] != null || obj["requires_download_link"] != null;
                JToken url = obj["url"];
                if (carriesSignedDownloadUrl && url != null && url.Type == JTokenType.String && !string.IsNullOrEmpty(url.ToString()))
                {
                    obj["url"] = "[REDACTED]";
                    redacted = true;
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (JToken element in token)
                {
                    RedactSignedDownloadUrlsInToken(element, ref redacted);
                }
            }
        }
#else //LOOTLOCKER_USE_NEWTONSOFTJSON
        private static void RedactSignedDownloadUrlsInValue(object value, ref bool redacted)
        {
            if (value is Dictionary<string, object> obj)
            {
                foreach (object child in obj.Values)
                {
                    RedactSignedDownloadUrlsInValue(child, ref redacted);
                }

                // Only redact "url" on objects that carry a signed download URL marker, which leaves
                // ordinary URLs in other responses intact.
                bool carriesSignedDownloadUrl = obj.ContainsKey("url_expires_at") || obj.ContainsKey("requires_download_link");
                if (carriesSignedDownloadUrl && obj.ContainsKey("url") && obj["url"] is string url && !string.IsNullOrEmpty(url))
                {
                    obj["url"] = "[REDACTED]";
                    redacted = true;
                }
            }
            else if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                foreach (object element in enumerable)
                {
                    RedactSignedDownloadUrlsInValue(element, ref redacted);
                }
            }
        }
#endif //LOOTLOCKER_USE_NEWTONSOFTJSON
    }
}
