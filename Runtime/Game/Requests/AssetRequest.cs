using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using System.Linq;

namespace LootLocker.LootLockerEnums
{
    public enum AssetFilter { purchasable, nonpurchasable, rentable, nonrentable, popular, nonpopular, none }
}

namespace LootLocker.Requests
{
    public class LootLockerLinks
    {
        public string thumbnail { get; set; }
    }

    public class LootLockerDefault_Loadouts_Info
    {
        public bool Default { get; set; }
    }

    public class LootLockerVariation_Info
    {
        public int id { get; set; }
        public string name { get; set; }
        public object primary_color { get; set; }
        public object secondary_color { get; set; }
        public object links { get; set; }
    }

    [System.Serializable]
    public class LootLockerAssetRequest : LootLockerResponse
    {
        public int count;
        public static int lastId;
        public static void ResetAssetCalls()
        {
            lastId = 0;
        }
    }

    public class LootLockerAssetResponse : LootLockerResponse
    {
        public LootLockerCommonAsset[] assets { get; set; }
    }

    public class LootLockerRental_Options
    {
        public int id { get; set; }
        public string name { get; set; }
        public int duration { get; set; }
        public int price { get; set; }
        public object sales_price { get; set; }
        public object links { get; set; }
    }

    //public class LootLockerStorage
    //{
    //    public string key;
    //    public string value;
    //}

    public class LootLockerRarity
    {
        public string name { get; set; }
        public string short_name { get; set; }
        public string color { get; set; }
    }

    public class LootLockerFilter
    {
        public string value { get; set; }
        public string name { get; set; }
    }

