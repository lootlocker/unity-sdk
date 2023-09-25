namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /*
     * Details about a particular currency
     */
    public class LootLockerCurrency
    {
        /*
         * The unique id of the currency
         */
        public string id { get; set; }
        /*
         * The name of the currency
         */
        public string name { get; set; }
        /*
         * The unique short code of the currency
         */
        public string code { get; set; }
        /*
         * True if this currency can be awarded to the player from the game api
         */
        public bool game_api_writes_enabled { get; set; }
        /*
         * The time that this currency was created
         */
        public string created_at { get; set; }
        /*
         * The time that this currency was published
         */
        public string published_at { get; set; }
    };

    /*
     * Represents a denomination of a currency
     */
    public class LootLockerDenomination
    {
        /*
         * The unique id of the denomination
         */
        public string id { get; set; }
        /*
         * The id of the currency this is a denomination of
         */
        public string currency { get; set; }
        /*
         * The name of this denomination
         */
        public string name { get; set; }
        /*
         * The value of this denomination in units of the currency
         */
        public int value { get; set; }
        /*
         * The time that this denomination was created
         */
        public string created_at { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================

    /*
     *
     */
    public class LootLockerListCurrenciesResponse : LootLockerResponse
    {
        /*
         * List of available currencies
         */
        public LootLockerCurrency[] currencies { get; set; }
    };

    /*
     *
     */
    public class LootLockerListDenominationsResponse : LootLockerResponse
    {
        /*
         * List of available denominations
         */
        public LootLockerDenomination[] denominations { get; set; }
    };

}
