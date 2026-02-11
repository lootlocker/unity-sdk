using System;
using System.Collections.Generic;

namespace LootLocker.Requests
{
    public class LootLockerStorage
    {
        public int id { get; set; }
        public string key { get; set; }
        public string value { get; set; }
    }

    public class LootLockerLinks : Dictionary<string, string>
    {
        public string thumbnail
        {
            get
            {
                TryGetValue(nameof(thumbnail), out var value);
                return value;
            }
            set
            {
                if (ContainsKey(nameof(thumbnail)))
                {
                    this[nameof(thumbnail)] = value;
                }
                else
                {
                    Add(nameof(thumbnail), value);
                }
            }
        }
    }

    public class LootLockerRarity
    {
        public string name { get; set; }
        public string short_name { get; set; }
        public string color { get; set; }
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

    public class LootLockerFilter
    {
        public string value { get; set; }
        public string name { get; set; }
    }

    public class LootLockerVariation
    {
        public int id { get; set; }
        public string name { get; set; }
        public object primary_color { get; set; }
        public object secondary_color { get; set; }
        public Dictionary<string, string> links { get; set; }
    }

    public class LootLockerFile
    {
        public string url { get; set; }
        public string[] tags { get; set; }
    }

    public class LootLockerAssetCandidate
    {
        public int created_by_player_id { get; set; }
        public string created_by_player_uid { get; set; }
    }

    [Serializable]
    public class LootLockerCommonAsset
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string ulid { get; set; }
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
        public string[] data_entities { get; set; }
    }

    [Serializable]
    public class LootLockerRental
    {
        public bool is_rental { get; set; }
        public string time_left { get; set; }
        public string duration { get; set; }
        public string is_active { get; set; }
    }

    public class LootLockerContextResponse : LootLockerResponse
    {
        public LootLockerContext[] contexts { get; set; }
    }

    public class LootLockerContext
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string name { get; set; }
        public string friendly_name { get; set; }
        public bool detachable { get; set; }
        public bool user_facing { get; set; }
        public object dependent_asset_id { get; set; }
        public int max_equip_count { get; set; }
    }

    public class LootLockerActivateRentalAssetResponse : LootLockerResponse
    {
        public int time_left { get; set; }
    }

    [Serializable]
    public class LootLockerInventoryResponse : LootLockerResponse
    {
        public LootLockerInventory[] inventory { get; set; }
    }

    public class LootLockerInventory
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public string rental_option_id { get; set; }
        public string acquisition_source { get; set; }
        public DateTime? acquisition_date { get; set; }
        public LootLockerCommonAsset asset { get; set; }
        public LootLockerRental rental { get; set; }


        public float balance { get; set; }
    }
}