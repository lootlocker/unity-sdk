using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using LootLocker.LootLockerEnums;


namespace LootLocker.Requests
{
    public class LootLockerReportsGetTypesResponse : LootLockerResponse
    {
        // we are doing thisfor legacy reasons, since it is no longer being set on the backend
        public int id { get; set; }
        public string text { get; set; }
    }

    public class LootLockerReportsCreatePlayerResponse : LootLockerResponse
    {
        // we are doing thisfor legacy reasons, since it is no longer being set on the backend
        public int id { get; set; }
        public string text { get; set; }
    }

    public class ReportsCreatePlayerRequest
    {
        public int[] report_types { get; set; }
        public string text { get; set; }
        public int player_id { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void ReportsGetTypes(Action<LootLockerReportsGetTypesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.reportsGetTypes;
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, ((serverResponse) =>
            {
                LootLockerReportsGetTypesResponse response = new LootLockerReportsGetTypesResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerReportsGetTypesResponse>(serverResponse.text);
                }

                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }), true, LootLockerCallerRole.User);
        }

        public static void ReportsGetRemovedUGCForPlayer(Action<LootLockerReportsGetTypesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.reportsGetRemovedUGCForPlayer;
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, ((serverResponse) =>
            {
                LootLockerReportsGetTypesResponse response = new LootLockerReportsGetTypesResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerReportsGetTypesResponse>(serverResponse.text);
                }

                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }), true, LootLockerCallerRole.User);
        }

        public static void ReportsCreatePlayer(LootLockerGetByListMembersRequest data, string id, Action<LootLockerReportsCreatePlayerResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.reportsCreatePlayer;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string endPoint = string.Format(requestEndPoint.endPoint, id);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, ((serverResponse) =>
            {
                LootLockerReportsCreatePlayerResponse response = new LootLockerReportsCreatePlayerResponse();
                if (string.IsNullOrEmpty(serverResponse.Error)) {
                    response = JsonConvert.DeserializeObject<LootLockerReportsCreatePlayerResponse>(serverResponse.text);
                }

                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;

                onComplete?.Invoke(response);
            }), true, LootLockerCallerRole.User);
        }
    }
}
