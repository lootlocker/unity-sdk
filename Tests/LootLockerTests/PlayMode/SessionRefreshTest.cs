using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class SessionRefreshTest
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
            if(gameUnderTest == null)
            {
                SetupFailed = true;
            }
            if (SetupFailed)
            {
                yield break;
            }
            gameUnderTest.SwitchToStageEnvironment();

            // Enable Whitelabel platform
            bool enableWLLogin = false;
            gameUnderTest.EnableWhiteLabelLogin((success, errorMessage) =>
            {
                SetupFailed = !success;
                enableWLLogin = true;
            });
            yield return new WaitUntil(() => enableWLLogin);

            SetupFailed |= !gameUnderTest.InitializeLootLockerSDK();
            if (SetupFailed)
            {
                yield break;
            }

            string email = GetRandomName() + "@lootlocker.com";

            bool whiteLabelSignUpCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(email, "123456789", (response) =>
            {
                SetupFailed |= !response.success;
                whiteLabelSignUpCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelSignUpCompleted);
            if (SetupFailed)
            {
                yield break;
            }
            bool whiteLabelLoginCompleted = false;
            LootLockerSDKManager.WhiteLabelLoginAndStartSession(email, "123456789", true, (response) =>
            {
                SetupFailed |= !response.success;
                whiteLabelLoginCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelLoginCompleted);
            if (SetupFailed)
            {
                yield break;
            }
            
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

        public string GetRandomName()
        {
            return LootLockerTestConfigurationUtilities.GetRandomNoun() +
                   LootLockerTestConfigurationUtilities.GetRandomVerb();
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator RefreshSession_ExpiredWhiteLabelSessionAndAutoRefreshEnabled_SessionIsAutomaticallyRefreshed()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            const string invalidToken = "ThisIsANonExistentToken";
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);
            if (playerData != null)
            {
                playerData.SessionToken = invalidToken;
                LootLockerStateData.SetPlayerData(playerData);
            }
            LootLockerConfig.current.allowTokenRefresh = true;
            LootLockerPingResponse actualPingResponse = null;

            // When
            bool completed = false;
            LootLockerSDKManager.Ping(response =>
            {
                actualPingResponse = response;
                completed = true;
            });

            // Wait for response
            yield return new WaitUntil(() => completed);

            // Then
            Assert.NotNull(actualPingResponse, "Request did not execute correctly");
            Assert.IsTrue(actualPingResponse.success, "Ping failed");
            Assert.AreNotEqual(invalidToken, LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null)?.SessionToken, "Token was not refreshed");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator RefreshSession_ExpiredWhiteLabelSessionButAutoRefreshDisabled_SessionDoesNotRefresh()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            const string invalidToken = "ThisIsANonExistentToken";
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);
            if (playerData != null)
            {
                playerData.SessionToken = invalidToken;
                LootLockerStateData.SetPlayerData(playerData);
            }
            LootLockerConfig.current.logLevel = LootLockerLogger.LogLevel.Info;
            LootLockerConfig.current.logErrorsAsWarnings = true;
            LootLockerConfig.current.allowTokenRefresh = false;
            LootLockerPingResponse actualPingResponse = null;

            // When
            bool completed = false;
            LootLockerSDKManager.Ping(response =>
            {
                actualPingResponse = response;
                completed = true;
            });

            // Wait for response
            yield return new WaitUntil(() => completed);

            // Then
            Assert.NotNull(actualPingResponse, "Request did not execute correctly");
            Assert.IsFalse(actualPingResponse.success, "Ping failed");
            Assert.AreEqual(invalidToken, LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null)?.SessionToken, "Token was not refreshed");
        }

    }
}