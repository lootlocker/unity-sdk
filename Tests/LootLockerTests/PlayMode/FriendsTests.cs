using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class FriendsTests
    {

#if LOOTLOCKER_BETA_FRIENDS
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

        // This test also tests List Friends, List Outgoing requests, List Incoming requests, Send Friend Request, and Accept Friend Request in passing
        [UnityTest]
        public IEnumerator Friends_DeleteFriend_RemovesFriendFromFriendsList()
        {
            // Given
            string Player1Identifier = "Id-1";
            string Player1Ulid = "";
            string Player2Identifier = "Id-2";
            string Player2Ulid = "";

            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                Player1Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 1 failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                Player2Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 2 failed");

            bool friendRequestCompleted = false;
            LootLockerFriendsOperationResponse friendOperationResponse = null;
            LootLockerSDKManager.SendFriendRequest(Player1Ulid, response =>
            {
                friendOperationResponse = response;
                friendRequestCompleted = true;
            });
            yield return new WaitUntil(() => friendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Friend request failed");

            bool listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsResponse.success, "List Outgoing requests failed");
            Assert.AreEqual(outgoingFriendRequestsResponse.outgoing.Length, 1, "Expected exactly 1 outgoing request");
            Assert.AreEqual(outgoingFriendRequestsResponse.outgoing[0].player_id, Player1Ulid, "The ulid of the outgoing request didn't match");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            bool listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsResponse.success, "List Incoming requests failed");
            Assert.AreEqual(incomingFriendRequestsResponse.incoming.Length, 1, "Expected exactly 1 incoming request");
            Assert.AreEqual(incomingFriendRequestsResponse.incoming[0].player_id, Player2Ulid, "The ulid of the incoming request didn't match");

            bool acceptFriendRequestCompleted = false;
            friendOperationResponse = null;
            LootLockerSDKManager.AcceptFriendRequest(Player2Ulid, response =>
            {
                friendOperationResponse = response;
                acceptFriendRequestCompleted = true;
            });
            yield return new WaitUntil(() => acceptFriendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Accepting friend request failed");


            // When
            bool listFriendsCompleted = false;
            LootLockerListFriendsResponse listFriendsPreDeleteResponse = null;
            LootLockerSDKManager.ListFriends(response =>
            {
                listFriendsPreDeleteResponse = response;
                listFriendsCompleted = true;
            });
            yield return new WaitUntil(() => listFriendsCompleted);
            Assert.IsTrue(listFriendsPreDeleteResponse.success, "Listing friends failed");

            bool deleteFriendCompleted = false;
            friendOperationResponse = null;
            LootLockerSDKManager.DeleteFriend(Player2Ulid, response =>
            {
                friendOperationResponse = response;
                deleteFriendCompleted = true;
            });
            yield return new WaitUntil(() => deleteFriendCompleted);

            listFriendsCompleted = false;
            LootLockerListFriendsResponse listFriendsPostDeleteResponse = null;
            LootLockerSDKManager.ListFriends(response =>
            {
                listFriendsPostDeleteResponse = response;
                listFriendsCompleted = true;
            });
            yield return new WaitUntil(() => listFriendsCompleted);

            // Then
            Assert.IsTrue(friendOperationResponse.success, "Deleting friend failed");
            Assert.Greater(listFriendsPreDeleteResponse.friends?.Length, listFriendsPostDeleteResponse.friends?.Length, "Friend was not deleted");
            bool foundFriendUlidPreDelete = false;
            foreach (var player in listFriendsPreDeleteResponse?.friends)
            {
                foundFriendUlidPreDelete |= player.player_id.Equals(Player2Ulid, System.StringComparison.OrdinalIgnoreCase);
            }
            Assert.IsTrue(foundFriendUlidPreDelete, "Friend ulid was not present in friends list pre delete");

            bool foundFriendUlidPostDelete = false;
            foreach (var player in listFriendsPostDeleteResponse?.friends)
            {
                foundFriendUlidPostDelete |= player.player_id.Equals(Player2Ulid, System.StringComparison.OrdinalIgnoreCase);
            }
            Assert.IsFalse(foundFriendUlidPostDelete, "Friend ulid was present in friends list post delete");
        }

        [UnityTest]
        public IEnumerator Friends_DeclineIncomingFriendRequest_DoesNotAddToFriendsListAndRemovesFromIncomingAndOutgoing()
        {
            // Given
            string Player1Identifier = "Id-1";
            string Player1Ulid = "";
            string Player2Identifier = "Id-2";
            string Player2Ulid = "";

            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                Player1Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 1 failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                Player2Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 2 failed");

            bool friendRequestCompleted = false;
            LootLockerFriendsOperationResponse friendOperationResponse = null;
            LootLockerSDKManager.SendFriendRequest(Player1Ulid, response =>
            {
                friendOperationResponse = response;
                friendRequestCompleted = true;
            });
            yield return new WaitUntil(() => friendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Friend request failed");

            bool listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsResponse.success, "List Outgoing requests failed");
            Assert.AreEqual(outgoingFriendRequestsResponse.outgoing.Length, 1, "Expected exactly 1 outgoing request");
            Assert.AreEqual(outgoingFriendRequestsResponse.outgoing[0].player_id, Player1Ulid, "The ulid of the outgoing request didn't match");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            bool listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsResponse.success, "List Incoming requests failed");
            Assert.AreEqual(incomingFriendRequestsResponse.incoming.Length, 1, "Expected exactly 1 incoming request");
            Assert.AreEqual(incomingFriendRequestsResponse.incoming[0].player_id, Player2Ulid, "The ulid of the incoming request didn't match");

            // When
            bool declineFriendRequestCompleted = false;
            friendOperationResponse = null;
            LootLockerSDKManager.DeclineFriendRequest(Player2Ulid, response =>
            {
                friendOperationResponse = response;
                declineFriendRequestCompleted = true;
            });
            yield return new WaitUntil(() => declineFriendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Declining friend request failed");

            bool listFriendsCompleted = false;
            LootLockerListFriendsResponse listFriendsResponse = null;
            LootLockerSDKManager.ListFriends(response =>
            {
                listFriendsResponse = response;
                listFriendsCompleted = true;
            });
            yield return new WaitUntil(() => listFriendsCompleted);

            listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsPostDeclineResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsPostDeclineResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsPostDeclineResponse.success, "List Incoming requests failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsPostDeclineResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsPostDeclineResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsPostDeclineResponse.success, "List Outgoing requests failed");

            // Then
            Assert.Greater(incomingFriendRequestsResponse.incoming?.Length, incomingFriendRequestsPostDeclineResponse.incoming?.Length, "Friend request was not removed when declining");
            Assert.Greater(outgoingFriendRequestsResponse.outgoing?.Length, outgoingFriendRequestsPostDeclineResponse.outgoing?.Length, "Friend request was not removed when declining");

            bool foundFriendUlid = false;
            foreach (var player in listFriendsResponse?.friends)
            {
                foundFriendUlid |= player.player_id.Equals(Player2Ulid, System.StringComparison.OrdinalIgnoreCase);
            }
            Assert.IsFalse(foundFriendUlid, "Friend ulid was present in friends list");
        }

        [UnityTest]
        public IEnumerator Friends_AcceptIncomingFriendRequest_AddsToFriendsListAndRemovesFromIncomingAndOutgoing()
        {
            // Given
            string Player1Identifier = "Id-1";
            string Player1Ulid = "";
            string Player2Identifier = "Id-2";
            string Player2Ulid = "";

            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                Player1Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 1 failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                Player2Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 2 failed");

            bool friendRequestCompleted = false;
            LootLockerFriendsOperationResponse friendOperationResponse = null;
            LootLockerSDKManager.SendFriendRequest(Player1Ulid, response =>
            {
                friendOperationResponse = response;
                friendRequestCompleted = true;
            });
            yield return new WaitUntil(() => friendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Friend request failed");

            bool listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsResponse.success, "List Outgoing requests failed");
            Assert.AreEqual(outgoingFriendRequestsResponse.outgoing.Length, 1, "Expected exactly 1 outgoing request");
            Assert.AreEqual(outgoingFriendRequestsResponse.outgoing[0].player_id, Player1Ulid, "The ulid of the outgoing request didn't match");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            bool listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsResponse.success, "List Incoming requests failed");
            Assert.AreEqual(incomingFriendRequestsResponse.incoming.Length, 1, "Expected exactly 1 incoming request");
            Assert.AreEqual(incomingFriendRequestsResponse.incoming[0].player_id, Player2Ulid, "The ulid of the incoming request didn't match");

            // When
            bool acceptFriendRequestCompleted = false;
            friendOperationResponse = null;
            LootLockerSDKManager.AcceptFriendRequest(Player2Ulid, response =>
            {
                friendOperationResponse = response;
                acceptFriendRequestCompleted = true;
            });
            yield return new WaitUntil(() => acceptFriendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Accepting friend request failed");

            bool listFriendsCompleted = false;
            LootLockerListFriendsResponse listFriendsResponse = null;
            LootLockerSDKManager.ListFriends(response =>
            {
                listFriendsResponse = response;
                listFriendsCompleted = true;
            });
            yield return new WaitUntil(() => listFriendsCompleted);

            listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsPostAcceptResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsPostAcceptResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsPostAcceptResponse.success, "List Incoming requests failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsPostAcceptResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsPostAcceptResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsPostAcceptResponse.success, "List Outgoing requests failed");

            // Then
            Assert.Greater(incomingFriendRequestsResponse.incoming?.Length, incomingFriendRequestsPostAcceptResponse.incoming?.Length, "Friend request was not removed when accepting");
            Assert.Greater(outgoingFriendRequestsResponse.outgoing?.Length, outgoingFriendRequestsPostAcceptResponse.outgoing?.Length, "Friend request was not removed when accepting");

            bool foundFriendUlid = false;
            foreach (var player in listFriendsResponse?.friends)
            {
                foundFriendUlid |= player.player_id.Equals(Player2Ulid, System.StringComparison.OrdinalIgnoreCase);
            }
            Assert.IsTrue(foundFriendUlid, "Friend ulid was not present in friends list");
            yield break;
        }

        [UnityTest]
        public IEnumerator Friends_CancelOutgoingFriendRequest_RemovesFriendRequestFromIncomingAndOutgoingRequest()
        {
            // Given
            string Player1Identifier = "Id-1";
            string Player1Ulid = "";
            string Player2Identifier = "Id-2";
            string Player2Ulid = "";

            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                Player1Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 1 failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                Player2Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 2 failed");

            bool friendRequestCompleted = false;
            LootLockerFriendsOperationResponse friendOperationResponse = null;
            LootLockerSDKManager.SendFriendRequest(Player1Ulid, response =>
            {
                friendOperationResponse = response;
                friendRequestCompleted = true;
            });
            yield return new WaitUntil(() => friendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Friend request failed");

            bool listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsResponse.success, "List Outgoing requests failed");
            Assert.AreEqual(outgoingFriendRequestsResponse.outgoing.Length, 1, "Expected exactly 1 outgoing request");
            Assert.AreEqual(outgoingFriendRequestsResponse.outgoing[0].player_id, Player1Ulid, "The ulid of the outgoing request didn't match");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            bool listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsResponse.success, "List Incoming requests failed");
            Assert.AreEqual(incomingFriendRequestsResponse.incoming.Length, 1, "Expected exactly 1 incoming request");
            Assert.AreEqual(incomingFriendRequestsResponse.incoming[0].player_id, Player2Ulid, "The ulid of the incoming request didn't match");

            // When
            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            bool cancelFriendRequestCompleted = false;
            friendOperationResponse = null;
            LootLockerSDKManager.CancelFriendRequest(Player1Ulid, response =>
            {
                friendOperationResponse = response;
                cancelFriendRequestCompleted = true;
            });
            yield return new WaitUntil(() => cancelFriendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Cancelling friend request failed");

            listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsPostCancelResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsPostCancelResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsPostCancelResponse.success, "List Outgoing requests failed");

            bool listFriendsCompleted = false;
            LootLockerListFriendsResponse listFriendsResponse = null;
            LootLockerSDKManager.ListFriends(response =>
            {
                listFriendsResponse = response;
                listFriendsCompleted = true;
            });
            yield return new WaitUntil(() => listFriendsCompleted);

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsPostCancelResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsPostCancelResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsPostCancelResponse.success, "List Incoming requests failed");

            // Then
            Assert.Greater(incomingFriendRequestsResponse.incoming?.Length, incomingFriendRequestsPostCancelResponse.incoming?.Length, "Friend request was not removed when cancelling");
            Assert.Greater(outgoingFriendRequestsResponse.outgoing?.Length, outgoingFriendRequestsPostCancelResponse.outgoing?.Length, "Friend request was not removed when cancelling");

            bool foundFriendUlid = false;
            foreach (var player in listFriendsResponse?.friends)
            {
                foundFriendUlid |= player.player_id.Equals(Player2Ulid, System.StringComparison.OrdinalIgnoreCase);
            }
            Assert.IsFalse(foundFriendUlid, "Friend ulid was present in friends list");
            yield break;
        }

        [UnityTest]
        public IEnumerator Friends_BlockPlayerWhenHavingIncomingFriendRequestThenUnblockAndReceiveNewFriendRequest_RemovesFriendRequestFromIncomingWhenBlockingAndUnblockingAllowsFriendRequestsAgain()
        {
            // Given
            string Player1Identifier = "Id-1";
            string Player1Ulid = "";
            string Player2Identifier = "Id-2";
            string Player2Ulid = "";

            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                Player1Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 1 failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                Player2Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 2 failed");

            bool friendRequestCompleted = false;
            LootLockerFriendsOperationResponse friendOperationResponse = null;
            LootLockerSDKManager.SendFriendRequest(Player1Ulid, response =>
            {
                friendOperationResponse = response;
                friendRequestCompleted = true;
            });
            yield return new WaitUntil(() => friendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Friend request failed");

            bool listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsResponse.success, "List Outgoing requests failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            bool listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsResponse.success, "List Incoming requests failed");

            // When
            bool blockPlayerCompleted = false;
            friendOperationResponse = null;
            LootLockerSDKManager.BlockPlayer(Player2Ulid, response =>
            {
                friendOperationResponse = response;
                blockPlayerCompleted = true;
            });
            yield return new WaitUntil(() => blockPlayerCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Block player request failed");

            listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsPostBlockResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsPostBlockResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsPostBlockResponse.success, "List Incoming requests failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsPostBlockResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsPostBlockResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsPostBlockResponse.success, "List Outgoing requests failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            bool unblockPlayerCompleted = false;
            LootLockerFriendsOperationResponse friendUnblockOperationResponse = null;
            LootLockerSDKManager.UnblockPlayer(Player2Ulid, response =>
            {
                friendUnblockOperationResponse = response;
                unblockPlayerCompleted = true;
            });
            yield return new WaitUntil(() => unblockPlayerCompleted);
            Assert.IsTrue(friendUnblockOperationResponse.success, "Block player request failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            bool friendRequestPostUnblockCompleted = false;
            LootLockerFriendsOperationResponse friendOperationPostUnblockResponse = null;
            LootLockerSDKManager.SendFriendRequest(Player1Ulid, response =>
            {
                friendOperationPostUnblockResponse = response;
                friendRequestPostUnblockCompleted = true;
            });
            yield return new WaitUntil(() => friendRequestPostUnblockCompleted);
            Assert.IsTrue(friendOperationPostUnblockResponse.success, "Friend request failed");

            listOutgoingRequestsCompleted = false;
            LootLockerListOutgoingFriendRequestsResponse outgoingFriendRequestsPostUnblockResponse = null;
            LootLockerSDKManager.ListOutgoingFriendRequests(response =>
            {
                outgoingFriendRequestsPostUnblockResponse = response;
                listOutgoingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listOutgoingRequestsCompleted);
            Assert.IsTrue(outgoingFriendRequestsPostUnblockResponse.success, "List Outgoing requests failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            listIncomingRequestsCompleted = false;
            LootLockerListIncomingFriendRequestsResponse incomingFriendRequestsPostUnblockResponse = null;
            LootLockerSDKManager.ListIncomingFriendRequests(response =>
            {
                incomingFriendRequestsPostUnblockResponse = response;
                listIncomingRequestsCompleted = true;
            });
            yield return new WaitUntil(() => listIncomingRequestsCompleted);
            Assert.IsTrue(incomingFriendRequestsPostUnblockResponse.success, "List Incoming requests failed");

            // Then
            Assert.Greater(incomingFriendRequestsResponse.incoming.Length, incomingFriendRequestsPostBlockResponse.incoming.Length, "Friend request was not removed from incoming when blocking");
            Assert.Greater(outgoingFriendRequestsResponse.outgoing.Length, outgoingFriendRequestsPostBlockResponse.outgoing.Length, "Friend request was not removed from outgoing when blocking");
            Assert.Greater(incomingFriendRequestsPostUnblockResponse.incoming.Length, incomingFriendRequestsPostBlockResponse.incoming.Length, "Friend request was not added to incoming when unblocking");
            Assert.Greater(outgoingFriendRequestsPostUnblockResponse.outgoing.Length, outgoingFriendRequestsPostBlockResponse.outgoing.Length, "Friend request was not added to outgoing when unblocking");
        }

        [UnityTest]
        public IEnumerator Friends_BlockPlayer_RemovesBlockedPlayerFromFriendsList()
        {
            // Given
            string Player1Identifier = "Id-1";
            string Player1Ulid = "";
            string Player2Identifier = "Id-2";
            string Player2Ulid = "";

            bool signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                Player1Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 1 failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                Player2Ulid = response.player_ulid;
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);
            Assert.IsNotEmpty(Player1Ulid, "Guest Session 2 failed");

            bool friendRequestCompleted = false;
            LootLockerFriendsOperationResponse friendOperationResponse = null;
            LootLockerSDKManager.SendFriendRequest(Player1Ulid, response =>
            {
                friendOperationResponse = response;
                friendRequestCompleted = true;
            });
            yield return new WaitUntil(() => friendRequestCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Friend request failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player1Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            bool acceptFriendRequestCompleted = false;
            friendOperationResponse = null;
            LootLockerSDKManager.AcceptFriendRequest(Player2Ulid, response =>
            {
                friendOperationResponse = response;
                acceptFriendRequestCompleted = true;
            });
            yield return new WaitUntil(() => acceptFriendRequestCompleted);

            bool listFriendsCompleted = false;
            LootLockerListFriendsResponse listFriendsPreBlockPlayer1 = null;
            LootLockerSDKManager.ListFriends(response =>
            {
                listFriendsPreBlockPlayer1 = response;
                listFriendsCompleted = true;
            });
            yield return new WaitUntil(() => listFriendsCompleted);
            Assert.IsTrue(listFriendsPreBlockPlayer1.success, "Listing friends failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            listFriendsCompleted = false;
            LootLockerListFriendsResponse listFriendsPreBlockPlayer2 = null;
            LootLockerSDKManager.ListFriends(response =>
            {
                listFriendsPreBlockPlayer2 = response;
                listFriendsCompleted = true;
            });
            yield return new WaitUntil(() => listFriendsCompleted);
            Assert.IsTrue(listFriendsPreBlockPlayer2.success, "Listing friends failed");

            // When
            bool blockPlayerCompleted = false;
            friendOperationResponse = null;
            LootLockerSDKManager.BlockPlayer(Player1Ulid, response =>
            {
                friendOperationResponse = response;
                blockPlayerCompleted = true;
            });
            yield return new WaitUntil(() => blockPlayerCompleted);
            Assert.IsTrue(friendOperationResponse.success, "Block player request failed");

            listFriendsCompleted = false;
            LootLockerListFriendsResponse listFriendsPostBlockPlayer1 = null;
            LootLockerSDKManager.ListFriends(response =>
            {
                listFriendsPostBlockPlayer1 = response;
                listFriendsCompleted = true;
            });
            yield return new WaitUntil(() => listFriendsCompleted);
            Assert.IsTrue(listFriendsPostBlockPlayer1.success, "Listing friends failed");

            signInCompleted = false;
            LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
            {
                signInCompleted = true;
            });
            yield return new WaitUntil(() => signInCompleted);

            listFriendsCompleted = false;
            LootLockerListFriendsResponse listFriendsPostBlockPlayer2 = null;
            LootLockerSDKManager.ListFriends(response =>
            {
                listFriendsPostBlockPlayer2 = response;
                listFriendsCompleted = true;
            });
            yield return new WaitUntil(() => listFriendsCompleted);
            Assert.IsTrue(listFriendsPostBlockPlayer2.success, "Listing friends failed");

            // Then
            bool foundFriendUlidPreBlockPlayer1 = false;
            foreach (var player in listFriendsPreBlockPlayer1?.friends)
            {
                foundFriendUlidPreBlockPlayer1 |= player.player_id.Equals(Player2Ulid, System.StringComparison.OrdinalIgnoreCase);
            }
            Assert.IsTrue(foundFriendUlidPreBlockPlayer1, "Friend ulid was not present in friends list pre block");

            bool foundFriendUlidPreBlockPlayer2 = false;
            foreach (var player in listFriendsPreBlockPlayer2?.friends)
            {
                foundFriendUlidPreBlockPlayer2 |= player.player_id.Equals(Player1Ulid, System.StringComparison.OrdinalIgnoreCase);
            }
            Assert.IsTrue(foundFriendUlidPreBlockPlayer2, "Friend ulid was not present in friends list pre block");

            bool foundFriendUlidPostBlockPlayer1 = false;
            foreach (var player in listFriendsPostBlockPlayer1?.friends)
            {
                foundFriendUlidPostBlockPlayer1 |= player.player_id.Equals(Player2Ulid, System.StringComparison.OrdinalIgnoreCase);
            }
            Assert.IsFalse(foundFriendUlidPostBlockPlayer1, "Friend ulid was present in friends list pre block");

            bool foundFriendUlidPostBlockPlayer2 = false;
            foreach (var player in listFriendsPostBlockPlayer2?.friends)
            {
                foundFriendUlidPostBlockPlayer2 |= player.player_id.Equals(Player1Ulid, System.StringComparison.OrdinalIgnoreCase);
            }
            Assert.IsFalse(foundFriendUlidPostBlockPlayer2, "Friend ulid was present in friends list pre block");
        }
#endif
    }
}