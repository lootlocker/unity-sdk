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
    public class BanTest
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

            bool gameCreationCallCompleted = false;
            LootLockerTestGame.CreateGame(testName: this.GetType().Name + TestCounter + " ", onComplete: (success, errorMessage, game) =>
            {
                if (!success)
                {
                    SetupFailed = true;
                    gameCreationCallCompleted = true;
                    return;
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
                if (!success) { SetupFailed = true; }
                enableGuestLoginCallCompleted = true;
            });
            yield return new WaitUntil(() => enableGuestLoginCallCompleted);
            if (SetupFailed) { yield break; }

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
        public IEnumerator GetPlayerBanStatus_ForUnbannedPlayer_ReturnsNotBanned()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            LootLockerGuestSessionResponse sessionResponse = null;
            bool sessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                sessionResponse = response;
                sessionCompleted = true;
            });
            yield return new WaitUntil(() => sessionCompleted);
            Assert.IsTrue(sessionResponse.success, "Failed to start guest session for setup");

            // When
            LootLockerBanStatusResponse banStatusResponse = null;
            bool banStatusCompleted = false;
            LootLockerSDKManager.GetPlayerBanStatus(sessionResponse.player_ulid, response =>
            {
                banStatusResponse = response;
                banStatusCompleted = true;
            });
            yield return new WaitUntil(() => banStatusCompleted);

            // Then
            Assert.IsTrue(banStatusResponse.success, "Expected GetPlayerBanStatus to succeed");
            Assert.IsFalse(banStatusResponse.is_banned, "Expected player to not be banned");
            Assert.IsNull(banStatusResponse.ban, "Expected no ban details for an unbanned player");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator StartSession_ForBannedPlayer_Returns403WithBanInfo()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given - start session to obtain the player ULID and identifier
            string playerIdentifier = Guid.NewGuid().ToString();
            LootLockerGuestSessionResponse sessionResponse = null;
            bool sessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(playerIdentifier, response =>
            {
                sessionResponse = response;
                sessionCompleted = true;
            });
            yield return new WaitUntil(() => sessionCompleted);
            Assert.IsTrue(sessionResponse.success, "Failed to start initial guest session");

            string playerUlid = sessionResponse.player_ulid;
            LootLockerStateData.ClearAllSavedStates();

            bool banCompleted = false;
            LootLockerResponse banResponse = null;
            LootLockerTestPlayerBan.BanPlayer(playerUlid, response =>
            {
                banResponse = response;
                banCompleted = true;
            });
            yield return new WaitUntil(() => banCompleted);
            Assert.IsTrue(banResponse?.success, "Failed to ban player via admin API");

            // When - try to start a new session with the banned player's identifier
            LootLockerGuestSessionResponse bannedSessionResponse = null;
            bool bannedSessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(playerIdentifier, response =>
            {
                bannedSessionResponse = response;
                bannedSessionCompleted = true;
            });
            yield return new WaitUntil(() => bannedSessionCompleted);

            // Then
            Assert.IsFalse(bannedSessionResponse.success, "Expected session start to fail for a banned player");
            Assert.AreEqual(403, bannedSessionResponse.statusCode, "Expected 403 status code for a banned player");
            Assert.AreEqual("player_banned", bannedSessionResponse.errorData?.code, "Expected player_banned error code");
            Assert.IsNotNull(bannedSessionResponse.errorData?.ban, "Expected ban info to be present in the error data");
            Assert.IsFalse(string.IsNullOrEmpty(bannedSessionResponse.errorData?.ban?.ban_reason), "Expected ban_reason to be populated");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator GetPlayerBanStatus_ForBannedPlayer_ReturnsIsBannedWithDetails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given - start session to obtain the player ULID, then ban the player
            LootLockerGuestSessionResponse sessionResponse = null;
            bool sessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                sessionResponse = response;
                sessionCompleted = true;
            });
            yield return new WaitUntil(() => sessionCompleted);
            Assert.IsTrue(sessionResponse.success, "Failed to start guest session");

            string playerUlid = sessionResponse.player_ulid;
            LootLockerStateData.ClearAllSavedStates();

            bool banCompleted = false;
            LootLockerResponse banResponse = null;
            LootLockerTestPlayerBan.BanPlayer(playerUlid, response =>
            {
                banResponse = response;
                banCompleted = true;
            });
            yield return new WaitUntil(() => banCompleted);
            Assert.IsTrue(banResponse?.success, "Failed to ban player via admin API");

            // When
            LootLockerBanStatusResponse banStatusResponse = null;
            bool banStatusCompleted = false;
            LootLockerSDKManager.GetPlayerBanStatus(playerUlid, response =>
            {
                banStatusResponse = response;
                banStatusCompleted = true;
            });
            yield return new WaitUntil(() => banStatusCompleted);

            // Then
            Assert.IsTrue(banStatusResponse.success, "Expected GetPlayerBanStatus to succeed");
            Assert.IsTrue(banStatusResponse.is_banned, "Expected player to be banned");
            Assert.IsNotNull(banStatusResponse.ban, "Expected ban details to be populated");
            Assert.IsFalse(string.IsNullOrEmpty(banStatusResponse.ban?.ban_reason), "Expected ban_reason to be populated");
            Assert.IsFalse(string.IsNullOrEmpty(banStatusResponse.ban?.banned_on), "Expected banned_on to be populated");
            Assert.IsTrue(banStatusResponse.ban?.permanent ?? false, "Expected permanent ban (no banned_until)");
        }
    }
}
