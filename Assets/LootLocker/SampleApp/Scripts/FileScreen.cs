using LootLocker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using LootLockerEnums;


namespace LootLockerEnums
{
    public enum TypeOfFile { Image, Scene, AssetBundle, None }
}

namespace LootLockerDemoApp
{
    public class TempImageClass : IScreenShotOwner
    {
        public Action OnDownloadCompleted;
        public string url { get; set; }
        public void SaveTexture(Sprite texture2D)
        {
            preview.sprite = texture2D;
            OnDownloadCompleted?.Invoke();
        }
        public Image preview { get; set; }
        public int downloadAttempts { get; set; }
        public Action onScreenShotDownloaded { get; set; }
    }



    public class FileScreen : MonoBehaviour, IStageOwner
    {
        public Text typeOfFile;
        public Image image;
        public Button downloadFileBtn;
        public Button bckBtn;
        InventoryAssetResponse.DemoAppAsset asset;
        string[] imageExtenstions = new string[] { "jpg", "jpeg", "png", "PNG", "JPG", "JPEG" };
        string[] assetBundle = new string[] { "unity3d" };
        string[] scene = new string[] { "unity" };
        string url = "";

        private void Awake()
        {
            downloadFileBtn.onClick.RemoveAllListeners();
            bckBtn.onClick.AddListener(ViewInventory);
        }

        public void UpdateScreenData(IStageData stageData)
        {
            asset = stageData as InventoryAssetResponse.DemoAppAsset;
            if (asset != null)
            {
                if (asset.files != null && asset.files.Length > 0)
                {
                    url = asset.files.Last().url;
                    TypeOfFile filetype = GetFileType(url);
                    downloadFileBtn.onClick.AddListener(() =>
                    {
                        LoadingManager.ShowLoadingScreen();
                        DownloadFile(filetype);
                    });
                }
            }
        }

        public void ViewInventory()
        {
            LoadingManager.ShowLoadingScreen();
            StagesManager.instance.GoToStage(StagesManager.StageID.Inventory, null);
            image.sprite = null;
        }

        public TypeOfFile GetFileType(string fileLink)
        {
            string[] filesArray = fileLink.Split('.');
            string ext = filesArray.Last();
            TypeOfFile fileType = TypeOfFile.Image;
            if (imageExtenstions.Contains(ext))
            {
                typeOfFile.text = "Avatar Image";
                fileType = TypeOfFile.Image;
            }
            else if (assetBundle.Contains(ext))
            {
                typeOfFile.text = "Asset Bundle";
                fileType = TypeOfFile.AssetBundle;
            }
            else if (scene.Contains(ext))
            {
                typeOfFile.text = "Scene File";
                fileType = TypeOfFile.AssetBundle;
            }
            return fileType;
        }

        public void DownloadFile(TypeOfFile typeOfFile)
        {
            switch (typeOfFile)
            {
                case TypeOfFile.Image:
                    TempImageClass tempImageClass = new TempImageClass();
                    tempImageClass.url = url;
                    tempImageClass.preview = image;
                    tempImageClass.OnDownloadCompleted = () => { LoadingManager.HideLoadingScreen(); };
                    TexturesSaver.QueueForDownload(tempImageClass);
                    break;
            }
        }

    }
}