using System;
using LootLocker.Requests;

namespace LootLocker
{
    public class LootLockerPlayerData
    {
        /// <summary>
        /// The session token stored for this player session
        /// </summary>
        public string SessionToken { get; set; }
        /// <summary>
        /// The Token to use when refreshing the session (only present for authentication methods that support it)
        /// </summary>
        public string RefreshToken { get; set; }
        /// <summary>
        /// The ULID for this player
        /// </summary>
        public string ULID { get; set; }
        /// <summary>
        /// The unique player identifier for this account
        /// </summary>
        public string Identifier { get; set; }
        /// <summary>
        /// The public UID for this player
        /// </summary>
        public string PublicUID { get; set; }
        /// <summary>
        /// The integer ID (legacy) for this player
        /// </summary>
        public int LegacyID { get; set; }
        /// <summary>
        /// The name of the player if any has been set
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// If this player is authorized using White Label, then this stores the email that was used
        /// </summary>
        public string WhiteLabelEmail { get; set; }
        /// <summary>
        /// If this player is authorized using white label, then this stores the token for authorizing the player with the White Label system
        /// </summary>
        public string WhiteLabelToken { get; set; }
        /// <summary>
        /// The platform/authentication method used to authorize this player
        /// </summary>
        public LL_AuthPlatformRepresentation CurrentPlatform { get; set; }
        /// <summary>
        /// When this player was last authenticated
        /// </summary>
        public DateTime LastSignIn { get; set; }
        /// <summary>
        /// When this player was first created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// The id of the wallet for this player
        /// </summary>
        public string WalletID { get; set; }
    }
}