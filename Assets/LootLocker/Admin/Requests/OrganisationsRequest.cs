using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerAdminRequests;
using Newtonsoft.Json;
using System;

namespace LootLockerAdminRequests
{

    #region GetUsersToAnOrganisation

    public class GetUsersToAnOrganisationResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public OrganisationUser[] users { get; set; }
    }

    public class OrganisationUser
    {
        public int id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public bool uses_2fa { get; set; }
        public string last_login { get; set; }
    }

    #endregion

}

namespace LootLockerAdmin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void GetUsersToAnOrganisation(LootLockerGetRequest data, Action<GetUsersToAnOrganisationResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getUsersToAnOrganisation;

            string getVariable = string.Format(endPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) =>
            {
                GetUsersToAnOrganisationResponse response = new GetUsersToAnOrganisationResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetUsersToAnOrganisationResponse>(serverResponse.text);
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