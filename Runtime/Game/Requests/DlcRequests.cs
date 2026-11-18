using System;

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// A DLC the player currently holds, as reported by the DLC check endpoint.
    /// </summary>
    [Serializable]
    public class LootLockerDlcCheckEntry
    {
        /// <summary>The ULID of the catalog item that carries the DLC listing.</summary>
        public string catalog_item_id { get; set; }

        /// <summary>The DLC's identifier on the platform (for example a Steam app id or a Meta product id). Always a string, even when numeric.</summary>
        public string dlc_identifier { get; set; }

        /// <summary>The platform the DLC was granted for. Only <see cref="LL_AuthPlatforms.Steam"/> and <see cref="LL_AuthPlatforms.Meta"/> support DLC checks.</summary>
        public LL_AuthPlatforms platform { get; set; }

        /// <summary>The ULID of the active DLC entitlement granting this DLC.</summary>
        public string entitlement_id { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response containing the DLCs the player currently holds for the session's platform.
    /// </summary>
    [Serializable]
    public class LootLockerCheckDLCsResponse : LootLockerResponse
    {
        /// <summary>The DLCs the player currently holds, ordered by catalog item id. Empty when the player owns no DLCs.</summary>
        public LootLockerDlcCheckEntry[] dlcs { get; set; }
    }
}
