using System;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    public class ReportType
    {
        public int ID { get; set; }
        public string Text { get; set; }
    }

    public class LootLockerReportsGetTypesResponse : LootLockerResponse
    {
        public ReportType[] Types { get; set; }
    }

    public class LootLockerReportsCreatePlayerResponse : LootLockerResponse
    {
        public int ID { get; set; }
        public int PlayerID { get; set; }
        public string Text { get; set; }
        public int[] ReportTypes { get; set; }
        public string ReportDate { get; set; }
    }

    public class LootLockerReportsCreateAssetResponse : LootLockerResponse
    {
        public int ID { get; set; }
        public int AssetID { get; set; }
        public string Text { get; set; }
        public int[] ReportTypes { get; set; }
        public string ReportDate { get; set; }
    }

    public class RemovedAsset
    {
        public int ID { get; set; }
        public int AssetID { get; set; }
        public string Name { get; set; }
        public int[] ReportTypes { get; set; }
        public string RemovedAt { get; set; }
    }

    public class LootLockerReportsGetRemovedAssetsResponse : LootLockerResponse
    {
        public RemovedAsset[] Assets { get; set; }
    }

    public class GetRemovedUGCForPlayerInput
    {
        /// <summary>
        /// Only get UGC removed after this date.
        /// 
        /// Should follow RFC3339 format
        /// </summary>
        public string Since { get; set; }

        /// <summary>
        /// Used for pagination.
        /// 
        /// Set this to the ID of the last retrieved report to get the next ones after.
        /// </summary>
        public string After { get; set; }

        /// <summary>
        /// Number of report you want to retrieve
        /// </summary>
        public int Count { get; set; }
    }

    public class ReportsCreatePlayerRequest
    {
        public int[] report_types { get; set; }
        public string message { get; set; }
        public int player_id { get; set; }
    }

    public class ReportsCreateAssetRequest
    {
        public int[] report_types { get; set; }
        public string message { get; set; }
        public int asset_id { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetReportTypes(string forPlayerWithUlid, Action<LootLockerReportsGetTypesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.reportsGetTypes;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetRemovedUGCForPlayer(string forPlayerWithUlid, GetRemovedUGCForPlayerInput input, Action<LootLockerReportsGetRemovedAssetsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.reportsGetRemovedUGCForPlayer;

            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();

            if (!string.IsNullOrEmpty(input.After))
            {
                queryParams.Add("after", input.After);
            }

            if (input.Count > 0)
            {
                queryParams.Add("count", input.Count);
            }

            if (!string.IsNullOrEmpty(input.Since))
            {
                queryParams.Add("since", input.Since);
            }

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint += queryParams.Build(), endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CreatePlayerReport(string forPlayerWithUlid, ReportsCreatePlayerRequest data, Action<LootLockerReportsCreatePlayerResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.reportsCreatePlayer;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerReportsCreatePlayerResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, requestEndPoint.endPoint, requestEndPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CreateAssetReport(string forPlayerWithUlid, ReportsCreateAssetRequest data, Action<LootLockerReportsCreateAssetResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.reportsCreateAsset;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerReportsCreateAssetResponse>(forPlayerWithUlid));
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, requestEndPoint.endPoint, requestEndPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}