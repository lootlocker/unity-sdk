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
    public class MessagesScreen : MonoBehaviour, IStageOwner
    {
        [Header("Messages")]
        public Transform messagesParent;
        public GameObject messagesObject, readMessageObject, messagePrefab;
        public GameObject backButton;
        [Header("Easy Prefab Setup")]
        public GameObject readMessages;
        public bool isEasyPrefab;

        private void Awake()
        {
            StartEasyPrefab();
        }

        public void StartEasyPrefab()
        {
            if (isEasyPrefab)
            {
                backButton?.SetActive(false);
                SetUpEasyPrefab();
                ListMessages();
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

        void ListMessages()
        {
            LoadingManager.ShowLoadingScreen();
            LootLockerSDKManager.GetMessages((response) =>
            {
                LoadingManager.HideLoadingScreen();
                if (response.success)
                {
                    Debug.Log("Successful got all messages: " + response.text);
                    for (int i = 0; i < messagesParent.childCount; i++)
                        Destroy(messagesParent.GetChild(i).gameObject);
                    foreach (GMMessage message in response.messages)
                    {
                        GameObject messageObject = Instantiate(messagePrefab, messagesParent);
                        messageObject.GetComponent<MessageElement>().InitMessage(message);
                        messageObject.GetComponent<Button>().onClick.AddListener(() => SelectMessage(message));
                    }
                }
                else
                {
                    Debug.LogError("failed to get all messages: " + response.Error);
                }
            });
        }
        public void ViewMessages()
        {
            ListMessages();
        }
        public void SelectMessage(GMMessage selectedMessage)
        {
            if (!readMessages)
                StagesManager.instance.GoToStage(StagesManager.StageID.ReadMessages, selectedMessage);
            else
                readMessages?.GetComponent<ReadMessageScreen>()?.StartEasyPrefab(selectedMessage);
        }

        public void UpdateScreenData(IStageData stageData)
        {
            ViewMessages();
        }
    }
}