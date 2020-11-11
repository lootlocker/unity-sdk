using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerAdmin;
using LootLockerAdminRequests;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;

namespace LootLockerAdminRequests
{

    #region GettingAllMaps
    public class GettingAllMapsToAGameResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Map[] maps { get; set; }
    }

    [System.Serializable]
    public class Map
    {
        public int map_id;
        public int asset_id;
        public Spawnpoint[] spawn_points;
    }

    [System.Serializable]
    public class Spawnpoint
    {
        public string guid;
        public int id;
        public int asset_id;
        public string position;
        public string rotation;
        public string name;
        public AdminCamera[] cameras;
    }

    #endregion

    #region CreatingMaps

    [Serializable]
    public class AdminCamera
    {
        public string position;
        public string rotation;

    }

    [Serializable]
    public class CreatingMapsRequest
    {
        public string name;
        public int game_id;
        public int asset_id;
        public Spawnpoint[] spawn_points;

    }

    [Serializable]
    public class Spawn_Points
    {

        public string guid;
        public string position;
        public string rotation;
        public string name;
        public AdminCamera[] cameras;
        //public bool include_guid, include_cameras;
    }

    public class CreatingMapsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string error { get; set; }

    }

    #endregion

    #region UpdatingMaps



    #endregion

}

namespace LootLockerAdmin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void GettingAllMapsToAGame(LootLockerGetRequest lootLockerGetRequest, Action<GettingAllMapsToAGameResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.gettingAllMapsToAGame;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                GettingAllMapsToAGameResponse response = new GettingAllMapsToAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GettingAllMapsToAGameResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: enums.CallerRole.Admin);

        }

        public static void CreatingMaps(CreatingMapsRequest data, bool sendAssetID, bool sendSpawnPoints, Action<CreatingMapsResponse> onComplete)
        {

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            var o = (JObject)JsonConvert.DeserializeObject(json);

            if (!sendAssetID)
                o.Property("asset_id").Remove();

            if (!sendSpawnPoints)
                o.Property("spawn_points").Remove();

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.creatingMaps;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, o.ToString(), (serverResponse) =>
            {
                CreatingMapsResponse response = new CreatingMapsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CreatingMapsResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: enums.CallerRole.Admin);
        }

        public static void UpdatingMaps(LootLockerGetRequest lootLockerGetRequest, CreatingMapsRequest data, Action<CreatingMapsResponse> onComplete)
        {

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            var o = (JObject)JsonConvert.DeserializeObject(json);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.updatingMaps;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, o.ToString(), (serverResponse) =>
            {
                CreatingMapsResponse response = new CreatingMapsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CreatingMapsResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: enums.CallerRole.Admin);
        }



    }

}