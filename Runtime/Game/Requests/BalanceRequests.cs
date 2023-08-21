using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /*
     * Possible account linking process statuses. Undefined means that the object couldn't be constructed correctly
     */
    public enum LootLockerWalletHolderTypes
    {
        Character = 0,
        Player = 1,
    };
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /*
     *
     */
    public class LootLockerBalance
    {
        /*
         * Current amount of the given currency in this wallet
         */
        public string amount { get; set; }
        /*
         * Information about the currency that this balance is in
         */
        public LootLockerCurrency currency { get; set; }
        /*
         * The id of the wallet holding this balance
         */
        public string wallet_id { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================

    /*
     *
     */
    public class LootLockerListBalancesForWalletResponse : LootLockerResponse
    {
        /*
         * List of balances in different currencies in the requested wallet
         */
        public LootLockerBalance[] balances { get; set; }
    };

    /*
     *
     */
    public class LootLockerGetWalletResponse : LootLockerResponse
    {
        /*
         * The unique id of the holder of this wallet
         */ 
        public string holder_id { get; set; }
        /*
         * The unique id of this wallet
         */
        public string id { get; set; }
        /*
         * The type of entity that holds this wallet
         */
        public LootLockerWalletHolderTypes type { get; set;}
    };

}
