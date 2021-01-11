using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Admin.Requests;
using Newtonsoft.Json;
using System;
using LootLocker.Requests;

namespace LootLocker.Admin.Requests
{

    #region SearchingForPlayers


    public class LootLockerSearchingForPlayersResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int offset { get; set; }
        public LootLockerPlayer[] players { get; set; }
    }

    public class LootLockerPlayer
    {
        public int id { get; set; }
        public string steamId { get; set; }
        public object psn_account_id { get; set; }
        public int banned { get; set; }
        public int _public { get; set; }
    }

    #endregion

}

namespace LootLocker.Admin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void SearchingForPlayers(LootLockerGetRequest data, Action<LootLockerSearchingForPlayersResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.searchingForPlayers;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerSearchingForPlayersResponse response = new LootLockerSearchingForPlayersResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerSearchingForPlayersResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }

    }

}