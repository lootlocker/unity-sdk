using System;
using System.Collections.Generic;
using System.Linq;
using LootLocker.Requests;

namespace LootLocker.LootLockerEnums
{
    public enum AssetFilter
    {
        purchasable,
        nonpurchasable,
        rentable,
        nonrentable,
        popular,
        nonpopular,
        none
    }
}

namespace LootLocker.Requests
{
    public class LootLockerLinks : Dictionary<string, string>
    {
        public string thumbnail
        {
            get
            {
                TryGetValue(nameof(thumbnail), out var value);
                return value;
            }
            set
            {
                if (ContainsKey(nameof(thumbnail)))
                {
                    this[nameof(thumbnail)] = value;
                }
                else
                {
                    Add(nameof(thumbnail), value);
                }
            }
        }
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

    [Serializable]
    public class LootLockerAssetRequest : LootLockerResponse
    {
        public int count { get; set; }
        public static int lastId { get; set; }

        public static void ResetAssetCalls()
        {
            lastId = 0;
        }
    }

    /// <summary>
    /// Request object for listing assets with settings for what to include, exclude, and filter.
    /// </summary>
    public class LootLockerListAssetsRequest
    {
        /// <summary>Fields to include in the response.</summary>
        public LootLockerAssetIncludes includes { get; set; } = new LootLockerAssetIncludes();
        /// <summary>Fields to exclude from the response.</summary>
        public LootLockerAssetExcludes excludes { get; set; } = new LootLockerAssetExcludes();
        /// <summary>Filters to apply to the asset listing.</summary>
        public LootLockerAssetFilters filters { get; set; } = new LootLockerAssetFilters();
    }

    /// <summary>
    /// Fields to include in the asset response.
    /// </summary>
    public class LootLockerAssetIncludes
    {
        ///<summary>If set to true, response will include storage key-value pairs.</summary>
        public bool storage { get; set; } = false;
        ///<summary>If set to true, response will include files.</summary>
        public bool files { get; set; } = false;
        ///<summary>If set to true, response will include asset data entities.</summary>
        public bool data_entities { get; set; } = false;
        ///<summary>If set to true, response will include asset metadata.</summary>
        public bool metadata { get; set; } = false;
    }

    /// <summary>
    /// Fields to exclude from the asset response.
    /// </summary>
    public class LootLockerAssetExcludes
    {
        ///<summary>If set to true, UGC assets authors will not be returned.</summary>
        public bool authors { get; set; } = false;
    }

    /// <summary>
    /// Filters to apply to the asset listing.
    /// </summary>
    public class LootLockerAssetFilters
    {
        ///<summary>If set to true, response will include only UGC assets.</summary>
        public bool ugc_only { get; set; } = false;
        ///<summary>If provided, only the requested ids will be returned. No pagination will be attempted or respected, maximum 100 assets.</summary>
        public List<int> asset_ids { get; set; } = new List<int>();
    }

    /// <summary>
    /// Response object for listing assets from the simple asset endpoint.
    /// </summary>
    [Serializable]
    public class LootLockerListAssetsResponse : LootLockerResponse
    {
        /// <summary>List of assets returned by the endpoint.</summary>
        public LootLockerSimpleAsset[] assets { get; set; }

        /// <summary>Pagination data for this request</summary>
        public LootLockerExtendedPagination pagination { get; set; }
    }

    public class LootLockerGrantAssetRequest
    {
        public int asset_id { get; set; }
        public int? asset_variation_id { get; set; }
        public int? asset_rental_option_id { get; set; }
    }

    public class LootLockerAssetResponse : LootLockerResponse
    {
        public LootLockerCommonAsset[] assets { get; set; }
    }

    public class LootLockerSingleAssetResponse : LootLockerResponse
    {
        public LootLockerCommonAsset asset { get; set; }
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

    [Serializable]
    public class LootLockerCommonAsset
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string ulid { get; set; }
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

    [Serializable]
    public class LootLockerCommonAssetResponse : LootLockerResponse
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string ulid { get; set; }
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

    /// <summary>
    /// A simplified asset object to improve performance
    /// </summary>
    [Serializable]
    public class LootLockerSimpleAsset
    {
        public int asset_id { get; set; }
        public string asset_uuid { get; set; }
        public string asset_ulid { get; set; }
        public string asset_name { get; set; }
        public int context_id { get; set; }
        public string context_name { get; set; }
        public LootLockerSimpleAssetAuthor author { get; set; }
        public LootLockerStorage[] storage { get; set; }
        public LootLockerSimpleAssetFile[] files { get; set; }
        public LootLockerSimpleAssetDataEntity[] data_entities { get; set; }
        public LootLockerMetadataEntry[] metadata { get; set; }
    }

