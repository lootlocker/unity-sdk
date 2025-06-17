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

        private Label securityWarning;

        // License Countdown UI
        private VisualElement licenseCountdownContainer;
        private Label licenseCountdownLabel;
        private Image licenseCountdownIcon;

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

            securityWarning = new Label("Warning: Do not use on shared or insecure machines, data is stored in EditorPrefs");
            securityWarning.style.color = new StyleColor(Color.yellow);
            securityWarning.style.unityFontStyleAndWeight = FontStyle.Bold;
            securityWarning.style.display = DisplayStyle.None; // Hide by default
            root.Add(securityWarning);

            live.value = new Color(0.749f, 0.325f, 0.098f, 1);
            stage.value = new Color(0.094f, 0.749f, 0.352f, 1);
            defaultButton.value = new Color(0.345f, 0.345f, 0.345f, 1);

            // Null checks for all Q<T>() calls
            menuLogoutBtn = root.Q<Button>("LogoutBtn");
            if (menuLogoutBtn == null) Debug.LogWarning("LogoutBtn not found in UXML");
            else menuLogoutBtn.style.display = DisplayStyle.None;

            environmentBackground = root.Q<VisualElement>("SwitchBackground");
            if (environmentBackground == null) Debug.LogWarning("SwitchBackground not found in UXML");
            else environmentBackground.AddManipulator(new Clickable(evt => SwapEnvironment()));

            environmentHandle = root.Q<VisualElement>("Handle");
            if (environmentHandle == null) Debug.LogWarning("Handle not found in UXML");
            else environmentHandle.AddManipulator(new Clickable(evt => SwapEnvironment()));

            // environmentBackground?.SetTooltip("Stage");
            if (environmentBackground != null) environmentBackground.tooltip = "Stage";

            environmentElement = root.Q<VisualElement>("Environment");
            if (environmentElement == null) Debug.LogWarning("Environment not found in UXML");
            else environmentElement.style.display = DisplayStyle.None;

            environmentTitle = root.Q<Label>("EnvironmentTitle");
            if (environmentTitle == null) Debug.LogWarning("EnvironmentTitle not found in UXML");

            gameName = root.Q<Label>("GameName");
            if (gameName == null) Debug.LogWarning("GameName not found in UXML");

            menu = root.Q<VisualElement>("MenuBar");
            if (menu == null) Debug.LogWarning("MenuBar not found in UXML");
            else menu.style.display = DisplayStyle.None;

            menuChangeGameBtn = root.Q<Button>("ChangeGameBtn");
            if (menuChangeGameBtn == null) Debug.LogWarning("ChangeGameBtn not found in UXML");

            menuAPIKeyBtn = root.Q<Button>("APIKeyBtn");
            if (menuAPIKeyBtn == null) Debug.LogWarning("APIKeyBtn not found in UXML");

            menuLogoutBtn = root.Q<Button>("LogoutBtn");
            if (menuLogoutBtn != null)
                menuLogoutBtn.clickable.clicked += () => { ConfirmLogout(); };

            // menuChangeGameBtn?.clickable.clicked += () => { SwapFlows(gameSelectorFlow); };
            if (menuChangeGameBtn != null) menuChangeGameBtn.clickable.clicked += () => { SwapFlows(gameSelectorFlow); };

            popup = root.Q<VisualElement>("PopUp");
            if (popup == null) Debug.LogWarning("PopUp not found in UXML");

            popupTitle = root.Q<Label>("popupTitle");
            popupMessage = root.Q<Label>("popupMessage");
            popupBtn = root.Q<Button>("popupCloseBtn");
            if (popupBtn != null) popupBtn.clickable.clickedWithEventInfo += ClosePopup;

            loginFlow = root.Q<VisualElement>("LoginFlow");
            if (loginFlow == null) Debug.LogWarning("LoginFlow not found in UXML");
            else loginFlow.style.display = DisplayStyle.None;

            emailField = root.Q<TextField>("EmailField");
            passwordField = root.Q<TextField>("PasswordField");
            if (passwordField != null)
            {
                passwordField.RegisterCallback<KeyDownEvent>((evt) =>
                {
                    if (evt.keyCode == KeyCode.Return)
                    {
                        Login();
                    }
                });
            }

            signupLink = root.Q<Label>("newUserLink");
            gettingStartedLink = root.Q<Label>("gettingStartedLink");
            forgotPasswordLink = root.Q<Label>("forgotPasswordLink");
            loginBtn = root.Q<Button>("LoginBtn");
            if (signupLink != null) signupLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://lootlocker.com/sign-up"));
            if (forgotPasswordLink != null) forgotPasswordLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://console.lootlocker.com/forgot-password"));
            if (gettingStartedLink != null) gettingStartedLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://docs.lootlocker.com/the-basics/readme"));
            if (loginBtn != null) loginBtn.clickable.clicked += Login;

            mfaFlow = root.Q<VisualElement>("MFAFlow");
            codeField = root.Q<TextField>("CodeField");
            signInBtn = root.Q<Button>("SignInBtn");
            if (signInBtn != null) signInBtn.clickable.clickedWithEventInfo += SignIn;
            if (mfaFlow != null) mfaFlow.style.display = DisplayStyle.None;

            gameSelectorFlow = root.Q<VisualElement>("GameSelectorFlow");
            gameSelectorList = root.Q<VisualElement>("GamesList");
            if (gameSelectorFlow != null) gameSelectorFlow.style.display = DisplayStyle.None;

            apiKeyFlow = root.Q<VisualElement>("APIKeyFlow");
            if (apiKeyFlow != null) apiKeyFlow.style.display = DisplayStyle.None;

            createApiKeyWindow = root.Q<VisualElement>("InfoandCreate");
            if (createApiKeyWindow != null) createApiKeyWindow.style.display = DisplayStyle.None;

            apiKeyList = root.Q<VisualElement>("APIKeyList");
            newApiKeyWindow = root.Q<VisualElement>("CreateAPIKeyWindow");
            newApiKeyName = root.Q<TextField>("newApiKeyName");
            newApiKeyCancel = root.Q<Label>("APINewKeyCancel");
            if (newApiKeyCancel != null)
                newApiKeyCancel.RegisterCallback<MouseDownEvent>(_ =>
                {
                    if (newApiKeyName != null) newApiKeyName.value = "";
                    if (newApiKeyWindow != null) newApiKeyWindow.style.display = DisplayStyle.None;
                });

            createApiKeyBtn = root.Q<Button>("CreateNewKey");
            if (createApiKeyBtn != null)
                createApiKeyBtn.clickable.clicked += () =>
                {
                    CreateNewAPIKey();
                    if (newApiKeyWindow != null) newApiKeyWindow.style.display = DisplayStyle.None;
                };

            createNewApiKeyBtn = root.Q<Button>("CreateKeyBtn");
            if (createNewApiKeyBtn != null)
                createNewApiKeyBtn.clickable.clicked += () =>
                {
                    if (newApiKeyWindow != null) newApiKeyWindow.style.display = DisplayStyle.Flex;
                };

            isStage = LootLockerEditorData.IsEnvironmentStage();
            if (isStage)
            {
                if (environmentHandle != null) environmentHandle.style.alignSelf = Align.FlexStart;
                if (environmentBackground != null) environmentBackground.style.backgroundColor = stage;
                if (environmentTitle != null) environmentTitle.text = "Environment: Stage";
            }
            else
            {
                if (environmentHandle != null) environmentHandle.style.alignSelf = Align.FlexEnd;
                if (environmentBackground != null) environmentBackground.style.backgroundColor = live;
                if (environmentTitle != null) environmentTitle.text = "Environment: Live";
            }

            loadingPage = root.Q<VisualElement>("LoadingBackground");
            if (loadingPage != null) loadingPage.style.display = DisplayStyle.Flex;
            currentFlow = loadingPage;

            loadingIcon = root.Q<VisualElement>("LoadingIcon");
            if (loadingIcon != null)
            {
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
                }).Every(16); // 60 FPS
            }

            // License Countdown UI
            licenseCountdownContainer = root.Q<VisualElement>("LicenseCountdownContainer");
            licenseCountdownLabel = root.Q<Label>("LicenseCountdownLabel");
            licenseCountdownIcon = root.Q<Image>("LicenseCountdownIcon");
            if (licenseCountdownContainer != null) licenseCountdownContainer.style.display = DisplayStyle.None;

            Run();
        }

        private bool IsStageOnlyGame()
        {
            int selectedGameId = LootLockerEditorData.GetSelectedGame();
            if (!gameData.ContainsKey(selectedGameId)) return false;
            var game = gameData[selectedGameId];
            if (game.created_at == default(System.DateTime) || game.created_at == null) return false;
            var cutoff = new System.DateTime(2025, 3, 10);
            return game.created_at < cutoff;
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
                    apiKeyList?.Clear();
                }
            }
            New.style.display = DisplayStyle.Flex;
            if (securityWarning != null) securityWarning.style.display = DisplayStyle.None;
            
            if (activeFlow == gameSelectorFlow)
            {
                if (licenseCountdownContainer != null) licenseCountdownContainer.style.display = DisplayStyle.None;
                if (gameName != null) gameName.text = "LootLocker";
                // Remove hardcoded credentials
                if (emailField != null) emailField.value = "";
                if (passwordField != null) passwordField.value = "";
                SetMenuVisibility(apiKey: false, changeGame: false, logout: true);
                if (environmentElement != null) environmentElement.style.display = DisplayStyle.None;
                if (createApiKeyWindow != null) createApiKeyWindow.style.display = DisplayStyle.None;
                CreateGameButtons();
            }
            if (activeFlow == apiKeyFlow)
            {
                if (gameName != null) gameName.text = LootLockerEditorData.GetSelectedGameName();
                UpdateLicenseCountdownUI();
                SetMenuVisibility(apiKey: true, changeGame: true, logout: true);
                if (createApiKeyWindow != null) createApiKeyWindow.style.display = DisplayStyle.Flex;
                if (environmentElement != null && environmentBackground != null && environmentHandle != null && environmentTitle != null)
                {
                    if (IsStageOnlyGame())
                    {
                        environmentElement.style.display = DisplayStyle.Flex;
                        environmentBackground.style.display = DisplayStyle.Flex;
                        environmentHandle.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        environmentElement.style.display = DisplayStyle.None;
                    }
                }
                RefreshAPIKeys();
            }
            if (activeFlow == loginFlow)
            {
                if (licenseCountdownContainer != null) licenseCountdownContainer.style.display = DisplayStyle.None;
                if (environmentElement != null) environmentElement.style.display = DisplayStyle.None;
                if (menu != null) menu.style.display = DisplayStyle.None;
                if (createApiKeyWindow != null) createApiKeyWindow.style.display = DisplayStyle.None;
                if (securityWarning != null) securityWarning.style.display = DisplayStyle.Flex;
            }
            if (activeFlow == mfaFlow)
            {
                SetMenuVisibility(apiKey: false, changeGame: false, logout: true);
            }
            currentFlow = New;
        }
        // Helper for menu visibility
        void SetMenuVisibility(bool apiKey, bool changeGame, bool logout)
        {
            if (menu != null) menu.style.display = DisplayStyle.Flex;
            if (menuAPIKeyBtn != null) menuAPIKeyBtn.style.display = apiKey ? DisplayStyle.Flex : DisplayStyle.None;
            if (menuChangeGameBtn != null) menuChangeGameBtn.style.display = changeGame ? DisplayStyle.Flex : DisplayStyle.None;
            if (menuLogoutBtn != null) menuLogoutBtn.style.display = logout ? DisplayStyle.Flex : DisplayStyle.None;
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
            if (string.IsNullOrEmpty(emailField?.value) || string.IsNullOrEmpty(passwordField?.value))
            {
                ShowPopup("Error", "Email or Password is empty.");
                if (loadingPage != null) loadingPage.style.display = DisplayStyle.None;
                return;
            }
            EditorApplication.update += OnEditorUpdate;
            if (loadingPage != null) loadingPage.style.display = DisplayStyle.Flex;
            try
            {
                LootLockerAdminManager.AdminLogin(emailField.value, passwordField.value, (onComplete) =>
                {
                    if (!onComplete.success)
                    {
                        string errorMsg = !string.IsNullOrEmpty(onComplete.errorData?.message) ? onComplete.errorData.message : "We couldn't recognize your information or there is no user with this email, please check and try again!";
                        ShowPopup("Error", errorMsg);
                        if (loadingPage != null) loadingPage.style.display = DisplayStyle.None;
                        return;
                    }
                    if (onComplete.mfa_key != null)
                    {
                        mfaKey = onComplete.mfa_key;
                        if (menu != null) menu.style.display = DisplayStyle.Flex;
                        if (loadingPage != null) loadingPage.style.display = DisplayStyle.None;
                        SwapFlows(mfaFlow);
                    }
                    else
                    {
                        if (menu != null) menu.style.display = DisplayStyle.Flex;
                        LootLockerConfig.current.adminToken = onComplete.auth_token;
                        LootLockerEditorData.SetAdminToken(onComplete.auth_token);
                        if (loadingPage != null) loadingPage.style.display = DisplayStyle.None;
                        SwapFlows(gameSelectorFlow);
                    }
                    if (menuLogoutBtn != null) menuLogoutBtn.style.display = DisplayStyle.Flex;
                    EditorApplication.update -= OnEditorUpdate;
                });
            }
            catch (Exception ex)
            {
                ShowPopup("Error", $"Unexpected error: {ex.Message}");
                if (loadingPage != null) loadingPage.style.display = DisplayStyle.None;
            }
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
                UpdateLicenseCountdownUI();

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
            var selectedGameData = gameData[int.Parse(target.name)];

            LootLockerEditorData.SetSelectedGameName(selectedGameData.name);
            gameName.text = selectedGameData.name;
            UpdateLicenseCountdownUI();

            LootLockerEditorData.SetEnvironmentStage();
            isStage = true;

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
            int gameID = LootLockerEditorData.GetSelectedGame();
            if (isStage)
            {
                gameID = gameData[gameID].development.id;
            }
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
                button.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 0.2f, 1f)); // Feedback highlight
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
                button.tooltip = "Click to select this API key.";
            }
            apiKeyList?.Add(button);
        }

        void OnAPIKeySelected(EventBase e)
        {
            if (!AtTarget(e))
                return;

            var target = e.target as Button;

            LootLockerConfig.current.apiKey = target.name;

            SwapNewSelectedKey();
            ShowPopup("API Key Selected", $"API Key {target.name} is now active."); // Feedback
        }

        void ConfirmLogout()
        {
            ShowPopup("Confirm Logout", "Are you sure you want to log out? This will clear your admin token and settings.");
            if (popupBtn != null)
            {
                popupBtn.clickable.clickedWithEventInfo -= ClosePopup;
                popupBtn.clickable.clickedWithEventInfo += (evt) =>
                {
                    Logout();
                    ClosePopup(evt);
                };
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

        void SwapNewSelectedKey()
        {
            if (apiKeyList == null) return;
            foreach (var element in apiKeyList.Children())
            {
                var key = element as Button;
                if (key == null) continue;
                if (key.name == LootLockerConfig.current.apiKey)
                {
                    key.style.borderRightColor = key.style.borderLeftColor = key.style.borderTopColor = key.style.borderBottomColor = stage;
                    key.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 0.2f, 1f)); // Feedback highlight
                }
                else
                {
                    key.style.backgroundColor = defaultButton;
                    key.style.borderRightColor = key.style.borderLeftColor = key.style.borderTopColor = key.style.borderBottomColor = defaultButton;
                }
            }
        }

        // Call this whenever the selected game changes or flows swap to APIKeyFlow
        private void UpdateLicenseCountdownUI()
        {
            if (licenseCountdownContainer == null || licenseCountdownLabel == null || licenseCountdownIcon == null)
                return;

            int selectedGameId = LootLockerEditorData.GetSelectedGame();
            if (!gameData.ContainsKey(selectedGameId))
            {
                licenseCountdownContainer.style.display = DisplayStyle.None;
                return;
            }
            var game = gameData[selectedGameId];
            string trialEndDate = game.trial_end_date;
            if (string.IsNullOrEmpty(trialEndDate))
            {
                licenseCountdownContainer.style.display = DisplayStyle.None;
                return;
            }
            if (!System.DateTime.TryParse(trialEndDate, out var endDate))
            {
                licenseCountdownContainer.style.display = DisplayStyle.None;
                return;
            }
            var now = System.DateTime.UtcNow;
            int daysLeft = (endDate.Date - now.Date).Days;
            if (endDate < now)
            {
                // Expired
                licenseCountdownLabel.text = "Trial Expired";
                licenseCountdownLabel.RemoveFromClassList("licenseCountdownLabel");
                licenseCountdownLabel.RemoveFromClassList("expired");
                licenseCountdownLabel.AddToClassList("licenseCountdownLabel");
                licenseCountdownLabel.AddToClassList("expired");
                licenseCountdownLabel.style.color = new Color(1f, 0.31f, 0.31f); // rgb(255,80,80)
                licenseCountdownLabel.style.display = DisplayStyle.Flex;
                licenseCountdownIcon.image = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                licenseCountdownIcon.style.display = DisplayStyle.Flex;
                licenseCountdownContainer.style.display = DisplayStyle.Flex;
                licenseCountdownContainer.tooltip = "Click to manage your license";
                licenseCountdownContainer.RegisterCallback<MouseDownEvent>(LicenseCountdownContainerClick);
            }
            else if (daysLeft == 0)
            {
                // Trial ends today
                licenseCountdownLabel.text = "Trial ends today";
                licenseCountdownLabel.RemoveFromClassList("licenseCountdownLabel");
                licenseCountdownLabel.RemoveFromClassList("expired");
                licenseCountdownLabel.AddToClassList("licenseCountdownLabel");
                licenseCountdownLabel.AddToClassList("expired");
                licenseCountdownLabel.style.color = new Color(1f, 0.31f, 0.31f); // rgb(255,80,80)
                licenseCountdownLabel.style.display = DisplayStyle.Flex;
                licenseCountdownIcon.image = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                licenseCountdownIcon.style.display = DisplayStyle.Flex;
                licenseCountdownContainer.style.display = DisplayStyle.Flex;
                licenseCountdownContainer.tooltip = "Click to manage your license";
                licenseCountdownContainer.RegisterCallback<MouseDownEvent>(LicenseCountdownContainerClick);
            }
            else if (daysLeft > 0 && daysLeft <= 10)
            {
                // Not expired, but less than or equal to 10 days left
                licenseCountdownLabel.text = $"Trial ends in {daysLeft} days";
                licenseCountdownLabel.RemoveFromClassList("expired");
                licenseCountdownLabel.AddToClassList("licenseCountdownLabel");
                licenseCountdownLabel.style.display = DisplayStyle.Flex;
                licenseCountdownIcon.image = EditorGUIUtility.IconContent("_Help").image as Texture2D;
                licenseCountdownIcon.style.display = DisplayStyle.Flex;
                licenseCountdownContainer.style.display = DisplayStyle.Flex;
                licenseCountdownContainer.tooltip = "Learn more about your license";
                licenseCountdownContainer.UnregisterCallback<MouseDownEvent>(LicenseCountdownContainerClick);
                licenseCountdownIcon.RegisterCallback<MouseDownEvent>(LicenseCountdownIconClick);
            }
            else
            {
                // More than 10 days left, hide
                licenseCountdownContainer.style.display = DisplayStyle.None;
            }
        }

        private void LicenseCountdownIconClick(MouseDownEvent evt)
        {
            Application.OpenURL("https://console.lootlocker.com");
            evt.StopPropagation();
        }
        private void LicenseCountdownContainerClick(MouseDownEvent evt)
        {
            Application.OpenURL("https://console.lootlocker.com");
            evt.StopPropagation();
        }
    }
}
#endif