using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LootLocker.Admin.Requests;
using Lootlocker.Admin.LootLockerViewType;
using System;
using LootLocker;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using System.Threading.Tasks;

namespace LootLocker.Admin
{
    public partial class LootlockerAdminPanel : EditorWindow
    {
        /// <summary>
        /// the default value is "Login" because it's abstract struct
        /// </summary>
        View currentView;

        Texture2D
        headerSectionTexture,
        defaultSectionTexture,
        iconTexture,
        tfaTexture;

        Rect
        HeaderSection,
        ContentSection,
        ButtomSection;

        Texture2D
        MapTexture,
        AssetTexture,
        FileTexture,
        MissionTexture,
        GameTexture;

        Vector2
        spawnPointsScrollPos = Vector2.zero,
        spawnPointCamerasScrollPos = Vector2.zero,
        assetsViewScrollPos = Vector2.zero,
        filesViewScrollPos = Vector2.zero,
        gamesViewScrollPos = Vector2.zero;

        string ButtomMessage;

        List<Texture2D> loadingTextures;

        Color headerSectionColor = new Color(100f / 255f, 100f / 255f, 100f / 255f, 1f);

        string email;
        string password;

        int verify2FASecret, remove2FASecret;
        bool mfaState;
        string verify2FARecovery;

        static LootLockerGetAllGamesToTheCurrentUserResponse gamesResponse;
        static LootLockerGettingAllMapsToAGameResponse mapsResponse;
        static LootLockerGetAssetsResponse assetsResponse;
        static LootLockerGetFilesResponse getFilesResponse;

        public static Action repaint;

        private int activeGameID;
        private LootLockerMap activeMap;
        private LootLockerCommonAsset activeAsset;
        string CreateGame_gameName;
        bool CreateMap_includeAssetID, CreateMap_includeSpawnPoints, CreateGame_sandboxMode;
        private string mapName;
        int CreateGame_organisationID, CreateGame_steamAppID;

        List<string> organizationIDs = new List<string>();

        enum MapMode { Create, Update };
        MapMode activeMapMode = MapMode.Update;

        GUIStyle style;

        [MenuItem("Window/Open Lootlocker AdminPanel")]
        static void OpenWindow()
        {
            LootlockerAdminPanel lootlockerAdminPanel = (LootlockerAdminPanel)GetWindow(typeof(LootlockerAdminPanel));
            lootlockerAdminPanel.minSize = new Vector2(500, 400);
            lootlockerAdminPanel.Show();
        }

        /// <summary>
        /// called in 2 cases: first time to open and edting the code
        /// </summary>
        private void OnEnable()
        {
            LootLockerSDKAdminManager.Init();

            InitTexture();
            InitSections();

            email = LootLockerAdminConfig.current.email;
            password = LootLockerAdminConfig.current.password;
            //these will be exposed to UI

            if (currentView == View.Login && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                AdminLogin();
            }
        }

        private void OnGUI()
        {
            DrawSections();
            DrawHeader();
            DrawLayouts();
            DrawButtomView();
        }

        private void OnInspectorUpdate()
        {

            if (currentView == View.Loading)
            {

                if (++curLoadingTextureIndex >= loadingTextures.Count)
                    curLoadingTextureIndex = 0;

                activeLoadingTexture = loadingTextures[curLoadingTextureIndex];

                Repaint();

            }

        }

        void InitTexture()
        {
            loadingTextures = new List<Texture2D>();
            for (int i = 1; i < 16; i++)
                loadingTextures.Add(Resources.Load("Icons/LoadingSprites/L" + i.ToString()) as Texture2D);

            headerSectionTexture = new Texture2D(1, 1);
            headerSectionTexture.SetPixel(0, 0, headerSectionColor);
            headerSectionTexture.Apply();
            defaultSectionTexture = Resources.Load("Backgrounds/bgTransparent") as Texture2D;

            lLTexture = Resources.Load("Icons/icon") as Texture2D;
            MissionTexture = Resources.Load("Icons/mission") as Texture2D;
            MapTexture = Resources.Load("Icons/map") as Texture2D;
            AssetTexture = Resources.Load("Icons/asset") as Texture2D;
            FileTexture = Resources.Load("Icons/file") as Texture2D;
            GameTexture = Resources.Load("Icons/game") as Texture2D;
        }

