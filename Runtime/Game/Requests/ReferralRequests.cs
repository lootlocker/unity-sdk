using System;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// The action that turns a claimed referral into a payout.
    /// </summary>
    public enum LootLockerReferralQualifier
    {
        /// <summary>The referee qualified by starting their first session with the code.</summary>
        session_started,
        /// <summary>The referee qualified by owning one of the program's configured catalog items.</summary>
        entitlement_owned,
        /// <summary>The referee qualified because the game server asserted it.</summary>
        game_asserted,
        /// <summary>The referee qualified by earning a configured publisher currency.</summary>
        publisher_currency_earned
    }

    /// <summary>
    /// The state of a referral relation.
    /// </summary>
    public enum LootLockerReferralStatus
    {
        /// <summary>The referral is claimed and waiting for its qualifying action.</summary>
        claimed,
        /// <summary>The qualifying action happened and both sides were rewarded.</summary>
        qualified,
        /// <summary>The referral was voided, for example because a party was banned.</summary>
        @void
    }
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// One referral program as it applies to the inviting player: the program's identity, the action that
    /// qualifies a referral, and how many rewarded referrals the player has against the program's cap.
    /// </summary>
    public class LootLockerReferralProgram
    {
        /// <summary>
        /// The game-unique machine key of the program.
        /// </summary>
        public string program_key { get; set; }
        /// <summary>
        /// The human-friendly display name of the program.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The action that turns a claimed referral into a payout.
        /// </summary>
        public LootLockerReferralQualifier qualifier { get; set; }
        /// <summary>
        /// How many rewarded referrals the player has made under this program.
        /// </summary>
        public int referral_count { get; set; }
        /// <summary>
        /// The maximum number of rewarded referrals this player may make under this program, or null when
        /// the program is uncapped.
        /// </summary>
        public int? referrer_cap { get; set; }
    }

    /// <summary>
    /// One referral relation as the referring player sees it: the program it was made under, the referee's
    /// public UID, and its state.
    /// </summary>
    public class LootLockerReferral
    {
        /// <summary>
        /// The referral ULID.
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The game-unique machine key of the program the referral was made under.
        /// </summary>
        public string program_key { get; set; }
        /// <summary>
        /// The human-friendly display name of the program.
        /// </summary>
        public string program_name { get; set; }
        /// <summary>
        /// The referred player's public UID, or null if their game-player row is gone.
        /// </summary>
        public string referee_public_uid { get; set; }
        /// <summary>
        /// The state of the referral.
        /// </summary>
        public LootLockerReferralStatus status { get; set; }
        /// <summary>
        /// When the referral was claimed.
        /// </summary>
        public DateTime claimed_at { get; set; }
        /// <summary>
        /// When the referral qualified, or null while it is still pending.
        /// </summary>
        public DateTime? qualified_at { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response containing the calling player's referral code and their rewarded-referral count against each
    /// active program's cap. Use this to render an invite-a-friend screen.
    /// </summary>
    public class LootLockerReferralInviteResponse : LootLockerResponse
    {
        /// <summary>
        /// The player's referral code, which is their public UID. Share this with friends so they can supply
        /// it as <see cref="LootLockerSessionOptionals.referral_code"/> when starting their first session.
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// The game's active referral programs, each with the player's progress against its cap.
        /// </summary>
        public LootLockerReferralProgram[] programs { get; set; }
    }

    /// <summary>
    /// Response containing one page of the calling player's own referrals, newest first, including ones still
    /// pending their qualifying action.
    /// </summary>
    public class LootLockerReferralListResponse : LootLockerResponse
    {
        /// <summary>
        /// The player's referrals, newest first.
        /// </summary>
        public LootLockerReferral[] referrals { get; set; }
        /// <summary>
        /// Pagination information for the referrals.
        /// </summary>
        public LootLockerExtendedPagination pagination { get; set; }
    }
}
