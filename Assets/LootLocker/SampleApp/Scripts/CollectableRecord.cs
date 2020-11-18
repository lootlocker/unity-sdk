using LootLocker;
using LootLockerRequests;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace AppDemoLootLockerRequests
{

    #region GettingCollectable

    public class GettingCollectablesResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Collectable[] collectables { get; set; }
    }

    public class Collectable
    {
        public string name { get; set; }
        public Group[] groups { get; set; }
        public int completion_percentage { get; set; }
        public Reward[] rewards { get; set; }
    }

    public class Reward : IPopupData
    {
        public InventoryAssetResponse.DemoAppAsset asset { get; set; }
        public int asset_variation_id { get; set; }
        public object asset_rental_option_id { get; set; }
        public string header => "You got a reward";
        public Dictionary<string, string> normalText => new Dictionary<string, string>() { { "Reward", asset.name } };
        public string buttonText => "Ok";
        public Action btnAction => null;
        public Sprite sprite => asset.texture2D;
        public bool ignoreCurrentSprite => true;
        public string url => asset.url;
        public bool withKey => true;
    }

    public class Group
    {
        public string name { get; set; }
        public int completion_percentage { get; set; }
        public Item[] items { get; set; }
        public bool grants_all_rewards { get; set; }
        public Reward[] rewards { get; set; }
    }

    public class Item : IScreenShotOwner
    {
        public string name { get; set; }
        public bool collected { get; set; }
        public bool grants_all_rewards { get; set; }
        public Reward[] rewards { get; set; }
        public string url { get; set; }
        public Image preview { get; set; }
        public int downloadAttempts { get; set; }
        public LootLocker.File[] files { get; set; }
        public Sprite texture2D;

        public void SaveTexture(Sprite texture2D)
        {
            if (preview != null && texture2D != null)
            {
                preview.sprite = texture2D;
                preview.color = new Color(preview.color.r, preview.color.g, preview.color.b, 1);
            }
            else
            {
                preview.color = new Color(preview.color.r, preview.color.g, preview.color.b, 0);
            }
        }
    }

    #endregion

    #region CollectingAnItem

    public class CollectingAnItemRequest
    {
        public string slug { get; set; }

    }

    public class CollectingAnItemResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Collectable[] collectables { get; set; }

        public Collectable mainCollectable;

        public Group mainGroup;

        public Item mainItem;
    }



    #endregion

}

public class CollectableRecord : MonoBehaviour
{
    string itemToCollect;
    Button button;
    CollectableImage collectableImage;
    public Image image;
    string groupName;
    string collectableName;
    string itemName;
    AppDemoLootLockerRequests.Item item;
    string url;

    private void Awake()
    {
        button = GetComponent<Button>();
        button?.onClick.AddListener(() => OnElementClicked());
    }

    public void OnElementClicked()
    {
        string[] names = itemToCollect.Split('.');
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("Collectable", names[0]);
        data.Add("Group Name", names[1]);
        data.Add("Item Name", names[2]);
        string header = "";

        item.url = groupName + "_Active";

        PopupSystem.ShowPopup("Collectable", data, "Collect", () =>
        {
            LoadingManager.ShowLoadingScreen();
            LootLockerSDKManager.CollectingAnItem(itemToCollect, (response) =>
            {
                LoadingManager.HideLoadingScreen();
                if (response.success)
                {
                    Debug.Log("Success\n" + response.text);
                    header = "Success";
                    data.Clear();
                    AppDemoLootLockerRequests.CollectingAnItemResponse mainResponse = JsonConvert.DeserializeObject<AppDemoLootLockerRequests.CollectingAnItemResponse>(response.text);

                    string[] collectableStrings = itemToCollect.Split('.');

                    string collectable = collectableStrings[0];
                    string group = collectableStrings[1];
                    string tempItem = collectableStrings[2];

                    mainResponse.mainCollectable = mainResponse.collectables?.FirstOrDefault(x => x.name == collectable);

                    mainResponse.mainGroup = mainResponse.mainCollectable?.groups?.FirstOrDefault(x => x.name == group);

                    mainResponse.mainItem = mainResponse.mainGroup?.items?.FirstOrDefault(x => x.name == tempItem);
                    //Preparing data to display or error messages we have
                    data.Add("1", "You successfully collected: " + itemToCollect);
                    PopupSystem.ShowApprovalFailPopUp(header, data, item.url, false, onComplete: () =>
                     {
                         ShowRewards(mainResponse.mainItem, mainResponse.mainGroup, mainResponse.mainCollectable);
                     });
                    UpdateButtonAppearance(mainResponse.mainItem);
                }
                else
                {
                    header = "Collection Failed";
                    data.Clear();
                    //Preparing data to display or error messages we have
                    data.Add("1", "Collection of item failed");
                    PopupSystem.ShowApprovalFailPopUp(header, data, item.url, true);
                    Debug.Log("Failed\n" + response.text);
                }
            });
        }, groupName + "_Active");
    }

    public void Init(string collectableName, AppDemoLootLockerRequests.Item item)
    {
        itemToCollect = collectableName;
        string[] names = itemToCollect.Split('.');
        groupName =  names[1];
        itemName =  names[2];
        UpdateButtonAppearance(item);
    }

    private void UpdateButtonAppearance(AppDemoLootLockerRequests.Item item)
    {
        this.item = item;
        string sub = item.collected ? "_Active" : "_Inactive";
        item.url = groupName + sub;
        item.preview = image;
        TexturesSaver.QueueForDownload(item);
        button.interactable = !item.collected;
    }

    public void ShowRewards(AppDemoLootLockerRequests.Item item, AppDemoLootLockerRequests.Group group, AppDemoLootLockerRequests.Collectable collectable)
    {
        if (item.collected)
        {
            for (int i = 0; i < item.rewards.Length; i++)
            {
                PopupSystem.ShowScheduledPopup(item.rewards[i]);
            }

        }

        if (group.completion_percentage >= 100)
        {
            for (int i = 0; i < group.rewards.Length; i++)
            {
                PopupSystem.ShowScheduledPopup(group.rewards[i]);
            }
        }

        if (collectable.completion_percentage >= 100)
        {
            for (int i = 0; i < collectable.rewards.Length; i++)
            {
                PopupSystem.ShowScheduledPopup(collectable.rewards[i]);
            }
        }
    }
}
