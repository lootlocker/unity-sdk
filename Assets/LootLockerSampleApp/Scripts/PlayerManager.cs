using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using LootLocker.Requests;
using Newtonsoft.Json;
using System;
using LootLocker;

namespace LootLockerDemoApp
{
    public class CreatePlayerRequest : ILootLockerStageData
    {
        public string playerName;
    }

    public class PlayerManager : MonoBehaviour, ILootLockerStageOwner
    {

        [Header("Screens")]
        public GameObject playersScreen;
        public GameObject createPlayerScreen;

        [Header("----------------------------------------------")]
        public GameObject playerElementPrefab;
        public Transform playersContent;
        public InputField newPlayerName;
        string playerKeyName = "localplayers";
        public Dictionary<LocalPlayer, GameObject> playerElements = new Dictionary<LocalPlayer, GameObject>();
        [Header("Easy Prefab Setup")]
        public bool isEasyPrefab;
        public static LocalPlayer localPlayer;
        public PlayerDataObject playerDataObject;

        private void Awake()
        {
            StartEasyPrefab();
        }

        public void StartEasyPrefab()
        {
            if (isEasyPrefab)
            {
                SetUpEasyPrefab();
                ListPlayers();
            }
        }

        public void SetUpEasyPrefab()
        {
            if (TexturesSaver.Instance == null)
            {
                GameObject saver = Resources.Load("EasyPrefabsResources/TextureSaver") as GameObject;
                Instantiate(saver);
            }

            if (LoadingManager.Instance == null)
            {
                GameObject loading = Resources.Load("EasyPrefabsResources/LoadingPrefab") as GameObject;
                Instantiate(loading);
            }

            if (PopupSystem.Instance == null)
            {
                GameObject popup = Resources.Load("EasyPrefabsResources/PopupPrefab") as GameObject;
                Instantiate(popup);
            }
        }

        public void UpdateScreenData(ILootLockerStageData stageData)
        {
            ListPlayers();
        }

        public void ListPlayers()
        {

            if (!PlayerPrefs.HasKey(playerDataObject?.playerStorageKeyNameToUse))
                PlayerPrefs.SetString(playerDataObject?.playerStorageKeyNameToUse, JsonConvert.SerializeObject(new List<LocalPlayer>()));

            List<LocalPlayer> localPlayers = JsonConvert.DeserializeObject<List<LocalPlayer>>(PlayerPrefs.GetString(playerDataObject?.playerStorageKeyNameToUse));

            FillPlayers(localPlayers);

        }

        void FillPlayers(List<LocalPlayer> players)
        {

            for (int i = 0; i < playersContent.childCount; i++)
                Destroy(playersContent.GetChild(i).gameObject);

            playerElements = new Dictionary<LocalPlayer, GameObject>();

            foreach (LocalPlayer user in players)
            {

                GameObject playerElementObject = Instantiate(playerElementPrefab, playersContent);
                playerElementObject.GetComponentInChildren<Text>().text = user.playerName;
                playerElementObject.GetComponent<Button>().onClick.AddListener(() => SelectPlayer(user));
                playerElements.Add(user, playerElementObject);
            }
        }

        public void ClickCreateNewPlayer()
        {
            playersScreen.SetActive(false);
            createPlayerScreen.SetActive(true);
        }

        public void ClickNextOnName()
        {
            if (string.IsNullOrEmpty(newPlayerName.text))
                return; //TODO: Show a message saying player name can't be empty
            createPlayerScreen.SetActive(false);
            playersScreen.SetActive(true);
            StartNewSession();
        }

        public void StartNewSession()
        {
            Guid guid = Guid.NewGuid();
            LoadingManager.ShowLoadingScreen();
            localPlayer = new LocalPlayer { playerName = newPlayerName.text, uniqueID = guid.ToString(), characterClass = null };
            StartSession(localPlayer, (response) =>
            {
                Debug.Log("Created Session for new player with id: " + guid.ToString());
                playerDataObject.SavePlayer(newPlayerName.text, guid.ToString());
                //we want to reset the current character
                playerDataObject?.SaveCharacter(new LootLockerCharacter {name = "None", type = "None"});
                StagesManager.instance.GoToStage(StagesManager.StageID.CreateCharacter, localPlayer);
                LoadingManager.HideLoadingScreen();
            },
            () =>
            {
                LoadingManager.HideLoadingScreen();
            });
        }

        public void StartSession(LocalPlayer player, Action<LootLockerSessionResponse> onStartSessionCompleted, Action onSessionStartingFailed = null)
        {
            LoadingManager.ShowLoadingScreen();
            //Starting a new session using the new id that has been created
            LootLockerSDKManager.StartSession(player.uniqueID, (response) =>
            {
                if (response.success)
                {
                    playerDataObject.SaveSession(response);
                    onStartSessionCompleted?.Invoke(response);
                    LoadingManager.HideLoadingScreen();
                }
                else
                {
                    onSessionStartingFailed?.Invoke();
                    Debug.LogError("Session failure: " + response.text);
                }

            });
        }

        public void SelectPlayer(LocalPlayer selectedPlayer)
        {
            if (isEasyPrefab)
            {
                Debug.LogError("You clicked on player " + selectedPlayer.playerName + " thats all we know :) ");
                return;
            }
            playersScreen.SetActive(false);
            createPlayerScreen.SetActive(false);
            LoadingManager.ShowLoadingScreen();
            StartSession(selectedPlayer, (response) =>
            {
                playersScreen.SetActive(true);
                Debug.Log("Logged in successfully.");
                LoadingManager.HideLoadingScreen();
                playerDataObject.SaveCharacter(selectedPlayer.playerName, selectedPlayer.characterClass);
                LootLockerConfig.current.deviceID = selectedPlayer.uniqueID;
                StagesManager.instance.GoToStage(StagesManager.StageID.Home, response);
            },
            () =>
             {
                 playersScreen.SetActive(true);
                 Debug.LogError("Log in failure.");
                 LoadingManager.HideLoadingScreen();
             });
        }
    }

}