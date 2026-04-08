using System;
using System.Collections.Generic;
using System.Linq;
using LootLocker.Requests;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// Filter criteria for listing assets based on purchasability, rentability, or popularity.
    /// </summary>
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

    /// <summary>
    /// The field by which to order an asset list response.
    /// </summary>
    public enum OrderAssetListBy
    {
        /// <summary>Do not apply a specific ordering.</summary>
        none,
        /// <summary>Order by asset ID.</summary>
        id,
        /// <summary>Order by asset name.</summary>
        name,
        /// <summary>Order by when the asset was created.</summary>
        created_at,
        /// <summary>Order by when the asset was last updated.</summary>
        updated_at,
    }

    /// <summary>
    /// The direction in which to order an asset list response.
    /// </summary>
    public enum OrderAssetListDirection
    {
        /// <summary>Do not apply a specific direction.</summary>
        none,
        /// <summary>Order ascending (lowest to highest).</summary>
        asc,
        /// <summary>Order descending (highest to lowest).</summary>
        desc
    }
}

namespace LootLocker.Requests
{

    /// <summary>
    /// Indicates whether an asset is part of the default loadout configuration.
    /// </summary>
    public class LootLockerDefault_Loadouts_Info
    {
        /// <summary>Whether this entry represents a default loadout asset.</summary>
        public bool Default { get; set; }
    }

    /// <summary>
    /// Variation information for a legacy asset, containing the id, name, colors, and links.
    /// </summary>
    public class LootLockerVariation_Info
    {
        /// <summary>The unique identifier of the variation.</summary>
        public int id { get; set; }
        /// <summary>The display name of the variation.</summary>
        public string name { get; set; }
        /// <summary>The primary color of the variation, or null if not set.</summary>
        public object primary_color { get; set; }
        /// <summary>The secondary color of the variation, or null if not set.</summary>
        public object secondary_color { get; set; }
        /// <summary>Links associated with the variation.</summary>
        public object links { get; set; }
    }

    /// <summary>
    /// Request object used internally to track pagination state for legacy asset list calls.
    /// </summary>
    [Serializable]
    public class LootLockerAssetRequest : LootLockerResponse
    {
        /// <summary>The number of assets to fetch per page.</summary>
        public int count { get; set; }
        /// <summary>The id of the last asset retrieved, used for cursor-based pagination.</summary>
        public static int lastId { get; set; }

        /// <summary>Resets the pagination cursor to the beginning of the asset list.</summary>
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
    /// Filters to apply to the asset listing based on key-value pairs.
    /// </summary>
    public class LootLockerSimpleAssetFilter
    {
        /// <summary>The key for which to look for the filtered values.</summary>
        public string key { get; set; }
        /// <summary>A list of values to filter by. If the asset has any of these values for the given key, it will be included in the results.</summary>
        public string[] values { get; set; }
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
        /// <summary>Filters to apply to the asset listing.</summary>
        public List<LootLockerSimpleAssetFilter> asset_filters { get; set; } = new List<LootLockerSimpleAssetFilter>();
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

    /// <summary>
    /// Request to grant an asset to the current player's inventory.
    /// </summary>
    public class LootLockerGrantAssetRequest
    {
        /// <summary>The id of the asset to grant.</summary>
        public int asset_id { get; set; }
        /// <summary>The variation id to grant, or null for the default variation.</summary>
        public int? asset_variation_id { get; set; }
        /// <summary>The rental option id to use, or null if the asset is not rented.</summary>
        public int? asset_rental_option_id { get; set; }
    }

    /// <summary>
    /// Response containing a list of assets returned by a legacy asset request.
    /// </summary>
    public class LootLockerAssetResponse : LootLockerResponse
    {
        /// <summary>The list of assets returned by the endpoint.</summary>
        public LootLockerCommonAsset[] assets { get; set; }
    }

    /// <summary>
    /// Response containing a single asset.
    /// </summary>
    public class LootLockerSingleAssetResponse : LootLockerResponse
    {
        /// <summary>The requested asset.</summary>
        public LootLockerCommonAsset asset { get; set; }
    }

