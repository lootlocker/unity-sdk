using LootLocker.LootLockerEnums;
using LootLocker;
using LootLocker.Requests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker.Example
{
    public class UserGeneratedTest : MonoBehaviour
    {
        public string assetName = "fgehgbrfg";
        public int assetId;
        public bool sendContextId;
        public string filePath;
        public string fileName;
        public LootLocker.LootLockerEnums.FilePurpose filePurpose;
        public int fileId;
        public bool markAssetAsComplete = true;

        [ContextMenu("CreatingAnAssetCandidate")]
        public void CreatingAnAssetCandidate()
        {

            LootLockerSDKManager.CreatingAnAssetCandidate(assetName, (response) =>
            {
                if (response.success)
                {
                    LootLockerSDKManager.DebugMessage("Successful");
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("failed: " + response.Error, true);
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
                     LootLockerSDKManager.DebugMessage("Successful");
                 }
                 else
                 {
                     LootLockerSDKManager.DebugMessage("failed: " + response.Error, true);
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
                    LootLockerSDKManager.DebugMessage("Successful");
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("failed: " + response.Error, true);
                }
            });//GettingASingleAssetCandidate
        }

        [ContextMenu("GettingAAssetCandidate")]
        public void GettingASingleAssetCandidate()
        {

            LootLockerSDKManager.GettingASingleAssetCandidate(assetId, (response) =>
            {
                if (response.success)
                {
                    LootLockerSDKManager.DebugMessage("Successful" + response.asset_candidate.asset_id);
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("failed: " + response.Error, true);
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
                    LootLockerSDKManager.DebugMessage("Successful");
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("failed: " + response.Error, true);
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
                    LootLockerSDKManager.DebugMessage("Successful");
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("failed: " + response.Error, true);
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
                    LootLockerSDKManager.DebugMessage("Successful");
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("failed: " + response.Error, true);
                }
            });
        }
    }
}