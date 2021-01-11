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

    #region GetUsersToAnOrganisation

    public class LootLockerGetUsersToAnOrganisationResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerOrganisationUser[] users { get; set; }
    }

    public class LootLockerOrganisationUser
    {
        public int id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public bool uses_2fa { get; set; }
        public string last_login { get; set; }
    }

    #endregion

}

namespace LootLocker.Admin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void GetUsersToAnOrganisation(LootLockerGetRequest data, Action<LootLockerGetUsersToAnOrganisationResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getUsersToAnOrganisation;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                LootLockerGetUsersToAnOrganisationResponse response = new LootLockerGetUsersToAnOrganisationResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerGetUsersToAnOrganisationResponse>(serverResponse.text);
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