    /// <summary>
    /// Response containing the full details of an asset, returned by the legacy common asset endpoint.
    /// </summary>
    [Serializable]
    public class LootLockerCommonAssetResponse : LootLockerResponse
    {
        /// <summary>The legacy integer id of the asset.</summary>
        public int id { get; set; }
        /// <summary>The UUID of the asset.</summary>
        public string uuid { get; set; }
        /// <summary>The ULID of the asset.</summary>
        public string ulid { get; set; }
        /// <summary>The display name of the asset.</summary>
        public string name { get; set; }
        /// <summary>Whether the asset is currently active.</summary>
        public bool active { get; set; }
        /// <summary>Whether the asset can be purchased.</summary>
        public bool purchasable { get; set; }
        /// <summary>The type category of the asset.</summary>
        public string type { get; set; }
        /// <summary>The regular price of the asset.</summary>
        public int price { get; set; }
        /// <summary>The sale price of the asset, or null if not on sale.</summary>
        public int? sales_price { get; set; }
        /// <summary>A formatted string representation of the price for display purposes.</summary>
        public string display_price { get; set; }
        /// <summary>The context (category) the asset belongs to.</summary>
        public string context { get; set; }
        /// <summary>The context unlocked by equipping this asset.</summary>
        public string unlocks_context { get; set; }
        /// <summary>Whether this asset can be removed from an equip slot after being equipped.</summary>
        public bool detachable { get; set; }
        /// <summary>When the asset was last updated, as a date string.</summary>
        public string updated { get; set; }
        /// <summary>When the asset was marked as new, as a date string.</summary>
        public string marked_new { get; set; }
        /// <summary>The id of the variation that is selected by default.</summary>
        public int default_variation_id { get; set; }
        /// <summary>The description of the asset.</summary>
        public string description { get; set; }
        /// <summary>Named links (e.g. image URLs) associated with this asset.</summary>
        public LootLockerLinks links { get; set; }
        /// <summary>Key-value storage entries attached to this asset.</summary>
        public LootLockerStorage[] storage { get; set; }
        /// <summary>Rarity information for this asset.</summary>
        public LootLockerRarity rarity { get; set; }
        /// <summary>Whether this asset is popular.</summary>
        public bool popular { get; set; }
        /// <summary>A numeric score used to rank asset popularity.</summary>
        public int popularity_score { get; set; }
        /// <summary>Whether each grant of this asset creates a unique inventory instance.</summary>
        public bool unique_instance { get; set; }
        /// <summary>Available rental options for this asset.</summary>
        public LootLockerRental_Options[] rental_options { get; set; }
        /// <summary>Filters (tags) applied to this asset for categorisation.</summary>
        public LootLockerFilter[] filters { get; set; }
        /// <summary>Variations available for this asset.</summary>
        public LootLockerVariation[] variations { get; set; }
        /// <summary>Whether this asset is featured.</summary>
        public bool featured { get; set; }
        /// <summary>Whether this asset is locked to its context.</summary>
        public bool context_locked { get; set; }
        /// <summary>Whether this asset can be purchased initially.</summary>
        public bool initially_purchasable { get; set; }
        /// <summary>Files associated with this asset.</summary>
        public LootLockerFile[] files { get; set; }
        /// <summary>Information about the UGC creator of this asset, if applicable.</summary>
        public LootLockerAssetCandidate asset_candidate { get; set; }
        /// <summary>Data entities attached to this asset.</summary>
        public string[] data_entities { get; set; }
    }

