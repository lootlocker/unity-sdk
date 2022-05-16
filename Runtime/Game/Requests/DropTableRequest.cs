using LootLocker.Requests;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker.Requests
{
    public class LootLockerComputeAndLockDropTableResponse : LootLockerResponse
    {
        public LootLockerComputeAndLockItem[] items { get; set; }
    }

    public class LootLockerComputeAndLockItem
    {
        public int asset_id { get; set; }
        public int? asset_variation_id { get; set; }
        public int? asset_rental_option_id { get; set; }
        public int id { get; set; }
    }

    public class LootLockerPickDropsFromDropTableResponse : LootLockerResponse
    {
        public LootLockerPickDropsFromDropTableItem[] items { get; set; }
    }

    public class LootLockerPickDropsFromDropTableItem
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public int? rental_option_id { get; set; }
        public int? quantity { get; set; }
        public LootLockerCommonAsset asset { get; set; }
    }

    public class PickDropsFromDropTableRequest
    {
        public int[] picks { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void ComputeAndLockDropTable(int tableInstanceId, Action<LootLockerComputeAndLockDropTableResponse> onComplete, bool AddAssetDetails = false, string tag = "")
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.ComputeAndLockDropTable;

            string endPoint = string.Format(requestEndPoint.endPoint, tableInstanceId, AddAssetDetails.ToString().ToLower());

            if (!string.IsNullOrEmpty(tag))
            {
                string tempEndpoint = $"&tag={tag}";
                endPoint += tempEndpoint;
            }

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.User);
        }

        public static void PickDropsFromDropTable(PickDropsFromDropTableRequest data, int tableInstanceId, Action<LootLockerPickDropsFromDropTableResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.PickDropsFromDropTable;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string endPoint = string.Format(requestEndPoint.endPoint, tableInstanceId);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.User);
        }
    }
}