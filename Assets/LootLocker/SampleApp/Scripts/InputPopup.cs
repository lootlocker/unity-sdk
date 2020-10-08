using LootLockerRequests;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemoAppGetPersistentStoragResponse : GetPersistentStoragResponse, IStageData
{
    public override Payload[] payload { get; set; }
}

public class InputPopup : MonoBehaviour
{
    public InputField key;
    public InputField value;
    public Text[] buttonsTxt;
    public string keySucces = "keySucces";
    List<Action> actions = new List<Action>();
    public Button closeBtn;

    void Awake()
    {
        actions.Add(Save);
        actions.Add(Delete);
        closeBtn.onClick.AddListener(Close);
    }

    public void Init(string key, string value, string[] btnText)
    {
        for (int i = 0; i < buttonsTxt.Length; i++)
        {
            buttonsTxt[i].transform.parent.gameObject.SetActive(false);
        }

        this.key.text = key;
        this.key.enabled = false;
        this.value.text = value;
        for (int i = 0; i < btnText.Length; i++)
        {
            buttonsTxt[i].text = btnText[i];
            buttonsTxt[i].transform.parent.gameObject.SetActive(true);
        }

        GetComponent<ScreenOpener>()?.Open();
    }
    public void Init(string[] btnText)
    {
        for (int i = 0; i < buttonsTxt.Length; i++)
        {
            buttonsTxt[i].transform.parent.gameObject.SetActive(false);
        }
        this.key.text = "";
        this.value.text = "";

        this.key.enabled = true;
        this.value.enabled = true;

        for (int i = 0; i < btnText.Length; i++)
        {
            buttonsTxt[i].text = btnText[i];
            buttonsTxt[i].transform.parent.gameObject.SetActive(true);
        }

        GetComponent<ScreenOpener>()?.Open();
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(key.text) || string.IsNullOrEmpty(key.text))
        {
            PopupSystem.ShowPopup("Please enter valid text for key and value", null, "Close", () =>
            {

            }, url: keySucces, isError: true);

            return;
        }
        LoadingManager.ShowLoadingScreen();
        LootLockerSDKManager.UpdateOrCreateKeyValue(key.text, value.text, (response) =>
        {
            LoadingManager.HideLoadingScreen();
            if (response.success)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("Key", key.text);
                data.Add("Value", value.text);
                DemoAppGetPersistentStoragResponse mainResponse = JsonConvert.DeserializeObject<DemoAppGetPersistentStoragResponse>(response.text);
                PopupSystem.ShowPopup("Save Successful", data, "Close", () =>
                {
                    StagesManager.instance.GoToStage(StagesManager.StageID.Storage, mainResponse);
                    GetComponent<ScreenCloser>()?.Close();
                    PopupSystem.CloseNow();
                }, url: keySucces);
            }
        });
    }

    public void Close()
    {
        GetComponent<ScreenCloser>()?.Close();
    }

    public void Delete()
    {
        if (string.IsNullOrEmpty(key.text) || string.IsNullOrEmpty(key.text))
        {
            PopupSystem.ShowPopup("Please enter valid text for key and value", null, "Close", () =>
            {

            }, url: keySucces, isError: true);

            return;
        }
        LoadingManager.ShowLoadingScreen();
        LootLockerSDKManager.DeleteKeyValue(key.text,(response) =>
        {
            LoadingManager.HideLoadingScreen();
            if (response.success)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("Key", key.text);

                PopupSystem.ShowPopup("Deletion Successful", data, "Close", () =>
                {
                    StagesManager.instance.GoToStage(StagesManager.StageID.Storage, null);
                    GetComponent<ScreenCloser>()?.Close();
                    PopupSystem.CloseNow();
                }, url: keySucces);
            }
        });
    }
}
