using LootLocker;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
#if !LOOTLOCKER_USE_NEWTONSOFTJSON
using System.Linq;
using LLlibs.ZeroDepJson;
#else
using Newtonsoft.Json;
#endif
using NUnit.Framework;
using UnityEngine;

namespace LootLockerTests.PlayMode
{
    public class MultiDimensionalArrayClass
    {
        public string[][] multiDimensionalArray { get; set; }
    }

    public class JsonTests
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_DeserializingSimpleJson_Succeeds()
        {
            // Given
            const string validGuestSessionResponse =
                "{\n  \"success\": true,\n  \"session_token\": \"e6fa44946f077dd9fe67311ab3f188c596df9969\",\n  \"player_id\": 3,\n  \"public_uid\": \"TSEYDXD8\",\n  \"player_identifier\": \"uuid-11223344\",\n  \"player_created_at\": \"2022-05-30T07:56:01+00:00\",\n  \"check_grant_notifications\": true,\n  \"check_deactivation_notifications\": false,\n  \"seen_before\": true\n}";

            // When
            LootLockerSessionRequest deserializedSessionRequest =
                LootLockerJson.DeserializeObject<LootLockerSessionRequest>(validGuestSessionResponse);

            // Then
            Assert.NotNull(deserializedSessionRequest, "Not deserialized, is null");
            Assert.NotNull(deserializedSessionRequest.player_identifier,
                "Not deserialized, does not contain player_identifier property");
            Assert.AreEqual(deserializedSessionRequest.player_identifier, "uuid-11223344",
                "Not deserialized, does not contain player_identifier value");
        }

