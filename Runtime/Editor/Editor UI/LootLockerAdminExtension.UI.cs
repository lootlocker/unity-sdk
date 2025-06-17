using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
namespace LootLocker.Extension
{
    public partial class LootLockerAdminExtension : EditorWindow
    {
        public void InitializeUIElements()
        {
            VisualElement root = rootVisualElement;
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();

            var styleLength = new StyleLength();
            var current = styleLength.value;
            current.unit = LengthUnit.Percent;
            current.value = 100;
            labelFromUXML.style.height = current;
            root.Add(labelFromUXML);

            CreateSecurityWarning(root);
            InitializeStyleColors();
            InitializeUIReferences(root);
            InitializeEventHandlers();
            InitializeEnvironmentUI();
            InitializeLoadingIcon(root);
            InitializeLicenseCountdownUI(root);
        }

        private void CreateSecurityWarning(VisualElement root)
        {
            securityWarning = new Label("Warning: Do not use on shared or insecure machines, data is stored in EditorPrefs");
            securityWarning.style.color = new StyleColor(Color.yellow);
            securityWarning.style.unityFontStyleAndWeight = FontStyle.Bold;
            securityWarning.style.display = DisplayStyle.None;
            root.Add(securityWarning);
        }

        private void InitializeStyleColors()
        {
            live.value = new Color(0.749f, 0.325f, 0.098f, 1);
            stage.value = new Color(0.094f, 0.749f, 0.352f, 1);
            defaultButton.value = new Color(0.345f, 0.345f, 0.345f, 1);
        }

        private void InitializeUIReferences(VisualElement root)
        {
            // Environment
            environmentBackground = root.Q<VisualElement>("SwitchBackground");
            environmentHandle = root.Q<VisualElement>("Handle");
            environmentElement = root.Q<VisualElement>("Environment");
            environmentTitle = root.Q<Label>("EnvironmentTitle");

            // Basic UI elements
            gameName = root.Q<Label>("GameName");
            menu = root.Q<VisualElement>("MenuBar");

            // Menu buttons
            menuChangeGameBtn = root.Q<Button>("ChangeGameBtn");
            menuAPIKeyBtn = root.Q<Button>("APIKeyBtn");
            menuLogoutBtn = root.Q<Button>("LogoutBtn");

            // Popup elements
            popup = root.Q<VisualElement>("PopUp");
            popupTitle = root.Q<Label>("popupTitle");
            popupMessage = root.Q<Label>("popupMessage");
            popupBtn = root.Q<Button>("popupCloseBtn");

            // Flow containers
            loginFlow = root.Q<VisualElement>("LoginFlow");
            mfaFlow = root.Q<VisualElement>("MFAFlow");
            gameSelectorFlow = root.Q<VisualElement>("GameSelectorFlow");
            apiKeyFlow = root.Q<VisualElement>("APIKeyFlow");
            settingsFlow = root.Q<VisualElement>("SettingsFlow");

            // Login elements
            emailField = root.Q<TextField>("EmailField");
            passwordField = root.Q<TextField>("PasswordField");
            signupLink = root.Q<Label>("newUserLink");
            gettingStartedLink = root.Q<Label>("gettingStartedLink");
            forgotPasswordLink = root.Q<Label>("forgotPasswordLink");
            loginBtn = root.Q<Button>("LoginBtn");

            // MFA elements
            codeField = root.Q<TextField>("CodeField");
            signInBtn = root.Q<Button>("SignInBtn");

            // Game selector elements
            gameSelectorList = root.Q<VisualElement>("GamesList");

            // API Key elements
            createApiKeyWindow = root.Q<VisualElement>("InfoandCreate");
            apiKeyList = root.Q<VisualElement>("APIKeyList");
            newApiKeyWindow = root.Q<VisualElement>("CreateAPIKeyWindow");
            newApiKeyName = root.Q<TextField>("newApiKeyName");
            newApiKeyCancel = root.Q<Label>("APINewKeyCancel");
            createApiKeyBtn = root.Q<Button>("CreateNewKey");
            createNewApiKeyBtn = root.Q<Button>("CreateKeyBtn");

            // Settings elements
            settingsBtn = root.Q<Button>("SettingsBtn");
            settingsBackBtn = root.Q<Button>("SettingsBackBtn");
            gameVersionField = root.Q<TextField>("GameVersionField");
            gameVersionWarning = root.Q<Label>("GameVersionWarning");
            logLevelField = root.Q<EnumField>("LogLevelField");
            logErrorsAsWarningsToggle = root.Q<Toggle>("LogErrorsAsWarningsToggle");
            logInBuildsToggle = root.Q<Toggle>("LogInBuildsToggle");
            allowTokenRefreshToggle = root.Q<Toggle>("AllowTokenRefreshToggle");

            // Set initial display states
            SetInitialDisplayStates();
        }

        private void SetInitialDisplayStates()
        {
            if (menuLogoutBtn != null) menuLogoutBtn.style.display = DisplayStyle.None;
            if (environmentElement != null) environmentElement.style.display = DisplayStyle.None;
            if (menu != null) menu.style.display = DisplayStyle.None;
            if (loginFlow != null) loginFlow.style.display = DisplayStyle.None;
            if (mfaFlow != null) mfaFlow.style.display = DisplayStyle.None;
            if (gameSelectorFlow != null) gameSelectorFlow.style.display = DisplayStyle.None;
            if (apiKeyFlow != null) apiKeyFlow.style.display = DisplayStyle.None;
            if (settingsFlow != null) settingsFlow.style.display = DisplayStyle.None;
            if (createApiKeyWindow != null) createApiKeyWindow.style.display = DisplayStyle.None;
        }

