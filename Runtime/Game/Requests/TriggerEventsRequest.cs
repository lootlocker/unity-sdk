using LootLocker.Requests;
using System;
using System.Collections.Generic;

namespace LootLocker.Requests
{
    public class LootLockerExecuteTriggerRequest : LootLockerResponse
    {
        public string name { get; set; }
    }

    public class LootLockerExecuteTriggerResponse : LootLockerResponse
    {
        public bool check_grant_notifications { get; set; }
        public LootLockerXp xp { get; set; }
        public LootLockerLevel[] levels { get; set; }
        public LootLockerGrantedAssets[] granted_assets { get; set; }
    }

    public class LootLockerListAllTriggersResponse : LootLockerResponse
    {
        public string[] triggers { get; set; }
    }

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
        public bool can_equip { get; set; }
        public int hero_id { get; set; }
        public string name { get; set; }
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
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerExecuteTriggerResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.triggeringAnEvent;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListAllExecutedTriggers(Action<LootLockerListAllTriggersResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listingTriggeredTriggerEvents;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, "", (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}
