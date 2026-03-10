using System;
using System.Collections;
using System.Linq;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class PlayerInventoryTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;
        private int createdAssetId = 0;
        private string createdAssetUlid = string.Empty;
        private int sessionPlayerId = 0;

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

            bool getAssetContextsCallCompleted = false;
            int contextId = 0;
            LootLockerTestAssets.GetAssetContexts((success, errorMessage, contextResponse) =>
            {
                if (!success)
                {
                    Debug.LogError(errorMessage);
                    SetupFailed = true;
                }
                contextId = contextResponse?.contexts?[0].id ?? 0;
                getAssetContextsCallCompleted = true;
            });
            yield return new WaitUntil(() => getAssetContextsCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }

            bool createAssetCallCompleted = false;
            LootLockerTestAssets.CreateAsset(contextId, (assetResponse) =>
            {
                if (assetResponse == null || !assetResponse.success)
                {
                    Debug.LogError("Failed to create asset: " + assetResponse?.errorData?.message);
                    SetupFailed = true;
                    createAssetCallCompleted = true;
                    return;
                }

                createdAssetId = assetResponse.asset.id;
                createdAssetUlid = assetResponse.asset.ulid;
                LootLockerTestAssets.ActivateAsset(createdAssetId, (activateResponse) =>
                {
                    if (activateResponse == null || !activateResponse.success)
                    {
                        Debug.LogError("Failed to activate asset: " + activateResponse?.errorData?.message);
                        SetupFailed = true;
                    }
                    createAssetCallCompleted = true;
                });
            });
            yield return new WaitUntil(() => createAssetCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }

            bool metadataCallCompleted = false;
            LootLockerTestAssets.AddMetadataToAsset(createdAssetUlid, "inventory_metadata_key", "inventory_metadata_value", (response) =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError("Failed to add metadata to asset: " + response?.errorData?.message);
                    SetupFailed = true;
                }
                metadataCallCompleted = true;
            });
            yield return new WaitUntil(() => metadataCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }

            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                SetupFailed |= !response.success;
                sessionPlayerId = response.player_id;
                guestLoginCompleted = true;
            });
            yield return new WaitUntil(() => guestLoginCompleted);
            if (SetupFailed)
            {
                yield break;
            }

            bool grantCallCompleted = false;
            LootLockerTestAssets.AdminGrantAssetToPlayerInventory(sessionPlayerId, createdAssetId, (grantResponse) =>
            {
                if (grantResponse == null || !grantResponse.success)
                {
                    Debug.LogError("Failed to grant asset to player inventory: " + grantResponse?.errorData?.message);
                    SetupFailed = true;
                }
                grantCallCompleted = true;
            });
            yield return new WaitUntil(() => grantCallCompleted);

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
        [Timeout(360_000)]
        [Ignore("Blocked by backend issue lootlocker/index#1384 - metadata on fast inventory endpoint not yet deployed")]
        public IEnumerator PlayerInventory_ListPlayerInventoryWithMetadataInclude_ReturnsMetadata()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            var requestWithMetadata = new LootLockerListSimplifiedInventoryRequest
            {
                includes = new LootLockerListSimplifiedInventoryIncludes
                {
                    metadata = true,
                },
                filters = new LootLockerListSimplifiedFilters
                {
                    asset_ids = new[] { createdAssetId },
                },
            };

            // When
            bool listWithMetadataCallCompleted = false;
            LootLockerSimpleInventoryResponse withMetadataResponse = null;
            LootLockerSDKManager.ListPlayerInventory(requestWithMetadata, 100, 1, (response) =>
            {
                withMetadataResponse = response;
                listWithMetadataCallCompleted = true;
            });
            yield return new WaitUntil(() => listWithMetadataCallCompleted);

            // Then
            Assert.IsTrue(withMetadataResponse.success, withMetadataResponse.errorData?.ToString() ?? "ListPlayerInventory call with metadata failed");
            Assert.IsNotNull(withMetadataResponse.items, "Inventory items should not be null");
            Assert.IsTrue(withMetadataResponse.items.Any(item => item.asset_id == createdAssetId), "Expected granted asset to be included in simplified inventory listing");

            var matchingItem = withMetadataResponse.items.First(item => item.asset_id == createdAssetId);
            Assert.IsNotNull(matchingItem.metadata, "Inventory item metadata should be included when requested");
            Assert.IsTrue(matchingItem.metadata.Length > 0, "Inventory item metadata should not be empty when requested");
            Assert.IsTrue(matchingItem.metadata.Any(entry => entry.key == "inventory_metadata_key"), "Expected metadata key on returned inventory item");

            // Given
            var requestWithoutMetadata = new LootLockerListSimplifiedInventoryRequest
            {
                filters = new LootLockerListSimplifiedFilters
                {
                    asset_ids = new[] { createdAssetId },
                },
            };

            // When
            bool listWithoutMetadataCallCompleted = false;
            LootLockerSimpleInventoryResponse withoutMetadataResponse = null;
            LootLockerSDKManager.ListPlayerInventory(requestWithoutMetadata, 100, 1, (response) =>
            {
                withoutMetadataResponse = response;
                listWithoutMetadataCallCompleted = true;
            });
            yield return new WaitUntil(() => listWithoutMetadataCallCompleted);

            // Then
            Assert.IsTrue(withoutMetadataResponse.success, withoutMetadataResponse.errorData?.ToString() ?? "ListPlayerInventory call without metadata failed");
            Assert.IsNotNull(withoutMetadataResponse.items, "Inventory items should not be null");
            Assert.IsTrue(withoutMetadataResponse.items.Any(item => item.asset_id == createdAssetId), "Expected granted asset to be included in simplified inventory listing without metadata");

            var itemWithoutMetadata = withoutMetadataResponse.items.First(item => item.asset_id == createdAssetId);
            Assert.IsTrue(itemWithoutMetadata.metadata == null || itemWithoutMetadata.metadata.Length == 0, "Inventory item metadata should be omitted when not requested");
        }
    }
}
