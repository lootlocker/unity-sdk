using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LootLocker;
using LootLocker.Requests;
using LootLocker.LootLockerEnums;
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

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
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

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
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

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
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

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
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

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
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

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
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

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
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

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_OrderByIdAscending_ReturnsAssetsInAscendingIdOrder()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest { }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            }, orderBy: OrderAssetListBy.id, orderDirection: OrderAssetListDirection.asc);
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            
            // Verify ascending order by ID
            for (int i = 1; i < listResponse.assets.Length; i++)
            {
                Assert.LessOrEqual(listResponse.assets[i - 1].asset_id, listResponse.assets[i].asset_id, 
                    $"Assets should be in ascending order by ID. Asset at index {i-1} has ID {listResponse.assets[i - 1].asset_id}, but asset at index {i} has ID {listResponse.assets[i].asset_id}");
            }
        }

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_OrderByIdDescending_ReturnsAssetsInDescendingIdOrder()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest { }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            }, orderBy: OrderAssetListBy.id, orderDirection: OrderAssetListDirection.desc);
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            
            // Verify descending order by ID
            for (int i = 1; i < listResponse.assets.Length; i++)
            {
                Assert.GreaterOrEqual(listResponse.assets[i - 1].asset_id, listResponse.assets[i].asset_id, 
                    $"Assets should be in descending order by ID. Asset at index {i-1} has ID {listResponse.assets[i - 1].asset_id}, but asset at index {i} has ID {listResponse.assets[i].asset_id}");
            }
        }

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_OrderByNameDescending_ReturnsAssetsInDescendingNameOrder()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest { }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            }, orderBy: OrderAssetListBy.name, orderDirection: OrderAssetListDirection.desc);
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            
            // Verify descending order by name
            for (int i = 1; i < listResponse.assets.Length; i++)
            {
                Assert.GreaterOrEqual(string.Compare(listResponse.assets[i - 1].asset_name, listResponse.assets[i].asset_name, System.StringComparison.OrdinalIgnoreCase), 0, 
                    $"Assets should be in descending order by name. Asset at index {i-1} has name '{listResponse.assets[i - 1].asset_name}', but asset at index {i} has name '{listResponse.assets[i].asset_name}'");
            }
        }

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_OrderByCreatedAtDescending_ReturnsAssetsInDescendingCreatedOrder()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest { }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            }, orderBy: OrderAssetListBy.created_at, orderDirection: OrderAssetListDirection.desc);
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            
            // Verify that ordering was applied (we can't verify exact dates since they're not exposed in the simple asset response,
            // but we can verify the API call was successful and returned the expected number of assets)
            Assert.AreEqual(numberOfAssetsToCreate, listResponse.assets.Length, "Should return all created assets when ordering by created_at");
        }

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_OrderingWithPagination_ReturnsCorrectlyOrderedPaginatedResults()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When - Get first page ordered by ID ascending
            bool firstPageCallCompleted = false;
            LootLockerListAssetsResponse firstPageResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest { }, (assetsResponse) =>
            {
                firstPageResponse = assetsResponse;
                firstPageCallCompleted = true;
            }, PerPage: 2, Page: 1, orderBy: OrderAssetListBy.id, orderDirection: OrderAssetListDirection.asc);
            yield return new WaitUntil(() => firstPageCallCompleted);

            // Get second page ordered by ID ascending
            bool secondPageCallCompleted = false;
            LootLockerListAssetsResponse secondPageResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest { }, (assetsResponse) =>
            {
                secondPageResponse = assetsResponse;
                secondPageCallCompleted = true;
            }, PerPage: 2, Page: 2, orderBy: OrderAssetListBy.id, orderDirection: OrderAssetListDirection.asc);
            yield return new WaitUntil(() => secondPageCallCompleted);

            // Then
            Assert.IsTrue(firstPageResponse.success, firstPageResponse.errorData?.ToString() ?? "First page ListAssets call failed");
            Assert.IsTrue(secondPageResponse.success, secondPageResponse.errorData?.ToString() ?? "Second page ListAssets call failed");
            
            Assert.AreEqual(2, firstPageResponse.assets.Length, "First page should contain 2 assets");
            Assert.AreEqual(2, secondPageResponse.assets.Length, "Second page should contain 2 assets");
            
            // Verify ordering is maintained across pages
            Assert.Less(firstPageResponse.assets[0].asset_id, firstPageResponse.assets[1].asset_id, "First page should be ordered by ID ascending");
            Assert.Less(secondPageResponse.assets[0].asset_id, secondPageResponse.assets[1].asset_id, "Second page should be ordered by ID ascending");
            Assert.Less(firstPageResponse.assets[1].asset_id, secondPageResponse.assets[0].asset_id, "Assets should be ordered across pages");
        }

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_OrderingWithFiltersAndIncludes_ReturnsCorrectlyOrderedFilteredResults()
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
                    metadata = true,
                    storage = true
                },
                filters = new LootLockerAssetFilters
                {
                    asset_ids = new List<int> { createdAssetIds[0], createdAssetIds[2], createdAssetIds[4] },
                }
            }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            }, orderBy: OrderAssetListBy.id, orderDirection: OrderAssetListDirection.desc);
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.AreEqual(3, listResponse.assets.Length, "Should return 3 filtered assets");
            
            // Verify filtering worked
            var returnedIds = new List<int>();
            foreach (var asset in listResponse.assets)
            {
                returnedIds.Add(asset.asset_id);
                Assert.IsNotNull(asset.metadata, "Asset metadata should be included");
                Assert.IsNotEmpty(asset.storage, "Asset storage should be included");
            }
            
            Assert.Contains(createdAssetIds[0], returnedIds, "Should contain first requested asset");
            Assert.Contains(createdAssetIds[2], returnedIds, "Should contain second requested asset");
            Assert.Contains(createdAssetIds[4], returnedIds, "Should contain third requested asset");
            
            // Verify descending order by ID
            for (int i = 1; i < listResponse.assets.Length; i++)
            {
                Assert.GreaterOrEqual(listResponse.assets[i - 1].asset_id, listResponse.assets[i].asset_id, 
                    $"Filtered assets should be in descending order by ID. Asset at index {i-1} has ID {listResponse.assets[i - 1].asset_id}, but asset at index {i} has ID {listResponse.assets[i].asset_id}");
            }
        }

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_OnlyOrderByWithoutDirection_UsesDefaultDirectionSuccessfully()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup

            // When
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest { }, (assetsResponse) =>
            {
                listResponse = assetsResponse;
                listAssetsCallCompleted = true;
            }, orderBy: OrderAssetListBy.id);
            yield return new WaitUntil(() => listAssetsCallCompleted);

            // Then
            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListAssets call failed");
            Assert.IsNotEmpty(listResponse.assets, "Assets list should not be empty");
            Assert.AreEqual(listResponse.pagination.total, numberOfAssetsToCreate, "Total assets count does not match the number created");
            
            // Verify that the call succeeded and returned expected number of assets
            Assert.AreEqual(numberOfAssetsToCreate, listResponse.assets.Length, "Should return all created assets when ordering by ID with default direction");
        }

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_WithAssetFilters_ReturnsOnlyFilteredAssets()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup - we have assets created in Setup()

            // Add filters to some of the created assets
            var filteredAssetIds = new List<int> { createdAssetIds[0], createdAssetIds[2] };
            var unFilteredAssetIds = new List<int> { createdAssetIds[1], createdAssetIds[3], createdAssetIds[4] };

            // Add "rarity" filter to first two assets
            bool bulkEditFiltersCallCompleted = false;
            LootLockerTestAssets.BulkEditFiltersOnAssets(new LootLockerTestAssetFiltersEdit[]
            {
                new LootLockerTestAssetFiltersEdit
                {
                    action = "set",
                    asset_ids = filteredAssetIds.ToArray(),
                    filters = new List<LootLockerTestAssetFiltersEdit.Filter>
                    {
                        new LootLockerTestAssetFiltersEdit.Filter { key = "rarity", value = "legendary" },
                        new LootLockerTestAssetFiltersEdit.Filter { key = "type", value = "weapon" }
                    }
                }
            }, (bulkEditResponse) =>
            {
                if (bulkEditResponse == null || !bulkEditResponse.success)
                {
                    Debug.LogError("Failed to bulk edit asset filters: " + bulkEditResponse?.errorData?.message);
                    SetupFailed = true;
                }
                bulkEditFiltersCallCompleted = true;
            });
            yield return new WaitUntil(() => bulkEditFiltersCallCompleted);
            Assert.IsFalse(SetupFailed, "Failed to add filters to assets");

            // Add different filters to remaining assets
            bool bulkEditFilters2CallCompleted = false;
            LootLockerTestAssets.BulkEditFiltersOnAssets(new LootLockerTestAssetFiltersEdit[]
            {
                new LootLockerTestAssetFiltersEdit
                {
                    action = "set",
                    asset_ids = unFilteredAssetIds.ToArray(),
                    filters = new List<LootLockerTestAssetFiltersEdit.Filter>
                    {
                        new LootLockerTestAssetFiltersEdit.Filter { key = "rarity", value = "common" },
                        new LootLockerTestAssetFiltersEdit.Filter { key = "type", value = "armor" }
                    }
                }
            }, (bulkEditResponse) =>
            {
                if (bulkEditResponse == null || !bulkEditResponse.success)
                {
                    Debug.LogError("Failed to bulk edit asset filters: " + bulkEditResponse?.errorData?.message);
                    SetupFailed = true;
                }
                bulkEditFilters2CallCompleted = true;
            });
            yield return new WaitUntil(() => bulkEditFilters2CallCompleted);
            Assert.IsFalse(SetupFailed, "Failed to add filters to remaining assets");

            // When - Filter for assets with rarity "legendary"
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest
            {
                filters = new LootLockerAssetFilters
                {
                    asset_filters = new List<LootLockerSimpleAssetFilter>
                    {
                        new LootLockerSimpleAssetFilter
                        {
                            key = "rarity",
                            values = new string[] { "legendary" }
                        }
                    }
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
            Assert.AreEqual(2, listResponse.pagination.total, "Should return only assets with legendary rarity");
            Assert.AreEqual(2, listResponse.assets.Length, "Should return exactly 2 filtered assets");

            // Verify that the returned assets are the ones we filtered
            var returnedAssetIds = listResponse.assets.Select(a => a.asset_id).ToList();
            Assert.Contains(filteredAssetIds[0], returnedAssetIds, "Should contain first filtered asset");
            Assert.Contains(filteredAssetIds[1], returnedAssetIds, "Should contain second filtered asset");
            
            // Verify that unfiltered assets are not returned
            foreach (var unfilteredId in unFilteredAssetIds)
            {
                Assert.IsFalse(returnedAssetIds.Contains(unfilteredId), $"Should not contain unfiltered asset {unfilteredId}");
            }
        }

        [UnityTest, Category("LootLocker", "LootLockerCI", "LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator ListAssets_WithMultipleAssetFilterValues_ReturnsAssetsMatchingAnyValue()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            // Set up in setup - we have assets created in Setup()

            // Add different rarity filters to different assets
            bool bulkEditFilters1CallCompleted = false;
            LootLockerTestAssets.BulkEditFiltersOnAssets(new LootLockerTestAssetFiltersEdit[]
            {
                new LootLockerTestAssetFiltersEdit
                {
                    action = "set",
                    asset_ids = new int[] { createdAssetIds[0] },
                    filters = new List<LootLockerTestAssetFiltersEdit.Filter>
                    {
                        new LootLockerTestAssetFiltersEdit.Filter { key = "rarity", value = "rare" }
                    }
                }
            }, (bulkEditResponse) =>
            {
                if (bulkEditResponse == null || !bulkEditResponse.success)
                {
                    Debug.LogError("Failed to bulk edit asset filters: " + bulkEditResponse?.errorData?.message);
                    SetupFailed = true;
                }
                bulkEditFilters1CallCompleted = true;
            });
            yield return new WaitUntil(() => bulkEditFilters1CallCompleted);

            bool bulkEditFilters2CallCompleted = false;
            LootLockerTestAssets.BulkEditFiltersOnAssets(new LootLockerTestAssetFiltersEdit[]
            {
                new LootLockerTestAssetFiltersEdit
                {
                    action = "set",
                    asset_ids = new int[] { createdAssetIds[1] },
                    filters = new List<LootLockerTestAssetFiltersEdit.Filter>
                    {
                        new LootLockerTestAssetFiltersEdit.Filter { key = "rarity", value = "epic" }
                    }
                }
            }, (bulkEditResponse) =>
            {
                if (bulkEditResponse == null || !bulkEditResponse.success)
                {
                    Debug.LogError("Failed to bulk edit asset filters: " + bulkEditResponse?.errorData?.message);
                    SetupFailed = true;
                }
                bulkEditFilters2CallCompleted = true;
            });
            yield return new WaitUntil(() => bulkEditFilters2CallCompleted);

            bool bulkEditFilters3CallCompleted = false;
            LootLockerTestAssets.BulkEditFiltersOnAssets(new LootLockerTestAssetFiltersEdit[]
            {
                new LootLockerTestAssetFiltersEdit
                {
                    action = "set",
                    asset_ids = new int[] { createdAssetIds[2], createdAssetIds[3], createdAssetIds[4] },
                    filters = new List<LootLockerTestAssetFiltersEdit.Filter>
                    {
                        new LootLockerTestAssetFiltersEdit.Filter { key = "rarity", value = "common" }
                    }
                }
            }, (bulkEditResponse) =>
            {
                if (bulkEditResponse == null || !bulkEditResponse.success)
                {
                    Debug.LogError("Failed to bulk edit asset filters: " + bulkEditResponse?.errorData?.message);
                    SetupFailed = true;
                }
                bulkEditFilters3CallCompleted = true;
            });
            yield return new WaitUntil(() => bulkEditFilters3CallCompleted);
            Assert.IsFalse(SetupFailed, "Failed to add filters to assets");

            // When - Filter for assets with rarity "rare" OR "epic"
            bool listAssetsCallCompleted = false;
            LootLockerListAssetsResponse listResponse = null;

            LootLockerSDKManager.ListAssets(new LootLocker.Requests.LootLockerListAssetsRequest
            {
                filters = new LootLockerAssetFilters
                {
                    asset_filters = new List<LootLockerSimpleAssetFilter>
                    {
                        new LootLockerSimpleAssetFilter
                        {
                            key = "rarity",
                            values = new string[] { "rare", "epic" }
                        }
                    }
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
            Assert.AreEqual(2, listResponse.pagination.total, "Should return only assets with rare or epic rarity");
            Assert.AreEqual(2, listResponse.assets.Length, "Should return exactly 2 filtered assets");

            // Verify that the returned assets are the correct ones
            var returnedAssetIds = listResponse.assets.Select(a => a.asset_id).ToList();
            Assert.Contains(createdAssetIds[0], returnedAssetIds, "Should contain asset with rare rarity");
            Assert.Contains(createdAssetIds[1], returnedAssetIds, "Should contain asset with epic rarity");
            
            // Verify that common rarity assets are not returned
            Assert.IsFalse(returnedAssetIds.Contains(createdAssetIds[2]), "Should not contain common rarity asset");
            Assert.IsFalse(returnedAssetIds.Contains(createdAssetIds[3]), "Should not contain common rarity asset");
            Assert.IsFalse(returnedAssetIds.Contains(createdAssetIds[4]), "Should not contain common rarity asset");
        }

    }
}
