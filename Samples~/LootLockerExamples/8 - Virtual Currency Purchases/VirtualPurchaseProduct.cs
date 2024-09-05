using LootLocker;
using LootLocker.Requests;
using UnityEngine;
using UnityEngine.UI;

public class VirtualPurchaseProduct : MonoBehaviour
{

    public Text productName;
    public Text price;

    private string productID;

    public void CreateProduct(string _productName, int _price, string _productID)
    {
        productName.text = "Buy " + _productName;
        price.text = "Price: " + _price.ToString();

        productID = _productID;
    }

    public void Purchase()
    {
        LootLockerSDKManager.LootLockerPurchaseSingleCatalogItem(LootLockerVirtualStore.Instance.walletID, productID, 1, (response) =>
        {
            if (!response.success)
            {
                LootLockerVirtualStoreWarner.Instance.ShowText(response.errorData.message);
                return;
            }

            LootLockerVirtualStore.Instance.GetPlayerGold();
        });
    }

}
