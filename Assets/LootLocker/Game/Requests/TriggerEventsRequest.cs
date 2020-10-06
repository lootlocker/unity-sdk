using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;
using Newtonsoft.Json;
using System;

namespace LootLockerRequests
{
    public class TriggerAnEventRequest
    {
        public string name { get; set; }
    }

    public class TriggerAnEventResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public bool check_grant_notifications { get; set; }
        public Xp xp { get; set; }
        public Level[] levels { get; set; }
    }


    public class ListingAllTriggersResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string[] triggers { get; set; }
    }


}

namespace LootLocker
{

    public partial class LootLockerAPIManager
    {
        public EndPointClass triggeringAnEvent;
        public EndPointClass listingTriggeredTriggerEvents;

        public static void TriggeringAnEvent(TriggerAnEventRequest data, Action<TriggerAnEventResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.triggeringAnEvent;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                TriggerAnEventResponse response = new TriggerAnEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<TriggerAnEventResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void ListingTriggeredTriggerEvents(Action<ListingAllTriggersResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPoints.current.listingTriggeredTriggerEvents;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, "", (serverResponse) =>
            {
                ListingAllTriggersResponse response = new ListingAllTriggersResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<ListingAllTriggersResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }
    }

}