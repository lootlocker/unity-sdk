using LootLockerRequests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MessagesScreen : MonoBehaviour,IStageOwner
{
    [Header("Messages")]
    public Transform messagesParent;
    public GameObject messagesObject, readMessageObject, messagePrefab;

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
        StagesManager.instance.GoToStage(StagesManager.StageID.ReadMessages, selectedMessage);
    }

    public void UpdateScreenData(IStageData stageData)
    {
        ViewMessages();
    }
}
