using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;

namespace LootLockerRequests
{

    public class GetPlayerInfoResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int? account_balance { get; set; }
        public int? xp { get; set; }
        public int? level { get; set; }
        public Level_Thresholds level_thresholds { get; set; }
    }

    [System.Serializable]
    public class StandardResponse : LootLockerResponse
    {
        public bool success { get; set; }
    }

    [System.Serializable]
    public class DlcResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string[] dlcs { get; set; }
    }

    [System.Serializable]
    public class DeactivatedAssetsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public DeactivatedObjects[] objects { get; set; }
    }
    [System.Serializable]
    public class DeactivatedObjects
    {
        public int deactivated_asset_id { get; set; }
        public int replacement_asset_id { get; set; }
        public string reason { get; set; }
    }


    [System.Serializable]
    public class BalanceResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int? balance { get; set; }
    }

    [System.Serializable]
    public class XpSubmitResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Xp xp { get; set; }
        public Level[] levels { get; set; }
        public bool check_grant_notifications { get; set; }
    }

    [System.Serializable]
    public class XpResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int? xp { get; set; }
        public int? level { get; set; }
    }
    [System.Serializable]
    public class Xp
    {
        public int? previous { get; set; }
        public int? current { get; set; }
    }
    [System.Serializable]
    public class Level
    {
        public int? level { get; set; }
        public int? xp_threshold { get; set; }
    }

    [System.Serializable]

    public class InventoryResponse : LootLockerResponse
    {

        public bool success;

        public Inventory[] inventory;

    }
    public class Inventory
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public string rental_option_id { get; set; }
        public string acquisition_source { get; set; }
        public Asset asset { get; set; }
        public Rental rental { get; set; }


        public float balance;
    }
    [System.Serializable]
    public class AssetClass
    {
        public string Asset { get; set; }
    }
    [System.Serializable]
    public class Rental
    {
        public bool is_rental { get; set; }
        public string time_left { get; set; }
        public string duration { get; set; }
        public string is_active { get; set; }
    }
    [System.Serializable]
    public class XpSubmitRequest
    {
        public int? points;

        public XpSubmitRequest(int points)
        {
            this.points = points;
        }
    }
    [System.Serializable]
    public class XpRequest : LootLockerGetRequest
    {
        public XpRequest()
        {
            getRequests.Clear();
            getRequests.Add(LootLockerConfig.current.deviceID);
            getRequests.Add(LootLockerConfig.current.platform.ToString());
        }
    }

    public class PlayerAssetNotificationsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public RewardObject[] objects { get; set; }
    }

    public class RewardObject
    {
        public int instance_id { get; set; }
        public int variation_id { get; set; }
        public string acquisition_source { get; set; }
        public Asset asset { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetPlayerInfo(Action<GetPlayerInfoResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getPlayerInfo;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                GetPlayerInfoResponse response = new GetPlayerInfoResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetPlayerInfoResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void GetInventory(Action<InventoryResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getInventory;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
               {
                   InventoryResponse response = new InventoryResponse();
                   if (string.IsNullOrEmpty(serverResponse.Error))
                   {
                       response = JsonConvert.DeserializeObject<InventoryResponse>(serverResponse.text);
                       response.text = serverResponse.text;
                       onComplete?.Invoke(response);
                   }
                   else
                   {
                       response.text = serverResponse.text;
                       response.message = serverResponse.message;
                       response.Error = serverResponse.Error;
                       onComplete?.Invoke(response);
                   }
               }, true);
        }

        public static void GetBalance(Action<BalanceResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getCurrencyBalance;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, onComplete: (serverResponse) =>
            {
                BalanceResponse response = new BalanceResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<BalanceResponse>(serverResponse.text);
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

        public static void SubmitXp(XpSubmitRequest data, Action<XpSubmitResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.submitXp;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                XpSubmitResponse response = new XpSubmitResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<XpSubmitResponse>(serverResponse.text);
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

        public static void GetXpAndLevel(LootLockerGetRequest data, Action<XpResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getXpAndLevel;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                XpResponse response = new XpResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<XpResponse>(serverResponse.text);
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

        public static void GetPlayerAssetNotification(Action<PlayerAssetNotificationsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.playerAssetNotifications;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                PlayerAssetNotificationsResponse response = new PlayerAssetNotificationsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<PlayerAssetNotificationsResponse>(serverResponse.text);
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

        public static void GetDeactivatedAssetNotification(Action<DeactivatedAssetsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.playerAssetDeactivationNotification;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                DeactivatedAssetsResponse response = new DeactivatedAssetsResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<DeactivatedAssetsResponse>(serverResponse.text);
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

        public static void InitiateDLCMigration(Action<DlcResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.initiateDlcMigration;

            string getVariable = endPoint.endPoint;
            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                DlcResponse response = new DlcResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<DlcResponse>(serverResponse.text);
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

        public static void GetDLCMigrated(Action<DlcResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.getDlcMigration;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                DlcResponse response = new DlcResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<DlcResponse>(serverResponse.text);
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

        public static void SetProfilePrivate(Action<StandardResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.setProfilePrivate;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                StandardResponse response = new StandardResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<StandardResponse>(serverResponse.text);
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

        public static void SetProfilePublic(Action<StandardResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.setProfilePublic;

            string getVariable = endPoint.endPoint;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                StandardResponse response = new StandardResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<StandardResponse>(serverResponse.text);
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