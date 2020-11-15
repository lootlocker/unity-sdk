using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLockerRequests;
using LootLocker;//holds common stuff between admin and user
using System.Text.RegularExpressions;
using Newtonsoft.Json;

public class AssetTest : MonoBehaviour
{
    public string assetCountToDownload = "20";
    public string assetId;
    public string labelText;
    Vector2 scrollPosition;
    public string[] assetsToRequest = new string[] { "23", "2342", "32152" };

    private void OnGUI()
    {

        GUIStyle centeredTextStyle = new GUIStyle();
        centeredTextStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Back", GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000)))
            UnityEngine.SceneManagement.SceneManager.LoadScene("NavigationScene");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Asset ID");

        assetId = GUILayout.TextField(assetId, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
        assetId = Regex.Replace(assetId, @"[^0-9 ]", "");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Asset Count To Download:");

        assetCountToDownload = GUILayout.TextField(assetCountToDownload, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
        assetCountToDownload = Regex.Replace(assetCountToDownload, @"[^0-9 ]", "");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Get Asset List With Count", GUILayout.ExpandWidth(true)))
        {
            GetAssetListWithCount();
        }

        if (GUILayout.Button("Get Next Asset List", GUILayout.ExpandWidth(true)))
        {
            GetNextAssetList();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Reset Asset Calls", GUILayout.ExpandWidth(true)))
        {
            ResetAssetCalls();
        }

        if (GUILayout.Button("Get Asset Information", GUILayout.ExpandWidth(true)))
        {
            GetAssetInformation();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("List Favourite Assets", GUILayout.ExpandWidth(true)))
        {
            ListFavouriteAssets();
        }

        if (GUILayout.Button("Add Favourite Asset", GUILayout.ExpandWidth(true)))
        {
            AddFavouriteAsset();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Remove Favourite Asset", GUILayout.ExpandWidth(true)))
        {
            RemoveFavouriteAsset();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label(labelText);

        GUILayout.EndScrollView();

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

    }

    [ContextMenu("Get Asset List")]
    public void GetAssetListWithCount()
    {
        LootLockerSDKManager.GetAssetListWithCount(int.Parse(assetCountToDownload), (response) =>
         {

             if (response.success)
             {
                 labelText = "Success\n" + response.text;
             }
             else
             {
                 labelText = "Failed\n" + response.text;
             }

         });
    }

    [ContextMenu("Get Next Asset List")]
    public void GetNextAssetList()
    {
        LootLockerSDKManager.GetAssetNextList(int.Parse(assetCountToDownload), (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }
        });
    }

    [ContextMenu("ResetAssetCalls")]
    public void ResetAssetCalls()
    {
        AssetRequest.ResetAssetCalls();
    }

    [ContextMenu("GetAssetInformation")]
    public void GetAssetInformation()
    {
        LootLockerSDKManager.GetAssetInformation(assetId, (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }
        });
    }

    [ContextMenu("ListFavouriteAssets")]
    public void ListFavouriteAssets()
    {
        LootLockerSDKManager.ListFavouriteAssets((response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }
        });
    }

    [ContextMenu("AddFavouriteAsset")]
    public void AddFavouriteAsset()
    {
        LootLockerSDKManager.AddFavouriteAsset(assetId, (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }
        });
    }

    [ContextMenu("RemoveFavouriteAsset")]
    public void RemoveFavouriteAsset()
    {
        LootLockerSDKManager.RemoveFavouriteAsset(assetId, (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }
        });
    }

    [ContextMenu("GetAssetsByiD")]
    public void GetAssetsByIds()
    {
        LootLockerSDKManager.GetAssetsById(assetsToRequest, (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }
        });
    }

}
