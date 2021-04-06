using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using LootLocker;
using System;
using LootLocker.Newtonsoft.Json;

namespace LootLocker.Requests
{
    [Serializable]
    public class LootLockerMapsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerMap[] maps { get; set; }
    }

    [Serializable]
    public class LootLockerMap
    {
        public int map_id;
        public int asset_id;
        public LootLockerSpawn_Points[] spawn_points;
        public bool player_access;
    }

    [Serializable]
    public class LootLockerSpawn_Points
    {
        public int asset_id;
        public string position;
        public string rotation;
        public LootLockerCamera[] cameras;
        public bool player_access;
    }

    [Serializable]
    public class LootLockerCamera
    {
        public string position;
        public string rotation;
    }
}


namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GettingAllMaps(Action<LootLockerMapsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAllMaps;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerMapsResponse response = new LootLockerMapsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerMapsResponse>(serverResponse.text);
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