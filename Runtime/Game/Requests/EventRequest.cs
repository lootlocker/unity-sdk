using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using Newtonsoft.Json;

namespace LootLocker.Requests
{

    [System.Serializable]
    public class LootLockerEventResponse : LootLockerResponse
    {
        
        public LootLockerEvent[] events { get; set; }
    }
    [System.Serializable]
    public class LootLockerSingleEventResponse : LootLockerResponse
    {
        
        public LootLockerEvent events { get; set; }
    }

    [System.Serializable]
    public class LootLockerFinishEventResponse : LootLockerResponse
    {
        
        public int score { get; set; }
        public bool check_grant_notifications { get; set; }
    }

    [System.Serializable]
    public class LootLockerStartinEventResponse : LootLockerResponse
    {
        
        public string signature { get; set; }
    }

    [System.Serializable]
    public class LootLockerEvent
    {
        public int event_id { get; set; }
        public int asset_id { get; set; }
        public int rounds { get; set; }
        public string round_length { get; set; }
        public object difficulty_name { get; set; }
        public object difficulty_multiplier { get; set; }
        public string difficulty_color { get; set; }
        public int difficulty_id { get; set; }
        public LootLockerGoals goals { get; set; }
        public LootLockerCheckpoint[] checkpoints { get; set; }
        public bool player_access { get; set; }
        public string best_goal { get; set; }
    }
    [System.Serializable]
    public class LootLockerGoals
    {
        public LootLockerGold gold { get; set; }
        public LootLockerSilver silver { get; set; }
        public LootLockerBronze bronze { get; set; }
    }
    [System.Serializable]
    public class LootLockerGold
    {
        public string goal { get; set; }
        public string points { get; set; }
        public LootLockerCommonAsset[] assets { get; set; }
    }
    [System.Serializable]
    public class LootLockerSilver
    {
        public string goal { get; set; }
        public string points { get; set; }
        public object[] assets { get; set; }
    }
    [System.Serializable]
    public class LootLockerBronze
    {
        public string goal { get; set; }
        public string points { get; set; }
        public object[] assets { get; set; }
    }
    [System.Serializable]
    public class LootLockerCheckpoint
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
        public LootLockerEventPayload payload { get; set; }
    }
    [System.Serializable]
    public class LootLockerEventPayload
    {
        public string finish_time { get; set; }
        public string finish_score { get; set; }
        public LootLockerCheckpointTimes[] checkpoint_times { get; set; }
    }
    [System.Serializable]
    public class LootLockerCheckpointTimes
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
        public static void GettingAllEvents(Action<LootLockerEventResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAllEvents;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerEventResponse response = new LootLockerEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerEventResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                         response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void GettingASingleEvent(LootLockerGetRequest data, Action<LootLockerSingleEventResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingASingleEvent;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerSingleEventResponse response = new LootLockerSingleEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerSingleEventResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                         response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void StartingEvent(LootLockerGetRequest data, Action<LootLockerStartinEventResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.startingEvent;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerStartinEventResponse response = new LootLockerStartinEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerStartinEventResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }
        public static void FinishingEvent(LootLockerGetRequest lootLockerGetRequest, FinishEventRequest data, Action<LootLockerFinishEventResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.finishingEvent;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerFinishEventResponse response = new LootLockerFinishEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerFinishEventResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

    }
}