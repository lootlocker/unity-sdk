using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;

namespace LootLocker.Requests
{

    public class LootLockerGetPlayerInfoResponse : LootLockerResponse
    {
        
        public int? account_balance { get; set; }
        public int? xp { get; set; }
        public int? level { get; set; }
        public LootLockerLevel_Thresholds level_thresholds { get; set; }
    }

    [System.Serializable]
    public class LootLockerStandardResponse : LootLockerResponse
    {
        
    }

    [System.Serializable]
    public class PlayerNameRequest 
    {
        public string name { get; set; }
    }

    [System.Serializable]
    public class PlayerNameResponse : LootLockerResponse
    {
        public string name { get; set; }
    }
    [System.Serializable]
    public class LootLockerDlcResponse : LootLockerResponse
    {
        
        public string[] dlcs { get; set; }
    }

    [System.Serializable]
    public class LootLockerDeactivatedAssetsResponse : LootLockerResponse
    {
        
        public LootLockerDeactivatedObjects[] objects { get; set; }
    }
    [System.Serializable]
    public class LootLockerDeactivatedObjects
    {
        public int deactivated_asset_id { get; set; }
        public int replacement_asset_id { get; set; }
        public string reason { get; set; }
    }


    [System.Serializable]
    public class LootLockerBalanceResponse : LootLockerResponse
    {
        
        public int? balance { get; set; }
    }

    [System.Serializable]
    public class LootLockerXpSubmitResponse : LootLockerResponse
    {
        
        public LootLockerXp xp { get; set; }
        public LootLockerLevel[] levels { get; set; }
        public bool check_grant_notifications { get; set; }
    }

    [System.Serializable]
    public class LootLockerXpResponse : LootLockerResponse
    {
        
        public int? xp { get; set; }
        public int? level { get; set; }
    }
    [System.Serializable]
    public class LootLockerXp
    {
        public int? previous { get; set; }
        public int? current { get; set; }
    }
    [System.Serializable]
    public class LootLockerLevel
    {
        public int? level { get; set; }
        public int? xp_threshold { get; set; }
    }

    [System.Serializable]

    public class LootLockerInventoryResponse : LootLockerResponse
    {
        public LootLockerInventory[] inventory;
    }

    public class LootLockerInventory
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public string rental_option_id { get; set; }
        public string acquisition_source { get; set; }
        public LootLockerCommonAsset asset { get; set; }
        public LootLockerRental rental { get; set; }


        public float balance;
    }
    [System.Serializable]
    public class LootLockerAssetClass
    {
        public string Asset { get; set; }
    }
    [System.Serializable]
    public class LootLockerRental
    {
        public bool is_rental { get; set; }
        public string time_left { get; set; }
        public string duration { get; set; }
        public string is_active { get; set; }
    }
    [System.Serializable]
    public class LootLockerXpSubmitRequest
    {
        public int? points;

        public LootLockerXpSubmitRequest(int points)
        {
            this.points = points;
        }
    }
    [System.Serializable]
    public class LootLockerXpRequest : LootLockerGetRequest
    {
        public LootLockerXpRequest()
        {
            getRequests.Clear();
            getRequests.Add(LootLockerConfig.current.deviceID);
            getRequests.Add(LootLockerConfig.current.platform.ToString());
        }
    }

    public class LootLockerPlayerAssetNotificationsResponse : LootLockerResponse
    {
        
        public LootLockerRewardObject[] objects { get; set; }
    }

    public class LootLockerRewardObject
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public string acquisition_source { get; set; }
        public LootLockerCommonAsset asset { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetPlayerInfo(Action<LootLockerGetPlayerInfoResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getPlayerInfo;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerGetPlayerInfoResponse response = new LootLockerGetPlayerInfoResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGetPlayerInfoResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetInventory(Action<LootLockerInventoryResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getInventory;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
               {
                   LootLockerInventoryResponse response = new LootLockerInventoryResponse();
                   if (string.IsNullOrEmpty(serverResponse.Error))
                       response = JsonConvert.DeserializeObject<LootLockerInventoryResponse>(serverResponse.text);

                   //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                   response.text = serverResponse.text;
                        response.success = serverResponse.success;
               response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                   onComplete?.Invoke(response);
               }, true);
        }

        public static void GetBalance(Action<LootLockerBalanceResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getCurrencyBalance;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                LootLockerBalanceResponse response = new LootLockerBalanceResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerBalanceResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void SubmitXp(LootLockerXpSubmitRequest data, Action<LootLockerXpSubmitResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.submitXp;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerXpSubmitResponse response = new LootLockerXpSubmitResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerXpSubmitResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetXpAndLevel(LootLockerGetRequest data, Action<LootLockerXpResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getXpAndLevel;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerXpResponse response = new LootLockerXpResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerXpResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetPlayerAssetNotification(Action<LootLockerPlayerAssetNotificationsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.playerAssetNotifications;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerPlayerAssetNotificationsResponse response = new LootLockerPlayerAssetNotificationsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerPlayerAssetNotificationsResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetDeactivatedAssetNotification(Action<LootLockerDeactivatedAssetsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.playerAssetDeactivationNotification;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerDeactivatedAssetsResponse response = new LootLockerDeactivatedAssetsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerDeactivatedAssetsResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void InitiateDLCMigration(Action<LootLockerDlcResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.initiateDlcMigration;

            string getVariable = endPoint.endPoint;
            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerDlcResponse response = new LootLockerDlcResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerDlcResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetDLCMigrated(Action<LootLockerDlcResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getDlcMigration;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerDlcResponse response = new LootLockerDlcResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerDlcResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void SetProfilePrivate(Action<LootLockerStandardResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.setProfilePrivate;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerStandardResponse response = new LootLockerStandardResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerStandardResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void SetProfilePublic(Action<LootLockerStandardResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.setProfilePublic;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerStandardResponse response = new LootLockerStandardResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerStandardResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void GetPlayerName(Action<PlayerNameResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getPlayerName;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (Action<LootLockerResponse>)((serverResponse) =>
            {
                PlayerNameResponse response = new PlayerNameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<PlayerNameResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; 
                response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }), true);
        }

        public static void SetPlayerName(PlayerNameRequest data, Action<PlayerNameResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.setPlayerName;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (Action<LootLockerResponse>)((serverResponse) =>
            {
                PlayerNameResponse response = new PlayerNameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<PlayerNameResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error;
                response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }), true);
        }
    }

}