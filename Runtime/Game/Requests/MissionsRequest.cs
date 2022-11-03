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
        public int? difficulty_id { get; set; }
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

    [Obsolete("This class is deprecated and will be removed at a later stage. Please use LootLockerFinishMissionRequest instead")]
    public class LootLockerFinishingAMissionRequest : LootLockerGetRequest
    {
        public string signature { get; set; }
        public LootLockerFinishingPayload payload { get; set; }
    }

    public class LootLockerFinishMissionRequest : LootLockerGetRequest
    {
        public string signature { get; set; }
        public LootLockerFinishingPayload payload { get; set; }
    }

    [Obsolete("This class is deprecated and will be removed at a later stage. Please use LootLockerGetAllMissionsResponse instead")]
    public class LootLockerGettingAllMissionsResponse : LootLockerResponse
    {
        public LootLockerMission[] missions { get; set; }
    }
    
    public class LootLockerGetAllMissionsResponse : LootLockerResponse
    {
        public LootLockerMission[] missions { get; set; }
    }

    [Obsolete("This class is deprecated and will be removed at a later stage. Please use LootLockerGetMissionResponse isntead")]
    public class LootLockerGettingASingleMissionResponse : LootLockerResponse
    {
        public LootLockerMission mission { get; set; }
    }

    public class LootLockerGetMissionResponse : LootLockerResponse
    {
        public LootLockerMission mission { get; set; }
    }

    [Obsolete("This class is deprecated and will be removed at a later stage. Please use LootLockerStartMissionResponse instead")]
    public class LootLockerStartingAMissionResponse : LootLockerResponse
    {
        public string signature { get; set; }
    }

    public class LootLockerStartMissionResponse : LootLockerResponse
    {
        public string signature { get; set; }
    }


    [Obsolete("This class is deprecated and will be removed at a later stage. Please use LootLockerFinishMissionResponse instead")]
    public class LootLockerFinishingAMissionResponse : LootLockerResponse
    {
        public int score { get; set; }
        public bool check_grant_notifications { get; set; }
    }

    public class LootLockerFinishMissionResponse : LootLockerResponse
    {
        public int score { get; set; }
        public bool check_grant_notifications { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetAllMissions(Action<LootLockerGetAllMissionsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAllMissions;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetAllMissions() instead")]
        public static void GettingAllMissions(Action<LootLockerGettingAllMissionsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAllMissions;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        public static void GetMission(LootLockerGetRequest data, Action<LootLockerGetMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.gettingASingleMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function GetMission() instead")]
        public static void GettingASingleMission(LootLockerGetRequest data, Action<LootLockerGettingASingleMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.gettingASingleMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        public static void StartMission(LootLockerGetRequest data, Action<LootLockerStartMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.startingMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function StartMission() instead")]
        public static void StartingAMission(LootLockerGetRequest data, Action<LootLockerStartingAMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.startingMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        public static void FinishMission(LootLockerFinishMissionRequest data, Action<LootLockerFinishMissionResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass requestEndPoint = LootLockerEndPoints.finishingMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: false);
        }
        [Obsolete("This function is deprecated and will be removed soon. Please use the function FinishMission() instead")]
        public static void FinishingAMission(LootLockerFinishingAMissionRequest data, Action<LootLockerFinishingAMissionResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass requestEndPoint = LootLockerEndPoints.finishingMission;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: false);
        }
    }
}