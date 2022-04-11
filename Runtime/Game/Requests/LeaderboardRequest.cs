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
    public class LootLockerGetMemberRankResponse : LootLockerResponse
    {
        // we are doing thisfor legacy reasons, since it is no longer being set on the backend
        public string member_id { get; set; }
        public int rank { get; set; }
        public int score { get; set; }
        public LootLockerPlayer player { get; set; }
        public string metadata { get; set; }
    }

    public class LootLockerPlayer
    {
        public int id { get; set; }
        public string public_uid { get; set; }
        public string name { get; set; }
    }


    public class LootLockerGetByListOfMembersResponse : LootLockerResponse
    {
        public LootLockerLeaderboardMember[] members { get; set; }
    }

    public class LootLockerLeaderboardMember
    {
        public string member_id { get; set; }
        public int rank { get; set; }
        public int score { get; set; }
        public LootLockerPlayer player { get; set; }
        public string metadata { get; set; }
    }

    public class LootLockerGetScoreListResponse : LootLockerResponse
    {
        public LootLockerPagination pagination { get; set; }
        public LootLockerLeaderboardMember[] items { get; set; }
    }


    public class LootLockerGetAllMemberRanksResponse : LootLockerResponse
    {
        public LootLockerLeaderboard[] leaderboards { get; set; }
        public LootLockerPagination pagination { get; set; }
    }

    public class LootLockerLeaderboard
    {
        public LootLockerLeaderboardMember member { get; set; }
        public int leaderboard_id { get; set; }
        public string leaderboard_key { get; set; }
    }

    public class LootLockerPagination
    {
        public int total { get; set; }
        public int? next_cursor { get; set; }
        public int? previous_cursor { get; set; }
        public bool allowNext { get; set; }
        public bool allowPrev { get; set; }
    }

    public class LootLockerSubmitScoreResponse : LootLockerResponse
    {
        public string member_id { get; set; }
        public int rank { get; set; }
        public int score { get; set; }
        public string metadata { get; set; }
    }


    public class LootLockerSubmitScoreRequest
    {
        public string member_id { get; set; }
        public int score { get; set; }
        public string metadata { get; set; }
    }

    public class LootLockerGetMemberRankRequest
    {
        public string leaderboardId { get; set; }
        public string member_id { get; set; }
    }

    public class LootLockerGetScoreListRequest : LootLockerGetRequests
    {
        public int leaderboardId { get; set; }
        public static int? nextCursor;
        public static int? prevCursor;
        public static void Reset()
        {
            nextCursor = 0;
            prevCursor = 0;
        }
    }

    public class LootLockerGetAllMemberRanksRequest : LootLockerGetRequests
    {
        public int member_id { get; set; }
        public static int? nextCursor;
        public static int? prevCursor;
        public static void Reset()
        {
            nextCursor = 0;
            prevCursor = 0;
        }
    }

    public class LootLockerGetRequests
    {
        public int count { get; set; }
        public string after { get; set; }
    }

    public class LootLockerGetByListMembersRequest
    {
        public string[] members { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetMemberRank(LootLockerGetMemberRankRequest data, Action<LootLockerGetMemberRankResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getMemberRank;
            string tempEndpoint = string.Format(endPoint.endPoint, data.leaderboardId, data.member_id);
            LootLockerServerRequest.CallAPI(tempEndpoint, endPoint.httpMethod, null, ((serverResponse) =>
            {
                LootLockerGetMemberRankResponse response = new LootLockerGetMemberRankResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGetMemberRankResponse>(serverResponse.text);

                //   LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }), true, LootLockerCallerRole.User);
        }

        public static void GetByListOfMembers(LootLockerGetByListMembersRequest data, string id, Action<LootLockerGetByListOfMembersResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.getByListOfMembers;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string endPoint = string.Format(requestEndPoint.endPoint, id);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, ((serverResponse) =>
            {
                LootLockerGetByListOfMembersResponse response = new LootLockerGetByListOfMembersResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGetByListOfMembersResponse>(serverResponse.text);

                //    LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;

                onComplete?.Invoke(response);
            }), true, LootLockerCallerRole.User);
        }

        public static void GetAllMemberRanks(LootLockerGetAllMemberRanksRequest getRequests, Action<LootLockerGetAllMemberRanksResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.getAllMemberRanks;

            string tempEndpoint = requestEndPoint.endPoint;
            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.member_id, getRequests.count);

            if (!string.IsNullOrEmpty(getRequests.after))
            {
                tempEndpoint = requestEndPoint.endPoint + "&after={2}";
                endPoint = string.Format(tempEndpoint, getRequests.member_id, getRequests.count, int.Parse(getRequests.after));
            }

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, null, ((serverResponse) =>
            {
                LootLockerGetAllMemberRanksResponse response = new LootLockerGetAllMemberRanksResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGetAllMemberRanksResponse>(serverResponse.text);

                //   LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }), true, LootLockerCallerRole.User);
        }

        public static void GetScoreList(LootLockerGetScoreListRequest getRequests, Action<LootLockerGetScoreListResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.getScoreList;

            string tempEndpoint = requestEndPoint.endPoint;
            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.leaderboardId, getRequests.count);

            if (!string.IsNullOrEmpty(getRequests.after))
            {
                tempEndpoint = requestEndPoint.endPoint + "&after={2}";
                endPoint = string.Format(tempEndpoint, getRequests.leaderboardId, getRequests.count, int.Parse(getRequests.after));
            }

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, null, ((serverResponse) =>
            {
                LootLockerGetScoreListResponse response = new LootLockerGetScoreListResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGetScoreListResponse>(serverResponse.text);

                //   LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }), true, LootLockerCallerRole.User);
        }

        public static void SubmitScore(LootLockerSubmitScoreRequest data, string id, Action<LootLockerSubmitScoreResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.submitScore;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string endPoint = string.Format(requestEndPoint.endPoint, id);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, onComplete: ((serverResponse) =>
            {
                LootLockerSubmitScoreResponse response = new LootLockerSubmitScoreResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerSubmitScoreResponse>(serverResponse.text);

                // LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                response.success = serverResponse.success;
                response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }), useAuthToken: true, callerRole: LootLockerCallerRole.User);
        }

    }
}
