using System;
using LootLocker.Requests;
using LootLocker.Utilities.HTTP;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// The type of an item, determining whether the item instances are stackable or individually tracked.
    /// </summary>
    public enum LootLockerItemType
    {
        /// <summary>Each granted item is a separate, individually tracked instance.</summary>
        instanced,
        /// <summary>The item is stored as a single entry with a count that can be incremented or decremented.</summary>
        stackable
    }

    /// <summary>
    /// The field by which to order a player item list response.
    /// </summary>
    public enum LootLockerItemSortField
    {
        /// <summary>Order by when the item was created.</summary>
        created_at,
        /// <summary>Order by when the item was last updated.</summary>
        updated_at
    }

    /// <summary>
    /// The direction in which to order a player item list response.
    /// </summary>
    public enum LootLockerSortOrder
    {
        /// <summary>Order ascending.</summary>
        ASC,
        /// <summary>Order descending.</summary>
        DESC
    }
}

namespace LootLocker.Requests
{
    //==================================================
    // Item Template Definitions
    //==================================================

    /// <summary>
    /// An item template defines a type of item that can be granted to players.
    /// </summary>
    public class LootLockerItemTemplate
    {
        /// <summary>The unique id of the item template.</summary>
        public string id { get; set; }

        /// <summary>The name of the item template.</summary>
        public string name { get; set; }

        /// <summary>The id of the game this item template belongs to.</summary>
        public int game_id { get; set; }

        /// <summary>Indicates whether this item template is limited in how many of it can exist.</summary>
        public int limited { get; set; }

        /// <summary>The type of the item (instanced or stackable).</summary>
        public string item_type { get; set; }

        /// <summary>True if the item can be consumed by the player.</summary>
        public bool consumable { get; set; }

        /// <summary>True if the item can be deleted by the player.</summary>
        public bool deletable { get; set; }

        /// <summary>The time that this item template was created.</summary>
        public string created_at { get; set; }

        /// <summary>The time that this item template was last updated.</summary>
        public string updated_at { get; set; }
    }

    //==================================================
    // Player Item Definitions
    //==================================================

    /// <summary>
    /// An item instance held in a player's inventory.
    /// </summary>
    public class LootLockerItem
    {
        /// <summary>The unique id of this inventory item.</summary>
        public string id { get; set; }

        /// <summary>The id of the player that owns this inventory item.</summary>
        public int player_id { get; set; }

        /// <summary>The id of the item template this inventory item is based on.</summary>
        public string item_template_id { get; set; }

        /// <summary>The type of the item (instanced or stackable).</summary>
        public string item_type { get; set; }

        /// <summary>True if the item can be consumed by the player.</summary>
        public bool consumable { get; set; }

        /// <summary>True if the item can be deleted by the player.</summary>
        public bool deletable { get; set; }

        /// <summary>The number of items in this stack (only relevant for stackable items).</summary>
        public int count { get; set; }

        /// <summary>The name of the item template this inventory item is based on.</summary>
        public string name { get; set; }

        /// <summary>The source of this inventory item, or null if not specified.</summary>
        public string source { get; set; }

        /// <summary>The time that this inventory item was created.</summary>
        public string created_at { get; set; }

        /// <summary>The time that this inventory item was last updated.</summary>
        public string updated_at { get; set; }

        /// <summary>Arbitrary metadata attached to this inventory item.</summary>
        public LootLockerMetadataEntry[] metadata { get; set; }
    }

    /// <summary>
    /// An item granted to a player as the result of consuming another item (or via an on-grant behaviour).
    /// Matches the go-backend `GrantResult` shape returned in `granted` / `behaviour_results`.
    /// </summary>
    public class LootLockerGrantedItem
    {
        /// <summary>The source id of the granted entry (inventory id, currency id, etc.).</summary>
        public string source_id { get; set; }

        /// <summary>The number of items granted.</summary>
        public int count { get; set; }

        /// <summary>The type of the granted entry ("item_template", "currency", or "publisher_currency").</summary>
        public string type { get; set; }

        /// <summary>The name of the granted entry, if applicable.</summary>
        public string name { get; set; }

        /// <summary>The code of the granted entry, if applicable.</summary>
        public string code { get; set; }
    }

    //==================================================
    // Request Definitions
    //==================================================

    /// <summary>
    /// Request to consume a stackable item from the player's inventory.
    /// </summary>
    public class LootLockerConsumeItemRequest
    {
        /// <summary>
        /// The number of items to consume. Defaults to 1 when omitted. To consume an entire stack,
        /// pass the item's current count.
        /// </summary>
        public int? count { get; set; }

        public bool ShouldSerializecount()
        {
            // Omit count entirely when unset so the backend applies its default of 1. This also keeps
            // the Newtonsoft and ZeroDep JSON backends consistent, since only ZeroDep skips nulls.
            return count.HasValue;
        }
    }

    /// <summary>
    /// Request to split a stackable item in the player's inventory into two stacks.
    /// </summary>
    public class LootLockerSplitItemStackRequest
    {
        /// <summary>The number of items to move into the new stack.</summary>
        public int count { get; set; }
    }

    /// <summary>
    /// Request to merge two stacks of the same item into one.
    /// </summary>
    public class LootLockerMergeItemStacksRequest
    {
        /// <summary>The id of the source inventory item to merge from.</summary>
        public string source_inventory_id { get; set; }

