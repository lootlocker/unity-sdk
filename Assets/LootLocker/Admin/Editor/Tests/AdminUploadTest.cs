using LootLocker.Admin.Requests;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.Networking;
using LootLocker;

namespace LootLocker.Admin
{
    public class AdminUploadTest : MonoBehaviour
    {
        [Header("Asset Info")]
        public string sendAssetID;

        public List<string> uploadPaths = new List<string>();
        public string[] splitFiles;
        public string[] lastSplit2;
        public string[] lastSplit;


        [ContextMenu("Upload Test")]
        public void UploadFile()
        {
            string path = EditorUtility.OpenFolderPanel("Select folder to upload", "", "");
            string[] files = Directory.GetFiles(path);
            if (files != null && files.Length > 0)
            {
                foreach (string file in files)
                {
                    Debug.Log(GetIdFromFile(file));
                    if (!file.EndsWith(".meta"))
                        LootLockerSDKAdminManager.UploadAFile(file, sendAssetID, LootLockerAdminConfig.current.gameID, (response) =>
                        {
                            if (response.success)
                            {
                                Debug.LogError("Successful created event: " + response.text);
                            }
                            else
                            {
                                Debug.LogError("failed to create event: " + response.Error);
                            }
                        });
                }
            }
            //  EditorUtility.DisplayDialog("Select Texture", "You must select a texture first!", "OK");
        }

        string GetIdFromFile(string file)
        {
            splitFiles = file.Split('/');
            lastSplit2 = splitFiles.Last().Split('\\');
            lastSplit = lastSplit2.Last().Split('.');
            return lastSplit.First();
        }

        [ContextMenu("LogCurrentToken")]
        public void LogCurrentToken()
        {
            Debug.Log(LootLocker.LootLockerBaseServerAPI.activeConfig.token);
        }
    }
}