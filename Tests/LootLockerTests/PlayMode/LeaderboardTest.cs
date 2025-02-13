using System;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;
using System.Collections.Generic;

namespace LootLockerTests.PlayMode
{
    public class LeaderboardTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;
        string leaderboardKey = "gl_leaderboard";

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
                    SetupFailed = true;
                    gameCreationCallCompleted = true;
                    return;
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
                    SetupFailed = true;
                }
                enableGuestLoginCallCompleted = true;
            });
            yield return new WaitUntil(() => enableGuestLoginCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }
            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Failed to initialize LootLocker");

            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Global Leaderboard",
                key = leaderboardKey,
                direction_method = LootLockerLeaderboardSortDirection.descending.ToString(),
                enable_game_api_writes = true,
                has_metadata = true,
                overwrite_score_on_submit = false,
                type = "player"
            };

            bool leaderboardCreated = false;
            bool leaderboardSuccess = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                leaderboardSuccess = response.success;
                SetupFailed |= !leaderboardSuccess;
                leaderboardCreated = true;

            });
            yield return new WaitUntil(() => leaderboardCreated);

            Assert.IsTrue(leaderboardSuccess, "Failed to create leaderboard");

            // Sign in client
            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(GUID.Generate().ToString(), response =>
            {
                SetupFailed |= !response.success;
                guestLoginCompleted = true;
            });
            yield return new WaitUntil(() => guestLoginCompleted);
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
        public IEnumerator Leaderboard_ListTopTenAsPlayer_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            int submittedScores = 0;
            List<string> Users = new List<string> {};
            List<int> Scores = new List<int> { };
            for (; submittedScores < 10; submittedScores++)
            {
                bool guestSessionCompleted = false;
                string user = $"User_number_{submittedScores}";
                string playerUlid = "";
                LootLockerSDKManager.StartGuestSession(user, (response) =>
                {
                    playerUlid = response.player_ulid;
                    guestSessionCompleted = true;
                });
                yield return new WaitUntil(() => guestSessionCompleted);

                Users.Add(playerUlid);
                bool scoreSubmitted = false;
                int score = (submittedScores + 1) * 100;
                LootLockerSDKManager.SubmitScore(null, score, leaderboardKey, (response) => 
                {
                    scoreSubmitted = true;
                });
                yield return new WaitUntil(()=> scoreSubmitted);
                Scores.Add(score);
            }

            //When
            LootLockerGetScoreListResponse actualResponse = null;
            bool scoreListCompleted = false;
            LootLockerSDKManager.GetScoreList(leaderboardKey, 100, 0, (response) =>
            {
                actualResponse = response;
                scoreListCompleted = true;
            });
            yield return new WaitUntil(() => scoreListCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "GetScoreList request failed");
            Assert.AreEqual(submittedScores, actualResponse.items.Length, "got more scores than expected");
            for (int i = 0; i < actualResponse.items.Length; i++)
            {
                Assert.AreEqual(i+1, actualResponse.items[i].rank, $"Did not get expected rank order");

                int expectedIndex = Users.Count - 1 - i; // List is descending and scores are increasing so reverse order
                string expectedUlid = Users[expectedIndex];
                string actualUlid = actualResponse.items[i].player.ulid;
                Assert.AreEqual(expectedUlid, actualUlid, $"Got unexpected ulid at position {i}");

                int expectedScore = Scores[expectedIndex];
                int actualScore = actualResponse.items[i].score;
                Assert.AreEqual(expectedScore, actualScore, $"Got unexpected score at position {i}");
            }
        }

        [UnityTest]
        public IEnumerator Leaderboard_ListTopTenAsGeneric_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given 
            string genericLeaderboardKey = "genericLeaderboard";
            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = "Local Generic Leaderboard",
                key = genericLeaderboardKey,
                direction_method = LootLockerLeaderboardSortDirection.ascending.ToString(),
                enable_game_api_writes = true,
                has_metadata = true,
                overwrite_score_on_submit = false,
                type = "generic"
            };

            bool leaderboardCreated = false;
            bool leaderboardSuccess = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                leaderboardSuccess = response.success;
                leaderboardCreated = true;

            });
            yield return new WaitUntil(() => leaderboardCreated);
            Assert.IsTrue(leaderboardSuccess, "Could not create generic leaderboard");


            int submittedScores = 0;
            List<string> Users = new List<string> { };
            List<int> Scores = new List<int> { };
            for (; submittedScores < 10; submittedScores++)
            {
                bool guestSessionCompleted = false;
                string user = $"User_number_{submittedScores}";
                string playerPublicUID = "";
                LootLockerSDKManager.StartGuestSession(user, (response) =>
                {
                    playerPublicUID = response.public_uid;
                    guestSessionCompleted = true;
                });
                yield return new WaitUntil(() => guestSessionCompleted);

                Users.Add(playerPublicUID);
                bool scoreSubmitted = false;
                int score = (submittedScores + 1) * 100;
                LootLockerSDKManager.SubmitScore(playerPublicUID, score, genericLeaderboardKey, (response) =>
                {
                    scoreSubmitted = true;
                });
                yield return new WaitUntil(() => scoreSubmitted);
                Scores.Add(score);
            }

            //When
            LootLockerGetScoreListResponse actualResponse = null;
            bool scoreListCompleted = false;
            LootLockerSDKManager.GetScoreList(genericLeaderboardKey, 100, 0, (response) =>
            {
                actualResponse = response;
                scoreListCompleted = true;
            });
            yield return new WaitUntil(() => scoreListCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "GetScoreList request failed");
            Assert.AreEqual(submittedScores, actualResponse.items?.Length, "Did not get the expected amount of scores");
            for (int i = 0; i < actualResponse.items?.Length; i++)
            {
                Assert.AreEqual(i + 1, actualResponse.items[i].rank, $"Did not get expected rank order");

                int expectedIndex = i; // List is ascending and scores are increasing
                string expectedUlid = Users[expectedIndex];
                string actualUlid = actualResponse.items[i].member_id;
                Assert.AreEqual(expectedUlid, actualUlid, $"Got unexpected public uid at position {i}");

                int expectedScore = Scores[expectedIndex];
                int actualScore = actualResponse.items[i].score;
                Assert.AreEqual(expectedScore, actualScore, $"Got unexpected score at position {i}");
            }
        }


        [UnityTest]
        public IEnumerator Leaderboard_ListScoresThatHaveMetadata_GetsMetadata()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            for (int i = 0; i < Random.Range(3, 8); i++)
            {
                string user = $"User_number_{i}";
                bool guestSessionCompleted = false;
                LootLockerSDKManager.StartGuestSession(user, (response) =>
                {
                    guestSessionCompleted = true;

                });
                yield return new WaitUntil(() => guestSessionCompleted);

                bool scoreSubmitted = false;
                LootLockerSDKManager.SubmitScore(null, i, leaderboardKey, $"Metadata_{i}", (response) =>
                {
                    scoreSubmitted = true;
                });
                yield return new WaitUntil(() => scoreSubmitted);

            }

            //When
            LootLockerGetScoreListResponse actualResponse = null;
            bool scoreListCompleted = false;
            LootLockerSDKManager.GetScoreList(leaderboardKey, 100, (response) =>
            {
                actualResponse = response;
                scoreListCompleted = true;
            });
            yield return new WaitUntil(() => scoreListCompleted);
            //Then
            Assert.IsTrue(actualResponse.success, "GetScoreList call failed");
            foreach(var item in actualResponse.items)
            {
                Assert.AreEqual($"Metadata_{item.score}", item.metadata, "Leaderboard item did not have the expected metadata");
            }

        }
    }
}
