using System;

#if LOOTLOCKER_BETA_FRIENDS

//==================================================
// Enum Definitions
//==================================================
namespace LootLocker.LootLockerEnums
{
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    ///</summary>
    public class LootLockerFriend
    {
        /// <summary>
        /// The id of the player
        ///</summary>
        public string player_id { get; set; }
        /// <summary>
        /// The name (if any has been set) of the player
        ///</summary>
        public string name { get; set; }
        /// <summary>
        /// The public uid of the player
        ///</summary>
        public string public_uid { get; set; }
        /// <summary>
        /// When the friend request for this friend was accepted
        ///</summary>
        public DateTime accepted_at { get; set; }
        /// <summary>
        /// When the player's account was created
        ///</summary>
        public DateTime created_at { get; set; }
    }

    /// <summary>
    ///</summary>
    public class LootLockerFriendWithOnlineStatus : LootLockerFriend
    {
        /// <summary>
        /// Whether or not the player is currently online
        ///</summary>
        public bool online { get; set; }
    }

    /// <summary>
    ///</summary>
    public class LootLockerBlockedPlayer
    {
        /// <summary>
        /// The id of the player
        ///</summary>
        public string player_id { get; set; }
        /// <summary>
        /// The name (if any has been set) of the player
        ///</summary>
        public string name { get; set; }
        /// <summary>
        /// The public uid of the player
        ///</summary>
        public string public_uid { get; set; }
        /// <summary>
        /// When this player was blocked
        ///</summary>
        public DateTime blocked_at { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerListFriendsResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of the friends for the currently logged in player
        /// </summary>
        public LootLockerFriendWithOnlineStatus[] friends { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerListIncomingFriendRequestsResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of the incoming friend requests for the currently logged in player
        /// </summary>
        public LootLockerFriend[] incoming { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerListOutgoingFriendRequestsResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of the outgoing friend requests for the currently logged in player
        /// </summary>
        public LootLockerFriend[] outgoing { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerListBlockedPlayersResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of players that the currently logged in player has blocked
        /// </summary>
        public LootLockerBlockedPlayer[] blocked { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerFriendsOperationResponse : LootLockerResponse
    {
        // Empty unless errors occured
    }

}

#endif