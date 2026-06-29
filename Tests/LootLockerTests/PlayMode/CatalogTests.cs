using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class CatalogTests
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;

        private const string CatalogKey = "catalog_test_key";

        // Reusable setup state
        private int _assetContextId;
        private string _currentAssetUlid;
        private int _currentAssetId;
        private string _currentCurrencyId;
        private string _currentCatalogId;
        private string _currentCatalogItemId;

        //===============================================================
        // Helper coroutines — populate instance fields above
        //===============================================================

        private IEnumerator LoadAssetContext()
        {
            bool done = false;
            LootLockerTestAssets.GetAssetContexts((success, errorMsg, contextResponse) =>
            {
                if (!success || contextResponse?.contexts == null || contextResponse.contexts.Length == 0)
                {
                    Debug.LogError($"Failed to get asset contexts: {errorMsg}");
                    SetupFailed = true;
                }
                else
                {
                    _assetContextId = contextResponse.contexts[0].id;
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
        }

        private IEnumerator CreateAndActivateAsset()
        {
            bool done = false;
            LootLockerTestAssets.CreateAsset(_assetContextId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create asset: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    _currentAssetId = response.asset.id;
                    _currentAssetUlid = response.asset.ulid;
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
            if (SetupFailed) { yield break; }

            done = false;
            LootLockerTestAssets.ActivateAsset(_currentAssetId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to activate asset: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
        }

        private IEnumerator CreateAndEnableCurrency(string name, string code)
        {
            bool done = false;
            LootLockerTestCurrency.CreateCurrency(name, code, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create currency: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    _currentCurrencyId = response.id;
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
            if (SetupFailed) { yield break; }

            done = false;
            LootLockerTestCurrency.EnableCurrencyGameWrites(_currentCurrencyId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to enable game writes: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
        }

        private IEnumerator CreateCatalog(string name, string key)
        {
            bool done = false;
            LootLockerTestCatalog.CreateCatalog(name, key, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create catalog: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    _currentCatalogId = response.id;
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
        }

        private IEnumerator PublishCurrentCatalog()
        {
            bool done = false;
            LootLockerTestCatalog.PublishCatalog(_currentCatalogId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to publish catalog: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
        }

        private IEnumerator AddCatalogItemAndToggle(string entityId, string entityKind, string amount = null)
        {
            bool done = false;
            LootLockerTestCatalog.CreateCatalogItem(_currentCatalogId, entityId, entityKind, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create catalog item: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    _currentCatalogItemId = response.catalog_item_id;
                }
                done = true;
            }, amount);
            yield return new WaitUntil(() => done);
            if (SetupFailed) { yield break; }

            done = false;
            LootLockerTestCatalog.ToggleCatalogItemPurchasable(_currentCatalogItemId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to toggle purchasable: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
        }

        private IEnumerator StartGuestSession()
        {
            bool done = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                if (!response.success)
                {
                    Debug.LogError($"Failed to start guest session: {response.errorData?.message}");
                    SetupFailed = true;
                }
                done = true;
            });
            yield return new WaitUntil(() => done);
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestCounter++;
            configCopy = LootLockerConfig.current;
            Debug.Log($"##### Start of {GetType().Name} test no.{TestCounter} setup #####");

            if (!LootLockerConfig.ClearSettings())
            {
                Debug.LogError("Could not clear LootLocker config");
            }

            // Create game
            bool gameCreated = false;
            LootLockerTestGame.CreateGame(testName: GetType().Name + TestCounter + " ", onComplete: (success, errorMsg, game) =>
            {
                if (!success)
                {
                    Debug.LogError($"Failed to create game: {errorMsg}");
                    SetupFailed = true;
                }
                else
                {
                    gameUnderTest = game;
                }
                gameCreated = true;
            });
            yield return new WaitUntil(() => gameCreated);
            if (SetupFailed) { yield break; }

            gameUnderTest.SwitchToStageEnvironment();

            // Enable guest login
            bool platformEnabled = false;
            gameUnderTest.EnableGuestLogin((success, errorMsg) =>
            {
                if (!success)
                {
                    Debug.LogError($"Failed to enable guest login: {errorMsg}");
                    SetupFailed = true;
                }
                platformEnabled = true;
            });
            yield return new WaitUntil(() => platformEnabled);
            if (SetupFailed) { yield break; }

            // Initialize SDK
            Assert.IsTrue(gameUnderTest.InitializeLootLockerSDK(), "Failed to initialize LootLocker SDK");

            Debug.Log($"##### End of {GetType().Name} test no.{TestCounter} setup #####");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"##### End of {GetType().Name} test no.{TestCounter} test case #####");
            if (gameUnderTest != null)
            {
                bool gameDeletionCallCompleted = false;
                gameUnderTest.DeleteGame(((success, errorMessage) =>
                {
                    if (!success) { Debug.LogError(errorMessage); }
                    gameUnderTest = null;
                    gameDeletionCallCompleted = true;
                }));
                yield return new WaitUntil(() => gameDeletionCallCompleted);
            }
            LootLockerStateData.ClearAllSavedStates();
            LootLockerConfig.CreateNewSettings(configCopy);
            Debug.Log($"##### End of {GetType().Name} test no.{TestCounter} tear down #####");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator ListCatalogItemsById_WithAssetItem_ReturnsInlinedAssetDetail()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // ----- Setup: create catalog with an asset item -----
            yield return LoadAssetContext();
            if (SetupFailed) { yield break; }

            yield return CreateAndActivateAsset();
            if (SetupFailed) { yield break; }

            yield return CreateCatalog("Asset Test Catalog", CatalogKey + "_asset");
            if (SetupFailed) { yield break; }

            yield return AddCatalogItemAndToggle(_currentAssetUlid, "asset");
            if (SetupFailed) { yield break; }

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            string idUnderTest = _currentCatalogItemId;

            // ----- Test: List catalog items by ID -----
            LootLockerListCatalogItemsByIdResponse result = null;
            bool apiCallDone = false;
            LootLockerSDKManager.ListCatalogItemsById(new[] { idUnderTest }, false, null, response =>
            {
                result = response;
                apiCallDone = true;
            });
            yield return new WaitUntil(() => apiCallDone);

            Assert.IsTrue(result.success, $"API call failed: {result.errorData?.message}");
            Assert.NotNull(result.items, "items should not be null");
            Assert.AreEqual(1, result.items.Length, "Expected 1 item");
            Assert.AreEqual("asset", result.items[0].entity_kind.ToString(), "Expected asset entity kind");
            Assert.NotNull(result.items[0].asset_detail, "asset_detail should not be null");
            Assert.AreEqual(_currentAssetUlid, result.items[0].asset_detail.id, "Expected matching asset ULID");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator ListCatalogItemsById_WithCurrencyItem_ReturnsInlinedCurrencyDetail()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            yield return CreateAndEnableCurrency("TestGold", "TG");
            if (SetupFailed) { yield break; }

            yield return CreateCatalog("Currency Test Catalog", CatalogKey + "_currency");
            if (SetupFailed) { yield break; }

            yield return AddCatalogItemAndToggle(_currentCurrencyId, "currency", "100");
            if (SetupFailed) { yield break; }

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            string idUnderTest = _currentCatalogItemId;

            // ----- Test: List catalog items by ID -----
            LootLockerListCatalogItemsByIdResponse result = null;
            bool apiCallDone = false;
            LootLockerSDKManager.ListCatalogItemsById(new[] { idUnderTest }, false, null, response =>
            {
                result = response;
                apiCallDone = true;
            });
            yield return new WaitUntil(() => apiCallDone);

            Assert.IsTrue(result.success, $"API call failed: {result.errorData?.message}");
            Assert.NotNull(result.items, "items should not be null");
            Assert.AreEqual(1, result.items.Length, "Expected 1 item");
            Assert.AreEqual("currency", result.items[0].entity_kind.ToString(), "Expected currency entity kind");
            Assert.NotNull(result.items[0].currency_detail, "currency_detail should not be null");
            Assert.AreEqual(_currentCurrencyId, result.items[0].currency_detail.id, "Expected matching currency ID");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator ListCatalogItemsById_WithGroupItem_ReturnsInlinedGroupWithAssociations()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // ----- Setup Phase -----
            yield return LoadAssetContext();
            if (SetupFailed) { yield break; }

            yield return CreateAndActivateAsset();
            if (SetupFailed) { yield break; }

            yield return CreateAndEnableCurrency("GroupGem", "GG");
            if (SetupFailed) { yield break; }

            // Create group reward with asset and currency associations
            bool groupRewardCreated = false;
            string groupRewardId = "";
            LootLockerTestGroup.CreateGroupReward(
                "Starter Pack",
                "A bundle of goodies",
                new[]
                {
                    new LootLockerTestGroupRewardAssociation
                    {
                        entity_kind = "asset",
                        entity_id = _currentAssetUlid
                    },
                    new LootLockerTestGroupRewardAssociation
                    {
                        entity_kind = "currency",
                        entity_id = _currentCurrencyId,
                        metadata = new[]
                        {
                            new LootLockerTestGroupRewardAssociationMetadata
                            {
                                key = "purchased_amount",
                                value = "500"
                            }
                        }
                    }
                },
                response =>
                {
                    if (response == null || !response.success)
                    {
                        Debug.LogError($"Failed to create group reward: {response?.errorData?.message}");
                        SetupFailed = true;
                    }
                    else
                    {
                        groupRewardId = response.id;
                    }
                    groupRewardCreated = true;
                });
            yield return new WaitUntil(() => groupRewardCreated);
            if (SetupFailed) { yield break; }

            yield return CreateCatalog("Group Test Catalog", CatalogKey + "_group");
            if (SetupFailed) { yield break; }

            yield return AddCatalogItemAndToggle(groupRewardId, "group");
            if (SetupFailed) { yield break; }

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            string idUnderTest = _currentCatalogItemId;

            // ----- Test: List catalog items by ID -----
            LootLockerListCatalogItemsByIdResponse result = null;
            bool apiCallDone = false;
            LootLockerSDKManager.ListCatalogItemsById(new[] { idUnderTest }, false, null, response =>
            {
                result = response;
                apiCallDone = true;
            });
            yield return new WaitUntil(() => apiCallDone);

            Assert.IsTrue(result.success, $"API call failed: {result.errorData?.message}");
            Assert.NotNull(result.items, "items should not be null");
            Assert.AreEqual(1, result.items.Length, "Expected 1 item");
            Assert.AreEqual("group", result.items[0].entity_kind.ToString(), "Expected group entity kind");

            // Verify group detail
            Assert.NotNull(result.items[0].group_detail, "group_detail should not be null");
            Assert.AreEqual("Starter Pack", result.items[0].group_detail.name, "Expected matching group name");
            Assert.NotNull(result.items[0].group_detail.associations, "associations should not be null");
            Assert.AreEqual(2, result.items[0].group_detail.associations.Length, "Expected 2 group associations");

            // Verify asset association
            var assetAssociation = result.items[0].group_detail.associations
                .FirstOrDefault(a => a.kind.ToString() == "asset");
            Assert.NotNull(assetAssociation, "Expected an asset association");
            Assert.NotNull(assetAssociation.asset_detail, "asset_detail should not be null on association");
            Assert.AreEqual(_currentAssetUlid, assetAssociation.asset_detail.id, "Expected matching asset ULID");

            // Verify currency association
            var currencyAssociation = result.items[0].group_detail.associations
                .FirstOrDefault(a => a.kind.ToString() == "currency");
            Assert.NotNull(currencyAssociation, "Expected a currency association");
            Assert.NotNull(currencyAssociation.currency_detail, "currency_detail should not be null on association");
            Assert.AreEqual(_currentCurrencyId, currencyAssociation.currency_detail.id, "Expected matching currency ID");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator ListCatalogItemsById_WithUnknownId_ReturnsError()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            // ----- Test: Call with a non-existent catalog listing ID -----
            LootLockerListCatalogItemsByIdResponse result = null;
            bool apiCallDone = false;
            // Use a plausible but non-existent ULID (must use valid Crockford Base32 characters)
            // ULID alphabet: 0123456789ABCDEFGHJKMNPQRSTVWXYZ (no I, L, O, U)
            LootLockerSDKManager.ListCatalogItemsById(new[] { "01ARZ3NDEKTSV4RRFFQ69G5FAV" }, false, null, response =>
            {
                result = response;
                apiCallDone = true;
            });
            yield return new WaitUntil(() => apiCallDone);

            // The request itself should succeed (200), but the item should not be found
            Assert.IsTrue(result.success, $"API call should succeed: {result.errorData?.message}");
            Assert.NotNull(result.errors, "errors should not be null when items are not found");
            Assert.AreEqual(1, result.errors.Length, "Expected 1 error for unknown catalog item");
            Assert.NotNull(result.items, "items should not be null");
            Assert.AreEqual(0, result.items.Length, "Expected 0 items returned for unknown catalog item");
            Assert.AreEqual("01ARZ3NDEKTSV4RRFFQ69G5FAV", result.errors[0].id, "Expected matching error id");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator ListCatalogItemsById_WithMultipleIds_ReturnsAllItems()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            yield return LoadAssetContext();
            if (SetupFailed) { yield break; }

            // Create first asset
            yield return CreateAndActivateAsset();
            if (SetupFailed) { yield break; }
            string asset1Ulid = _currentAssetUlid;

            // Create second asset
            yield return CreateAndActivateAsset();
            if (SetupFailed) { yield break; }
            string asset2Ulid = _currentAssetUlid;

            yield return CreateCatalog("Multi Test Catalog", CatalogKey + "_multi");
            if (SetupFailed) { yield break; }

            // Create first catalog item
            yield return AddCatalogItemAndToggle(asset1Ulid, "asset");
            if (SetupFailed) { yield break; }
            string item1Id = _currentCatalogItemId;

            // Create second catalog item
            yield return AddCatalogItemAndToggle(asset2Ulid, "asset");
            if (SetupFailed) { yield break; }
            string item2Id = _currentCatalogItemId;

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            // ----- Test: List both catalog items by ID -----
            LootLockerListCatalogItemsByIdResponse result = null;
            bool apiCallDone = false;
            LootLockerSDKManager.ListCatalogItemsById(new[] { item1Id, item2Id }, false, null, response =>
            {
                result = response;
                apiCallDone = true;
            });
            yield return new WaitUntil(() => apiCallDone);

            Assert.IsTrue(result.success, $"API call failed: {result.errorData?.message}");
            Assert.NotNull(result.items, "items should not be null");
            Assert.AreEqual(2, result.items.Length, "Expected 2 items");

            var foundItem1 = result.items.FirstOrDefault(i => i.catalog_listing_id == item1Id);
            var foundItem2 = result.items.FirstOrDefault(i => i.catalog_listing_id == item2Id);
            Assert.NotNull(foundItem1, "First item should be in results");
            Assert.NotNull(foundItem2, "Second item should be in results");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator ListCatalogs_ReturnsCatalogsArray()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            // ----- Test: List all catalogs -----
            LootLockerListCatalogsResponse result = null;
            bool apiCallDone = false;
            LootLockerSDKManager.ListCatalogs(response =>
            {
                result = response;
                apiCallDone = true;
            });
            yield return new WaitUntil(() => apiCallDone);

            Assert.IsTrue(result.success, $"API call failed: {result.errorData?.message}");
            Assert.NotNull(result.catalogs, "catalogs should not be null");
            // We haven't created any catalogs yet, so the array should be empty
            Assert.AreEqual(0, result.catalogs.Length, "Expected no catalogs for a fresh game");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator ListCatalogItems_WithAssetAndCurrency_ReturnsEntriesAndLookupMaps()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // ----- Setup: create catalog with asset and currency items -----
            yield return LoadAssetContext();
            if (SetupFailed) { yield break; }

            yield return CreateAndActivateAsset();
            if (SetupFailed) { yield break; }
            string assetUlid = _currentAssetUlid;

            yield return CreateAndEnableCurrency("TestSilver", "TS");
            if (SetupFailed) { yield break; }
            string currencyId = _currentCurrencyId;

            yield return CreateCatalog("List Items Catalog", CatalogKey + "_list_items");
            if (SetupFailed) { yield break; }

            // Create asset catalog item + toggle
            yield return AddCatalogItemAndToggle(assetUlid, "asset");
            if (SetupFailed) { yield break; }

            // Create currency catalog item + toggle
            yield return AddCatalogItemAndToggle(currencyId, "currency", "250");
            if (SetupFailed) { yield break; }

            yield return PublishCurrentCatalog();
            if (SetupFailed) { yield break; }

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            // ----- Test: List catalog items by key -----
            LootLockerListCatalogPricesV2Response result = null;
            bool apiCallDone = false;
            LootLockerSDKManager.ListCatalogItems(CatalogKey + "_list_items", 10, 1, response =>
            {
                result = response;
                apiCallDone = true;
            });
            yield return new WaitUntil(() => apiCallDone);

            Assert.IsTrue(result.success, $"API call failed: {result.errorData?.message}");
            Assert.NotNull(result.catalog, "catalog should not be null");
            Assert.AreEqual(CatalogKey + "_list_items", result.catalog.key, "Expected matching catalog key");
            Assert.NotNull(result.entries, "entries should not be null");
            Assert.AreEqual(2, result.entries.Length, "Expected 2 entries");

            // Find asset and currency entries
            var assetEntry = result.entries.FirstOrDefault(e => e.entity_kind.ToString() == "asset");
            var currencyEntry = result.entries.FirstOrDefault(e => e.entity_kind.ToString() == "currency");
            Assert.NotNull(assetEntry, "Expected an asset entry");
            Assert.NotNull(currencyEntry, "Expected a currency entry");

            // Verify asset entry resolves in asset_details lookup map
            Assert.NotNull(result.asset_details, "asset_details should not be null");
            var assetKey = new LootLockerItemDetailsKey
            {
                catalog_listing_id = assetEntry.catalog_listing_id,
                item_id = assetEntry.entity_id
            };
            Assert.IsTrue(result.asset_details.ContainsKey(assetKey), "Asset detail should be in lookup map");
            Assert.AreEqual(assetUlid, result.asset_details[assetKey].id, "Expected matching asset ULID");

            // Verify currency entry resolves in currency_details lookup map
            Assert.NotNull(result.currency_details, "currency_details should not be null");
            var currencyKey = new LootLockerItemDetailsKey
            {
                catalog_listing_id = currencyEntry.catalog_listing_id,
                item_id = currencyEntry.entity_id
            };
            Assert.IsTrue(result.currency_details.ContainsKey(currencyKey), "Currency detail should be in lookup map");
            Assert.AreEqual(currencyId, result.currency_details[currencyKey].id, "Expected matching currency ID");

            // Verify pagination
            Assert.NotNull(result.pagination, "pagination should not be null");
            Assert.AreEqual(2, result.pagination.total, "Expected total of 2 items");
            Assert.AreEqual(1, result.pagination.current_page, "Expected current page 1");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator ListCatalogItems_WithGroup_ReturnsGroupInLookupMap()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // ----- Setup: create group reward and add it to a catalog -----
            yield return LoadAssetContext();
            if (SetupFailed) { yield break; }

            yield return CreateAndActivateAsset();
            if (SetupFailed) { yield break; }

            // Create group reward with asset association
            bool groupRewardCreated = false;
            string groupRewardId = "";
            LootLockerTestGroup.CreateGroupReward(
                "Bundle Pack",
                "Group reward bundle",
                new[]
                {
                    new LootLockerTestGroupRewardAssociation
                    {
                        entity_kind = "asset",
                        entity_id = _currentAssetUlid
                    }
                },
                response =>
                {
                    if (response == null || !response.success)
                    {
                        Debug.LogError($"Failed to create group reward: {response?.errorData?.message}");
                        SetupFailed = true;
                    }
                    else
                    {
                        groupRewardId = response.id;
                    }
                    groupRewardCreated = true;
                });
            yield return new WaitUntil(() => groupRewardCreated);
            if (SetupFailed) { yield break; }

            yield return CreateCatalog("Group List Catalog", CatalogKey + "_group_list");
            if (SetupFailed) { yield break; }

            yield return AddCatalogItemAndToggle(groupRewardId, "group");
            if (SetupFailed) { yield break; }

            yield return PublishCurrentCatalog();
            if (SetupFailed) { yield break; }

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            // ----- Test: List catalog items by key -----
            LootLockerListCatalogPricesV2Response result = null;
            bool apiCallDone = false;
            LootLockerSDKManager.ListCatalogItems(CatalogKey + "_group_list", 10, 1, response =>
            {
                result = response;
                apiCallDone = true;
            });
            yield return new WaitUntil(() => apiCallDone);

            Assert.IsTrue(result.success, $"API call failed: {result.errorData?.message}");
            Assert.NotNull(result.entries, "entries should not be null");
            Assert.AreEqual(1, result.entries.Length, "Expected 1 entry");

            var groupEntry = result.entries[0];
            Assert.AreEqual("group", groupEntry.entity_kind.ToString(), "Expected group entity kind");

            // Verify group entry resolves in group_details lookup map
            Assert.NotNull(result.group_details, "group_details should not be null");
            var groupKey = new LootLockerItemDetailsKey
            {
                catalog_listing_id = groupEntry.catalog_listing_id,
                item_id = groupEntry.entity_id
            };
            Assert.IsTrue(result.group_details.ContainsKey(groupKey), "Group detail should be in lookup map");
            Assert.AreEqual("Bundle Pack", result.group_details[groupKey].name, "Expected matching group name");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator ListCatalogItems_InvalidKey_ReturnsError()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            // ----- Test: Call with non-existent catalog key -----
            // Ensure logging is enabled so the expected error log is emitted
            var prevLogLevel = LootLockerConfig.current.logLevel;
            try
            {
                LootLockerConfig.current.logLevel = LootLockerLogger.LogLevel.Info;
                LogAssert.Expect(LogType.Error, new Regex("nonexistent_catalog_key_12345.*catalog not found"));
                LootLockerListCatalogPricesV2Response result = null;
                bool apiCallDone = false;
                LootLockerSDKManager.ListCatalogItems("nonexistent_catalog_key_12345", 10, 1, response =>
                {
                    result = response;
                    apiCallDone = true;
                });
                yield return new WaitUntil(() => apiCallDone);

                Assert.IsFalse(result.success, "Expected API call to fail with invalid key");
                Assert.NotNull(result.errorData, "errorData should not be null");
            }
            finally
            {
                LootLockerConfig.current.logLevel = prevLogLevel;
            }
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator ListCatalogItems_Pagination_ReturnsCorrectPage()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // ----- Setup: create 3 test assets and add them to a catalog -----
            yield return LoadAssetContext();
            if (SetupFailed) { yield break; }

            string[] assetUlids = new string[3];
            for (int i = 0; i < 3; i++)
            {
                yield return CreateAndActivateAsset();
                if (SetupFailed) { yield break; }
                assetUlids[i] = _currentAssetUlid;
            }

            yield return CreateCatalog("Pagination Test Catalog", CatalogKey + "_pagination");
            if (SetupFailed) { yield break; }

            string[] catalogItemIds = new string[3];
            for (int i = 0; i < 3; i++)
            {
                yield return AddCatalogItemAndToggle(assetUlids[i], "asset");
                if (SetupFailed) { yield break; }
                catalogItemIds[i] = _currentCatalogItemId;
            }

            yield return PublishCurrentCatalog();
            if (SetupFailed) { yield break; }

            yield return StartGuestSession();
            if (SetupFailed) { yield break; }

            // ----- Test: Page 1 with per_page=2 should return 2 items -----
            LootLockerListCatalogPricesV2Response page1Result = null;
            bool page1Done = false;
            LootLockerSDKManager.ListCatalogItems(CatalogKey + "_pagination", 2, 1, response =>
            {
                page1Result = response;
                page1Done = true;
            });
            yield return new WaitUntil(() => page1Done);

            Assert.IsTrue(page1Result.success, $"Page 1 call failed: {page1Result.errorData?.message}");
            Assert.NotNull(page1Result.entries, "entries should not be null");
            Assert.AreEqual(2, page1Result.entries.Length, "Expected 2 entries on page 1");
            Assert.NotNull(page1Result.pagination, "pagination should not be null");
            Assert.AreEqual(2, page1Result.pagination.per_page, "Expected per_page=2");
            Assert.AreEqual(1, page1Result.pagination.current_page, "Expected current page 1");
            Assert.AreEqual(3, page1Result.pagination.total, "Expected total of 3 items");
            Assert.Greater(page1Result.pagination.last_page, 1, "Expected more than 1 page");

            // ----- Test: Page 2 with per_page=2 should return 1 item -----
            LootLockerListCatalogPricesV2Response page2Result = null;
            bool page2Done = false;
            LootLockerSDKManager.ListCatalogItems(CatalogKey + "_pagination", 2, 2, response =>
            {
                page2Result = response;
                page2Done = true;
            });
            yield return new WaitUntil(() => page2Done);

            Assert.IsTrue(page2Result.success, $"Page 2 call failed: {page2Result.errorData?.message}");
            Assert.NotNull(page2Result.entries, "entries should not be null");
            Assert.AreEqual(1, page2Result.entries.Length, "Expected 1 entry on page 2");
            Assert.AreEqual(2, page2Result.pagination.current_page, "Expected current page 2");

            // Verify no overlap between pages
            var page1Ids = new System.Collections.Generic.HashSet<string>(page1Result.entries.Select(e => e.catalog_listing_id));
            var page2Ids = new System.Collections.Generic.HashSet<string>(page2Result.entries.Select(e => e.catalog_listing_id));
            Assert.AreEqual(0, page1Ids.Intersect(page2Ids).Count(), "Pages should not have overlapping items");
        }
    }
}
