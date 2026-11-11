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
    public class LifecycleManagerTests
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;

        // Bounds the request waits so a killed request coroutine fails the test instead of hanging it.
        private const float RequestTimeoutSeconds = 30f;

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
            if (SetupFailed) { yield break; }

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
            if (SetupFailed) { yield break; }

            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Failed to initialize LootLocker");

            bool sessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                SetupFailed |= !response.success;
                sessionCompleted = true;
            });
            yield return new WaitUntil(() => sessionCompleted);
            if (SetupFailed) { yield break; }

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
                    if (!success) { Debug.LogError(errorMessage); }
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
        public IEnumerator LifecycleManager_DestroyingReplacedInstance_KeepsCurrentInstanceAlive()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            Assert.IsTrue(LootLockerLifecycleManager.IsReady, "SDK was not ready before the test");
            var staleInstance = LootLockerLifecycleManager.Instance;
            Assert.IsNotNull(staleInstance, "No lifecycle manager instance to replace");

            // When - replace the registered instance. TeardownInstance() defers Destroy() to the end
            // of the frame, so the stale instance's OnDestroy runs after the replacement is registered.
            LootLockerLifecycleManager.ResetInstance();
            var currentInstance = LootLockerLifecycleManager.Instance;
            Assert.IsNotNull(currentInstance, "ResetInstance did not create a replacement");
            Assert.AreNotSame(staleInstance, currentInstance, "ResetInstance did not replace the instance");

            // Let the deferred OnDestroy of the stale instance run.
            yield return null;

            // Then
            Assert.IsTrue(LootLockerLifecycleManager.IsReady, "Destroying a replaced instance tore down the current instance");

            float deadline = Time.realtimeSinceStartup + RequestTimeoutSeconds;
            bool pingRequestCompleted = false;
            LootLockerPingResponse pingResponse = null;
            LootLockerSDKManager.Ping(_pingResponse =>
            {
                pingResponse = _pingResponse;
                pingRequestCompleted = true;
            });
            yield return new WaitUntil(() => pingRequestCompleted || Time.realtimeSinceStartup >= deadline);

            Assert.IsTrue(pingRequestCompleted, "Ping callback was never invoked");
            Assert.IsTrue(pingResponse.success, pingResponse.errorData?.ToString());
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator LifecycleManager_StaleInstanceDestroyedWhileRequestInFlight_InvokesCallback()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given - the currently registered instance, which becomes stale once replaced
            Assert.IsTrue(LootLockerLifecycleManager.IsReady, "SDK was not ready before the test");
            var staleInstance = LootLockerLifecycleManager.Instance;
            Assert.IsNotNull(staleInstance, "No lifecycle manager instance to replace");

            // When - replace the instance and immediately send a request, then let the stale
            // instance's deferred OnDestroy run while the request is still in flight. This mirrors a
            // game that calls Init() from a coroutine and sends its first request in the same frame,
            // with the previous instance being destroyed on the following frame.
            LootLockerLifecycleManager.ResetInstance();
            Assert.AreNotSame(staleInstance, LootLockerLifecycleManager.Instance, "ResetInstance did not replace the instance");
            Assert.IsTrue(gameUnderTest.InitializeLootLockerSDK(), "Failed to re-initialize LootLocker");

            float deadline = Time.realtimeSinceStartup + RequestTimeoutSeconds;
            bool sessionCompleted = false;
            LootLockerGuestSessionResponse sessionResponse = null;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                sessionResponse = response;
                sessionCompleted = true;
            });

            yield return new WaitUntil(() => sessionCompleted || Time.realtimeSinceStartup >= deadline);

            // Then - the in-flight request must still complete
            Assert.IsTrue(sessionCompleted, "Session callback was never invoked");
            Assert.IsTrue(sessionResponse.success, sessionResponse.errorData?.ToString());
            Assert.IsTrue(LootLockerLifecycleManager.IsReady, "SDK was not ready after the request completed");
        }
    }
}
