using LootLocker.Requests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker;

namespace LootLockerDemoApp
{
    public class StorageScreen : MonoBehaviour, ILootLockerStageOwner
    {
        public GameObject keyValueElement;
        public GameObject content;
        public Button addKey;
        public InputPopup inputPopup;
        public Button refresh;
        [Header("Easy Prefab Setup")]
        public GameObject backButton;
        public bool isEasyPrefab;

        public void Awake()
        {
            addKey.onClick.AddListener(OpenKeysWindow);
            refresh.onClick.AddListener(Refresh);
            StartEasyPrefab();
        }

        public void StartEasyPrefab()
        {
            if (isEasyPrefab)
            {
                backButton?.SetActive(false);
                SetUpEasyPrefab();
                Refresh();
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

        public void Refresh()
        {
            LoadingManager.ShowLoadingScreen();
            foreach (Transform tr in content.transform)
            {
                Destroy(tr.gameObject);
            }
            LootLockerSDKManager.GetEntirePersistentStorage((response) =>
            {
                LoadingManager.HideLoadingScreen();
                UpdateScreen(response.payload);
            });
        }

        public void UpdateScreen(LootLockerPayload[] payload)
        {
            foreach (Transform tr in content.transform)
            {
                Destroy(tr.gameObject);
            }
            for (int i = 0; i < payload.Length; i++)
            {
                GameObject go = Instantiate(keyValueElement, content.transform);
                KeyValueElements keyValueElements = go?.GetComponent<KeyValueElements>();
                if (keyValueElements != null)
                {
                    keyValueElements.Init(payload[i]);
                }
            }
        }

        public void OpenKeysWindow()
        {
            inputPopup.Init(new string[] { "Save" });
        }

        public void OpenKeyWindow(string key, string value, string[] btns)
        {
            inputPopup.Init(key, value, btns);
        }

        public void UpdateScreenData(ILootLockerStageData stageData)
        {
            LootLockerGetPersistentStoragResponse response = stageData as LootLockerGetPersistentStoragResponse;
            if (response != null)
            {
                UpdateScreen(response.payload);
                LoadingManager.HideLoadingScreen();
            }
            else
                Refresh();
        }

    }
}
