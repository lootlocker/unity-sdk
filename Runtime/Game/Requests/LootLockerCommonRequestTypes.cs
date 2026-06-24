using System;
using System.Collections.Generic;

namespace LootLocker.Requests
{
    /// <summary>
    /// A key-value pair used to store arbitrary data on a player or asset.
    /// </summary>
    public class LootLockerStorage
    {
        /// <summary>The unique identifier for this storage entry.</summary>
        public int id { get; set; }
        /// <summary>The key for this storage entry.</summary>
        public string key { get; set; }
        /// <summary>The value for this storage entry.</summary>
        public string value { get; set; }
    }

    /// <summary>
    /// A dictionary of named links (URLs) associated with an asset, with a convenience accessor for the thumbnail URL.
    /// </summary>
    public class LootLockerLinks : Dictionary<string, string>
    {
        /// <summary>The URL pointing to the asset's thumbnail image, if available.</summary>
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

    /// <summary>
    /// Rarity information for an asset, describing its name, short code, and display color.
    /// </summary>
    public class LootLockerRarity
    {
        /// <summary>The full name of the rarity tier (e.g. "Legendary").</summary>
        public string name { get; set; }
        /// <summary>A short code for the rarity tier (e.g. "L").</summary>
        public string short_name { get; set; }
        /// <summary>The hex color code associated with this rarity tier.</summary>
        public string color { get; set; }
    }

    /// <summary>
    /// A rental option available for an asset, defining duration, price, and related links.
    /// </summary>
    public class LootLockerRental_Options
    {
        /// <summary>The unique identifier for this rental option.</summary>
        public int id { get; set; }
        /// <summary>The display name of this rental option.</summary>
        public string name { get; set; }
        /// <summary>The rental duration in seconds.</summary>
        public int duration { get; set; }
        /// <summary>The regular price for this rental option.</summary>
        public int price { get; set; }
        /// <summary>The sale price for this rental option, or null if not on sale.</summary>
        public object sales_price { get; set; }
        /// <summary>Links associated with this rental option.</summary>
        public object links { get; set; }
    }

    /// <summary>
    /// A filter applied to an asset, consisting of a name-value pair used to categorise or tag the asset.
    /// </summary>
    public class LootLockerFilter
    {
        /// <summary>The value of this filter tag.</summary>
        public string value { get; set; }
        /// <summary>The name of this filter tag.</summary>
        public string name { get; set; }
    }

    /// <summary>
    /// A named variation of an asset with optional color information and associated links.
    /// </summary>
    public class LootLockerVariation
    {
        /// <summary>The unique identifier of this variation.</summary>
        public int id { get; set; }
        /// <summary>The display name of this variation.</summary>
        public string name { get; set; }
        /// <summary>The primary color of this variation, or null if not set.</summary>
        public object primary_color { get; set; }
        /// <summary>The secondary color of this variation, or null if not set.</summary>
        public object secondary_color { get; set; }
        /// <summary>A dictionary of named links (e.g. image URLs) associated with this variation.</summary>
        public Dictionary<string, string> links { get; set; }
    }

    /// <summary>
    /// A file associated with an asset, described by a URL and an optional set of tags.
    /// </summary>
    public class LootLockerFile
    {
        /// <summary>The URL from which the file can be downloaded.</summary>
        public string url { get; set; }
        /// <summary>Tags categorising or labelling the file.</summary>
        public string[] tags { get; set; }
    }

    /// <summary>
    /// Information about the player who submitted an asset as a user-generated content (UGC) candidate.
    /// </summary>
    public class LootLockerAssetCandidate
    {
        /// <summary>The legacy integer id of the player who created this asset candidate.</summary>
        public int created_by_player_id { get; set; }
        /// <summary>The public UID of the player who created this asset candidate.</summary>
        public string created_by_player_uid { get; set; }
    }

