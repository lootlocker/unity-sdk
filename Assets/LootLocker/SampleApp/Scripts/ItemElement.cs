using LootLocker;
using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LootLockerDemoApp
{
    public class OnItemClickedEvent : UnityEvent<ItemElement> { }
    public class ItemElement : MonoBehaviour
    {
        public static OnItemClickedEvent onElementClicked = new OnItemClickedEvent();
        ILootLockertemData data;
        public GameObject activeBorder;
        Image preview;
        public InventoryAssetResponse.DemoAppAsset asset;
        public Color activeColor;
        public Color selectedColor;
        public Color inActiveColor;

        private void Awake()
        {
            preview = GetComponent<Image>();
            onElementClicked.AddListener(OnElementClicked);
            GetComponent<Button>()?.onClick.AddListener(Select);
        }

        public void OnElementClicked(ItemElement itemElement)
        {
            if (itemElement == this)
            {
                GetComponentInParent<StoreScreen>().currentAsset = asset;
                GetComponentInParent<StoreScreen>()?.SelectItem();
                activeBorder.GetComponent<Image>().color = selectedColor;
            }
            else
            {
                activeBorder.GetComponent<Image>().color = inActiveColor;
                //Inventory inventory = data as Inventory;
                //if (inventory != null)
                //{
                //    activeBorder.GetComponent<Image>().color = Color.blue;
                //    activeBorder.SetActive((bool)inventory.rental.is_active);
                //}
            }
        }

        public void Init(ILootLockertemData data)
        {
            this.data = data;
            asset = data as InventoryAssetResponse.DemoAppAsset;
            InventoryAssetResponse.Inventory inventory = data as InventoryAssetResponse.Inventory;
            if (asset == null && inventory != null)
                asset = inventory.asset;

            if (asset != null)
                asset.preview = preview;

            TexturesSaver.QueueForDownload(asset);
            activeBorder.GetComponent<Image>().color = inActiveColor;
            //if (inventory != null)
            //{
            //    activeBorder.SetActive((bool)inventory.rental.is_active);
            //}

        }

        public void Select()
        {
            onElementClicked?.Invoke(this);
        }

    }
}