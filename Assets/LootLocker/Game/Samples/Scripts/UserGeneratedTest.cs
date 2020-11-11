using enums;
using LootLockerRequests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserGeneratedTest : MonoBehaviour
{
    public string assetName = "fgehgbrfg";
    public int assetId;
    public bool sendContextId;
    public string filePath;
    public string fileName;
    public enums.FilePurpose filePurpose;
    public int fileId;
    public bool markAssetAsComplete = true;

    [ContextMenu("CreatingAnAssetCandidate")]
    public void CreatingAnAssetCandidate()
    {

        LootLockerSDKManager.CreatingAnAssetCandidate(assetName, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Successful");
            }
            else
            {
                Debug.Log("failed: " + response.Error);
            }
        }, context_id: sendContextId ? 21 : -1);
    }

    [ContextMenu("UpdatingAnAssetCandidate")]
    public void UpdatingAnAssetCandidate()
    {

        LootLockerSDKManager.UpdatingAnAssetCandidate(assetId, markAssetAsComplete, (response) =>
         {
             if (response.success)
             {
                 Debug.Log("Successful");
             }
             else
             {
                 Debug.Log("failed: " + response.Error);
             }
         }, name: assetName);
    }

    [ContextMenu("DeletingAnAssetCandidate")]
    public void DeletingAnAssetCandidate()
    {

        LootLockerSDKManager.DeletingAnAssetCandidate(assetId, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Successful");
            }
            else
            {
                Debug.Log("failed: " + response.Error);
            }
        });
    }

    [ContextMenu("ListingAssetCandidates")]
    public void ListingAssetCandidates()
    {

        LootLockerSDKManager.ListingAssetCandidates((response) =>
        {
            if (response.success)
            {
                Debug.Log("Successful");
            }
            else
            {
                Debug.Log("failed: " + response.Error);
            }
        });
    }

    [ContextMenu("AddingFilesToAssetCandidates")]
    public void AddingFilesToAssetCandidates()
    {

        LootLockerSDKManager.AddingFilesToAssetCandidates(assetId, filePath, fileName, filePurpose, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Successful");
            }
            else
            {
                Debug.Log("failed: " + response.Error);
            }
        });
    }

    [ContextMenu("RemovingFilesFromAssetCandidates")]
    public void RemovingFilesFromAssetCandidates()
    {

        LootLockerSDKManager.RemovingFilesFromAssetCandidates(assetId, fileId, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Successful");
            }
            else
            {
                Debug.Log("failed: " + response.Error);
            }
        });
    }
}
