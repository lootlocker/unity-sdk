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

    #region GettingAllMaps
    public class LootLockerGettingAllMapsToAGameResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerMap[] maps { get; set; }
    }

    [System.Serializable]
    public class LootLockerMap
    {
        public int map_id;
        public int asset_id;
        public LootLockerSpawnpoint[] spawn_points;
    }

    [System.Serializable]
    public class LootLockerSpawnpoint
    {
        public string guid;
        public int id;
        public int asset_id;
        public string position;
        public string rotation;
        public string name;
        public LootLockerAdminCamera[] cameras;
    }

    #endregion

    #region CreatingMaps

    [Serializable]
    public class LootLockerAdminCamera
    {
        public string position;
        public string rotation;

    }

    [Serializable]
    public class LootLockerCreatingMapsRequest
    {
        public string name;
        public int game_id;
        public int asset_id;
        public LootLockerSpawnpoint[] spawn_points;

    }

    [Serializable]
    public class LootLockerSpawn_Points
    {

        public string guid;
        public string position;
        public string rotation;
        public string name;
        public LootLockerAdminCamera[] cameras;
        //public bool include_guid, include_cameras;
    }

    public class LootLockerCreatingMapsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string error { get; set; }

    }

    #endregion

    #region UpdatingMaps



    #endregion

}

namespace LootLocker.Admin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void GettingAllMapsToAGame(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerGettingAllMapsToAGameResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.gettingAllMapsToAGame;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                LootLockerGettingAllMapsToAGameResponse response = new LootLockerGettingAllMapsToAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerGettingAllMapsToAGameResponse>(serverResponse.text);
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

        public static void CreatingMaps(LootLockerCreatingMapsRequest data, bool sendAssetID, bool sendSpawnPoints, Action<LootLockerCreatingMapsResponse> onComplete)
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

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, o.ToString(), (serverResponse) =>
            {
                LootLockerCreatingMapsResponse response = new LootLockerCreatingMapsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerCreatingMapsResponse>(serverResponse.text);
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

        public static void UpdatingMaps(LootLockerGetRequest lootLockerGetRequest, LootLockerCreatingMapsRequest data, Action<LootLockerCreatingMapsResponse> onComplete)
        {

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            var o = (JObject)JsonConvert.DeserializeObject(json);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.updatingMaps;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, o.ToString(), (serverResponse) =>
            {
                LootLockerCreatingMapsResponse response = new LootLockerCreatingMapsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerCreatingMapsResponse>(serverResponse.text);
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