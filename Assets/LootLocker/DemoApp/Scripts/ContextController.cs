using LootLocker;
using LootLockerRequests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Enums;

public class InventoryAssetPair
{
    public InventoryAssetResponse.Inventory inventory;
    public InventoryAssetResponse.Asset asset;
    public bool isDefault;
}
namespace InventoryAssetResponse
{

    public class Links
    {
        public string thumbnail { get; set; }
    }

    public class Default_Loadouts_Info
    {
        public bool Default { get; set; }
    }

    public class Variation_Info
    {
        public int id { get; set; }
        public string name { get; set; }
        public object primary_color { get; set; }
        public object secondary_color { get; set; }
        public object links { get; set; }
    }

    [System.Serializable]
    public class AssetRequest : LootLockerResponse
    {
        public int count;
        public static int lastId;
    }

    public class AssetResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Asset[] assets { get; set; }
    }

    public class Rental_Options
    {
        public int id { get; set; }
        public string name { get; set; }
        public int duration { get; set; }
        public int price { get; set; }
        public object sales_price { get; set; }
        public object links { get; set; }
    }

    public class Asset : ItemData, IScreenShotOwner, IStageData
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
        public bool purchasable { get; set; }
        public string type { get; set; }
        public int price { get; set; }
        public string sales_price { get; set; }
        public string display_price { get; set; }
        public string context { get; set; }
        public string unlocks_context { get; set; }
        public bool detachable { get; set; }
        public string updated { get; set; }
        public string marked_new { get; set; }
        public int default_variation_id { get; set; }
        public string description { get; set; }
        public Links links { get; set; }
        public string[] storage { get; set; }
        public string rarity { get; set; }
        public bool popular { get; set; }
        public int popularity_score { get; set; }
        public bool unique_instance { get; set; }
        public string external_identifiers { get; set; }
        public Rental_Options[] rental_options { get; set; }
        public string[] filters { get; set; }
        public Variation[] variations { get; set; }
        public bool featured { get; set; }
        public bool context_locked { get; set; }
        public bool initially_purchasable { get; set; }
        public string url => links?.thumbnail;
        public Image preview { get; set; }
        public int downloadAttempts { get; set; }
        public File[] files { get; set; }
        public Sprite texture2D;

        public void SaveTexture(Sprite texture2D)
        {
            if (preview != null)
            {
                preview.sprite = texture2D;
                preview.color = new Color(preview.color.r, preview.color.g, preview.color.b, 1);
            }
            this.texture2D = texture2D;
        }
        public Enums.DownloadState downloadState;
        public void SetState(DownloadState downloadState)
        {
            this.downloadState = downloadState;
        }

    }

    public class File
    {
        public string url { get; set; }
        public string[] tags { get; set; }
    }


    public class Default_Loadouts
    {
        public bool Default { get; set; }
    }

    public class Variation
    {
        public int id { get; set; }
        public string name { get; set; }
        public object primary_color { get; set; }
        public object secondary_color { get; set; }
        public object links { get; set; }
    }


    [System.Serializable]

    public class InventoryResponse : LootLockerResponse
    {

        public bool success;

        public Inventory[] inventory;

    }
    public class Inventory : ItemData
    {
        public int instance_id { get; set; }
        public int? variation_id { get; set; }
        public string rental_option_id { get; set; }
        public string acquisition_source { get; set; }
        public Asset asset { get; set; }
        public Rental rental { get; set; }


        public float balance;
    }
    [System.Serializable]
    public class AssetClass
    {
        public string Asset { get; set; }
    }
    [System.Serializable]
    public class Rental
    {
        public bool is_rental { get; set; }
        public string time_left { get; set; }
        public string duration { get; set; }
        public string is_active { get; set; }
    }
    [System.Serializable]
    public class XpSubmitRequest
    {
        public int points;

        public XpSubmitRequest(int points)
        {
            this.points = points;
        }
    }
    [System.Serializable]
    public class XpRequest : LootLockerGetRequest
    {
        public XpRequest()
        {
            getRequests.Clear();
            getRequests.Add(LootLockerConfig.current.deviceID);
            getRequests.Add(LootLockerConfig.current.platform.ToString());
        }
    }
}

public class ContextController : MonoBehaviour
{
    public string context;
    public Transform inventoryParent;
    public GameObject inventoryPrefab;
    public Text header;
    List<InventoryItemElement> elements = new List<InventoryItemElement>();
    InventoryScreen ic;
    string defaultAssetId;
    public Color inActiveColor;

    public void Init(string context, string defaultAssetId)
    {
        this.context = context;
        this.defaultAssetId = defaultAssetId;
        header.text = context;
        ic = GetComponentInParent<InventoryScreen>();
    }

    public void Populate(InventoryAssetResponse.Inventory[] inventories, Dictionary<int, int> numberOfInventories)
    {
        foreach (Transform tr in inventoryParent)
        {
            Destroy(tr.gameObject);
        }
        elements.Clear();
        foreach (InventoryAssetResponse.Inventory inventory in inventories)
        {
            GameObject inventoryObject = Instantiate(inventoryPrefab, inventoryParent);
            InventoryItemElement element = inventoryObject.GetComponent<InventoryItemElement>();
            bool isDefault = inventory.asset.id.ToString() == defaultAssetId;
            element.Init(inventory, isDefault, numberOfInventories[inventory.asset.id]);
            elements.Add(element);
        }
        //for (int i = 0; i < inventories.Length; i++)
        //{
        //    InventoryItemElement element = elements.FirstOrDefault(x => x.inventoryAssetPair.asset.id == inventories[i].asset.id);
        //    if (element != null)
        //        element.Init(inventories[i]);
        //}
    }

    public void Populate(InventoryAssetResponse.Asset[] inventories)
    {
        foreach (Transform tr in inventoryParent)
        {
            Destroy(tr.gameObject);
        }
        elements.Clear();
        foreach (InventoryAssetResponse.Asset asset in inventories)
        {
            GameObject inventoryObject = Instantiate(inventoryPrefab, inventoryParent);
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("Credits", asset.price.ToString());
            data.Add("Asset Name", asset.name);
            data.Add("Asset Context", asset.context);
            inventoryObject.GetComponent<ItemElement>().Init(asset);
        }
    }

    public void OnElementClicked(InventoryItemElement itemElement)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] == itemElement)
            {
                //ic?.SelectItem(itemElement.inventoryAssetPair);
                //itemElement.activeBorder.GetComponent<Image>().color = selectedColor;
            }
            else
            {
                //elements[i].activeBorder.GetComponent<Image>().color = inActiveColor;
            }

        }
    }


}
