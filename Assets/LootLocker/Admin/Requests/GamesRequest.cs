using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerAdmin;
using LootLockerAdminRequests;
using Newtonsoft.Json;
using System;

namespace LootLockerAdminRequests
{

    #region GetAllGamesToTheCurrentUser

    public class GetAllGamesToTheCurrentUserResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Game[] games { get; set; }
    }

    #endregion

    #region CreatingAGame


    #endregion

    #region GetDetailedInformationAboutAGame

    //Exact same response as previous request " Creating A Game "

    #endregion

    #region UpdatingInformationAboutAGame

    //Exact same response as previous 2 requests " Creating A Game " & " Get Detailed Information About A Game "

    public class UpdatingInformationAboutAGameRequest
    {

        public string name, game_key, steam_api_key;
        public int steam_app_id;
        public bool sandbox_mode;

    }

    #endregion

    #region DeletingGames

    public class DeletingGamesResponse : LootLockerResponse
    {

        public bool success;

    }

    #endregion

}

namespace LootLockerAdmin
{

    public partial class LootLockerAPIManagerAdmin
    {

        public static void GetAllGamesToTheCurrentUser(Action<GetAllGamesToTheCurrentUserResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getAllGamesToTheCurrentUser;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, "", (serverResponse) =>
            {
                GetAllGamesToTheCurrentUserResponse response = new GetAllGamesToTheCurrentUserResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetAllGamesToTheCurrentUserResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, isAdminCall: true);

        }

        public static void CreatingAGame(CreatingAGameRequest data, Action<CreatingAGameResponse> onComplete)
        {

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.creatingAGame;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                CreatingAGameResponse response = new CreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CreatingAGameResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, isAdminCall: true);
        }

        //Both this and the previous call share the same response
        public static void GetDetailedInformationAboutAGame(LootLockerGetRequest lootLockerGetRequest, Action<CreatingAGameResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getDetailedInformationAboutAGame;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                CreatingAGameResponse response = new CreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CreatingAGameResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, isAdminCall: true);

        }

        //Both this and the previous 2 calls share the same response
        public static void UpdatingInformationAboutAGame(LootLockerGetRequest lootLockerGetRequest, Dictionary<string, object> data, Action<CreatingAGameResponse> onComplete)
        {

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.updatingInformationAboutAGame;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) =>
            {
                CreatingAGameResponse response = new CreatingAGameResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CreatingAGameResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, isAdminCall: true);

        }

        public static void DeletingGames(LootLockerGetRequest lootLockerGetRequest, Action<DeletingGamesResponse> onComplete)
        {

            EndPointClass endPoint = LootLockerEndPointsAdmin.current.deletingGames;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                DeletingGamesResponse response = new DeletingGamesResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<DeletingGamesResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, isAdminCall: true);

        }

    }

}