using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;
using Newtonsoft.Json;
using System;

namespace LootLockerRequests
{
    public class PurchaseRequests
    {

    }

    public class NormalPurchaseRequest
    {
        public int asset_id { get; set; }
        public int variation_id { get; set; }
    }

    public class RentalPurchaseRequest
    {
        public int asset_id { get; set; }
        public int variation_id { get; set; }
        public int rental_option_id { get; set; }
    }


    public class PurchaseResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public bool overlay { get; set; }
        public int order_id { get; set; }
    }

    public class IosPurchaseVerificationRequest
    {
        public string receipt_data { get; set; }
    }

    public class AndroidPurchaseVerificationRequest
    {
        public int asset_id { get; set; }
        public string purchase_token { get; set; }
    }
}

namespace LootLocker
{
 
        public partial class LootLockerAPIManager
        {
            public static void NormalPurchaseCall(NormalPurchaseRequest[] data, Action<PurchaseResponse> onComplete)
            {
                string json = "";
                if (data == null) return;
                else json = JsonConvert.SerializeObject(data);

                EndPointClass endPoint = LootLockerEndPoints.current.normalPurchaseCall;

                ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
                {
                    PurchaseResponse response = new PurchaseResponse();
                    if (string.IsNullOrEmpty(serverResponse.Error))
                    {
                        response = JsonConvert.DeserializeObject<PurchaseResponse>(serverResponse.text);
                        response.text = serverResponse.text;
                        onComplete?.Invoke(response);
                    }
                    else
                    {
                        response.message = serverResponse.message;
                        response.Error = serverResponse.Error;
                        onComplete?.Invoke(response);
                    }
                }, true);
            }

            public static void RentalPurchaseCall(RentalPurchaseRequest data, Action<PurchaseResponse> onComplete)
            {
                string json = "";
                if (data == null) return;
                else json = JsonConvert.SerializeObject(data);

                EndPointClass endPoint = LootLockerEndPoints.current.rentalPurchaseCall;

                ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod,json, (serverResponse) =>
                {
                    PurchaseResponse response = new PurchaseResponse();
                    if (string.IsNullOrEmpty(serverResponse.Error))
                    {
                        response = JsonConvert.DeserializeObject<PurchaseResponse>(serverResponse.text);
                        response.text = serverResponse.text;
                        onComplete?.Invoke(response);
                    }
                    else
                    {
                        response.message = serverResponse.message;
                        response.Error = serverResponse.Error;
                        onComplete?.Invoke(response);
                    }
                }, true);
            }

            public static void IosPurchaseVerification(IosPurchaseVerificationRequest[] data, Action<PurchaseResponse> onComplete)
            {
                string json = "";
                if (data == null) return;
                else json = JsonConvert.SerializeObject(data);

                EndPointClass endPoint = LootLockerEndPoints.current.iosPurchaseVerification;

                ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
                {
                    PurchaseResponse response = new PurchaseResponse();
                    if (string.IsNullOrEmpty(serverResponse.Error))
                    {
                        response = JsonConvert.DeserializeObject<PurchaseResponse>(serverResponse.text);
                        response.text = serverResponse.text;
                        onComplete?.Invoke(response);
                    }
                    else
                    {
                        response.message = serverResponse.message;
                        response.Error = serverResponse.Error;
                        onComplete?.Invoke(response);
                    }
                }, true);
            }

            public static void AndroidPurchaseVerification(AndroidPurchaseVerificationRequest[] data, Action<PurchaseResponse> onComplete)
            {
                string json = "";
                if (data == null) return;
                else json = JsonConvert.SerializeObject(data);

                EndPointClass endPoint = LootLockerEndPoints.current.androidPurchaseVerification;

                ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
                {
                    PurchaseResponse response = new PurchaseResponse();
                    if (string.IsNullOrEmpty(serverResponse.Error))
                    {
                        response = JsonConvert.DeserializeObject<PurchaseResponse>(serverResponse.text);
                        response.text = serverResponse.text;
                        onComplete?.Invoke(response);
                    }
                    else
                    {
                        response.message = serverResponse.message;
                        response.Error = serverResponse.Error;
                        onComplete?.Invoke(response);
                    }
                }, true);
            }

            public static void PollingOrderStatus(LootLockerGetRequest lootLockerGetRequest, Action<CharacterLoadoutResponse> onComplete)
            {
                EndPointClass endPoint = LootLockerEndPoints.current.pollingOrderStatus;

                string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

                ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
                {
                    CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                    if (string.IsNullOrEmpty(serverResponse.Error))
                    {
                        response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                        response.text = serverResponse.text;
                        onComplete?.Invoke(response);
                    }
                    else
                    {
                        response.message = serverResponse.message;
                        response.Error = serverResponse.Error;
                        onComplete?.Invoke(response);
                    }
                }, true);
            }

            public static void ActivatingARentalAsset(LootLockerGetRequest lootLockerGetRequest, Action<CharacterLoadoutResponse> onComplete)
            {
                EndPointClass endPoint = LootLockerEndPoints.current.activatingARentalAsset;

                string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

                ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
                {
                    CharacterLoadoutResponse response = new CharacterLoadoutResponse();
                    if (string.IsNullOrEmpty(serverResponse.Error))
                    {
                        response = JsonConvert.DeserializeObject<CharacterLoadoutResponse>(serverResponse.text);
                        response.text = serverResponse.text;
                        onComplete?.Invoke(response);
                    }
                    else
                    {
                        response.message = serverResponse.message;
                        response.Error = serverResponse.Error;
                        onComplete?.Invoke(response);
                    }
                }, true);
            }
        }
    
}