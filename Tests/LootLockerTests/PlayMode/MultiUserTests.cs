using System.Collections;
using LootLocker.Requests;
using LootLocker;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


namespace LootLockerTests.PlayMode
{
    //TODO: Add more multi user tests testing all public methods
    public class MultiUserTests
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
            LootLockerTestGame.CreateGame(testName: this.GetType().Name + TestCounter + " ",
                onComplete: (success, errorMessage, game) =>
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

            gameUnderTest?.InitializeLootLockerSDK();

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
                configCopy.logLevel, configCopy.logInBuilds, configCopy.logErrorsAsWarnings,
                configCopy.allowTokenRefresh);
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} tear down #####");
        }

        [UnityTest]
        public IEnumerator MultiUser_MakingRequestsWithoutSpecifyingUser_UsesDefaultUser()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response => { guestLoginCompleted = true; });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            string defaultPlayerUlid = LootLockerStateData.GetDefaultPlayerULID();

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
            Assert.AreEqual(defaultPlayerUlid, pingResponse.requestContext.player_ulid,
                "Default player was not used for request");
            Assert.AreEqual(guestUsersToCreate, LootLockerStateData.GetActivePlayerULIDs().Count,
                "The expected number of local players were not 'active'");
        }

        [UnityTest]
        public IEnumerator MultiUser_MakingRequestsWithSpecifiedUser_UsesSpecifiedUser()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response => { guestLoginCompleted = true; });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            string defaultPlayerUlid = LootLockerStateData.GetDefaultPlayerULID();

            // When
            string usePlayerWithUlid = LootLockerStateData.GetActivePlayerULIDs()[2];
            bool pingRequestCompleted = false;
            LootLockerPingResponse pingResponse = null;
            LootLockerSDKManager.Ping(_pingResponse =>
            {
                pingResponse = _pingResponse;
                pingRequestCompleted = true;
            }, usePlayerWithUlid);
            yield return new WaitUntil(() => pingRequestCompleted);

            // Then
            Assert.IsTrue(pingResponse.success, pingResponse.errorData?.ToString());
            Assert.AreNotEqual(defaultPlayerUlid, pingResponse.requestContext.player_ulid,
                "Default player was used for request despite a player being specified");
            Assert.AreEqual(usePlayerWithUlid, pingResponse.requestContext.player_ulid,
                "The specified player was used");
            Assert.AreEqual(guestUsersToCreate, LootLockerStateData.GetActivePlayerULIDs().Count,
                "The expected number of local players were not 'active'");
        }

        //TODO: Deprecated (or rather temporary) - Remove after 20251001
        [UnityTest]
        public IEnumerator MultiUser_JustMigratedToMultiUserSDK_TransfersStoredUser()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            LootLockerGuestSessionResponse preMultiUserGuestSessionResponse = null;
            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(response =>
            {
                preMultiUserGuestSessionResponse = response;
                guestLoginCompleted = true;
            });
            yield return new WaitUntil(() => guestLoginCompleted);
            Assert.IsNotNull(preMultiUserGuestSessionResponse);

            // Reset State
            LootLockerStateData.ClearAllSavedStates();
            LootLockerStateData.Reset();

#pragma warning disable CS0618 // Set the deprecated properties to mimic pre-multiuser state
            LootLockerConfig.current.deviceID = preMultiUserGuestSessionResponse.player_identifier;
            LootLockerConfig.current.token = preMultiUserGuestSessionResponse.session_token;
            LootLockerConfig.current.refreshToken = null;
            LootLockerConfig.current.playerULID = null;
            PlayerPrefs.SetInt("LastActivePlatform", (int)LL_AuthPlatforms.Guest);
            PlayerPrefs.SetString("LootLockerGuestPlayerID", preMultiUserGuestSessionResponse.player_identifier);
            PlayerPrefs.SetString("LootLockerWhiteLabelSessionEmail", "");
            PlayerPrefs.SetString("LootLockerWhiteLabelSessionToken", "");
#pragma warning restore CS0618 // end of mimic state

#if UNITY_EDITOR
            LootLockerStateData.ResetMultiUserTransferFlag();
#endif
            LootLockerStateData.TransferPlayerCacheToMultiUserSystem();
            yield return new WaitUntil(() => !string.IsNullOrEmpty(LootLockerStateData.GetDefaultPlayerULID()));

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
            Assert.AreEqual(preMultiUserGuestSessionResponse.player_ulid, pingResponse.requestContext.player_ulid,
                "Pre multi-user user was migrated, set as default and used for request");
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(preMultiUserGuestSessionResponse.player_ulid);
            Assert.IsNotNull(playerData);
            Assert.AreEqual(preMultiUserGuestSessionResponse.player_identifier, playerData.Identifier, "Identifier changed in migration");
            Assert.AreEqual(preMultiUserGuestSessionResponse.session_token, playerData.SessionToken, "Token changed in migration");
            Assert.AreEqual(LL_AuthPlatforms.Guest, playerData.CurrentPlatform.Platform, "Platform changed in migration");
            Assert.False(PlayerPrefs.HasKey("LastActivePlatform"), "Key LastActivePlatform was not cleared from player prefs");
            Assert.False(PlayerPrefs.HasKey("LootLockerGuestPlayerID"), "Key LootLockerGuestPlayerID was not cleared from player prefs");
            Assert.False(PlayerPrefs.HasKey("LootLockerWhiteLabelSessionEmail"), "Key LootLockerWhiteLabelSessionEmail was not cleared from player prefs");
            Assert.False(PlayerPrefs.HasKey("LootLockerWhiteLabelSessionToken"), "Key LootLockerWhiteLabelSessionToken was not cleared from player prefs");
        }
    }
}
