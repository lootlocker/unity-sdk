using System.Collections;
using System.Collections.Generic;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class SurveyTests
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

            // Enable guest platform
            bool enableGuestLoginCallCompleted = false;
            gameUnderTest?.EnableGuestLogin((success, errorMessage) =>
            {
                if (!success)
                {
                    Debug.LogError(errorMessage);
                    SetupFailed = true;
                }
                enableGuestLoginCallCompleted = true;
            });
            yield return new WaitUntil(() => enableGuestLoginCallCompleted);
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
        public IEnumerator Surveys_ListSurveys_ReturnsStandaloneSurveyWithQuestions()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession("Survey-Player-1", response => { signInCompleted = true; });
            yield return new WaitUntil(() => signInCompleted);

            bool createCompleted = false;
            LootLockerTestSurvey.LootLockerTestCreateSurveyResponse createdSurvey = null;
            LootLockerTestSurvey.CreateSurveyWithRequiredQuestion("Survey-1", "survey_answer_1", response =>
            {
                createdSurvey = response;
                createCompleted = true;
            });
            yield return new WaitUntil(() => createCompleted);
            Assert.IsTrue(createdSurvey.success, "Failed to create survey");
            Assert.IsNotEmpty(createdSurvey.id, "Created survey has no id");

            // When
            bool listCompleted = false;
            LootLockerListSurveysResponse listResponse = null;
            LootLockerSDKManager.ListSurveys(response =>
            {
                listResponse = response;
                listCompleted = true;
            });
            yield return new WaitUntil(() => listCompleted);

            // Then
            Assert.IsTrue(listResponse.success, "List surveys failed");
            Assert.AreEqual(1, listResponse.surveys?.Length, "Expected exactly one visible survey");
            var survey = listResponse.surveys[0];
            Assert.AreEqual(createdSurvey.id, survey.id, "Unexpected survey id");
            Assert.AreEqual("Survey-1", survey.name, "Unexpected survey name");
            Assert.IsTrue(survey.standalone, "Survey should be standalone");
            Assert.IsFalse(survey.completed, "Survey should not be completed yet");
            Assert.IsEmpty(survey.completed_at, "completed_at should be empty when not completed");
            Assert.AreEqual(1, survey.questions?.Length, "Expected exactly one question");
            Assert.AreEqual("survey_answer_1", survey.questions[0].metadata_key, "Unexpected metadata key");
            Assert.IsTrue(survey.questions[0].required, "Question should be required");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator Surveys_GetSurvey_ReturnsSurveyWithQuestions()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession("Survey-Player-2", response => { signInCompleted = true; });
            yield return new WaitUntil(() => signInCompleted);

            bool createCompleted = false;
            LootLockerTestSurvey.LootLockerTestCreateSurveyResponse createdSurvey = null;
            LootLockerTestSurvey.CreateSurveyWithRequiredQuestion("Survey-2", "survey_answer_2", response =>
            {
                createdSurvey = response;
                createCompleted = true;
            });
            yield return new WaitUntil(() => createCompleted);
            Assert.IsTrue(createdSurvey.success, "Failed to create survey");

            // When
            bool getCompleted = false;
            LootLockerGetSurveyResponse getResponse = null;
            LootLockerSDKManager.GetSurvey(createdSurvey.id, response =>
            {
                getResponse = response;
                getCompleted = true;
            });
            yield return new WaitUntil(() => getCompleted);

            // Then
            Assert.IsTrue(getResponse.success, "Get survey failed");
            Assert.AreEqual(createdSurvey.id, getResponse.id, "Unexpected survey id");
            Assert.AreEqual("Survey-2", getResponse.name, "Unexpected survey name");
            Assert.AreEqual(1, getResponse.questions?.Length, "Expected exactly one question");
            Assert.AreEqual("survey_answer_2", getResponse.questions[0].metadata_key, "Unexpected metadata key");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator Surveys_CompleteSurvey_WithoutRequiredAnswer_Fails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession("Survey-Player-3", response => { signInCompleted = true; });
            yield return new WaitUntil(() => signInCompleted);

            bool createCompleted = false;
            LootLockerTestSurvey.LootLockerTestCreateSurveyResponse createdSurvey = null;
            LootLockerTestSurvey.CreateSurveyWithRequiredQuestion("Survey-3", "survey_answer_3", response =>
            {
                createdSurvey = response;
                createCompleted = true;
            });
            yield return new WaitUntil(() => createCompleted);
            Assert.IsTrue(createdSurvey.success, "Failed to create survey");

            // When
            bool completeCompleted = false;
            LootLockerCompleteSurveyResponse completeResponse = null;
            LootLockerSDKManager.CompleteSurvey(createdSurvey.id, response =>
            {
                completeResponse = response;
                completeCompleted = true;
            });
            yield return new WaitUntil(() => completeCompleted);

            // Then
            Assert.IsFalse(completeResponse.success, "Completing a survey with an unanswered required question should fail");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator Surveys_CompleteSurvey_WithRequiredAnswer_SucceedsAndMarksCompleted()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            bool signInCompleted = false;
            string playerUlid = null;
            LootLockerSDKManager.StartGuestSession("Survey-Player-4", response =>
            {
                playerUlid = response?.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(playerUlid, "Guest session failed");

            bool createCompleted = false;
            LootLockerTestSurvey.LootLockerTestCreateSurveyResponse createdSurvey = null;
            LootLockerTestSurvey.CreateSurveyWithRequiredQuestion("Survey-4", "survey_answer_4", response =>
            {
                createdSurvey = response;
                createCompleted = true;
            });
            yield return new WaitUntil(() => createCompleted);
            Assert.IsTrue(createdSurvey.success, "Failed to create survey");

            // Submit the answer through the player metadata API.
            bool metadataCompleted = false;
            LootLockerTestMetadata.LootLockerTestMetadataOperationsResponse metadataResponse = null;
            LootLockerTestMetadata.PerformMetadataOperations(
                LootLockerTestMetadata.LootLockerTestMetadataSources.player,
                playerUlid,
                new List<LootLockerTestMetadata.LootLockerTestMetadataOperation>
                {
                    new LootLockerTestMetadata.LootLockerTestMetadataOperation
                    {
                        key = "survey_answer_4",
                        value = "From a friend",
                        type = LootLockerTestMetadata.LootLockerTestMetadataTypes.String,
                        action = LootLockerTestMetadata.LootLockerTestMetadataActions.create_or_update
                    }
                },
                response =>
                {
                    metadataResponse = response;
                    metadataCompleted = true;
                });
            yield return new WaitUntil(() => metadataCompleted);
            Assert.IsTrue(metadataResponse.success, "Failed to submit survey answer metadata");

            // When
            bool completeCompleted = false;
            LootLockerCompleteSurveyResponse completeResponse = null;
            LootLockerSDKManager.CompleteSurvey(createdSurvey.id, response =>
            {
                completeResponse = response;
                completeCompleted = true;
            });
            yield return new WaitUntil(() => completeCompleted);

            // Then
            Assert.IsTrue(completeResponse.success, "Complete survey failed");
            Assert.IsTrue(completeResponse.complete, "Response should report the survey as complete");
            Assert.AreEqual(createdSurvey.id, completeResponse.survey_id, "Unexpected survey id in response");
            Assert.IsNotEmpty(completeResponse.completed_at, "completed_at should be set");

            bool listCompleted = false;
            LootLockerListSurveysResponse listResponse = null;
            LootLockerSDKManager.ListSurveys(response =>
            {
                listResponse = response;
                listCompleted = true;
            });
            yield return new WaitUntil(() => listCompleted);

            Assert.IsTrue(listResponse.success, "List surveys failed");
            Assert.AreEqual(1, listResponse.surveys?.Length, "Expected exactly one visible survey");
            Assert.IsTrue(listResponse.surveys[0].completed, "Survey should be marked completed");
            Assert.IsNotEmpty(listResponse.surveys[0].completed_at, "completed_at should be set on the listed survey");
        }
    }
}
