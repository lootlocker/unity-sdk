using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class StartSessionManualTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;

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

            LootLockerConfig.current.logLevel = LootLockerLogger.LogLevel.Debug;

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

            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Failed to initialize LootLocker");

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

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator StartSessionManual_WithNullPlayerData_ReturnsFalse()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // When
            bool result = LootLockerSDKManager.StartSessionManual(null);

            // Then
            Assert.IsFalse(result, "Expected StartSessionManual to return false with null playerData");

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator StartSessionManual_WithEmptySessionToken_ReturnsFalse()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            var playerData = new LootLockerPlayerData { SessionToken = "", ULID = "01JVWXYZ0123456789ABCDEFGH" };

            // When
            bool result = LootLockerSDKManager.StartSessionManual(playerData);

            // Then
            Assert.IsFalse(result, "Expected StartSessionManual to return false with empty SessionToken");

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator StartSessionManual_WithEmptyUlid_ReturnsFalse()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            var playerData = new LootLockerPlayerData { SessionToken = "fake-session-token", ULID = "" };

            // When
            bool result = LootLockerSDKManager.StartSessionManual(playerData);

            // Then
            Assert.IsFalse(result, "Expected StartSessionManual to return false with empty ULID");

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator StartSessionManual_WithValidData_PersistsState()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            string expectedToken = "fake-session-token";
            string expectedUlid = "01JVWXYZ0123456789ABCDEFGH";
            var playerData = new LootLockerPlayerData
            {
                SessionToken = expectedToken,
                ULID = expectedUlid,
                Name = "Test Player"
            };

            // When
            bool result = LootLockerSDKManager.StartSessionManual(playerData);

            // Then
            Assert.IsTrue(result, "Expected StartSessionManual to return true with valid data");
            var storedData = LootLockerSDKManager.GetPlayerDataForPlayerWithUlid(expectedUlid);
            Assert.IsNotNull(storedData, "Expected state to be persisted after StartSessionManual");
            Assert.AreEqual(expectedToken, storedData.SessionToken, "Expected stored SessionToken to match provided value");
            Assert.AreEqual(expectedUlid, storedData.ULID, "Expected stored ULID to match provided value");

            yield return null;
        }

    }
}