    /// <summary>
    /// A full asset object containing all properties of a LootLocker asset, including variations, rental options, storage, and files.
    /// </summary>
    [Serializable]
    public class LootLockerCommonAsset
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
        /// <summary>The type category of the asset (e.g. "character", "consumable").</summary>
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
        public LootLockerStorage[] storage { get; set; }
        public LootLockerRarity rarity { get; set; }
        public bool popular { get; set; }
        public int popularity_score { get; set; }
        public bool unique_instance { get; set; }
        /// <summary>Available rental options for this asset.</summary>
        public LootLockerRental_Options[] rental_options { get; set; }
        /// <summary>Filters (tags) applied to this asset for categorisation.</summary>
        public LootLockerFilter[] filters { get; set; }
        /// <summary>Variations available for this asset.</summary>
        public LootLockerVariation[] variations { get; set; }
        /// <summary>Whether this asset is featured.</summary>
        public bool featured { get; set; }
        /// <summary>Whether this asset is locked to its context and cannot be used outside of it.</summary>
        public bool context_locked { get; set; }
        /// <summary>Whether this asset can be purchased initially (before any special conditions are met).</summary>
        public bool initially_purchasable { get; set; }
        /// <summary>Files associated with this asset.</summary>
        public LootLockerFile[] files { get; set; }
        /// <summary>Information about the UGC creator of this asset, if applicable.</summary>
        public LootLockerAssetCandidate asset_candidate { get; set; }
        /// <summary>Data entities attached to this asset.</summary>
        public string[] data_entities { get; set; }
    }

    /// <summary>
    /// Rental status information for an inventory item that was acquired as a rental.
    /// </summary>
    [Serializable]
    public class LootLockerRental
    {
        /// <summary>Whether this inventory item is a rental.</summary>
        public bool is_rental { get; set; }
        /// <summary>The amount of time remaining on the rental, as a formatted string.</summary>
        public string time_left { get; set; }
        /// <summary>The total duration of the rental, as a formatted string.</summary>
        public string duration { get; set; }
        /// <summary>Whether the rental is currently active.</summary>
        public string is_active { get; set; }
    }

    /// <summary>
    /// Response containing a list of available asset contexts (categories).
    /// </summary>
    public class LootLockerContextResponse : LootLockerResponse
    {
        /// <summary>The list of asset contexts available for this game.</summary>
        public LootLockerContext[] contexts { get; set; }
    }

    /// <summary>
    /// An asset context (category) that defines a slot or grouping in which assets can be equipped.
    /// </summary>
    public class LootLockerContext
    {
        /// <summary>The unique integer identifier of this context.</summary>
        public int id { get; set; }
        /// <summary>The UUID of this context.</summary>
        public string uuid { get; set; }
        /// <summary>The internal name of this context.</summary>
        public string name { get; set; }
        /// <summary>The player-facing display name of this context.</summary>
        public string friendly_name { get; set; }
        /// <summary>Whether assets in this context can be removed after equipping.</summary>
        public bool detachable { get; set; }
        /// <summary>Whether this context is visible to players.</summary>
        public bool user_facing { get; set; }
        /// <summary>The id of an asset that must be equipped before assets in this context can be used, or null if there is no dependency.</summary>
        public object dependent_asset_id { get; set; }
        /// <summary>The maximum number of assets from this context that can be equipped simultaneously.</summary>
        public int max_equip_count { get; set; }
    }

    /// <summary>
    /// Response containing the time remaining after activating a rental asset.
    /// </summary>
    public class LootLockerActivateRentalAssetResponse : LootLockerResponse
    {
        /// <summary>The number of seconds remaining on the activated rental.</summary>
        public int time_left { get; set; }
    }

    /// <summary>
    /// Response containing the player's full inventory.
    /// </summary>
    [Serializable]
    public class LootLockerInventoryResponse : LootLockerResponse
    {
        /// <summary>The list of inventory items owned by the player.</summary>
        public LootLockerInventory[] inventory { get; set; }
    }

    /// <summary>
    /// A single item in a player's inventory, combining asset details with instance-specific metadata.
    /// </summary>
    public class LootLockerInventory
    {
        /// <summary>The unique identifier for this specific inventory instance.</summary>
        public int instance_id { get; set; }
        /// <summary>The id of the asset variation for this item, or null for the default variation.</summary>
        public int? variation_id { get; set; }
        /// <summary>The id of the rental option used to acquire this item, or null if not rented.</summary>
        public string rental_option_id { get; set; }
        /// <summary>The source through which this item was acquired (e.g. "purchase", "grant").</summary>
        public string acquisition_source { get; set; }
        /// <summary>When this item was acquired, or null if the date is not available.</summary>
        public DateTime? acquisition_date { get; set; }
        /// <summary>Full asset details for this inventory item.</summary>
        public LootLockerCommonAsset asset { get; set; }
        /// <summary>Rental status for this item, if it was acquired as a rental.</summary>
        public LootLockerRental rental { get; set; }

        /// <summary>The current balance associated with this inventory item.</summary>
        public float balance { get; set; }
    }
}