        private void InitializeEventHandlers()
        {
            // Environment switcher
            if (environmentBackground != null) environmentBackground.AddManipulator(new Clickable(evt => SwapEnvironment()));
            if (environmentHandle != null) environmentHandle.AddManipulator(new Clickable(evt => SwapEnvironment()));

            // Menu buttons
            if (menuLogoutBtn != null) menuLogoutBtn.clickable.clicked += ConfirmLogout;
            if (menuChangeGameBtn != null) menuChangeGameBtn.clickable.clicked += () => RequestFlowSwitch(gameSelectorFlow);
            if (menuAPIKeyBtn != null) menuAPIKeyBtn.clickable.clicked += () => RequestFlowSwitch(apiKeyFlow);

            // Popup
            if (popupBtn != null) popupBtn.clickable.clickedWithEventInfo += ClosePopup;

            // Login
            if (passwordField != null) passwordField.RegisterCallback<KeyDownEvent>(OnPasswordKeyDown);
            if (signupLink != null) signupLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://lootlocker.com/sign-up"));
            if (forgotPasswordLink != null) forgotPasswordLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://console.lootlocker.com/forgot-password"));
            if (gettingStartedLink != null) gettingStartedLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://docs.lootlocker.com/the-basics/readme"));
            if (loginBtn != null) loginBtn.clickable.clicked += Login;

            // MFA
            if (signInBtn != null) signInBtn.clickable.clickedWithEventInfo += SignIn;

            // API Key creation
            if (newApiKeyCancel != null) newApiKeyCancel.RegisterCallback<MouseDownEvent>(OnNewApiKeyCancelClick);
            if (createApiKeyBtn != null) createApiKeyBtn.clickable.clicked += OnCreateApiKeyClick;
            if (createNewApiKeyBtn != null) createNewApiKeyBtn.clickable.clicked += OnCreateNewApiKeyClick;

            // Settings
            if (settingsBtn != null) settingsBtn.clickable.clicked += OnSettingsClick;
            if (settingsBackBtn != null) settingsBackBtn.clickable.clicked += OnSettingsBackClick;

            // Settings value change handlers
            InitializeSettingsEventHandlers();
        }

        private void InitializeSettingsEventHandlers()
        {
            if (gameVersionField != null) gameVersionField.RegisterValueChangedCallback(evt => SaveGameVersion(evt.newValue));
            if (logLevelField != null) logLevelField.RegisterValueChangedCallback(evt => SaveLogLevel((LootLockerLogger.LogLevel)evt.newValue));
            if (logErrorsAsWarningsToggle != null) logErrorsAsWarningsToggle.RegisterValueChangedCallback(evt => SaveLogErrorsAsWarnings(evt.newValue));
            if (logInBuildsToggle != null) logInBuildsToggle.RegisterValueChangedCallback(evt => SaveLogInBuilds(evt.newValue));
            if (allowTokenRefreshToggle != null) allowTokenRefreshToggle.RegisterValueChangedCallback(evt => SaveAllowTokenRefresh(evt.newValue));
        }

        private void InitializeEnvironmentUI()
        {
            if (environmentBackground != null) environmentBackground.tooltip = "Stage";
            
            isStage = LootLockerEditorData.IsEnvironmentStage();
            UpdateEnvironmentUI();
        }

        private void InitializeLoadingIcon(VisualElement root)
        {
            loadingPage = root.Q<VisualElement>("LoadingBackground");
            if (loadingPage != null) loadingPage.style.display = DisplayStyle.Flex;
            currentFlow = loadingPage;

            loadingIcon = root.Q<VisualElement>("LoadingIcon");
            if (loadingIcon != null)
            {
                loadingIcon.schedule.Execute(() =>
                {
                    if (rotateIndex >= 360) rotateIndex = 0;
                    rotateIndex += 1;
                    EditorApplication.update += OnEditorUpdate;
                    loadingIcon.style.rotate = new StyleRotate(new Rotate(new Angle(rotateIndex, AngleUnit.Degree)));
                    EditorApplication.update -= OnEditorUpdate;
                }).Every(16); // 60 FPS
            }
        }

        private void InitializeLicenseCountdownUI(VisualElement root)
        {
            licenseCountdownContainer = root.Q<VisualElement>("LicenseCountdownContainer");
            licenseCountdownLabel = root.Q<Label>("LicenseCountdownLabel");
            licenseCountdownIcon = root.Q<Image>("LicenseCountdownIcon");
            if (licenseCountdownContainer != null) licenseCountdownContainer.style.display = DisplayStyle.None;
        }

        public void UpdateEnvironmentUI()
        {
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
        }

        public void SetupSettingsButton()
        {
            if (settingsBtn != null)
            {
                var settingsIcon = EditorGUIUtility.IconContent("SettingsIcon");
                if (settingsIcon?.image != null)
                {
                    settingsBtn.style.backgroundImage = new StyleBackground(settingsIcon.image as Texture2D);
                }
            }
        }        // Helper for menu visibility
        public void SetMenuVisibility(bool apiKey, bool changeGame, bool logout)
        {
            if (menu != null) menu.style.display = DisplayStyle.Flex;
            if (menuAPIKeyBtn != null) menuAPIKeyBtn.style.display = apiKey ? DisplayStyle.Flex : DisplayStyle.None;
            if (menuChangeGameBtn != null) menuChangeGameBtn.style.display = changeGame ? DisplayStyle.Flex : DisplayStyle.None;
            if (menuLogoutBtn != null) menuLogoutBtn.style.display = logout ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
#endif
