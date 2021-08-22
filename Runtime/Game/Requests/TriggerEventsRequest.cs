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
        public LootLockerLevel[] levels { get; set; }
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

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerTriggerAnEventResponse response = new LootLockerTriggerAnEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerTriggerAnEventResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void ListingTriggeredTriggerEvents(Action<LootLockerListingAllTriggersResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPoints.listingTriggeredTriggerEvents;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, "", (serverResponse) =>
            {
                LootLockerListingAllTriggersResponse response = new LootLockerListingAllTriggersResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerListingAllTriggersResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }
    }

}