    /// <summary>
    /// A simplified asset object to improve performance by including only the fields most commonly needed.
    /// </summary>
    [Serializable]
    public class LootLockerSimpleAsset
    {
        /// <summary>The legacy integer id of the asset.</summary>
        public int asset_id { get; set; }
        /// <summary>The UUID of the asset.</summary>
        public string asset_uuid { get; set; }
        /// <summary>The ULID of the asset.</summary>
        public string asset_ulid { get; set; }
        /// <summary>The display name of the asset.</summary>
        public string asset_name { get; set; }
        /// <summary>The id of the context this asset belongs to.</summary>
        public int context_id { get; set; }
        /// <summary>The name of the context this asset belongs to.</summary>
        public string context_name { get; set; }
        /// <summary>The player who authored this asset, if it is user-generated content.</summary>
        public LootLockerSimpleAssetAuthor author { get; set; }
        /// <summary>Key-value storage entries attached to this asset, if requested.</summary>
        public LootLockerStorage[] storage { get; set; }
        /// <summary>Files associated with this asset, if requested.</summary>
        public LootLockerSimpleAssetFile[] files { get; set; }
        /// <summary>Data entities attached to this asset, if requested.</summary>
        public LootLockerSimpleAssetDataEntity[] data_entities { get; set; }
        /// <summary>Metadata entries associated with this asset, if requested.</summary>
        public LootLockerMetadataEntry[] metadata { get; set; }
    }

    /// <summary>
    /// Authorship information for a user-generated content (UGC) asset.
    /// </summary>
    public class LootLockerSimpleAssetAuthor
    {
        /// <summary>The legacy integer id of the player who authored the asset.</summary>
        public int player_id { get; set; }
        /// <summary>The ULID of the player who authored the asset.</summary>
        public string player_ulid { get; set; }
        /// <summary>The public UID of the player who authored the asset.</summary>
        public string public_uid { get; set; }
        /// <summary>The current display name of the player who authored the asset.</summary>
        public string active_name { get; set; }
    }

    /// <summary>
    /// A file associated with a simplified asset, providing the size, name, URL, and optional tags.
    /// </summary>
    public class LootLockerSimpleAssetFile
    {
        /// <summary>The file size in bytes.</summary>
        public int size { get; set; }
        /// <summary>The file name.</summary>
        public string name { get; set; }
        /// <summary>The URL from which the file can be downloaded.</summary>
        public string url { get; set; }
        /// <summary>Tags categorising or labelling the file.</summary>
        public string[] tags { get; set; }
    }

    /// <summary>
    /// A named data entity attached to a simplified asset.
    /// </summary>
    public class LootLockerSimpleAssetDataEntity
    {
        /// <summary>The name of this data entity.</summary>
        public string name { get; set; }
        /// <summary>The data payload of this entity, as a JSON string or raw value.</summary>
        public string data { get; set; }
    }

    /// <summary>
    /// Indicates whether a legacy asset entry is part of the player's default loadout.
    /// </summary>
    public class LootLockerDefault_Loadouts
    {
        /// <summary>Whether this asset is in the default loadout.</summary>
        public bool Default { get; set; }
    }

    /// <summary>
    /// Response containing the list of asset ids that the current player has marked as favourites.
    /// </summary>
    public class LootLockerFavouritesListResponse : LootLockerResponse
    {
        /// <summary>The list of asset ids marked as favourites by the current player.</summary>
        public int[] favourites { get; set; }
    }

    /// <summary>
    /// Response returned after granting an asset to the current player's inventory.
    /// </summary>
    public class LootLockerGrantAssetResponse : LootLockerResponse
    {
        /// <summary>The inventory instance id of the granted item.</summary>
        public int id { get; set; }
        /// <summary>The id of the asset that was granted.</summary>
        public int asset_id { get; set; }
        /// <summary>The ULID of the asset that was granted.</summary>
        public string asset_ulid { get; set; }
        /// <summary>The variation id of the granted item, or null for the default variation.</summary>
        public int? asset_variation_id { get; set; }
        /// <summary>The rental option id of the granted item, or null if not rented.</summary>
        public int? asset_rental_option_id { get; set; }
        /// <summary>The source through which this item was acquired.</summary>
        public string acquisition_source { get; set; }
        /// <summary>The date and time when this item was acquired, as a string.</summary>
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
            int UGCCreatorPlayerID = 0, int contextId = 0)
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

            if (assetFilters != null && assetFilters.Count > 0)
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

            if (contextId > 0)
            {
                tempEndpoint = $"&context_id={contextId}";
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
