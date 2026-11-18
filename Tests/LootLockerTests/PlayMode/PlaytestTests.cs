using System;
using System.Collections;
using LootLocker;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class PlaytestTests
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;

        private const string PlaytestSubdomain = "ci-playtest";

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

            bool gameCreationCallCompleted = false;
            LootLockerTestGame.CreateGame(testName: this.GetType().Name + TestCounter + " ", onComplete: (success, errorMessage, game) =>
            {
                if (!success)
                {
                    Debug.LogError(errorMessage);
                    SetupFailed = true;
                    gameCreationCallCompleted = true;
                    return;
                }
                gameUnderTest = game;
                gameCreationCallCompleted = true;
            });
            yield return new WaitUntil(() => gameCreationCallCompleted);
            if (SetupFailed) { yield break; }

            gameUnderTest?.SwitchToStageEnvironment();

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
            if (SetupFailed) { yield break; }

            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Successfully created test game and initialized LootLocker");

            bool sessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                if (!response.success)
                {
                    Debug.LogError(response.errorData?.message);
                    SetupFailed = true;
                }
                sessionCompleted = true;
            });
            yield return new WaitUntil(() => sessionCompleted);
            if (SetupFailed) { yield break; }

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

        /// <summary>
        /// Creates an active playtest with the given subdomain and returns its ULID.
        /// </summary>
        private IEnumerator CreatePlaytest(Action<string> onCreated)
        {
            string playtestId = null;
            bool playtestCreated = false;
            LootLockerTestPlaytest.CreatePlaytest("CI Playtest", PlaytestSubdomain, response =>
            {
                if (!response.success)
                {
                    Debug.LogError($"Failed to create playtest: {response.errorData?.message}");
                    SetupFailed = true;
                }
                playtestId = response.id;
                playtestCreated = true;
            });
            yield return new WaitUntil(() => playtestCreated);
            if (SetupFailed) { yield break; }

            onCreated?.Invoke(playtestId);
        }

        /// <summary>
        /// Creates a document on the playtest and returns its ULID and content hash.
        /// </summary>
        private IEnumerator CreateDocument(string playtestId, bool required, string[] statementIds, Action<string, string> onCreated)
        {
            string documentId = null;
            string contentHash = null;
            bool documentCreated = false;
            LootLockerTestPlaytest.CreateDocument(playtestId, "CI Document", "Some terms", required, statementIds, response =>
            {
                if (!response.success)
                {
                    Debug.LogError($"Failed to create document: {response.errorData?.message}");
                    SetupFailed = true;
                }
                documentId = response.id;
                contentHash = response.content_hash;
                documentCreated = true;
            });
            yield return new WaitUntil(() => documentCreated);
            if (SetupFailed) { yield break; }

            onCreated?.Invoke(documentId, contentHash);
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator Playtest_GetOnboardingStatus_ReturnsNotCompletedForNewPlayer()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            yield return CreatePlaytest(_ => { });
            if (SetupFailed) { Assert.Fail("Failed to create playtest"); }

            // When
            LootLockerPlaytestOnboardingStatusResponse actualResponse = null;
            bool callCompleted = false;
            LootLockerSDKManager.GetPlaytestOnboardingStatus(PlaytestSubdomain, response =>
            {
                actualResponse = response;
                callCompleted = true;
            });
            yield return new WaitUntil(() => callCompleted);

            // Then
            Assert.IsTrue(actualResponse.success, "Expected call to succeed");
            Assert.IsFalse(actualResponse.completed, "Expected onboarding to not be completed");
            Assert.AreEqual(default(DateTime), actualResponse.completed_at, "Expected completed_at to be unset");
            Assert.IsNotNull(actualResponse.documents, "Expected documents to be an array");
            Assert.AreEqual(0, actualResponse.documents.Length, "Expected no acknowledged documents");
            Assert.IsNotNull(actualResponse.answers, "Expected answers to be an array");
            Assert.AreEqual(0, actualResponse.answers.Length, "Expected no stored answers");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator Playtest_CompleteOnboardingWithoutRequiredDocument_Fails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            string playtestId = null;
            yield return CreatePlaytest(id => playtestId = id);
            if (SetupFailed) { Assert.Fail("Failed to create playtest"); }

            yield return CreateDocument(playtestId, true, null, (_, __) => { });
            if (SetupFailed) { Assert.Fail("Failed to create document"); }

            // When
            LootLockerCompletePlaytestOnboardingResponse actualResponse = null;
            bool callCompleted = false;
            LootLockerSDKManager.CompletePlaytestOnboarding(PlaytestSubdomain, response =>
            {
                actualResponse = response;
                callCompleted = true;
            });
            yield return new WaitUntil(() => callCompleted);

            // Then
            Assert.IsFalse(actualResponse.success, "Expected completion to fail while a required document is unacknowledged");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator Playtest_CompleteOnboardingAfterAcknowledgingRequiredDocument_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            string playtestId = null;
            yield return CreatePlaytest(id => playtestId = id);
            if (SetupFailed) { Assert.Fail("Failed to create playtest"); }

            string documentId = null;
            yield return CreateDocument(playtestId, true, null, (id, _) => documentId = id);
            if (SetupFailed) { Assert.Fail("Failed to create document"); }

            LootLockerAcknowledgePlaytestDocumentResponse acknowledgeResponse = null;
            bool acknowledgeCompleted = false;
            LootLockerSDKManager.AcknowledgePlaytestDocument(PlaytestSubdomain, documentId, null, response =>
            {
                acknowledgeResponse = response;
                acknowledgeCompleted = true;
            });
            yield return new WaitUntil(() => acknowledgeCompleted);
            Assert.IsTrue(acknowledgeResponse.success, "Expected acknowledge to succeed");

            // When
            LootLockerCompletePlaytestOnboardingResponse actualResponse = null;
            bool callCompleted = false;
            LootLockerSDKManager.CompletePlaytestOnboarding(PlaytestSubdomain, response =>
            {
                actualResponse = response;
                callCompleted = true;
            });
            yield return new WaitUntil(() => callCompleted);

            // Then
            Assert.IsTrue(actualResponse.success, "Expected completion to succeed");
            Assert.IsTrue(actualResponse.complete, "Expected complete to be true");
            Assert.AreNotEqual(default(DateTime), actualResponse.completed_at, "Expected completed_at to be set");
            Assert.AreEqual(playtestId, actualResponse.playtest_id, "Expected the completed playtest id");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator Playtest_AcknowledgeDocumentWithoutStatements_ReturnsContentHash()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            string playtestId = null;
            yield return CreatePlaytest(id => playtestId = id);
            if (SetupFailed) { Assert.Fail("Failed to create playtest"); }

            string documentId = null;
            string documentContentHash = null;
            yield return CreateDocument(playtestId, false, null, (id, hash) => { documentId = id; documentContentHash = hash; });
            if (SetupFailed) { Assert.Fail("Failed to create document"); }

            // When
            LootLockerAcknowledgePlaytestDocumentResponse actualResponse = null;
            bool callCompleted = false;
            LootLockerSDKManager.AcknowledgePlaytestDocument(PlaytestSubdomain, documentId, null, response =>
            {
                actualResponse = response;
                callCompleted = true;
            });
            yield return new WaitUntil(() => callCompleted);

            // Then
            Assert.IsTrue(actualResponse.success, "Expected acknowledge to succeed");
            Assert.AreEqual(documentId, actualResponse.document_id, "Expected the acknowledged document id");
            Assert.AreEqual(documentContentHash, actualResponse.content_hash, "Expected the document's content hash");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator Playtest_AcknowledgeDocumentWithStatements_IsReportedInStatus()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            string playtestId = null;
            yield return CreatePlaytest(id => playtestId = id);
            if (SetupFailed) { Assert.Fail("Failed to create playtest"); }

            string statementId = Guid.NewGuid().ToString();
            string documentId = null;
            yield return CreateDocument(playtestId, false, new[] { statementId }, (id, _) => documentId = id);
            if (SetupFailed) { Assert.Fail("Failed to create document"); }

            LootLockerAcknowledgePlaytestDocumentResponse acknowledgeResponse = null;
            bool acknowledgeCompleted = false;
            LootLockerSDKManager.AcknowledgePlaytestDocument(PlaytestSubdomain, documentId,
                new LootLockerAcknowledgePlaytestDocumentRequest { statement_ids = new[] { statementId } }, response =>
                {
                    acknowledgeResponse = response;
                    acknowledgeCompleted = true;
                });
            yield return new WaitUntil(() => acknowledgeCompleted);
            Assert.IsTrue(acknowledgeResponse.success, "Expected acknowledge to succeed");

            // When
            LootLockerPlaytestOnboardingStatusResponse actualResponse = null;
            bool callCompleted = false;
            LootLockerSDKManager.GetPlaytestOnboardingStatus(PlaytestSubdomain, response =>
            {
                actualResponse = response;
                callCompleted = true;
            });
            yield return new WaitUntil(() => callCompleted);

            // Then
            Assert.IsTrue(actualResponse.success, "Expected call to succeed");
            Assert.AreEqual(1, actualResponse.documents.Length, "Expected one acknowledged document");
            Assert.AreEqual(documentId, actualResponse.documents[0].document_id, "Expected the acknowledged document id");
            Assert.AreEqual(1, actualResponse.documents[0].statement_ids.Length, "Expected one acknowledged statement");
            Assert.AreEqual(statementId, actualResponse.documents[0].statement_ids[0], "Expected the acknowledged statement id");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator Playtest_SubmitAnswersThenCompleteOnboarding_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given: a playtest whose required question comes from a linked survey.
            string playtestId = null;
            yield return CreatePlaytest(id => playtestId = id);
            if (SetupFailed) { Assert.Fail("Failed to create playtest"); }

            string surveyId = null;
            bool surveyCreated = false;
            LootLockerTestPlaytest.CreateSurveyWithRequiredQuestion("CI Playtest Survey", "ci_playtest_answer", response =>
            {
                if (!response.success)
                {
                    Debug.LogError($"Failed to create survey: {response.errorData?.message}");
                    SetupFailed = true;
                }
                surveyId = response.id;
                surveyCreated = true;
            });
            yield return new WaitUntil(() => surveyCreated);
            if (SetupFailed) { Assert.Fail("Failed to create survey"); }

            bool playtestUpdated = false;
            LootLockerTestPlaytest.LinkSurveyToPlaytest(playtestId, "CI Playtest", surveyId, response =>
            {
                if (!response.success)
                {
                    Debug.LogError($"Failed to link survey to playtest: {response.errorData?.message}");
                    SetupFailed = true;
                }
                playtestUpdated = true;
            });
            yield return new WaitUntil(() => playtestUpdated);
            if (SetupFailed) { Assert.Fail("Failed to link survey to playtest"); }

            // When: the required answer is submitted through the convenience method.
            LootLockerMetadataOperationsResponse submitResponse = null;
            bool submitCompleted = false;
            LootLockerSDKManager.SubmitPlaytestAnswers(
                new[]
                {
                    new LootLockerPlaytestAnswerSubmission
                    {
                        metadata_key = "ci_playtest_answer",
                        value = "From a friend",
                        type = LootLockerMetadataTypes.String
                    }
                },
                response =>
                {
                    submitResponse = response;
                    submitCompleted = true;
                });
            yield return new WaitUntil(() => submitCompleted);

            // Then
            Assert.IsTrue(submitResponse.success, "Expected submitting the answer to succeed");

            LootLockerCompletePlaytestOnboardingResponse completeResponse = null;
            bool completeCompleted = false;
            LootLockerSDKManager.CompletePlaytestOnboarding(PlaytestSubdomain, response =>
            {
                completeResponse = response;
                completeCompleted = true;
            });
            yield return new WaitUntil(() => completeCompleted);

            Assert.IsTrue(completeResponse.success, "Expected completion to succeed once the required answer is stored");
            Assert.IsTrue(completeResponse.complete, "Expected complete to be true");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator Playtest_SubmitAnswersWithNoAnswers_Fails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            LootLockerMetadataOperationsResponse actualResponse = null;
            bool callCompleted = false;
            LootLockerSDKManager.SubmitPlaytestAnswers(new LootLockerPlaytestAnswerSubmission[0], response =>
            {
                actualResponse = response;
                callCompleted = true;
            });
            yield return new WaitUntil(() => callCompleted);

            // Then
            Assert.IsFalse(actualResponse.success, "Expected an empty answer list to be rejected client side");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator Playtest_GetOnboardingStatusForUnknownSubdomain_Fails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            LootLockerPlaytestOnboardingStatusResponse actualResponse = null;
            bool callCompleted = false;
            LootLockerSDKManager.GetPlaytestOnboardingStatus("this-playtest-does-not-exist", response =>
            {
                actualResponse = response;
                callCompleted = true;
            });
            yield return new WaitUntil(() => callCompleted);

            // Then
            Assert.IsFalse(actualResponse.success, "Expected call to fail for an unknown subdomain");
        }

    }
}
