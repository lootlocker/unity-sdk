using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /*
     * Possible account linking process statuses. Undefined means that the object couldn't be constructed correctly
     */
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

    /*
     *
     */
    public class LootLockerAccountLinkStartResponse : LootLockerResponse
    {
        /*
         * ID of the account linking process. Save this as you will need it for checking the linking process status and if you want to cancel it.
         */
        public string Link_id { get; set; }
        /*
         * Used by the user in the online account linking process
         */
        public string Code { get; set; }
        /*
         * Base64 encoded PNG image of a qr code that can be shown to the player for them to scan and open the account linking flow
         */
        public string Qr_code { get; set; }
        /*
         * URL to where the user can continue the online account linking process
         */
        public string Code_page_url { get; set; }
    };

    /*
     *
     */
    public class LootLockerAccountLinkProcessStatusResponse : LootLockerResponse
    {
        /*
         * Current status of the specified account linking process
         */

        LootLockerAccountLinkingProcessStatus Status;
        /*
         * Time when the specified account linking process was started
         */
        public string Created_at { get; set; }
    };

    /*
     * This response will be empty unless there's an error
     */
    public class LootLockerCancelAccountLinkingProcessResponse : LootLockerResponse
    {
    };

    /*
     * This response will be empty unless there's an error
     */
    public class LootLockerUnlinkProviderFromAccountResponse : LootLockerResponse
    {
    };

}
