using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /// <summary> Possible account linking process statuses. Undefined means that the object couldn't be constructed correctly
     /// </summary>
    public enum LootLockerAccountLinkingProcessStatus
    {
        Undefined = 0,
        Started = 1,
        Cancelled = 2,
        Completed = 3
    };
}

namespace LootLocker.Requests
{
    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
     /// </summary>
    public class LootLockerAccountLinkStartResponse : LootLockerResponse
    {
        /// <summary>
        /// ID of the account linking process. Save this as you will need it for checking the linking process status and if you want to cancel it.
        /// </summary>
        public string Link_id { get; set; }
        /// <summary>
        /// Used by the user in the online account linking process
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// Base64 encoded PNG image of a qr code that can be shown to the player for them to scan and open the account linking flow
        /// </summary>
        public string Qr_code { get; set; }
        /// <summary>
        /// URL to where the user can continue the online account linking process
        /// </summary>
        public string Code_page_url { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerAccountLinkProcessStatusResponse : LootLockerResponse
    {
        /// <summary>
         /// Current status of the specified account linking process
         /// </summary>

        LootLockerAccountLinkingProcessStatus Status;
        /// <summary>
         /// Time when the specified account linking process was started
         /// </summary>
        public string Created_at { get; set; }
    };

    /// <summary>
    /// This response will be empty unless there's an error
    /// </summary>
    public class LootLockerCancelAccountLinkingProcessResponse : LootLockerResponse
    {
    };

    /// <summary>
    /// This response will be empty unless there's an error
    /// </summary>
    public class LootLockerUnlinkProviderFromAccountResponse : LootLockerResponse
    {
    };

}
