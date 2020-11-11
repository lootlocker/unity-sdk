using LootLockerRequests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using Newtonsoft.Json;

namespace LootLockerRequests
{

    [System.Serializable]
    public class EventResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Event[] events { get; set; }
    }
    [System.Serializable]
    public class SingleEventResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Event events { get; set; }
    }

    [System.Serializable]
    public class FinishEventResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int score { get; set; }
        public bool check_grant_notifications { get; set; }
    }

    [System.Serializable]
    public class StartinEventResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string signature { get; set; }
    }

    [System.Serializable]
    public class Event
    {
        public int event_id { get; set; }
        public int asset_id { get; set; }
        public int rounds { get; set; }
        public string round_length { get; set; }
        public object difficulty_name { get; set; }
        public object difficulty_multiplier { get; set; }
        public string difficulty_color { get; set; }
        public int difficulty_id { get; set; }
        public Goals goals { get; set; }
        public Checkpoint[] checkpoints { get; set; }
        public bool player_access { get; set; }
        public string best_goal { get; set; }
    }
    [System.Serializable]
    public class Goals
    {
        public Gold gold { get; set; }
        public Silver silver { get; set; }
        public Bronze bronze { get; set; }
    }
    [System.Serializable]
    public class Gold
    {
        public string goal { get; set; }
        public string points { get; set; }
        public Asset[] assets { get; set; }
    }
    [System.Serializable]
    public class Silver
    {
        public string goal { get; set; }
        public string points { get; set; }
        public object[] assets { get; set; }
    }
    [System.Serializable]
    public class Bronze
    {
        public string goal { get; set; }
        public string points { get; set; }
        public object[] assets { get; set; }
    }
    [System.Serializable]
    public class Checkpoint
    {
        public int index { get; set; }
        public int time { get; set; }
        public string your_key { get; set; }
        public string your_second_key { get; set; }
    }

    [System.Serializable]
    public class FinishEventRequest
    {
        public string signature { get; set; }
        public EventPayload payload { get; set; }
    }
    [System.Serializable]
    public class EventPayload
    {
        public string finish_time { get; set; }
        public string finish_score { get; set; }
        public CheckpointTimes[] checkpoint_times { get; set; }
    }
    [System.Serializable]
    public class CheckpointTimes
    {
        public int index;
        public int time;
        public int score;
    }


}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GettingAllEvents(Action<EventResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAllEvents;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                EventResponse response = new EventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<EventResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void GettingASingleEvent(LootLockerGetRequest data, Action<SingleEventResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingASingleEvent;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                SingleEventResponse response = new SingleEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<SingleEventResponse>(serverResponse.text);
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

        public static void StartingEvent(LootLockerGetRequest data, Action<StartinEventResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.startingEvent;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                StartinEventResponse response = new StartinEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<StartinEventResponse>(serverResponse.text);
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
        public static void FinishingEvent(LootLockerGetRequest lootLockerGetRequest, FinishEventRequest data, Action<FinishEventResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.finishingEvent;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                FinishEventResponse response = new FinishEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<FinishEventResponse>(serverResponse.text);
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