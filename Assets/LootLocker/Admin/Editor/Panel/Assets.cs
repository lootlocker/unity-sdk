using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LootLockerAdminRequests;
using ViewType;
using System;
using LootLocker;
using System.Linq;
using System.Threading.Tasks;

public partial class LootlockerAdminPanel : EditorWindow
{
    LootLockerAdminRequests.Context[] Contexts;
    string[] ContextNames;
    int AssetContextIndex;

    int AssetPage;
    string AssetSearch;

    void PopulateAssets(string search = null)
    {
        LootLockerSDKAdminManager.DebugMessage("Getting assets..");

        currentView = View.Loading;

        LootLockerSDKAdminManager.GetAssets((response) =>
        {
            LootLockerSDKAdminManager.GetContexts((contextResponse) =>
            {
                if (contextResponse.success)
                {
                    Contexts = contextResponse.Contexts;
                    ContextNames = Contexts.Select(x => x.name).ToArray();
                    LootLockerSDKAdminManager.DebugMessage("Successful got all contexts: " + contextResponse.text);
                }
                else
                {
                    LootLockerSDKAdminManager.DebugMessage("failed to get all contexts: " + contextResponse.Error, true);
                }

                if (response.success)
                {
                    assetsResponse = response;
                    currentView = View.Assets;
                    Repaint();
                    LootLockerSDKAdminManager.DebugMessage("Successful got all assets: " + response.text);
                }
                else
                {
                    LootLockerSDKAdminManager.DebugMessage("failed to get all assets: " + response.Error, true);
                }

            });
        }, search);
    }

    void DrawAssetsView()
    {
        style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

        GUILayout.BeginArea(ContentSection);

        EditorGUILayout.Space();
        if (GUILayout.Button("Create New Asset", GUILayout.Height(20)))
        {
            StartCreateAsset();
        }

        if (GUILayout.Button("Back", GUILayout.Height(20)))
        {
            currentView = View.Menu;
        }

        EditorGUILayout.Separator();

        GUILayout.BeginHorizontal();
        AssetSearch = EditorGUILayout.TextField(AssetSearch);
        if (GUILayout.Button("Search"))
        {
            PopulateAssets(AssetSearch);
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Separator();

        if (assetsResponse != null)
        {
            assetsViewScrollPos = EditorGUILayout.BeginScrollView(assetsViewScrollPos);
            var style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 20 };

            for (int i = 0; i < assetsResponse.assets.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(new GUIContent("  " + assetsResponse.assets[i].name, AssetTexture), style, GUILayout.Height(40)))
                    SelectAsset(i);

                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

        }
        GUILayout.EndArea();
    }

    bool UploadingToSelectedAsset, UploadSucceedForSelectAsset, UploadFailedForSelectAsset;

    void SelectAssetBase()
    {
        UploadingToSelectedAsset = false;
        UploadSucceedForSelectAsset = false;
        UploadFailedForSelectAsset = false;

        AssetContextIndex = Array.IndexOf(ContextNames, activeAsset.context);
    }

    void SelectAsset(int assetIndex)
    {
        Debug.Log($"Current Asset set to: {assetsResponse.assets[assetIndex].id} Named: {assetsResponse.assets[assetIndex].name}");

        activeAsset = assetsResponse.assets[assetIndex];
        currentView = View.UpdateAsset;
        SelectAssetBase();
    }

    void StartCreateAsset()
    {
        activeAsset = new Asset() { name = "untitleted", context = ContextNames[0] };
        currentView = View.CreateAsset;
        SelectAssetBase();
    }


