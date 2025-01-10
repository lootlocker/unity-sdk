using LootLocker.LootLockerEnums;
using System.Collections.Generic;

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
        group = 4,
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
    public class LootLockerCatalogAppleAppStoreListing
    {
        /// <summary>
        /// The id of the product in Apple App Store that can be purchased and then used to redeem this catalog entry
        /// </summary>
        public string product_id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerCatalogGooglePlayStoreListing
    {
        /// <summary>
        /// The id of the product in Google Play Store that can be purchased and then used to redeem this catalog entry
        /// </summary>
        public string product_id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerCatalogSteamStoreListingPrice
    {
        /// <summary>
        /// Currency code of the currency to be used for purchasing this listing
        /// </summary>
        public string currency { get; set; }
        /// <summary>
        /// Amount of the base value of the specified currency that this listing costs to purchase
        /// </summary>
        public int amount { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerCatalogSteamStoreListing
    {
        /// <summary>
        /// Description of this listing
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// List of prices for this listing
        /// </summary>
        public LootLockerCatalogSteamStoreListingPrice[] prices { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerCatalogEntryListings
    {
        /// <summary>
        /// The listing information (if configured) for Apple App Store
        /// </summary>
        public LootLockerCatalogAppleAppStoreListing apple_app_store { get; set; }
        /// <summary>
        /// The listing information (if configured) for Google Play Store
        /// </summary>
        public LootLockerCatalogGooglePlayStoreListing google_play_store { get; set; }
        /// <summary>
        /// The listing information (if configured) for Steam Store
        /// </summary>
        public LootLockerCatalogSteamStoreListing steam_store { get; set; }
    }

    /// <summary>
    /// Class to help simply getting item details
    /// </summary>
    public class LootLockerItemDetailsKey
    {
        /// <summary>
        /// The id of a catalog listing
        /// </summary>
        public string catalog_listing_id { get; set; }
        /// <summary>
        /// The id of the item
        /// </summary>
        public string item_id { get; set; }

        public override int GetHashCode()
        {
            return catalog_listing_id.GetHashCode() + item_id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

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
        /// All the listings configured for this catalog entry
        /// </summary>
        public LootLockerCatalogEntryListings listings { get; set; }
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
        /// <summary>
        /// Function to help identify details simpler
        /// </summary>
        /// <returns>The identifier for looking up details</returns>
        public LootLockerItemDetailsKey GetItemDetailsKey()
        {
            return new LootLockerItemDetailsKey { catalog_listing_id = catalog_listing_id, item_id = entity_id };
        }

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
        /// The unique identifying ulid of this asset
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
        /// <summary>
        /// Function to help identify details simpler
        /// </summary>
        /// <returns>The identifier for looking up details</returns>
        public LootLockerItemDetailsKey GetItemDetailsKey()
        {
            return new LootLockerItemDetailsKey { catalog_listing_id = catalog_listing_id, item_id = id };
        }
    }

    /// <summary>
    /// </summary>
    public class LootLockerProgressionPointDetails
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
        /// <summary>
        /// Function to help identify details simpler
        /// </summary>
        /// <returns>The identifier for looking up details</returns>
        public LootLockerItemDetailsKey GetItemDetailsKey()
        {
            return new LootLockerItemDetailsKey { catalog_listing_id = catalog_listing_id, item_id = id };
        }
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
        /// <summary>
        /// Function to help identify details simpler
        /// </summary>
        /// <returns>The identifier for looking up details</returns>
        public LootLockerItemDetailsKey GetItemDetailsKey()
        {
            return new LootLockerItemDetailsKey { catalog_listing_id = catalog_listing_id, item_id = id };
        }
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
        /// 
        /// The unique id of the currency that this refers to
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The catalog listing id for this currency detail
        /// </summary>
        public string catalog_listing_id { get; set; }

        /// <summary>
        /// Function to help identify details simpler
        /// </summary>
        /// <returns>The identifier for looking up details</returns>
        public LootLockerItemDetailsKey GetItemDetailsKey()
        {
            return new LootLockerItemDetailsKey { catalog_listing_id = catalog_listing_id, item_id = id };
        }
    }

    public class LootLockerGroupAssociation
    {
        /// <summary>
        /// The kind of reward, (asset / currency / group / progression points / progression reset).
        /// </summary>
        public LootLockerCatalogEntryEntityKind kind { get; set; }

        /// <summary>
        /// The unique id of the group that this refers to
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The catalog listing id for this group detail.
        /// </summary>
        public string catalog_listing_id { get; set; }

        /// <summary>
        /// Function to help identify details simpler
        /// </summary>
        /// <returns>The identifier for looking up details</returns>
        public LootLockerItemDetailsKey GetItemDetailsKey()
        {
            return new LootLockerItemDetailsKey { catalog_listing_id = catalog_listing_id, item_id = id };
        }
    }

    public class LootLockerGroupMetadata
    {
        /// <summary>
        /// The Key of a metadata
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// the Value of a metadata
        /// </summary>
        public string value { get; set; }
    }

    public class LootLockerInlinedGroupDetails : LootLockerGroupDetails
    {
        public List<LootLockerAssetDetails> assetDetails { get; set; } = new List<LootLockerAssetDetails>();
        public List<LootLockerCurrencyDetails> currencyDetails { get; set; } = new List<LootLockerCurrencyDetails>();
        public List<LootLockerProgressionPointDetails> progressionPointDetails { get; set; } = new List<LootLockerProgressionPointDetails>();
        public List<LootLockerProgressionResetDetails> progressionResetDetails { get; set; } = new List<LootLockerProgressionResetDetails>();
    }

    public class LootLockerGroupDetails
    {
        /// <summary>
        /// The name of the Group.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The description of the Group.
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// The metadata for the Group reward.
        /// </summary>
        public LootLockerGroupMetadata[] metadata { get; set; }

        /// <summary>
        /// The ID of the reward.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The catalog listing id for this group detail.
        /// </summary>
        public string catalog_listing_id { get; set; }

        /// <summary>
        /// The associations for the Group reward.
        /// </summary>
        public LootLockerGroupAssociation[] associations { get; set; }

        /// <summary>
        /// Function to help identify details simpler
        /// </summary>
        /// <returns>The identifier for looking up details</returns>
        public LootLockerItemDetailsKey GetItemDetailsKey()
        {
            return new LootLockerItemDetailsKey { catalog_listing_id = catalog_listing_id, item_id = id };
        }

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
        public Dictionary<LootLockerItemDetailsKey, LootLockerAssetDetails> asset_details { get; set; }

        /// <summary>
        /// Lookup map for details about entities of entity type progression_points
        /// </summary>
        public Dictionary<LootLockerItemDetailsKey, LootLockerProgressionPointDetails> progression_points_details
        {
            get;
            set;
        }

        /// <summary>
        /// Lookup map for details about entities of entity type progression_reset
        /// </summary>
        public Dictionary<LootLockerItemDetailsKey, LootLockerProgressionResetDetails> progression_resets_details
        {
            get;
            set;
        }

        /// <summary>
        /// Lookup map for details about entities of entity type currency
        /// </summary>
        public Dictionary<LootLockerItemDetailsKey, LootLockerCurrencyDetails> currency_details { get; set; }

        /// <summary>
        /// Lookup map for details about entities of entity type group
        /// </summary>
        public Dictionary<LootLockerItemDetailsKey, LootLockerGroupDetails> group_details { get; set; }

        /// <summary>
        /// Pagination data to use for subsequent requests
        /// </summary>
        public LootLockerPaginationResponse<string> pagination { get; set; }

        public void AppendCatalogItems(LootLockerListCatalogPricesResponse catalogPrices)
        {
            var concatenatedArray = new LootLockerCatalogEntry[entries.Length + catalogPrices.entries.Length];
            entries.CopyTo(concatenatedArray, 0);
            catalogPrices.entries.CopyTo(concatenatedArray, entries.Length);
            pagination.total = catalogPrices.pagination.total;
            pagination.next_cursor = catalogPrices.pagination.next_cursor;

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

            foreach (var groupDetail in catalogPrices.group_details)
            {
                group_details.Add(groupDetail.Key, groupDetail.Value);
            }

        }

        public LootLockerListCatalogPricesResponse() { }

        /// This is the way that the response actually looks, but we don't want to expose it, hence the conversion
        private class LootLockerListCatalogItemsWithArraysResponse : LootLockerResponse
        {
            public LootLockerCatalog catalog { get; set; }
            public LootLockerCatalogEntry[] entries { get; set; }
            public LootLockerAssetDetails[] assets_details { get; set; }
            public LootLockerProgressionPointDetails[] progression_points_details { get; set; }
            public LootLockerProgressionResetDetails[] progression_resets_details { get; set; }
            public LootLockerCurrencyDetails[] currency_details { get; set; }
            public LootLockerGroupDetails[] group_details { get; set; }
            public LootLockerPaginationResponse<string> pagination { get; set; }
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
                asset_details = new Dictionary<LootLockerItemDetailsKey, LootLockerAssetDetails>();
                foreach (var detail in parsedResponse.assets_details)
                {
                    asset_details[detail.GetItemDetailsKey()] = detail;
                }
            }

            if (parsedResponse.progression_points_details != null &&
                parsedResponse.progression_points_details.Length > 0)
            {
                progression_points_details = new Dictionary<LootLockerItemDetailsKey, LootLockerProgressionPointDetails>();
                foreach (var detail in parsedResponse.progression_points_details)
                {
                    progression_points_details[detail.GetItemDetailsKey()] = detail;
                }
            }

            if (parsedResponse.progression_resets_details != null &&
                parsedResponse.progression_resets_details.Length > 0)
            {
                progression_resets_details = new Dictionary<LootLockerItemDetailsKey, LootLockerProgressionResetDetails>();
                foreach (var detail in parsedResponse.progression_resets_details)
                {
                    progression_resets_details[detail.GetItemDetailsKey()] = detail;
                }
            }

            if (parsedResponse.currency_details != null && parsedResponse.currency_details.Length > 0)
            {
                currency_details = new Dictionary<LootLockerItemDetailsKey, LootLockerCurrencyDetails>();
                foreach (var detail in parsedResponse.currency_details)
                {
                    currency_details[detail.GetItemDetailsKey()] = detail;
                }
            }

            if (parsedResponse.group_details != null && parsedResponse.group_details.Length > 0)
            {
                group_details = new Dictionary<LootLockerItemDetailsKey, LootLockerGroupDetails>();
                foreach (var detail in parsedResponse.group_details)
                {
                    group_details[detail.GetItemDetailsKey()] = detail;
                }
            }
        }

        /// <summary>
        /// </summary>
        public class LootLockerInlinedCatalogEntry : LootLockerCatalogEntry
        {

            /// <summary>
            /// Asset details inlined for this catalog entry, will be null if the entity_kind is not asset
            /// </summary>
            public LootLockerAssetDetails asset_details { get; set; }

            /// <summary>
            /// Progression point details inlined for this catalog entry, will be null if the entity_kind is not progression_points
            /// </summary>
            public LootLockerProgressionPointDetails progression_point_details { get; set; }

            /// <summary>
            /// Progression reset details inlined for this catalog entry, will be null if the entity_kind is not progression_reset
            /// </summary>
            public LootLockerProgressionResetDetails progression_reset_details { get; set; }

            /// <summary>
            /// Currency details inlined for this catalog entry, will be null if the entity_kind is not currency
            /// </summary>
            public LootLockerCurrencyDetails currency_details { get; set; }

            /// <summary>
            /// Group details inlined for this catalog entry, will be null if the entity_kind is not group
            /// </summary>
            public LootLockerInlinedGroupDetails group_details { get; set; }

            public LootLockerInlinedCatalogEntry(LootLockerCatalogEntry entry, LootLockerListCatalogPricesResponse catalogListing)
            {
                created_at = entry.created_at;
                entity_kind = entry.entity_kind;
                entity_name = entry.entity_name;
                entity_id = entry.entity_id;
                listings = entry.listings;
                prices = entry.prices;
                catalog_listing_id = entry.catalog_listing_id;
                purchasable = entry.purchasable;

                switch (entity_kind)
                {
                    case LootLockerCatalogEntryEntityKind.asset:
                        if (catalogListing.asset_details.ContainsKey(entry.GetItemDetailsKey()))
                        {
                            asset_details = catalogListing.asset_details[entry.GetItemDetailsKey()];
                        }
                        break;
                    case LootLockerCatalogEntryEntityKind.currency:
                        if (catalogListing.currency_details.ContainsKey(entry.GetItemDetailsKey()))
                        {
                            currency_details = catalogListing.currency_details[entry.GetItemDetailsKey()];
                        }
                        break;
                    case LootLockerCatalogEntryEntityKind.progression_points:
                        if (catalogListing.progression_points_details.ContainsKey(entry.GetItemDetailsKey()))
                        {
                            progression_point_details = catalogListing.progression_points_details[entry.GetItemDetailsKey()];
                        }
                        break;
                    case LootLockerCatalogEntryEntityKind.progression_reset:
                        if (catalogListing.progression_resets_details.ContainsKey(entry.GetItemDetailsKey()))
                        {
                            progression_reset_details = catalogListing.progression_resets_details[entry.GetItemDetailsKey()];
                        }
                        break;
                    case LootLockerCatalogEntryEntityKind.group:
                        if (!catalogListing.group_details.ContainsKey(entry.GetItemDetailsKey()))
                            break;

                        var catalogLevelGroup = catalogListing.group_details[entry.GetItemDetailsKey()];

                        LootLockerInlinedGroupDetails inlinedGroupDetails = new LootLockerInlinedGroupDetails();

                        inlinedGroupDetails.name = catalogLevelGroup.name;
                        inlinedGroupDetails.description = catalogLevelGroup.description;
                        inlinedGroupDetails.metadata = catalogLevelGroup.metadata;
                        inlinedGroupDetails.id = catalogLevelGroup.id;
                        inlinedGroupDetails.associations = catalogLevelGroup.associations;

                        foreach (var association in catalogLevelGroup.associations)
                        {
                            switch (association.kind)
                            {
                                case LootLockerCatalogEntryEntityKind.asset:
                                    if (catalogListing.asset_details.ContainsKey(association.GetItemDetailsKey()))
                                    {
                                        inlinedGroupDetails.assetDetails.Add(catalogListing.asset_details[association.GetItemDetailsKey()]);
                                    }
                                    break;
                                case LootLockerCatalogEntryEntityKind.progression_points:
                                    if (catalogListing.progression_points_details.ContainsKey(association.GetItemDetailsKey()))
                                    {
                                        inlinedGroupDetails.progressionPointDetails.Add(catalogListing.progression_points_details[association.GetItemDetailsKey()]);
                                    }
                                    break;
                                case LootLockerCatalogEntryEntityKind.progression_reset:
                                    if (catalogListing.progression_resets_details.ContainsKey(association.GetItemDetailsKey()))
                                    {
                                        inlinedGroupDetails.progressionResetDetails.Add(catalogListing.progression_resets_details[association.GetItemDetailsKey()]);
                                    }
                                    break;
                                case LootLockerCatalogEntryEntityKind.currency:
                                    if (catalogListing.currency_details.ContainsKey(association.GetItemDetailsKey()))
                                    {
                                        inlinedGroupDetails.currencyDetails.Add(catalogListing.currency_details[association.GetItemDetailsKey()]);
                                    }
                                    break;
                                case LootLockerCatalogEntryEntityKind.group:
                                default:
                                    break;
                            }
                        }

                        group_details = inlinedGroupDetails;

                        break;
                    default:
                        break;
                }

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
                inlinedEntries.Add(new LootLockerInlinedCatalogEntry(
                    lootLockerCatalogEntry,
                    this
                    ));
            }
            return inlinedEntries.ToArray();
        }
    }
}
