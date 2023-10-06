using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// Possible account linking process statuses. Undefined means that the object couldn't be constructed correctly
    /// </summary>
    public enum LootLockerWalletHolderTypes
    {
        character = 0,
        player = 1,
    };
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerBalance
    {
        /// <summary>
        /// Current amount of the given currency in this wallet
        /// </summary>
        public string amount { get; set; }
        /// <summary>
        /// Information about the currency that this balance is in
        /// </summary>
        public LootLockerCurrency currency { get; set; }
        /// <summary>
        /// The id of the wallet holding this balance
        /// </summary>
        public string wallet_id { get; set; }
        /// <summary>
        /// The time that this balance was created
        /// </summary>
        public string created_at { get; set; }
    };

    //==================================================
    // Request Definitions
    //==================================================
    
    /// <summary>
    /// </summary>
    public class LootLockerCreateWalletRequest
    {
        /// <summary>
        /// ULID of the holder you want to create a wallet for
        /// </summary>
        public string holder_id { get; set; }
        /// <summary>
        /// The type of holder that this holder id refers to
        /// </summary>
        public string holder_type { get; set; }
        /// <summary>
        /// The id of the created wallet
        /// </summary>
        public string id { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerCreditRequest
    {
        /// <summary>
        /// Amount of the given currency to debit/credit to/from the given wallet
        /// </summary>
        public string amount { get; set; }
        /// <summary>
        /// The id of the currency that the amount is given in
        /// </summary>
        public string currency_id { get; set; }
        /// <summary> The id of the wallet to credit/debit to/from
        /// </summary>
        public string wallet_id { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerDebitRequest
    {
        /// <summary>
        /// Amount of the given currency to debit/credit to/from the given wallet
        /// </summary>
        public string amount { get; set; }
        /// <summary>
        /// The id of the currency that the amount is given in
        /// </summary>
        public string currency_id { get; set; }
        /// <summary> The id of the wallet to credit/debit to/from
        /// </summary>
        public string wallet_id { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerListBalancesForWalletResponse : LootLockerResponse
    {
        /// <summary>
        /// List of balances in different currencies in the requested wallet
        /// </summary>
        public LootLockerBalance[] balances { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerGetWalletResponse : LootLockerResponse
    {
        /// <summary>
        /// The unique id of the holder of this wallet
        /// </summary>
        public string holder_id { get; set; }
        /// <summary>
        /// The unique id of this wallet
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The type of entity that holds this wallet
        /// </summary>
        public LootLockerWalletHolderTypes type { get; set;}
    };

    /// <summary>
    /// </summary>
    public class LootLockerCreditWalletResponse : LootLockerResponse
    {
        /// <summary>
        /// Current amount of the given currency in this wallet
        /// </summary>
        public string amount { get; set; }
        /// <summary>
        /// Information about the currency that this balance is in
        /// </summary>
        public LootLockerCurrency currency { get; set; }
        /// <summary>
        /// The id of the wallet holding this balance
        /// </summary>
        public string wallet_id { get; set; }
        /// <summary>
        /// The time that this balance was created
        /// </summary>
        public string created_at { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerDebitWalletResponse : LootLockerResponse
    {
        /// <summary>
        /// Current amount of the given currency in this wallet
        /// </summary>
        public string amount { get; set; }
        /// <summary>
        /// Information about the currency that this balance is in
        /// </summary>
        public LootLockerCurrency currency { get; set; }
        /// <summary>
        /// The id of the wallet holding this balance
        /// </summary>
        public string wallet_id { get; set; }
        /// <summary>
        /// The time that this balance was created
        /// </summary>
        public string created_at { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerCreateWalletResponse : LootLockerResponse
    {
        /// <summary>
        /// The unique id of this wallet
        /// </summary>
        public string wallet_id { get; set; }
    };

}
