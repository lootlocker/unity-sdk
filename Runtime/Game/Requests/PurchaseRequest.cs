using LootLocker.Requests;
using System;
using LootLocker.LootLockerEnums;
using System.Collections.Generic;

#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
#else
using LLlibs.ZeroDepJson;
#endif

namespace LootLocker.LootLockerEnums
{
    public enum SteamPurchaseRedemptionStatus
    {
        Init,
        Approved,
        Succeeded,
        Failed,
        Refunded,
        PartialRefund,
        ChargedBack,
        RefundedSuspectedFraud,
        RefundedFriendlyFraud
    }
}

namespace LootLocker.Requests
{
    public class LootLockerPurchaseCatalogItemResponse : LootLockerResponse
    {

    }

    public class LootLockerPurchaseOrderStatus : LootLockerResponse
    {
        public string status { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerCatalogItemAndQuantityPair
    {
        /// <summary>
         /// The unique listing id of the catalog item to purchase
         /// </summary>
        public string catalog_listing_id { get; set; }
        /// <summary>
         /// The quantity of the specified item to purchase
         /// </summary>
        public int quantity { get; set; }
    }

    /// <summary>
     /// 
     /// </summary>
    public class LootLockerPurchaseCatalogItemRequest
    {
        /// <summary>
         /// The id of the wallet to be used for the purchase
         /// </summary>
        public string wallet_id { get; set; }
        /// <summary>
         /// A list of items to purchase
         /// </summary>
        public LootLockerCatalogItemAndQuantityPair[] items { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerRedeemAppleAppStorePurchaseForPlayerRequest
    {
        /// <summary>
        /// Whether or not to use the app store sandbox for this redemption
        /// </summary>
        public bool sandboxed { get; set; } = false;
        /// <summary>
        /// The id of the transaction successfully made towards the Apple App Store
        /// </summary>
        public string transaction_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerRedeemAppleAppStorePurchaseForClassRequest : LootLockerRedeemAppleAppStorePurchaseForPlayerRequest
    {
        /// <summary>
        /// The id of the class to redeem this transaction for
        /// </summary>
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("character_id")]
#else
        [Json(Name = "character_id")]
#endif
        public int class_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerRedeemGooglePlayStorePurchaseForPlayerRequest
    {
        /// <summary>
        /// The id of the product that this redemption refers to
        /// </summary>
        public string product_id { get; set; }
        /// <summary>
        /// The token from the purchase successfully made towards the Google Play Store
        /// </summary>
        public string purchase_token { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerRedeemGooglePlayStorePurchaseForClassRequest : LootLockerRedeemGooglePlayStorePurchaseForPlayerRequest
    {
        /// <summary>
        /// The id of the class to redeem this purchase for
        /// </summary>
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("character_id")]
#else
        [Json(Name = "character_id")]
#endif
        public int class_id { get; set; }
    }

    public class LootLockerRedeemEpicStorePurchaseForPlayerRequest
    {
        /// <summary>
        /// The epic account id of the account that this purchase was made for
        /// </summary>
        public string account_id;
        /// <summary>
        /// This is the token from epic used to allow the LootLocker backend to verify ownership of the specified entitlements. This is sometimes referred to as the Server Auth Ticket or Auth Token depending on your Epic integration.
        /// </summary>
        public string bearer_token;
        /// <summary>
        /// The ids of the purchased entitlements that you wish to redeem
        /// </summary>
        public List<string> entitlement_ids;
        /// <summary>
        /// The Sandbox Id configured for the game making the purchase (this is the sandbox id from your epic online service configuration)
        /// </summary>
        public string sandbox_id;
    }

    public class LootLockerRedeemEpicStorePurchaseForClassRequest : LootLockerRedeemEpicStorePurchaseForPlayerRequest
    {
        /// <summary>
        /// The ulid of the character to redeem this purchase for
        /// </summary>
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonProperty("character_id")]
#else
        [Json(Name = "character_id")]
#endif
        public int class_id;
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerBeginSteamPurchaseRedemptionRequest
    {
        /// <summary>
        /// Id of the Steam User that is making the purchase
        /// </summary>
        public string steam_id { get; set; }
        /// <summary>
        /// The currency to use for the purchase
        /// </summary>
        public string currency { get; set; }
        /// <summary>
        /// The language to use for the purchase
        /// </summary>
        public string language { get; set; }
        /// <summary>
        /// The LootLocker Catalog Item Id for the item you wish to purchase
        /// </summary>
        public string catalog_item_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerBeginSteamPurchaseRedemptionForClassRequest : LootLockerBeginSteamPurchaseRedemptionRequest
    {
        /// <summary>
        /// Id of the class to make the purchase for
        /// </summary>
        public int class_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerBeginSteamPurchaseRedemptionResponse : LootLockerResponse
    {
        /// <summary>
        /// Was the purchase redemption process started successfully
        /// </summary>
        public bool isSuccess { get; set; }
        /// <summary>
        /// The id of the entitlement this purchase relates to
        /// </summary>
        public string entitlement_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerQuerySteamPurchaseRedemptionStatusRequest
    {
        /// <summary>
        /// The id of the entitlement to check the status for
        /// </summary>
        public string entitlement_id { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerQuerySteamPurchaseRedemptionStatusResponse : LootLockerResponse
    {
        /// <summary>
        /// The status of the steam purchase
        /// </summary>
        public SteamPurchaseRedemptionStatus status { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LootLockerFinalizeSteamPurchaseRedemptionRequest
    {
        /// <summary>
        /// The id of the entitlement to finalize the purchase for
        /// </summary>
        public string entitlement_id { get; set; }
    }

    /// <summary>
    /// Request to refund one or more entitlements by their IDs.
    /// </summary>
    public class LootLockerRefundByEntitlementIdsRequest
    {
        /// <summary>
        /// The IDs of the entitlements to refund
        /// </summary>
        public string[] entitlement_ids { get; set; }
    }

    /// <summary>
    /// Represents the action taken on a player inventory item during a refund.
    /// </summary>
    public class LootLockerRefundPlayerInventoryEvent
    {
        /// <summary>
        /// The legacy numeric asset ID
        /// </summary>
        public ulong asset_id { get; set; }
        /// <summary>
        /// Display name of the asset
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// "removed" if taken back from inventory, "skipped" if it could not be removed (e.g. already consumed)
        /// </summary>
        public string action { get; set; }
    }

    /// <summary>
    /// Represents a currency entry (amount credited or debited) as part of a refund
    /// </summary>
    public class LootLockerRefundCurrencyEntry
    {
        /// <summary>
        /// The ULID of the currency
        /// </summary>
        public string currency_id { get; set; }
        /// <summary>
        /// Short code identifying the currency (e.g. "gold", "gems")
        /// </summary>
        public string currency_code { get; set; }
        /// <summary>
        /// The amount credited or debited, as a string to support arbitrary precision
        /// </summary>
        public string amount { get; set; }
    }

    /// <summary>
    /// Represents a non-reversible reward that was granted alongside an entitlement and could not be clawed back
    /// </summary>
    public class LootLockerRefundNonReversibleReward
    {
        /// <summary>
        /// "progression_points": points were added to a progression.
        /// "progression_reset": a progression was reset to its initial state.
        /// </summary>
        public string kind { get; set; }
        /// <summary>
        /// The ULID of the progression that was affected
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// Display name of the progression
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The number of points that were granted and cannot be reversed. Only present for kind "progression_points".
        /// </summary>
        public string amount { get; set; }
    }

    /// <summary>
    /// Represents a single warning detail for a refund
    /// </summary>
    public class LootLockerRefundWarningDetail
    {
        /// <summary>
        /// The warning category:
        /// "non_reversible_rewards": rewards granted that cannot be automatically clawed back.
        /// "insufficient_funds": the player does not have enough currency balance to cover the clawback.
        /// "already_refunded": the entitlement was already refunded before this request.
        /// "refund_failed": the entitlement could not be refunded due to an unexpected error.
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// Human-readable explanation of the warning
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// The specific rewards that could not be reversed. Only present when type is "non_reversible_rewards".
        /// </summary>
        public LootLockerRefundNonReversibleReward[] rewards { get; set; }
    }

    /// <summary>
    /// Warnings for a specific entitlement during refund processing
    /// </summary>
    public class LootLockerRefundWarning
    {
        /// <summary>
        /// The entitlement this warning applies to
        /// </summary>
        public string entitlement_id { get; set; }
        /// <summary>
        /// One or more warning conditions for this entitlement
        /// </summary>
        public LootLockerRefundWarningDetail[] details { get; set; }
    }

    /// <summary>
    /// Response from the refund by entitlement IDs endpoint
    /// </summary>
    public class LootLockerRefundByEntitlementIdsResponse : LootLockerResponse
    {
        /// <summary>
        /// Assets that were added or removed from the player's inventory as part of the refund.
        /// </summary>
        public LootLockerRefundPlayerInventoryEvent[] player_inventory_events { get; set; }
        /// <summary>
        /// Currency amounts credited back to the player's wallet (the purchase price being returned).
        /// </summary>
        public LootLockerRefundCurrencyEntry[] currency_refunded { get; set; }
        /// <summary>
        /// Currency amounts debited from the player's wallet (currency rewards from the entitlement being reclaimed).
        /// </summary>
        public LootLockerRefundCurrencyEntry[] currency_clawback { get; set; }
        /// <summary>
        /// Warnings encountered during refund processing, grouped by entitlement.
        /// A non-empty warnings array does not mean the refund failed — it means some aspects could not be fully reversed.
        /// </summary>
        public LootLockerRefundWarning[] warnings { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {

        public static void PollOrderStatus(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, Action<LootLockerPurchaseOrderStatus> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.pollingOrderStatus;

            string getVariable = endPoint.WithPathParameter(lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, "", (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ActivateRentalAsset(string forPlayerWithUlid, LootLockerGetRequest lootLockerGetRequest, Action<LootLockerActivateRentalAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.activatingARentalAsset;

            string getVariable = endPoint.WithPathParameter(lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, getVariable, endPoint.httpMethod, "", (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void RefundByEntitlementIds(string forPlayerWithUlid, string[] entitlementIds, Action<LootLockerRefundByEntitlementIdsResponse> onComplete)
        {
            if (entitlementIds == null || entitlementIds.Length == 0)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerRefundByEntitlementIdsResponse>(forPlayerWithUlid));
                return;
            }
            EndPointClass endPoint = LootLockerEndPoints.refundByEntitlementIds;
            var body = LootLockerJson.SerializeObject(new LootLockerRefundByEntitlementIdsRequest { entitlement_ids = entitlementIds });
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, body, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: true);
        }
    }
}