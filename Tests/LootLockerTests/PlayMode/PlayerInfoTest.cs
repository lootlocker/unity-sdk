using System;
using System.Collections;
using System.Collections.Generic;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;


namespace LootLockerTests.PlayMode
{
    public class PlayerInfoTest
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
        public IEnumerator PlayerInfo_GetCurrentPlayerInfo_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            DateTime expectedCreatedAt = DateTime.MinValue;
            string expectedPlayerId = "";
            string expectedPlayerPublicUid = "";
            int expectedPlayerLegacyId = -1;
            bool guestSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                if (!response.success)
                {
                    guestSessionCompleted = true;
                    return;
                }
                expectedCreatedAt = response.player_created_at;
                expectedPlayerId = response.player_ulid;
                expectedPlayerPublicUid = response.public_uid;
                expectedPlayerLegacyId = response.player_id;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            Assert.Greater(expectedCreatedAt, DateTime.MinValue, "Guest Session failed");

            //When
            bool currentPlayerInfoRequestSucceeded = false;
            DateTime actualCreatedAt = DateTime.MinValue;
            string actualPlayerId = "";
            string actualPlayerPublicUid = "";
            int actualPlayerLegacyId = -1;
            bool getCurrentPlayerInfoCompleted = false;
            LootLockerSDKManager.GetCurrentPlayerInfo((response) =>
            {
                currentPlayerInfoRequestSucceeded = response.success;
                if(currentPlayerInfoRequestSucceeded)
                {
                    actualPlayerId = response.info.id;
                    actualPlayerPublicUid = response.info.public_uid;
                    actualPlayerLegacyId = response.info.legacy_id;
                    actualCreatedAt = response.info.created_at;
                }
                getCurrentPlayerInfoCompleted = true;
            });
            yield return new WaitUntil(() => getCurrentPlayerInfoCompleted);

