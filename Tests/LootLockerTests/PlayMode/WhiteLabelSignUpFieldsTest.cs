using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class WhiteLabelSignUpFieldsTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestCounter++;
            configCopy = LootLockerConfig.current;
            Debug.Log($"##### Start of {this.GetType().Name} test no.{TestCounter} setup #####");

            if (!LootLockerConfig.ClearSettings())
            {
                Debug.LogError("Could not clear LootLocker config");
            }

            LootLockerConfig.current.logLevel = LootLockerLogger.LogLevel.Debug;

            // Create game
            bool gameCreationCallCompleted = false;
            LootLockerTestGame.CreateGame(testName: this.GetType().Name + TestCounter + " ", onComplete: (success, errorMessage, game) =>
            {
                if (!success)
                {
                    gameCreationCallCompleted = true;
                    Debug.LogError(errorMessage);
                    SetupFailed = true;
                }
                gameUnderTest = game;
                gameCreationCallCompleted = true;
            });
            yield return new WaitUntil(() => gameCreationCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }
            gameUnderTest?.SwitchToStageEnvironment();

            // Enable white label login
            bool enableWLCompleted = false;
            gameUnderTest?.EnableWhiteLabelLogin((success, errorMessage) =>
            {
                if (!success)
                {
                    Debug.LogError(errorMessage);
                    SetupFailed = true;
                }
                enableWLCompleted = true;
            });
            yield return new WaitUntil(() => enableWLCompleted);
            if (SetupFailed)
            {
                yield break;
            }

            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Successfully created test game and initialized LootLocker");

            Debug.Log($"##### Start of {this.GetType().Name} test no.{TestCounter} test case #####");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} test case #####");
            if (gameUnderTest != null)
            {
                bool gameDeletionCallCompleted = false;
                gameUnderTest.DeleteGame(((success, errorMessage) =>
                {
                    if (!success)
                    {
                        Debug.LogError(errorMessage);
                    }

                    gameUnderTest = null;
                    gameDeletionCallCompleted = true;
                }));
                yield return new WaitUntil(() => gameDeletionCallCompleted);
            }

            LootLockerStateData.ClearAllSavedStates();

            LootLockerConfig.CreateNewSettings(configCopy);
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} tear down #####");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator GetSignUpFields_WithWhiteLabelEnabled_ReturnsFieldsResponse()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            LootLockerWhiteLabelSignUpFieldsResponse actualResponse = null;
            bool getFieldsCallCompleted = false;
            LootLockerSDKManager.WhiteLabelGetSignUpFields(response =>
            {
                actualResponse = response;
                getFieldsCallCompleted = true;
            });
            yield return new WaitUntil(() => getFieldsCallCompleted);

            // Then
            Assert.IsTrue(actualResponse.success, "GetSignUpFields returned unsuccessful: " + actualResponse.errorData?.message);
            // Fields array should be present (empty if no custom fields configured on this game)
            Assert.IsNotNull(actualResponse.fields, "Fields array should not be null");
        }

        // Verifies serialization round-trip for the @params keyword-escaped property
        [Test, Category("LootLocker"), Category("LootLockerCI")]
        public void CustomField_SerializeDeserialize_HandlesParamsKeywordProperty()
        {
            // Given — a custom field with the @params property set
            var original = new LootLockerWhiteLabelCustomField
            {
                question_text = "When were you born?",
                metadata_key = "birth_date",
                field_type = "date",
                required = true,
                sensitive = false,
                sort_order = 1
            };

            // Assign via the @params property (C# verbatim identifier for the keyword 'params')
            original.@params = "{\"min\":\"1900-01-01\",\"max\":\"2026-01-01\"}";

            // When — serialize to JSON
            string json = LootLockerJson.SerializeObject(original);
            Debug.Log($"Serialized custom field: {json}");

            // Then — the @params property serialized as "params" in JSON
            Assert.IsTrue(json.Contains("\"params\""),
                $"JSON must contain the key \"params\", got:\n{json}");
            Assert.IsTrue(json.Contains("\"min\":\"1900-01-01\""),
                $"JSON must contain the nested JSON payload, got:\n{json}");

            // When — deserialize back
            var deserialized = LootLockerJson.DeserializeObject<LootLockerWhiteLabelCustomField>(json);

            // Then — the @params value round-trips
            Assert.AreEqual(original.question_text, deserialized.question_text, "question_text should round-trip");
            Assert.AreEqual(original.metadata_key, deserialized.metadata_key, "metadata_key should round-trip");
            Assert.AreEqual(original.field_type, deserialized.field_type, "field_type should round-trip");
            Assert.AreEqual(original.required, deserialized.required, "required should round-trip");
            Assert.AreEqual(original.@params, deserialized.@params, "@params should round-trip through serialize/deserialize");
            Assert.AreEqual(original.sort_order, deserialized.sort_order, "sort_order should round-trip");
        }

        // Verifies serialization of request body with custom_fields array
        [Test, Category("LootLocker"), Category("LootLockerCI")]
        public void UserRequest_SerializeDeserialize_IncludesCustomFields()
        {
            // Given
            var customFieldValue = new LootLockerWhiteLabelCustomFieldValue
            {
                metadata_key = "tos_agree",
                value_json = "true"
            };

            var request = new LootLockerWhiteLabelUserRequest
            {
                email = "player@example.com",
                password = "s3cur3p4ssw0rd",
                remember = false,
                custom_fields = new[] { customFieldValue }
            };

            // When
            string json = LootLockerJson.SerializeObject(request);
            Debug.Log($"Serialized sign-up request: {json}");

            // Then — verify custom_fields appear in JSON with correct keys
            Assert.IsTrue(json.Contains("\"custom_fields\""),
                $"JSON must contain \"custom_fields\", got:\n{json}");
            Assert.IsTrue(json.Contains("\"metadata_key\":\"tos_agree\""),
                $"JSON must contain metadata_key, got:\n{json}");
            Assert.IsTrue(json.Contains("\"value_json\":\"true\""),
                $"JSON must contain value_json, got:\n{json}");
            // Verify existing fields still serialize
            Assert.IsTrue(json.Contains("\"email\":\"player@example.com\""),
                $"JSON must contain email, got:\n{json}");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator SignUp_WithCustomFields_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given — a unique email so we don't conflict with repeated test runs
            string email = $"test-{TestCounter}-{System.Guid.NewGuid():N}@example.com";
            string password = "TestPassword123!";

            LootLockerWhiteLabelCustomFieldValue[] customFields = new LootLockerWhiteLabelCustomFieldValue[]
            {
                new LootLockerWhiteLabelCustomFieldValue
                {
                    metadata_key = "birth_date",
                    value_json = "\"2000-01-15\""
                },
                new LootLockerWhiteLabelCustomFieldValue
                {
                    metadata_key = "tos_agree",
                    value_json = "true"
                }
            };

            // When
            LootLockerWhiteLabelSignupResponse actualResponse = null;
            bool signUpCallCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(email, password, customFields, response =>
            {
                actualResponse = response;
                signUpCallCompleted = true;
            });
            yield return new WaitUntil(() => signUpCallCompleted);

            // Then
            Assert.IsTrue(actualResponse.success, "WhiteLabelSignUp with custom fields failed: " + actualResponse.errorData?.message);
            Assert.IsNotNull(actualResponse.Email, "Email should be present in response");
        }
    }
}
