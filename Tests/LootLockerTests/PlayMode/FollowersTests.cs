using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class FollowersTests
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

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator Followers_FollowPlayer_AddsToFollowingListAndFollowersList()
        {
            // Given
            string Player1Identifier = "Follower-1";
            string Player1PublicUid = "";
            string Player1Ulid = "";
            string Player2Identifier = "Followee-2";
            string Player2PublicUid = "";
            string Player2Ulid = "";

            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                Player1PublicUid = response?.public_uid;
                Player1Ulid = response?.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1PublicUid, "Guest Session 1 failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                Player2PublicUid = response?.public_uid;
                Player2Ulid = response?.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player2PublicUid, "Guest Session 2 failed");

            // When
            bool followCompleted = false;
            LootLockerFollowersOperationResponse followResponse = null;
            LootLockerSDKManager.FollowPlayer(Player2PublicUid, response =>
            {
                followResponse = response;
                followCompleted = true;
            }, Player1Ulid);
            yield return new WaitUntil(() => followCompleted);
            Assert.IsTrue(followResponse.success, "Follow request failed");

            // Then
            bool listFollowingCompleted = false;
            LootLockerListFollowingResponse followingResponse = null;
            LootLockerSDKManager.ListFollowing(response =>
            {
                followingResponse = response;
                listFollowingCompleted = true;
            }, Player1Ulid);
            yield return new WaitUntil(() => listFollowingCompleted);
            Assert.IsTrue(followingResponse.success, "List following failed");

            bool listFollowersCompleted = false;
            LootLockerListFollowersResponse followersResponse = null;
            LootLockerSDKManager.ListFollowers(response =>
            {
                followersResponse = response;
                listFollowersCompleted = true;
            }, Player2Ulid);
            yield return new WaitUntil(() => listFollowersCompleted);
            Assert.IsTrue(followersResponse.success, "List followers failed");

            Assert.AreEqual(1, followingResponse?.following?.Length, "Following list count incorrect");
            Assert.AreEqual(1, followersResponse?.followers?.Length, "Followers list count incorrect");
            Assert.AreEqual(Player2Ulid, followingResponse?.following?[0]?.player_id, "Player 1 not following player 2");
            Assert.AreEqual(Player1Ulid, followersResponse?.followers?[0]?.player_id, "Player 2 not followed by player 1");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator Followers_UnfollowPlayer_RemovesFromFollowingListAndFollowersList()
        {
            // Given
            string Player1Identifier = "Follower-1";
            string Player1PublicUid = "";
            string Player1Ulid = "";
            string Player2Identifier = "Followee-2";
            string Player2PublicUid = "";
            string Player2Ulid = "";

            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                Player1PublicUid = response?.public_uid;
                Player1Ulid = response?.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1PublicUid, "Guest Session 1 failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                Player2PublicUid = response?.public_uid;
                Player2Ulid = response?.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player2PublicUid, "Guest Session 2 failed");

            bool followCompleted = false;
            LootLockerFollowersOperationResponse followResponse = null;
            LootLockerSDKManager.FollowPlayer(Player2PublicUid, response =>
            {
                followResponse = response;
                followCompleted = true;
            }, Player1Ulid);
            yield return new WaitUntil(() => followCompleted);
            Assert.IsTrue(followResponse.success, "Follow request failed");

            // When
            bool unfollowCompleted = false;
            LootLockerFollowersOperationResponse unfollowResponse = null;
            LootLockerSDKManager.UnfollowPlayer(Player2PublicUid, response =>
            {
                unfollowResponse = response;
                unfollowCompleted = true;
            }, Player1Ulid);
            yield return new WaitUntil(() => unfollowCompleted);
            Assert.IsTrue(unfollowResponse.success, "Unfollow request failed");

            // Then
            bool listFollowingCompleted = false;
            LootLockerListFollowingResponse followingResponse = null;
            LootLockerSDKManager.ListFollowing(response =>
            {
                followingResponse = response;
                listFollowingCompleted = true;
            }, Player1Ulid);
            yield return new WaitUntil(() => listFollowingCompleted);
            Assert.IsTrue(followingResponse.success, "List following failed");

            bool listFollowersCompleted = false;
            LootLockerListFollowersResponse followersResponse = null;
            LootLockerSDKManager.ListFollowers(response =>
            {
                followersResponse = response;
                listFollowersCompleted = true;
            }, Player2Ulid);
            yield return new WaitUntil(() => listFollowersCompleted);
            Assert.IsTrue(followersResponse.success, "List followers failed");

            Assert.IsEmpty(followingResponse?.following, "Following list not empty after unfollow");
            Assert.IsEmpty(followersResponse?.followers, "Followers list not empty after unfollow");
        }
    }
}