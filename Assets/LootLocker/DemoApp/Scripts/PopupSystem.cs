using LootLockerRequests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PopupData:IPopupData,IScreenShotOwner
{
    public string header { get; set; }
    public Dictionary<string, string> normalText { get; set; }
    public string buttonText { get; set; }
    public Action btnAction { get; set; }
    public Sprite sprite { get; set; }
    public bool withKey { get; set; }
    public bool isError { get; set; }
    public bool ignoreCurrentSprite { get; set; }
    public string url { get; set; }
    public void SaveTexture(Sprite texture2D)
    {
        if (preview != null)
        {
            preview.sprite = texture2D;
            preview.color = new Color(preview.color.r, preview.color.g, preview.color.b, 1);
        }
        sprite = texture2D;
    }
    public Image preview { get; set; }
    public int downloadAttempts { get; set; }

    public PopupData(string header, Dictionary<string, string> normalText, string buttonText, Action btnAction, string url = null, bool withKey = true, bool isError = false)
    {
        this.header = header;
        this.normalText = normalText;
        this.buttonText = buttonText;
        this.btnAction = btnAction;
        this.url = url;
        this.isError = isError;
        this.withKey = withKey;
    }
    public PopupData()
    {
    }
}

public class PopupSystem : MonoBehaviour
{
    public static PopupSystem Instance;
    public Button button;
    public Text btnText;
    public Text headerText;
    public Text normalText;
    public Image image;
    public List<PopupData> messages = new List<PopupData>();
    bool displaying;
    PopupData currentPopup;

    private void Awake()
    {
        Instance = this;
    }

    public static void ShowPopup(PopupData popupData)
    {
        Instance.currentPopup = popupData;
        Instance?.button?.onClick?.RemoveAllListeners();
        Instance?.button?.onClick?.AddListener(() => { popupData?.btnAction(); });
        Instance.normalText.text = "";
        Instance.headerText.color = Color.black;

        if (popupData.normalText != null)
        {
            foreach (var data in popupData.normalText)
            {
                Instance.normalText.text += popupData.withKey ? data.Key + " : " + data.Value + "\n" : data.Value + "\n";
            }
        }

        Instance.btnText.text = popupData.buttonText;
        Instance.headerText.text = popupData.header;
        Instance.headerText.color = popupData.isError ? Color.red : Color.black;
        Instance.GetComponent<ScreenOpener>()?.Open();
        popupData.preview = Instance.image;
        TexturesSaver.QueueForDownload(popupData);
    }

    public static void ShowPopup(string header, Dictionary<string, string> normalText, string buttonText, Action btnAction, string url = null, bool withKey = true, bool isError = false)
    {
        PopupData popupData = new PopupData(header, normalText, buttonText, btnAction, url, withKey, isError);
        ShowPopup(popupData);
    }

    public static void ShowApprovalFailPopUp(string header, Dictionary<string, string> data, string url, bool isError = false, Action onComplete = null)
    {
        ShowPopup(header, data, "Close", () =>
        {
            CloseNow();
            onComplete?.Invoke();
        }, url, false, isError);
    }

    public void Close()
    {
        CloseNow();
    }

    public static void CloseNow()
    {
        Instance.image.color = new Color(Instance.image.color.r, Instance.image.color.g, Instance.image.color.b, 0);
        Instance.currentPopup.preview = null;
        Instance.displaying = false;
        Instance.GetComponent<ScreenCloser>()?.Close();
    }

    public static void ShowScheduledPopup(PopupData message)
    {
        if (!Instance.messages.Contains(message))
        {
            Instance.messages.Add(message);
            if (!Instance.displaying)
                Instance.Show();
        }
    }

    public static void ShowScheduledPopup(IPopupData data)
    {
        PopupData message = new PopupData();
        message.btnAction = data.btnAction;
        message.buttonText = data.buttonText;
        message.header = data.header;
        message.normalText = data.normalText;
        message.sprite = data.sprite;
        message.ignoreCurrentSprite = data.ignoreCurrentSprite;
        message.url = data.url;

        if (!Instance.messages.Contains(message))
        {
            Instance.messages.Add(message);
            if (!Instance.displaying)
            {
                Instance.Show();
            }
        }
    }

    public void Show()
    {
        if (messages.Count > 0)
        {
            PopupData mess = messages.First();
            if (mess != null)
            {
                displaying = true;
                messages.Remove(mess);
                ShowPopup(mess);
                Action action = mess.btnAction;

                Action tempAction = () =>
                {
                    action?.Invoke();
                    Show();
                };

                mess.btnAction = tempAction;
            }
            else
            {
                messages.Remove(mess);
            }
        }
        else
        {
            displaying = false;
            Close();
        }
    }
}
