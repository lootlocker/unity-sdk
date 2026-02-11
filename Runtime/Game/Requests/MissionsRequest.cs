using System;
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
    public class LootLockerCheckpointTimes
    {
        public int index { get; set; }
        public int time { get; set; }
        public int score { get; set; }
    }

    public class LootLockerFinishMissionRequest : LootLockerGetRequest
    {
        public string signature { get; set; }
        public LootLockerFinishingPayload payload { get; set; }
    }
    
    public class LootLockerGetAllMissionsResponse : LootLockerResponse
    {
        public LootLockerMission[] missions { get; set; }
    }

    public class LootLockerGetMissionResponse : LootLockerResponse
    {
        public LootLockerMission mission { get; set; }
    }

    public class LootLockerStartMissionResponse : LootLockerResponse
    {
        public string signature { get; set; }
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
        public static void GetAllMissions(string forPlayerWithUlid, Action<LootLockerGetAllMissionsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAllMissions;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        public static void GetMission(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerGetMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.gettingASingleMission;

            string endPoint = requestEndPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        public static void StartMission(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerStartMissionResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.startingMission;

            string endPoint = requestEndPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        public static void FinishMission(string forPlayerWithUlid, LootLockerFinishMissionRequest data, Action<LootLockerFinishMissionResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerFinishMissionResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass requestEndPoint = LootLockerEndPoints.finishingMission;

            string endPoint = requestEndPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false);
        }
    }
}