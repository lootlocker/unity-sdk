using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Admin;
using LootLocker.Admin.Requests;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
using LootLocker.Requests;

namespace LootLocker.Admin.Requests
{

    #region CreatingEvent

    [Serializable]
    public class LootLockerCreatingEventRequest
    {

        //Do NOT send CreatingEventRequest class the way it is. Send a dictionary using GetCreatingEventRequestDictionary

        //Required
        public string name;
        public int game_id, type, map_id;

        //Not required
        public int asset_id;
        public string poster_path;
        public int rounds;
        [Tooltip("Must be formatted as 00:00:000")]
        public string round_length; //Must be formatted as "00:00:000"
        public int completion_bonus;
        public string difficulty_name;
        public decimal difficulty_multiplier;
        public int time_score_multiplier;
        [Serializable]
        public struct LootLockerGoalStruct
        {
            public string goalName;
            public LootLockerGoal goal;
        }
        public List<LootLockerGoalStruct> goals;
        public LootLockerCheckpoint[] checkpoints;
        public LootLockerFilter[] filters;

        public Dictionary<string, object> GetCreatingEventRequestDictionary(bool sendAssetID, bool sendPosterPath, bool sendRounds, bool sendRoundLength,
            bool sendCompletionBonus, bool sendDifficultyName, bool sendDifficultyMultiplier, bool sendTimeScoreMultiplier,
            bool sendGoals, bool sendCheckpoints, bool sendFilters)
        {

            Dictionary<string, object> dictToConvertToJson = new Dictionary<string, object>();
            dictToConvertToJson.Add("name", name);
            dictToConvertToJson.Add("game_id", game_id);
            dictToConvertToJson.Add("type", type);
            dictToConvertToJson.Add("map_id", map_id);

            if (sendAssetID)
                dictToConvertToJson.Add("asset_id", asset_id);
            if (sendPosterPath)
                dictToConvertToJson.Add("poster_path", poster_path);
            if (sendRounds)
                dictToConvertToJson.Add("rounds", rounds);
            if (sendRoundLength)
                dictToConvertToJson.Add("round_length", round_length);
            if (sendCompletionBonus)
                dictToConvertToJson.Add("completion_bonus", completion_bonus);
            if (sendDifficultyName)
                dictToConvertToJson.Add("difficulty_name", difficulty_name);
            if (sendDifficultyMultiplier)
                dictToConvertToJson.Add("difficulty_multiplier", difficulty_multiplier);
            if (sendTimeScoreMultiplier)
                dictToConvertToJson.Add("time_score_multiplier", time_score_multiplier);

            if (sendGoals)
            {

                Dictionary<string, Dictionary<string, object>> goalsDict = new Dictionary<string, Dictionary<string, object>>();

                foreach (LootLockerGoalStruct goal in goals)
                    goalsDict.Add(goal.goalName, goal.goal.GetGoalDict());

                dictToConvertToJson.Add("goals", goalsDict);

            }

            if (sendCheckpoints)
            {

                List<Dictionary<string, object>> checkpointsToSend = new List<Dictionary<string, object>>();

                foreach (LootLockerCheckpoint checkpoint in checkpoints)
                    checkpointsToSend.Add(checkpoint.GetCheckpointDict());

                dictToConvertToJson.Add("checkpoints", checkpointsToSend);

            }

            if (sendFilters)
                dictToConvertToJson.Add("filters", filters);

            return dictToConvertToJson;

        }

        public string GetCreatingEventRequestJson(bool sendAssetID, bool sendPosterPath, bool sendRounds, bool sendRoundLength,
            bool sendCompletionBonus, bool sendDifficultyName, bool sendDifficultyMultiplier, bool sendTimeScoreMultiplier,
            bool sendGoals, bool sendCheckpoints, bool sendFilters)
        {

            return JsonConvert.SerializeObject(GetCreatingEventRequestDictionary(sendAssetID, sendPosterPath, sendRounds, sendRoundLength,
            sendCompletionBonus, sendDifficultyName, sendDifficultyMultiplier, sendTimeScoreMultiplier,
            sendGoals, sendCheckpoints, sendFilters));

        }

