using System;
using System.Collections.Generic;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LootLocker.Extension.Responses;

namespace LootLocker.Extension
{
    public class LootLockerAdminExtension : EditorWindow
    {
        // State Variables
        bool isStage = true;
        bool isLoadingKeys = false;
        Dictionary<int, LootLocker.Extension.DataTypes.Game> gameData = new Dictionary<int, LootLocker.Extension.DataTypes.Game>();
        private Label gameName;
        DateTime gameDataRefreshTime;
        readonly int gameDataCacheExpirationTimeMinutes = 1;
        private VisualElement currentFlow = null;

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        [Header("Colors")]
        StyleColor stage;
        StyleColor live;
        StyleColor defaultButton;


        [Header("Environment")]
        private VisualElement environmentBackground, environmentHandle;
        private VisualElement environmentElement;
        private Label environmentTitle;

        [Header("Flows")]
        private VisualElement activeFlow;
        private VisualElement loginFlow, mfaFlow, gameSelectorFlow, apiKeyFlow;

        [Header("Menu")]
        private VisualElement menu;
        private Button menuAPIKeyBtn;
        private Button menuChangeGameBtn;
        private Button menuLogoutBtn;

        [Header("Popup")]
        private VisualElement popup;
        private Label popupTitle, popupMessage;
        private Button popupBtn;

        [Header("API Key")]
        private Label newApiKeyCancel;
        private VisualElement newApiKeyWindow;
        private TextField newApiKeyName;
        private VisualElement createApiKeyWindow;
        private Button createNewApiKeyBtn;
        private Button createApiKeyBtn;
        private VisualElement apiKeyList;

        [Header("Login UI")]
        private TextField emailField, passwordField;
        private Label signupLink, gettingStartedLink, forgotPasswordLink;
        private Button loginBtn;

        [Header("MFA UI")]
        private TextField codeField;
        private Button signInBtn;
        private string mfaKey;

        [Header("Game Selector UI")]
        VisualElement gameSelectorList;

        [Header("Loading Icon")]
        VisualElement loadingPage;
        VisualElement loadingIcon;
        int rotateIndex = 0;

        [MenuItem("Window/LootLocker")]
        public static void Run()
        {
            LootLockerAdminExtension wnd = GetWindow<LootLockerAdminExtension>();
            wnd.titleContent = new GUIContent("LootLocker");

            if (!string.IsNullOrEmpty(LootLockerEditorData.GetAdminToken()))
            {
                LootLockerConfig.current.adminToken = LootLockerEditorData.GetAdminToken();
                wnd.RefreshUserInformation(() =>
                {
                    if (LootLockerEditorData.GetSelectedGame() != 0)
                    {
                        wnd.SwapFlows(wnd.apiKeyFlow);
                        return;
                    }
                    wnd.SwapFlows(wnd.gameSelectorFlow);

                });
                return;
            }

            wnd.SwapFlows(wnd.loginFlow);
        }

        [InitializeOnLoadMethod]
        public static void LoadFirstTime()
        {
            if (LootLockerEditorData.ShouldAutoShowWindow())
            {
                EditorApplication.delayCall = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall, (EditorApplication.CallbackFunction)delegate
                {
                    LootLockerAdminExtension wnd = GetWindow<LootLockerAdminExtension>();
                    wnd.titleContent = new GUIContent("LootLocker");
                    wnd.ShowUtility();
                });

                return;
            }

        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();

            var styleLength = new StyleLength();
            var current = styleLength.value;
            current.unit = LengthUnit.Percent;

            current.value = 100;

            labelFromUXML.style.height = current;

            root.Add(labelFromUXML);

            live.value = new Color(0.749f, 0.325f, 0.098f, 1);
            stage.value = new Color(0.094f, 0.749f, 0.352f, 1);
            defaultButton.value = new Color(0.345f, 0.345f, 0.345f, 1);

            menuLogoutBtn = root.Q<Button>("LogoutBtn");

            menuLogoutBtn.style.display = DisplayStyle.None;

            environmentBackground = root.Q<VisualElement>("SwitchBackground");

            environmentBackground.AddManipulator(new Clickable(evt => SwapEnvironment()));

            environmentHandle = root.Q<VisualElement>("Handle");

            environmentHandle.AddManipulator(new Clickable(evt => SwapEnvironment()));

            environmentBackground.tooltip = "Stage";

            environmentElement = root.Q<VisualElement>("Environment");

            environmentElement.style.display = DisplayStyle.None;

            environmentTitle = root.Q<Label>("EnvironmentTitle");

