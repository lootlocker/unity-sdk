using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLockerRequests;
using LootLocker;
using System;
using Newtonsoft.Json;

namespace LootLockerRequests
{
    [Serializable]
    public class MapsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Map[] maps { get; set; }
    }

    [Serializable]
    public class Map
    {
        public int map_id;
        public int asset_id;
        public Spawn_Points[] spawn_points;
        public bool player_access;
    }

    [Serializable]
    public class Spawn_Points
    {
        public int asset_id;
        public string position;
        public string rotation;
        public Camera[] cameras;
        public bool player_access;
    }

    [Serializable]
    public class Camera
    {
        public string position;
        public string rotation;
    }
}


namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GettingAllMaps(Action<MapsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAllMaps;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                MapsResponse response = new MapsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<MapsResponse>(serverResponse.text);
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