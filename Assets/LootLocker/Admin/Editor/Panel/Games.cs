using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LootLocker.Admin.Requests;
using Lootlocker.Admin.LootLockerViewType;
using System;
using LootLocker;
using System.Linq;

namespace LootLocker.Admin
{
    public partial class LootlockerAdminPanel : EditorWindow
    {
        public void PopulateGames()
        {
            Repaint();
            Debug.Log("Getting games..");
            LootLockerSDKAdminManager.GetAllGamesToTheCurrentUser((response) =>
            {
                if (response.success)
                {
                    gamesResponse = response;
                    currentView = LootLockerView.Games;
                    Repaint();
                    Debug.Log("Successful got all games: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to get all games: " + response.Error);
                }
            });
        }

        void DrawGamesView()
        {

            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            GUIStyle selectGameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };

            GUIStyle createGameStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };

            GUIStyle ORStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };

            GUIStyleState gss = new GUIStyleState();
            gss.textColor = Color.red;

            GUIStyle deleteButtonStyle = new GUIStyle(GUI.skin.button) { normal = gss, fontSize = 15 };

            // gamesSection.x = 0;
            // gamesSection.y = 60;
            // gamesSection.width = Screen.width;
            // gamesSection.height = Screen.width - 50;
            // GUI.DrawTexture(gamesSection, defaultSectionTexture);

            GUILayout.BeginArea(ContentSection);

            //TODO: Uncomment if you want to activate/deactivate 2FA from editor.
            //EditorGUILayout.BeginHorizontal();

            //if (!mfaState)
            //{ 
            //    if (GUILayout.Button("Activate 2FA", createGameStyle, GUILayout.Height(30)))
            //        AdminSetup2FA();
            //}
            //else
            //{
            //    if (GUILayout.Button("Remove 2FA", createGameStyle, GUILayout.Height(30)))
            //        AdminRemove2FA();
            //}

            //EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            //if (GUILayout.Button("Create Game", createGameStyle, GUILayout.Height(40)))
            //{
            //    currentView = View.CreateGame;
            //}
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("OR", ORStyle, GUILayout.ExpandHeight(false));
            //EditorGUILayout.EndHorizontal();
            //EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select Game", selectGameStyle, GUILayout.ExpandHeight(false));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            if (gamesResponse != null)
            {

                gamesViewScrollPos = EditorGUILayout.BeginScrollView(gamesViewScrollPos, GUILayout.ExpandHeight(false));

                for (int i = 0; i < gamesResponse.games.Length; i++)
                {
                    //TODO: Change this later to actual game texture
                    button_tex = Resources.Load("Icons/icon") as Texture2D;
                    button_tex_con = new GUIContent("  " + gamesResponse.games[i].name, GameTexture, "View and modify " + gamesResponse.games[i].name + "'s maps and missions");

                    EditorGUILayout.BeginHorizontal();

                    var style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 20 };

                    if (GUILayout.Button(button_tex_con, style, GUILayout.Height(70), GUILayout.ExpandWidth(true)))
                    {
                        SetCurrentGame(gamesResponse.games[i].id);
                    }

                    //if (GUILayout.Button("Delete", deleteButtonStyle, GUILayout.Height(70), GUILayout.Width(100)))
                    //{
                    //    SetCurrentGame(gamesResponse.games[i].id, true);
                    //}

                    EditorGUILayout.EndHorizontal();

                }

                EditorGUILayout.EndScrollView();

            }

            if (GUILayout.Button("Back", GUILayout.Height(40)))
            {
                currentView = LootLockerView.Login;
            }

            GUILayout.EndArea();

        }

        void DrawDeleteGameConfirmationView()
        {

            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            GUIStyle deleteGameWordStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
            GUIStyle deleteGameConfirmationStyle = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleCenter, fontSize = 25 };

            // gamesSection.x = 0;
            // gamesSection.y = 60;
            // gamesSection.width = Screen.width;
            // gamesSection.height = Screen.height - 100;
            // GUI.DrawTexture(gamesSection, defaultSectionTexture);

            GUILayout.BeginArea(ContentSection);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Delete Game", deleteGameWordStyle, GUILayout.Height(20));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Are you sure you want to delete " + gamesResponse.games.ToList().Find(g => g.id == activeGameID).name + "?", deleteGameConfirmationStyle, GUILayout.Height(50));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            EditorGUILayout.Separator();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Yes", GUILayout.Height(40)))
            {
                DeleteGame();
                currentView = LootLockerView.Loading;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("No", GUILayout.Height(40)))
            {
                currentView = LootLockerView.Games;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

        }

        void DrawCreateGameView()
        {

            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            GUIStyle createGameWordStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
            GUIStyle propertyNameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            GUIStyle propertyValueStyle = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };

            // gamesSection.x = 0;
            // gamesSection.y = 60;
            // gamesSection.width = Screen.width;
            // gamesSection.height = Screen.height - 100;
            // GUI.DrawTexture(gamesSection, defaultSectionTexture);

            GUILayout.BeginArea(ContentSection);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Create Game", createGameWordStyle, GUILayout.Height(20));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Game name", propertyNameStyle, GUILayout.Height(20));
            CreateGame_gameName = EditorGUILayout.TextField(CreateGame_gameName, propertyValueStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sandbox mode", propertyNameStyle, GUILayout.Height(20));
            CreateGame_sandboxMode = EditorGUILayout.Toggle(CreateGame_sandboxMode);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Organization ID", propertyNameStyle, GUILayout.Height(20));
            int selectedOrganizationID = 0;
            //EditorGUI.BeginChangeCheck();

            selectedOrganizationID = EditorGUILayout.Popup(selectedOrganizationID, organizationIDs.ToArray());

            //if (EditorGUI.EndChangeCheck())
            //{
            //    int.TryParse(organizationIDs[selectedOrganizationID], out CreateGame_organisationID);
            //}

            int.TryParse(organizationIDs[selectedOrganizationID], out CreateGame_organisationID);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Steam App ID", propertyNameStyle, GUILayout.Height(20));
            CreateGame_steamAppID = EditorGUILayout.IntField(CreateGame_steamAppID, propertyValueStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            EditorGUILayout.Separator();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create", GUILayout.Height(40)))
            {
                LootLockerCreatingAGameRequest cagr = new LootLockerCreatingAGameRequest
                {
                    name = CreateGame_gameName,
                    sandbox_mode = CreateGame_sandboxMode,
                    organisation_id = CreateGame_organisationID,
                    steam_app_id = CreateGame_steamAppID.ToString()
                };
                CreateGame(cagr);
                currentView = LootLockerView.Loading;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Back", GUILayout.Height(40)))
            {
                PopulateGames();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

        }

        public void CreateGame(LootLockerCreatingAGameRequest gameToCreate)
        {

            LootLockerSDKAdminManager.CreatingAGame(gameToCreate.name, gameToCreate.steam_app_id, gameToCreate.sandbox_mode, gameToCreate.organisation_id, gameToCreate.demo, (response) =>
            {
                if (response.success)
                {
                    Debug.Log("Successful created game: " + response.text);
                    PopulateGames();
                }
                else
                {
                    Debug.LogError("failed to create game: " + response.Error);
                    currentView = LootLockerView.CreateGame;
                }
            });
        }

        public void DeleteGame()
        {
            LootLockerSDKAdminManager.DeletingGames(activeGameID, (response) =>
            {
                if (response.success)
                {
                    Debug.Log("Successful deleted a game: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to delete a game: " + response.Error);
                }

                PopulateGames();

            });
        }

        private void SetCurrentGame(int id, bool setToDelete = false)
        {
            Debug.Log("Current Game set to: " + id);
            activeGameID = id;
            if (!setToDelete)
            {
                currentView = LootLockerView.Loading;
                LootLockerSDKAdminManager.GetDetailedInformationAboutAGame(id.ToString(), (response) =>
                 {
                     if (response.success)
                     {
                         LootLockerAdminConfig.current.gameName = response.game.development.name;
                         LootLockerAdminConfig.current.apiKey = response.game.development.game_key;
                         LootLockerAdminConfig.current.gameID = response.game.development.id;
                         LootLockerAdminConfig.current.EditorSave();

                     //normal setup
                     if (LootLockerConfig.current != null)
                         {
                             LootLockerConfig.current.apiKey = response.game.game_key;
                             LootLockerConfig.current.gameID = response.game.id;
                             LootLockerConfig.current.EditorSave();
                         }
                         currentView = LootLockerView.Menu;
                     }
                     else
                     {
                         Debug.LogError("Could not get game details");
                     }

                 });
            }
            else
            {
                currentView = LootLockerView.DeleteGameConfirmation;
            }
        }

    }
}
