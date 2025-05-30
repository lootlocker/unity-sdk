﻿using LootLocker.Requests;
using System;

namespace LootLocker.Requests
{
    [Serializable]
    public class LootLockerEventResponse : LootLockerResponse
    {
        public LootLockerEvent[] events { get; set; }
    }

    [Serializable]
    public class LootLockerSingleEventResponse : LootLockerResponse
    {
        public LootLockerEvent events { get; set; }
    }

    [Serializable]
    public class LootLockerFinishEventResponse : LootLockerResponse
    {
        public int score { get; set; }
        public bool check_grant_notifications { get; set; }
    }

    [Serializable]
    public class LootLockerStartinEventResponse : LootLockerResponse
    {
        public string signature { get; set; }
    }

    [Serializable]
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

    [Serializable]
    public class LootLockerGoals
    {
        public LootLockerGold gold { get; set; }
        public LootLockerSilver silver { get; set; }
        public LootLockerBronze bronze { get; set; }
    }

    [Serializable]
    public class LootLockerGold
    {
        public string goal { get; set; }
        public string points { get; set; }
        public LootLockerCommonAsset[] assets { get; set; }
    }

    [Serializable]
    public class LootLockerSilver
    {
        public string goal { get; set; }
        public string points { get; set; }
        public object[] assets { get; set; }
    }

    [Serializable]
    public class LootLockerBronze
    {
        public string goal { get; set; }
        public string points { get; set; }
        public object[] assets { get; set; }
    }

    [Serializable]
    public class LootLockerCheckpoint
    {
        public int index { get; set; }
        public int time { get; set; }
        public string your_key { get; set; }
        public string your_second_key { get; set; }
    }

    [Serializable]
    public class FinishEventRequest
    {
        public string signature { get; set; }
        public LootLockerEventPayload payload { get; set; }
    }

    [Serializable]
    public class LootLockerEventPayload
    {
        public string finish_time { get; set; }
        public string finish_score { get; set; }
        public LootLockerCheckpointTimes[] checkpoint_times { get; set; }
    }

    [Serializable]
    public class LootLockerCheckpointTimes
    {
        public int index { get; set; }
        public int time { get; set; }
        public int score { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GettingAllEvents(Action<LootLockerEventResponse> onComplete, string forPlayerWithUlid = null)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAllEvents;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GettingASingleEvent(LootLockerGetRequest data, Action<LootLockerSingleEventResponse> onComplete, string forPlayerWithUlid = null)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingASingleEvent;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void StartingEvent(LootLockerGetRequest data, Action<LootLockerStartinEventResponse> onComplete, string forPlayerWithUlid = null)
        {
            EndPointClass endPoint = LootLockerEndPoints.startingEvent;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void FinishingEvent(LootLockerGetRequest lootLockerGetRequest, FinishEventRequest data, Action<LootLockerFinishEventResponse> onComplete, string forPlayerWithUlid = null)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerFinishEventResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.finishingEvent;

            string getVariable = endPoint.WithPathParameter(lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}