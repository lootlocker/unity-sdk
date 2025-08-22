
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class PlayerFilesTest
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

            // Sign in client
            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                SetupFailed |= !response.success;
                guestLoginCompleted = true;
            });
            yield return new WaitUntil(() => guestLoginCompleted);

            

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

            LootLockerConfig.CreateNewSettings(configCopy.apiKey, configCopy.game_version, configCopy.domainKey,
                configCopy.logLevel, configCopy.logInBuilds, configCopy.logErrorsAsWarnings, configCopy.allowTokenRefresh);
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} tear down #####");
        }


        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_UploadSimplePublicFile_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string path = Application.temporaryCachePath + "/PlayerFileCanBeCreatedWithPathUpdatedAndThenDeleted-creation.txt";
            string content = "First added line";
            TextWriter writer = new StreamWriter(path);
            writer.WriteLine(content);
            writer.Close();

            // When
            LootLockerPlayerFile actualResponse = new LootLockerPlayerFile();
            bool setToPublic = true;
            bool playerFileUploadCompleted = false;
            LootLockerSDKManager.UploadPlayerFile(path, "test", setToPublic, fileResponse =>
            {
                actualResponse = fileResponse;
                playerFileUploadCompleted = true;
            });

            // Wait for response
            yield return new WaitUntil(() => playerFileUploadCompleted);

            // Then
            Assert.IsTrue(actualResponse.success, "File upload failed");
            Assert.Greater(actualResponse.size, 0, "File Size was 0");
            Assert.AreEqual(setToPublic, actualResponse.is_public, "File does not have the same public setting");
        }
    }
}
