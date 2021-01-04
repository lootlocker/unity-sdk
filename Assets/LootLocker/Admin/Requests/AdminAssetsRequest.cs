using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerAdmin;
using LootLockerAdminRequests;
using Newtonsoft.Json;
using System;

namespace LootLockerAdminRequests
{
    public class GetAssetsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Asset[] assets { get; set; }
    }

    public class CreateAssetRequest
    {
        public string name;
        public int context_id;
    }

    public class CreateAssetResponse : LootLockerResponse
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

    public class GetContextsResponse : LootLockerResponse
    {

        public bool success { get; set; }
        public Context[] Contexts { get; set; }

    }

    public class Context
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

namespace LootLockerAdmin
{
    public partial class LootLockerAPIManagerAdmin
    {
        public static void GetAssets(Action<GetAssetsResponse> onComplete, string search = null)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getAllAssets;
            string getVariable = string.Format(endPoint.endPoint, BaseServerAPI.activeConfig.gameID);

            if (!string.IsNullOrEmpty(search))
                getVariable += "?search=" + search;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
             {
                 var response = new GetAssetsResponse();
                 if (string.IsNullOrEmpty(serverResponse.Error))
                 {
                     response = JsonConvert.DeserializeObject<GetAssetsResponse>(serverResponse.text);
                     response.text = serverResponse.text;
                     onComplete?.Invoke(response);
                 }
                 else
                 {
                     response.message = serverResponse.message;
                     response.Error = serverResponse.Error;
                     onComplete?.Invoke(response);
                 }
             }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin); 
        }

        public static void CreateAsset(CreateAssetRequest request, Action<CreateAssetResponse> onComplete)
        {
            var json = JsonConvert.SerializeObject(request);
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.createAsset;

            string getVariable = string.Format(endPoint.endPoint, LootLockerAdminConfig.current.gameID);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
             {
                 Debug.Log("--------------------");
                 Debug.Log(serverResponse.text);
                 var response = new CreateAssetResponse();
                 if (string.IsNullOrEmpty(serverResponse.Error))
                 {
                     try
                     {
                         response = JsonConvert.DeserializeObject<CreateAssetResponse>(serverResponse.text);
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
             }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);

        }

        public static void GetContexts(Action<GetContextsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getContexts;
            string getVariable = string.Format(endPoint.endPoint, LootLockerAdminConfig.current.gameID);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                var response = new GetContextsResponse();

                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetContextsResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);
        }

    }
}