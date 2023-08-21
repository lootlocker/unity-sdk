using LootLocker.LootLockerEnums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /*
     * Information about a particular currency
     */
    public class LootLockerCurrency : LootLockerResponse
    {
        /*
         * The time when this currency was created
         */
        public string created_at { get; set; }
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
         * Whether this currency is published or not
         */
        public bool published { get; set; }
        /*
         * The time when this currency was published (if it was)
         */
        public string published_at { get; set; }
    };

    /*
     * Represents a denomination of a currency
     */
    public class LootLockerDenomination : LootLockerResponse
    {
        /*
         * The time when this denomination was created
         */
        public string created_at { get; set; }
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
    };

    //==================================================
    // Response Definitions
    //==================================================

    /*
     *
     */
    public class LootLockerGetCurrencyByCodeResponse : LootLockerResponse
    {
        /*
         * The time when this currency was created
         */
        public string created_at { get; set; }
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
         * Whether this currency is published or not
         */
        public bool published { get; set; }
        /*
         * The time when this currency was published (if it was)
         */
        public string published_at { get; set; }
    };

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
