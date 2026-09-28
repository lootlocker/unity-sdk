using System;
using System.Collections;
using System.Linq;
using LootLocker;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class ItemTests
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;
        private int sessionPlayerId = 0;
        private string createdTemplateId = string.Empty;
        private string createdTemplateName = string.Empty;
        private string grantedInventoryId = string.Empty;

        private const string PublicAudience = "public";

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

            // Create a stackable, consumable, deletable template with a public audience. The
            // audience is required: the game API only lists templates that have an audience row,
            // so without it the template would be invisible even to the player it was granted to.
            createdTemplateName = LootLockerTestItems.GetRandomItemTemplateName();
            bool createTemplateCallCompleted = false;
            LootLockerTestItems.CreateItemTemplate(createdTemplateName, "stackable", true, true, new[] { PublicAudience }, (templateResponse) =>
            {
                if (templateResponse == null || !templateResponse.success)
                {
                    Debug.LogError("Failed to create item template: " + templateResponse?.errorData?.message);
                    SetupFailed = true;
                    createTemplateCallCompleted = true;
                    return;
                }

                createdTemplateId = templateResponse.id;
                createTemplateCallCompleted = true;
            });
            yield return new WaitUntil(() => createTemplateCallCompleted);
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
            LootLockerTestItems.GrantItemToPlayer(sessionPlayerId, createdTemplateId, 5, (grantResponse) =>
            {
                if (grantResponse == null || !grantResponse.success)
                {
                    Debug.LogError("Failed to grant item to player: " + grantResponse?.errorData?.message);
                    SetupFailed = true;
                    grantCallCompleted = true;
                    return;
                }

                grantedInventoryId = grantResponse.id;
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

        /// <summary>
        /// The designated fast test: it covers the whole setup chain (create template with an
        /// audience, grant to player) plus the central read path, so it gives early signal
        /// without paying for the full suite.
        /// </summary>
        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        [Timeout(360_000)]
        public IEnumerator Items_ListItemTemplates_ReturnsCreatedTemplate()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            bool listCallCompleted = false;
            LootLockerListItemTemplatesResponse response = null;
            LootLockerSDKManager.ListItemTemplates(1, 100, (r) =>
            {
                response = r;
                listCallCompleted = true;
            });
            yield return new WaitUntil(() => listCallCompleted);

            // Then
            Assert.IsTrue(response.success, response.errorData?.ToString() ?? "ListItemTemplates call failed");
            Assert.IsNotNull(response.items, "Item templates should not be null");
            Assert.IsTrue(response.items.Any(t => t.id == createdTemplateId), "Expected the created template to be visible to the player");

            var template = response.items.First(t => t.id == createdTemplateId);
            Assert.AreEqual(createdTemplateName, template.name, "Template name should match the created template");
            Assert.AreEqual(LootLockerItemType.stackable, template.item_type, "Template should be stackable");
            Assert.IsTrue(template.consumable, "Template should be consumable");
            Assert.IsTrue(template.deletable, "Template should be deletable");
            Assert.AreNotEqual(default(DateTime), template.created_at, "Template created_at should be populated");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        [Timeout(360_000)]
        public IEnumerator Items_ListPlayerItems_ReturnsGrantedItem()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            bool listCallCompleted = false;
            LootLockerListPlayerItemsResponse response = null;
            LootLockerSDKManager.ListPlayerItems(1, 100, onComplete: (r) =>
            {
                response = r;
                listCallCompleted = true;
            });
            yield return new WaitUntil(() => listCallCompleted);

            // Then
            Assert.IsTrue(response.success, response.errorData?.ToString() ?? "ListPlayerItems call failed");
            Assert.IsNotNull(response.items, "Player items should not be null");
            Assert.IsTrue(response.items.Any(i => i.id == grantedInventoryId), "Expected the granted item to be listed");

            var item = response.items.First(i => i.id == grantedInventoryId);
            Assert.AreEqual(5, item.count, "Granted count should be 5");
            Assert.AreEqual(createdTemplateName, item.name, "Item name should be the template name");
            Assert.AreEqual(LootLockerItemType.stackable, item.item_type, "Item should be stackable");
            Assert.IsTrue(item.deletable, "Item should be deletable");
            Assert.IsNotNull(item.source, "Item source should be populated");
            Assert.AreNotEqual(default(DateTime), item.created_at, "Item created_at should be populated");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        [Timeout(360_000)]
        public IEnumerator Items_ListPlayerItems_WithFilters_ReturnsFilteredItems()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            bool listCallCompleted = false;
            LootLockerListPlayerItemsResponse response = null;
            LootLockerSDKManager.ListPlayerItems(1, 100, name: createdTemplateName, itemType: LootLockerItemType.stackable,
                consumable: true, sort: LootLockerItemSortField.created_at, order: LootLockerSortOrder.DESC, onComplete: (r) =>
                {
                    response = r;
                    listCallCompleted = true;
                });
            yield return new WaitUntil(() => listCallCompleted);

            // Then
            Assert.IsTrue(response.success, response.errorData?.ToString() ?? "Filtered ListPlayerItems call failed");
            Assert.IsNotNull(response.items, "Player items should not be null");
            Assert.IsTrue(response.items.Any(i => i.id == grantedInventoryId), "Expected the granted item to match the filters");
            Assert.IsTrue(response.items.All(i => i.item_type == LootLockerItemType.stackable), "All returned items should be stackable");
            Assert.IsTrue(response.items.All(i => i.consumable), "All returned items should be consumable");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        [Timeout(360_000)]
        public IEnumerator Items_GetPlayerItem_ReturnsItemWithTemplate()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            bool getCallCompleted = false;
            LootLockerGetPlayerItemResponse response = null;
            LootLockerSDKManager.GetPlayerItem(grantedInventoryId, (r) =>
            {
                response = r;
                getCallCompleted = true;
            });
            yield return new WaitUntil(() => getCallCompleted);

            // Then
            Assert.IsTrue(response.success, response.errorData?.ToString() ?? "GetPlayerItem call failed");
            Assert.AreEqual(grantedInventoryId, response.id, "Returned item id should match the requested id");
            Assert.AreEqual(5, response.count, "Item count should be 5");
            Assert.AreEqual(LootLockerItemType.stackable, response.item_type, "Item should be stackable");
            Assert.IsTrue(response.deletable, "Item should be deletable");
            Assert.IsNotNull(response.template, "Item template should be populated on the single item response");
            Assert.AreEqual(createdTemplateId, response.template.id, "Template id should match the created template");
            Assert.AreEqual(createdTemplateName, response.template.name, "Template name should match the created template");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        [Timeout(360_000)]
        public IEnumerator Items_ConsumePlayerItem_PartialStack_DecrementsCount()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            bool consumeCallCompleted = false;
            LootLockerConsumeItemResponse consumeResponse = null;
            LootLockerSDKManager.ConsumePlayerItem(grantedInventoryId, 2, (r) =>
            {
                consumeResponse = r;
                consumeCallCompleted = true;
            });
            yield return new WaitUntil(() => consumeCallCompleted);

            // Then
            Assert.IsTrue(consumeResponse.success, consumeResponse.errorData?.ToString() ?? "ConsumePlayerItem call failed");
            Assert.IsTrue(consumeResponse.consumed, "Item should report as consumed");

            bool getCallCompleted = false;
            LootLockerGetPlayerItemResponse getResponse = null;
            LootLockerSDKManager.GetPlayerItem(grantedInventoryId, (r) =>
            {
                getResponse = r;
                getCallCompleted = true;
            });
            yield return new WaitUntil(() => getCallCompleted);

            Assert.IsTrue(getResponse.success, getResponse.errorData?.ToString() ?? "GetPlayerItem after consume failed");
            Assert.AreEqual(3, getResponse.count, "Consuming 2 of 5 should leave 3");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        [Timeout(360_000)]
        public IEnumerator Items_ConsumePlayerItem_WholeStack_RemovesItem()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When - passing the full count consumes the entire row
            bool consumeCallCompleted = false;
            LootLockerConsumeItemResponse consumeResponse = null;
            LootLockerSDKManager.ConsumePlayerItem(grantedInventoryId, 5, (r) =>
            {
                consumeResponse = r;
                consumeCallCompleted = true;
            });
            yield return new WaitUntil(() => consumeCallCompleted);

            // Then
            Assert.IsTrue(consumeResponse.success, consumeResponse.errorData?.ToString() ?? "ConsumePlayerItem call failed");
            Assert.IsTrue(consumeResponse.consumed, "Item should report as consumed");

            bool listCallCompleted = false;
            LootLockerListPlayerItemsResponse listResponse = null;
            LootLockerSDKManager.ListPlayerItems(1, 100, onComplete: (r) =>
            {
                listResponse = r;
                listCallCompleted = true;
            });
            yield return new WaitUntil(() => listCallCompleted);

            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListPlayerItems after consume failed");
            Assert.IsFalse(listResponse.items.Any(i => i.id == grantedInventoryId), "Fully consumed item should no longer be listed");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        [Timeout(360_000)]
        public IEnumerator Items_SplitPlayerItemStack_CreatesSecondStack()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            bool splitCallCompleted = false;
            LootLockerSplitItemStackResponse splitResponse = null;
            LootLockerSDKManager.SplitPlayerItemStack(grantedInventoryId, 2, (r) =>
            {
                splitResponse = r;
                splitCallCompleted = true;
            });
            yield return new WaitUntil(() => splitCallCompleted);

            // Then
            Assert.IsTrue(splitResponse.success, splitResponse.errorData?.ToString() ?? "SplitPlayerItemStack call failed");
            Assert.IsNotNull(splitResponse.id, "Split should return the id of the new stack");
            Assert.AreNotEqual(grantedInventoryId, splitResponse.id, "The new stack should have a different id");

            bool listCallCompleted = false;
            LootLockerListPlayerItemsResponse listResponse = null;
            LootLockerSDKManager.ListPlayerItems(1, 100, onComplete: (r) =>
            {
                listResponse = r;
                listCallCompleted = true;
            });
            yield return new WaitUntil(() => listCallCompleted);

            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListPlayerItems after split failed");
            var original = listResponse.items.FirstOrDefault(i => i.id == grantedInventoryId);
            var split = listResponse.items.FirstOrDefault(i => i.id == splitResponse.id);
            Assert.IsNotNull(original, "Original stack should still be listed");
            Assert.IsNotNull(split, "New stack should be listed");
            Assert.AreEqual(3, original.count, "Original stack should be left with 3");
            Assert.AreEqual(2, split.count, "New stack should hold 2");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        [Timeout(360_000)]
        public IEnumerator Items_MergePlayerItemStacks_CombinesCounts()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given - split off a second stack to merge back
            bool splitCallCompleted = false;
            LootLockerSplitItemStackResponse splitResponse = null;
            LootLockerSDKManager.SplitPlayerItemStack(grantedInventoryId, 2, (r) =>
            {
                splitResponse = r;
                splitCallCompleted = true;
            });
            yield return new WaitUntil(() => splitCallCompleted);
            Assert.IsTrue(splitResponse.success, splitResponse.errorData?.ToString() ?? "SplitPlayerItemStack call failed");

            // When - merge the new stack back into the original
            bool mergeCallCompleted = false;
            LootLockerResponse mergeResponse = null;
            LootLockerSDKManager.MergePlayerItemStacks(splitResponse.id, grantedInventoryId, (r) =>
            {
                mergeResponse = r;
                mergeCallCompleted = true;
            });
            yield return new WaitUntil(() => mergeCallCompleted);

            // Then
            Assert.IsTrue(mergeResponse.success, mergeResponse.errorData?.ToString() ?? "MergePlayerItemStacks call failed");

            bool listCallCompleted = false;
            LootLockerListPlayerItemsResponse listResponse = null;
            LootLockerSDKManager.ListPlayerItems(1, 100, onComplete: (r) =>
            {
                listResponse = r;
                listCallCompleted = true;
            });
            yield return new WaitUntil(() => listCallCompleted);

            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListPlayerItems after merge failed");
            var target = listResponse.items.FirstOrDefault(i => i.id == grantedInventoryId);
            Assert.IsNotNull(target, "Target stack should still be listed");
            Assert.AreEqual(5, target.count, "Merged stack should hold the combined count of 5");
            Assert.IsFalse(listResponse.items.Any(i => i.id == splitResponse.id), "Source stack should be gone after merging");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        [Timeout(360_000)]
        public IEnumerator Items_DeletePlayerItem_RemovesItem()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            bool deleteCallCompleted = false;
            LootLockerResponse deleteResponse = null;
            LootLockerSDKManager.DeletePlayerItem(grantedInventoryId, (r) =>
            {
                deleteResponse = r;
                deleteCallCompleted = true;
            });
            yield return new WaitUntil(() => deleteCallCompleted);

            // Then
            Assert.IsTrue(deleteResponse.success, deleteResponse.errorData?.ToString() ?? "DeletePlayerItem call failed");

            bool listCallCompleted = false;
            LootLockerListPlayerItemsResponse listResponse = null;
            LootLockerSDKManager.ListPlayerItems(1, 100, onComplete: (r) =>
            {
                listResponse = r;
                listCallCompleted = true;
            });
            yield return new WaitUntil(() => listCallCompleted);

            Assert.IsTrue(listResponse.success, listResponse.errorData?.ToString() ?? "ListPlayerItems after delete failed");
            Assert.IsFalse(listResponse.items.Any(i => i.id == grantedInventoryId), "Deleted item should no longer be listed");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        [Timeout(360_000)]
        public IEnumerator Items_DeletePlayerItem_NotDeletable_ReturnsForbidden()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given - a second template that is explicitly not deletable
            bool createTemplateCallCompleted = false;
            string nonDeletableTemplateId = string.Empty;
            LootLockerTestItems.CreateItemTemplate(LootLockerTestItems.GetRandomItemTemplateName(), "stackable", false, false, new[] { PublicAudience }, (templateResponse) =>
            {
                if (templateResponse == null || !templateResponse.success)
                {
                    Debug.LogError("Failed to create non-deletable item template: " + templateResponse?.errorData?.message);
                    createTemplateCallCompleted = true;
                    return;
                }

                nonDeletableTemplateId = templateResponse.id;
                createTemplateCallCompleted = true;
            });
            yield return new WaitUntil(() => createTemplateCallCompleted);
            Assert.IsNotEmpty(nonDeletableTemplateId, "Failed to create the non-deletable template");

            bool grantCallCompleted = false;
            string nonDeletableInventoryId = string.Empty;
            LootLockerTestItems.GrantItemToPlayer(sessionPlayerId, nonDeletableTemplateId, 1, (grantResponse) =>
            {
                if (grantResponse == null || !grantResponse.success)
                {
                    Debug.LogError("Failed to grant non-deletable item: " + grantResponse?.errorData?.message);
                    grantCallCompleted = true;
                    return;
                }

                nonDeletableInventoryId = grantResponse.id;
                grantCallCompleted = true;
            });
            yield return new WaitUntil(() => grantCallCompleted);
            Assert.IsNotEmpty(nonDeletableInventoryId, "Failed to grant the non-deletable item");

            // When
            bool deleteCallCompleted = false;
            LootLockerResponse deleteResponse = null;
            LootLockerSDKManager.DeletePlayerItem(nonDeletableInventoryId, (r) =>
            {
                deleteResponse = r;
                deleteCallCompleted = true;
            });
            yield return new WaitUntil(() => deleteCallCompleted);

            // Then
            Assert.IsFalse(deleteResponse.success, "Deleting a non-deletable item should fail");
            Assert.AreEqual(403, deleteResponse.statusCode, "Deleting a non-deletable item should return 403 Forbidden");
        }
    }
}
