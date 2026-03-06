using LootLocker.Requests;
using System;
using System.Collections;
using LootLocker.LootLockerEnums;
using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>
    /// The action taken on a player inventory item as part of a refund
    /// </summary>
    public enum LootLockerRefundInventoryEventAction
    {
        /// <summary>The item was successfully removed from the player's inventory</summary>
        removed = 0,
        /// <summary>The item could not be removed (e.g. already consumed) and was left in place</summary>
        skipped = 1,
    }

    /// <summary>
    /// The kind of non-reversible reward that was granted alongside an entitlement
    /// </summary>
    public enum LootLockerRefundNonReversibleRewardKind
    {
        /// <summary>Points were added to a progression and cannot be taken back</summary>
        progression_points = 0,
        /// <summary>A progression was reset to its initial state and cannot be undone</summary>
        progression_reset = 1,
    }

    /// <summary>
    /// The category of a per-entitlement warning returned during a refund
    /// </summary>
    public enum LootLockerRefundWarningType
    {
        /// <summary>Rewards granted that cannot be automatically clawed back</summary>
        non_reversible_rewards = 0,
        /// <summary>The player does not have enough currency balance to cover the clawback</summary>
        insufficient_funds = 1,
        /// <summary>The entitlement was already refunded before this request</summary>
        already_refunded = 2,
        /// <summary>The entitlement could not be refunded due to an unexpected error</summary>
        refund_failed = 3,
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
    /// The possible statuses of an async purchase entitlement
    /// </summary>
    public enum LootLockerAsyncPurchaseStatus
    {
        /// <summary>The purchase is still being processed</summary>
        pending,
        /// <summary>The purchase completed successfully and items have been granted</summary>
        active,
        /// <summary>The purchase failed; see the error field for the reason</summary>
        failed
    }

    /// <summary>
    /// Response returned when an async purchase is successfully initiated or retried (HTTP 202)
    /// </summary>
    public class LootLockerAsyncPurchaseInitiatedResponse : LootLockerResponse
    {
        /// <summary>
        /// The entitlement id for this async purchase. Use this to poll for status or retry on failure.
        /// </summary>
        public string entitlement_id { get; set; }
    }

    /// <summary>
    /// Response returned when polling the status of an async purchase
    /// </summary>
    public class LootLockerAsyncPurchaseStatusResponse : LootLockerResponse
    {
        /// <summary>
        /// The entitlement id for this async purchase
        /// </summary>
        public string entitlement_id { get; set; }
        /// <summary>
        /// The current status of the purchase: pending, active, or failed
        /// </summary>
        public LootLockerAsyncPurchaseStatus status { get; set; }
        /// <summary>
        /// The failure reason. Only populated when status is failed.
        /// </summary>
        public string error { get; set; }
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
        /// The action taken on this item: removed if taken back from inventory, skipped if it could not be removed (e.g. already consumed)
        /// </summary>
        public LootLockerRefundInventoryEventAction action { get; set; }
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
        /// The kind of non-reversible reward: progression_points if points were added to a progression, progression_reset if a progression was reset to its initial state
        /// </summary>
        public LootLockerRefundNonReversibleRewardKind kind { get; set; }
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
        /// The warning category: non_reversible_rewards if rewards granted cannot be automatically clawed back, insufficient_funds if the player does not have enough currency balance to cover the clawback, already_refunded if the entitlement was already refunded before this request, refund_failed if the entitlement could not be refunded due to an unexpected error
        /// </summary>
        public LootLockerRefundWarningType type { get; set; }
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

        public static void InitiateAsyncPurchase(string forPlayerWithUlid, string walletId, LootLockerCatalogItemAndQuantityPair[] items, Action<LootLockerAsyncPurchaseInitiatedResponse> onComplete)
        {
            var body = LootLockerJson.SerializeObject(new LootLockerPurchaseCatalogItemRequest
            {
                wallet_id = walletId,
                items = items
            });
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, LootLockerEndPoints.initiateAsyncPurchase.endPoint, LootLockerEndPoints.initiateAsyncPurchase.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetAsyncPurchaseStatus(string forPlayerWithUlid, string entitlementId, Action<LootLockerAsyncPurchaseStatusResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.pollAsyncPurchaseStatus;
            string url = endPoint.WithPathParameter(entitlementId);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, url, endPoint.httpMethod, "", onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void RetryAsyncPurchase(string forPlayerWithUlid, string entitlementId, string walletId, LootLockerCatalogItemAndQuantityPair[] items, Action<LootLockerAsyncPurchaseInitiatedResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.retryAsyncPurchase;
            string url = endPoint.WithPathParameter(entitlementId);
            var body = LootLockerJson.SerializeObject(new LootLockerPurchaseCatalogItemRequest
            {
                wallet_id = walletId,
                items = items
            });
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, url, endPoint.httpMethod, body, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public class AsyncPurchasePoller : MonoBehaviour, ILootLockerService
        {
            #region ILootLockerService Implementation

            public bool IsInitialized { get; private set; }
            public string ServiceName => "AsyncPurchasePoller";

            void ILootLockerService.Initialize()
            {
                if (IsInitialized) return;
                IsInitialized = true;
            }

            void ILootLockerService.Reset()
            {
                if (_asyncPurchaseProcesses != null)
                {
                    foreach (var process in _asyncPurchaseProcesses.Values)
                    {
                        if (process != null)
                        {
                            process.ShouldCancel = true;
                        }
                    }
                    _asyncPurchaseProcesses.Clear();
                }

                IsInitialized = false;
                _instance = null;
            }

            void ILootLockerService.HandleApplicationPause(bool pauseStatus) { }

            void ILootLockerService.HandleApplicationFocus(bool hasFocus) { }

            void ILootLockerService.HandleApplicationQuit()
            {
                ((ILootLockerService)this).Reset();
            }

            #endregion

            #region Hybrid Singleton Pattern

            private static AsyncPurchasePoller _instance;
            private static readonly object _instanceLock = new object();

            private static AsyncPurchasePoller GetInstance()
            {
                if (_instance != null) return _instance;

                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        if (!LootLockerLifecycleManager.HasService<AsyncPurchasePoller>())
                        {
                            LootLockerLifecycleManager.RegisterService<AsyncPurchasePoller>();
                        }
                        _instance = LootLockerLifecycleManager.GetService<AsyncPurchasePoller>();
                    }
                }

                return _instance;
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Initiate an async purchase and continuously poll until the purchase is active or failed.
            /// </summary>
            /// <param name="walletId">The wallet id to use for the purchase.</param>
            /// <param name="items">The catalog items with quantities to purchase.</param>
            /// <param name="onStatusUpdate">Called on each poll while the purchase is still pending.</param>
            /// <param name="onComplete">Called when the purchase reaches a terminal state (active, failed, timeout, or cancellation).</param>
            /// <param name="pollingIntervalSeconds">How often to poll. Minimum 1 second (rate-limit enforced).</param>
            /// <param name="timeoutAfterMinutes">How many minutes before the process is considered timed out.</param>
            /// <param name="forPlayerWithUlid">The ULID of the player to initiate the purchase for, or null for the default player.</param>
            /// <returns>A Guid identifying this process. Use it to cancel polling via CancelAsyncPurchasePolling.</returns>
            public static Guid StartAsyncPurchasePolling(
                string walletId,
                LootLockerCatalogItemAndQuantityPair[] items,
                Action<LootLockerAsyncPurchaseStatusResponse> onStatusUpdate,
                Action<LootLockerAsyncPurchaseStatusResponse> onComplete,
                float pollingIntervalSeconds = 1.0f,
                float timeoutAfterMinutes = 5.0f,
                string forPlayerWithUlid = null)
            {
                pollingIntervalSeconds = Math.Max(1.0f, pollingIntervalSeconds);
                var instance = GetInstance();
                if (instance == null)
                {
                    onComplete?.Invoke(new LootLockerAsyncPurchaseStatusResponse
                    {
                        success = false,
                        errorData = new LootLockerErrorData { message = "Failed to start async purchase polling: AsyncPurchasePoller instance could not be created." }
                    });
                    return Guid.Empty;
                }
                return instance._StartAsyncPurchasePolling(walletId, items, onStatusUpdate, onComplete, pollingIntervalSeconds, timeoutAfterMinutes, forPlayerWithUlid);
            }

            /// <summary>
            /// Cancel an ongoing async purchase polling process.
            /// </summary>
            /// <param name="processGuid">The Guid returned by StartAsyncPurchasePolling.</param>
            public static void CancelAsyncPurchasePolling(Guid processGuid)
            {
                GetInstance()?._CancelAsyncPurchasePolling(processGuid);
            }

            #endregion

            #region Internal Workings

            private static readonly int _asyncPurchasePollingRetryLimit = 5;

            private class LootLockerAsyncPurchaseProcess
            {
                public string EntitlementId;
                public string WalletId;
                public LootLockerCatalogItemAndQuantityPair[] Items;
                public DateTime TimeoutTime;
                public float PollingIntervalSeconds = 1.0f;
                public int Retries = 0;
                public bool ShouldCancel;
                public string ForPlayerWithUlid;
                public Action<LootLockerAsyncPurchaseStatusResponse> StatusUpdateCallback;
                public Action<LootLockerAsyncPurchaseStatusResponse> CompletedCallback;
            }

            private readonly Dictionary<Guid, LootLockerAsyncPurchaseProcess> _asyncPurchaseProcesses =
                new Dictionary<Guid, LootLockerAsyncPurchaseProcess>();

            private static void RemoveAsyncPurchaseProcess(Guid processGuid)
            {
                var i = GetInstance();
                if (i == null) return;
                i._asyncPurchaseProcesses.Remove(processGuid);
                if (i._asyncPurchaseProcesses.Count <= 0)
                {
                    CleanupServiceWhenDone();
                }
            }

            private static void CleanupServiceWhenDone()
            {
                if (LootLockerLifecycleManager.HasService<AsyncPurchasePoller>())
                {
                    LootLockerLogger.Log("All async purchase processes complete - cleaning up AsyncPurchasePoller", LootLockerLogger.LogLevel.Debug);
                    _instance = null;
                    LootLockerLifecycleManager.UnregisterService<AsyncPurchasePoller>();
                }
            }

            private IEnumerator ContinualPollAction(Guid processGuid)
            {
                if (!_asyncPurchaseProcesses.TryGetValue(processGuid, out var preProcess))
                {
                    yield break;
                }
                yield return new WaitForSeconds(preProcess.PollingIntervalSeconds);

                while (_asyncPurchaseProcesses.TryGetValue(processGuid, out var process))
                {
                    if (process.TimeoutTime <= DateTime.UtcNow)
                    {
                        process.CompletedCallback?.Invoke(new LootLockerAsyncPurchaseStatusResponse
                        {
                            success = false,
                            errorData = new LootLockerErrorData { message = "Async purchase polling timed out." }
                        });
                        RemoveAsyncPurchaseProcess(processGuid);
                        yield break;
                    }

                    if (process.ShouldCancel)
                    {
                        process.CompletedCallback?.Invoke(new LootLockerAsyncPurchaseStatusResponse
                        {
                            success = false,
                            errorData = new LootLockerErrorData { message = "Async purchase polling cancelled." }
                        });
                        RemoveAsyncPurchaseProcess(processGuid);
                        yield break;
                    }

                    LootLockerAsyncPurchaseStatusResponse statusResponse = null;
                    GetAsyncPurchaseStatus(process.ForPlayerWithUlid, process.EntitlementId, response => { statusResponse = response; });
                    yield return new WaitUntil(() => statusResponse != null);

                    if (!_asyncPurchaseProcesses.TryGetValue(processGuid, out var processAfterPoll))
                    {
                        yield break;
                    }

                    if (!statusResponse.success)
                    {
                        if (statusResponse.statusCode >= 500 && statusResponse.statusCode <= 599 && processAfterPoll.Retries < _asyncPurchasePollingRetryLimit)
                        {
                            processAfterPoll.Retries++;
                            yield return new WaitForSeconds(processAfterPoll.PollingIntervalSeconds);
                            continue;
                        }
                        processAfterPoll.CompletedCallback?.Invoke(statusResponse);
                        RemoveAsyncPurchaseProcess(processGuid);
                        yield break;
                    }

                    processAfterPoll.Retries = 0;

                    if (statusResponse.status == LootLockerAsyncPurchaseStatus.active || statusResponse.status == LootLockerAsyncPurchaseStatus.failed)
                    {
                        processAfterPoll.CompletedCallback?.Invoke(statusResponse);
                        RemoveAsyncPurchaseProcess(processGuid);
                        yield break;
                    }

                    // Still pending — notify and wait
                    processAfterPoll.StatusUpdateCallback?.Invoke(statusResponse);
                    yield return new WaitForSeconds(processAfterPoll.PollingIntervalSeconds);
                }
            }

            private Guid _StartAsyncPurchasePolling(
                string walletId,
                LootLockerCatalogItemAndQuantityPair[] items,
                Action<LootLockerAsyncPurchaseStatusResponse> onStatusUpdate,
                Action<LootLockerAsyncPurchaseStatusResponse> onComplete,
                float pollingIntervalSeconds,
                float timeoutAfterMinutes,
                string forPlayerWithUlid)
            {
                Guid processGuid = Guid.NewGuid();
                var process = new LootLockerAsyncPurchaseProcess
                {
                    WalletId = walletId,
                    Items = items,
                    TimeoutTime = DateTime.UtcNow.AddMinutes(timeoutAfterMinutes),
                    PollingIntervalSeconds = pollingIntervalSeconds,
                    StatusUpdateCallback = onStatusUpdate,
                    CompletedCallback = onComplete,
                    ForPlayerWithUlid = forPlayerWithUlid
                };
                _asyncPurchaseProcesses.Add(processGuid, process);

                InitiateAsyncPurchase(forPlayerWithUlid, walletId, items, initiateResponse =>
                {
                    if (!_asyncPurchaseProcesses.TryGetValue(processGuid, out var p))
                    {
                        return;
                    }
                    if (!initiateResponse.success)
                    {
                        RemoveAsyncPurchaseProcess(processGuid);
                        onComplete?.Invoke(new LootLockerAsyncPurchaseStatusResponse
                        {
                            success = false,
                            statusCode = initiateResponse.statusCode,
                            text = initiateResponse.text,
                            errorData = initiateResponse.errorData,
                            requestContext = initiateResponse.requestContext
                        });
                        return;
                    }
                    p.EntitlementId = initiateResponse.entitlement_id;
                    StartCoroutine(ContinualPollAction(processGuid));
                });

                return processGuid;
            }

            private void _CancelAsyncPurchasePolling(Guid processGuid)
            {
                if (_asyncPurchaseProcesses.TryGetValue(processGuid, out var process))
                {
                    process.ShouldCancel = true;
                }
            }

            #endregion
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