    void DrawAssetView(bool create)
    {
        style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

        // assetsSection.x = 0;
        // assetsSection.y = 60;
        // assetsSection.width = Screen.width;
        // assetsSection.height = Screen.width - 100;
        // GUI.DrawTexture(assetsSection, defaultSectionTexture);

        GUILayout.BeginArea(ContentSection);

        if (activeAsset != null)
        {
            assetsViewScrollPos = EditorGUILayout.BeginScrollView(assetsViewScrollPos, GUILayout.ExpandHeight(true));

            #region BackButton and upload

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Back", GUILayout.Height(20)))
            {
                PopulateAssets();
            }

            EditorGUILayout.EndHorizontal();

            if (UploadingToSelectedAsset) GUI.backgroundColor = Color.blue;
            else if (UploadSucceedForSelectAsset) GUI.backgroundColor = Color.green;
            else if (UploadFailedForSelectAsset) GUI.backgroundColor = Color.red;

            if (!create)
                if (GUILayout.Button("Upload", GUILayout.Height(40)))
                {
                    var filePath = EditorUtility.OpenFilePanel("choose a file", "", "");
                    UploadingToSelectedAsset = true;//you can do this after call because the response can be in the same frame!
                    if (string.IsNullOrEmpty(filePath))
                    {
                        Debug.LogWarning("please select a valid file");
                        UploadingToSelectedAsset = false;
                    }
                    LootLockerSDKAdminManager.UploadAFile(filePath, activeAsset.id.ToString(), LootLockerAdminConfig.current.gameID, (response) =>
                    {
                        UploadingToSelectedAsset = false;
                        if (response.success)
                        {
                            UploadSucceedForSelectAsset = true;
                            Task.Delay(2000).ContinueWith(t => UploadSucceedForSelectAsset = false);
                            Debug.Log("Successful uploade file: " + response.text);
                        }
                        else
                        {
                            UploadFailedForSelectAsset = true;
                            Task.Delay(2000).ContinueWith(t => UploadFailedForSelectAsset = false);
                            Debug.LogError("failed to uploade file: " + response.Error);
                        }
                    });
                }

            GUI.backgroundColor = Color.white;
            #endregion

            #region Name

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name: ");
            // EditorGUILayout.LabelField(activeAsset.name.ToString());
            activeAsset.name = EditorGUILayout.TextField(activeAsset.name);
            EditorGUILayout.EndHorizontal();

            #endregion

            #region context

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("context: ");
            // EditorGUILayout.LabelField(activeAsset.context.ToString());
            AssetContextIndex = EditorGUILayout.Popup(AssetContextIndex, ContextNames);
            activeAsset.context = Contexts[AssetContextIndex].name;
            EditorGUILayout.EndHorizontal();

            #endregion

            #region ID

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID: ");
            EditorGUILayout.LabelField(activeAsset.id.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion

            #region Is Active

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Is Active: ");
            EditorGUILayout.LabelField(activeAsset.active.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion

            #region purchasable

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("purchasable: ");
            EditorGUILayout.LabelField(activeAsset.purchasable.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion

            #region price

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("price: ");
            EditorGUILayout.LabelField(activeAsset.price.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion
            // #region display_price

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("display_price: ");
            // EditorGUILayout.LabelField(activeAsset.display_price.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion

            // #region unlocks_context

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("unlocks_context: ");
            // EditorGUILayout.LabelField(activeAsset.unlocks_context.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            #region detachable

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("detachable: ");
            EditorGUILayout.LabelField(activeAsset.detachable.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion
            // #region updated

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("updated: ");
            // EditorGUILayout.LabelField(activeAsset.updated.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            // #region marked_new

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("marked_new: ");
            // EditorGUILayout.LabelField(activeAsset.marked_new.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            #region default_variation_id

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("default_variation_id: ");
            EditorGUILayout.LabelField(activeAsset.default_variation_id.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion
            // #region default_loadouts

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("default_loadouts: ");
            // EditorGUILayout.LabelField(activeAsset.default_loadouts.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion

            // #region description

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("description: ");
            // EditorGUILayout.LabelField(activeAsset.description.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            // #region links

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("links: ");
            // EditorGUILayout.LabelField(activeAsset.links.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            // #region storage

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("storage: ");
            // EditorGUILayout.LabelField(activeAsset.storage.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            // #region rarity

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("rarity: ");
            // EditorGUILayout.LabelField(activeAsset.rarity.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            #region popular

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("popular: ");
            EditorGUILayout.LabelField(activeAsset.popular.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion
            #region popularity_score

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("popularity_score: ");
            EditorGUILayout.LabelField(activeAsset.popularity_score.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion
            // #region package_contents

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("package_contents: ");
            // EditorGUILayout.LabelField(activeAsset.package_contents.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            #region unique_instance

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("unique_instance: ");
            EditorGUILayout.LabelField(activeAsset.unique_instance.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion
            // #region external_identifiers

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("external_identifiers: ");
            // EditorGUILayout.LabelField(activeAsset.external_identifiers.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            // #region rental_options

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("rental_options: ");
            // EditorGUILayout.LabelField(activeAsset.rental_options.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            // #region filters

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("filters: ");
            // EditorGUILayout.LabelField(activeAsset.filters.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            // #region variations

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("variations: ");
            // EditorGUILayout.LabelField(activeAsset.variations.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion
            #region featured

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("featured: ");
            EditorGUILayout.LabelField(activeAsset.featured.ToString());
            EditorGUILayout.EndHorizontal();

            #endregion
            // #region success

            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField("success: ");
            // EditorGUILayout.LabelField(activeAsset.s.ToString());
            // EditorGUILayout.EndHorizontal();

            // #endregion

            var msg = String.Empty;

            if (GUILayout.Button(create ? "Create" : "Update", GUILayout.Height(50)))
            {
                var request = new CreateAssetRequest() { name = activeAsset.name, context_id = Contexts[Array.IndexOf(ContextNames, activeAsset.context)].id };

                LootLockerSDKAdminManager.CreateAsset(request, (response) =>
                {
                    if (response.success)
                    {
                        msg = "asset created/updated successfully";
                        if (create) DrawAssetView(create: false);
                    }
                    else
                    {
                        Debug.LogError("failed to get create/update asset: " + response.Error);
                    }
                });
            }

            EditorGUILayout.LabelField(msg);

            EditorGUILayout.EndScrollView();
        }

        GUILayout.EndArea();
    }
}