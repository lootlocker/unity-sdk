using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Admin;
using LootLocker.Admin.Requests;
using Newtonsoft.Json;
using System;
using LootLocker.Requests;

namespace LootLocker.Admin.Requests
{

    #region GetAllGamesToTheCurrentUser

    public class LootLockerGetAllGamesToTheCurrentUserResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerGame[] games { get; set; }
    }

    #endregion

    #region CreatingAGame


    #endregion

    #region GetDetailedInformationAboutAGame

    //Exact same response as previous request " Creating A Game "

    #endregion

    #region UpdatingInformationAboutAGame

    //Exact same response as previous 2 requests " Creating A Game " & " Get Detailed Information About A Game "

    public class LootLockerUpdatingInformationAboutAGameRequest
    {

        public string name, game_key, steam_api_key;
        public int steam_app_id;
        public bool sandbox_mode;

    }

    #endregion

    #region DeletingGames

    public class LootLockerDeletingGamesResponse : LootLockerResponse
    {

        public bool success;

    }

    #endregion

}

namespace LootLocker.Admin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void GetAllGamesToTheCurrentUser(Action<LootLockerGetAllGamesToTheCurrentUserResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getAllGamesToTheCurrentUser;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, "", (serverResponse) =>
            {
                LootLockerGetAllGamesToTheCurrentUserResponse response = new LootLockerGetAllGamesToTheCurrentUserResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerGetAllGamesToTheCurrentUserResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);

        }

        public static void CreatingAGame(LootLockerCreatingAGameRequest data, Action<LootLockerCreatingAGameResponse> onComplete)
        {

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.creatingAGame;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerCreatingAGameResponse response = new LootLockerCreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerCreatingAGameResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }

        //Both this and the previous call share the same response
        public static void GetDetailedInformationAboutAGame(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerCreatingAGameResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getDetailedInformationAboutAGame;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                LootLockerCreatingAGameResponse response = new LootLockerCreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerCreatingAGameResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);

        }

        //Both this and the previous 2 calls share the same response
        public static void UpdatingInformationAboutAGame(LootLockerGetRequest lootLockerGetRequest, Dictionary<string, object> data, Action<LootLockerCreatingAGameResponse> onComplete)
        {

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.updatingInformationAboutAGame;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerCreatingAGameResponse response = new LootLockerCreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerCreatingAGameResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);

        }

        public static void DeletingGames(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerDeletingGamesResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.deletingGames;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                LootLockerDeletingGamesResponse response = new LootLockerDeletingGamesResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerDeletingGamesResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);

        }

    }

}