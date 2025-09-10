using System;
using LootLocker;
using LootLocker.Requests;
using NUnit.Framework;

namespace LootLockerTests.PlayMode
{
    public class BroadcastJsonTests
    {
        

        [Test, Category("LootLocker"), Category("LootLockerCI")]
        public void Broadcasts_DeserializingBroadcasts_PopulatesConvenienceResponseObject()
        {
            // Given
            string json = "{\"broadcasts\":[{\"id\":\"01K4QGQTNHTXHM9ET5238NB4NF\",\"name\":\"Broadcast no 5\",\"game_name\":\"MetaDataTest Aug24\",\"games\":[{\"id\":93893,\"name\":\"MetaDataTest Aug24\"}],\"publication_settings\":[{\"start\":\"2025-09-09T11:12:00Z\",\"end\":\"2025-09-11T15:15:00Z\",\"tz\":\"UTC\"}],\"languages\":[{\"language_code\":\"en\",\"localizations\":[{\"key\":\"ll.headline\",\"value\":\"Broadcast Title\"},{\"key\":\"ll.image_url\",\"value\":\"https://google.com\"},{\"key\":\"ll.action\",\"value\":\"window.open(\\\"/some/page\\\")\"},{\"key\":\"ll.body\",\"value\":\"Broadcast content\"},{\"key\":\"extra-key\",\"value\":\"extra-value\"},{\"key\":\"final-toll\",\"value\":\"troll toll\"},{\"key\":\"what are the requirements here?\",\"value\":\"{ \\\"i-dont-know\\\": true }\"}]},{\"language_code\":\"sv\",\"localizations\":[{\"key\":\"ll.headline\",\"value\":\"Meddelandetitel\"},{\"key\":\"ll.image_url\",\"value\":\"https://feet.com\"},{\"key\":\"ll.action\",\"value\":\"window.open(\\\"/some/page\\\")\"},{\"key\":\"ll.body\",\"value\":\"Lite innehåll\"},{\"key\":\"🤩🤩 Hello 🤩🤩\",\"value\":\"kjhasdkjah\"}]}]}],\"pagination\":{\"errors\":null,\"per_page\":10,\"offset\":0,\"total\":1,\"last_page\":1,\"current_page\":1,\"next_page\":null,\"prev_page\":null}}";

            // When
            var deserialized =
                LootLockerJson.DeserializeObject<__LootLockerInternalListBroadcastsResponse>(json);

            var broadcastResponse = new LootLockerListBroadcastsResponse(deserialized);
            // Then
            Assert.IsNotNull(broadcastResponse);
            Assert.IsNotEmpty(broadcastResponse.broadcasts);
            Assert.IsNotNull(broadcastResponse.pagination);
            Assert.AreEqual(1, broadcastResponse.pagination.total);
            Assert.AreEqual(1, broadcastResponse.pagination.current_page);
            Assert.AreEqual(1, broadcastResponse.broadcasts.Length);
            Assert.AreEqual("01K4QGQTNHTXHM9ET5238NB4NF", broadcastResponse.broadcasts[0].id);
            Assert.AreEqual("Broadcast no 5", broadcastResponse.broadcasts[0].name);
            Assert.IsNotEmpty(broadcastResponse.broadcasts[0].publication_settings);
            Assert.AreEqual(1, broadcastResponse.broadcasts[0].publication_settings.Length);
            Assert.AreEqual(DateTime.Parse("2025-09-09T11:12:00Z").ToUniversalTime(), broadcastResponse.broadcasts[0].publication_settings[0].start.ToUniversalTime());
            Assert.AreEqual(DateTime.Parse("2025-09-11T15:15:00Z").ToUniversalTime(), broadcastResponse.broadcasts[0].publication_settings[0].end.ToUniversalTime());
            Assert.AreEqual("UTC", broadcastResponse.broadcasts[0].publication_settings[0].tz);

            Assert.IsNotEmpty(broadcastResponse.broadcasts[0].languages);
            Assert.AreEqual(2, broadcastResponse.broadcasts[0].language_codes.Length);
            Assert.AreEqual(2, broadcastResponse.broadcasts[0].languages.Count);
            Assert.AreEqual("en", broadcastResponse.broadcasts[0].language_codes[0]);

            var enLang = broadcastResponse.broadcasts[0].languages["en"];
            Assert.IsNotNull(enLang);

            Assert.IsNotEmpty(enLang.localizations);
            Assert.AreEqual(enLang.language_code, "en");
            Assert.AreEqual(enLang.headline, "Broadcast Title");
            Assert.AreEqual(enLang.image_url, "https://google.com");
            Assert.AreEqual(enLang.action, "window.open(\"/some/page\")");
            Assert.AreEqual(enLang.body, "Broadcast content");
            Assert.AreEqual(3, enLang.localizations.Count);
            Assert.AreEqual(3, enLang.localization_keys.Length);
            
            Assert.AreEqual("extra-key", enLang.localization_keys[0]);
            Assert.AreEqual("extra-value", enLang.localizations["extra-key"]);
            Assert.AreEqual("final-toll", enLang.localization_keys[1]);
            Assert.AreEqual("troll toll", enLang.localizations["final-toll"]);
            Assert.AreEqual("what are the requirements here?", enLang.localization_keys[2]);
            Assert.AreEqual("{ \"i-dont-know\": true }", enLang.localizations["what are the requirements here?"]);

            Assert.AreEqual("sv", broadcastResponse.broadcasts[0].language_codes[1]);
            var sweLang = broadcastResponse.broadcasts[0].languages["sv"];
            Assert.IsNotNull(sweLang);

            Assert.IsNotEmpty(sweLang.localizations);
            Assert.AreEqual(sweLang.language_code, "sv");
            Assert.AreEqual(sweLang.headline, "Meddelandetitel");
            Assert.AreEqual(sweLang.image_url, "https://feet.com");
            Assert.AreEqual(sweLang.action, "window.open(\"/some/page\")");
            Assert.AreEqual(sweLang.body, "Lite innehåll");
            Assert.AreEqual(1, sweLang.localizations.Count);
            Assert.AreEqual(1, sweLang.localization_keys.Length);
            
            Assert.AreEqual("🤩🤩 Hello 🤩🤩", sweLang.localization_keys[0]);
            Assert.AreEqual("kjhasdkjah", sweLang.localizations["🤩🤩 Hello 🤩🤩"]);
        }
    }
}