            //Then
            Assert.IsTrue(currentPlayerInfoRequestSucceeded, "GetCurrentPlayerInfo request failed");
            Assert.AreEqual(expectedCreatedAt, actualCreatedAt, "Player creation time was not the same between session start and player info fetch");
            Assert.AreEqual(expectedPlayerId, actualPlayerId, "Player id was not the same between session start and player info fetch");
            Assert.AreEqual(expectedPlayerPublicUid, actualPlayerPublicUid, "Player public uid was not the same between session start and player info fetch");
            Assert.AreEqual(expectedPlayerLegacyId, actualPlayerLegacyId, "Player legacy id was not the same between session start and player info fetch");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator PlayerInfo_ListPlayerInfoForMultiplePlayersUsingDifferentIds_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            //Given
            int playersToCreate = 15;
            Dictionary<string, LootLockerPlayerInfo> expectedPlayerInfoToPlayerIdMap = new Dictionary<string, LootLockerPlayerInfo>();
            Dictionary<string, LootLockerPlayerInfo> expectedPlayerInfoToPlayerPublicUidMap = new Dictionary<string, LootLockerPlayerInfo>();
            Dictionary<int, LootLockerPlayerInfo> expectedPlayerInfoToPlayerLegacyIdMap = new Dictionary<int, LootLockerPlayerInfo>();
            for (int playersCreated = 0; playersCreated < playersToCreate; playersCreated++)
            {
                bool playerCreateCompleted = false;
                string errorMessage = null;
                LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), (sessionResponse) =>
                {
                    if (!sessionResponse.success)
                    {
                        errorMessage = sessionResponse.errorData.ToString();
                        playerCreateCompleted = true;
                        return;
                    }
                    var playerInfo = new LootLockerPlayerInfo { created_at = sessionResponse.player_created_at, id = sessionResponse.player_ulid, name = sessionResponse.player_name, legacy_id = sessionResponse.player_id, public_uid = sessionResponse.public_uid };
                    LootLockerSDKManager.SetPlayerName(sessionResponse.public_uid, (setPlayerNameResponse) =>
                    {
                        if (!setPlayerNameResponse.success)
                        {
                            errorMessage = setPlayerNameResponse.errorData.ToString();
                            playerCreateCompleted = true;
                            return;
                        }
                        playerInfo.name = setPlayerNameResponse.name;
                        expectedPlayerInfoToPlayerIdMap.Add(playerInfo.id, playerInfo);
                        expectedPlayerInfoToPlayerPublicUidMap.Add(playerInfo.public_uid, playerInfo);
                        expectedPlayerInfoToPlayerLegacyIdMap.Add(playerInfo.legacy_id, playerInfo);
                        playerCreateCompleted = true;
                    }, sessionResponse.player_ulid);
                });
                yield return new WaitUntil(() => playerCreateCompleted);
                Assert.IsNull(errorMessage, errorMessage);
            }

            var guestSessionSucceeded = false;
            var guestSessionCompleted = false;
            string guestUlid = null;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), (sessionResponse) =>
            {
                guestSessionSucceeded = sessionResponse?.success ?? false;
                guestSessionCompleted = true;
                guestUlid = sessionResponse?.player_ulid ?? string.Empty;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            Assert.IsTrue(guestSessionSucceeded, "Guest Session Start failed");

            //Then
            List<string> createdPlayerIds = new List<string>(expectedPlayerInfoToPlayerIdMap.Keys);
            List<string> playerIdsToLookUp = new List<string>();
            List<string> createdPlayerPublicUids = new List<string>(expectedPlayerInfoToPlayerPublicUidMap.Keys);
            List<string> playerPublicUidsToLookUp = new List<string>();
            List<int> createdPlayerLegacyIds = new List<int>(expectedPlayerInfoToPlayerLegacyIdMap.Keys);
            List<int> playerLegacyIdsToLookUp = new List<int>();
            for (int i = 0; i < playersToCreate; i++)
            {
                if(i < playersToCreate/3)
                    playerIdsToLookUp.Add(createdPlayerIds[i]);
                if(i < (playersToCreate / 3) * 2)
                    playerPublicUidsToLookUp.Add(createdPlayerPublicUids[i]);
                else
                    playerLegacyIdsToLookUp.Add(createdPlayerLegacyIds[i]);
            }

            LootLockerListPlayerInfoResponse actualResponse = null;
            bool listPlayerInfoCompleted = false;
            LootLockerSDKManager.ListPlayerInfo(playerIdsToLookUp.ToArray(), playerLegacyIdsToLookUp.ToArray(), playerPublicUidsToLookUp.ToArray(), (response) =>
            {
                actualResponse = response;
                listPlayerInfoCompleted = true;
            }, guestUlid);
            yield return new WaitUntil(() => listPlayerInfoCompleted);

            Assert.IsTrue(actualResponse?.success, "ListPlayerInfo request failed");
            Assert.AreEqual(playersToCreate, actualResponse.info.Length, "The same amount of players were not returned as were requested");
            int j = 0;
            foreach(var actualPlayerInfo in actualResponse.info)
            {
                Assert.IsTrue(playerLegacyIdsToLookUp.Contains(actualPlayerInfo.legacy_id) || playerIdsToLookUp.Contains(actualPlayerInfo.id) || playerPublicUidsToLookUp.Contains(actualPlayerInfo.public_uid), "Found id that was not requested");
                LootLockerPlayerInfo expectedPlayerInfo = null;
                if (j%3 == 0)
                    Assert.IsTrue(expectedPlayerInfoToPlayerIdMap.TryGetValue(actualPlayerInfo.id, out expectedPlayerInfo));
                else if(j%3 == 1)
                    Assert.IsTrue(expectedPlayerInfoToPlayerLegacyIdMap.TryGetValue(actualPlayerInfo.legacy_id, out expectedPlayerInfo));
                else if (j % 3 == 2)
                    Assert.IsTrue(expectedPlayerInfoToPlayerPublicUidMap.TryGetValue(actualPlayerInfo.public_uid, out expectedPlayerInfo));

                Assert.IsNotNull(expectedPlayerInfo);
                Assert.Greater(actualPlayerInfo.created_at, expectedPlayerInfo.created_at.AddMinutes(-5), "Player creation time was not as expected for player " + j);
                Assert.LessOrEqual(actualPlayerInfo.created_at, expectedPlayerInfo.created_at.AddMinutes(5), "Player creation time was not as expected for player " + j);
                Assert.AreEqual(expectedPlayerInfo.name, actualPlayerInfo.name, "Player name was not as expected for player " + j);
                Assert.AreEqual(expectedPlayerInfo.id, actualPlayerInfo.id, "Player id was not as expected for player " + j);
                Assert.AreEqual(expectedPlayerInfo.public_uid, actualPlayerInfo.public_uid, "Player public uid was not as expected for player " + j);
                Assert.AreEqual(expectedPlayerInfo.legacy_id, actualPlayerInfo.legacy_id, "Player legacy id was not as expected for player " + j);
                ++j;
            }
        }


    }
}