    [System.Serializable]
    public class LootLockerCommonAsset : LootLockerResponse
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
        public bool purchasable { get; set; }
        public string type { get; set; }
        public int price { get; set; }
        public int? sales_price { get; set; }
        public string display_price { get; set; }
        public string context { get; set; }
        public string unlocks_context { get; set; }
        public bool detachable { get; set; }
        public string updated { get; set; }
        public string marked_new { get; set; }
        public int default_variation_id { get; set; }
        public string description { get; set; }
        public LootLockerLinks links { get; set; }
        public LootLockerStorage[] storage { get; set; }
        public LootLockerRarity rarity { get; set; }
        public bool popular { get; set; }
        public int popularity_score { get; set; }
        public bool unique_instance { get; set; }
        public LootLockerRental_Options[] rental_options { get; set; }
        public LootLockerFilter[] filters { get; set; }
        public LootLockerVariation[] variations { get; set; }
        public bool featured { get; set; }
        public bool context_locked { get; set; }
        public bool initially_purchasable { get; set; }
        public LootLockerFile[] files { get; set; }
        public LootLockerAssetCandidate asset_candidate { get; set; }
        public string[] data_entities { get; set; }
    }

    public class LootLockerAssetCandidate
    {
        public int created_by_player_id;
        public string created_by_player_uid;
    }

    public class LootLockerFile
    {
        public string url { get; set; }
        public string[] tags { get; set; }
    }

    public class LootLockerDefault_Loadouts
    {
        public bool Default { get; set; }
    }

    public class LootLockerVariation
    {
        public int id { get; set; }
        public string name { get; set; }
        public object primary_color { get; set; }
        public object secondary_color { get; set; }
        public object links { get; set; }
    }


    public class LootLockerContextResponse : LootLockerResponse
    {
        
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
        
        public int[] favourites { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetContext(Action<LootLockerContextResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingContexts;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerContextResponse response = new LootLockerContextResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerContextResponse>(serverResponse.text);

                //     LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetAssetsOriginal(Action<LootLockerAssetResponse> onComplete, int assetCount, int? idOfLastAsset = null, List<LootLocker.LootLockerEnums.AssetFilter> filter = null, bool includeUGC = false, Dictionary<string, string> assetFilters = null, int UGCCreatorPlayerID = 0)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAssetListWithCount;
            string getVariable = string.Format(endPoint.endPoint, assetCount);
            string tempEndpoint = string.Empty;
            string filterString = string.Empty;
            if (idOfLastAsset != null)
            {
                tempEndpoint = $"&after={idOfLastAsset}";
                getVariable += tempEndpoint;
            }

            if (filter != null)
            {
                filterString = GetStringOfEnum(filter.First());
                for (int i = 1; i < filter.Count; i++)
                {
                    filterString += "," + GetStringOfEnum(filter[i]);
                }
                tempEndpoint = $"&filter={filterString}";
                getVariable += tempEndpoint;
            }

            if (includeUGC)
            {
                tempEndpoint = $"&include_ugc={includeUGC.ToString().ToLower()}";
                getVariable += tempEndpoint;
            }

            if (UGCCreatorPlayerID > 0)
            {
                tempEndpoint = $"&ugc_creator_player_id={UGCCreatorPlayerID.ToString().ToLower()}";
                getVariable += tempEndpoint;
            }

            if (assetFilters != null)
            {
                KeyValuePair<string, string> keys = assetFilters.First();
                filterString = $"{keys.Key}={keys.Value}";
                int count = 0;
                foreach (var kvp in assetFilters)
                {
                    if (count > 0)
                        filterString += $";{kvp.Key}={kvp.Value}";
                    count++;
                }
                tempEndpoint = $"&asset_filters={filterString}";
                getVariable += tempEndpoint;
            }
            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerAssetResponse response = new LootLockerAssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);

                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);

            }, true);
        }

        public static string GetStringOfEnum(LootLocker.LootLockerEnums.AssetFilter filter)
        {
            string filterString = "";
            switch (filter)
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
            return filterString;
        }


        //public static void GetAssetListWithCount(LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        //{
        //    EndPointClass endPoint = LootLockerEndPoints.gettingAssetListWithCount;

        //    string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

        //    LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
        //       {
        //           LootLockerAssetResponse response = new LootLockerAssetResponse();
        //           if (string.IsNullOrEmpty(serverResponse.Error))
        //           {
        //               response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);
        //               if (response != null && response.assets.Length > 0)
        //                   LootLockerAssetRequest.lastId = response.assets.Last()?.id != null ? response.assets.Last().id : 0;
        //           }

        //           //     LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
        //           response.text = serverResponse.text;
        //           response.status = serverResponse.status;
        //           response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
        //           onComplete?.Invoke(response);
        //       }, true);
        //}

        //public static void GetAssetListWithAfterCount(LootLockerAssetRequest data, Action<LootLockerAssetResponse> onComplete)
        //{
        //    EndPointClass endPoint = LootLockerEndPoints.gettingAssetListWithAfterAndCount;

        //    string getVariable = string.Format(endPoint.endPoint, LootLockerAssetRequest.lastId, data.count);

        //    LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
        //    {
        //        LootLockerAssetResponse response = new LootLockerAssetResponse();
        //        if (string.IsNullOrEmpty(serverResponse.Error))
        //        {
        //            response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);

        //            if (response != null && response.assets.Length > 0)
        //                LootLockerAssetRequest.lastId = response.assets.Last()?.id != null ? response.assets.Last().id : 0;
        //        }
        //        //     LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
        //        response.text = serverResponse.text;
        //        response.status = serverResponse.status;
        //        response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
        //        onComplete?.Invoke(response);
        //    }, true);
        //}

        public static void GetAssetsById(LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getAssetsById;

            string builtAssets = data.getRequests.First();

            if (data.getRequests.Count > 0)
                for (int i = 1; i < data.getRequests.Count; i++)
                    builtAssets += "," + data.getRequests[i];


            string getVariable = string.Format(endPoint.endPoint, builtAssets);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerAssetResponse response = new LootLockerAssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);

                //     LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public void ResetAssetCalls()
        {
            LootLockerAssetRequest.lastId = 0;
        }

        public static void GetAssetInformation(LootLockerGetRequest data, Action<LootLockerCommonAsset> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAssetInformationForOneorMoreAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerCommonAsset response = new LootLockerCommonAsset();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerCommonAsset>(serverResponse.text);

                //    LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void ListFavouriteAssets(Action<LootLockerFavouritesListResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listingFavouriteAssets;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerFavouritesListResponse response = new LootLockerFavouritesListResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerFavouritesListResponse>(serverResponse.text);

                //     LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void AddFavouriteAsset(LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.addingFavouriteAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", onComplete: (serverResponse) =>
            {
                LootLockerAssetResponse response = new LootLockerAssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);

                //   LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void RemoveFavouriteAsset(LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.removingFavouriteAssets;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", onComplete: (serverResponse) =>
            {
                LootLockerAssetResponse response = new LootLockerAssetResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerAssetResponse>(serverResponse.text);

                // LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

    }

}