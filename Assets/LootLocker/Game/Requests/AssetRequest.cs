using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;
using System.Linq;

namespace LootLockerRequests
{
    public class ContextResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Context[] contexts { get; set; }
    }

    public class Context
    {
        public int id { get; set; }
        public string name { get; set; }
        public string friendly_name { get; set; }
        public bool detachable { get; set; }
        public bool user_facing { get; set; }
        public object dependent_asset_id { get; set; }
    }

    public class AssetInformationResponse : LootLockerResponse
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
        public bool purchasable { get; set; }
        public string type { get; set; }
        public int price { get; set; }
        public object sales_price { get; set; }
        public object display_price { get; set; }
        public string context { get; set; }
        public object unlocks_context { get; set; }
        public bool detachable { get; set; }
        public string updated { get; set; }
        public object marked_new { get; set; }
        public int default_variation_id { get; set; }
        public Default_Loadouts_Info default_loadouts { get; set; }
        public string description { get; set; }
        public object links { get; set; }
        public object[] storage { get; set; }
        public object rarity { get; set; }
        public bool popular { get; set; }
        public int popularity_score { get; set; }
        public object package_contents { get; set; }
        public bool unique_instance { get; set; }
        public object external_identifiers { get; set; }
        public object rental_options { get; set; }
        public object[] filters { get; set; }
        public Variation_Info[] variations { get; set; }
        public bool featured { get; set; }
        public bool success { get; set; }
    }

    public class FavouritesListResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int[] favourites { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetContext(Action<ContextResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingContexts;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                ContextResponse response = new ContextResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    Debug.Log("Server Response Text: " + serverResponse.text);
                    response = JsonConvert.DeserializeObject<ContextResponse>(serverResponse.text);
                    //you are checking the wrong response. that is null because you have not assigned anything to it 
                    //data you are looking for is in serverResponse.text
                    //if you want the response.text to have the value you need, you need to assign it here 
                    response.text = serverResponse.text;
                    Debug.Log("Response is: " + serverResponse.text);
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

        public static void GetAssetListWithCount(LootLockerGetRequest data, Action<AssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetListWithCount;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
               {
                   AssetResponse response = new AssetResponse();
                   if (string.IsNullOrEmpty(serverResponse.Error))
                   {
                       LootLockerSDKManager.DebugMessage(serverResponse.text);
                       Debug.Log("Server Response Text: " + serverResponse.text);
                       response = JsonConvert.DeserializeObject<AssetResponse>(serverResponse.text);
                       //you are checking the wrong response. that is null because you have not assigned anything to it 
                       //data you are looking for is in serverResponse.text
                       //if you want the response.text to have the value you need, you need to assign it here 
                       response.text = serverResponse.text;
                       Debug.Log("Response is: " + serverResponse.text);
                       if (response != null)
                       {
                           AssetRequest.lastId = response.assets.Last()?.id != null ? response.assets.Last().id : 0;
                       }
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

        public static void GetAssetListWithAfterCount(AssetRequest data, Action<AssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetListWithAfterAndCount;

            string getVariable = string.Format(endPoint.endPoint, AssetRequest.lastId, data.count);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                AssetResponse response = new AssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetResponse>(serverResponse.text);
                    response.text = serverResponse.text;
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

        public void ResetAssetCalls()
        {
            AssetRequest.lastId = 0;
        }

        public static void GetAssetInformation(LootLockerGetRequest data, Action<AssetInformationResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetInformationForOneorMoreAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                AssetInformationResponse response = new AssetInformationResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    //we get info for one or more assets so the passed asset info should be for only one of them and the whole json is not parsed correctly
                    response = JsonConvert.DeserializeObject<AssetInformationResponse>(serverResponse.text);
                    response.text = serverResponse.text;
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

        public static void ListFavouriteAssets(Action<FavouritesListResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.listingFavouriteAssets;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                FavouritesListResponse response = new FavouritesListResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<FavouritesListResponse>(serverResponse.text);
                    response.text = serverResponse.text;
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

        public static void AddFavouriteAsset(LootLockerGetRequest data, Action<AssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetInformationForOneorMoreAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", onComplete: (serverResponse) =>
            {
                AssetResponse response = new AssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetResponse>(serverResponse.text);
                    response.text = serverResponse.text;
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

        public static void RemoveFavouriteAsset(LootLockerGetRequest data, Action<AssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetInformationForOneorMoreAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", onComplete: (serverResponse) =>
            {
                AssetResponse response = new AssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetResponse>(serverResponse.text);
                    response.text = serverResponse.text;
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