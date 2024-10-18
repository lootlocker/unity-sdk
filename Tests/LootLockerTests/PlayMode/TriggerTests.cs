using System;
using System.Collections;
using LootLocker;
using NUnit.Framework;
using UnityEngine.TestTools;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using UnityEngine;

namespace LootLockerTests.PlayMode
{
    public class TriggerTests
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private bool SetupFailed = false;
        private static int TestCounter = 0;

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
            LootLockerTestGame.CreateGame(testName: "TriggerTest" + TestCounter + " ",
                onComplete: (success, errorMessage, game) =>
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

            // Initialize SDK
            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Failed to initialize LootLocker");

            // Sign in client
            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(response =>
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
                configCopy.currentDebugLevel, configCopy.allowTokenRefresh);
        }

        private IEnumerator CreateTriggerWithReward(string triggerKey, string triggerName, int limit,
            Action<bool, string, LootLockerTestTrigger> onComplete)
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
        public IEnumerator Triggers_InvokeTriggerWithoutLimit_Succeeds()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            string triggerKey = "test_trigger_no_limit";
            bool triggerCreated = false;
            string errorMessage = null;
            LootLockerTestTrigger createdTrigger = null;
            yield return CreateTriggerWithReward(triggerKey, "Test Trigger No Limit", 0, (success, error, trigger) =>
            {
                triggerCreated = success;
                errorMessage = error;
                createdTrigger = trigger;
            });
            Assert.IsTrue(triggerCreated, errorMessage);
            // When
            bool invokeCompleted = false;
            LootLockerInvokeTriggersByKeyResponse invokeResponse = null;

            LootLockerSDKManager.InvokeTriggersByKey(new string[] { triggerKey }, response =>
            {
                invokeResponse = response;
                invokeCompleted = true;
            });
            yield return new WaitUntil(() => invokeCompleted);

            // Then
            Assert.IsTrue(invokeResponse.success, "Trigger invocation failed");
            Assert.AreEqual(1, invokeResponse.Successful_keys?.Length, "Successful Keys was not of expected length");
            Assert.AreEqual(0, invokeResponse.Failed_keys?.Length, "Failed Keys was not of expected length");
            Assert.AreEqual(createdTrigger.key, invokeResponse?.Successful_keys[0].Key,
                "The right key was not successfully executed");
        }

        [UnityTest]
        public IEnumerator Triggers_InvokeTriggerUnderLimit_Succeeds()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            string triggerKey = "test_trigger_under_limit";
            bool triggerCreated = false;
            string errorMessage = null;
            LootLockerTestTrigger createdTrigger = null;
            yield return CreateTriggerWithReward(triggerKey, "Test Trigger Under Limit", 2, (success, error, trigger) =>
            {
                triggerCreated = success;
                errorMessage = error;
                createdTrigger = trigger;
            });
            Assert.IsTrue(triggerCreated, errorMessage);
            // When
            bool invokeCompleted = false;
            LootLockerInvokeTriggersByKeyResponse invokeResponse = null;
            LootLockerSDKManager.InvokeTriggersByKey(new string[] { triggerKey }, response =>
            {
                invokeResponse = response;
                invokeCompleted = true;
            });
            yield return new WaitUntil(() => invokeCompleted);

            // Then
            Assert.IsTrue(invokeResponse.success, "Trigger invocation failed");
            Assert.AreEqual(1, invokeResponse.Successful_keys?.Length, "Successful Keys was not of expected length");
            Assert.AreEqual(0, invokeResponse.Failed_keys?.Length, "Failed Keys was not of expected length");
            Assert.AreEqual(createdTrigger.key, invokeResponse?.Successful_keys[0].Key,
                "The right key was not successfully executed");
        }
    }
}