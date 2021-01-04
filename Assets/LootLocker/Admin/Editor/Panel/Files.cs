using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LootLockerAdminRequests;
using ViewType;
using System;
using LootLocker;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;

namespace LootLockerAdmin
{
    public partial class LootlockerAdminPanel : EditorWindow
    {

        void PopulateFiles()
        {
            Debug.Log("Getting files..");
            LootLockerSDKAdminManager.GetFiles(LootLockerAdminRequests.FileFilterType.none, (response) =>
            {
                Debug.Log("files on complete");
                if (response.success)
                {

                    LootLockerSDKAdminManager.GetAssets((assetssResponse) =>
                    {
                        LootLockerSDKAdminManager.GetContexts((contextResponse) =>
                        {
                            if (contextResponse.success)
                            {
                                Contexts = contextResponse.Contexts;
                                ContextNames = Contexts.Select(x => x.name).ToArray();
                                Debug.Log("Successful got all contexts: " + contextResponse.text);
                            }
                            else
                            {
                                Debug.LogError("failed to get all contexts: " + contextResponse.Error);
                            }

                            if (assetssResponse.success)
                            {
                                assetsResponse = assetssResponse;
                                Debug.Log("Successful got all assets: " + response.text);
                            }
                            else
                            {
                                Debug.LogError("failed to get all assets: " + response.Error);
                            }

                        });
                    });

                    getFilesResponse = response;
                    currentView = View.Files;
                    Repaint();
                    Debug.Log("Successful got all files: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to get all files: " + response.Error);
                }

            // getFilesResponse = new GetFilesResponse()
            // {
            //     success = true,
            //     files = new File[]
            //     {
            //         new File()
            //         {
            //             name = "testName1",
            //             id = 2,
            //         },
            //         new File()
            //         {
            //             name = "testName2",
            //             id = 3,
            //             tags = new string[]{"tag1" , "tag2" , "tag3" },
            //         },
            //         new File()
            //         {
            //             name = "testName3",
            //             id = 4,
            //         },
            //         new File()
            //         {
            //             name = "testName4",
            //             id = 5,
            //             tags = new string[]{"tag1" , "tag2"  },
            //         },
            //         new File()
            //         {
            //             name = "testName5",
            //             id = 6,
            //         },

            //     }
            // };

            // currentView = View.Files;
            // Repaint();
            // DestroyImmediate(ServerAPI.Instance.gameObject);
            // ServerAPI.ResetManager();
            // Debug.Log("Successful got all files: " + response.text);
        });
        }

        string FileTags;
        LootLockerAdminRequests.File activeFile;

        void DrawFilesView()
        {
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            GUILayout.BeginArea(ContentSection);

            if (GUILayout.Button("Back", GUILayout.Height(20))) currentView = View.Menu;
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Files", style);
            EditorGUILayout.Separator();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Upload New File", GUILayout.Height(40)))
            {
                CreateFile();
            }
            EditorGUILayout.EndHorizontal();

            if (getFilesResponse != null && getFilesResponse.files != null)
            {
                filesViewScrollPos = EditorGUILayout.BeginScrollView(filesViewScrollPos);
                var style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 20 };

                for (int i = 0; i < getFilesResponse.files.Length; i++)
                {
                    if (GUILayout.Button(new GUIContent($"  {getFilesResponse.files[i].name}     Id({getFilesResponse.files[i].id})", FileTexture), style, GUILayout.Height(40)))
                    {
                        SelectFile(i);
                    }
                    var tags = String.Empty;
                }

                EditorGUILayout.EndScrollView();
            }

            GUILayout.EndArea();

        }

        void DrawFileView()
        {
            GUILayout.BeginArea(ContentSection);

            EditorGUILayout.LabelField("Tags");
            EditorGUILayout.LabelField("saperated by comma", new GUIStyle(GUI.skin.label) { fontSize = 10 });
            FileTags = EditorGUILayout.TextField(FileTags);

            if (GUILayout.Button("Update", GUILayout.Height(50)))
            {
                LootLockerSDKAdminManager.UpdateFile(activeFile.id.ToString(), new UpdateFileRequest() { tags = FileTags.Split(',') }, (updateResponse) =>
                {
                    if (updateResponse.success)
                    {
                        ButtomMessage = "Update Succeeded";
                        Debug.Log("Successful update file: " + updateResponse.text);
                    }
                    else
                    {
                        Debug.LogError("failed to update file: " + updateResponse.Error);
                    }
                });
            }

            if (GUILayout.Button("Delete", GUILayout.Height(50)))
            {
                LootLockerSDKAdminManager.DeleteFile(activeFile.id.ToString(), (response) =>
                {
                    if (response.success)
                    {
                        ButtomMessage = "Delete Succeeded";
                        PopulateFiles();
                        Debug.Log("Successful deleted file: " + response.text);
                    }
                    else
                    {
                        Debug.LogError("failed to delete file: " + response.Error);
                    }
                });
            }

            if (GUILayout.Button("Back", GUILayout.Height(20))) PopulateFiles();

            GUILayout.EndArea();
        }

