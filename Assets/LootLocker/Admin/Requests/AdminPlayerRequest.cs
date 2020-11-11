using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerAdminRequests;
using Newtonsoft.Json;
using System;

namespace LootLockerAdminRequests
{

    #region SearchingForPlayers


    public class SearchingForPlayersResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int offset { get; set; }
        public Player[] players { get; set; }
    }

    public class Player
    {
        public int id { get; set; }
        public string steamId { get; set; }
        public object psn_account_id { get; set; }
        public int banned { get; set; }
        public int _public { get; set; }
    }

    #endregion

}

namespace LootLockerAdmin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void SearchingForPlayers(LootLockerGetRequest data, Action<SearchingForPlayersResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.searchingForPlayers;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                SearchingForPlayersResponse response = new SearchingForPlayersResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<SearchingForPlayersResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true, callerRole: enums.CallerRole.Admin);
        }

    }

}