        /// <summary>The id of the target inventory item to merge into.</summary>
        public string target_inventory_id { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response containing a paginated list of visible item templates.
    /// </summary>
    public class LootLockerListItemTemplatesResponse : LootLockerResponse
    {
        /// <summary>The list of visible item templates.</summary>
        public LootLockerItemTemplate[] items { get; set; }

        /// <summary>Pagination details for the response.</summary>
        public LootLockerExtendedPagination pagination { get; set; }
    }

    /// <summary>
    /// Response containing a paginated list of the player's inventory items.
    /// </summary>
    public class LootLockerListPlayerItemsResponse : LootLockerResponse
    {
        /// <summary>The list of the player's inventory items.</summary>
        public LootLockerItem[] items { get; set; }

        /// <summary>Pagination details for the response.</summary>
        public LootLockerExtendedPagination pagination { get; set; }
    }

    /// <summary>
    /// Response containing the player's inventory item, returned at the top level with its template and metadata.
    /// </summary>
    public class LootLockerGetPlayerItemResponse : LootLockerResponse
    {
        /// <summary>The unique id of this inventory item.</summary>
        public string id { get; set; }

        /// <summary>The id of the player that owns this inventory item.</summary>
        public int player_id { get; set; }

        /// <summary>The id of the item template this inventory item is based on.</summary>
        public string item_template_id { get; set; }

        /// <summary>The type of the item (instanced or stackable).</summary>
        public string item_type { get; set; }

        /// <summary>True if the item can be consumed by the player.</summary>
        public bool consumable { get; set; }

        /// <summary>True if the item can be deleted by the player.</summary>
        public bool deletable { get; set; }

        /// <summary>The number of items in this stack (only relevant for stackable items).</summary>
        public int count { get; set; }

        /// <summary>The source of this inventory item, or null if not specified.</summary>
        public string source { get; set; }

        /// <summary>The time that this inventory item was created.</summary>
        public string created_at { get; set; }

        /// <summary>The time that this inventory item was last updated.</summary>
        public string updated_at { get; set; }

        /// <summary>The item template this inventory item is based on.</summary>
        public LootLockerItemTemplate template { get; set; }

        /// <summary>Arbitrary metadata attached to this inventory item.</summary>
        public LootLockerMetadataEntry[] metadata { get; set; }
    }

    /// <summary>
    /// Response containing the result of consuming an item, including any items granted as a result.
    /// </summary>
    public class LootLockerConsumeItemResponse : LootLockerResponse
    {
        /// <summary>True if the item was consumed.</summary>
        public bool consumed { get; set; }

        /// <summary>Any items granted as a result of consuming the item.</summary>
        public LootLockerGrantedItem[] granted { get; set; }
    }

    /// <summary>
    /// Response containing the id of the newly created stack after splitting an item.
    /// </summary>
    public class LootLockerSplitItemStackResponse : LootLockerResponse
    {
        /// <summary>The id of the newly created item stack.</summary>
        public string id { get; set; }
    }
}

namespace LootLocker
{
    //==================================================
    // API Class Definition
    //==================================================

    public partial class LootLockerAPIManager
    {
        public static void ListItemTemplates(string forPlayerWithUlid, int page, int perPage, Action<LootLockerListItemTemplatesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listItemTemplates;

            var queryParams = new QueryParamaterBuilder();
            queryParams.Add("page", page);
            queryParams.Add("per_page", perPage);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint + queryParams.Build(), endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListPlayerItems(string forPlayerWithUlid, int page, int perPage, string name, string itemType, bool? consumable, string sort, string order, Action<LootLockerListPlayerItemsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listPlayerItems;

            var queryParams = new QueryParamaterBuilder();
            queryParams.Add("page", page);
            queryParams.Add("per_page", perPage);
            queryParams.Add("name", name);
            queryParams.Add("item_type", itemType);
            if (consumable.HasValue)
            {
                queryParams.Add("consumable", consumable.Value.ToString().ToLower());
            }
            queryParams.Add("sort", sort);
            queryParams.Add("order", order);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint + queryParams.Build(), endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetPlayerItem(string forPlayerWithUlid, string inventoryId, Action<LootLockerGetPlayerItemResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getPlayerItem;

            string getVariable = endPoint.WithPathParameter(inventoryId);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void DeletePlayerItem(string forPlayerWithUlid, string inventoryId, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deletePlayerItem;

            string getVariable = endPoint.WithPathParameter(inventoryId);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ConsumePlayerItem(string forPlayerWithUlid, string inventoryId, LootLockerConsumeItemRequest data, Action<LootLockerConsumeItemResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.consumePlayerItem;

            string getVariable = endPoint.WithPathParameter(inventoryId);
            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void SplitPlayerItemStack(string forPlayerWithUlid, string inventoryId, LootLockerSplitItemStackRequest data, Action<LootLockerSplitItemStackResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.splitPlayerItemStack;

            string getVariable = endPoint.WithPathParameter(inventoryId);
            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void MergePlayerItemStacks(string forPlayerWithUlid, LootLockerMergeItemStacksRequest data, Action<LootLockerResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.mergePlayerItemStacks;

            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}
