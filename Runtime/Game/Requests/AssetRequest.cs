using LootLocker.Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using System.Linq;

namespace LootLocker.Requests
{
    public class LootLockerContextResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerContext[] contexts { get; set; }
    }

    public class LootLockerContext
    {
        public int id { get; set; }
        public string name { get; set; }
        public string friendly_name { get; set; }
        public bool detachable { get; set; }
        public bool user_facing { get; set; }
        public object dependent_asset_id { get; set; }
    }

    public class LootLockerFavouritesListResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int[] favourites { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetContext(Action<LootLockerContextResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingContexts;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerContextResponse response = new LootLockerContextResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerContextResponse>(serverResponse.text);
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

        public static void GetAssetsOriginal(Action<LootLockerAssetResponse> onComplete, int assetCount, int? idOfLastAsset = null, LootLocker.LootLockerEnums.AssetFilter filter = LootLocker.LootLockerEnums.AssetFilter.none)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetListWithCount;
            string getVariable = string.Format(endPoint.endPoint, assetCount);

            if (idOfLastAsset != null && assetCount > 0) 
            {
                endPoint = LootLockerEndPoints.current.gettingAssetListWithAfterAndCount;
                getVariable = string.Format(endPoint.endPoint, assetCount, idOfLastAsset.ToString());
            }
            else if (idOfLastAsset != null && assetCount > 0 && filter!= LootLocker.LootLockerEnums.AssetFilter.none)
            {
                endPoint = LootLockerEndPoints.current.gettingAssetListOriginal;
                string filterString = "";
                switch(filter)
                {
                    case LootLocker.LootLockerEnums.AssetFilter.purchasable:
                        filterString = LootLocker.LootLockerEnums.AssetFilter.purchasable.ToString();
                        break;
                    case LootLocker.LootLockerEnums.AssetFilter.nonpurchasable:
                        filterString = "!purchasable";
                        break;
                    case LootLocker.LootLockerEnums.AssetFilter.rentable:
                        filterString = LootLocker.LootLockerEnums.AssetFilter.rentable.ToString();
                        break;
                    case LootLocker.LootLockerEnums.AssetFilter.nonrentable:
                        filterString = "!rentable";
                        break;
                    case LootLocker.LootLockerEnums.AssetFilter.popular:
                        filterString = LootLocker.LootLockerEnums.AssetFilter.popular.ToString();
                        break;
                    case LootLocker.LootLockerEnums.AssetFilter.nonpopular:
                        filterString = "!popular";
                        break;
                }
                getVariable = string.Format(endPoint.endPoint, assetCount, idOfLastAsset.ToString(), filterString);
            }

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerAssetResponse response = new LootLockerAssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    if (response != null)
                    {
                        LootLockerAssetRequest.lastId = response.assets.Last()?.id != null ? response.assets.Last().id : 0;
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

        public static void GetAssetListWithCount(LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetListWithCount;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
               {
                   LootLockerAssetResponse response = new LootLockerAssetResponse();
                   if (string.IsNullOrEmpty(serverResponse.Error))
                   {
                       LootLockerSDKManager.DebugMessage(serverResponse.text);
                       response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);
                       response.text = serverResponse.text;
                       if (response != null)
                       {
                           LootLockerAssetRequest.lastId = response.assets.Last()?.id != null ? response.assets.Last().id : 0;
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

        public static void GetAssetListWithAfterCount(LootLockerAssetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetListWithAfterAndCount;

            string getVariable = string.Format(endPoint.endPoint, LootLockerAssetRequest.lastId, data.count);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerAssetResponse response = new LootLockerAssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);
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

        public static void GetAssetsById(LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getAssetsById;

            string builtAssets = data.getRequests.First();

            if (data.getRequests.Count > 0)
                for (int i = 1; i < data.getRequests.Count; i++)
                    builtAssets += "," + data.getRequests[i];


            string getVariable = string.Format(endPoint.endPoint, builtAssets);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerAssetResponse response = new LootLockerAssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);
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
            LootLockerAssetRequest.lastId = 0;
        }

        public static void GetAssetInformation(LootLockerGetRequest data, Action<LootLockerCommonAsset> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetInformationForOneorMoreAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerCommonAsset response = new LootLockerCommonAsset();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerCommonAsset>(serverResponse.text);
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

        public static void ListFavouriteAssets(Action<LootLockerFavouritesListResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.listingFavouriteAssets;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerFavouritesListResponse response = new LootLockerFavouritesListResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerFavouritesListResponse>(serverResponse.text);
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

        public static void AddFavouriteAsset(LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.addingFavouriteAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", onComplete: (serverResponse) =>
            {
                LootLockerAssetResponse response = new LootLockerAssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);
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

        public static void RemoveFavouriteAsset(LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.removingFavouriteAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", onComplete: (serverResponse) =>
            {
                LootLockerAssetResponse response = new LootLockerAssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);
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