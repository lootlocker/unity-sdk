using LootLocker.LootLockerEnums;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// Possible entity kinds that catalog entries can have
    /// </summary>
    public enum LootLockerCatalogEntryEntityKind
    {
        asset = 0,
        currency = 1,
        progression_points = 2,
        progression_reset = 3,
    };
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerCatalog
    {
        /// <summary>
        /// The time when this catalog was created
        /// </summary>
        public string created_at { get; set; }
        /// <summary>
        /// The name of the catalog
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The unique identifying key of the catalog
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// The id of the catalog
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The time when this catalog was deleted, should normally be null
        /// </summary>
        public string deleted_at { get; set; }
    }

    /// <summary>
   /// </summary>
    public class LootLockerCatalogPagination
    {
        /// <summary>
        /// The total available items in this catalog
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// The cursor that points to the next item in the catalog list. Use this in subsequent requests to get additional items from the catalog.
        /// </summary>
        public string cursor { get; set; }
        /// <summary>
        /// The cursor that points to the first item in this batch of items.
        /// </summary>
        public string previous_cursor { get; set; }
    }

    /// <summary>
   /// </summary>
    public class LootLockerCatalogEntryPrice
    {
        /// <summary>
       /// The amount (cost) set for this price
       /// </summary>
        public int amount { get; set; }
        /// <summary>
       /// A prettyfied version of the amount to use for display
       /// </summary>
        public string display_amount { get; set; }
        /// <summary>
       /// The short code for the currency this price is in
       /// </summary>
        public string currency_code { get; set; }
        /// <summary>
       /// The name of the currency this price is in
       /// </summary>
        public string currency_name { get; set; }
        /// <summary>
       /// The unique id of this price
       /// </summary>
        public string price_id { get; set; }
        /// <summary>
       /// The unique id of the currency this price is in
       /// </summary>
        public string currency_id { get; set; }
    }

    /// <summary>
   /// </summary>
    public class LootLockerCatalogEntry
    {
        /// <summary>
       /// The time when this catalog entry was created
       /// </summary>
        public string created_at { get; set; }
        /// <summary>
       /// The kind of entity that this entry is. This signifies in which lookup structure to find the details of this entry by using the catalog_listing_id.
       /// </summary>
        public LootLockerCatalogEntryEntityKind entity_kind { get; set; }
        /// <summary>
       /// The name of this entity
       /// </summary>
        public string entity_name { get; set; }
        /// <summary>
       /// A list of prices for this catalog entry
       /// </summary>
        public LootLockerCatalogEntryPrice[] prices { get; set; }
        /// <summary>
       /// The unique id of the entity that this entry refers to.
       /// </summary>
        public string entity_id { get; set; }
        /// <summary>
        /// A unique listing id for this entry in this catalog, grouping the entity and the prices. This is the key you use to look up details about the entity in the structure signified by the entity_kind.
        /// </summary>
        public string catalog_listing_id { get; set; }
        /// <summary>
       /// Whether this entry is currently purchasable
       /// </summary>
        public bool purchasable { get; set; }
    }

    /// <summary>
   /// </summary>
    public class LootLockerAssetDetails
    {
        /// <summary>
       /// The name of this asset
       /// </summary>
        public string name { get; set; }
        /// <summary>
       /// The id of the specific variation of this asset that this refers to
       /// </summary>
        public string variation_id { get; set; }
        /// <summary>
       /// The id of the specific rental option of this asset that this refers to
       /// </summary>
        public string rental_option_id { get; set; }
        /// <summary>
       /// The legacy id of this asset
       /// </summary>
        public int legacy_id { get; set; }
        /// <summary>
       /// The unique identyfing id of this asset.
       /// </summary>
        public string id { get; set; }
        /// <summary>
       /// The thumbnail for this asset
       /// </summary>
        public string thumbnail { get; set; }
        /// <summary>
       /// The catalog listing id for this asset detail
       /// </summary>
        public string catalog_listing_id { get; set; }

    }

    /// <summary>
   /// </summary>
    public class LootLockerProgressionPointDetails
    {
        /// <summary>
       /// The unique key of the pogression that this refers to
       /// </summary>
        public string key { get; set; }
        /// <summary>
       /// The name of the progression that this refers to
       /// </summary>
        public string name { get; set; }
        /// <summary>
       /// The amount of points to be added to the progression in question
       /// </summary>
        public int amount { get; set; }
        /// <summary>
       /// The unique id of the progression that this refers to
       /// </summary>
        public string id { get; set; }
        /// <summary>
       /// The catalog listing id for this progression point detail
       /// </summary>
        public string catalog_listing_id { get; set; }
    }

    /// <summary>
   /// </summary>
    public class LootLockerProgressionResetDetails
    {
        /// <summary>
       /// The unique key of the progression that this refers to
       /// </summary>
        public string key { get; set; }
        /// <summary>
       /// The name of the progression that this refers to
       /// </summary>
        public string name { get; set; }
        /// <summary>
       /// The unique id of the progression that this refers to
       /// </summary>
        public string id { get; set; }
        /// <summary>
       /// The catalog listing id for this progression reset detail
       /// </summary>
        public string catalog_listing_id { get; set; }
    }

    /// <summary>
   /// </summary>
    public class LootLockerCurrencyDetails
    {
        /// <summary>
       /// The name of the currency that this refers to
       /// </summary>
        public string name { get; set; }
        /// <summary>
       /// The unique code of the currency that this refers to
       /// </summary>
        public string code { get; set; }
        /// <summary>
       /// The amount of this currency to be awarded
       /// </summary>
        public string amount { get; set; }
        /// <summary>
       /// The unique id of the currency that this refers to
       /// </summary>
        public string id { get; set; }
        /// <summary>
       /// The catalog listing id for this currency detail
       /// </summary>
        public string catalog_listing_id { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
   /// </summary>
    public class LootLockerListCatalogsResponse : LootLockerResponse 
    {
        /// <summary>
       /// A list of the prices for the game
       /// </summary>
        public LootLockerCatalog[] catalogs { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerListCatalogPricesResponse : LootLockerResponse
    {
        /// <summary>
        /// Details about the catalog that the prices is in
        /// </summary>
        public LootLockerCatalog catalog { get; set; }

        /// <summary>
        /// A list of entries available in this catalog
        /// </summary>
        public LootLockerCatalogEntry[] entries { get; set; }

        /// <summary>
        /// Lookup map for details about entities of entity type assets
        /// </summary>
        public Dictionary<string /*catalog_listing_id*/, LootLockerAssetDetails> asset_details { get; set; }

        /// <summary>
        /// Lookup map for details about entities of entity type progression_points
        /// </summary>
        public Dictionary<string /*catalog_listing_id*/, LootLockerProgressionPointDetails> progression_points_details
        {
            get;
            set;
        }

        /// <summary>
        /// Lookup map for details about entities of entity type progression_reset
        /// </summary>
        public Dictionary<string /*catalog_listing_id*/, LootLockerProgressionResetDetails> progression_resets_details
        {
            get;
            set;
        }

        /// <summary>
        /// Lookup map for details about entities of entity type currency
        /// </summary>
        public Dictionary<string /*catalog_listing_id*/, LootLockerCurrencyDetails> currency_details { get; set; }

        /// <summary>
        /// Pagination data to use for subsequent requests
        /// </summary>
        public LootLockerCatalogPagination pagination { get; set; }

        public void AppendCatalogItems(LootLockerListCatalogPricesResponse catalogPrices)
        {
            var concatenatedArray = new LootLockerCatalogEntry[entries.Length + catalogPrices.entries.Length];
            entries.CopyTo(concatenatedArray, 0);
            catalogPrices.entries.CopyTo(concatenatedArray, entries.Length);
            pagination.total = catalogPrices.pagination.total;
            pagination.cursor = catalogPrices.pagination.cursor;

            foreach (var assetDetail in catalogPrices.asset_details)
            {
                asset_details.Add(assetDetail.Key, assetDetail.Value);
            }

            foreach (var progressionPointDetail in catalogPrices.progression_points_details)
            {
                progression_points_details.Add(progressionPointDetail.Key, progressionPointDetail.Value);
            }

            foreach (var progressionResetDetail in catalogPrices.progression_resets_details)
            {
                progression_resets_details.Add(progressionResetDetail.Key, progressionResetDetail.Value);
            }

            foreach (var currencyDetail in catalogPrices.currency_details)
            {
                currency_details.Add(currencyDetail.Key, currencyDetail.Value);
            }
        }

        public LootLockerListCatalogPricesResponse() {}

        /// This is the way that the response actually looks, but we don't want to expose it, hence the conversion
        private class LootLockerListCatalogItemsWithArraysResponse : LootLockerResponse
        {
            public LootLockerCatalog catalog { get; set; }
            public LootLockerCatalogEntry[] entries { get; set; }
            public LootLockerAssetDetails[] assets_details { get; set; }
            public LootLockerProgressionPointDetails[] progression_points_details { get; set; }
            public LootLockerProgressionResetDetails[] progression_resets_details { get; set; }
            public LootLockerCurrencyDetails[] currency_details { get; set; }
            public LootLockerCatalogPagination pagination { get; set; }
        }

        public LootLockerListCatalogPricesResponse(LootLockerResponse serverResponse)
        {
            LootLockerListCatalogItemsWithArraysResponse parsedResponse =
                Deserialize<LootLockerListCatalogItemsWithArraysResponse>(serverResponse);
            success = parsedResponse.success;
            statusCode = parsedResponse.statusCode;
            text = parsedResponse.text;
            errorData = parsedResponse.errorData;
            if (!success)
            {
                return;
            }

            catalog = parsedResponse.catalog;
            entries = parsedResponse.entries;
            pagination = parsedResponse.pagination;

            if (parsedResponse.assets_details != null && parsedResponse.assets_details.Length > 0)
            {
                asset_details = new Dictionary<string, LootLockerAssetDetails>();
                foreach (var assetDetail in parsedResponse.assets_details)
                {
                    asset_details[assetDetail.catalog_listing_id] = assetDetail;
                }
            }

            if (parsedResponse.progression_points_details != null &&
                parsedResponse.progression_points_details.Length > 0)
            {
                progression_points_details = new Dictionary<string, LootLockerProgressionPointDetails>();
                foreach (var detail in parsedResponse.progression_points_details)
                {
                    progression_points_details[detail.catalog_listing_id] = detail;
                }
            }

            if (parsedResponse.progression_resets_details != null &&
                parsedResponse.progression_resets_details.Length > 0)
            {
                progression_resets_details = new Dictionary<string, LootLockerProgressionResetDetails>();
                foreach (var detail in parsedResponse.progression_resets_details)
                {
                    progression_resets_details[detail.catalog_listing_id] = detail;
                }
            }

            if (parsedResponse.currency_details != null && parsedResponse.currency_details.Length > 0)
            {
                currency_details = new Dictionary<string, LootLockerCurrencyDetails>();
                foreach (var detail in parsedResponse.currency_details)
                {
                    currency_details[detail.catalog_listing_id] = detail;
                }
            }
        }

#if UNITY_2020_2_OR_NEWER
        /// <summary>
        /// </summary>
        public class LootLockerInlinedCatalogEntry : LootLockerCatalogEntry
        {
            /// <summary>
            /// Asset details inlined for this catalog entry, will be null if the entity_kind is not asset
            /// </summary>
            [CanBeNull]
            public LootLockerAssetDetails asset_details { get; set; }
            /// <summary>
            /// Progression point details inlined for this catalog entry, will be null if the entity_kind is not progression_points
            /// </summary>
            [CanBeNull]
            public LootLockerProgressionPointDetails progression_point_details { get; set; }
            /// <summary>
            /// Progression reset details inlined for this catalog entry, will be null if the entity_kind is not progression_reset
            /// </summary>
            [CanBeNull]
            public LootLockerProgressionResetDetails progression_reset_details { get; set; }
            /// <summary>
            /// Currency details inlined for this catalog entry, will be null if the entity_kind is not currency
            /// </summary>
            [CanBeNull]
            public LootLockerCurrencyDetails currency_details { get; set; }

            public LootLockerInlinedCatalogEntry(LootLockerCatalogEntry entry, [CanBeNull] LootLockerAssetDetails assetDetails, [CanBeNull] LootLockerProgressionPointDetails progressionPointDetails, [CanBeNull] LootLockerProgressionResetDetails progressionResetDetails, [CanBeNull] LootLockerCurrencyDetails currencyDetails) 
            {
                created_at = entry.created_at;
                entity_kind = entry.entity_kind;
                entity_name = entry.entity_name;
                entity_id = entry.entity_id;
                prices = entry.prices;
                catalog_listing_id = entry.catalog_listing_id;
                purchasable = entry.purchasable;
                asset_details = assetDetails;
                progression_point_details = progressionPointDetails;
                progression_reset_details = progressionResetDetails;
                currency_details = currencyDetails;
            }
        }

        /// <summary>
        /// Get all the entries with details inlined into the entries themselves
        /// </summary>
        public LootLockerInlinedCatalogEntry[] GetLootLockerInlinedCatalogEntries()
        {
            List<LootLockerInlinedCatalogEntry> inlinedEntries = new List<LootLockerInlinedCatalogEntry>();
            foreach (var lootLockerCatalogEntry in entries)
            {
                var catalogListingID = lootLockerCatalogEntry.catalog_listing_id;
                var entityKind = lootLockerCatalogEntry.entity_kind;
                inlinedEntries.Add(new LootLockerInlinedCatalogEntry(
                    lootLockerCatalogEntry, 
                    LootLockerCatalogEntryEntityKind.asset == entityKind && asset_details.ContainsKey(catalogListingID) ? asset_details[catalogListingID] : null, 
                    LootLockerCatalogEntryEntityKind.progression_points == entityKind && progression_points_details.ContainsKey(catalogListingID) ? progression_points_details[catalogListingID] : null, 
                    LootLockerCatalogEntryEntityKind.progression_reset == entityKind && progression_resets_details.ContainsKey(catalogListingID) ? progression_resets_details[catalogListingID] : null, 
                    LootLockerCatalogEntryEntityKind.currency == entityKind && currency_details.ContainsKey(catalogListingID) ? currency_details[catalogListingID] : null
                ));
            }
            return inlinedEntries.ToArray();
        }
#endif
    }
}