    public class LootLockerSimpleAssetAuthor
    {
        public int player_id { get; set; }
        public string player_ulid { get; set; }
        public string public_uid { get; set; }
        public string active_name { get; set; }
    }

    public class LootLockerSimpleAssetFile
    {
        public int size { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string[] tags { get; set; }
    }

    public class LootLockerSimpleAssetDataEntity
    {
        public string name { get; set; }
        public string data { get; set; }
    }

    public class LootLockerAssetCandidate
    {
        public int created_by_player_id { get; set; }
        public string created_by_player_uid { get; set; }
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
        public Dictionary<string, string> links { get; set; }
    }


    public class LootLockerContextResponse : LootLockerResponse
    {
        public LootLockerContext[] contexts { get; set; }
    }

    public class LootLockerContext
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string name { get; set; }
        public string friendly_name { get; set; }
        public bool detachable { get; set; }
        public bool user_facing { get; set; }
        public object dependent_asset_id { get; set; }
        public int max_equip_count { get; set; }
    }

    public class LootLockerFavouritesListResponse : LootLockerResponse
    {
        public int[] favourites { get; set; }
    }

    public class LootLockerActivateRentalAssetResponse : LootLockerResponse
    {
        public int time_left { get; set; }
    }

    public class LootLockerGrantAssetResponse : LootLockerResponse
    {
        public int id { get; set; }
        public int asset_id { get; set; }
        public string asset_ulid { get; set; }
        public int? asset_variation_id { get; set; }
        public int? asset_rental_option_id { get; set; }
        public string acquisition_source { get; set; }
        public string acquisition_date { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetContext(string forPlayerWithUlid, Action<LootLockerContextResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingContexts;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetAssetsOriginal(string forPlayerWithUlid, Action<LootLockerAssetResponse> onComplete, int assetCount, int? idOfLastAsset = null, List<LootLocker.LootLockerEnums.AssetFilter> filter = null, bool includeUGC = false, Dictionary<string, string> assetFilters = null,
            int UGCCreatorPlayerID = 0)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAssetListWithCount;
            string getVariable = endPoint.WithPathParameter(assetCount);
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

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
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

        public static void GetAssetsById(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getAssetsById;

            string builtAssets = data.getRequests.First();

            if (data.getRequests.Count > 0)
                for (int i = 1; i < data.getRequests.Count; i++)
                    builtAssets += "," + data.getRequests[i];


            string getVariable = endPoint.WithPathParameter(builtAssets);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetAssetById(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerSingleAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getAssetsById;

            string builtAssets = data.getRequests.First();

            if (data.getRequests.Count > 0)
                for (int i = 1; i < data.getRequests.Count; i++)
                    builtAssets += "," + data.getRequests[i];

            string getVariable = endPoint.WithPathParameter(builtAssets);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (Action<LootLockerResponse>)((serverResponse) => {

                LootLockerAssetResponse assetListResponse = LootLockerResponse.Deserialize<LootLockerAssetResponse>(serverResponse);
                LootLockerSingleAssetResponse singleAssetResponse = new LootLockerSingleAssetResponse();
                singleAssetResponse.success = assetListResponse.success;
                singleAssetResponse.statusCode = assetListResponse.statusCode;
                singleAssetResponse.errorData = assetListResponse.errorData;
                singleAssetResponse.EventId = assetListResponse.EventId;
                singleAssetResponse.asset = assetListResponse.assets != null && assetListResponse.assets.Count() > 0 ? assetListResponse.assets[0] : null;
                string serializedSingleAssetResponse = LootLockerJson.SerializeObject(singleAssetResponse);
                singleAssetResponse.text = serializedSingleAssetResponse;

                onComplete?.Invoke(singleAssetResponse);
            }));
        }

        public static void GetAssetInformation(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerCommonAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAssetInformationForOneorMoreAssets;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListFavouriteAssets(string forPlayerWithUlid, Action<LootLockerFavouritesListResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listingFavouriteAssets;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void AddFavouriteAsset(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.addingFavouriteAssets;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, "", onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void RemoveFavouriteAsset(string forPlayerWithUlid, LootLockerGetRequest data, Action<LootLockerAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.removingFavouriteAssets;

            string getVariable = endPoint.WithPathParameter(data.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, "", onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GrantAssetToPlayerInventory(string forPlayerWithUlid, LootLockerGrantAssetRequest data, Action<LootLockerGrantAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.grantAssetToPlayerInventory;

            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

    }
}