        public Dictionary<string, object> GetUpdatingEventRequestDictionary(bool protectName, bool sendAssetID, bool sendPosterPath, bool sendRounds, bool sendRoundLength,
           bool sendCompletionBonus, bool sendDifficultyName, bool sendDifficultyMultiplier, bool sendTimeScoreMultiplier,
           bool sendGoals, bool sendCheckpoints, bool sendFilters)
        {

            Dictionary<string, object> dictToConvertToJson = GetCreatingEventRequestDictionary(sendAssetID, sendPosterPath, sendRounds, sendRoundLength,
            sendCompletionBonus, sendDifficultyName, sendDifficultyMultiplier, sendTimeScoreMultiplier,
            sendGoals, sendCheckpoints, sendFilters);

            dictToConvertToJson.Add("protect_name", protectName);

            return dictToConvertToJson;

        }

    }

    public class LootLockerCreatingEventResponse : LootLockerResponse
    {

        public bool success { get; set; }
        public string error { get; set; }

    }

    #endregion

    #region GettingAllEvents

    public class LootLockerGettingAllEventsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerEvent[] events { get; set; }
    }
    public class LootLockerEvent
    {
        public int event_id { get; set; }
        public int asset_id { get; set; }
        public int rounds { get; set; }
        public string round_length { get; set; }
        public object difficulty_name { get; set; }
        public object difficulty_multiplier { get; set; }
        public Dictionary<string, Dictionary<string, object>> goals { get; set; }
        public LootLockerCheckpointGAE[] checkpoints { get; set; }
    }
    public class LootLockerCheckpointGAE
    {
        public string guid { get; set; }
        public int index { get; set; }
        public int segment_time { get; set; }
        public string your_key { get; set; }
        public string your_second_key { get; set; }
    }

    #endregion

    #region CommonClasses

    [Serializable]
    public class LootLockerCheckpoint
    {

        public int index;
        [Serializable]
        public struct PropertiesStruct
        {

            public string propertyName, propertyValue;

        }
        public List<PropertiesStruct> properties;

        public string GetCheckpointJson()
        {

            return JsonConvert.SerializeObject(GetCheckpointDict());

        }

        public Dictionary<string, object> GetCheckpointDict()
        {

            Dictionary<string, object> dictToConvertToJson = new Dictionary<string, object>();
            dictToConvertToJson.Add("index", index);
            foreach (PropertiesStruct property in properties)
                dictToConvertToJson.Add(property.propertyName, property.propertyValue);

            return dictToConvertToJson;

        }

    }

    [Serializable]
    public class LootLockerFilter
    {

        public string name, value;

    }

    [Serializable]
    public class LootLockerGoal
    {

        //Required
        public string goal;
        public int points;
        public bool includeAssets;

        //Not required
        public LootLockerEventAsset[] assets;

        public Dictionary<string, object> GetGoalDict()
        {

            Dictionary<string, object> dictToConvertToJson = new Dictionary<string, object>();
            dictToConvertToJson.Add("goal", goal);
            dictToConvertToJson.Add("points", points);

            if (includeAssets)
                dictToConvertToJson.Add("assets", assets);

            return dictToConvertToJson;

        }

        public string GetGoalJson()
        {

            return JsonConvert.SerializeObject(GetGoalDict());

        }

    }
    [Serializable]
    public class LootLockerEventAsset
    {
        public int asset_id;
        public int asset_variation_id;
    }


    #endregion

}

namespace LootLocker.Admin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void CreatingEvent(Dictionary<string, object> requestData, Action<LootLockerCreatingEventResponse> onComplete)
        {

            string json = "";
            if (requestData == null) return;
            else json = JsonConvert.SerializeObject(requestData);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.creatingEvent;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerCreatingEventResponse response = new LootLockerCreatingEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    Debug.Log("Server response for creating event: " + serverResponse.text);
                    Debug.Log("Server response code: " + serverResponse.statusCode);
                    response = JsonConvert.DeserializeObject<LootLockerCreatingEventResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);

        }

        public static void UpdatingEvent(LootLockerGetRequest lootLockerGetRequest, Dictionary<string, object> requestData, Action<LootLockerCreatingEventResponse> onComplete)
        {

            string json = "";
            if (requestData == null) return;
            else json = JsonConvert.SerializeObject(requestData);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.updatingEvent;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerCreatingEventResponse response = new LootLockerCreatingEventResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    Debug.Log("Server response for updating event: " + serverResponse.text);
                    Debug.Log("Server response code: " + serverResponse.statusCode);
                    response = JsonConvert.DeserializeObject<LootLockerCreatingEventResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);

        }

        public static void GettingAllEvents(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerGettingAllEventsResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.gettingAllEvents;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                LootLockerGettingAllEventsResponse response = new LootLockerGettingAllEventsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerGettingAllEventsResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);

        }

    }

}