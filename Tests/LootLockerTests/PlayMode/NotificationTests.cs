using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LootLocker;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode {
public class NotificationTests
{
    private static int TestCounter = 0;
    private static int DefaultTriggersToCreate = 4;
    private LootLockerTestGame gameUnderTest = null;
    private LootLockerConfig configCopy = null;
    private bool SetupFailed = false;
    private List<string> CreatedTriggers = null;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        TestCounter++;
        CreatedTriggers = new List<string>();
        SetupFailed = false;
        gameUnderTest = null;
        configCopy = LootLockerConfig.current;

        if (!LootLockerConfig.ClearSettings())
        {
            Debug.LogError("Could not clear LootLocker config");
        }

        bool gameCreationCallCompleted = false;
        LootLockerTestGame.CreateGame(testName: this.GetType().Name + TestCounter, onComplete: (success, errorMessage, game) =>
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

        // Enable necessary platform or features
        bool platformEnabled = false;
        gameUnderTest?.EnableGuestLogin((success, errorMessage) =>
        {
            SetupFailed = !success;
            platformEnabled = true;
        });
        yield return new WaitUntil(() => platformEnabled);
        if (SetupFailed)
        {
            yield break;
        }
        Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Successfully created test game and initialized LootLocker");

        // Sign in client
        bool guestLoginCompleted = false;
        LootLockerSDKManager.StartGuestSession(GUID.Generate().ToString(), response =>
        {
            SetupFailed |= !response.success;
            guestLoginCompleted = true;
        });
        yield return new WaitUntil(() => guestLoginCompleted);

        for(int i = 0; i < DefaultTriggersToCreate; i++) {
            string triggerKey = "test_trigger_no_limit_"+i;
            bool triggerCreated = false;
            LootLockerTestTrigger createdTrigger = null;
            yield return CreateTriggerWithReward(triggerKey, "Test Trigger No Limit" + i, 0, (success, error, trigger) =>
            {
                triggerCreated = success;
                createdTrigger = trigger;
            });
            SetupFailed |= !triggerCreated;
            if(SetupFailed) {
                yield break;
            }
            CreatedTriggers.Add(createdTrigger.key);
        }

        bool invokeCompleted = false;
        LootLockerSDKManager.InvokeTriggersByKey(CreatedTriggers.ToArray(), response =>
        {
            SetupFailed |= !response.success || response.Successful_keys.Length != CreatedTriggers.Count;
            invokeCompleted = true;
        });
        yield return new WaitUntil(() => invokeCompleted);
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

    private IEnumerator CreateTriggerWithReward(string triggerKey, string triggerName, int limit, Action<bool, string, LootLockerTestTrigger> onComplete)
    {
        string errorMessage = "";
        bool contextRetrieved = false;
        LootLockerTestContextResponse contextResponse = null;

        // Step 1: Get Asset Contexts
        LootLockerTestAssets.GetAssetContexts((success, error, response) =>
        {
            contextResponse = response;
            contextRetrieved = true;
            if (!success) errorMessage = "Failed to retrieve asset contexts: " + error;
        });
        yield return new WaitUntil(() => contextRetrieved);

        if (contextResponse == null || contextResponse.contexts.Length == 0)
        {
            onComplete?.Invoke(false, errorMessage ?? "No asset contexts found", null);
            yield break;
        }

        LootLockerTestAssetResponse assetResponse = null;
        bool assetCreated = false;

        // Step 2: Create Asset
        LootLockerTestAssets.CreateAsset(contextResponse.contexts[0].id, response =>
        {
            assetResponse = response;
            assetCreated = true;
            if (!response.success) errorMessage = "Failed to create asset: " + response.errorData.ToString();
        });
        yield return new WaitUntil(() => assetCreated);

        if (assetResponse == null || !assetResponse.success)
        {
            onComplete?.Invoke(false, errorMessage, null);
            yield break;
        }

        bool rewardCreated = false;
        string rewardId = null;

        // Step 3: Create Reward
        LootLockerTestAssets.CreateReward(
            new LootLockerRewardRequest { entity_id = assetResponse.asset.ulid, entity_kind = "asset" }, response =>
            {
                rewardId = response.id;
                rewardCreated = true;
                if (string.IsNullOrEmpty(rewardId)) errorMessage = "Failed to create reward";
            });
        yield return new WaitUntil(() => rewardCreated);

        if (string.IsNullOrEmpty(rewardId))
        {
            onComplete?.Invoke(false, errorMessage, null);
            yield break;
        }

        bool triggerCreated = false;
        LootLockerTestTrigger createdTrigger = null;

        // Step 4: Create Trigger
        gameUnderTest.CreateTrigger(triggerKey, triggerName, limit, rewardId, (success, error, trigger) =>
        {
            createdTrigger = trigger;
            if (!success) errorMessage = "Failed to create trigger: " + error;
            triggerCreated = success;
        });
        yield return new WaitUntil(() => triggerCreated);

        onComplete?.Invoke(triggerCreated, errorMessage, createdTrigger);
    }

    [UnityTest]
    public IEnumerator Notifications_ListWithDefaultParametersWhenLessThanDefaultPageSize_ReturnsAllNotifications()
    {
        Assert.IsFalse(SetupFailed, "Setup did not succeed");

        // When
        bool requestCompleted = false;
        LootLockerListNotificationsResponse response = null;
        LootLockerSDKManager.ListNotificationsWithDefaultParameters((res) =>
        {
            response = res;
            requestCompleted = true;
        });
        yield return new WaitUntil(() => requestCompleted);

        // Then
        Assert.IsTrue(response.success, "Failed to list notifications");
        Assert.IsNotNull(response.Notifications, "Notifications list is null");
        Assert.AreEqual(CreatedTriggers.Count, response.Notifications.Length, "Notifications list is not of expected length");

        foreach (LootLockerNotification notification in response.Notifications)
        {
            Assert.AreEqual(LootLocker.LootLockerStaticStrings.LootLockerNotificationTypes.PullRewardAcquired, notification.Notification_type, "Type was not as expected");
            Assert.AreEqual(LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Triggers, notification.Source, "Source was not as expected");
            Assert.AreEqual(LootLockerNotificationPriority.medium, notification.Priority, "Priority was not as expected");
            switch (notification.Content.Body.Kind)
            {
                case LootLockerNotificationContentKind.asset:
                {
                    Assert.IsNotEmpty(notification.Content.Body.Asset.Asset_ulid, "No ulid parsed for asset");
                    Assert.IsNotEmpty(notification.Content.Body.Asset.Details.Name, "No name was parsed for asset");
                    Assert.IsNotEmpty(notification.Content.ContextAsDictionary[LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Key], "Notification trigger key was empty");
                    Assert.IsNotEmpty(notification.Content.ContextAsDictionary[LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Id], "Notification trigger id was empty");
                    Assert.IsNotEmpty(notification.Content.ContextAsDictionary[LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Limit], "Notification trigger limit was empty");
                    Assert.AreEqual(0, int.Parse(notification.Content.ContextAsDictionary[LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Limit]), "Notification trigger limit was not 0");
                }
                    break;
                case LootLockerNotificationContentKind.currency:
                case LootLockerNotificationContentKind.progression_reset:
                case LootLockerNotificationContentKind.progression_points:
                case LootLockerNotificationContentKind.group:
                    Assert.Fail("Test has not been prepared for group, currency, or progression rewards");
                    break;
            }
        }
    }

    [UnityTest]
    public IEnumerator Notifications_ListNotificationsWithPaginationParameters_ReturnsCorrectPage()
    {
        // Given

        // Create some more triggers and execute them 
        List<string> localTriggersCreated = new List<string>();
        for (int i = DefaultTriggersToCreate; i < DefaultTriggersToCreate+15; i++)
        {
            string triggerKey = "test_trigger_no_limit_" + i;
            bool triggerCreated = false;
            LootLockerTestTrigger createdTrigger = null;
            yield return CreateTriggerWithReward(triggerKey, "Test Trigger No Limit" + i, 0, (success, error, trigger) =>
            {
                triggerCreated = success;
                createdTrigger = trigger;
            });
            SetupFailed |= !triggerCreated;
            if (SetupFailed)
            {
                yield break;
            }
            CreatedTriggers.Add(createdTrigger.key);
            localTriggersCreated.Add(createdTrigger.key);
        }

        bool invokeCompleted = false;
        bool triggerInvocationSucceeded = false;
        LootLockerSDKManager.InvokeTriggersByKey(localTriggersCreated.ToArray(), response =>
        {
            triggerInvocationSucceeded |= response.success && response.Successful_keys?.Length == localTriggersCreated.Count;
            invokeCompleted = true;
        });
        yield return new WaitUntil(() => invokeCompleted);
        Assert.IsTrue(triggerInvocationSucceeded, "Trigger invocation did not succeed");

        // When
        bool notificationsListed = false;
        LootLockerListNotificationsResponse paginationRequest1 = null;
        LootLockerSDKManager.ListNotifications(false, null, "", "", 5, 3, response =>
        {
            paginationRequest1 = response;
            notificationsListed = true;
        });
        yield return new WaitUntil(() => notificationsListed);

        notificationsListed = false;
        LootLockerListNotificationsResponse paginationRequest2 = null;
        LootLockerSDKManager.ListNotifications(false, null, "", "", 5, paginationRequest1.Pagination.last_page, response =>
        {
            paginationRequest2 = response;
            notificationsListed = true;
        });
        yield return new WaitUntil(() => notificationsListed);

        // Then
        Assert.IsTrue(paginationRequest1.success, "Listing notifications with pagination failed");
        Assert.IsTrue(paginationRequest2.success, "Listing notifications with pagination failed");
        Assert.IsNotNull(paginationRequest1.Notifications, "Notifications list should not be null");
        Assert.IsNotNull(paginationRequest2.Notifications, "Notifications list should not be null");
        Assert.AreEqual(5, paginationRequest1.Notifications.Length, "The number of notifications returned is not as expected");
        int expectedNotificationsOnLastPage = CreatedTriggers.Count % 5;
        Assert.AreEqual(expectedNotificationsOnLastPage, paginationRequest2.Notifications.Length, "The number of notifications returned is not as expected");
        List<string> paginationRequest2NotificationIds = new List<string>();
        foreach (LootLockerNotification notification in paginationRequest2.Notifications)
        {
            paginationRequest2NotificationIds.Add(notification.Id);
        }
        foreach (LootLockerNotification notification in paginationRequest1.Notifications)
        {
            Assert.False(paginationRequest2NotificationIds.Contains(notification.Id), $"Same notification was returned on both pages {notification.Id}");
        }
    }

    [UnityTest]
    public IEnumerator Notifications_MarkAllNotificationsAsRead_AllNotificationsMarkedAsRead()
    {
        // Given
        bool notificationsListed = false;
        LootLockerListNotificationsResponse initialListResponse = null;
        LootLockerSDKManager.ListNotifications(false, null, "", "", 100, 0,  response =>
        {
            initialListResponse = response;
            notificationsListed = true;
        });
        yield return new WaitUntil(() => notificationsListed);
        Assert.IsTrue(initialListResponse.success, "Listing notifications before marking as read failed");
        Assert.AreEqual(CreatedTriggers.Count, initialListResponse.Notifications.Length, "Not all expected notifications were returned");

        foreach (LootLockerNotification notification in initialListResponse.Notifications)
        {
            Assert.IsFalse(notification.Read, $"Notification was not marked as read: {notification.Content.ContextAsDictionary[LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Key]}");
        }

        // When
        bool markAllAsReadSuccess = false;
        bool markAllCompleted = false;

        LootLockerSDKManager.MarkAllNotificationsAsRead(response =>
        {
            markAllAsReadSuccess = response.success;
            markAllCompleted = true;
        });
        yield return new WaitUntil(() => markAllCompleted);

        // Then
        Assert.IsTrue(markAllAsReadSuccess, "Marking all as read failed");
        
        notificationsListed = false;
        LootLockerListNotificationsResponse listResponseAfterMarkAsRead = null;
        LootLockerSDKManager.ListNotifications(true, null, "", "", 100, 0, response =>
        {
            listResponseAfterMarkAsRead = response;
            notificationsListed = true;
        });
        yield return new WaitUntil(() => notificationsListed);

        Assert.IsTrue(listResponseAfterMarkAsRead.success, "Listing notifications after marking as read failed");
        Assert.AreEqual(CreatedTriggers.Count, listResponseAfterMarkAsRead.Notifications.Length, "Not all expected notifications were returned");
        foreach (LootLockerNotification notification in listResponseAfterMarkAsRead.Notifications)
        {
            Assert.IsTrue(notification.Read, $"Notification was not marked as read: {notification.Content.ContextAsDictionary[LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Key]}");
        }
    }
    
    [UnityTest]
    public IEnumerator Notifications_MarkSpecificNotificationsAsRead_SpecifiedNotificationsMarkedAsRead()
    {
        Assert.IsFalse(SetupFailed, "Setup did not succeed");

        // Given
        bool notificationsListed = false;
        LootLockerListNotificationsResponse listResponse = null;
        LootLockerSDKManager.ListNotifications(false, null, "", "", 100, 0, response =>
        {
            listResponse = response;
            notificationsListed = true;
        });
        yield return new WaitUntil(() => notificationsListed);
        Assert.IsTrue(listResponse.success, "Listing notifications failed");
        Assert.AreEqual(CreatedTriggers.Count, listResponse.Notifications.Length, "Not all expected notifications were returned");

        string[] notificationIdsToMarkAsRead = listResponse.Notifications.Take(2).Select(n => n.Id).ToArray();

        // When
        bool markAsReadCompleted = false;
        LootLockerSDKManager.MarkNotificationsAsRead(notificationIdsToMarkAsRead, response =>
        {
            markAsReadCompleted = true;
        });
        yield return new WaitUntil(() => markAsReadCompleted);

        // Then
        notificationsListed = false;
        LootLockerListNotificationsResponse listReadNotificationsAfterMarkAsReadResponse = null;
        LootLockerSDKManager.ListNotifications(true, null, "", "", 100, 0, response =>
        {
            listReadNotificationsAfterMarkAsReadResponse = response;
            notificationsListed = true;
        });
        yield return new WaitUntil(() => notificationsListed);
        notificationsListed = false;
        LootLockerListNotificationsResponse listUnreadNotificationsAfterMarkAsReadResponse = null;
        LootLockerSDKManager.ListNotifications(false, null, "", "", 100, 0, response =>
        {
            listUnreadNotificationsAfterMarkAsReadResponse = response;
            notificationsListed = true;
        });
        yield return new WaitUntil(() => notificationsListed);

        Assert.IsTrue(listReadNotificationsAfterMarkAsReadResponse.success, "Listing read notifications after marking specific as read failed");
        int nrOfMarkedFound = 0;
        foreach (LootLockerNotification notification in listReadNotificationsAfterMarkAsReadResponse.Notifications)
        {
            bool shouldBeRead = notificationIdsToMarkAsRead.Contains(notification.Id);
            if(shouldBeRead) {
                nrOfMarkedFound++;
            }
            Assert.AreEqual(shouldBeRead, notification.Read, $"Notification read status was not as expected for {notification.Id}");
        }
        Assert.AreEqual(notificationIdsToMarkAsRead.Count(), nrOfMarkedFound, "Not all notifications that were marked as read actually were");

        Assert.IsTrue(listUnreadNotificationsAfterMarkAsReadResponse.success, "Listing unread notifications after marking specific as read failed");
        Assert.AreEqual(listResponse.Notifications.Length - notificationIdsToMarkAsRead.Count(), listUnreadNotificationsAfterMarkAsReadResponse.Notifications.Length, "Not all notifications that were marked as read actually were");
        }

        [UnityTest]
        public IEnumerator Notifications_MarkSpecificNotificationsAsReadUsingConvenienceMethod_SpecifiedNotificationsMarkedAsRead()
        {
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            bool notificationsListed = false;
            LootLockerListNotificationsResponse listResponse = null;
            LootLockerSDKManager.ListNotifications(false, null, "", "", 100, 0, response =>
            {
                listResponse = response;
                notificationsListed = true;
            });
            yield return new WaitUntil(() => notificationsListed);
            Assert.IsTrue(listResponse.success, "Listing notifications failed");
            Assert.AreEqual(CreatedTriggers.Count, listResponse.Notifications.Length, "Not all expected notifications were returned");

            string notificationIdToMarkAsRead = listResponse.Notifications[0].Id;

            // When
            bool markAsReadSucceeded = false;
            bool markAsReadCompleted = false;
            listResponse.Notifications[0].MarkThisNotificationAsRead(response =>
            {
                markAsReadSucceeded = response.success;
                markAsReadCompleted = true;
            });
            yield return new WaitUntil(() => markAsReadCompleted);

            // Then
            Assert.IsTrue(markAsReadSucceeded, "Failed to mark as read");
            notificationsListed = false;
            LootLockerListNotificationsResponse listReadNotificationsAfterMarkAsReadResponse = null;
            LootLockerSDKManager.ListNotifications(true, null, "", "", 100, 0, response =>
            {
                listReadNotificationsAfterMarkAsReadResponse = response;
                notificationsListed = true;
            });
            yield return new WaitUntil(() => notificationsListed);
            notificationsListed = false;
            LootLockerListNotificationsResponse listUnreadNotificationsAfterMarkAsReadResponse = null;
            LootLockerSDKManager.ListNotifications(false, null, "", "", 100, 0, response =>
            {
                listUnreadNotificationsAfterMarkAsReadResponse = response;
                notificationsListed = true;
            });
            yield return new WaitUntil(() => notificationsListed);

            Assert.IsTrue(listReadNotificationsAfterMarkAsReadResponse.success, "Listing read notifications after marking specific as read failed");
            Assert.AreEqual(1, listReadNotificationsAfterMarkAsReadResponse.Notifications.Length, "The number of read notifications was not as expected");
            Assert.AreEqual(notificationIdToMarkAsRead, listReadNotificationsAfterMarkAsReadResponse.Notifications[0].Id, "The expected notification was not marked as read");

            Assert.IsTrue(listUnreadNotificationsAfterMarkAsReadResponse.success, "Listing unread notifications after marking specific as read failed");
            Assert.AreEqual(listResponse.Notifications.Length - 1, listUnreadNotificationsAfterMarkAsReadResponse.Notifications.Length, "Not all notifications that were marked as read actually were");
        }

        [UnityTest]
        public IEnumerator Notifications_MarkAllNotificationsAsReadUsingConvenienceMethod_SpecifiedNotificationsMarkedAsRead()
        {
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            bool notificationsListed = false;
            LootLockerListNotificationsResponse listResponse = null;
            LootLockerSDKManager.ListNotifications(false, null, "", "", CreatedTriggers.Count-1, 0, response =>
            {
                listResponse = response;
                notificationsListed = true;
            });
            yield return new WaitUntil(() => notificationsListed);
            Assert.IsTrue(listResponse.success, "Listing notifications failed");
            Assert.AreEqual(CreatedTriggers.Count-1, listResponse.Notifications.Length, "Not all expected notifications were returned");

            string[] notificationIdsToMarkAsRead = listResponse.Notifications.Select(n => n.Id).ToArray();

            // When
            bool markAsReadSucceeded = false;
            bool markAsReadCompleted = false;
            listResponse.MarkUnreadNotificationsAsRead(response =>
            {
                markAsReadSucceeded = response.success;
                markAsReadCompleted = true;
            });
            yield return new WaitUntil(() => markAsReadCompleted);

            // Then
            Assert.IsTrue(markAsReadSucceeded, "Failed to mark as read");
            notificationsListed = false;
            LootLockerListNotificationsResponse listReadNotificationsAfterMarkAsReadResponse = null;
            LootLockerSDKManager.ListNotifications(true, null, "", "", 100, 0, response =>
            {
                listReadNotificationsAfterMarkAsReadResponse = response;
                notificationsListed = true;
            });
            yield return new WaitUntil(() => notificationsListed);
            notificationsListed = false;
            LootLockerListNotificationsResponse listUnreadNotificationsAfterMarkAsReadResponse = null;
            LootLockerSDKManager.ListNotifications(false, null, "", "", 100, 0, response =>
            {
                listUnreadNotificationsAfterMarkAsReadResponse = response;
                notificationsListed = true;
            });
            yield return new WaitUntil(() => notificationsListed);

            Assert.IsTrue(listReadNotificationsAfterMarkAsReadResponse.success, "Listing read notifications after marking specific as read failed");
            Assert.AreEqual(notificationIdsToMarkAsRead.Length, listReadNotificationsAfterMarkAsReadResponse.Notifications.Length, "The number of read notifications was not as expected");
            int nrOfMarkedFound = 0;
            foreach (LootLockerNotification notification in listReadNotificationsAfterMarkAsReadResponse.Notifications)
            {
                bool shouldBeRead = notificationIdsToMarkAsRead.Contains(notification.Id);
                if (shouldBeRead)
                {
                    nrOfMarkedFound++;
                }
                Assert.AreEqual(shouldBeRead, notification.Read, $"Notification read status was not as expected for {notification.Id}");
            }

            Assert.IsTrue(listUnreadNotificationsAfterMarkAsReadResponse.success, "Listing unread notifications after marking specific as read failed");
            Assert.AreEqual(CreatedTriggers.Count - notificationIdsToMarkAsRead.Length, listUnreadNotificationsAfterMarkAsReadResponse.Notifications.Length, "Not all notifications that were marked as read actually were");
        }

        [UnityTest]
        public IEnumerator Notifications_ConvenienceLookupTable_CanLookUpAllNotificationTypes()
        {
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            string TriggerIdentifyingValue = "trigger_key";
            string triggerNotification1Id = GUID.Generate().ToString();
            string triggerNotification2Id = GUID.Generate().ToString();
            string triggerNotification3Id = GUID.Generate().ToString();
            string LootLockerVirtualStorePurchaseIdentifyingValue = "catalog_item_id";
            string lootLockerVirtualStoreNotification1Id = GUID.Generate().ToString();
            string AppleAppStorePurchaseIdentifyingValue = "transaction_id";
            string appleAppStoreNotification1Id = GUID.Generate().ToString();
            string GooglePlayStoreStorePurchaseIdentifyingValue = "product_id";
            string googlePlayStoreNotification1Id = GUID.Generate().ToString();
            var fakeResponse = new LootLockerListNotificationsResponse()
            {
                statusCode = 200,
                success = true,
                text = "",
                errorData = null,
                EventId = "1234",
                Pagination = new LootLockerExtendedPagination
                {
                    errors = null,
                    current_page = 1,
                    prev_page = null,
                    next_page = null,
                    last_page = 1,
                    offset = 0,
                    per_page = 100,
                    total = 6
                },
                Notifications = new LootLockerNotification[]
                {
                    GenerateLootLockerNotification(triggerNotification1Id, LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Triggers, TriggerIdentifyingValue),
                    GenerateLootLockerNotification(triggerNotification2Id, LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Triggers, TriggerIdentifyingValue),
                    GenerateLootLockerNotification(triggerNotification3Id, LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Triggers, "some_other_trigger"),
                    GenerateLootLockerNotification(lootLockerVirtualStoreNotification1Id, LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Purchasing.LootLocker, LootLockerVirtualStorePurchaseIdentifyingValue),
                    GenerateLootLockerNotification(appleAppStoreNotification1Id, LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Purchasing.AppleAppStore, AppleAppStorePurchaseIdentifyingValue),
                    GenerateLootLockerNotification(googlePlayStoreNotification1Id, LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Purchasing.GooglePlayStore, GooglePlayStoreStorePurchaseIdentifyingValue),
                }
            };

            fakeResponse.PopulateConvenienceStructures();

            // When
            bool triggerLookupSucceeded = fakeResponse.TryGetNotificationsByIdentifyingValue(TriggerIdentifyingValue, out var triggerNotifications);
            bool lootLockerVirtualStoreLookupSucceeded = fakeResponse.TryGetNotificationsByIdentifyingValue(LootLockerVirtualStorePurchaseIdentifyingValue, out var lootLockerVirtualStoreNotifications);
            bool appleAppStoreLookupSucceeded = fakeResponse.TryGetNotificationsByIdentifyingValue(AppleAppStorePurchaseIdentifyingValue, out var appleAppStoreNotifications);
            bool googlePlayStoreLookupSucceeded = fakeResponse.TryGetNotificationsByIdentifyingValue(GooglePlayStoreStorePurchaseIdentifyingValue, out var googlePlayStoreNotifications);

            // Then
            Assert.IsTrue(triggerLookupSucceeded, "Trigger notification lookup failed");
            Assert.IsNotEmpty(triggerNotifications, "Trigger notification lookup array was empty");
            Assert.AreEqual(2, triggerNotifications.Length, "The right amount of trigger notifications were not retrieved");
            var retrievedNotificationIds = triggerNotifications.Take(2).Select(notification => notification.Id).ToArray();
            Assert.Contains(triggerNotification1Id, retrievedNotificationIds, "The retrieved trigger notifications did not contain the expected id");
            Assert.Contains(triggerNotification2Id, retrievedNotificationIds, "The retrieved trigger notifications did not contain the expected id");

            Assert.IsTrue(lootLockerVirtualStoreLookupSucceeded, "LootLocker Virtual Store notification lookup failed");
            Assert.IsNotEmpty(lootLockerVirtualStoreNotifications, "LootLocker Virtual Store notification lookup array was empty");
            Assert.AreEqual(lootLockerVirtualStoreNotification1Id, lootLockerVirtualStoreNotifications[0].Id, "The retrieved lootlocker virtual store notification id was not as expected");

            Assert.IsTrue(appleAppStoreLookupSucceeded, "Apple app store notification lookup failed");
            Assert.IsNotEmpty(appleAppStoreNotifications, "Apple app store notification lookup array was empty");
            Assert.AreEqual(appleAppStoreNotification1Id, appleAppStoreNotifications[0].Id, "The retrieved Apple app store notification id was not as expected");

            Assert.IsTrue(googlePlayStoreLookupSucceeded, "Google Play store notification lookup failed");
            Assert.IsNotEmpty(googlePlayStoreNotifications, "Google Play store notification lookup array was empty");
            Assert.AreEqual(googlePlayStoreNotification1Id, googlePlayStoreNotifications[0].Id, "The retrieved Google Play store notification id was not as expected");

            yield break;
        }

        private static LootLockerNotification GenerateLootLockerNotification(string notificationId, string source, string identifyingValue)
        {
            LootLockerNotificationContextEntry[] context = null;
            if (source.Equals(LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Triggers, StringComparison.OrdinalIgnoreCase))
            {
                context = new[]
                {
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Key,
                        Value = identifyingValue
                    },
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Id,
                        Value = GUID.Generate().ToString(),
                    },
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Limit,
                        Value = "10"
                    }
                };
            }
            else if (source.Equals(LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Purchasing.LootLocker, StringComparison.OrdinalIgnoreCase))
            {
                context = new[]
                {
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.LootLocker.CatalogItemId,
                        Value = identifyingValue
                    },
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.LootLocker.CatalogId,
                        Value = GUID.Generate().ToString(),
                    }
                };
            }
            else if (source.Equals(LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Purchasing.GooglePlayStore, StringComparison.OrdinalIgnoreCase))
            {
                context = new[]
                {
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.GooglePlayStore.ProductId,
                        Value = identifyingValue
                    },
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.GooglePlayStore.CatalogItemId,
                        Value = GUID.Generate().ToString(),
                    },
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.GooglePlayStore.CatalogId,
                        Value = GUID.Generate().ToString()
                    }
                };
            }
            else if (source.Equals(LootLocker.LootLockerStaticStrings.LootLockerNotificationSources.Purchasing.AppleAppStore, StringComparison.OrdinalIgnoreCase))
            {
                context = new[]
                {
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.AppleAppStore.TransactionId,
                        Value = identifyingValue
                    },
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.AppleAppStore.CatalogItemId,
                        Value = GUID.Generate().ToString(),
                    },
                    new LootLockerNotificationContextEntry
                    {
                        Key = LootLocker.LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.AppleAppStore.CatalogId,
                        Value = GUID.Generate().ToString()
                    }
                };
            }
            return new LootLockerNotification
            {
                Id = notificationId,
                Created_at = DateTime.Now,
                Expiration_date = DateTime.Now.AddDays(30).ToString(CultureInfo.InvariantCulture),
                Player_id = GUID.Generate().ToString(),
                Priority = LootLockerNotificationPriority.medium,
                Read = false,
                Read_at = null,
                Notification_type = LootLocker.LootLockerStaticStrings.LootLockerNotificationTypes.PullRewardAcquired,
                Source = source,
                Content = new LootLockerNotificationContent
                {
                    Context = context,
                    Body = new LootLockerNotificationContentBody
                    {
                        Kind = LootLockerNotificationContentKind.currency,
                        Asset = null,
                        Group = null,
                        Progression_reset = null,
                        Progression_points = null,
                        Currency = new LootLockerNotificationRewardCurrency
                        {
                            Amount = "100",
                            Created_at = DateTime.Now,
                            Currency_id = GUID.Generate().ToString(),
                            Reward_id = GUID.Generate().ToString(),
                            Updated_at = DateTime.Now,
                            Details = new LootLockerNotificationRewardCurrencyDetails
                            {
                                Amount = "100",
                                Code = "GLD",
                                Id = GUID.Generate().ToString(),
                                Name = "Gold"
                            }
                        }
                    }
                }
            };
        }
}
}