        //attach to some asset
        int FileAssociatedAssetIndex;
        Asset AllAssets;
        string[] AssetsNames;

        int assetContextID;

        string assetName,
            filePath;

        void DrawCreateFileView()
        {
            GUILayout.BeginArea(ContentSection);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Asset name: ");
            assetName = EditorGUILayout.TextField(assetName);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Asset Context ID: ");

            activeAsset = assetsResponse.assets[0];

            AssetContextIndex = EditorGUILayout.Popup(AssetContextIndex, ContextNames);
            activeAsset.context = Contexts[AssetContextIndex].name;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Tags");

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("saperated by comma", new GUIStyle(GUI.skin.label) { fontSize = 10 });
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            FileTags = EditorGUILayout.TextField(FileTags);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(string.IsNullOrEmpty(filePath) ? "Attach File" : "Choose a different file", GUILayout.Height(30), GUILayout.MaxWidth(200)))
            {

                filePath = EditorUtility.OpenFilePanel("Choose a file", "", "");

            }

            if (string.IsNullOrEmpty(filePath))
            {

                EditorGUILayout.Separator();
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("You must attach a file to be able to upload",
                    new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft },
                    GUILayout.MaxWidth(1000), GUILayout.Height(25));

            }
            else
            {
                EditorGUILayout.LabelField("File name: " + Path.GetFileName(filePath),
                    new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft },
                    GUILayout.MaxWidth(1000), GUILayout.Height(25));

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Separator();
                EditorGUILayout.Separator();

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Upload", GUILayout.Height(30)))
                {

                    currentView = View.Loading;

                    var request = new CreateAssetRequest() { name = assetName, context_id = Contexts[Array.IndexOf(ContextNames, activeAsset.context)].id };

                    LootLockerSDKAdminManager.CreateAsset(request, (response) =>
                    {
                        if (response.success)
                        {

                            Debug.LogError("Asset created successfully. Uploading file..");

                            LootLockerSDKAdminManager.GetAssets((getAssetsResponse) =>
                            {
                                if (getAssetsResponse.success)
                                {
                                    Debug.Log("Successfully got uploaded asset: " + getAssetsResponse.text);

                                    Asset uploadedAsset = getAssetsResponse.assets[0];

                                    LootLockerSDKAdminManager.UploadAFile(filePath, uploadedAsset.id.ToString(), LootLockerAdminConfig.current.gameID, (uploadResponse) =>
                                    {
                                        if (uploadResponse.success)
                                        {
                                            Debug.Log("Successfully uploaded file: " + uploadResponse.text);
                                            PopulateFiles();
                                        }
                                        else
                                        {
                                            Debug.LogError("Failed to upload file: " + uploadResponse.Error);
                                            currentView = View.CreateFile;
                                        }
                                    }, tags: FileTags.Split(','));

                                }
                                else
                                {
                                    Debug.LogError("Failed to get assets: " + getAssetsResponse.Error);
                                    currentView = View.CreateFile;
                                }
                            });

                        }
                        else
                        {
                            Debug.LogError("failed to get create/update asset: " + response.Error);
                            currentView = View.CreateFile;
                        }
                    });


                }

            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            if (GUILayout.Button("Back", GUILayout.Height(20))) PopulateFiles();

            GUILayout.EndArea();
        }

        void CreateFile()
        {

            filePath = "";
            currentView = View.CreateFile;

            //LootLockerSDKAdminManager.GetAssets((response) =>
            //{
            //    if (response.success)
            //    {
            //        assetsResponse = response;
            //        filePath = "";
            //        currentView = View.CreateFile;
            //        Repaint();
            //        Debug.Log("Successful got all assets: " + response.text);

            //        AssetsNames = new string[assetsResponse.assets.Length];
            //        for (int i = 0; i < AssetsNames.Length; i++)
            //        {
            //            AssetsNames[i] = assetsResponse.assets[i].name;
            //        }
            //    }
            //    else
            //    {
            //        Debug.LogError("failed to get all assets: " + response.Error);
            //    }
            //});
        }

        void SelectFile(int index)
        {
            activeFile = getFilesResponse.files[index];
            currentView = View.File;

            FileTags = String.Empty;
            if (activeFile.tags == null) return;

            foreach (var item in activeFile.tags) FileTags += item + ',';
            FileTags.Remove(FileTags.Length - 1);
        }
    }
}