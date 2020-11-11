using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;


namespace LootLockerRequests
{
    public class Mission
    {
        public int mission_id { get; set; }
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

    public class FinishingPayload
    {
        public string finish_time { get; set; }
        public string finish_score { get; set; }
        public CheckpointTimes[] checkpoint_times { get; set; }
    }

    public class FinishingAMissionRequest : LootLockerGetRequest
    {
        public string signature { get; set; }
        public FinishingPayload payload { get; set; }
    }

    public class GettingAllMissionsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Mission[] missions { get; set; }
    }

    public class GettingASingleMissionResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Mission mission { get; set; }
    }


    public class StartingAMissionResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string signature { get; set; }
    }


    public class FinishingAMissionResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int score { get; set; }
        public bool check_grant_notifications { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GettingAllMissions(Action<GettingAllMissionsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAllMissions;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, onComplete: (serverResponse) =>
            {
                GettingAllMissionsResponse response = new GettingAllMissionsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GettingAllMissionsResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: false);
        }

        public static void GettingASingleMission(LootLockerGetRequest data, Action<GettingASingleMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.current.gettingASingleMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) =>
            {
                GettingASingleMissionResponse response = new GettingASingleMissionResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GettingASingleMissionResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: false);
        }

        public static void StartingAMission(LootLockerGetRequest data, Action<StartingAMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.current.startingMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) =>
            {
                StartingAMissionResponse response = new StartingAMissionResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<StartingAMissionResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: false);
        }

        public static void FinishingAMission(FinishingAMissionRequest data, Action<FinishingAMissionResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass requestEndPoint = LootLockerEndPoints.current.finishingMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                FinishingAMissionResponse response = new FinishingAMissionResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<FinishingAMissionResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: false);
        }
    }
}
