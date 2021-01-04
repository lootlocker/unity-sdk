using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LootLockerAdminRequests;
using ViewType;
using System;
using LootLocker;
using System.Linq;

namespace LootLockerAdmin
{
    public partial class LootlockerAdminPanel : EditorWindow
    {

        public void PopulateMaps()
        {
            Repaint();
            Debug.Log("Getting maps..");
            mapsResponse = null;
            currentView = View.Loading;
            LootLockerSDKAdminManager.GettingAllMapsToAGame(activeGameID, (response) =>
            {
                if (response.success)
                {
                    mapsResponse = response;
                    currentView = View.Maps;
                    Repaint();
                    Debug.Log("Successful got all maps: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to get all maps: " + response.Error);
                }
            });
        }

        private void SelectMap(int mapID)
        {
            Debug.Log("Current Map set to: " + mapID);
            activeMap = mapsResponse.maps.ToList().Find(m => m.map_id == mapID);
            currentView = View.UpdateMap;
        }

        void DrawMapsView()
        {

            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            // mapsSection.x = 0;
            // mapsSection.y = 60;
            // mapsSection.width = Screen.width;
            // mapsSection.height = Screen.height - 100;

            // GUI.DrawTexture(mapsSection, defaultSectionTexture);

            GUILayout.BeginArea(ContentSection);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New Map", GUILayout.Height(20)))
            {
                activeMap = new Map();
                activeMap.spawn_points = new Spawnpoint[0];
                currentView = View.CreateMap;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Back", GUILayout.Height(20)))
            {
                currentView = View.Menu;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Maps", style);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            if (mapsResponse != null)
            {

                assetsViewScrollPos = EditorGUILayout.BeginScrollView(assetsViewScrollPos);

                var style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 20 };

                for (int i = 0; i < mapsResponse.maps.Length; i++)
                    if (GUILayout.Button(new GUIContent("Map #" + mapsResponse.maps[i].map_id.ToString(), MapTexture), style, GUILayout.Height(40)))
                        SelectMap(mapsResponse.maps[i].map_id);

                EditorGUILayout.EndScrollView();

            }


            GUILayout.EndArea();

        }

        void DrawMapView()
        {

            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            // mapsSection.x = 0;
            // mapsSection.y = 60;
            // mapsSection.width = Screen.width;
            // mapsSection.height = Screen.height - 100;
            // GUI.DrawTexture(mapsSection, defaultSectionTexture);

            GUILayout.BeginArea(ContentSection);

            string topButtonText = "";

            switch (activeMapMode)
            {

                case MapMode.Create:

                    topButtonText = "Create Map";

                    break;

                case MapMode.Update:

                    topButtonText = "Update Map";

                    break;

                default:

                    break;

            }

            if (activeMap != null)
            {

                #region BackButton

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Back", GUILayout.Height(20)))
                {
                    PopulateMaps();
                }

                EditorGUILayout.EndHorizontal();

                #endregion

                #region TopButton

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(topButtonText, GUILayout.Height(30)))
                {
                    CreatingMapsRequest map = new CreatingMapsRequest();
                    map.asset_id = activeMap.asset_id;
                    map.game_id = activeGameID;
                    map.name = mapName;
                    map.spawn_points = activeMap.spawn_points;

                    switch (activeMapMode)
                    {

                        case MapMode.Update:

                            UpdateMap(map);

                            break;

                        case MapMode.Create:

                            CreateMap(map, CreateMap_includeAssetID, CreateMap_includeSpawnPoints);

                            break;

                    }

                }

                EditorGUILayout.EndHorizontal();

                #endregion

                if (activeMapMode == MapMode.Create)
                {
                    EditorGUILayout.BeginHorizontal();
                    CreateMap_includeAssetID = EditorGUILayout.Toggle("Send Asset ID", CreateMap_includeAssetID);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    CreateMap_includeSpawnPoints = EditorGUILayout.Toggle("Send Spawn Points", CreateMap_includeSpawnPoints);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Separator();
                EditorGUILayout.Separator();

                #region MapID

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Map ID: ");
                EditorGUILayout.LabelField(activeMap.map_id.ToString());
                EditorGUILayout.EndHorizontal();

                #endregion

                #region MapName

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Map Name: ");
                mapName = EditorGUILayout.TextField(mapName);

                EditorGUILayout.EndHorizontal();

                #endregion

                #region AssetID

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Asset ID: ");
                int.TryParse(EditorGUILayout.TextField(activeMap.asset_id.ToString()), out activeMap.asset_id);

                EditorGUILayout.EndHorizontal();

                #endregion

                EditorGUILayout.Space();
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();

                #region SpawnPoints

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Spawn Points: " + (activeMap.spawn_points == null ? "0" : activeMap.spawn_points.Length.ToString()), style);
                EditorGUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Create new Spawn Point", GUILayout.Height(20)))
                {
                    Spawnpoint spawnpoint = new Spawnpoint();
                    spawnpoint.asset_id = 0;
                    spawnpoint.id = 0;
                    spawnpoint.position = "";
                    spawnpoint.rotation = "";
                    List<Spawnpoint> sps = activeMap.spawn_points.ToList();
                    sps.Add(spawnpoint);
                    activeMap.spawn_points = sps.ToArray();
                }

                GUILayout.EndHorizontal();


                var fieldStyle = new GUIStyle(GUI.skin.box) { };

                if (activeMap.spawn_points != null)
                {

                    if (activeMap.spawn_points.Length > 0)
                    {

                        spawnPointsScrollPos = EditorGUILayout.BeginScrollView(spawnPointsScrollPos, false, true, GUILayout.Height(200));

                        foreach (Spawnpoint sp in activeMap.spawn_points)
                        {

                            if (sp == null)
                                continue;

                            EditorGUILayout.LabelField("Spawn point ID: " + sp.id == null ? "" : sp.id.ToString(), style, GUILayout.ExpandWidth(true));
                            EditorGUILayout.Separator();

                            EditorGUILayout.BeginHorizontal();

                            EditorGUILayout.LabelField("Name: ");
                            sp.name = EditorGUILayout.TextField(sp.name ?? "");

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();

                            EditorGUILayout.LabelField("GUID: ");
                            sp.guid = EditorGUILayout.TextField(sp.guid ?? "");

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();

                            EditorGUILayout.LabelField("Asset ID: ");
                            int.TryParse(EditorGUILayout.TextField(sp.asset_id.ToString()), out sp.asset_id);

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();

                            EditorGUILayout.LabelField("Position: ");
                            sp.position = EditorGUILayout.TextField(sp.position ?? "");

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();

                            EditorGUILayout.LabelField("Rotation: ");
                            sp.rotation = EditorGUILayout.TextField(sp.rotation ?? "");

                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.Separator();

                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Cameras: " + (sp.cameras == null ? "0" : sp.cameras.Length.ToString()), style);
                            EditorGUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();

                            if (GUILayout.Button("Create new camera", GUILayout.Height(20)))
                            {
                                AdminCamera cam = new AdminCamera();
                                cam.position = "";
                                cam.rotation = "";

                                if (sp.cameras == null)
                                    sp.cameras = new AdminCamera[0];

                                List<AdminCamera> acs = sp.cameras.ToList();
                                acs.Add(cam);
                                sp.cameras = acs.ToArray();
                            }

                            GUILayout.EndHorizontal();

                            if (sp.cameras != null)
                            {

                                if (sp.cameras.Length > 0)
                                {

                                    EditorGUILayout.Separator();
                                    EditorGUILayout.Separator();

                                    spawnPointCamerasScrollPos = EditorGUILayout.BeginScrollView(spawnPointCamerasScrollPos, false, true, GUILayout.Height(200));

                                    for (int j = 0; j < sp.cameras.Length; j++)
                                    {

                                        EditorGUILayout.LabelField("Camera " + j.ToString(), style);
                                        EditorGUILayout.Separator();

                                        EditorGUILayout.BeginHorizontal();

                                        EditorGUILayout.LabelField("Position: ");
                                        sp.cameras[j].position = EditorGUILayout.TextField(sp.cameras[j].position);

                                        EditorGUILayout.EndHorizontal();

                                        EditorGUILayout.BeginHorizontal();

                                        EditorGUILayout.LabelField("Rotation: ");
                                        sp.cameras[j].rotation = EditorGUILayout.TextField(sp.cameras[j].rotation);

                                        EditorGUILayout.EndHorizontal();

                                        EditorGUILayout.Separator();
                                        EditorGUILayout.LabelField("______________________________________________________", style);

                                    }

                                    EditorGUILayout.EndScrollView();

                                }

                            }

                            EditorGUILayout.Separator();

                            if (GUILayout.Button("Delete Spawnpoint"))
                            {
                                List<Spawnpoint> sps = activeMap.spawn_points.ToList();
                                sps.Remove(sp);
                                activeMap.spawn_points = sps.ToArray();
                            }

                            EditorGUILayout.Separator();
                            EditorGUILayout.LabelField("______________________________________________________", style);

                        }

                        EditorGUILayout.EndScrollView();

                    }


                }

                #endregion

            }

            GUILayout.EndArea();

        }

        public void UpdateMap(CreatingMapsRequest updatedMap)
        {

            LootLockerSDKAdminManager.UpdatingMaps(updatedMap, activeMap.map_id, (response) =>
            {
                if (response.success)
                {
                    Debug.Log("Successful updated map: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to update map: " + response.Error);
                }
            });
        }

        public void CreateMap(CreatingMapsRequest mapToCreate, bool includeAssetID, bool includeSpawnPoints)
        {

            LootLockerSDKAdminManager.CreatingMaps(mapToCreate, includeAssetID, includeSpawnPoints, (response) =>
            {
                if (response.success)
                {
                    Debug.Log("Successful created map: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to create map: " + response.Error);
                }
            });
        }

    }
}