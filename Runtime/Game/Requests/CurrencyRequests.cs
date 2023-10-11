namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// Details about a particular currency
    /// </summary>
    public class LootLockerCurrency
    {
        /// <summary>
        /// The unique id of the currency
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The name of the currency
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The unique short code of the currency
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// True if this currency can be awarded to the player from the game api
        /// </summary>
        public bool game_api_writes_enabled { get; set; }
        /// <summary>
        /// The time that this currency was created
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// The time that this currency was published
        /// </summary>
        public string published_at { get; set; }
    };

    /// <summary>
    /// Represents a denomination of a currency
    /// </summary>
    public class LootLockerDenomination
    {
        /// <summary>
        /// The unique id of the denomination
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The id of the currency this is a denomination of
        /// </summary>
        public string currency { get; set; }
        /// <summary>
        /// The name of this denomination
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The value of this denomination in units of the currency
        /// </summary>
        public int value { get; set; }
        /// <summary>
        /// The time that this denomination was created
        /// </summary>
        public string created_at { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerListCurrenciesResponse : LootLockerResponse
    {
        /// <summary>
        /// List of available currencies
        /// </summary>
        public LootLockerCurrency[] currencies { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerListDenominationsResponse : LootLockerResponse
    {
        /// <summary>
        /// List of available denominations
        /// </summary>
        public LootLockerDenomination[] denominations { get; set; }
    };

}
