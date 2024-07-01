using System;
using System.Collections;
using LootLocker;
using LootLocker.Requests;
using UnityEngine.TestTools;
using LootLockerTestConfigurationUtils;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace LootLockerTests.PlayMode
{
    public class PingTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private static readonly string TestClassName = "PingTest";
        private bool SetupFailed = false;

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
            LootLockerTestGame.CreateGame(testName: TestClassName + TestCounter + " ", onComplete: (success, errorMessage, game) =>
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
                enableGuestLoginCallCompleted = true;
                if (!success)
                {
                    Debug.LogError(errorMessage);
                    SetupFailed = true;
                }
            });
            yield return new WaitUntil(() => enableGuestLoginCallCompleted); 
            if (SetupFailed)
            {
                yield break;
            }
            gameUnderTest?.InitializeLootLockerSDK();
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

        [UnityTest]
        public IEnumerator PingWithGuestUserSucceeds()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            string userGuid = Guid.NewGuid().ToString();
            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(userGuid, response =>
            {
                Assert.IsTrue(response.success, response?.errorData?.ToString());
                if (!response.success)
                {
                    return;
                }
                guestLoginCompleted = true;
            });
            yield return new WaitUntil(() => guestLoginCompleted);

            // When
            bool pingRequestCompleted = false;
            LootLockerPingResponse pingResponse = null;
            LootLockerSDKManager.Ping(_pingResponse =>
            {
                pingResponse = _pingResponse;
                pingRequestCompleted = true;
            });
            yield return new WaitUntil(() => pingRequestCompleted);

            // Then
            Assert.IsTrue(pingResponse.success, pingResponse.errorData?.ToString());
            Assert.IsNotNull(pingResponse.date, "Ping response contained no date");
        }

        [UnityTest]
        public IEnumerator PingWithoutSessionFails()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given -- empty

            // When
            bool pingRequestCompleted = false;
            LootLockerPingResponse pingResponse = null;
            LootLockerSDKManager.Ping(_pingResponse =>
            {
                pingResponse = _pingResponse;
                pingRequestCompleted = true;
            });
            yield return new WaitUntil(() => pingRequestCompleted);

            // Then
            Assert.IsFalse(pingResponse.success, pingResponse.errorData?.ToString());
            Assert.IsTrue(string.IsNullOrEmpty(pingResponse.date), "Ping response contained a date despite failing request");
        }
    }
}