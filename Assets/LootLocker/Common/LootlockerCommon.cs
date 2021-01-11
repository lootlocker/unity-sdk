using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using System.Linq;
using UnityEngine.UI;

namespace LootLocker.LootLockerEnums
{
    public enum AssetFilter { purchasable , nonpurchasable , rentable, nonrentable, popular , nonpopular, none }
}

namespace LootLocker
{
    public class LootLockerLinks
    {
        public string thumbnail { get; set; }
    }

    public class LootLockerDefault_Loadouts_Info
    {
        public bool Default { get; set; }
    }

    public class LootLockerVariation_Info
    {
        public int id { get; set; }
        public string name { get; set; }
        public object primary_color { get; set; }
        public object secondary_color { get; set; }
        public object links { get; set; }
    }

    [System.Serializable]
    public class LootLockerAssetRequest : LootLockerResponse
    {
        public int count;
        public static int lastId;
        public static void ResetAssetCalls()
        {
            lastId = 0;
        }
    }

    public class LootLockerAssetResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerCommonAsset[] assets { get; set; }
    }

    public class LootLockerRental_Options
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

    public class LootLockerRarity
    {
        public string name { get; set; }
        public string short_name { get; set; }
        public string color { get; set; }
    }

    public class LootLockerFilter
    {
        public string value { get; set; }
        public string name { get; set; }
    }


    public class LootLockerCommonAsset: LootLockerResponse
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
        public LootLockerLinks links { get; set; }
        public LootLockerStorage[] storage { get; set; }
        public LootLockerRarity rarity { get; set; }
        public bool popular { get; set; }
        public int popularity_score { get; set; }
        public bool unique_instance { get; set; }
        public LootLockerRental_Options[] rental_options { get; set; }
        public LootLockerFilter[] filters { get; set; }
        public LootLockerVariation[] variations { get; set; }
        public bool featured { get; set; }
        public bool context_locked { get; set; }
        public bool initially_purchasable { get; set; }
        public LootLockerFile[] files { get; set; }
        public LootLockerAssetCandidate asset_candidate { get; set; }
    }

    public class LootLockerAssetCandidate
    {
        public int created_by_player_id;
        public string created_by_player_uid;
    }

    public class LootLockerFile
    {
        public string url { get; set; }
        public string[] tags { get; set; }
    }

    public class LootLockerDefault_Loadouts
    {
        public bool Default { get; set; }
    }

    public class LootLockerVariation
    {
        public int id { get; set; }
        public string name { get; set; }
        public object primary_color { get; set; }
        public object secondary_color { get; set; }
        public object links { get; set; }
    }

}