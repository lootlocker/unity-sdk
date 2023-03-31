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
        public static void GetReportTypes(Action<LootLockerReportsGetTypesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.reportsGetTypes;
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetRemovedUGCForPlayer(GetRemovedUGCForPlayerInput input, Action<LootLockerReportsGetRemovedAssetsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.reportsGetRemovedUGCForPlayer;
            string tempEndpoint = endPoint.endPoint;

            if (!string.IsNullOrEmpty(input.After))
            {
                tempEndpoint = tempEndpoint + "?after={0}";
                tempEndpoint = string.Format(tempEndpoint, input.After);
            }

            if (input.Count > 0)
            {
                if (tempEndpoint.IndexOf("?") > -1)
                {
                    tempEndpoint = tempEndpoint + "&";
                }
                else
                {
                    tempEndpoint = tempEndpoint + "?";
                }

                tempEndpoint = tempEndpoint + "count={0}";
                tempEndpoint = string.Format(tempEndpoint, input.Count);
            }

            if (!string.IsNullOrEmpty(input.Since))
            {
                if (tempEndpoint.IndexOf("?") > -1)
                {
                    tempEndpoint = tempEndpoint + "&";
                }
                else
                {
                    tempEndpoint = tempEndpoint + "?";
                }

                tempEndpoint = tempEndpoint + "since={0}";
                tempEndpoint = string.Format(tempEndpoint, input.Since);
            }

            LootLockerServerRequest.CallAPI(tempEndpoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CreatePlayerReport(ReportsCreatePlayerRequest data, Action<LootLockerReportsCreatePlayerResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.reportsCreatePlayer;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerReportsCreatePlayerResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(requestEndPoint.endPoint, requestEndPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CreateAssetReport(ReportsCreateAssetRequest data, Action<LootLockerReportsCreateAssetResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.reportsCreateAsset;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerReportsCreateAssetResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            LootLockerServerRequest.CallAPI(requestEndPoint.endPoint, requestEndPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}