            [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
            public void StreamedResponse_ParsesMultipleObjectsAndCount()
            {
                // Given
                string streamedJson = "{\"id\":1,\"name\":\"A\"},{\"id\":2,\"name\":\"B\"},{\"streamedObjectCount\":2}";

                // When
                var result = LootLockerJson.DeserializeStreamedResponse<TestStreamedObject>(streamedJson);

                // Then
                Assert.AreEqual(2, result.streamedObjectCount);
                Assert.AreEqual(2, result.objects.Length);
                Assert.AreEqual(1, result.objects[0].id);
                Assert.AreEqual("A", result.objects[0].name);
                Assert.AreEqual(2, result.objects[1].id);
                Assert.AreEqual("B", result.objects[1].name);
            }

            [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
            public void StreamedResponse_HandlesEmptyInput()
            {
                // When
                var result = LootLockerJson.DeserializeStreamedResponse<TestStreamedObject>("");

                // Then
                Assert.AreEqual(0, result.streamedObjectCount);
                Assert.IsNotNull(result.objects);
                Assert.AreEqual(0, result.objects.Length);
            }

            [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
            public void StreamedResponse_HandlesMalformedSegments()
            {
                // Given
                string streamedJson = "{\"id\":1,\"name\":\"A\"},{{malformed}},{\"streamedObjectCount\":2}";

                // When
                var result = LootLockerJson.DeserializeStreamedResponse<TestStreamedObject>(streamedJson);

                // Then
                Assert.AreEqual(2, result.streamedObjectCount);
                Assert.AreEqual(1, result.objects.Length); // Only valid object parsed
                Assert.AreEqual(1, result.objects[0].id);
                Assert.AreEqual("A", result.objects[0].name);
            }

            public class TestStreamedObject
            {
                public int id { get; set; }
                public string name { get; set; }
            }
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_DeserializingComplexArrayJson_Succeeds()
        {
            // Given
            const string complexJson =
                "{\n\"success\": true,\n\"loadouts\": [\n{\n\"character\": {\n\"id\": 3015691,\n\"type\": \"Wizard\",\n\"name\": \"Bb32\",\n\"is_default\": true\n},\n\"loadout\": []\n}\n]\n}";

            // When
            LootLockerClassLoadoutResponse deserializedCharacterLoadoutResponse =
                LootLockerJson.DeserializeObject<LootLockerClassLoadoutResponse>(complexJson);

            // Then
            Assert.NotNull(deserializedCharacterLoadoutResponse, "Not deserialized, is null");
            Assert.NotNull(deserializedCharacterLoadoutResponse.GetClassess(),
                "Not deserialized, does not contain characters");
            Assert.IsNotEmpty(deserializedCharacterLoadoutResponse.GetClassess(),
                "Not deserialized, does not contain characters");
            Assert.AreEqual(deserializedCharacterLoadoutResponse.GetClass("Bb32").type, "Wizard",
                "Not deserialized, does not contain the correct character");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_DeserializingMultiDimensionalArray_Succeeds()
        {
            // Given
            string multiDimJson = "{\"multi_dimensional_array\":[[\"1-1\",\"1-2\",\"1-3\",\"1-4\"],[\"2-1\",\"2-2\",\"2-3\"],[\"3-1\",\"3-2\"]]}";

            // When
            MultiDimensionalArrayClass deserializedMultiDimensionalArray =
                LootLockerJson.DeserializeObject<MultiDimensionalArrayClass>(multiDimJson);

            // Then
            Assert.NotNull(deserializedMultiDimensionalArray, "Not deserialized, is null");
            Assert.NotNull(deserializedMultiDimensionalArray.multiDimensionalArray,
                "Not deserialized, does not contain multi dimensional array");
            Assert.IsNotEmpty(deserializedMultiDimensionalArray.multiDimensionalArray,
                "Not deserialized, does not contain multi dimensional array");
            Assert.AreEqual("2-2", deserializedMultiDimensionalArray.multiDimensionalArray[1][1],
                "Not deserialized, does not contain the correct value");
        }


        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_SerializingMultidimensionalArray_Succeeds()
        {
            // Given
            MultiDimensionalArrayClass mdArray = new MultiDimensionalArrayClass();
            mdArray.multiDimensionalArray = new[]
                { new[] { "1-1", "1-2", "1-3", "1-4" }, new[] { "2-1", "2-2", "2-3" }, new[] { "3-1", "3-2" } };

            // When
            string serializedJson = LootLockerJson.SerializeObject(mdArray);

            // Then
            Assert.NotNull(serializedJson, "Not serialized, is null");
            Assert.AreNotEqual("{}", serializedJson, "Not serialized, empty");
            Assert.IsTrue(serializedJson.Contains("multi_dimensional_array"),
                "Not Serialized, does not contain multiDimensionalArray property");
            Assert.IsTrue(serializedJson.Contains("3-1"), "Not Serialized, does not contain 3-1 value");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_SerializingSimpleJson_Succeeds()
        {
            // Given
            LootLockerSessionRequest SessionRequest = new LootLockerSessionRequest("uuid-11223344", LL_AuthPlatforms.NintendoSwitch);

            // When
            string serializedJson = LootLockerJson.SerializeObject(SessionRequest);

            // Then
            Assert.NotNull(serializedJson, "Not serialized, is null");
            Assert.AreNotEqual("{}", serializedJson, "Not serialized, empty");
            Assert.IsTrue(serializedJson.Contains("player_identifier"),
                "Not Serialized, does not contain player_identifier property");
            Assert.IsTrue(serializedJson.Contains("uuid-11223344"),
                "Not Serialized, does not contain player_identifier value");
        }

        public class ConditionalSerialization
        {

#if LOOTLOCKER_USE_NEWTONSOFTJSON
            [JsonProperty("Prop1")]
#else
            [Json(Name = "Prop1")]
#endif
            public string AttributeRenamedProperty { get; set; } = "Hello";
#if LOOTLOCKER_USE_NEWTONSOFTJSON
            [JsonIgnore]
#else
            [Json(IgnoreWhenSerializing = true, IgnoreWhenDeserializing = true)]
#endif
            public string IgnoredProperty { get; set; } = "ignored";
            public bool Completed { get; set; } = false;
            public int NormalProperty { get; set; } = 1234;

            public bool ShouldSerializeCompleted()
            {
                // don't serialize the Completed property if it is not set.
                return Completed;
            }
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_ConditionalSerializationConfigured_OnlyConfiguredFieldsAreSerialized()
        {
            // Given
            ConditionalSerialization conditionalSerialization = new ConditionalSerialization();

            // When
            string serializedJson = LootLockerJson.SerializeObject(conditionalSerialization);
            Debug.Log(serializedJson);
            // Then
            Assert.NotNull(serializedJson, "Not serialized, is null");
            Assert.AreNotEqual("{}", serializedJson, "Not serialized, empty");
            Assert.IsFalse(serializedJson.Contains("ignored_property"),
                "Not Serialized correctly, contains IgnoredProperty property");
            Assert.IsFalse(serializedJson.Contains("ignored"),
                "Not Serialized correctly, contains ignored value");
            Assert.IsTrue(serializedJson.Contains("prop_1") || serializedJson.Contains("Prop1") /*For some reason Newtonsoft fails to snake case this one*/,
                "Not Serialized correctly, does not contain Prop1 property");
            Assert.IsTrue(serializedJson.Contains("Hello"),
                "Not Serialized correctly, does not contain Prop1 value");
            Assert.IsFalse(serializedJson.Contains("attribute_renamed_property"),
                "Not Serialized correctly, contains AttributeRenamedProperty property");
            Assert.IsFalse(serializedJson.Contains("completed"),
                "Not Serialized correctly, contains Completed property");
            Assert.IsFalse(serializedJson.Contains("false"),
                "Not Serialized correctly, contains Completed value");
            Assert.IsTrue(serializedJson.Contains("normal_property"),
                "Not Serialized correctly, does not contain NormalProperty property");
            Assert.IsTrue(serializedJson.Contains("1234"),
                "Not Serialized correctly, does not contain NormalProperty value");

            // Then Given
            conditionalSerialization.Completed = true;

            // When
            serializedJson = LootLockerJson.SerializeObject(conditionalSerialization);
            
            // Then
            Assert.NotNull(serializedJson, "Not serialized, is null");
            Assert.AreNotEqual("{}", serializedJson, "Not serialized, empty");
            Assert.IsFalse(serializedJson.Contains("ignored_property"),
                "Not Serialized correctly, contains IgnoredProperty property");
            Assert.IsFalse(serializedJson.Contains("ignored"),
                "Not Serialized correctly, contains ignored value");
            Assert.IsTrue(serializedJson.Contains("prop_1") || serializedJson.Contains("Prop1") /*For some reason Newtonsoft fails to snake case this one*/,
                "Not Serialized correctly, does not contain prop_1 property");
            Assert.IsTrue(serializedJson.Contains("Hello"),
                "Not Serialized correctly, does not contain prop_1 value");
            Assert.IsFalse(serializedJson.Contains("attribute_renamed_property"),
                "Not Serialized correctly, contains AttributeRenamedProperty property");
            Assert.IsTrue(serializedJson.Contains("completed"),
                "Not Serialized correctly, does not contain Completed property");
            Assert.IsTrue(serializedJson.Contains("true"),
                "Not Serialized correctly, does not contain Completed value");
            Assert.IsTrue(serializedJson.Contains("normal_property"),
                "Not Serialized correctly, does not contain NormalProperty property");
            Assert.IsTrue(serializedJson.Contains("1234"),
                "Not Serialized correctly, does not contain NormalProperty value");
        }

        public class CaseVariationClass
        {
            public string normalCamelCase { get; set; } = "n/a";
            public string PascalCase { get; set; } = "n/a";
            public string SCREAMINGCASE { get; set; } = "n/a";
            public string Upper_Snake_Case { get; set; } = "n/a";
            public string lower_snake_case { get; set; } = "n/a";
            public string number79CamelCase { get; set; } = "n/a";
            public string CamelCase69 { get; set; } = "n/a";
            public string MultiLetterISAok { get; set; } = "n/a";
        };

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_SerializationCaseConversion_CaseIsConvertedToSnake()
        {
            // Given
            CaseVariationClass caseVariationClass = new CaseVariationClass();

            // When
            string serializedJson = LootLockerJson.SerializeObject(caseVariationClass);

            // Then
            Assert.IsTrue(serializedJson.Contains("normal_camel_case"), "Field normalCamelCase was not serialized correctly, json: " + serializedJson);
            Assert.IsTrue(serializedJson.Contains("pascal_case"), "Field PascalCase was not serialized correctly, json: " + serializedJson);
            Assert.IsTrue(serializedJson.Contains("screamingcase"), "Field SCREAMINGCASE was not serialized correctly, json: " + serializedJson);
            Assert.IsTrue(serializedJson.Contains("upper_snake_case"), "Field Upper_Snake_Case was not serialized correctly, json: " + serializedJson);
            Assert.IsTrue(serializedJson.Contains("lower_snake_case"), "Field lower_snake_case was not serialized correctly, json: " + serializedJson);
#if !LOOTLOCKER_USE_NEWTONSOFTJSON
            // I don't agree with how newtonsoft serializes these fields
            Assert.IsTrue(serializedJson.Contains("number_79_camel_case"), "Field number79CamelCase was not serialized correctly, json: " + serializedJson);
            Assert.IsTrue(serializedJson.Contains("camel_case_69"), "Field CamelCase69 was not serialized correctly, json: " + serializedJson);
            Assert.IsTrue(serializedJson.Contains("multi_letter_isa_ok"), "Field MultiLetterISAok was not serialized correctly, json: " + serializedJson);
#endif
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_DeserializationCaseConversion_CaseIsConvertedToSnake()
        {
            // Given
            string caseVariationClassJson =
                "{\"normal_camel_case\":\"1234\",\"pascal_case\":\"1234\",\"screamingcase\":\"1234\",\"upper_snake_case\":\"1234\",\"lower_snake_case\":\"1234\",\"number_79_camel_case\":\"1234\",\"camel_case_69\":\"1234\",\"multi_letter_isa_ok\":\"1234\"}";

            // When
            CaseVariationClass caseVariationClass =
                LootLockerJson.DeserializeObject<CaseVariationClass>(caseVariationClassJson);

            // Then
            Assert.AreEqual("1234", caseVariationClass.normalCamelCase, "Field: " + nameof(caseVariationClass.normalCamelCase));
            Assert.AreEqual("1234", caseVariationClass.PascalCase, "Field: " + nameof(caseVariationClass.PascalCase));
            Assert.AreEqual("1234", caseVariationClass.SCREAMINGCASE, "Field: " + nameof(caseVariationClass.SCREAMINGCASE));
            Assert.AreEqual("1234", caseVariationClass.Upper_Snake_Case, "Field: " + nameof(caseVariationClass.Upper_Snake_Case));
            Assert.AreEqual("1234", caseVariationClass.lower_snake_case, "Field: " + nameof(caseVariationClass.lower_snake_case));
#if !LOOTLOCKER_USE_NEWTONSOFTJSON
            // I don't agree with how newtonsoft deserializes these fields
            Assert.AreEqual("1234", caseVariationClass.number79CamelCase, "Field: " + nameof(caseVariationClass.number79CamelCase));
            Assert.AreEqual("1234", caseVariationClass.CamelCase69, "Field: " + nameof(caseVariationClass.CamelCase69));
            Assert.AreEqual("1234", caseVariationClass.MultiLetterISAok, "Field: " + nameof(caseVariationClass.MultiLetterISAok));
#endif
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_SerializingConsumeItemRequestWithoutCount_OmitsCount()
        {
            // Given
            var request = new LootLockerConsumeItemRequest();

            // When
            string serializedJson = LootLockerJson.SerializeObject(request);

            // Then
            Assert.IsNotNull(serializedJson, "Not serialized, is null");
            Assert.IsFalse(serializedJson.Contains("count"),
                "Not serialized correctly, contains count even though it was never set");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_SerializingConsumeItemRequestWithCount_IncludesCount()
        {
            // Given
            var request = new LootLockerConsumeItemRequest { count = 3 };

            // When
            string serializedJson = LootLockerJson.SerializeObject(request);

            // Then
            Assert.IsNotNull(serializedJson, "Not serialized, is null");
            Assert.IsTrue(serializedJson.Contains("count"),
                "Not serialized correctly, does not contain count");
            Assert.IsTrue(serializedJson.Contains("3"),
                "Not serialized correctly, does not contain the count value");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_SerializingSplitItemStackRequest_IncludesCount()
        {
            // Given
            var request = new LootLockerSplitItemStackRequest { count = 5 };

            // When
            string serializedJson = LootLockerJson.SerializeObject(request);

            // Then
            Assert.IsNotNull(serializedJson, "Not serialized, is null");
            Assert.IsTrue(serializedJson.Contains("count"),
                "Not serialized correctly, does not contain count");
            Assert.IsTrue(serializedJson.Contains("5"),
                "Not serialized correctly, does not contain the count value");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_SerializingMergeItemStacksRequest_IncludesBothInventoryIds()
        {
            // Given
            var request = new LootLockerMergeItemStacksRequest
            {
                source_inventory_id = "01HZZZZZZZZZZZZZZZZZZZZZZZ",
                target_inventory_id = "01HYYYYYYYYYYYYYYYYYYYYYYY"
            };

            // When
            string serializedJson = LootLockerJson.SerializeObject(request);

            // Then
            Assert.IsNotNull(serializedJson, "Not serialized, is null");
            Assert.IsTrue(serializedJson.Contains("source_inventory_id"),
                "Not serialized correctly, does not contain source_inventory_id");
            Assert.IsTrue(serializedJson.Contains("target_inventory_id"),
                "Not serialized correctly, does not contain target_inventory_id");
            Assert.IsTrue(serializedJson.Contains("01HZZZZZZZZZZZZZZZZZZZZZZZ"),
                "Not serialized correctly, does not contain the source inventory id value");
            Assert.IsTrue(serializedJson.Contains("01HYYYYYYYYYYYYYYYYYYYYYYY"),
                "Not serialized correctly, does not contain the target inventory id value");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_DeserializingListPlayerItemsResponse_Succeeds()
        {
            // Given
            const string listPlayerItemsResponse =
                "{\"success\":true,\"items\":[{\"id\":\"01HZZZZZZZZZZZZZZZZZZZZZZZ\",\"player_id\":3,\"item_template_id\":\"01HAAAAAAAAAAAAAAAAAAAAAAA\",\"item_type\":\"stackable\",\"consumable\":true,\"count\":7,\"source\":\"grant\",\"name\":\"Health Potion\",\"deletable\":true,\"created_at\":\"2024-01-01T00:00:00Z\"}],\"pagination\":{\"total\":1,\"offset\":0,\"per_page\":25,\"last_page\":1,\"current_page\":1,\"next_page\":null,\"prev_page\":null}}";

            // When
            var deserialized = LootLockerJson.DeserializeObject<LootLockerListPlayerItemsResponse>(listPlayerItemsResponse);

            // Then
            Assert.IsNotNull(deserialized, "Not deserialized, is null");
            Assert.IsNotNull(deserialized.items, "Not deserialized, items is null");
            Assert.AreEqual(1, deserialized.items.Length, "Not deserialized, wrong number of items");
            Assert.AreEqual("01HZZZZZZZZZZZZZZZZZZZZZZZ", deserialized.items[0].id, "Wrong item id");
            Assert.AreEqual(3, deserialized.items[0].player_id, "Wrong player id");
            Assert.AreEqual("01HAAAAAAAAAAAAAAAAAAAAAAA", deserialized.items[0].item_template_id, "Wrong item template id");
            Assert.AreEqual("stackable", deserialized.items[0].item_type, "Wrong item type");
            Assert.IsTrue(deserialized.items[0].consumable, "Wrong consumable value");
            Assert.AreEqual(7, deserialized.items[0].count, "Wrong count");
            Assert.AreEqual("Health Potion", deserialized.items[0].name, "Wrong name");
            Assert.IsTrue(deserialized.items[0].deletable, "Wrong deletable value");
            Assert.IsNotNull(deserialized.pagination, "Not deserialized, pagination is null");
            Assert.AreEqual(1, deserialized.pagination.total, "Wrong pagination total");
            Assert.AreEqual(25, deserialized.pagination.per_page, "Wrong pagination per_page");
            Assert.IsNull(deserialized.pagination.next_page, "Wrong pagination next_page");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_DeserializingGetPlayerItemResponseWithTemplateAndMetadata_Succeeds()
        {
            // Given
            const string getPlayerItemResponse =
                "{\"success\":true,\"id\":\"01HZZZZZZZZZZZZZZZZZZZZZZZ\",\"player_id\":3,\"item_template_id\":\"01HAAAAAAAAAAAAAAAAAAAAAAA\",\"item_type\":\"instanced\",\"consumable\":false,\"count\":1,\"source\":\"grant\",\"deletable\":true,\"created_at\":\"2024-01-01T00:00:00Z\",\"template\":{\"id\":\"01HAAAAAAAAAAAAAAAAAAAAAAA\",\"name\":\"Sword\",\"game_id\":42,\"limited\":0,\"item_type\":\"instanced\",\"consumable\":false,\"deletable\":true},\"metadata\":[{\"key\":\"damage\",\"value\":12,\"type\":\"number\",\"access\":[\"game_api.read\"],\"tags\":[\"combat\"]}]}";

            // When
            var deserialized = LootLockerJson.DeserializeObject<LootLockerGetPlayerItemResponse>(getPlayerItemResponse);

            // Then
            Assert.IsNotNull(deserialized, "Not deserialized, is null");
            Assert.AreEqual("01HZZZZZZZZZZZZZZZZZZZZZZZ", deserialized.id, "Wrong item id");
            Assert.AreEqual("instanced", deserialized.item_type, "Wrong item type");
            Assert.IsTrue(deserialized.deletable, "Wrong deletable value");
            Assert.IsNotNull(deserialized.template, "Not deserialized, template is null");
            Assert.AreEqual("Sword", deserialized.template.name, "Wrong template name");
            Assert.AreEqual(42, deserialized.template.game_id, "Wrong template game id");
            Assert.IsNotNull(deserialized.metadata, "Not deserialized, metadata is null");
            Assert.AreEqual(1, deserialized.metadata.Length, "Not deserialized, wrong number of metadata entries");
            Assert.AreEqual("damage", deserialized.metadata[0].key, "Wrong metadata key");
            Assert.AreEqual(LootLockerMetadataTypes.Number, deserialized.metadata[0].type, "Wrong metadata type");
            Assert.IsNotNull(deserialized.metadata[0].access, "Not deserialized, metadata access is null");
            Assert.AreEqual("game_api.read", deserialized.metadata[0].access[0], "Wrong metadata access");
            Assert.IsNotNull(deserialized.metadata[0].tags, "Not deserialized, metadata tags is null");
            Assert.AreEqual("combat", deserialized.metadata[0].tags[0], "Wrong metadata tag");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_DeserializingConsumeItemResponseWithGrantedItems_Succeeds()
        {
            // Given
            const string consumeItemResponse =
                "{\"success\":true,\"consumed\":true,\"granted\":[{\"source_id\":\"01HBBBBBBBBBBBBBBBBBBBBBBB\",\"count\":2,\"type\":\"currency\",\"name\":\"Gold\",\"code\":\"gold\"}]}";

            // When
            var deserialized = LootLockerJson.DeserializeObject<LootLockerConsumeItemResponse>(consumeItemResponse);

            // Then
            Assert.IsNotNull(deserialized, "Not deserialized, is null");
            Assert.IsTrue(deserialized.consumed, "Wrong consumed value");
            Assert.IsNotNull(deserialized.granted, "Not deserialized, granted is null");
            Assert.AreEqual(1, deserialized.granted.Length, "Not deserialized, wrong number of granted items");
            Assert.AreEqual("01HBBBBBBBBBBBBBBBBBBBBBBB", deserialized.granted[0].source_id, "Wrong granted source id");
            Assert.AreEqual(2, deserialized.granted[0].count, "Wrong granted count");
            Assert.AreEqual("currency", deserialized.granted[0].type, "Wrong granted type");
            Assert.AreEqual("Gold", deserialized.granted[0].name, "Wrong granted name");
            Assert.AreEqual("gold", deserialized.granted[0].code, "Wrong granted code");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_DeserializingListItemTemplatesResponse_Succeeds()
        {
            // Given
            const string listItemTemplatesResponse =
                "{\"success\":true,\"items\":[{\"id\":\"01HAAAAAAAAAAAAAAAAAAAAAAA\",\"name\":\"Sword\",\"game_id\":42,\"limited\":10,\"item_type\":\"instanced\",\"consumable\":false,\"deletable\":true,\"created_at\":\"2024-01-01T00:00:00Z\",\"updated_at\":\"2024-01-02T00:00:00Z\"}],\"pagination\":{\"total\":1,\"offset\":0,\"per_page\":25,\"last_page\":1,\"current_page\":1,\"next_page\":null,\"prev_page\":null}}";

            // When
            var deserialized = LootLockerJson.DeserializeObject<LootLockerListItemTemplatesResponse>(listItemTemplatesResponse);

            // Then
            Assert.IsNotNull(deserialized, "Not deserialized, is null");
            Assert.IsNotNull(deserialized.items, "Not deserialized, items is null");
            Assert.AreEqual(1, deserialized.items.Length, "Not deserialized, wrong number of item templates");
            Assert.AreEqual("01HAAAAAAAAAAAAAAAAAAAAAAA", deserialized.items[0].id, "Wrong item template id");
            Assert.AreEqual("Sword", deserialized.items[0].name, "Wrong item template name");
            Assert.AreEqual(42, deserialized.items[0].game_id, "Wrong item template game id");
            Assert.AreEqual(10, deserialized.items[0].limited, "Wrong item template limited value");
            Assert.IsTrue(deserialized.items[0].deletable, "Wrong item template deletable value");
            Assert.IsNotNull(deserialized.pagination, "Not deserialized, pagination is null");
            Assert.AreEqual(1, deserialized.pagination.total, "Wrong pagination total");
        }

#if !LOOTLOCKER_USE_NEWTONSOFTJSON
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_SimpleTypeSerialization_Succeeds()
        {
            Assert.AreEqual("true", Json.Serialize(true));
            Assert.AreEqual("false", Json.Serialize(false));
            Assert.AreEqual("12345678", Json.Serialize(12345678));
            Assert.AreEqual("12345678901234567890", Json.Serialize(12345678901234567890));
            Assert.AreEqual("1234567890123456789.0123456789", Json.Serialize(1234567890123456789.01234567890m));
            Assert.AreEqual("12345678", Json.Serialize((uint)12345678));
            Assert.AreEqual("128", Json.Serialize((byte)128));
            Assert.AreEqual("-56", Json.Serialize((sbyte)-56));
            Assert.AreEqual("-56", Json.Serialize((short)-56));
            Assert.AreEqual("12345", Json.Serialize((ushort)12345));
            Assert.AreEqual("\"héllo world\"", Json.Serialize("héllo world"));
            var ts = new TimeSpan(12, 34, 56, 7, 8);
            Assert.AreEqual("11625670080000", Json.Serialize(ts));
            Assert.AreEqual("\"13:10:56:07.008\"", Json.Serialize(ts, new JsonOptions { SerializationOptions = JsonSerializationOptions.TimeSpanAsText }));
            var guid = Guid.NewGuid();
            Assert.AreEqual("\"" + guid + "\"", Json.Serialize(guid));
            Assert.AreEqual("\"https://github.com/smourier/ZeroDepJson\"", Json.Serialize(new Uri("https://github.com/smourier/ZeroDepJson")));
            Assert.AreEqual("2", Json.Serialize(UriKind.Relative));
            Assert.AreEqual("\"Relative\"", Json.Serialize(UriKind.Relative, new JsonOptions { SerializationOptions = JsonSerializationOptions.EnumAsText }));
            Assert.AreEqual("\"x\"", Json.Serialize('x'));
            Assert.AreEqual("1234.56775", Json.Serialize(1234.56775f));
            Assert.AreEqual("1234.5678", Json.Serialize(1234.5678d));
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_ListSerializationDeserialization_BackAndForthPreservesData()
        {
            var list = new List<Customer>();
            for (var i = 0; i < 10; i++)
            {
                var customer = new Customer();
                customer.Index = i;
                list.Add(customer);
            }

            var json = Json.Serialize(list);
            var list2 = Json.Deserialize<List<Customer>>(json);
            var json2 = Json.Serialize(list2);
            Assert.AreEqual(json, json2);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_DictionarySerializationAndDeserialization_Succeeds()
        {
            var dic = new Dictionary<Guid, Customer>();
            for (var i = 0; i < 10; i++)
            {
                var customer = new Customer();
                customer.Index = i;
                customer.Name = "This is a name 这是一个名字" + Environment.TickCount;
                var address1 = new Address();
                address1.ZipCode = 75000;
                address1.City = new City();
                address1.City.Name = "Paris";
                address1.City.Country = new Country();
                address1.City.Country.Name = "France";

                var address2 = new Address();
                address2.ZipCode = 10001;
                address2.City = new City();
                address2.City.Name = "New York";
                address2.City.Country = new Country();
                address2.City.Country.Name = "USA";

                customer.Addresses = new[] { address1, address2 };

                dic[customer.Id] = customer;
            }

            var json1 = Json.Serialize(dic);
            var list2 = (Dictionary<string, object>)Json.Deserialize(json1);
            var json2 = Json.Serialize(list2);
            Assert.AreEqual(json1, json2);

            var customers = list2.Values.Cast<Dictionary<string, object>>().ToList();
            var json3 = Json.Serialize(customers);
            var list3 = Json.Deserialize<List<Customer>>(json3);
            var json4 = Json.Serialize(list3);
            Assert.AreEqual(json3, json4);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_CyclicJsonSerialization_ThrowsCyclicJsonException()
        {
            var person = new Person { Name = "foo" };
            var persons = new Person[] { person, person };
            try
            {
                var json = Json.Serialize(persons);
                Assert.Fail();
            }
            catch (JsonException ex)
            {
                Assert.IsTrue(ex.Code == 9);
            }
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_CyclicJsonSerializationWithCustomOptions_Succeeds()
        {
            var person = new Person { Name = "héllo" };
            var persons = new Person[] { person, person };
            var options = new CustomOptions();
            var json = Json.Serialize(persons, options);
            Assert.IsTrue(json == "[{\"name\":\"héllo\"},{\"name\":\"héllo\"}]");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Json_SerializingAndDesierializingEnumArrays_Works()
        {
            // Given
            EnumArrayMember eam = new EnumArrayMember
            {
                enumArray = new JsonEnumTest[] {
                    JsonEnumTest.Number_one,
                    JsonEnumTest.Number_two
                }
            };
            JsonEnumTest defaultEnumValue = (JsonEnumTest)(0);

            // When
            var serialized = LootLockerJson.SerializeObject(eam);
            var deserialized =
                LootLockerJson.DeserializeObject<EnumArrayMember>(serialized);

            // Then
            Assert.IsNotNull(serialized);
            Assert.IsNotEmpty(serialized);
            Assert.IsNotEmpty(deserialized.enumArray);
            Assert.AreNotEqual(defaultEnumValue, deserialized.enumArray[0]);
        }

    }

    class CustomOptions : JsonOptions
    {
        public CustomOptions()
        {
            ObjectGraph = new CustomObjectGraph();
        }

        private class CustomObjectGraph : IDictionary<object, object>, Json.IOptionsHolder
        {
            private readonly Dictionary<object, int> _hash = new Dictionary<object, int>();

            public JsonOptions Options { get; set; }

            public void Add(object key, object value)
            {
                _hash[key] = Options.SerializationLevel;
            }

            public bool ContainsKey(object key)
            {
                if (!_hash.TryGetValue(key, out var level))
                    return false;

                if (Options.SerializationLevel == level)
                    return false;

                return true;
            }

            public object this[object key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public ICollection<object> Keys => throw new NotImplementedException();
            public ICollection<object> Values => throw new NotImplementedException();
            public int Count => throw new NotImplementedException();
            public bool IsReadOnly => throw new NotImplementedException();
            public void Add(KeyValuePair<object, object> item) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(KeyValuePair<object, object> item) => throw new NotImplementedException();
            public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex) => throw new NotImplementedException();
            public IEnumerator<KeyValuePair<object, object>> GetEnumerator() => throw new NotImplementedException();
            public bool Remove(object key) => throw new NotImplementedException();
            public bool Remove(KeyValuePair<object, object> item) => throw new NotImplementedException();
            public bool TryGetValue(object key, out object value) => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }

    public enum JsonEnumTest
    {
        None = 0,
        Number_one = 1,
        Number_two = 2
    }

    public struct EnumArrayMember
    {
        public JsonEnumTest[] enumArray { get; set; }
    }

    class Person
    {
        public string Name { get; set; }
    }

    public class Customer
    {
        public Customer()
        {
            Id = Guid.NewGuid();

        }

        public Guid Id { get; }
        public int Index { get; set; }
        public string Name { get; set; }

        public Address[] Addresses { get; set; }

        public override string ToString() => Name;
    }

    public class Address
    {
        public City City { get; set; }
        public int ZipCode { get; set; }

        public override string ToString() => ZipCode.ToString();
    }

    public class City
    {
        public string Name { get; set; }
        public Country Country { get; set; }

        public override string ToString() => Name;
    }

    public class Country
    {
        public string Name { get; set; }

        public override string ToString() => Name;
    }
#else
    }
#endif
}
