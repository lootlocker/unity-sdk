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
        string playerStorageKeyNameToUse;
        [Header("Easy Prefab Setup")]
        public bool isEasyPrefab;
        string easyPrefabPlayerKeyName = "easyPrefabLocalplayers";

        private void Awake()
        {
            playerStorageKeyNameToUse = playerKeyName;
            StartEasyPrefab();
        }

        public void StartEasyPrefab()
        {
            if (isEasyPrefab)
            {
                playerStorageKeyNameToUse = easyPrefabPlayerKeyName;
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

            if (!PlayerPrefs.HasKey(playerStorageKeyNameToUse))
                PlayerPrefs.SetString(playerStorageKeyNameToUse, JsonConvert.SerializeObject(new List<LocalPlayer>()));

            List<LocalPlayer> localPlayers = JsonConvert.DeserializeObject<List<LocalPlayer>>(PlayerPrefs.GetString(playerStorageKeyNameToUse));

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
            if (!isEasyPrefab)
            {
                CreatePlayerRequest createPlayerRequest = new CreatePlayerRequest { playerName = newPlayerName.text };
                StagesManager.instance.GoToStage(StagesManager.StageID.SwapClass, createPlayerRequest);
            }
            else
            {
                StartSession();
            }
        }

        public void StartSession()
        {
            Guid guid = Guid.NewGuid();

            LoadingManager.ShowLoadingScreen();
            //Starting a new session using the new id that has been created
            LootLockerSDKManager.StartSession(guid.ToString(), (response) =>
            {
                if (response.success)
                {
                    Debug.Log("Created Session for new player with id: " + guid.ToString());
                    LocalPlayer localPlayer = new LocalPlayer { playerName = newPlayerName.text, uniqueID = guid.ToString(), characterClass = null };
                    List<LocalPlayer> localPlayers = JsonConvert.DeserializeObject<List<LocalPlayer>>(PlayerPrefs.GetString(playerStorageKeyNameToUse));
                    localPlayers.Add(localPlayer);
                    PlayerPrefs.SetString(playerStorageKeyNameToUse, JsonConvert.SerializeObject(localPlayers));
                    ListPlayers();
                    LoadingManager.HideLoadingScreen();
                }
                else
                {
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

            LootLockerConfig.current.deviceID = selectedPlayer.uniqueID;

            LootLockerSDKManager.StartSession(selectedPlayer.uniqueID, (response) =>
            {

                if (response.success)
                {
                    playersScreen.SetActive(true);
                    Debug.Log("Logged in successfully.");
                    LoadingManager.HideLoadingScreen();
                    LootLockerConfig.current.playerName = selectedPlayer.playerName;
                    LootLockerConfig.current.playerClass = selectedPlayer.characterClass.type.ToString();
                    StagesManager.instance.GoToStage(StagesManager.StageID.Home, response);

                }
                else
                {
                    playersScreen.SetActive(true);
                    Debug.LogError("Log in failure.");
                    LoadingManager.HideLoadingScreen();

                }

            });

        }

    }

    public class LocalPlayer
    {
        public string playerName, uniqueID;
        public LootLockerCharacter characterClass;

    }
}