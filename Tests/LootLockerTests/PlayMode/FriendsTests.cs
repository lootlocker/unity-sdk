using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
        LootLockerTestGame.CreateGame(testName: "FriendsTest" + TestCounter + " ", onComplete: (success, errorMessage, game) =>
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
            configCopy.currentDebugLevel, configCopy.allowTokenRefresh);
    }

    // This test also tests List Friends, Send Friend Request, and Accept Friend Request in passing
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
            Player1Ulid = response.public_uid;
            signInCompleted = true;
        });
        yield return new WaitUntil(() => signInCompleted);
        Assert.IsNotEmpty(Player1Ulid, "Guest Session 1 failed");

        signInCompleted = false;
        LootLockerSDKManager.StartGuestSession(Player2Identifier, response =>
        {
            Player2Ulid = response.public_uid;
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
        foreach(var player in listFriendsPreDeleteResponse?.friends)
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
    public IEnumerator Friends_ListIncomingRequests_Succeeds()
    {
        // Given

        // When

        // Then
        yield break;
    }

    [UnityTest]
    public IEnumerator Friends_ListOutgoingRequests_Succeeds()
    {
        // Given

        // When

        // Then
        yield break;
    }

    [UnityTest]
    public IEnumerator Friends_AcceptIncomingFriendRequest_AddsToFriendsListAndRemovesFromIncoming()
    {
        // Given

        // When

        // Then
        yield break;
    }

    [UnityTest]
    public IEnumerator Friends_DeclineIncomingFriendRequest_DoesNotAddToFriendsListAndRemovesFromIncoming()
    {
        // Given

        // When

        // Then
        yield break;
    }

    [UnityTest]
    public IEnumerator Friends_CancelOutgoingFriendRequest_RemovesFriendRequestFromIncomingAndOutgoingRequest()
    {
        // Given

        // When

        // Then
        yield break;
    }

    [UnityTest]
    public IEnumerator Friends_BlockPlayerWhenHavingIncomingFriendRequest_RemovesFriendRequestFromIncoming()
    {
        // Given

        // When

        // Then
        yield break;
    }

    [UnityTest]
    public IEnumerator Friends_BlockPlayerAndReceiveFriendRequest_BlockedPlayersFriendRequestIsNotListedInIncoming()
    {
        // Given

        // When

        // Then
        yield break;
    }

    [UnityTest]
    public IEnumerator Friends_UnblockPlayerAndReceiveFriendRequest_UnblockedPlayersFriendRequestIsListedInIncoming()
    {
        // Given

        // When

        // Then
        yield break;
    }
#endif
}
