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
    public class AssetTests
    {
        // Setup and teardown similar to LeaderboardTest
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;
        private int numberOfAssetsToCreate = 5;
        private int numberOfAssetDataEntitiesToAdd = 7;
        private int numberOfAssetMetadataToAdd = 8;
        private int numberOfAssetStorageKeysToAdd = 4;
        private List<int> createdAssetIds = new List<int>();
        private List<string> createdAssetUlids = new List<string>();

        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestCounter++;
            createdAssetIds.Clear();
            createdAssetUlids.Clear();
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

            for (int i = 0; i < numberOfAssetsToCreate; i++)
            {
                bool assetCreationCallCompleted = false;
                LootLockerTestAssets.CreateAsset(contextId, (assetResponse) =>
                {
                    if (assetResponse == null || !assetResponse.success)
                    {
                        Debug.LogError("Failed to create asset: " + assetResponse?.errorData?.message);
                        SetupFailed = true;
                        assetCreationCallCompleted = true;
                    }
                    else
                    {
                        createdAssetIds.Add(assetResponse.asset.id);
                        createdAssetUlids.Add(assetResponse.asset.ulid);
                        LootLockerTestAssets.ActivateAsset(assetResponse.asset.id, (activateResponse) =>
                        {
                            if (activateResponse == null || !activateResponse.success)
                            {
                                Debug.LogError("Failed to activate asset: " + activateResponse?.errorData?.message);
                                SetupFailed = true;
                            }

                            string stringAsset = assetResponse.text;
                            stringAsset = stringAsset.Replace("{\"success\":true,\"asset\":", "");
                            stringAsset = stringAsset.Remove(stringAsset.Length - 1);
                            string storageArrayString = "\"storage\": [";
                            for (int j = 0; j < numberOfAssetStorageKeysToAdd; j++)
                            {
                                if (j > 0)
                                {
                                    storageArrayString += ",";
                                }
                                storageArrayString += "{\"key\":\"storage_key_" + j + "\",\"value\":\"storage_value_" + j + "\"}";
                            }
                            storageArrayString += "]";

                            stringAsset = stringAsset.Replace("\"storage\":[]", storageArrayString);
                            LootLockerTestAssets.UpdateAsset(stringAsset, assetResponse.asset.id, (updateResponse) =>
                            {
                                if (updateResponse == null || !updateResponse.success)
                                {
                                    Debug.LogError("Failed to update asset: " + updateResponse?.errorData?.message);
                                    SetupFailed = true;
                                }
                                assetCreationCallCompleted = true;
                            });
                        });
                    }
                });
                yield return new WaitUntil(() => assetCreationCallCompleted);
                if (SetupFailed)
                {
                    yield break;
                }
            }

            for (int i = 0; i < createdAssetIds.Count; i++)
            {
                for (int j = 0; j < numberOfAssetDataEntitiesToAdd; j++)
                {
                    bool addDataEntityCallCompleted = false;
                    LootLockerTestAssets.AddDataEntityToAsset(createdAssetIds[i], "data_entity_" + j, "data_value_" + j, (response) =>
                    {
                        if (response == null || !response.success)
                        {
                            Debug.LogError("Failed to add data entity to asset: " + response?.errorData?.message);
                            SetupFailed = true;
                        }
                        addDataEntityCallCompleted = true;
                    });
                    yield return new WaitUntil(() => addDataEntityCallCompleted);
                    if (SetupFailed)
                    {
                        yield break;
                    }
                }

                for (int j = 0; j < numberOfAssetMetadataToAdd; j++)
                {
                    bool addMetadataCallCompleted = false;
                    LootLockerTestAssets.AddMetadataToAsset(createdAssetUlids[i], "metadata_key_" + j, "metadata_value_" + j, (response) =>
                    {
                        if (response == null || !response.success)
                        {
                            Debug.LogError("Failed to add metadata to asset: " + response?.errorData?.message);
                            SetupFailed = true;
                        }
                        addMetadataCallCompleted = true;
                    });
                    yield return new WaitUntil(() => addMetadataCallCompleted);
                    if (SetupFailed)
                    {
                        yield break;
                    }
                }
            }

            // Sign in client
            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(GUID.Generate().ToString(), response =>
            {
                SetupFailed |= !response.success;
                guestLoginCompleted = true;
            });
            yield return new WaitUntil(() => guestLoginCompleted);

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

            LootLockerConfig.CreateNewSettings(configCopy.apiKey, configCopy.game_version, configCopy.domainKey,
                configCopy.logLevel, configCopy.logInBuilds, configCopy.logErrorsAsWarnings, configCopy.allowTokenRefresh);
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} tear down #####");
        }

        [UnityTest, Category("LootLocker")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_DefaultParameters_ReturnsAssetsWithNullValuesForIncludables()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssetsWithDefaultParameters((assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            });
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            foreach (var asset in listResponse.assets)
            {
                Assert.IsNotNull(asset.asset_id, "Asset ID should not be null");
                Assert.IsNotNull(asset.asset_ulid, "Asset ULID should not be null");
                Assert.IsNotNull(asset.asset_name, "Asset name should not be null");
                Assert.IsNull(asset.metadata, "Asset metadata should be null");
                Assert.IsEmpty(asset.data_entities, "Asset data entities should be null");
                Assert.IsEmpty(asset.storage, "Asset storage should be null");
                Assert.IsNull(asset.author, "Asset author should be null");
            }
        }

        [UnityTest, Category("LootLocker")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_IncludeStorage_ReturnsAssetsWithStorageButNullOtherwise()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest
            {
                includes = new LootLockerAssetIncludes
                {
                    storage = true
                }
            }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            });
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            foreach (var asset in listResponse.assets)
            {
                Assert.IsNotNull(asset.asset_id, "Asset ID should not be null");
                Assert.IsNotNull(asset.asset_ulid, "Asset ULID should not be null");
                Assert.IsNotNull(asset.asset_name, "Asset name should not be null");
                Assert.IsNull(asset.metadata, "Asset metadata should be null");
                Assert.IsEmpty(asset.data_entities, "Asset data entities should be null");
                Assert.IsNotEmpty(asset.storage, "Asset storage should not be empty");
                Assert.AreEqual(numberOfAssetStorageKeysToAdd, asset.storage.Length, "Asset storage count does not match the number added");
                Assert.IsNull(asset.author, "Asset author should be null");
            }
        }

        [UnityTest, Category("LootLocker")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_IncludeMetadata_ReturnsAssetsWithMetadataButNullOtherwise()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest
            {
                includes = new LootLockerAssetIncludes
                {
                    metadata = true
                }
            }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            });
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            foreach (var asset in listResponse.assets)
            {
                Assert.IsNotNull(asset.asset_id, "Asset ID should not be null");
                Assert.IsNotNull(asset.asset_ulid, "Asset ULID should not be null");
                Assert.IsNotNull(asset.asset_name, "Asset name should not be null");
                Assert.IsNotEmpty(asset.metadata, "Asset metadata should not be empty");
                Assert.AreEqual(numberOfAssetMetadataToAdd, asset.metadata.Length, "Asset metadata count does not match the number added");
                Assert.IsEmpty(asset.data_entities, "Asset data entities should be null");
                Assert.IsEmpty(asset.storage, "Asset storage should be null");
                Assert.IsNull(asset.author, "Asset author should be null");
            }
        }

        [UnityTest, Category("LootLocker")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_IncludeDataEntities_ReturnsAssetsWithDataEntitiesButNullOtherwise()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest
            {
                includes = new LootLockerAssetIncludes
                {
                    data_entities = true
                }
            }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            });
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            foreach (var asset in listResponse.assets)
            {
                Assert.IsNotNull(asset.asset_id, "Asset ID should not be null");
                Assert.IsNotNull(asset.asset_ulid, "Asset ULID should not be null");
                Assert.IsNotNull(asset.asset_name, "Asset name should not be null");
                Assert.IsNull(asset.metadata, "Asset metadata should be null");
                Assert.IsNotEmpty(asset.data_entities, "Asset data entities should not be null");
                Assert.AreEqual(numberOfAssetDataEntitiesToAdd, asset.data_entities.Length, "Asset data entities count does not match the number added");
                Assert.IsEmpty(asset.storage, "Asset storage should be null");
                Assert.IsNull(asset.author, "Asset author should be null");
            }
        }

        [UnityTest, Category("LootLocker")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_IncludeEverything_ReturnsAssetsWithAllIncludables()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest
            {
                includes = new LootLockerAssetIncludes
                {
                    data_entities = true,
                    storage = true,
                    metadata = true,
                    files = true
                }
            }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            });
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            foreach (var asset in listResponse.assets)
            {
                Assert.IsNotNull(asset.asset_id, "Asset ID should not be null");
                Assert.IsNotNull(asset.asset_ulid, "Asset ULID should not be null");
                Assert.IsNotNull(asset.asset_name, "Asset name should not be null");
                Assert.IsNotNull(asset.metadata, "Asset metadata should not be null");
                Assert.AreEqual(numberOfAssetMetadataToAdd, asset.metadata.Length, "Asset metadata count does not match the number added");
                Assert.IsNotEmpty(asset.data_entities, "Asset data entities should not be null");
                Assert.AreEqual(numberOfAssetDataEntitiesToAdd, asset.data_entities.Length, "Asset data entities count does not match the number added");
                Assert.IsNotEmpty(asset.storage, "Asset storage should not be null");
                Assert.AreEqual(numberOfAssetStorageKeysToAdd, asset.storage.Length, "Asset storage count does not match the number added");
                Assert.IsNull(asset.author, "Asset author should be null");
            }
        }

        [UnityTest, Category("LootLocker")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_WithPaginationParameters_ReturnsExpectedAsset()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest { }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            });
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // When
            bool listAssetsWithPaginationCallCompleted = false;
            LootLockerListAssetsResponse paginatedListResponse = null;
            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest { }, (assetsResponse) =>
            {
                paginatedListResponse = assetsResponse;
                listAssetsWithPaginationCallCompleted = true;
            }, 1, 2);
            yield return new WaitUntil(() => listAssetsWithPaginationCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsTrue(paginatedListResponse.success, paginatedListResponse.errorData?.ToString() ?? "Paginated ListAssets call failed");
            Assert.AreEqual(listResponse.pagination.total, paginatedListResponse.pagination.total, "Total assets count does not match");
            Assert.AreEqual(1, paginatedListResponse.assets.Length, "Paginated response should contain only one asset");
            Assert.AreEqual(listResponse.assets[2].asset_ulid, paginatedListResponse.assets[0].asset_ulid, "The expected asset was not returned in the paginated response");
        }

        [UnityTest, Category("LootLocker")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_WithFilterAndAllIncludes_ReturnsExpectedAssetWithAllIncludes()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsWithFilterCallCompleted = false;
            LootLockerListAssetsResponse filteredListResponse = null;
            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest
            {
                includes = new LootLockerAssetIncludes
                {
                    data_entities = true,
                    storage = true,
                    metadata = true,
                    files = true,
                },
                filters = new LootLockerAssetFilters
                {
                    asset_ids = new List<int> { createdAssetIds[2] },
                }
            }, (assetsResponse) =>
            {
                filteredListResponse = assetsResponse;
                listAssetsWithFilterCallCompleted = true;
            });
            yield return new WaitUntil(() => listAssetsWithFilterCallCompleted);

            // Then
            Assert.IsTrue(filteredListResponse.success, filteredListResponse.errorData?.ToString() ?? "Paginated ListAssets call failed");
            Assert.AreEqual(1, filteredListResponse.pagination.total, "Should only be 1 asset with current filter");
            Assert.AreEqual(1, filteredListResponse.assets.Length, "Paginated response should contain only one asset");
            Assert.AreEqual(createdAssetIds[2], filteredListResponse.assets[0].asset_id, "The expected asset was not returned in the paginated response");
            Assert.AreEqual(createdAssetUlids[2], filteredListResponse.assets[0].asset_ulid, "The expected asset was not returned in the paginated response");
            foreach (var asset in filteredListResponse.assets)
            {
                Assert.IsNotNull(asset.metadata, "Asset metadata should not be null");
                Assert.AreEqual(numberOfAssetMetadataToAdd, asset.metadata.Length, "Asset metadata count does not match the number added");
                Assert.IsNotEmpty(asset.data_entities, "Asset data entities should not be null");
                Assert.AreEqual(numberOfAssetDataEntitiesToAdd, asset.data_entities.Length, "Asset data entities count does not match the number added");
                Assert.IsNotEmpty(asset.storage, "Asset storage should not be null");
                Assert.AreEqual(numberOfAssetStorageKeysToAdd, asset.storage.Length, "Asset storage count does not match the number added");
                Assert.IsNull(asset.author, "Asset author should be null");
            }
        }
    }
}
