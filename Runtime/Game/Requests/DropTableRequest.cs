using LootLocker.Requests;
using System;
using System.Net;

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
        public static void ComputeAndLockDropTable(string forPlayerWithUlid, int tableInstanceId, Action<LootLockerComputeAndLockDropTableResponse> onComplete, bool AddAssetDetails = false, string tag = "")
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.ComputeAndLockDropTable;

            string endPoint = requestEndPoint.WithPathParameters(tableInstanceId, AddAssetDetails.ToString().ToLower());

            if (!string.IsNullOrEmpty(tag))
            {
                string tempEndpoint = $"&tag={WebUtility.UrlEncode(tag)}";
                endPoint += tempEndpoint;
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: true);
        }

        public static void PickDropsFromDropTable(string forPlayerWithUlid, PickDropsFromDropTableRequest data, int tableInstanceId, Action<LootLockerPickDropsFromDropTableResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.PickDropsFromDropTable;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerPickDropsFromDropTableResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            string endPoint = requestEndPoint.WithPathParameter(tableInstanceId);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, requestEndPoint.httpMethod, json, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: true);
        }
    }
}