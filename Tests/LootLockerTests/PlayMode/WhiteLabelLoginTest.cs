using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

namespace LootLockerTests.PlayMode
{
    public class WhiteLabelLoginTest
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

            // Enable Whitelabel platform
            bool enableWLLogin = false;
            gameUnderTest?.EnableWhiteLabelLogin((success, errorMessage) =>
            {
                SetupFailed = !success;
                enableWLLogin = true;
            });
            yield return new WaitUntil(() => enableWLLogin);
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

            LootLockerConfig.CreateNewSettings(configCopy);
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} tear down #####");
        }
        public string GetRandomName()
        {
            return LootLockerTestConfigurationUtilities.GetRandomNoun() +
                   LootLockerTestConfigurationUtilities.GetRandomVerb();

        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator WhiteLabel_SignUp_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string email = GetRandomName() + "@lootlocker.com";

            //When
            LootLockerWhiteLabelSignupResponse actualResponse = null;
            bool whiteLabelSignUpCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(email, "123456789", (response) =>
            {

                actualResponse = response;
                whiteLabelSignUpCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelSignUpCompleted);

            //Then
            Assert.IsTrue(actualResponse.success, "Could not sign up with WhiteLabel");
            Assert.IsNotEmpty(actualResponse.CreatedAt, "Created At date is empty in the response");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator WhiteLabel_SignUpAndLogin_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string email = GetRandomName() + "@lootlocker.com";

            LootLockerWhiteLabelSignupResponse signupResponse = null;
            bool whiteLabelSignUpCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(email, "123456789", (response) =>
            {

                signupResponse = response;
                whiteLabelSignUpCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelSignUpCompleted);

            Assert.IsTrue(signupResponse.success, "Could not sign up with WhiteLabel");
            Assert.IsNotEmpty(signupResponse.CreatedAt, "Created At date is empty in the response");

            //When
            LootLockerWhiteLabelLoginAndStartSessionResponse actualResponse = null;
            bool whiteLabelLoginCompleted = false;
            LootLockerSDKManager.WhiteLabelLoginAndStartSession(email, "123456789", true, (response) =>
            {

                actualResponse = response;
                whiteLabelLoginCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelLoginCompleted);

            //Then
            Assert.IsTrue(actualResponse.SessionResponse.success, "Could not start White Label Session");
            Assert.IsNotEmpty(actualResponse.LoginResponse.SessionToken, "No session token found from login");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator WhiteLabel_SignUpAndLoginWithWrongPassword_Fails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            string pattern = @"wrong email/password";

            Regex regex = new Regex(pattern);

            LogAssert.Expect(LogType.Error, regex);

            //Given
            string email = GetRandomName() + "@lootlocker.com";

            LootLockerWhiteLabelSignupResponse signupResponse = null;
            bool whiteLabelSignUpCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(email, "123456789", (response) =>
            {
                signupResponse = response;
                whiteLabelSignUpCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelSignUpCompleted);

            Assert.IsTrue(signupResponse.success, "Could not sign up with WhiteLabel");
            Assert.IsNotEmpty(signupResponse.CreatedAt, "Created At date is empty in the response");

            //When
            LootLockerWhiteLabelLoginAndStartSessionResponse actualResponse = null;
            bool whiteLabelLoginCompleted = false;

            LootLockerSDKManager.WhiteLabelLoginAndStartSession(email, "wrongPassword", false, (response) =>
            {
                actualResponse = response;
                whiteLabelLoginCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelLoginCompleted);

            //Then
            Assert.IsFalse(actualResponse.success, "Started White Label Session with wrong password");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator WhiteLabel_VerifySession_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string email = GetRandomName() + "@lootlocker.com";

            LootLockerWhiteLabelSignupResponse signupResponse = null;
            bool whiteLabelSignUpCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(email, "123456789", (response) =>
            {

                signupResponse = response;
                whiteLabelSignUpCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelSignUpCompleted);

            Assert.IsTrue(signupResponse.success, "Could not sign up with WhiteLabel");
            Assert.IsNotEmpty(signupResponse.CreatedAt, "Created At date is empty in the response");

            LootLockerWhiteLabelLoginAndStartSessionResponse loginResponse = null;
            bool whiteLabelLoginCompleted = false;
            LootLockerSDKManager.WhiteLabelLoginAndStartSession(email, "123456789", true, (response) =>
            {

                loginResponse = response;
                whiteLabelLoginCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelLoginCompleted);

            Assert.IsTrue(loginResponse.SessionResponse.success, "Could not start White Label Session");
            Assert.IsNotEmpty(loginResponse.LoginResponse.SessionToken, "No session token fround from login");

            //When
            bool actualResponse = false;
            bool whiteLabelRequestVerificationCompleted = false;
            LootLockerSDKManager.CheckWhiteLabelSession((response) =>
            {
                actualResponse = response;
                whiteLabelRequestVerificationCompleted = true;

            });
            yield return new WaitUntil(() => whiteLabelRequestVerificationCompleted);

            //Then
            Assert.IsTrue(actualResponse, "Could not Verify Session");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator WhiteLabel_RequestsAfterGameResetWhenWLDefaultUser_ReusesSession()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string email = GetRandomName() + "@lootlocker.com";
            string expectedPlayerUlid = null;

            LootLockerWhiteLabelSignupResponse signupResponse = null;
            bool whiteLabelSignUpCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(email, "123456789", (response) =>
            {

                signupResponse = response;
                whiteLabelSignUpCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelSignUpCompleted);

            Assert.IsTrue(signupResponse.success, "Could not sign up with WhiteLabel");
            Assert.IsNotEmpty(signupResponse.CreatedAt, "Created At date is empty in the response");

            LootLockerWhiteLabelLoginAndStartSessionResponse loginResponse = null;
            bool whiteLabelLoginCompleted = false;
            LootLockerSDKManager.WhiteLabelLoginAndStartSession(email, "123456789", true, (response) =>
            {
                loginResponse = response;
                expectedPlayerUlid = response?.SessionResponse?.player_ulid;
                whiteLabelLoginCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelLoginCompleted);

            Assert.IsTrue(loginResponse.SessionResponse.success, "Could not start White Label Session");
            Assert.IsNotEmpty(loginResponse.LoginResponse.SessionToken, "No session token found from login");

            //When
            LootLockerStateData.UnloadState();

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
            Assert.AreEqual(expectedPlayerUlid, pingResponse.requestContext.player_ulid, "WL user not used for request");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator WhiteLabel_WLSessionStartByEmailAfterGameReset_ReusesSession()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            //Given
            string email = GetRandomName() + "@lootlocker.com";
            string initialPlayerUlid = null;
            string initialPlayerSessionToken = null;
            string initialPlayerWLToken = null;

            LootLockerWhiteLabelSignupResponse signupResponse = null;
            bool whiteLabelSignUpCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(email, "123456789", (response) =>
            {

                signupResponse = response;
                whiteLabelSignUpCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelSignUpCompleted);

            Assert.IsTrue(signupResponse.success, "Could not sign up with WhiteLabel");
            Assert.IsNotEmpty(signupResponse.CreatedAt, "Created At date is empty in the response");

            LootLockerWhiteLabelLoginAndStartSessionResponse loginResponse = null;
            bool whiteLabelLoginCompleted = false;
            LootLockerSDKManager.WhiteLabelLoginAndStartSession(email, "123456789", true, (response) =>
            {
                loginResponse = response;
                initialPlayerUlid = response?.SessionResponse?.player_ulid;
                initialPlayerSessionToken = response?.SessionResponse?.session_token;
                initialPlayerWLToken = response?.LoginResponse.SessionToken;
                whiteLabelLoginCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelLoginCompleted);

            Assert.IsTrue(loginResponse.SessionResponse.success, "Could not start White Label Session");
            Assert.IsNotEmpty(loginResponse.SessionResponse.session_token, "No session token found from login");

            //When
            LootLockerStateData.UnloadState();

            bool postResetSessionRequestCompleted = false;
            LootLockerSessionResponse postResetSessionResponse = null;
            LootLockerSDKManager.StartWhiteLabelSession(email, (sessionResponse) =>
            {
                postResetSessionResponse = sessionResponse;
                postResetSessionRequestCompleted = true;
            });

            yield return new WaitUntil(() => postResetSessionRequestCompleted);

            // Then
            Assert.IsTrue(postResetSessionResponse.success, postResetSessionResponse.errorData?.ToString());
            Assert.AreEqual(initialPlayerUlid, postResetSessionResponse.player_ulid, "Session started for another user");
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(initialPlayerUlid);
            Assert.IsNotNull(playerData);
            Assert.AreEqual(initialPlayerWLToken, playerData.WhiteLabelToken, "White label token was not re-used");
            Assert.AreNotEqual(initialPlayerSessionToken, playerData.SessionToken, "New session token was not generated");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator WhiteLabel_WLLoginAndStartSessionWhenOtherPlayerExists_CreatesWhiteLabelPlayer()
        {
            //Given
            string otherEmail = GetRandomName() + "@lootlocker.com";
            string email = GetRandomName() + "@lootlocker.com";

            Assert.AreNotEqual(otherEmail, email, "Emails were randomized to the same value, test will not work");

            LootLockerWhiteLabelSignupResponse signupResponse = null;
            bool whiteLabelSignUpCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(otherEmail, "123456789", (response) =>
            {

                signupResponse = response;
                whiteLabelSignUpCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelSignUpCompleted);
            Assert.IsTrue(signupResponse.success, "Could not sign up other player with WhiteLabel");

            LootLockerWhiteLabelLoginAndStartSessionResponse loginResponse = null;
            bool whiteLabelLoginCompleted = false;
            LootLockerSDKManager.WhiteLabelLoginAndStartSession(otherEmail, "123456789", true, (response) =>
            {
                loginResponse = response;
                whiteLabelLoginCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelLoginCompleted);

            Assert.IsTrue(loginResponse.SessionResponse.success, "Could not start other player White Label Session");
            Assert.IsNotEmpty(loginResponse.SessionResponse.session_token, "No session token found from login");
            string otherPlayerUlid = loginResponse?.SessionResponse?.player_ulid;
            string otherPlayerSessionToken = loginResponse?.SessionResponse?.session_token;
            string otherPlayerWLEmail = loginResponse?.LoginResponse?.Email;

            signupResponse = null;
            whiteLabelSignUpCompleted = false;
            LootLockerSDKManager.WhiteLabelSignUp(email, "123456789", (response) =>
            {

                signupResponse = response;
                whiteLabelSignUpCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelSignUpCompleted);

            Assert.IsTrue(signupResponse.success, "Could not sign up with WhiteLabel");

            //When
            loginResponse = null;
            whiteLabelLoginCompleted = false;
            LootLockerSDKManager.WhiteLabelLoginAndStartSession(email, "123456789", true, (response) =>
            {
                loginResponse = response;
                whiteLabelLoginCompleted = true;
            });
            yield return new WaitUntil(() => whiteLabelLoginCompleted);

            // Then
            Assert.IsTrue(loginResponse.SessionResponse.success, "Could not start White Label Session");
            Assert.IsNotEmpty(loginResponse.SessionResponse.session_token, "No session token found from login");
            Assert.AreNotEqual(otherPlayerUlid, loginResponse?.SessionResponse?.player_ulid, "WL login did not create new player");
            Assert.AreNotEqual(otherPlayerSessionToken, loginResponse?.SessionResponse?.session_token, "WL login did not create new session");
            Assert.AreNotEqual(otherPlayerWLEmail, loginResponse?.LoginResponse?.Email, "WL login did not create new WL user");
            Assert.AreEqual(email, loginResponse?.LoginResponse?.Email, "WL login email does not match");
            Assert.AreEqual(otherPlayerUlid, LootLockerSDKManager.GetDefaultPlayerUlid(), "Default player ULID was changed by WL login");
        }

    }
}