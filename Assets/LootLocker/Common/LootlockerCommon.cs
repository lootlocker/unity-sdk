using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using System.Linq;
using UnityEngine.UI;

namespace LootLockerEnums
{
    public enum AssetFilter { purchasable , nonpurchasable , rentable, nonrentable, popular , nonpopular, none }
}

namespace LootLocker
{
    public class Links
    {
        public string thumbnail { get; set; }
    }

    public class Default_Loadouts_Info
    {
        public bool Default { get; set; }
    }

    public class Variation_Info
    {
        public int id { get; set; }
        public string name { get; set; }
        public object primary_color { get; set; }
        public object secondary_color { get; set; }
        public object links { get; set; }
    }

    [System.Serializable]
    public class AssetRequest : LootLockerResponse
    {
        public int count;
        public static int lastId;
        public static void ResetAssetCalls()
        {
            lastId = 0;
        }
    }

    public class AssetResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Asset[] assets { get; set; }
    }

    public class Rental_Options
    {
        public int id { get; set; }
        public string name { get; set; }
        public int duration { get; set; }
        public int price { get; set; }
        public object sales_price { get; set; }
        public object links { get; set; }
    }

    public class LootLockerStorage
    {
        public string key;
        public string value;
    }

    public class Rarity
    {
        public string name { get; set; }
        public string short_name { get; set; }
        public string color { get; set; }
    }

    public class Filter
    {
        public string value { get; set; }
        public string name { get; set; }
    }


    public class Asset: LootLockerResponse
    {
        public bool success;
        public int id { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
        public bool purchasable { get; set; }
        public string type { get; set; }
        public int price { get; set; }
        public int? sales_price { get; set; }
        public string display_price { get; set; }
        public string context { get; set; }
        public string unlocks_context { get; set; }
        public bool detachable { get; set; }
        public string updated { get; set; }
        public string marked_new { get; set; }
        public int default_variation_id { get; set; }
        public string description { get; set; }
        public Links links { get; set; }
        public LootLockerStorage[] storage { get; set; }
        public Rarity rarity { get; set; }
        public bool popular { get; set; }
        public int popularity_score { get; set; }
        public bool unique_instance { get; set; }
        public Rental_Options[] rental_options { get; set; }
        public Filter[] filters { get; set; }
        public Variation[] variations { get; set; }
        public bool featured { get; set; }
        public bool context_locked { get; set; }
        public bool initially_purchasable { get; set; }
        public File[] files { get; set; }
        public LootLockerAssetCandidate asset_candidate { get; set; }
    }

    public class LootLockerAssetCandidate
    {
        public int created_by_player_id;
        public string created_by_player_uid;
    }

    public class File
    {
        public string url { get; set; }
        public string[] tags { get; set; }
    }

    public class Default_Loadouts
    {
        public bool Default { get; set; }
    }

    public class Variation
    {
        public int id { get; set; }
        public string name { get; set; }
        public object primary_color { get; set; }
        public object secondary_color { get; set; }
        public object links { get; set; }
    }

}