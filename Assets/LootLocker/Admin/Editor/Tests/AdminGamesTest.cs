using LootLockerAdminRequests;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LootLockerAdmin
{
    public class AdminGamesTest : MonoBehaviour
    {

        [Header("Creating A Game")]
        public string gameName;
        public string gameSteamAppID;
        public bool sandboxMode;
        public int organisationID;

        [Header("Get Detailed Info About A Game")]
        [Header("---------------------------")]

        public int gameIDToGetInfo;

        [Header("Updating Info About A Game")]
        [Header("---------------------------")]
        public int gameIDToUpdateInfo;

        [Header("Name")]
        public string newName;
        public bool sendNewName;

        [Header("Game Key")]
        public string newGameKey;
        public bool sendNewGameKey;

        [Header("Steam App ID")]
        public int newSteamAppID;
        public bool sendNewSteamAppID;

        [Header("Steam API Key")]
        public string newSteamAPIKey;
        public bool sendNewSteamAPIKey;

        [Header("Sandbox Mode")]
        public bool newSandboxMode;
        public bool sendNewSandboxMode;

        [Header("Deleting Games")]
        [Header("---------------------------")]
        public int gameIDToDelete;

        [ContextMenu("Get All Games To The Current User")]
        public void GetAllGamesToTheCurrentUser()
        {
            LootLockerSDKAdminManager.GetAllGamesToTheCurrentUser((response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful got all games: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to get all games: " + response.Error);
                }
            });
        }

        [ContextMenu("Creating A Game")]
        public void CreatingAGame()
        {
            LootLockerSDKAdminManager.CreatingAGame(gameName, gameSteamAppID, sandboxMode, organisationID, false, (response) =>
             {
                 if (response.success)
                 {
                     Debug.LogError("Successful created a game: " + response.text);
                 }
                 else
                 {
                     Debug.LogError("failed to get all games: " + response.Error);
                 }
             });
        }

        [ContextMenu("Get Detailed Information About A Game")]
        public void GetDetailedInformationAboutAGame()
        {

            //LootLockerGetRequest lootLockerGetRequest = new LootLockerGetRequest();

            //lootLockerGetRequest.getRequests.Add(gameIDToGetInfo.ToString());

            LootLockerSDKAdminManager.GetDetailedInformationAboutAGame(gameIDToGetInfo.ToString(), (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful got info about game " + gameIDToGetInfo.ToString() + ": " + response.text);
                }
                else
                {
                    Debug.LogError("failed to get info about game " + gameIDToGetInfo.ToString() + ": " + response.Error);
                }
            });
        }

        [ContextMenu("Updating Information About A Game")]
        public void UpdatingInformationAboutAGame()
        {
            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();

            List<bool> sendBools = new List<bool> { sendNewName, sendNewGameKey, sendNewSteamAppID, sendNewSteamAPIKey, sendNewSandboxMode };
            List<string> itemNames = new List<string> { "name", "game_key", "steam_app_id", "steam_api_key", "sandbox_mode" };
            List<object> objectsToAdd = new List<object> { newName, newGameKey, newSteamAppID, newSteamAPIKey, newSandboxMode };

            for (int i = 0; i < sendBools.Count; i++)
                if (sendBools[i])
                    keyValuePairs.Add(itemNames[i], objectsToAdd[i]);

            LootLockerSDKAdminManager.UpdatingInformationAboutAGame(gameIDToUpdateInfo, keyValuePairs, (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful updated info about game " + gameIDToUpdateInfo.ToString() + ": " + response.text);
                }
                else
                {
                    Debug.LogError("failed to update info about game " + gameIDToUpdateInfo.ToString() + ": " + response.Error);
                }
            });
        }

        [ContextMenu("Deleting Games")]
        public void DeletingGames()
        {
            LootLockerSDKAdminManager.DeletingGames(gameIDToDelete, (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful deleted a game: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to delete a game: " + response.Error);
                }
            });
        }

    }

}