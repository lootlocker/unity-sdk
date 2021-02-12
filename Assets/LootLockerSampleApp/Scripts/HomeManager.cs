using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using LootLocker;

namespace LootLockerDemoApp
{
    public class HomeManager : MonoBehaviour, ILootLockerStageOwner
    {
        public ScreenOpener bottomOpener;
        public PlayerDataObject dataObject;

        [Header("Easy Prefab Setup")]
        public bool isEasyPrefab;
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
                CreateNewSession();
            }
        }

        public void CreateNewSession()
        {

            LoadingManager.ShowLoadingScreen();
            string defaultUser = LootLockerConfig.current != null && string.IsNullOrEmpty(LootLockerConfig.current.deviceID) ? LootLockerConfig.current.deviceID : "NewUserDefault";

            //Starting a new session using the new id that has been created
            LootLockerSDKManager.StartSession(defaultUser, (response) =>
            {
                if (response.success)
                {
                    playerDataObject.SaveSession(response);
                    UpdateScreenData(response);
                    Debug.Log("Created Session for new player with id: " + defaultUser);
                }
                else
                {
                    Debug.LogError("Session failure: " + response.text);
                }
            });
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
            GetComponentInChildren<PlayerProfile>()?.UpdateScreen(dataObject?.session);
            GetComponentInChildren<Progression>()?.UpdateScreen(dataObject?.session);
            bottomOpener?.Open();
        }

    }
}