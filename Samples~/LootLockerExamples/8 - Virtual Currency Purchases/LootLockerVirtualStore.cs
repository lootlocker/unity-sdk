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

        private string playerUlid = "";

        private string currencyCode = "gld";
        private string currencyDisplayCode = "";
        private string currencyIdentifier = "";

        private string grantCurrencyAmount = "200";

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
                    Debug.Log("Could not start guest session\nError: " + response.errorData.ToString());
                    return;
                }

                playerUlid = response.player_ulid;

                FetchGameCurrencies();  
            });
        }

        public void FetchGameCurrencies()
        {
            LootLockerSDKManager.ListCurrencies((response) =>
            {

                if (!response.success)
                {
                    Debug.Log("Could not find Currencies\nError: " + response.errorData.ToString());
                    return;
                }

                foreach (var currency in response.currencies)
                {
                    if (currency.code == currencyCode)
                    {
                        currencyIdentifier = currency.id;
                        currencyDisplayCode = currency.code.ToUpper() + ": ";
                    }
                }

                FetchPlayerWallet(playerUlid);
            });
        }

        public void FetchPlayerWallet(string ulid)
        {
            LootLockerSDKManager.GetWalletByHolderId(ulid, LootLockerEnums.LootLockerWalletHolderTypes.player, (response) =>
            {
                if (!response.success)
                {
                    Debug.Log("Could not Get Players Wallet\nError: " + response.errorData.ToString());
                    return;
                }

                walletID = response.id;

                FetchVirtualProductsFromCatalog();

                GiveGold();
            });
        }

        public void GetPlayerGold()
        {
            LootLockerSDKManager.ListBalancesInWallet(walletID, (response) =>
            {
                if (!response.success)
                {
                    Debug.Log("Could not list balances\nError: " + response.errorData.ToString());
                    return;
                }

                goldAmount.text = currencyDisplayCode + response.balances[0].amount;

            });
        }

        public void FetchVirtualProductsFromCatalog()
        {
            LootLockerSDKManager.ListCatalogItems(catalogKey, 0, "", (response) =>
            {
                if (!response.success)
                {
                    Debug.Log("Could not fetch Catalog Items\nError: " + response.errorData.ToString());
                    return;
                }

                foreach (var item in response.entries)
                {                   
                    var obj = Instantiate(virtualPurchaseProductPrefab, storeUI);
                    obj.GetComponent<VirtualPurchaseProduct>().CreateProduct(item.entity_name,
                                                                                item.prices[0].amount,
                                                                                   item.catalog_listing_id);
                }
            });
        }

        public void GiveGold()
        {
            LootLockerSDKManager.CreditBalanceToWallet(walletID, currencyIdentifier, grantCurrencyAmount, (response) =>
            {
                if (!response.success)
                {
                    Debug.Log("Could not Credit gold\nError: " + response.errorData.ToString());
                    return;
                }

                goldAmount.text = currencyDisplayCode + response.amount;
                GetPlayerGold();
            });
        }
    }
}