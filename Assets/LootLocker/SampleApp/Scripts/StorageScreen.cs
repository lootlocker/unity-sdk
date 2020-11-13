using LootLockerRequests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StorageScreen : MonoBehaviour, IStageOwner
{
    public GameObject keyValueElement;
    public GameObject content;
    public Button addKey;
    public InputPopup inputPopup;
    public Button refresh;

    public void Awake()
    {
        addKey.onClick.AddListener(OpenKeysWindow);
        refresh.onClick.AddListener(Refresh);
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

    public void UpdateScreen(Payload[] payload)
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

    public void UpdateScreenData(IStageData stageData)
    {
        GetPersistentStoragResponse response = stageData as GetPersistentStoragResponse;
        if(response!=null)
        {
            UpdateScreen(response.payload);
            LoadingManager.HideLoadingScreen();
        }
        else
        Refresh();
    }

    void Start()
    {

    }

    void Update()
    {

    }
}
