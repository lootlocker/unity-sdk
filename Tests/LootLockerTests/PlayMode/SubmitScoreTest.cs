using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace LootLockerTests.PlayMode
{

    public class SubmitScoreTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;
        private static readonly string GlobalPlayerLeaderboardKey = "gl_player_leaderboard";
        private static readonly string GlobalGenericLeaderboardKey = "gl_generic_leaderboard";

        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestCounter++;
            configCopy = LootLockerConfig.current;

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

            bool guestSessionCompleted = false;
            LootLockerGuestSessionResponse actualResponse = null;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                Debug.Log("Started a guest session ");
                actualResponse = response;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            if (!actualResponse.success)
            {
                SetupFailed = true;
                yield break;
            }

            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Global Player Leaderboard",
                key = GlobalPlayerLeaderboardKey,
                direction_method = "descending",
                enable_game_api_writes = true,
                has_metadata = true,
                overwrite_score_on_submit = false,
                type = "player"
            };

            bool leaderboardCreated = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) => {
                if (!response.success)
                {
                    SetupFailed = true;
                }
                leaderboardCreated = true;
            });
            yield return new WaitUntil(() => leaderboardCreated);
            
            createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Global Generic Leaderboard",
                key = GlobalGenericLeaderboardKey,
                direction_method = "descending",
                enable_game_api_writes = true,
                has_metadata = true,
                overwrite_score_on_submit = false,
                type = "generic"
            };

            leaderboardCreated = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) => {
                if (!response.success)
                {
                    SetupFailed = true;
                }
                leaderboardCreated = true;
            });
            yield return new WaitUntil(() => leaderboardCreated);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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

            LootLockerConfig.CreateNewSettings(configCopy.apiKey, configCopy.game_version, configCopy.domainKey,
                configCopy.logLevel, configCopy.allowTokenRefresh);
        }

        [UnityTest]
        public IEnumerator SubmitScore_SubmitToPlayerLeaderboard_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            int submittedScore = Random.Range(0, 100) + 1;

            //When
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, submittedScore, GlobalPlayerLeaderboardKey, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "SubmitScore failed");
            Assert.AreEqual(submittedScore, actualResponse.score, "Score was not as submitted");
        }

        [UnityTest]
        public IEnumerator SubmitScore_SubmitToGenericLeaderboard_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string memberID = LootLockerTestConfigurationUtilities.GetRandomNoun() + LootLockerTestConfigurationUtilities.GetRandomVerb();
            int submittedScore = Random.Range(0, 100) + 1;

            //When
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(memberID, submittedScore, GlobalGenericLeaderboardKey, (response) =>
            {

                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "SubmitScore failed");
            Assert.AreEqual(submittedScore, actualResponse.score, "Score was not as submitted");
        }

        [UnityTest]
        public IEnumerator SubmitScore_AttemptSubmitOnOverwriteScore_DoesNotUpdateScoreWhenScoreIsLower()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            var actualScore = Random.Range(2, 100);

            LootLockerSDKManager.SubmitScore(null, actualScore, GlobalPlayerLeaderboardKey, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);
            Assert.IsTrue(actualResponse.success, "Failed to submit score");

            //When
            LootLockerSubmitScoreResponse secondResponse = null;
            bool secondScoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, actualScore - 1, GlobalPlayerLeaderboardKey, (response) =>
            {
                secondResponse = response;
                secondScoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => secondScoreSubmittedCompleted);

            //Then
            Assert.IsTrue(secondResponse.success, "SubmitScore failed");
            Assert.AreEqual(actualResponse.score, secondResponse.score, "Score got updated, even though it was smaller");
        }

        [UnityTest]
        public IEnumerator SubmitScore_AttemptSubmitOnOverwriteScore_UpdatesScoreWhenScoreIsHigher()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            var actualScore = Random.Range(0, 100);

            LootLockerSDKManager.SubmitScore(null, actualScore, GlobalPlayerLeaderboardKey, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);
            Assert.IsTrue(actualResponse.success, "Failed to submit score");

            //When
            LootLockerSubmitScoreResponse secondResponse = null;
            bool secondScoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, actualScore + 1, GlobalPlayerLeaderboardKey, (response) =>
            {
                secondResponse = response;
                secondScoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => secondScoreSubmittedCompleted);

            //Then
            Assert.IsTrue(secondResponse.success, "SubmitScore failed");
            Assert.AreEqual(actualResponse.score + 1, secondResponse.score, "Score did not get updated, even though it was higher");
        }

        [UnityTest]
        public IEnumerator SubmitScore_SubmitOnOverwriteScoreWhenOverwriteIsAllowed_UpdatesScore()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string leaderboardKey = "overwrites_enabled_leaderboard";
            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Overwrites Enabled Leaderboard",
                key = leaderboardKey,
                direction_method = LootLockerLeaderboardSortDirection.descending.ToString(),
                enable_game_api_writes = true,
                has_metadata = false,
                overwrite_score_on_submit = true,
                type = "player"
            };

            bool leaderboardCreated = false;
            bool leaderboardSuccess = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                leaderboardSuccess = response.success;
                leaderboardCreated = true;

            });
            yield return new WaitUntil(() => leaderboardCreated);

            Assert.IsTrue(leaderboardSuccess, "Failed to create leaderboard");

            LootLockerSubmitScoreResponse actualResponse = null;
            bool submitScoreCompleted = false;
            var actualScore = Random.Range(50, 100);
            LootLockerSDKManager.SubmitScore(null, actualScore, leaderboardKey, (response) =>
            {
                actualResponse = response;
                submitScoreCompleted = true;
            });
            yield return new WaitUntil(() => submitScoreCompleted);
            Assert.IsTrue(actualResponse.success, "Failed to submit score");

            //When
            LootLockerSubmitScoreResponse secondResponse = null;
            bool secondScoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, actualScore - 1, leaderboardKey, (response) =>
            {
                secondResponse = response;
                secondScoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => secondScoreSubmittedCompleted);

            //Then
            Assert.AreNotEqual(actualResponse.score, secondResponse.score, "Score got updated, even though it was smaller");
            Assert.IsTrue(secondResponse.success, "SubmitScore failed");
        }

        [UnityTest]
        public IEnumerator SubmitScore_SubmitToLeaderboardWithMetadata_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string submittedMetadata = "Random Message";

            //When
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;

            LootLockerSDKManager.SubmitScore(null, Random.Range(0, 100), GlobalPlayerLeaderboardKey, submittedMetadata, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "SubmitScore failed");
            Assert.AreEqual(submittedMetadata, actualResponse.metadata, "Metadata was not as expected");
        }

        [UnityTest]
        public IEnumerator SubmitScore_SubmitMetadataToLeaderboardWithoutMetadata_IgnoresMetadata()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string leaderboardKey = "non_metadata_leaderboard";
            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Non Metadata Leaderboard",
                key = leaderboardKey,
                direction_method = LootLockerLeaderboardSortDirection.descending.ToString(),
                enable_game_api_writes = true,
                has_metadata = false,
                overwrite_score_on_submit = false,
                type = "player"
            };

            bool leaderboardCreated = false;
            bool leaderboardSuccess = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                leaderboardSuccess = response.success;
                leaderboardCreated = true;

            });
            yield return new WaitUntil(() => leaderboardCreated);

            Assert.IsTrue(leaderboardSuccess, "Failed to create leaderboard");

            string submittedMetadata = "Random Message";

            //When
            LootLockerSubmitScoreResponse actualResponse = null;
            bool scoreSubmittedCompleted = false;
            LootLockerSDKManager.SubmitScore(null, Random.Range(0, 100), leaderboardKey, submittedMetadata, (response) =>
            {
                actualResponse = response;
                scoreSubmittedCompleted = true;
            });
            yield return new WaitUntil(() => scoreSubmittedCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "SubmitScore failed");
            Assert.IsEmpty(actualResponse.metadata, "Metadata was not empty");
        }

    }
}