using LootLocker;
using LootLockerRequests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InventoryItemElementEvent : UnityEvent<InventoryItemElement> { }
public class InventoryItemElement : MonoBehaviour
{
    public static InventoryItemElementEvent onElementClicked = new InventoryItemElementEvent();
    public GameObject activeBorder;
    public Image preview;
    public Text amountText;
    public InventoryAssetPair inventoryAssetPair = new InventoryAssetPair();
    InventoryScreen ic;
    int amountOfThisInventory;
    public Color activeColor;
    public Color selectedColor;
    public Color inActiveColor;
    public Color prevColor;
    bool isDefaultActive;

    private void Awake()
    {
        inventoryAssetPair = new InventoryAssetPair();
        onElementClicked.AddListener(OnElementClickChanged);
        preview = GetComponent<Image>();
        ic = GetComponentInParent<InventoryScreen>();
        GetComponent<Button>()?.onClick.AddListener(Select);
    }

    public void Init(InventoryAssetResponse.Inventory inventory, bool isDefault, int amountOfInventories)
    {
        inventoryAssetPair.inventory = inventory;
        inventoryAssetPair.asset = inventory.asset;
        amountText.text = amountOfInventories <= 1 ? "" : amountOfInventories.ToString();
        if (inventory.asset != null)
            inventory.asset.preview = preview;
        TexturesSaver.QueueForDownload(inventory.asset);
        activeBorder.GetComponent<Image>().color = inActiveColor;
        isDefaultActive = isDefault;
        if (isDefault)
            Activate();
    }

    public void Activate()
    {
        activeBorder.GetComponent<Image>().color = activeColor;
    }

    public void Select()
    {
        onElementClicked?.Invoke(this);
        GetComponentInParent<ContextController>()?.OnElementClicked(this);
    }

    public void OnElementClickChanged(InventoryItemElement itemElement)
    {
        if (this == itemElement)
        {
            ic?.SelectItem(itemElement.inventoryAssetPair);
            itemElement.activeBorder.GetComponent<Image>().color = selectedColor;
        }
        else
        {
            if (!isDefaultActive)
                activeBorder.GetComponent<Image>().color = inActiveColor;
            else
                activeBorder.GetComponent<Image>().color = activeColor;
        }
    }

}
