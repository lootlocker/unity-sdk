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
        public string GetRandomName()
        {
            return LootLockerTestConfigurationUtilities.GetRandomNoun() +
                   LootLockerTestConfigurationUtilities.GetRandomVerb();

        }

        [UnityTest]
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

        [UnityTest]
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

        [UnityTest]
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

        [UnityTest]
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

    }
}