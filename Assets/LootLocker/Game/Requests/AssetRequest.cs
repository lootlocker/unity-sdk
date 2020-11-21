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
                    response = JsonConvert.DeserializeObject<ContextResponse>(serverResponse.text);
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

        public static void GetAssetsOriginal(Action<AssetResponse> onComplete, int assetCount, int? idOfLastAsset = null, Enums.AssetFilter filter = Enums.AssetFilter.none)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetListWithCount;
            string getVariable = string.Format(endPoint.endPoint, assetCount);

            if (idOfLastAsset != null && assetCount > 0) 
            {
                endPoint = LootLockerEndPoints.current.gettingAssetListWithAfterAndCount;
                getVariable = string.Format(endPoint.endPoint, assetCount, idOfLastAsset.ToString());
            }
            else if (idOfLastAsset != null && assetCount > 0 && filter!= Enums.AssetFilter.none)
            {
                endPoint = LootLockerEndPoints.current.gettingAssetListOriginal;
                string filterString = "";
                switch(filter)
                {
                    case Enums.AssetFilter.purchasable:
                        filterString = Enums.AssetFilter.purchasable.ToString();
                        break;
                    case Enums.AssetFilter.nonpurchasable:
                        filterString = "!purchasable";
                        break;
                    case Enums.AssetFilter.rentable:
                        filterString = Enums.AssetFilter.rentable.ToString();
                        break;
                    case Enums.AssetFilter.nonrentable:
                        filterString = "!rentable";
                        break;
                    case Enums.AssetFilter.popular:
                        filterString = Enums.AssetFilter.popular.ToString();
                        break;
                    case Enums.AssetFilter.nonpopular:
                        filterString = "!popular";
                        break;
                }
                getVariable = string.Format(endPoint.endPoint, assetCount, idOfLastAsset.ToString(), filterString);
            }

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                AssetResponse response = new AssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<AssetResponse>(serverResponse.text);
                    response.text = serverResponse.text;
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
                       response = JsonConvert.DeserializeObject<AssetResponse>(serverResponse.text);
                       response.text = serverResponse.text;
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

        public static void GetAssetsById(LootLockerGetRequest data, Action<AssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getAssetsById;

            string builtAssets = data.getRequests.First();

            if (data.getRequests.Count > 0)
                for (int i = 1; i < data.getRequests.Count; i++)
                    builtAssets += "," + data.getRequests[i];


            string getVariable = string.Format(endPoint.endPoint, builtAssets);

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

        public static void GetAssetInformation(LootLockerGetRequest data, Action<Asset> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingAssetInformationForOneorMoreAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                Asset response = new Asset();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<Asset>(serverResponse.text);
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
            EndPointClass endPoint = LootLockerEndPoints.current.addingFavouriteAssets;

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
            EndPointClass endPoint = LootLockerEndPoints.current.removingFavouriteAssets;

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