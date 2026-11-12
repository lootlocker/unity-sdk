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
        /// Cheap pre-check so the JSON traversal only runs for bodies that can actually carry a
        /// signed download URL. This is a substring scan, not a parse, and it is the reason the
        /// redaction is affordable on every HTTP response.
        /// </summary>
        private static bool MayContainSignedDownloadUrl(string json)
        {
            return json.IndexOf("url_expires_at", StringComparison.Ordinal) >= 0
                || json.IndexOf("requires_download_link", StringComparison.Ordinal) >= 0
                || json.IndexOf("\"url\"", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Redacts signed download URLs from a JSON body before it is logged.
        ///
        /// Player file responses carry a short-lived signed URL in "url". That URL is a bearer
        /// credential: anyone holding it can download the file until it expires, so it must never
        /// reach logs or the log UI. An object is treated as carrying a signed download URL when it
        /// has "url_expires_at", when "requires_download_link" is true, or when it is a file
        /// revision payload (identified by "revision_id" or a ULID-shaped "id" alongside "size").
        /// The revision case is needed because revision payloads carry a signed "url" with neither
        /// an expiry field nor a true marker. Scoping on those markers leaves ordinary URLs in
        /// other responses intact.
        ///
        /// Cost: a substring pre-check runs first and returns immediately for the overwhelming
        /// majority of responses, which contain no "url" field at all. Only bodies that pass the
        /// pre-check are parsed and walked.
        /// </summary>
        public static string RedactSignedDownloadUrlsForLogging(string json)
        {
            if (string.IsNullOrEmpty(json) || !MayContainSignedDownloadUrl(json))
            {
                return json;
            }

            // A response body can itself be a JSON string (for example LootLockerResponse.text),
            // so redact embedded JSON payloads as well as the outer document.
            string redactedJson = RedactSignedDownloadUrlsInJson(json);
            return RedactEmbeddedJsonStrings(redactedJson);
        }

        private static string RedactSignedDownloadUrlsInJson(string json)
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

        /// <summary>
        /// Walks a parsed document and redacts any string value that is itself a JSON object or
        /// array, so a response body stored as a string cannot smuggle a signed URL past the
        /// object-level redaction.
        /// </summary>
        private static string RedactEmbeddedJsonStrings(string json)
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
            RedactEmbeddedJsonStringsInToken(token, ref redacted);
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
            RedactEmbeddedJsonStringsInValue(parsed, ref redacted);
            return redacted ? LootLockerJson.SerializeObject(parsed) : json;
#endif //LOOTLOCKER_USE_NEWTONSOFTJSON
        }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
        private static void RedactEmbeddedJsonStringsInToken(JToken token, ref bool redacted)
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
                    RedactEmbeddedJsonStringsInToken(property.Value, ref redacted);
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (JToken element in token)
                {
                    RedactEmbeddedJsonStringsInToken(element, ref redacted);
                }
            }
            else if (token.Type == JTokenType.String)
            {
                string value = token.ToString();
                string trimmed = value.TrimStart();
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                {
                    string redactedValue = RedactSignedDownloadUrlsInJson(value);
                    if (!ReferenceEquals(redactedValue, value) && redactedValue != value)
                    {
                        token.Replace(new JValue(redactedValue));
                        redacted = true;
                    }
                }
            }
        }
#else //LOOTLOCKER_USE_NEWTONSOFTJSON
        private static void RedactEmbeddedJsonStringsInValue(object value, ref bool redacted)
        {
            if (value is Dictionary<string, object> obj)
            {
                foreach (string key in new List<string>(obj.Keys))
                {
                    if (obj[key] is string stringValue)
                    {
                        string trimmed = stringValue.TrimStart();
                        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                        {
                            string redactedValue = RedactSignedDownloadUrlsInJson(stringValue);
                            if (redactedValue != stringValue)
                            {
                                obj[key] = redactedValue;
                                redacted = true;
                            }
                        }
                    }
                    else
                    {
                        RedactEmbeddedJsonStringsInValue(obj[key], ref redacted);
                    }
                }
            }
            else if (value is System.Collections.IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is string stringValue)
                    {
                        string trimmed = stringValue.TrimStart();
                        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                        {
                            string redactedValue = RedactSignedDownloadUrlsInJson(stringValue);
                            if (redactedValue != stringValue)
                            {
                                list[i] = redactedValue;
                                redacted = true;
                            }
                        }
                    }
                    else
                    {
                        RedactEmbeddedJsonStringsInValue(list[i], ref redacted);
                    }
                }
            }
        }
#endif //LOOTLOCKER_USE_NEWTONSOFTJSON

        /// <summary>
        /// Whether an object looks like a player file revision payload, which carries a signed
        /// "url" without an expiry field or a true "requires_download_link" marker.
        /// </summary>
        private static bool IsFileRevisionPayload(bool hasRevisionId, string id, bool hasSize)
        {
            if (hasRevisionId)
            {
                return true;
            }

            // A revision's "id" is a ULID string, and it always carries a numeric "size".
            return id != null && id.Length == 26 && hasSize;
        }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
        private static void RedactSignedDownloadUrlsInToken(JToken token, ref bool redacted)        {
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
                // ordinary URLs in other responses intact. "requires_download_link" is present (and
                // false) on ordinary file responses, so it only counts when true. Revision payloads
                // carry a signed "url" with neither an expiry nor a true marker, so they are
                // identified by their shape: a revision id plus a size.
                bool carriesSignedDownloadUrl = obj["url_expires_at"] != null
                    || (obj["requires_download_link"] != null && obj["requires_download_link"].Type == JTokenType.Boolean && obj["requires_download_link"].Value<bool>())
                    || IsFileRevisionPayload(obj["revision_id"] != null, obj["id"]?.Type == JTokenType.String ? obj["id"].ToString() : null, obj["size"] != null);
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
                // ordinary URLs in other responses intact. "requires_download_link" is present (and
                // false) on ordinary file responses, so it only counts when true. Revision payloads
                // carry a signed "url" with neither an expiry nor a true marker, so they are
                // identified by their shape: a revision id plus a size.
                bool carriesSignedDownloadUrl = obj.ContainsKey("url_expires_at")
                    || (obj.TryGetValue("requires_download_link", out object requiresDownloadLink) && requiresDownloadLink is bool required && required)
                    || IsFileRevisionPayload(obj.ContainsKey("revision_id"), obj.ContainsKey("id") ? obj["id"] as string : null, obj.ContainsKey("size"));
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