        Vector2 HeaderPadding = new Vector2(0, 0);
        Vector2 ContentSectionPadding = new Vector2(20, 60);
        /// <summary>
        /// adjuest who next to whom
        /// </summary>
        void InitSections()
        {
            HeaderSection = new Rect(position: HeaderPadding / 2, size: Vector2.zero);
            ContentSection = new Rect(position: new Vector2(0, 20) + ContentSectionPadding / 2, size: Vector2.zero);
        }

        void DrawSections()
        {
            ContentSection.size = new Vector2(Screen.width, Screen.height - 40) - ContentSectionPadding;
            HeaderSection.size = new Vector2(Screen.width, 30) - HeaderPadding;
            ButtomSection = new Rect(position: Vector2.up * (ContentSection.position.y + ContentSection.size.y), size: new Vector2(Screen.width, 50));

            GUI.DrawTexture(ContentSection, defaultSectionTexture);
            GUI.DrawTexture(HeaderSection, headerSectionTexture);
        }

        void DrawLayouts()
        {
            switch (currentView)
            {
                case View.Login:
                    DrawLoginView();
                    break;
                case View.Games:
                    DrawGamesView();
                    break;
                //case View.Maps:
                //    DrawMapsView();
                //    break;
                case View.Assets:
                    DrawAssetsView();
                    break;
                case View.Menu:
                    DrawMenuView();
                    break;
                //case View.Missions:
                //    DrawMissionsView();
                //    break;
                case View.Loading:
                    DrawLoading();
                    break;
                //case View.UpdateMap:
                //    DrawMapView();
                //    activeMapMode = MapMode.Update;
                //    break;
                case View.UpdateAsset:
                    DrawAssetView(create: false);
                    break;
                //case View.CreateMap:
                //    DrawMapView();
                //    activeMapMode = MapMode.Create;
                //    break;
                case View.CreateGame:
                    DrawCreateGameView();
                    break;
                case View.DeleteGameConfirmation:
                    DrawDeleteGameConfirmationView();
                    break;
                case View.Files:
                    DrawFilesView();
                    break;
                case View.CreateAsset:
                    DrawAssetView(create: true);
                    break;
                case View.File:
                    DrawFileView();
                    break;
                case View.CreateFile:
                    DrawCreateFileView();
                    break;
                case View.TwoFactorAuth:
                    DrawTwoFactorAuthView();
                    break;

                case View.VerifyTwoFactorAuth:
                    DrawVerifyTwoFactorAuthView();
                    break;

                case View.VerifySuccess:
                    DrawVerifySuccessView();
                    break;

                case View.Remove2FAConfirm:
                    DrawRemoveTwoFactorAuthView();
                    break;

            }
        }
        void DrawHeader()
        {
            GUILayout.BeginArea(HeaderSection);
            GUILayout.BeginHorizontal();
            iconTexture = Resources.Load("Icons/icon") as Texture2D;
            GUIContent icon = new GUIContent(iconTexture);

            GUIStyleState gss = new GUIStyleState();
            gss.textColor = Color.white;
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { normal = gss, alignment = TextAnchor.MiddleLeft, fontSize = 20, fontStyle = FontStyle.Bold };
            GUIStyle logoutStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
            GUILayout.Label(iconTexture, GUILayout.Height(30), GUILayout.Width(30));
            EditorGUILayout.LabelField("Lootlocker Admin", labelStyle, GUILayout.Height(30));
            if (GUILayout.Button("Log out", logoutStyle, GUILayout.Height(30)))
            {
                ResetToLogin();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawLoginView()
        {
            var style = new GUIStyle(GUI.skin.textField) { fontSize = 17, alignment = TextAnchor.MiddleLeft };
            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleLeft };

            GUILayout.BeginArea(ContentSection);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Email", labelStyle, GUILayout.Width(100));
            email = EditorGUILayout.TextField(email, style, GUILayout.Height(25));
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Password", labelStyle, GUILayout.Width(100));
            password = EditorGUILayout.PasswordField(password, style, GUILayout.Height(25));
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Login", GUILayout.Height(40)))
                AdminLogin();

            // if (GUILayout.Button("tst", GUILayout.Height(30)))
            //     EditorCoroutineUtility.StartCoroutineOwnerless(tst());

            // if (GUILayout.Button("tstFlag", GUILayout.Height(30)))
            //     tstFlag = true;

            GUILayout.EndArea();

        }
        void DrawMenuView()
        {
            // menuSection.x = 0;
            // menuSection.y = 60;
            // menuSection.width = Screen.width;
            // menuSection.height = Screen.height - 100;
            // GUI.DrawTexture(menuSection, defaultSectionTexture);
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            GUIStyle selectGameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };

            GUILayout.BeginArea(ContentSection);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Your game has been set, \n You can close this window", selectGameStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndHorizontal();
            //var style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 20 };

            ////if (GUILayout.Button(new GUIContent("  Maps", MapTexture), style, GUILayout.Height(50)))
            ////{
            ////    currentView = View.Maps;
            ////    PopulateMaps();
            ////}

            ////if (GUILayout.Button(new GUIContent("  Missions", MissionTexture), style, GUILayout.Height(50)))
            ////{
            ////    currentView = View.Missions;
            ////}

            //if (GUILayout.Button(new GUIContent("  Assets", AssetTexture), style, GUILayout.Height(50)))
            //{
            //    currentView = View.Assets;
            //    PopulateAssets();
            //}

            //if (GUILayout.Button(new GUIContent("  Files", FileTexture), style, GUILayout.Height(50)))
            //{
            //    PopulateFiles();
            //}

            EditorGUILayout.Space();
            if (GUILayout.Button("Back", GUILayout.Height(30), GUILayout.ExpandWidth(false)))
            {
                PopulateGames();
            }
            GUILayout.EndArea();

        }

