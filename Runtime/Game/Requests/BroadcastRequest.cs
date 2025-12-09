using System;
using System.Collections.Generic;

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// Represents a publication setting for a broadcast message
    /// </summary>
    public class LootLockerBroadcastPublicationSetting
    {
        /// <summary>
        /// The id of the publication setting
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// The time of publication
        /// </summary>
        public DateTime start { get; set; }
        /// <summary>
        /// The optional time of when the broadcast will no longer be returned
        /// </summary>
        public DateTime end { get; set; }
        /// <summary>
        /// The IANA timezone that the start and end times are specified in, eg. UTC, Asia/Tokyo, or America/Washington
        /// </summary>
        public string tz { get; set; }
    };

    /// <summary>
    /// Indicates which games are allowed to see this broadcast.
    /// This may be useful if you want to know what other games might be seeing this broadcast at the point of display.
    /// </summary>
    public class LootLockerBroadcastGame
    {
        /// <summary>
        /// The id of the game
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// The name of the game
        /// </summary>
        public string name { get; set; }
    };

    public class __LootLockerInternalBroadcastLocalization
    {
        /// <summary>
        /// The key for this localization entry
        /// Some keys are system defined, eg. ll.headline, ll.body, ll.image_url, ll.action
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// The value for this localization entry
        /// </summary>
        public string value { get; set; }
    }

    /// <summary>
    /// Represents a localised version of a broadcast message
    /// </summary>
    public class __LootLockerInternalBroadcastLanguage
    {
        /// <summary>
        /// The language code for this localised version of the broadcast message, eg. en-GB
        /// </summary>
        public string language_code { get; set; }
        /// <summary>
        /// Metadata associated with the localised version of this broadcast message
        /// </summary>
        public __LootLockerInternalBroadcastLocalization[] localizations { get; set; }
    };

    /// <summary>
    /// Represents a localised version of a broadcast message
    /// </summary>
    public class LootLockerBroadcastLanguage
    {
        /// <summary>
        /// The language code for this localised version of the broadcast message, eg. en-GB
        /// </summary>
        public string language_code { get; set; }
        /// <summary>
        /// The headline for this broadcast message
        /// </summary>
        public string headline { get; set; }
        /// <summary>
        /// The body for this broadcast message
        /// </summary>
        public string body { get; set; }
        /// <summary>
        /// The image URL for this broadcast message
        /// </summary>
        public string image_url { get; set; }
        /// <summary>
        /// The action for this broadcast message
        /// </summary>
        public string action { get; set; }
        /// <summary>
        /// List of the keys available in the localizations dictionary
        /// </summary>
        public string[] localization_keys { get; set; }
        /// <summary>
        /// Localized entries for this broadcast message
        /// </summary>
        public Dictionary<string, string> localizations { get; set; }
    };

    /// <summary>
    /// Represents a broadcast message
    /// </summary>
    public class __LootLockerInternalBroadcast
    {
        /// <summary>
        /// The unique identifier (ULID) for this broadcast message
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The name of this broadcast message
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Name of the current game you're seeing this broadcast on.
        /// </summary>
        public string game_name { get; set; }
        /// <summary>
        /// Indicates which games are allowed to see this broadcast.
        /// This may be useful if you want to know what other games might be seeing this broadcast at the point of display.
        /// </summary>
        public List<LootLockerBroadcastGame> games { get; set; }
        /// <summary>
        /// A list of publication settings for this broadcast message
        /// This list will always contain at least the publication time in UTC, but may also contain additional publication settings for different timezones
        /// </summary>
        public LootLockerBroadcastPublicationSetting[] publication_settings { get; set; }
        /// <summary>
        /// Localised versions of this broadcast message
        /// </summary>
        public __LootLockerInternalBroadcastLanguage[] languages { get; set; }
    };

    /// <summary>
    /// Represents a broadcast message
    /// </summary>
    public class LootLockerBroadcast
    {
        /// <summary>
        /// The unique identifier (ULID) for this broadcast message
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The name of this broadcast message
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Name of the current game you're seeing this broadcast on.
        /// </summary>
        public string game_name { get; set; }
        /// <summary>
        /// Indicates which games are allowed to see this broadcast.
        /// This may be useful if you want to know what other games might be seeing this broadcast at the point of display.
        /// </summary>
        public List<LootLockerBroadcastGame> games { get; set; }
        /// <summary>
        /// A list of publication settings for this broadcast message
        /// This list will always contain at least the publication time in UTC, but may also contain additional publication settings for different timezones
        /// </summary>
        public LootLockerBroadcastPublicationSetting[] publication_settings { get; set; }
        /// <summary>
        /// The language codes available for this broadcast message
        /// eg. ["en", "en-US", "zh"]
        /// </summary>
        public string[] language_codes { get; set; }
        /// <summary>
        /// Localised versions of this broadcast message
        /// </summary>
        public Dictionary<string, LootLockerBroadcastLanguage> languages { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response for listing broadcasts
    /// </summary>
    public class __LootLockerInternalListBroadcastsResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of cronologically ordered broadcasts
        /// </summary>
        public __LootLockerInternalBroadcast[] broadcasts { get; set; }
    };

    /// <summary>
    /// Response for listing broadcasts
    /// </summary>
    public class LootLockerListBroadcastsResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of cronologically ordered broadcasts
        /// </summary>
        public LootLockerBroadcast[] broadcasts { get; set; }

        public LootLockerListBroadcastsResponse()
        {
            broadcasts = Array.Empty<LootLockerBroadcast>();
        }

        public LootLockerListBroadcastsResponse(__LootLockerInternalListBroadcastsResponse internalResponse)
        {
            if (internalResponse == null)
            {
                broadcasts = Array.Empty<LootLockerBroadcast>();
                return;
            }

            success = internalResponse.success;
            statusCode = internalResponse.statusCode;
            requestContext = internalResponse.requestContext;
            EventId = internalResponse.EventId;
            errorData = internalResponse.errorData;
            text = internalResponse.text;

            if (internalResponse.broadcasts == null || internalResponse.broadcasts.Length == 0)
            {
                broadcasts = Array.Empty<LootLockerBroadcast>();
                return;
            }

            // Convert internal broadcasts to public broadcasts
            broadcasts = new LootLockerBroadcast[internalResponse.broadcasts.Length];
            for (int i = 0; i < internalResponse.broadcasts.Length; i++)
            {
                __LootLockerInternalBroadcast internalBroadcast = internalResponse.broadcasts[i];
                LootLockerBroadcast translatedBroadcast = new LootLockerBroadcast();
                translatedBroadcast.id = internalBroadcast.id;
                translatedBroadcast.name = internalBroadcast.name;
                translatedBroadcast.game_name = internalBroadcast.game_name;
                translatedBroadcast.games = internalBroadcast.games;
                translatedBroadcast.publication_settings = internalBroadcast.publication_settings;
                translatedBroadcast.language_codes = new string[internalBroadcast.languages?.Length ?? 0];
                translatedBroadcast.languages = new Dictionary<string, LootLockerBroadcastLanguage>();

                for (int j = 0; j < internalBroadcast?.languages?.Length; j++)
                {
                    var internalLang = internalBroadcast.languages[j];
                    if (internalLang == null || string.IsNullOrEmpty(internalLang.language_code))
                        continue;
                    LootLockerBroadcastLanguage lang = new LootLockerBroadcastLanguage();
                    translatedBroadcast.language_codes[j] = internalLang.language_code;
                    lang.language_code = internalLang.language_code;
                    lang.localizations = new Dictionary<string, string>();
                    
                    List<string> localizationKeys = new List<string>();
                    for (int k = 0; k < (internalLang.localizations?.Length ?? 0); k++)
                    {
                        switch (internalLang.localizations[k].key)
                        {
                            case "ll.headline":
                                lang.headline = internalLang.localizations[k].value;
                                break;
                            case "ll.body":
                                lang.body = internalLang.localizations[k].value;
                                break;
                            case "ll.image_url":
                                lang.image_url = internalLang.localizations[k].value;
                                break;
                            case "ll.action":
                                lang.action = internalLang.localizations[k].value;
                                break;
                            default:
                                localizationKeys.Add(internalLang.localizations[k].key);
                                lang.localizations[internalLang.localizations[k].key] = internalLang.localizations[k].value;
                                break;
                        }
                    }
                    lang.localization_keys = localizationKeys.ToArray();
                    translatedBroadcast.languages[lang.language_code] = lang;
                }

                broadcasts[i] = translatedBroadcast;
            }
        }
    };
}
