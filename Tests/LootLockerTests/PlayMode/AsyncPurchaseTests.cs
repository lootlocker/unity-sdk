using System;
using System.Collections;
using System.Linq;
using LootLocker;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class AsyncPurchaseTests
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;

        private string paymentCurrencyId = "";
        private string rewardCurrencyId = "";
        private int assetId = 0;
        private string walletId = "";
        private string assetCatalogListingId = "";
        private string currencyCatalogListingId = "";

        private const int DenominationValue = 100;
        private const int CurrencyPriceForAsset = 100;
        private const int CurrencyPriceForCurrencyReward = 50;
        private const int InitialBalance = 1000;
        private const string CatalogKey = "async_test_catalog";

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

            // Enable player-requested refunds on the game
            bool refundsEnabled = false;
            LootLockerTestGameAdmin.SetGameConfig("global_refunds", "{\"allow_player_requested_refunds\": true}", response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to enable player refunds: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                refundsEnabled = true;
            });
            yield return new WaitUntil(() => refundsEnabled);
            if (SetupFailed) { yield break; }

            // Create payment currency (TestGold)
            bool paymentCurrencyCreated = false;
            LootLockerTestCurrency.CreateCurrency("TestGold", "TG", response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create payment currency: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    paymentCurrencyId = response.id;
                }
                paymentCurrencyCreated = true;
            });
            yield return new WaitUntil(() => paymentCurrencyCreated);
            if (SetupFailed) { yield break; }

            // Create reward currency (TestGems)
            bool rewardCurrencyCreated = false;
            LootLockerTestCurrency.CreateCurrency("TestGems", "TM", response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create reward currency: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    rewardCurrencyId = response.id;
                }
                rewardCurrencyCreated = true;
            });
            yield return new WaitUntil(() => rewardCurrencyCreated);
            if (SetupFailed) { yield break; }

            // Enable game API writes on the reward currency (required for catalog item creation)
            bool gameWritesEnabled = false;
            LootLockerTestCurrency.EnableCurrencyGameWrites(rewardCurrencyId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to enable game writes on reward currency: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                gameWritesEnabled = true;
            });
            yield return new WaitUntil(() => gameWritesEnabled);
            if (SetupFailed) { yield break; }

            // Get asset context to create an asset
            bool contextsLoaded = false;
            int assetContextId = 0;
            LootLockerTestAssets.GetAssetContexts((success, errorMsg, contextResponse) =>
            {
                if (!success || contextResponse?.contexts == null || contextResponse.contexts.Length == 0)
                {
                    Debug.LogError($"Failed to get asset contexts: {errorMsg}");
                    SetupFailed = true;
                }
                else
                {
                    assetContextId = contextResponse.contexts[0].id;
                }
                contextsLoaded = true;
            });
            yield return new WaitUntil(() => contextsLoaded);
            if (SetupFailed) { yield break; }

            // Create asset for use as a catalog reward
            bool assetCreated = false;
            string assetUlid = "";
            LootLockerTestAssets.CreateAsset(assetContextId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create asset: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    assetId = response.asset.id;
                    assetUlid = response.asset.ulid;
                }
                assetCreated = true;
            });
            yield return new WaitUntil(() => assetCreated);
            if (SetupFailed) { yield break; }

            // Activate asset so it can be purchased
            bool assetActivated = false;
            LootLockerTestAssets.ActivateAsset(assetId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to activate asset: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                assetActivated = true;
            });
            yield return new WaitUntil(() => assetActivated);
            if (SetupFailed) { yield break; }

            // Create catalog
            bool catalogCreated = false;
            string catalogId = "";
            LootLockerTestCatalog.CreateCatalog("Async Test Catalog", CatalogKey, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create catalog: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    catalogId = response.id;
                }
                catalogCreated = true;
            });
            yield return new WaitUntil(() => catalogCreated);
            if (SetupFailed) { yield break; }

            // Create asset catalog item (entity_kind="asset", entity_id=assetUlid)
            bool assetItemCreated = false;
            string assetCatalogItemId = "";
            LootLockerTestCatalog.CreateCatalogItem(catalogId, assetUlid, "asset", response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create asset catalog item: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    assetCatalogItemId = response.catalog_item_id;
                }
                assetItemCreated = true;
            });
            yield return new WaitUntil(() => assetItemCreated);
            if (SetupFailed) { yield break; }

            // Enable purchasing on the asset catalog item
            bool assetPurchasableToggled = false;
            LootLockerTestCatalog.ToggleCatalogItemPurchasable(assetCatalogItemId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to toggle purchasable on asset catalog item: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                assetPurchasableToggled = true;
            });
            yield return new WaitUntil(() => assetPurchasableToggled);
            if (SetupFailed) { yield break; }

            // Create currency catalog item (entity_kind="currency", entity_id=rewardCurrencyId, amount=DenominationValue)
            bool currencyItemCreated = false;
            string currencyCatalogItemId = "";
            LootLockerTestCatalog.CreateCatalogItem(catalogId, rewardCurrencyId, "currency", response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to create currency catalog item: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    currencyCatalogItemId = response.catalog_item_id;
                }
                currencyItemCreated = true;
            }, DenominationValue.ToString());
            yield return new WaitUntil(() => currencyItemCreated);
            if (SetupFailed) { yield break; }

            // Enable purchasing on the currency catalog item
            bool currencyPurchasableToggled = false;
            LootLockerTestCatalog.ToggleCatalogItemPurchasable(currencyCatalogItemId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to toggle purchasable on currency catalog item: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                currencyPurchasableToggled = true;
            });
            yield return new WaitUntil(() => currencyPurchasableToggled);
            if (SetupFailed) { yield break; }

            // Add price to asset catalog item (priced in TestGold)
            bool assetPriceAdded = false;
            LootLockerTestCatalog.AddPriceToCatalogItem(assetCatalogItemId, paymentCurrencyId, CurrencyPriceForAsset.ToString(), response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to add price to asset catalog item: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                assetPriceAdded = true;
            });
            yield return new WaitUntil(() => assetPriceAdded);
            if (SetupFailed) { yield break; }

            // Add price to currency catalog item (priced in TestGold)
            bool currencyPriceAdded = false;
            LootLockerTestCatalog.AddPriceToCatalogItem(currencyCatalogItemId, paymentCurrencyId, CurrencyPriceForCurrencyReward.ToString(), response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to add price to currency catalog item: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                currencyPriceAdded = true;
            });
            yield return new WaitUntil(() => currencyPriceAdded);
            if (SetupFailed) { yield break; }

            // Publish catalog so it is available to players
            bool catalogPublished = false;
            LootLockerTestCatalog.PublishCatalog(catalogId, response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to publish catalog: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                catalogPublished = true;
            });
            yield return new WaitUntil(() => catalogPublished);
            if (SetupFailed) { yield break; }

            // Start guest session to get wallet ID
            bool guestLoginDone = false;
            LootLockerSDKManager.StartGuestSession(GUID.Generate().ToString(), response =>
            {
                if (!response.success)
                {
                    Debug.LogError($"Failed to start guest session: {response.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    walletId = response.wallet_id;
                }
                guestLoginDone = true;
            });
            yield return new WaitUntil(() => guestLoginDone);
            if (SetupFailed) { yield break; }

            // Credit player with initial TestGold balance via admin API
            bool creditDone = false;
            LootLockerTestGameAdmin.AdminCreditBalance(walletId, paymentCurrencyId, InitialBalance.ToString(), response =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"Failed to credit player balance: {response?.errorData?.message}");
                    SetupFailed = true;
                }
                creditDone = true;
            });
            yield return new WaitUntil(() => creditDone);
            if (SetupFailed) { yield break; }

            // List catalog items to obtain catalog_listing_ids needed for purchasing
            bool catalogItemsListed = false;
            LootLockerSDKManager.ListCatalogItems(CatalogKey, 100, 0, response =>
            {
                if (!response.success || response.entries == null)
                {
                    Debug.LogError($"Failed to list catalog items: {response.errorData?.message}");
                    SetupFailed = true;
                }
                else
                {
                    foreach (var entry in response.entries)
                    {
                        if (entry.entity_id == assetUlid)
                            assetCatalogListingId = entry.catalog_listing_id;
                        else if (entry.entity_id == rewardCurrencyId)
                            currencyCatalogListingId = entry.catalog_listing_id;
                    }

                    if (string.IsNullOrEmpty(assetCatalogListingId) || string.IsNullOrEmpty(currencyCatalogListingId))
                    {
                        Debug.LogError("Could not find one or more catalog listing IDs in catalog items response");
                        SetupFailed = true;
                    }
                }
                catalogItemsListed = true;
            });
            yield return new WaitUntil(() => catalogItemsListed);
            if (SetupFailed) { yield break; }

            Debug.Log($"##### Start of {GetType().Name} test no.{TestCounter} test case #####");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"##### End of {GetType().Name} test no.{TestCounter} test case #####");

            if (gameUnderTest != null)
            {
                bool gameDeletionDone = false;
                gameUnderTest.DeleteGame((success, errorMsg) =>
                {
                    if (!success)
                    {
                        Debug.LogError($"Failed to delete game: {errorMsg}");
                    }
                    gameUnderTest = null;
                    gameDeletionDone = true;
                });
                yield return new WaitUntil(() => gameDeletionDone);
            }

            LootLockerStateData.ClearAllSavedStates();
            LootLockerConfig.CreateNewSettings(configCopy);
            Debug.Log($"##### End of {GetType().Name} test no.{TestCounter} tear down #####");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator AsyncPurchase_InitiateMultipleItemsAndManuallyGetStatus_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Setup failed, see logs for details");

            // Initiate a single purchase containing both catalog items (asset + currency reward)
            LootLockerAsyncPurchaseInitiatedResponse initiatedResponse = null;
            LootLockerSDKManager.InitiateAsyncPurchaseCatalogItems(
                walletId,
                new[]
                {
                    new LootLockerCatalogItemAndQuantityPair { catalog_listing_id = assetCatalogListingId, quantity = 1 },
                    new LootLockerCatalogItemAndQuantityPair { catalog_listing_id = currencyCatalogListingId, quantity = 1 }
                },
                r => initiatedResponse = r);
            yield return new WaitUntil(() => initiatedResponse != null);

            Assert.IsTrue(initiatedResponse.success,
                $"InitiateAsyncPurchaseCatalogItems failed: {initiatedResponse.errorData?.message}");
            Assert.IsFalse(string.IsNullOrEmpty(initiatedResponse.entitlement_id),
                "Expected a non-empty entitlement_id from async purchase initiation");

            // Manually poll GetAsyncPurchaseStatus until the purchase reaches a terminal state
            LootLockerAsyncPurchaseStatusResponse statusResponse = null;
            for (int i = 0; i < 30; i++)
            {
                statusResponse = null;
                LootLockerSDKManager.GetAsyncPurchaseStatus(initiatedResponse.entitlement_id, r => statusResponse = r);
                yield return new WaitUntil(() => statusResponse != null);

                if (statusResponse.status != LootLockerAsyncPurchaseStatus.pending)
                    break;

                yield return new WaitForSeconds(2f);
            }

            Assert.IsTrue(statusResponse.success,
                $"GetAsyncPurchaseStatus failed: {statusResponse.errorData?.message}");
            Assert.AreEqual(LootLockerAsyncPurchaseStatus.active, statusResponse.status,
                $"Expected purchase status 'active', got '{statusResponse.status}': {statusResponse.error}");

            // Verify TestGold was debited for both items
            LootLockerListBalancesForWalletResponse balancesResponse = null;
            LootLockerSDKManager.ListBalancesInWallet(walletId, r => balancesResponse = r);
            yield return new WaitUntil(() => balancesResponse != null);

            Assert.IsTrue(balancesResponse.success, $"ListBalancesInWallet failed: {balancesResponse.errorData?.message}");
            var goldBalance = balancesResponse.balances?.FirstOrDefault(b => b.currency?.id == paymentCurrencyId);
            Assert.IsNotNull(goldBalance, "TestGold balance entry not found after purchase");
            Assert.AreEqual(
                InitialBalance - CurrencyPriceForAsset - CurrencyPriceForCurrencyReward,
                int.Parse(goldBalance.amount),
                "TestGold balance was not correctly debited after purchasing both catalog items");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator AsyncPurchase_PollSingleCurrencyItemAndVerifyBalance_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Setup failed, see logs for details");

            // Buy the currency reward item and wait for automatic polling to complete
            LootLockerAsyncPurchaseStatusResponse completedResponse = null;
            LootLockerSDKManager.InitiateAndPollAsyncPurchaseSingleCatalogItem(
                walletId,
                currencyCatalogListingId,
                1,
                onStatusUpdate: null,
                onComplete: r => completedResponse = r);
            yield return new WaitUntil(() => completedResponse != null);

            Assert.IsTrue(completedResponse.success,
                $"InitiateAndPollAsyncPurchaseSingleCatalogItem failed: {completedResponse.errorData?.message}");
            Assert.AreEqual(LootLockerAsyncPurchaseStatus.active, completedResponse.status,
                $"Expected 'active', got '{completedResponse.status}': {completedResponse.error}");

            // Verify both the debit and the currency reward in a single balances request
            LootLockerListBalancesForWalletResponse balancesResponse = null;
            LootLockerSDKManager.ListBalancesInWallet(walletId, r => balancesResponse = r);
            yield return new WaitUntil(() => balancesResponse != null);

            Assert.IsTrue(balancesResponse.success, $"ListBalancesInWallet failed: {balancesResponse.errorData?.message}");

            var goldBalance = balancesResponse.balances?.FirstOrDefault(b => b.currency?.id == paymentCurrencyId);
            Assert.IsNotNull(goldBalance, "TestGold balance entry not found after purchase");
            Assert.AreEqual(
                InitialBalance - CurrencyPriceForCurrencyReward,
                int.Parse(goldBalance.amount),
                "TestGold balance was not correctly debited after currency reward purchase");

            var gemsBalance = balancesResponse.balances?.FirstOrDefault(b => b.currency?.id == rewardCurrencyId);
            Assert.IsNotNull(gemsBalance, "TestGems balance entry not found after currency reward purchase");
            Assert.AreEqual(
                DenominationValue,
                int.Parse(gemsBalance.amount),
                "TestGems balance does not match the denomination value after purchase");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator AsyncPurchase_CancelPolling_StopsPolling()
        {
            Assert.IsFalse(SetupFailed, "Setup failed, see logs for details");

            LootLockerAsyncPurchaseStatusResponse cancelledResponse = null;
            Guid pollGuid = LootLockerSDKManager.InitiateAndPollAsyncPurchaseCatalogItems(
                walletId,
                new[] { new LootLockerCatalogItemAndQuantityPair { catalog_listing_id = assetCatalogListingId, quantity = 1 } },
                onStatusUpdate: null,
                onComplete: r => cancelledResponse = r);

            Assert.AreNotEqual(Guid.Empty, pollGuid, "Expected a valid (non-empty) Guid from the polling initiator");

            LootLockerSDKManager.CancelAsyncPurchasePolling(pollGuid);

            yield return new WaitUntil(() => cancelledResponse != null);

            Assert.IsFalse(cancelledResponse.success,
                "Expected the cancelled polling response to have success=false");
            StringAssert.Contains("cancelled", cancelledResponse.errorData?.message ?? "",
                "Expected the error message to contain 'cancelled'");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator AsyncPurchase_RefundAssetPurchaseByEntitlementId_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Setup failed, see logs for details");

            // Buy the asset item and wait for it to reach an active state
            LootLockerAsyncPurchaseStatusResponse purchaseCompleted = null;
            LootLockerSDKManager.InitiateAndPollAsyncPurchaseSingleCatalogItem(
                walletId,
                assetCatalogListingId,
                1,
                onStatusUpdate: null,
                onComplete: r => purchaseCompleted = r);
            yield return new WaitUntil(() => purchaseCompleted != null);

            Assert.IsTrue(purchaseCompleted.success,
                $"Purchase polling failed: {purchaseCompleted.errorData?.message}");
            Assert.AreEqual(LootLockerAsyncPurchaseStatus.active, purchaseCompleted.status,
                $"Expected purchase to be 'active', got '{purchaseCompleted.status}': {purchaseCompleted.error}");
            Assert.IsFalse(string.IsNullOrEmpty(purchaseCompleted.entitlement_id),
                "Expected a non-empty entitlement_id in the completed purchase response");

            // Verify asset is in inventory before refunding
            LootLockerInventoryResponse inventoryBefore = null;
            LootLockerSDKManager.GetInventory(r => inventoryBefore = r);
            yield return new WaitUntil(() => inventoryBefore != null);

            Assert.IsTrue(inventoryBefore.success, $"GetInventory failed: {inventoryBefore.errorData?.message}");
            Assert.IsTrue(
                inventoryBefore.inventory?.Any(item => item.asset?.id == assetId) == true,
                "Expected purchased asset to appear in player inventory before refund");

            // Refund the purchase
            LootLockerRefundByEntitlementIdsResponse refundResponse = null;
            LootLockerSDKManager.RefundByEntitlementIds(new[] { purchaseCompleted.entitlement_id }, r => refundResponse = r);
            yield return new WaitUntil(() => refundResponse != null);

            Assert.IsTrue(refundResponse.success, $"RefundByEntitlementIds failed: {refundResponse.errorData?.message}");

            Assert.IsNotNull(refundResponse.currency_refunded,
                "Expected currency_refunded to be present in the refund response");
            Assert.IsTrue(refundResponse.currency_refunded.Length > 0,
                "Expected at least one currency_refunded entry");
            var goldRefund = refundResponse.currency_refunded.FirstOrDefault(c => c.currency_id == paymentCurrencyId);
            Assert.IsNotNull(goldRefund,
                "Expected a TestGold entry in currency_refunded");
            Assert.AreEqual(CurrencyPriceForAsset.ToString(), goldRefund.amount,
                "Refunded TestGold amount does not match the original purchase price");

            Assert.IsNotNull(refundResponse.player_inventory_events,
                "Expected player_inventory_events to be present in the refund response");
            Assert.IsTrue(refundResponse.player_inventory_events.Length > 0,
                "Expected at least one player_inventory_event entry for the refunded asset");
            var assetEvent = refundResponse.player_inventory_events.FirstOrDefault(e => e.asset_id > 0);
            Assert.IsNotNull(assetEvent, "Expected an inventory event for the asset in the refund response");
            Assert.AreEqual(LootLockerRefundInventoryEventAction.removed, assetEvent.action,
                "Expected the asset to be removed from inventory as part of the refund");
        }
    }
}