        void DrawButtomView()
        {
            GUILayout.BeginArea(ButtomSection);
            EditorGUILayout.LabelField(ButtomMessage);
            GUILayout.EndArea();
        }

        private Texture button_tex;
        Texture lLTexture;
        private GUIContent button_tex_con;

        int curLoadingTextureIndex = 0;
        Texture2D activeLoadingTexture;
        void DrawLoading()
        {

            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            // loadingSection.x = 0;
            // loadingSection.y = Screen.height / 2 - 100;
            // loadingSection.width = Screen.width;
            // loadingSection.height = Screen.height;

            // GUI.DrawTexture(loadingSection, defaultSectionTexture);

            GUILayout.BeginArea(ContentSection);

            EditorGUILayout.LabelField("Loading", style);
            GUI.DrawTexture(new Rect((Screen.width / 2) - 50, 50, 100, 100), activeLoadingTexture);

            GUILayout.EndArea();

        }

        public void AdminRemove2FA()
        {

            currentView = View.Remove2FAConfirm;

        }

        public void AdminSetup2FA()
        {

            currentView = View.Loading;

            LootLockerSDKAdminManager.SetupTwoFactorAuthentication((response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful setup two factor authentication: " + response.text);
                    tfaTexture = new Texture2D(200, 200);
                    tfaTexture.LoadImage(Convert.FromBase64String(response.mfa_token_url.Substring(22)));
                    currentView = View.VerifyTwoFactorAuth;
                }
                else
                {
                    Debug.LogError("failed to set two factor authentication: " + response.Error);
                    ResetToLogin();
                }
            });

        }

        public void AdminLogin()
        {
            currentView = View.Loading;

            LootLockerSDKAdminManager.InitialAuthRequest(email, password, (response) =>
            {
                if (response.success)
                {
                    Debug.Log("Successful got admin auth response: " + response.text);
                    if (response.mfa_key != null)
                    {
                        mfaState = true;
                        Debug.Log("the admin enabled 2fa");
                        StartTwoFA(response.mfa_key);
                    }
                    else
                    {
                        mfaState = false;
                        Debug.Log("the admin didn't enable 2fa");
                        FinalAuth(response);
                    }
                }
                else
                {
                    ResetToLogin();
                    Debug.LogError("failed to get admin auth response: " + response.Error);
                }
            });

        }

        string MFAKey;
        string SecretCode;
        EditorCoroutine TwoFATockenTimoutCo;

        void StartTwoFA(string mFAKey)
        {
            MFAKey = mFAKey;
            SecretCode = String.Empty;
            currentView = View.TwoFactorAuth;

            TwoFATockenTimoutCo = EditorCoroutineUtility.StartCoroutineOwnerless(TwoFATockenTimout());
        }

        /// <summary>
        /// reset to login if user didn't login in 4 minutes
        /// </summary>
        IEnumerator TwoFATockenTimout()
        {
            yield return new EditorWaitForSeconds(4 * 60);
            ResetToLogin();
        }

