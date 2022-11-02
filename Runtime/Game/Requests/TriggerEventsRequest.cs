using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using Newtonsoft.Json;
using System;

namespace LootLocker.Requests
{
    [Obsolete("This class is deprecated and will be removed at a later stage. Please use LootLockerExecuteTriggerRequest instead")]
    public class LootLockerTriggerAnEventRequest
    {
        public string name { get; set; }
    }

    #pragma warning disable 0618
    // Disabling the "Obsolete warning" for this class, since we want to keep the old class for backwards compatibility.
    public class LootLockerExecuteTriggerRequest : LootLockerTriggerAnEventRequest
    {
    }
    #pragma warning restore 0618
   
    [Obsolete("This class is deprecated and will be removed at a later stage. Please use LootLockerExecuteTriggerResponse instead")]
    public class LootLockerTriggerAnEventResponse : LootLockerResponse
    {
        public bool check_grant_notifications { get; set; }
        public LootLockerXp xp { get; set; }
        public LootLockerLevel[] levels { get; set; }
        public LootLockerGrantedAssets [] granted_assets;
    }
    #pragma warning disable 0618
    // Disabling the "Obsolete warning" for this class, since we want to keep the old class for backwards compatibility.
    public class LootLockerExecuteTriggerResponse : LootLockerTriggerAnEventResponse
    {
    }
    #pragma warning restore 0618

    [Obsolete("This class is deprecated and will be removed at a later stage. Please use LootLockerListAllTriggersResponse instead")]
    public class LootLockerListingAllTriggersResponse : LootLockerResponse
    {
        public string[] triggers { get; set; }
    }
    #pragma warning disable 0618
    // Disabling the "Obsolete warning" for this class, since we want to keep the old class for backwards compatibility.
    public class LootLockerListAllTriggersResponse : LootLockerListingAllTriggersResponse
    {
    }
    #pragma warning restore 0618

    public class LootLockerGrantedAssets
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
        public bool purchasable { get; set; }
        public int price { get; set; }
        public object sales_price { get; set; }
        public object display_price { get; set; }
        public string context { get; set; }
        public int context_id { get; set; }
        public string [] character_classes { get; set; }
        public object unlocks_context { get; set; }
        public bool detachable { get; set; }
        public string updated { get; set; }
        public string marked_new { get; set; }
        public int default_variation_id { get; set; }
        //public LootLockerDefault_Loadouts default_loadouts { get; set; }
        public string description { get; set; }
        public object links { get; set; }
        public LootLockerStorage [] storage { get; set; }
        public object rarity { get; set; }
        public bool popular { get; set; }
        public int popularity_score { get; set; }
        public object package_contents { get; set; }
        public bool unique_instance { get; set; }
        public object external_identifiers { get; set; }
        public object rental_options { get; set; }
        public LootLockerFilter [] filters { get; set; }
        public LootLockerFile [] files { get; set; }
        public List<object> data_entities { get; set; }
        public LootLockerHeroEquipExceptions hero_equip_exceptions { get; set; }
        public object asset_candidate { get; set; }
        public int? drop_table_max_picks { get; set; }
    }

    public class LootLockerHeroEquipExceptions
    {
        public bool can_equip;
        public int hero_id;
        public string name;
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public EndPointClass triggeringAnEvent;
        public EndPointClass listingTriggeredTriggerEvents;

        public static void ExecuteTrigger(LootLockerExecuteTriggerRequest data, Action<LootLockerExecuteTriggerResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.triggeringAnEvent;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void ListAllExecutedTriggers(Action<LootLockerListAllTriggersResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listingTriggeredTriggerEvents;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, "", (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }
    }
}
