using System;
using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;



namespace LootLockerTests.PlayMode
{
    public class PlayerStorageTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;
        private string player1ID = null;

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
                    Debug.LogError(errorMessage);
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
                SetupFailed |= !success;
                enableGuestLoginCallCompleted = true;
            });
            yield return new WaitUntil(() => enableGuestLoginCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }
            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Successfully created test game and initialized LootLocker");

            bool guestSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                player1ID = response.player_id.ToString();
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
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

        public LootLockerGetPersistentStorageRequest GetStorageTemplate()
        {
            var data = new LootLockerGetPersistentStorageRequest();
            data.AddToPayload(new LootLockerPayload
            {
                key = "ClanID",
                value = "674219761",
                is_public = true,
            });
            data.AddToPayload(new LootLockerPayload
            {
                key = "Referral",
                value = "gamer123321",
                is_public = false,
            });
            data.AddToPayload(new LootLockerPayload
            {
                key = "NumberOfIceCreamStolen",
                value = "too many",
                is_public = true,
            });

            return data;
        }

        [UnityTest]
        public IEnumerator PlayerStorage_CreatePayload_SucceedsAndReturnsStorage()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            LootLockerGetPersistentStorageRequest actualRequest = GetStorageTemplate();
            //When
            LootLockerGetPersistentStorageResponse actualResponse = null;
            bool createKeyValueCompleted = false;
            LootLockerSDKManager.UpdateOrCreateKeyValue(actualRequest, (onComplete) =>
            {
                actualResponse = onComplete;
                createKeyValueCompleted = true;
            });
            yield return new WaitUntil(() => createKeyValueCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Player Persistent Storage could not be updated");
            Assert.AreEqual(actualRequest.payload.Count, actualResponse.payload.Length, "Payload was not of the expected size");
            int matchedKVPairs = 0;
            foreach (var expectedKVPair in actualRequest.payload)
            {
                foreach (var actualKVPair in actualResponse.payload)
                {
                    if (expectedKVPair.key.Equals(actualKVPair.key))
                    {
                        matchedKVPairs++;
                        Assert.AreEqual(expectedKVPair.value, actualKVPair.value, $"Value did not match for key {expectedKVPair.key}");
                        Assert.AreEqual(expectedKVPair.is_public, actualKVPair.is_public, $"Is Public flag did not match for key {expectedKVPair.key}");
                    }
                }
            }
            Assert.AreEqual(actualRequest.payload.Count, matchedKVPairs, "Not all storage kv pairs were matched in the response");
        }

        [UnityTest]
        public IEnumerator PlayerStorage_UpdatePayload_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            LootLockerGetPersistentStorageRequest actualRequest = GetStorageTemplate();
            
            LootLockerGetPersistentStorageResponse initialResponse = null;
            bool createKeyValueCompleted = false;
            LootLockerSDKManager.UpdateOrCreateKeyValue(actualRequest, (onComplete) =>
            {
                initialResponse = onComplete;
                createKeyValueCompleted = true;
            });
            yield return new WaitUntil(() => createKeyValueCompleted);

            Assert.IsTrue(initialResponse.success, "Player Persistent Storage could not be updated");
            Assert.AreEqual(actualRequest.payload.Count, initialResponse.payload.Length, "Payload did not contain the expected number of items");

            //When
            LootLockerGetPersistentStorageRequest updateRequest = new LootLockerGetPersistentStorageRequest();
            updateRequest.payload.AddRange(initialResponse.payload);
            foreach (var pair in updateRequest.payload)
            {
                pair.is_public = !pair.is_public;
                pair.value = pair.value.GetHashCode().ToString();
            }

            bool updateSucceeded = false;
            bool updateCompleted = false;
            LootLockerSDKManager.UpdateOrCreateKeyValue(updateRequest,updateResponse =>
            {
                updateSucceeded = updateResponse.success;
                updateCompleted = true;
            } );

            yield return new WaitUntil(() => updateCompleted);

            Assert.IsTrue(updateSucceeded, "Update of key values failed");

            //Then
            LootLockerGetPersistentStorageResponse actualResponse = null;
            bool persistentStorageCompleted = false;
            LootLockerSDKManager.GetEntirePersistentStorage((response) =>
            {
                actualResponse = response;
                persistentStorageCompleted = true;
            });
            yield return new WaitUntil(() => persistentStorageCompleted);

            Assert.IsTrue(actualResponse.success, "Could not get persistent storage");
            Assert.AreEqual(actualRequest.payload.Count, actualResponse.payload.Length, "Payload was not of the expected size");
            int keysMatched = 0;
            foreach (var expectedKVPair in actualRequest.payload)
            {
                foreach (var actualKVPair in actualResponse.payload)
                {
                    if (expectedKVPair.key.Equals(actualKVPair.key))
                    {
                        keysMatched++;
                        Assert.AreNotEqual(expectedKVPair.is_public, actualKVPair.is_public, "Is Public flag was not flipped");
                        Assert.AreNotEqual(expectedKVPair.value, actualKVPair.value, "Value did not change from creation");
                        foreach (var updatedKVPair in updateRequest.payload)
                        {
                            if (updatedKVPair.key.Equals(expectedKVPair.key))
                            {
                                Assert.AreEqual(updatedKVPair.value, actualKVPair.value, "Value was not what the update request set");
                            }
                        }
                    }
                }
            }
            Assert.AreEqual(actualRequest.payload.Count, keysMatched, "Not all keys were found in fetched payload");
        }

        [UnityTest]
        public IEnumerator PlayerStorage_GetOtherPlayersStorage_GetsOnlyPublicValues()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            LootLockerGetPersistentStorageRequest actualRequest = GetStorageTemplate();

            LootLockerGetPersistentStorageResponse firstResponse = null;
            bool createKeyValueCompleted = false;
            LootLockerSDKManager.UpdateOrCreateKeyValue(actualRequest, (onComplete) =>
            {
                firstResponse = onComplete;
                createKeyValueCompleted = true;
            });
            yield return new WaitUntil(() => createKeyValueCompleted);

            Assert.IsTrue(firstResponse.success, "Player Persistent Storage could not be updated");
            Assert.IsNotEmpty(firstResponse.payload, "Payload was empty!");

            bool endSessionCompleted = false;
            LootLockerSDKManager.EndSession((response) =>
            {
                endSessionCompleted = true;
            });
            yield return new WaitUntil(() => endSessionCompleted);

            string newIdentifier = Guid.NewGuid().ToString();
            bool secondSessionCompleted = false;
            bool secondSessionStartSucceeded = false;
            LootLockerSDKManager.StartGuestSession(newIdentifier, (guestResponse) =>
            {
                secondSessionStartSucceeded = true;
                secondSessionCompleted = true;
            });
            yield return new WaitUntil(() => secondSessionCompleted);

            Assert.IsTrue(secondSessionStartSucceeded, "Failed to start second guest session");

            //When
            LootLockerGetPersistentStorageResponse actualResponse = null;
            bool otherPlayersStorageCompleted = false;
            LootLockerSDKManager.GetOtherPlayersPublicKeyValuePairs(player1ID, (response) =>
            {
                actualResponse = response;
                otherPlayersStorageCompleted = true;
            });
            yield return new WaitUntil(() => otherPlayersStorageCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Could not get other players storage!");
            int publicKVPairCount = 0;
            int kvPairsMatched = 0;
            foreach (var expectedKVPair in actualRequest.payload)
            {
                if (expectedKVPair.is_public)
                {
                    publicKVPairCount++;
                    foreach (var actualKVPairs in actualResponse.payload)
                    {
                        if (expectedKVPair.key.Equals(actualKVPairs.key))
                        {
                            kvPairsMatched++;
                            Assert.AreEqual(expectedKVPair.value, actualKVPairs.value, "Value as gotten by player 2 was not as set by player 1");
                        }
                    }
                }
            }
            Assert.AreEqual(publicKVPairCount, actualResponse.payload.Length, "Player 2's received storage length did not match the amount of public keys in player 1's storage");
            Assert.AreEqual(kvPairsMatched, actualResponse.payload.Length, "Not all received keys matched with the expected keys");
        }
    }
}