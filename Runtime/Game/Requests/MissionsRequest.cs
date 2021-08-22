using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    public class LootLockerMission
    {
        public int mission_id { get; set; }
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

    public class LootLockerFinishingPayload
    {
        public string finish_time { get; set; }
        public string finish_score { get; set; }
        public LootLockerCheckpointTimes[] checkpoint_times { get; set; }
    }

    public class LootLockerFinishingAMissionRequest : LootLockerGetRequest
    {
        public string signature { get; set; }
        public LootLockerFinishingPayload payload { get; set; }
    }

    public class LootLockerGettingAllMissionsResponse : LootLockerResponse
    {
        
        public LootLockerMission[] missions { get; set; }
    }

    public class LootLockerGettingASingleMissionResponse : LootLockerResponse
    {
        
        public LootLockerMission mission { get; set; }
    }


    public class LootLockerStartingAMissionResponse : LootLockerResponse
    {
        
        public string signature { get; set; }
    }


    public class LootLockerFinishingAMissionResponse : LootLockerResponse
    {
        
        public int score { get; set; }
        public bool check_grant_notifications { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GettingAllMissions(Action<LootLockerGettingAllMissionsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAllMissions;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, onComplete: (serverResponse) =>
            {
                LootLockerGettingAllMissionsResponse response = new LootLockerGettingAllMissionsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGettingAllMissionsResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, useAuthToken: false);
        }

        public static void GettingASingleMission(LootLockerGetRequest data, Action<LootLockerGettingASingleMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.gettingASingleMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) =>
            {
                LootLockerGettingASingleMissionResponse response = new LootLockerGettingASingleMissionResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGettingASingleMissionResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, useAuthToken: false);
        }

        public static void StartingAMission(LootLockerGetRequest data, Action<LootLockerStartingAMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.startingMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) =>
            {
                LootLockerStartingAMissionResponse response = new LootLockerStartingAMissionResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerStartingAMissionResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, useAuthToken: false);
        }

        public static void FinishingAMission(LootLockerFinishingAMissionRequest data, Action<LootLockerFinishingAMissionResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass requestEndPoint = LootLockerEndPoints.finishingMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) =>
            {
                LootLockerFinishingAMissionResponse response = new LootLockerFinishingAMissionResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerFinishingAMissionResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, useAuthToken: false);
        }
    }
}
