using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLockerRequests;
using LootLocker;
using System.Linq;
using Newtonsoft.Json;

namespace LootLockerDemoApp
{
    public class StoreScreen : MonoBehaviour, IStageOwner
    {
        [Header("Store")]
        public Transform parent;
        public GameObject contextPrefab;
        public Text normalText, headerText;
        public ToggleGroup inventoryToggles;
        public InventoryAssetResponse.DemoAppAsset currentAsset;
        public Button selectBtn;
        public Text selectText;
        public Text creditsAmount;
        public bool isEasyPrefab;


        private void Awake()
        {
            selectBtn.onClick.AddListener(ButtonClicked);
            if (isEasyPrefab)
            {
                SetUpEasyPrefab();
                ViewStore();
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

        public void ViewStore()
        {
            for (int i = 0; i < parent.childCount; i++)
                Destroy(parent.GetChild(i).gameObject);
            this.normalText.text = "";
            this.normalText.text += "Price" + " : " + "" + "\n";
            this.normalText.text += "Asset Name" + " : " + "" + "\n";
            this.normalText.text += "Asset Context" + " : " + "" + "\n";
            LootLockerSDKManager.GetEquipableContextToDefaultCharacter((contextResponse) =>
            {
                LoadingManager.HideLoadingScreen();
                if (contextResponse.success)
                {
                    Debug.Log("Successful got context: " + contextResponse.text);

                    UpdateBalanceDisplay();

                    LootLockerSDKManager.GetAssetListWithCount(50, (response) =>
                    {
                        Debug.Log("Successful got Assets: " + response.text);
                        LoadingManager.HideLoadingScreen();

                        InventoryAssetResponse.AssetResponse mainResponse = JsonConvert.DeserializeObject<InventoryAssetResponse.AssetResponse>(response.text);

                        if (mainResponse.success)
                        {
                            string[] contexts = contextResponse.contexts.Select(x => x.name).ToArray();
                            InventoryAssetResponse.DemoAppAsset[] assets = mainResponse.assets?.Where(x => x.purchasable && x.price > 0)?.ToArray();
                            for (int i = 0; i < contexts.Length; i++)
                            {
                                GameObject contextObject = Instantiate(contextPrefab, parent);
                                ContextController contextController = contextObject.GetComponent<ContextController>();
                                contextController.Init(contexts[i], assets.FirstOrDefault(x => x.context == contexts[i])?.id.ToString());

                                InventoryAssetResponse.DemoAppAsset[] assetsForContextController = assets.Where(x => x.context == contextController.context)?.ToArray();
                                contextController.Populate(assetsForContextController);
                            }

                            this.headerText.text = "Store";
                            selectText.text = "Buy";
                            this.normalText.text = "";
                            this.normalText.text += "Price" + " : " + "" + "\n";
                            this.normalText.text += "Asset Name" + " : " + "" + "\n";
                            this.normalText.text += "Asset Context" + " : " + "" + "\n";
                        }
                        else
                        {
                            Debug.LogError("failed to get all inventory items: " + response.Error);
                        }
                    });
                }
                else
                {
                    Debug.LogError("Failed to get context: " + contextResponse.Error);
                }
            });
        }

        public void SelectItem()
        {
            this.normalText.text = "";
            this.normalText.text += "Price" + " : " + currentAsset.price.ToString() + "\n";
            this.normalText.text += "Asset Name" + " : " + currentAsset.name + "\n";
            this.normalText.text += "Asset Context" + " : " + currentAsset.context + "\n";
        }

        public void ButtonClicked()
        {
            string header = "Buy Item";
            string btnText = "Buy";
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("Asset Name", currentAsset.name);
            data.Add("Asset Context", currentAsset.context);
            data.Add("Cost", currentAsset.price.ToString());
            PopupSystem.ShowPopup(header, data, btnText, () =>
            {
                LoadingManager.ShowLoadingScreen();
                LootLockerSDKManager.NormalPurchaseCall(currentAsset.id, currentAsset.variations.First().id, (response) =>
                {
                    LoadingManager.HideLoadingScreen();

                    if (response.success)
                    {
                        Debug.Log(response.text);
                        UpdateBalanceDisplay();

                        header = "Success";
                        data.Clear();
                    //Preparing data to display or error messages we have
                    data.Add("1", "You successfully bought: " + currentAsset.name);
                        PopupSystem.ShowApprovalFailPopUp(header, data, currentAsset.url, false);
                    }
                    else
                    {
                        Debug.Log(response.Error);

                    //making object from error json
                    string correctedResponse = response.Error.First() == '{' ? response.Error : response.Error.Substring(response.Error.IndexOf('{'));
                        ResponseError purchaseErrorResponse = new ResponseError();
                        purchaseErrorResponse = JsonConvert.DeserializeObject<ResponseError>(correctedResponse);

                        header = "Purchase Failed";
                        data.Clear();
                    //Preparing data to display or error messages we have
                    data.Add("1", purchaseErrorResponse.messages[0]);
                        PopupSystem.ShowApprovalFailPopUp(header, data, currentAsset.url, true);
                    }

                });
            }, currentAsset.url);
        }

        public void UpdateScreenData(IStageData stageData)
        {
            ViewStore();
        }

        private void UpdateBalanceDisplay()
        {
            LootLockerSDKManager.GetBalance((response) =>
            {
                if (response.success)
                {
                    Debug.Log(response.text);
                    creditsAmount.text = response.balance.ToString();
                }
                else
                {
                    Debug.Log(response.Error);
                }
            });
        }
    }
}