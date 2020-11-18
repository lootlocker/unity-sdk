using LootLockerRequests;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using LootLocker;

public class FileItemElement : MonoBehaviour
{
    public Image preview;
    public Text text;
    //private Asset asset;

    public void Init(InventoryAssetResponse.DemoAppAsset asset)
    {
        //this.asset = asset;
        if (asset != null)
        {
            text.text = asset.name;
            asset.preview = preview;

        }
        TexturesSaver.QueueForDownload(asset);
    }
}