        void DrawVerifyTwoFactorAuthView()
        {

            GUILayout.BeginArea(ContentSection);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            GUI.DrawTexture(new Rect((Screen.width / 2) - 100, 100, 200, 200), tfaTexture);

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Back", GUILayout.Height(20))) currentView = View.Games;

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Please scan the QR code on your Google Authenticator then enter your secret code");
            //EditorGUILayout.LabelField("You can obtain it from google authenticator", new GUIStyle(GUI.skin.label) { fontSize = 10 });

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            verify2FASecret = EditorGUILayout.IntField(verify2FASecret);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Verify", GUILayout.Height(20)))
            {

                currentView = View.Loading;

                LootLockerSDKAdminManager.VerifyTwoFactorAuthenticationSetup(verify2FASecret, (response) =>
                {
                    if (response.success)
                    {
                        Debug.LogError("Successfully verified two factor authentication setup: " + response.text);
                        verify2FARecovery = response.recover_token;
                        currentView = View.VerifySuccess;
                    }
                    else
                    {
                        Debug.LogError("failed to set two factor authentication: " + response.Error);
                        currentView = View.Games;
                    }
                });

            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.EndArea();

        }

        void DrawVerifySuccessView()
        {

            GUILayout.BeginArea(ContentSection);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Two-Factor Authentication enabled successfully. Please save the following recovery token: ");

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(verify2FARecovery, new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold });

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Back", GUILayout.Height(20))) currentView = View.Login;

            EditorGUILayout.EndVertical();

            GUILayout.EndArea();

        }

        void DrawRemoveTwoFactorAuthView()
        {

            GUILayout.BeginArea(ContentSection);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("To remove Two-Factor Authentication, Please enter a secret key from your Google Authenticator and click Remove");

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            remove2FASecret = EditorGUILayout.IntField(remove2FASecret);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Remove", GUILayout.Height(20)))
            {

                currentView = View.Loading;
                LootLockerSDKAdminManager.RemoveTwoFactorAuthentication(remove2FASecret, (response) =>
                {
                    if (response.success)
                    {
                        Debug.Log("Successful removed 2fa: " + response.text);
                        mfaState = false;
                        currentView = View.Games;
                    }
                    else
                    {
                        currentView = View.Remove2FAConfirm;
                        Debug.LogError("failed to get admin auth response: " + response.Error);
                    }
                });

            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Back", GUILayout.Height(20))) currentView = View.Login;

            EditorGUILayout.EndVertical();

            GUILayout.EndArea();

        }


        void DrawTwoFactorAuthView()
        {
            GUILayout.BeginArea(ContentSection);

            EditorGUILayout.LabelField("Please Enter Your Secret Code");
            EditorGUILayout.LabelField("You can obtain it from google authenticator", new GUIStyle(GUI.skin.label) { fontSize = 10 });

            SecretCode = EditorGUILayout.TextField(SecretCode);

            if (GUILayout.Button("Submit"))
            {
                LootLockerSDKAdminManager.TwoFactorAuthVerification(MFAKey, SecretCode, (response) =>
                {
                    if (response.success)
                    {
                        Debug.Log("Successful 2FA: " + response.text);
                        FinalAuth(response);
                    }
                    else
                    {
                        ResetToLogin();
                        Debug.LogError("failed to get admin auth response: " + response.Error);
                    }
                });
            }

            if (GUILayout.Button("Back", GUILayout.Height(20))) currentView = View.Login;

            GUILayout.EndArea();
        }

        void FinalAuth(LootLockerAuthResponse response)
        {
            if (TwoFATockenTimoutCo != null)
            {
                EditorCoroutineUtility.StopCoroutine(TwoFATockenTimoutCo);
            }

            PopulateGames();

            //Fill in organization IDs
            for (int i = 0; i < response.user.organisations.Length; i++)
                organizationIDs.Add(response.user.organisations[i].id.ToString());
        }

        void ResetToLogin()
        {
            currentView = View.Login;
        }

    }


}

namespace Lootlocker.Admin.LootLockerViewType
{
    public enum View
    {
        Login, Menu, Games, Maps, Missions, Loading, UpdateMap, CreateMap, CreateGame,
        DeleteGameConfirmation, Assets, UpdateAsset, Files, CreateAsset, File, CreateFile,
        TwoFactorAuth, VerifyTwoFactorAuth, VerifySuccess, Remove2FAConfirm
    }
}