            gameName = root.Q<Label>("GameName");

            menu = root.Q<VisualElement>("MenuBar");

            menu.style.display = DisplayStyle.None;

            menuChangeGameBtn = root.Q<Button>("ChangeGameBtn");

            menuAPIKeyBtn = root.Q<Button>("APIKeyBtn");

            menuLogoutBtn = root.Q<Button>("LogoutBtn");

            menuLogoutBtn.clickable.clicked += () =>
            {
                Logout();
            };

            menuChangeGameBtn.clickable.clicked += () =>
            {
                SwapFlows(gameSelectorFlow);
            };

            popup = root.Q<VisualElement>("PopUp");

            popupTitle = root.Q<Label>("popupTitle");
            popupMessage = root.Q<Label>("popupMessage");

            popupBtn = root.Q<Button>("popupCloseBtn");

            popupBtn.clickable.clickedWithEventInfo += ClosePopup;

            loginFlow = root.Q<VisualElement>("LoginFlow");

            loginFlow.style.display = DisplayStyle.None;

            emailField = root.Q<TextField>("EmailField");
            passwordField = root.Q<TextField>("PasswordField");

            passwordField.RegisterCallback<KeyDownEvent>((evt) =>
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    Login();
                }
            });

            signupLink = root.Q<Label>("newUserLink");
            gettingStartedLink = root.Q<Label>("gettingStartedLink");
            forgotPasswordLink = root.Q<Label>("forgotPasswordLink");

            loginBtn = root.Q<Button>("LoginBtn");

            signupLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://lootlocker.com/sign-up"));
            forgotPasswordLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://console.lootlocker.com/forgot-password"));
            gettingStartedLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://docs.lootlocker.com/the-basics/readme"));

            loginBtn.clickable.clicked += Login;

            mfaFlow = root.Q<VisualElement>("MFAFlow");

            codeField = root.Q<TextField>("CodeField");

            signInBtn = root.Q<Button>("SignInBtn");

            signInBtn.clickable.clickedWithEventInfo += SignIn;

            mfaFlow.style.display = DisplayStyle.None;

            gameSelectorFlow = root.Q<VisualElement>("GameSelectorFlow");

            gameSelectorList = root.Q<VisualElement>("GamesList");

            gameSelectorFlow.style.display = DisplayStyle.None;

            apiKeyFlow = root.Q<VisualElement>("APIKeyFlow");

            apiKeyFlow.style.display = DisplayStyle.None;

            createApiKeyWindow = root.Q<VisualElement>("InfoandCreate");

            createApiKeyWindow.style.display = DisplayStyle.None;

            apiKeyList = root.Q<VisualElement>("APIKeyList");

            newApiKeyWindow = root.Q<VisualElement>("CreateAPIKeyWindow");

            newApiKeyName = root.Q<TextField>("newApiKeyName");

            newApiKeyCancel = root.Q<Label>("APINewKeyCancel");

            newApiKeyCancel.RegisterCallback<MouseDownEvent>(_ =>
            {
                newApiKeyName.value = "";
                newApiKeyWindow.style.display = DisplayStyle.None;

            });

            createApiKeyBtn = root.Q<Button>("CreateNewKey");

            createApiKeyBtn.clickable.clicked += () =>
            {
                CreateNewAPIKey();
                newApiKeyWindow.style.display = DisplayStyle.None;
            };

            createNewApiKeyBtn = root.Q<Button>("CreateKeyBtn");

            createNewApiKeyBtn.clickable.clicked += () =>
            {
                newApiKeyWindow.style.display = DisplayStyle.Flex;
            };

            isStage = LootLockerEditorData.IsEnvironmentStage();

            if (isStage)
            {
                environmentHandle.style.alignSelf = Align.FlexStart;
                environmentBackground.style.backgroundColor = stage;
                environmentTitle.text = "Environment: Stage";
            }
            else
            {
                environmentHandle.style.alignSelf = Align.FlexEnd;
                environmentBackground.style.backgroundColor = live;
                environmentTitle.text = "Environment: Live";
            }

            loadingPage = root.Q<VisualElement>("LoadingBackground");
            loadingPage.style.display = DisplayStyle.Flex;
            currentFlow = loadingPage;

            loadingIcon = root.Q<VisualElement>("LoadingIcon");

            loadingIcon.schedule.Execute(() =>
            {
                if (rotateIndex >= 360)
                {
                    rotateIndex = 0;
                }
                rotateIndex += 1;
                EditorApplication.update += OnEditorUpdate;
                loadingIcon.style.rotate = new StyleRotate(new Rotate(new Angle(rotateIndex, AngleUnit.Degree)));
                EditorApplication.update -= OnEditorUpdate;

            }).Every(1);

            Run();
        }

        void SwapEnvironment()
        {
            if (isLoadingKeys)
            {
                ShowPopup("Error", "Please wait...");
                loadingPage.style.display = DisplayStyle.None;
                return;
            }

            isStage = !isStage;

            RefreshAPIKeys();

            if (isStage)
            {
                LootLockerEditorData.SetEnvironmentStage();
                environmentHandle.style.alignSelf = Align.FlexStart;
                environmentBackground.style.backgroundColor = stage;
                environmentTitle.text = "Environment: Stage";
            }
            else
            {
                LootLockerEditorData.SetEnvironmentLive();
                environmentHandle.style.alignSelf = Align.FlexEnd;
                environmentBackground.style.backgroundColor = live;
                environmentTitle.text = "Environment: Live";
            }
        }

        void SwapFlows(VisualElement New)
        {
            if (currentFlow == New) return;

            activeFlow = New;

            if (currentFlow != null)
            {
                currentFlow.style.display = DisplayStyle.None;
                if (currentFlow == apiKeyFlow)
                {
                    apiKeyList.Clear();
                }
            }

            New.style.display = DisplayStyle.Flex;

            if (activeFlow == gameSelectorFlow)
            {
                gameName.text = "LootLocker";
                emailField.value = "hector@lootlocker.com";
                passwordField.value = "its my password!";
                menuAPIKeyBtn.style.display = DisplayStyle.None;
                menuChangeGameBtn.style.display = DisplayStyle.None;
                environmentElement.style.display = DisplayStyle.None;
                createApiKeyWindow.style.display = DisplayStyle.None;
                menu.style.display = DisplayStyle.Flex;
                menuLogoutBtn.style.display = DisplayStyle.Flex;
                menuAPIKeyBtn.style.display = DisplayStyle.None;
                menuChangeGameBtn.style.display = DisplayStyle.None;

                CreateGameButtons();
            }

            if (activeFlow == apiKeyFlow)
            {
                gameName.text = LootLockerEditorData.GetSelectedGameName();
                menu.style.display = DisplayStyle.Flex;
                menuAPIKeyBtn.style.display = DisplayStyle.Flex;
                menuChangeGameBtn.style.display = DisplayStyle.Flex;
                environmentElement.style.display = DisplayStyle.Flex;
                createApiKeyWindow.style.display = DisplayStyle.Flex;
                menuLogoutBtn.style.display = DisplayStyle.Flex;
                menuAPIKeyBtn.style.display = DisplayStyle.Flex;
                menuChangeGameBtn.style.display = DisplayStyle.Flex;

                RefreshAPIKeys();
            }

            if (activeFlow == loginFlow)
            {
                environmentElement.style.display = DisplayStyle.None;
                menu.style.display = DisplayStyle.None;
                createApiKeyWindow.style.display = DisplayStyle.None;

            }

            if (activeFlow == mfaFlow)
            {
                menu.style.display = DisplayStyle.Flex;
                menuLogoutBtn.style.display = DisplayStyle.Flex;
                menuAPIKeyBtn.style.display = DisplayStyle.None;
                menuChangeGameBtn.style.display = DisplayStyle.None;
            }

            currentFlow = New;
        }

        private void OnEditorUpdate()
        {
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void ShowPopup(string title, string message)
        {
            popupTitle.text = title;
            popupMessage.text = message;

            popup.style.display = DisplayStyle.Flex;
        }

        private void ClosePopup(EventBase e)
        {
            popup.style.display = DisplayStyle.None;
        }

        public void Login()
        {
            if (string.IsNullOrEmpty(emailField.value) || string.IsNullOrEmpty(passwordField.value))
            {
                ShowPopup("Error", "Email or Password is empty.");
                loadingPage.style.display = DisplayStyle.None;
                return;
            }

            EditorApplication.update += OnEditorUpdate;
            loadingPage.style.display = DisplayStyle.Flex;

            LootLockerAdminManager.AdminLogin(emailField.value, passwordField.value, (onComplete) =>
            {
                if (!onComplete.success)
                {
                    ShowPopup("Error", "We couldn't recognize your information or there is no user with this email, please check and try again!");
                    loadingPage.style.display = DisplayStyle.None;
                    return;
                }

                if (onComplete.mfa_key != null)
                {
                    mfaKey = onComplete.mfa_key;
                    menu.style.display = DisplayStyle.Flex;
                    loadingPage.style.display = DisplayStyle.None;
                    SwapFlows(mfaFlow);
                }
                else
                {
                    menu.style.display = DisplayStyle.Flex;
                    LootLockerConfig.current.adminToken = onComplete.auth_token;
                    LootLockerEditorData.SetAdminToken(onComplete.auth_token);
                    loadingPage.style.display = DisplayStyle.None;

                    SwapFlows(gameSelectorFlow);
                }
                menuLogoutBtn.style.display = DisplayStyle.Flex;
                EditorApplication.update -= OnEditorUpdate;
            });
        }

        public void SignIn(EventBase e)
        {
            EditorApplication.update += OnEditorUpdate;
            loadingPage.style.display = DisplayStyle.Flex;
            LootLockerAdminManager.MFAAuthenticate(mfaKey, codeField.value, (onComplete) =>
            {
                if (!onComplete.success)
                {
                    ShowPopup("Error", "Could not authenticate MFA!");
                    loadingPage.style.display = DisplayStyle.None;
                }

                LootLockerConfig.current.adminToken = onComplete.auth_token;
                LootLockerEditorData.SetAdminToken(onComplete.auth_token);
                string projectPrefix = PlayerSettings.productGUID.ToString();

                SwapFlows(gameSelectorFlow);

                EditorApplication.update -= OnEditorUpdate;
                loadingPage.style.display = DisplayStyle.None;

            });
        }

        public void RefreshUserInformation(Action onComplete)
        {
            EditorApplication.update += OnEditorUpdate;
            loadingPage.style.display = DisplayStyle.Flex;

            LootLockerAdminManager.GetUserInformation((response) =>
            {
                if (!response.success)
                {
                    ShowPopup("Error", "Your token has expired, will redirect you to Login page now!");
                    Debug.Log(response.errorData.message);
                    loadingPage.style.display = DisplayStyle.None;
                    Logout();
                    return;
                }
                gameData.Clear();

                foreach (var org in response.user.organisations)
                {
                    foreach (var game in org.games)
                    {
                        if (game.id == LootLockerEditorData.GetSelectedGame() || game.development.id == LootLockerEditorData.GetSelectedGame())
                        {
                            LootLockerEditorData.SetSelectedGameName(game.name);
                        }
                        game.organisation_name = org.name;
                        gameData.Add(game.id, game);
                    }
                }

                gameDataRefreshTime = DateTime.Now;

                onComplete();

                EditorApplication.update -= OnEditorUpdate;
                loadingPage.style.display = DisplayStyle.None;

            });
        }

        public void CreateGameButtons()
        {
            gameSelectorList.Clear();

            var action = new Action(() =>
            {

                foreach (var game in gameData.Values)
                {
                    GameButtonTemplate(game, game.organisation_name);
                }
            });

            if (gameDataRefreshTime == null || DateTime.Now >= gameDataRefreshTime.AddMinutes(gameDataCacheExpirationTimeMinutes) || LootLockerEditorData.IsNewSession())
            {
                RefreshUserInformation(action);

            }
            else
            {
                action();
            }
        }

        public void GameButtonTemplate(LootLocker.Extension.DataTypes.Game game, string orgName)
        {
            Button button = new Button();

            button.style.flexDirection = FlexDirection.Column;
            button.name = game.id.ToString();

            button.AddToClassList("gameButton");

            Label gameTitle = new Label();
            gameTitle.text = game.name;
            gameTitle.name = "GameTitle";

            gameTitle.AddToClassList("gameButtonTitle");

            button.Add(gameTitle);

            Label gameOrg = new Label();
            gameOrg.text = orgName;
            gameOrg.name = "OrgTitle";

            gameOrg.AddToClassList("gameButtonOrgTitle");

            button.Add(gameOrg);

            button.clickable.clickedWithEventInfo += OnGameSelected;

            gameSelectorList.Add(button);
        }

        bool AtTarget(EventBase eventBase)
        {
#if UNITY_2023_1_OR_NEWER
            if(eventBase.propagationPhase == PropagationPhase.BubbleUp) 
                return true;
#else
            if(eventBase.propagationPhase == PropagationPhase.AtTarget) 
                return true;
#endif
            return false;
        }

        void OnGameSelected(EventBase e)
        {
            if (!AtTarget(e))
                return;

            var target = e.target as Button;

            LootLockerEditorData.SetSelectedGame(target.name);

            gameName.text = gameData[int.Parse(target.name)].name;

            EditorApplication.update += OnEditorUpdate;

            LootLockerAdminManager.GetGameDomainKey(LootLockerEditorData.GetSelectedGame(), (onComplete) =>
            {
                if (!onComplete.success)
                {
                    ShowPopup("Error", "Could not find Selected game!");
                    EditorApplication.update -= OnEditorUpdate;
                    return;
                }
                LootLockerConfig.current.domainKey = onComplete.game.domain_key;
                EditorApplication.update -= OnEditorUpdate;
            });



            SwapFlows(apiKeyFlow);
        }

        void CreateNewAPIKey()
        {
            int gameId = LootLockerEditorData.GetSelectedGame();

            if (gameId == 0)
            {
                ShowPopup("Error", "No active Game found!");
                return;
            }

            if (isStage)
            {
                gameId = gameData[gameId].development.id;
            }

            EditorApplication.update += OnEditorUpdate;
            loadingPage.style.display = DisplayStyle.Flex;

            LootLockerAdminManager.GenerateKey(gameId, newApiKeyName.value, "game", (onComplete) =>
            {
                if (!onComplete.success)
                {
                    ShowPopup("Error", "Could not create a new API Key!");
                    loadingPage.style.display = DisplayStyle.None;
                }

                APIKeyTemplate(onComplete);

                EditorApplication.update -= OnEditorUpdate;
                loadingPage.style.display = DisplayStyle.None;

            });



            newApiKeyName.value = "";
        }

        void RefreshAPIKeys()
        {
            apiKeyList.Clear();
            int gameID = isStage ? gameData[LootLockerEditorData.GetSelectedGame()].development.id : LootLockerEditorData.GetSelectedGame();
            isLoadingKeys = true;

            EditorApplication.update += OnEditorUpdate;
            loadingPage.style.display = DisplayStyle.Flex;

            LootLockerAdminManager.GetAllKeys(gameID, (onComplete) =>
            {
                if (!onComplete.success)
                {
                    ShowPopup("Error", "Could not find API Keys!");
                    loadingPage.style.display = DisplayStyle.None;
                    return;
                }

                foreach (var key in onComplete.api_keys)
                {
                    APIKeyTemplate(key);
                }

                isLoadingKeys = false;

                EditorApplication.update -= OnEditorUpdate;
                loadingPage.style.display = DisplayStyle.None;

            });
        }

        void APIKeyTemplate(KeyResponse key)
        {
            Button button = new Button();

            bool isLegacyKey = !(key.api_key.StartsWith("prod_") || key.api_key.StartsWith("dev_"));
            button.name = key.api_key;

            Label keyName = new Label();
            keyName.text = key.name;
            if (string.IsNullOrEmpty(key.name))
            {
                keyName.text += "Unnamed API Key";
            }
            if (button.name == LootLockerConfig.current.apiKey)
            {
                button.style.borderRightColor = button.style.borderLeftColor = button.style.borderTopColor = button.style.borderBottomColor = stage;
            }

            if (isLegacyKey)
            {
                keyName.text = "Legacy key: " + keyName.text;
                button.AddToClassList("legacyApikey");
            }
            else
            {
                button.AddToClassList("apikey");
            }


            keyName.AddToClassList("apikeyName");


            button.Add(keyName);

            if (!isLegacyKey)
            {
                button.clickable.clickedWithEventInfo += OnAPIKeySelected;
            }

            apiKeyList.Add(button);
        }

        void OnAPIKeySelected(EventBase e)
        {
            if (!AtTarget(e))
                return;

            var target = e.target as Button;

            LootLockerConfig.current.apiKey = target.name;

            SwapNewSelectedKey();
        }

        void SwapNewSelectedKey()
        {
            foreach (var element in apiKeyList.Children())
            {

                var key = element as Button;

                if (key.name == LootLockerConfig.current.apiKey)
                {
                    key.style.borderRightColor = key.style.borderLeftColor = key.style.borderTopColor = key.style.borderBottomColor = stage;
                }
                else
                {
                    key.style.backgroundColor = defaultButton;
                }
            }
        }

        void Logout()
        {
            loadingPage.style.display = DisplayStyle.Flex;
            LootLockerEditorData.ClearLootLockerPrefs();

            gameData.Clear();
            apiKeyList.Clear();
            gameSelectorList.Clear();
            loadingPage.style.display = DisplayStyle.None;
            SwapFlows(loginFlow);
        }

        private void OnDestroy()
        {
            LootLockerHTTPClient.ResetInstance();
        }

    }
}
#endif