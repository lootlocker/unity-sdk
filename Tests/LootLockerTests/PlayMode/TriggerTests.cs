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
            LootLockerTestGame.CreateGame(testName: this.GetType().Name + TestCounter + " ",
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

        [UnityTest]
        public IEnumerator Triggers_MultipleTriggersWithoutLimitCalledInSameCall_Succeeds()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            string triggerKey1 = "test_trigger_no_limit_1";
            string triggerKey2 = "test_trigger_no_limit_2";

            bool trigger1Created = false;
            bool trigger2Created = false;
            string errorMessage1 = null;
            string errorMessage2 = null;

            yield return CreateTriggerWithReward(triggerKey1, "Test Trigger No Limit 1", 0, (success, error, trigger) =>
            {
                trigger1Created = success;
                errorMessage1 = error;
            });
            Assert.IsTrue(trigger1Created, errorMessage1);

            yield return CreateTriggerWithReward(triggerKey2, "Test Trigger No Limit 2", 0, (success, error, trigger) =>
            {
                trigger2Created = success;
                errorMessage2 = error;
            });
            Assert.IsTrue(trigger2Created, errorMessage2);

            // When
            bool invokeCompleted = false;
            LootLockerInvokeTriggersByKeyResponse invokeResponse = null;
            LootLockerSDKManager.InvokeTriggersByKey(new string[] { triggerKey1, triggerKey2 }, response =>
            {
                invokeResponse = response;
                invokeCompleted = true;
            });
            yield return new WaitUntil(() => invokeCompleted);

            // Then
            Assert.IsTrue(invokeResponse.success, "Trigger invocation failed");
            Assert.AreEqual(2, invokeResponse.Successful_keys?.Length, "Successful Keys were not of expected length");
            Assert.AreEqual(0, invokeResponse.Failed_keys?.Length, "Failed Keys were not of expected length");
        }

        [UnityTest]
        public IEnumerator Triggers_InvokeNonExistentTrigger_Fails()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            string nonExistentTriggerKey = "non_existent_trigger";

            // When
            bool invokeCompleted = false;
            LootLockerInvokeTriggersByKeyResponse invokeResponse = null;
            LootLockerSDKManager.InvokeTriggersByKey(new string[] { nonExistentTriggerKey }, response =>
            {
                invokeResponse = response;
                invokeCompleted = true;
            });
            yield return new WaitUntil(() => invokeCompleted);

            // Then
            Assert.IsTrue(invokeResponse.success, "Non-existent trigger invocation should fail");
            Assert.AreEqual(0, invokeResponse.Successful_keys?.Length, "Successful Keys should be empty");
            Assert.AreEqual(1, invokeResponse.Failed_keys?.Length, "Failed Keys should have one entry");
            Assert.AreEqual(nonExistentTriggerKey, invokeResponse?.Failed_keys[0].Key, "Failed key was not as expected");
        }

        [UnityTest]
        public IEnumerator Triggers_InvokeTriggerOverLimit_Fails()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            string triggerKey = "test_trigger_over_limit";
            bool triggerCreated = false;
            string errorMessage = null;
            LootLockerTestTrigger createdTrigger = null;
            yield return CreateTriggerWithReward(triggerKey, "Test Trigger Over Limit", 1, (success, error, trigger) =>
            {
                triggerCreated = success;
                errorMessage = error;
                createdTrigger = trigger;
            });
            Assert.IsTrue(triggerCreated, errorMessage);

            // Invoke once within limit
            bool firstInvokeCompleted = false;
            LootLockerSDKManager.InvokeTriggersByKey(new string[] { triggerKey }, response =>
            {
                firstInvokeCompleted = true;
            });
            yield return new WaitUntil(() => firstInvokeCompleted);

            // When invoking over the limit
            bool secondInvokeCompleted = false;
            LootLockerInvokeTriggersByKeyResponse secondInvokeResponse = null;
            LootLockerSDKManager.InvokeTriggersByKey(new string[] { triggerKey }, response =>
            {
                secondInvokeResponse = response;
                secondInvokeCompleted = true;
            });
            yield return new WaitUntil(() => secondInvokeCompleted);

            // Then
            Assert.IsTrue(secondInvokeResponse.success, "Trigger invocation should fail when over the limit");
            Assert.AreEqual(0, secondInvokeResponse.Successful_keys?.Length, "Successful Keys should be empty");
            Assert.AreEqual(1, secondInvokeResponse.Failed_keys?.Length, "Failed Keys should have one entry");
            Assert.AreEqual(createdTrigger.key, secondInvokeResponse?.Failed_keys[0].Key, "Failed key was not as expected");
        }

        [UnityTest]
        public IEnumerator Triggers_InvokeSameTriggerTwiceInSameCall_InvokesOnlyOnce()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            string triggerKey = "test_trigger_single_invocation";
            bool triggerCreated = false;
            string errorMessage = null;
            LootLockerTestTrigger createdTrigger = null;
            yield return CreateTriggerWithReward(triggerKey, "Test Trigger Single Invocation", 0, (success, error, trigger) =>
            {
                triggerCreated = success;
                errorMessage = error;
                createdTrigger = trigger;
            });
            Assert.IsTrue(triggerCreated, errorMessage);

            // When
            bool invokeCompleted = false;
            LootLockerInvokeTriggersByKeyResponse invokeResponse = null;
            LootLockerSDKManager.InvokeTriggersByKey(new string[] { triggerKey, triggerKey }, response =>
            {
                invokeResponse = response;
                invokeCompleted = true;
            });
            yield return new WaitUntil(() => invokeCompleted);

            // Then
            Assert.IsTrue(invokeResponse.success, "Trigger invocation failed");
            Assert.AreEqual(1, invokeResponse.Successful_keys?.Length, "Successful Keys should contain only one entry");
            Assert.AreEqual(0, invokeResponse.Failed_keys?.Length, "Failed Keys should be empty");
            Assert.AreEqual(createdTrigger.key, invokeResponse?.Successful_keys[0].Key, "The right key was not successfully executed");
        }
    }
}