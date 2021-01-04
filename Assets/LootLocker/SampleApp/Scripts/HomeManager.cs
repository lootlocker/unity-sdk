using LootLockerRequests;
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
    public class HomeManager : MonoBehaviour, IStageOwner
    {
        public ScreenOpener bottomOpener;

        SessionResponse sessionResponse;

        [Header("Easy Prefab Setup")]
        public bool isEasyPrefab;

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
        public void UpdateScreenData(IStageData stageData)
        {
            SessionResponse sessionResponse = stageData != null ? stageData as SessionResponse : this.sessionResponse;
            this.sessionResponse = sessionResponse;
            GetComponentInChildren<PlayerProfile>()?.UpdateScreen(sessionResponse);
            GetComponentInChildren<Progression>()?.UpdateScreen(sessionResponse);
            bottomOpener?.Open();
        }

    }
}