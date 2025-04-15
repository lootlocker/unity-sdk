using System;
using System.Collections;
using System.Text.RegularExpressions;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class GuestSessionTest
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


        [UnityTest]
        public IEnumerator StartGuestSession_WithPlayerAsIdentifier_Fails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            string pattern = @"Each player must have a unique identifier assigned";

            Regex regex = new Regex(pattern);

            LogAssert.Expect(LogType.Error, regex);

            //Given
            string playerAsIdentifier = "player";

            //When
            LootLockerGuestSessionResponse actualResponse = null;
            bool guestSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(playerAsIdentifier, (response) =>
            {
                actualResponse = response;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);

            //Then
            Assert.IsFalse(actualResponse.success, "Guest Session with 'player' as Identifier started despite it being disallowed");
        }

        [UnityTest]
        public IEnumerator StartGuestSession_WithoutIdentifier_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            //When
            bool guestSessionCompleted = false;
            LootLockerGuestSessionResponse actualResponse = null;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                actualResponse = response;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Guest Session without Identifier failed to start");
            Assert.IsFalse(string.IsNullOrEmpty(actualResponse.player_identifier), "No player_identifier found in response");
        }

        [UnityTest]
        public IEnumerator StartGuestSession_WithProvidedIdentifier_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string providedIdentifier = Guid.NewGuid().ToString();

            //When
            LootLockerGuestSessionResponse actualResponse = null;
            bool guestSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(providedIdentifier, (response) =>
            {

                actualResponse = response;
                guestSessionCompleted = true;

            });
            yield return new WaitUntil(() => guestSessionCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Guest Session with random Identifier failed to start");
            Assert.AreEqual(providedIdentifier, actualResponse.player_identifier, "response player_identifier did not match the expected identifier");
        }

        [UnityTest]
        public IEnumerator EndGuestSession_Succeeds()
        {
            //Given
            bool startGuestSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                startGuestSessionCompleted = true;
            });
            yield return new WaitUntil(() => startGuestSessionCompleted);

            LootLockerSessionResponse actualResponse = null;

            //When
            bool endGuestSessionCompleted = false;
            LootLockerSDKManager.EndSession((response) =>
            {
                actualResponse = response;
                endGuestSessionCompleted = true;
            });
            yield return new WaitUntil(() => endGuestSessionCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "GuestSession was not ended correctly");
        }

        [UnityTest]
        public IEnumerator StartGuestSession_WithStoredIdentifier_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            LootLockerGuestSessionResponse actualResponse = null;
            string expectedIdentifier = null;
            bool firstSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession((startSessionResponse) =>
            {
                expectedIdentifier = startSessionResponse.player_identifier;

                LootLockerSDKManager.EndSession((endSessionResponse) =>
                {
                    firstSessionCompleted = true;
                });
            });
            yield return new WaitUntil(() => firstSessionCompleted);

            //When
            bool secondSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                actualResponse = response;
                secondSessionCompleted = true;
            });
            yield return new WaitUntil(() => secondSessionCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Guest Session failed to start");
            Assert.AreEqual(expectedIdentifier, actualResponse.player_identifier, "Guest Session using stored Identifier failed to work");
        }

        [UnityTest]
        public IEnumerator StartGuestSession_MultipleSessionStartsWithoutIdentifierWithDefaultPlayerActive_CreatesMultipleUsers()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            //When
            bool guestSessionCompleted = false;
            string player1Ulid = null;
            string player2Ulid = null;
            string player3Ulid = null;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                player1Ulid = response?.player_ulid;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;

            LootLockerSDKManager.StartGuestSession((response) =>
            {
                player2Ulid = response?.player_ulid;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;

            LootLockerSDKManager.StartGuestSession((response) =>
            {
                player3Ulid = response?.player_ulid;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;

            //Then
            Assert.IsNotNull(player1Ulid);
            Assert.IsNotNull(player2Ulid);
            Assert.IsNotNull(player3Ulid);
            Assert.AreNotEqual(player1Ulid, player2Ulid, "Same user created with multiple start guest session requests");
            Assert.AreNotEqual(player2Ulid, player3Ulid, "Same user created with multiple start guest session requests");
        }

        [UnityTest]
        public IEnumerator StartGuestSession_MultipleSessionStartsWithoutIdentifierWithDefaultPlayerNotActive_ReusesDefaultUser()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            //When
            bool guestSessionCompleted = false;
            string player1Ulid = null;
            string player2Ulid = null;
            string player3Ulid = null;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                player1Ulid = response?.player_ulid;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;
            LootLockerStateData.Reset();

            LootLockerSDKManager.StartGuestSession((response) =>
            {
                player2Ulid = response?.player_ulid;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;
            LootLockerStateData.Reset();

            LootLockerSDKManager.StartGuestSession((response) =>
            {
                player3Ulid = response?.player_ulid;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;
            LootLockerStateData.Reset();

            //Then
            Assert.IsNotNull(player1Ulid);
            Assert.IsNotNull(player2Ulid);
            Assert.IsNotNull(player3Ulid);
            Assert.AreEqual(player1Ulid, player2Ulid, "Default user not re-used with session request 2");
            Assert.AreEqual(player1Ulid, player3Ulid, "Default user not re-used with session request 3");
        }

        [UnityTest]
        public IEnumerator StartGuestSession_MultipleSessionStartsWithUlid_ReusesSpecifiedUser()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            //Given
            bool guestSessionCompleted = false;
            string player1Ulid = null;
            string player1InitialSessionToken = null;
            string player2Ulid = null;
            string player2InitialSessionToken = null;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                player1Ulid = response?.player_ulid;
                player1InitialSessionToken = response?.session_token;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;

            LootLockerSDKManager.StartGuestSession((response) =>
            {
                player2Ulid = response?.player_ulid;
                player2InitialSessionToken = response?.session_token;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;

            // When
            string player1Ulid2 = null;
            string player1SubsequentSessionToken = null;
            string player2Ulid2 = null;
            string player2SubsequentSessionToken = null;
            LootLockerSDKManager.StartGuestSessionForPlayer(player1Ulid, (response) =>
            {
                player1Ulid2 = response?.player_ulid;
                player1SubsequentSessionToken = response?.session_token;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;

            LootLockerSDKManager.StartGuestSessionForPlayer(player2Ulid, (response) =>
            {
                player2Ulid2 = response?.player_ulid;
                player2SubsequentSessionToken = response?.session_token;
                guestSessionCompleted = true;
            });
            yield return new WaitUntil(() => guestSessionCompleted);
            guestSessionCompleted = false;

            //Then
            Assert.IsNotNull(player1Ulid);
            Assert.IsNotNull(player2Ulid);
            Assert.IsNotNull(player1Ulid2);
            Assert.IsNotNull(player2Ulid2);
            Assert.IsNotNull(player1InitialSessionToken);
            Assert.IsNotNull(player1SubsequentSessionToken);
            Assert.IsNotNull(player2InitialSessionToken);
            Assert.IsNotNull(player2SubsequentSessionToken);
            Assert.AreEqual(player1Ulid, player1Ulid2, "Player 1 was not re-used after new session start");
            Assert.AreNotEqual(player1InitialSessionToken, player1SubsequentSessionToken, "New session wasn't started (old one was re-used)");
            Assert.AreEqual(player2Ulid, player2Ulid2, "Player 2 was not re-used after new session start");
            Assert.AreNotEqual(player2InitialSessionToken, player2SubsequentSessionToken, "New session wasn't started (old one was re-used)");
        }

    }
}