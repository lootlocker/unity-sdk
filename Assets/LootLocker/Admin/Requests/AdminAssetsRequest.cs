using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Admin;
using LootLocker.Admin.Requests;
using Newtonsoft.Json;
using System;

namespace LootLocker.Admin.Requests
{
    public class LootLockerGetAssetsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerCommonAsset[] assets { get; set; }
    }

    public class LootLockerCreateAssetRequest
    {
        public string name;
        public int context_id;
    }

    public class LootLockerCreateAssetResponse : LootLockerResponse
    {

        public bool success { get; set; }
        //todo the deserializer can't handle null passed to not nullable structures

        //private Asset assetValue;
        //public Asset Asset {

        //    get { return assetValue; }   // get method

        //    set {
        //        try
        //        {
        //            Debug.Log("Setting asset value");
        //            assetValue = value;
        //        }
        //        catch (Exception ex)
        //        { 
        //            Debug.LogWarning("Couldn't deserialize asset. " + ex);
        //        }
        //    }

        //}

    }

    public class LootLockerGetContextsResponse : LootLockerResponse
    {

        public bool success { get; set; }
        public LootLockerContext[] Contexts { get; set; }

    }

    public class LootLockerContext
    {
        public int id { get; set; }
        public string name { get; set; }
        public string friendly_name { get; set; }
        public bool detachable { get; set; }
        public bool user_facing { get; set; }
        public bool dependent_asset_id { get; set; }
        public bool editable { get; set; }
    }

}

namespace LootLocker.Admin
{
    public partial class LootLockerAPIManagerAdmin
    {
        public static void GetAssets(Action<LootLockerGetAssetsResponse> onComplete, string search = null)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getAllAssets;
            string getVariable = string.Format(endPoint.endPoint, LootLockerBaseServerAPI.activeConfig.gameID);

            if (!string.IsNullOrEmpty(search))
                getVariable += "?search=" + search;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
             {
                 var response = new LootLockerGetAssetsResponse();
                 if (string.IsNullOrEmpty(serverResponse.Error))
                 {
                     response = JsonConvert.DeserializeObject<LootLockerGetAssetsResponse>(serverResponse.text);
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

        public static void CreateAsset(LootLockerCreateAssetRequest request, Action<LootLockerCreateAssetResponse> onComplete)
        {
            var json = JsonConvert.SerializeObject(request);
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.createAsset;

            string getVariable = string.Format(endPoint.endPoint, LootLockerAdminConfig.current.gameID);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
             {
                 Debug.Log("--------------------");
                 Debug.Log(serverResponse.text);
                 var response = new LootLockerCreateAssetResponse();
                 if (string.IsNullOrEmpty(serverResponse.Error))
                 {
                     try
                     {
                         response = JsonConvert.DeserializeObject<LootLockerCreateAssetResponse>(serverResponse.text);
                     }
                     catch (System.InvalidCastException)
                     {
                         Debug.LogError("The reponse is not valide");
                         throw;
                     }
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

        public static void GetContexts(Action<LootLockerGetContextsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getContexts;
            string getVariable = string.Format(endPoint.endPoint, LootLockerAdminConfig.current.gameID);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                var response = new LootLockerGetContextsResponse();

                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerGetContextsResponse>(serverResponse.text);
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