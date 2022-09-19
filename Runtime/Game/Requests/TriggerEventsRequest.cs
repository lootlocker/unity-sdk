using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using Newtonsoft.Json;
using System;

namespace LootLocker.Requests
{
    public class LootLockerTriggerAnEventRequest
    {
        public string name { get; set; }
    }

    public class LootLockerTriggerAnEventResponse : LootLockerResponse
    {
        public bool check_grant_notifications { get; set; }
        public LootLockerXp xp { get; set; }
        public LootLockerLevel [] levels { get; set; }

        public LootLockerGrantedAssets [] granted_assets {get; set;}
    }

    public class LootLockerGrantedAssets
    {
        public int id;
        public string uuid;
        public string name;
        public bool active;
        public bool purchasable;
        public int price;
        public string sales_price;
        public string display_price;
        public string context;
        public string context_id;
        public int[] character_classes;
        public string unlocks_context;
        public bool detachable;
        public string updated;
        public string marked_new;
        public int default_variation_id;
        public string description;
        public LootLockerDefault_Loadouts default_Loadouts;
        public string links;
        public LootLockerStorage[] storage;
        public string rarity;
        public string popular;
        public int popularity_score;
        public string package_contents;
        public bool unique_instance;
        public string external_identifiers;
        public string rental_options;
        public LootLockerEnums.AssetFilter[] filters;
        public LootLockerFile [] files;
        public string [] data_entities;
        public LootLockerHeroEquipExceptions hero_equip_exceptions;
        public string asset_candidate;
        public int drop_table_max_picks;
    }

    public class LootLockerHeroEquipExceptions
    {
        public bool can_equip;
        public int hero_id;
        public string name;
    }

    public class LootLockerListingAllTriggersResponse : LootLockerResponse
    {
        public string[] triggers { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public EndPointClass triggeringAnEvent;
        public EndPointClass listingTriggeredTriggerEvents;

        public static void TriggeringAnEvent(LootLockerTriggerAnEventRequest data, Action<LootLockerTriggerAnEventResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.triggeringAnEvent;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }

        public static void ListingTriggeredTriggerEvents(Action<LootLockerListingAllTriggersResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listingTriggeredTriggerEvents;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, "", (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }
    }
}