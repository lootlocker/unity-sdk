using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;


namespace LootLockerTests.PlayMode
{
    public class PlayerInfoTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;
        string guestSessionIdentifier = null; 

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
            LootLockerTestGame.CreateGame(testName: "GuestSessionTest" + TestCounter + " ", onComplete: (success, errorMessage, game) =>
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

            bool guestSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                if (!response.success)
                {
                    SetupFailed = true;
                }
                guestSessionIdentifier = response.player_identifier;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
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
        public IEnumerator PlayerInfo_GetSelf_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //When
            bool getPlayerInfoCompleted = false;
            LootLockerGetPlayerInfoResponse actualResponse = null;
            LootLockerSDKManager.GetPlayerInfo((response) =>
            {
                actualResponse = response;
                getPlayerInfoCompleted = true;
            });
            yield return new WaitUntil(() => getPlayerInfoCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Getting PlayerInfo failed");
        }

        [UnityTest]
        public IEnumerator PlayerInfo_GetOther_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            LootLockerGuestSessionResponse firstSessionResponse = null;
            bool firstSessionCompleted = false;

            LootLockerSDKManager.EndSession((endSessionResponse) =>
            {
                firstSessionCompleted = true;
            });

            yield return new WaitUntil(() => firstSessionCompleted);

            string playerIdentifier = GUID.Generate().ToString();
            bool secondSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(playerIdentifier , (response) =>
            {
                firstSessionResponse = response;
                secondSessionCompleted = true;
            });
            yield return new WaitUntil(() => secondSessionCompleted);

            //When
            bool getPlayerInfoCompleted = false;
            LootLockerXpResponse actualResponse = null;
            LootLockerSDKManager.GetOtherPlayerInfo(guestSessionIdentifier, (response) =>
            {
                actualResponse = response;
                getPlayerInfoCompleted = true;
            });
            yield return new WaitUntil(() => getPlayerInfoCompleted);

            //Then
            Assert.IsTrue(firstSessionResponse.success, "Getting PlayerInfo failed");
            Assert.IsFalse(string.IsNullOrEmpty(actualResponse.xp.ToString()), "No xp found in response");
        }


    }
}

