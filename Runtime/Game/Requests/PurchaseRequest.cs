using LootLocker.Requests;
using System;

namespace LootLocker.Requests
{
    public class LootLockerPurchaseRequests
    {
    }

    public class LootLockerNormalPurchaseRequest
    {
        public int asset_id { get; set; }
        public int variation_id { get; set; }
    }

    public class LootLockerRentalPurchaseRequest
    {
        public int asset_id { get; set; }
        public int variation_id { get; set; }
        public int rental_option_id { get; set; }
    }


    public class LootLockerPurchaseResponse : LootLockerResponse
    {
        public bool overlay { get; set; }
        public int order_id { get; set; }
    }

    public class LootLockerIosPurchaseVerificationRequest
    {
        public string receipt_data { get; set; }
    }

    public class LootLockerAndroidPurchaseVerificationRequest
    {
        public int asset_id { get; set; }
        public string purchase_token { get; set; }
    }

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
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void NormalPurchaseCall(LootLockerNormalPurchaseRequest[] data, Action<LootLockerPurchaseResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerPurchaseResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.normalPurchaseCall;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void RentalPurchaseCall(LootLockerRentalPurchaseRequest data, Action<LootLockerPurchaseResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerPurchaseResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.rentalPurchaseCall;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void IosPurchaseVerification(LootLockerIosPurchaseVerificationRequest[] data, Action<LootLockerPurchaseResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerPurchaseResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.iosPurchaseVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void AndroidPurchaseVerification(LootLockerAndroidPurchaseVerificationRequest[] data, Action<LootLockerPurchaseResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerPurchaseResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.androidPurchaseVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void PollOrderStatus(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerPurchaseOrderStatus> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.pollingOrderStatus;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ActivateRentalAsset(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerActivateRentalAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.activatingARentalAsset;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}