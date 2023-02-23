﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using Newtonsoft.Json;
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

            string json = JsonConvert.SerializeObject(data);

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

            string json = JsonConvert.SerializeObject(data);

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

            string json = JsonConvert.SerializeObject(data);

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

            string json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.androidPurchaseVerification;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void PollOrderStatus(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerCharacterLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.pollingOrderStatus;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        [Obsolete("This function is deprecated and will be removed soon. Please use the function PollOrderStatus() instead")]
        public static void PollingOrderStatus(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerCharacterLoadoutResponse> onComplete)
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

        [Obsolete("This function is deprecated and will be removed soon. Please use the function ActivateRentalAsset() instead")]
        public static void ActivatingARentalAsset(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerActivateARentalAssetResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.activatingARentalAsset;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}