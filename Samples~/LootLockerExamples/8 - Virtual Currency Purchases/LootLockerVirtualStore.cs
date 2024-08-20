using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;

namespace LootLocker
{
    public class LootLockerVirtualStore : MonoBehaviour
    {
        public static LootLockerVirtualStore Instance { get; private set; }

        public Transform storeUI;
        public Transform virtualPurchaseProductPrefab;

        public Text goldAmount;

        public string catalogKey = "";
        public string walletID = "";

        private string currencyCode;

        public void Awake()
        {

            if (Instance == null)
            {
                Instance = this;
            }
            else Destroy(gameObject);

            LootLockerSettingsOverrider.OverrideSettings();

            LootLockerSDKManager.StartGuestSession((response) =>
            {
                if (!response.success)
                {
                    Debug.Log("Could not start guest session");
                    return;
                }
                FetchPlayerWallet(response.player_ulid);
                FetchVirtualProductsFromCatalog();

            });
        }

        public void FetchPlayerWallet(string ulid)
        {
            LootLockerSDKManager.GetWalletByHolderId(ulid, LootLockerEnums.LootLockerWalletHolderTypes.player, (response) =>
            {
                if (!response.success)
                {
                    Debug.Log("Could not start guest session");
                    return;
                }

                walletID = response.id;
                GetPlayerGold();
                GiveGold();
            });
        }

        public void GetPlayerGold()
        {
            LootLockerSDKManager.ListBalancesInWallet(walletID, (response) =>
            {
                if (!response.success)
                {
                    Debug.Log("Could not list balances");
                    return;
                }

                currencyCode = response.balances[0].currency.code.ToUpper() + ": ";

                goldAmount.text = currencyCode + response.balances[0].amount;
            });
        }


        public void FetchVirtualProductsFromCatalog()
        {
            LootLockerSDKManager.ListCatalogItems(catalogKey, 0, "", (response) =>
            {
                if (!response.success)
                {
                    Debug.Log("Could not fetch Catalog Items");
                    return;
                }


                foreach (var item in response.entries)
                {

                    switch(item.entity_kind){
                        case LootLockerEnums.LootLockerCatalogEntryEntityKind.asset:
                            var something = response.asset_details[item.GetItemDetailsKey()];
                            Debug.Log(something.name);
                            break;

                    }



                   
                    Debug.Log("Item: " + item.entity_name + " | " + item.prices[0].display_amount);
                    var obj = Instantiate(virtualPurchaseProductPrefab, storeUI);
                    obj.GetComponent<VirtualPurchaseProduct>().CreateProduct(item.entity_name,
                                                                                item.prices[0].amount,
                                                                                   item.catalog_listing_id);
                }
            });
        }

        public void GiveGold()
        {
            LootLockerSDKManager.CreditBalanceToWallet(walletID, "01J3MK8BTC6JC9850YTXGXP1H5", "200", (response) =>
            {
                if (!response.success)
                {
                    Debug.Log("Could not Credit gold");
                    return;
                }

                goldAmount.text = currencyCode + response.amount;
            });
        }
    }
}
