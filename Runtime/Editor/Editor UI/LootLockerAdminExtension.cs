using System;
using System.Collections.Generic;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LootLocker.Extension.Responses;

namespace LootLocker.Extension
{
    public partial class LootLockerAdminExtension : EditorWindow
    {
        #region State Variables
        private bool isStage = true;
        private bool isLoadingKeys = false;
        private Dictionary<int, LootLocker.Extension.DataTypes.Game> gameData = new Dictionary<int, LootLocker.Extension.DataTypes.Game>();
        private DateTime gameDataRefreshTime;
        private readonly int gameDataCacheExpirationTimeMinutes = 1;
        private VisualElement currentFlow = null;
        private VisualElement previousFlow = null;
        private string mfaKey;
        private int rotateIndex = 0;
        #endregion

        #region Serialized Fields
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;
        #endregion

        #region Style Colors
        [Header("Colors")]
        private StyleColor stage;
        private StyleColor live;
        private StyleColor defaultButton;
        #endregion

        #region UI Element References
        // Basic UI elements
        private Label gameName;
        private Label securityWarning;

        // Environment
        private VisualElement environmentBackground, environmentHandle;
        private VisualElement environmentElement;
        private Label environmentTitle;

        // Flows
        private VisualElement activeFlow;
        private VisualElement loginFlow, mfaFlow, gameSelectorFlow, apiKeyFlow, settingsFlow;

        // Menu
        private VisualElement menu;
        private Button menuAPIKeyBtn;
        private Button menuChangeGameBtn;
        private Button menuLogoutBtn;

        // Popup
        private VisualElement popup;
        private Label popupTitle, popupMessage;
        private Button popupBtn;

        // API Key
        private Label newApiKeyCancel;
        private VisualElement newApiKeyWindow;
        private TextField newApiKeyName;
        private VisualElement createApiKeyWindow;
        private Button createNewApiKeyBtn;
        private Button createApiKeyBtn;
        private VisualElement apiKeyList;

        // Login UI
        private TextField emailField, passwordField;
        private Label signupLink, gettingStartedLink, forgotPasswordLink;
        private Button loginBtn;

        // MFA UI
        private TextField codeField;
        private Button signInBtn;

        // Game Selector UI
        private VisualElement gameSelectorList;

        // Loading Icon
        private VisualElement loadingPage;
        private VisualElement loadingIcon;

        // License Countdown UI
        private VisualElement licenseCountdownContainer;
        private Label licenseCountdownLabel;
        private Image licenseCountdownIcon;

        // Settings UI
        private Button settingsBtn;
        private Button settingsBackBtn;
        private TextField gameVersionField;
        private Label gameVersionWarning;
#if UNITY_2022_1_OR_NEWER
        private EnumField logLevelField;
#endif
        private Toggle logErrorsAsWarningsToggle, logInBuildsToggle, allowTokenRefreshToggle;
        #endregion

        #region Window Management
        [MenuItem("Window/LootLocker/Manage", false, 100)]
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
                EditorApplication.delayCall = (EditorApplication.CallbackFunction)Delegate.Combine(
                    EditorApplication.delayCall, 
                    (EditorApplication.CallbackFunction)delegate
                    {
                        LootLockerAdminExtension wnd = GetWindow<LootLockerAdminExtension>();
                        wnd.titleContent = new GUIContent("LootLocker");
                        wnd.ShowUtility();
                    });
            }
        }

        public void CreateGUI()
        {
            InitializeUIElements();
            SetupSettingsButton();
            Run();
        }
        #endregion

        #region Flow Management
        private bool IsStageOnlyGame()
        {
            int selectedGameId = LootLockerEditorData.GetSelectedGame();
            if (!gameData.ContainsKey(selectedGameId)) return false;
            var game = gameData[selectedGameId];
            if (game.created_at == default(System.DateTime) || game.created_at == null) return false;
            var cutoff = new System.DateTime(2025, 3, 10);
            return game.created_at >= cutoff;
        }        void SwapEnvironment()
        {
            if (isLoadingKeys)
            {
                if (popup != null && popupTitle != null && popupMessage != null)
                {
                    popupTitle.text = "Error";
                    popupMessage.text = "Please wait...";
                    popup.style.display = DisplayStyle.Flex;
                }
                if (loadingPage != null) loadingPage.style.display = DisplayStyle.None;
                return;
            }

            isStage = !isStage;
            RefreshAPIKeys();

            if (isStage)
            {
                LootLockerEditorData.SetEnvironmentStage();
            }
            else
            {
                LootLockerEditorData.SetEnvironmentLive();
            }

            UpdateEnvironmentUI();
        }

        void SwapFlows(VisualElement newFlow)
        {
            if (currentFlow == newFlow) return;
            
            activeFlow = newFlow;
            
            if (currentFlow != null)
            {
                currentFlow.style.display = DisplayStyle.None;
                if (currentFlow == apiKeyFlow && apiKeyList != null)
                {
                    apiKeyList.Clear();
                }
            }

            newFlow.style.display = DisplayStyle.Flex;
            
            ConfigureFlowUI(newFlow);
            
            currentFlow = newFlow;
        }

        private void ConfigureFlowUI(VisualElement flow)
        {
            // Hide security warning by default
            if (securityWarning != null) securityWarning.style.display = DisplayStyle.None;

            if (flow == gameSelectorFlow)
            {
                ConfigureGameSelectorFlow();
            }
            else if (flow == apiKeyFlow)
            {
                ConfigureApiKeyFlow();
            }
            else if (flow == settingsFlow)
            {
                ConfigureSettingsFlow();
            }
            else if (flow == loginFlow)
            {
                ConfigureLoginFlow();
            }
            else if (flow == mfaFlow)
            {
                ConfigureMfaFlow();
            }
        }

        private void ConfigureGameSelectorFlow()
        {
            if (licenseCountdownContainer != null) licenseCountdownContainer.style.display = DisplayStyle.None;
            if (gameName != null) gameName.text = "LootLocker";
            if (settingsBtn != null) settingsBtn.style.display = DisplayStyle.None;
            if (emailField != null) emailField.value = "";
            if (passwordField != null) passwordField.value = "";
            SetMenuVisibility(apiKey: false, changeGame: false, logout: true);
            if (environmentElement != null) environmentElement.style.display = DisplayStyle.None;
            if (createApiKeyWindow != null) createApiKeyWindow.style.display = DisplayStyle.None;
            CreateGameButtons();
        }

        private void ConfigureApiKeyFlow()
        {
            if (settingsBtn != null) settingsBtn.style.display = DisplayStyle.Flex;
            if (gameName != null) gameName.text = LootLockerEditorData.GetSelectedGameName();
            UpdateLicenseCountdownUI();
            SetMenuVisibility(apiKey: true, changeGame: true, logout: true);
            if (createApiKeyWindow != null) createApiKeyWindow.style.display = DisplayStyle.Flex;

            // Show environment switcher only for non-stage-only games
            if (environmentElement != null && environmentBackground != null && environmentHandle != null && environmentTitle != null)
            {
                if (!IsStageOnlyGame())
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

        private void ConfigureSettingsFlow()
        {
            if (settingsBtn != null) settingsBtn.style.display = DisplayStyle.Flex;
            if (licenseCountdownContainer != null) licenseCountdownContainer.style.display = DisplayStyle.None;
            SetMenuVisibility(apiKey: true, changeGame: true, logout: true);
            if (environmentElement != null) environmentElement.style.display = DisplayStyle.None;
            if (createApiKeyWindow != null) createApiKeyWindow.style.display = DisplayStyle.None;
            LoadSettingsUI();
        }

        private void ConfigureLoginFlow()
        {
            if (licenseCountdownContainer != null) licenseCountdownContainer.style.display = DisplayStyle.None;
            if (settingsBtn != null) settingsBtn.style.display = DisplayStyle.None;
            if (menu != null) menu.style.display = DisplayStyle.None;
            if (createApiKeyWindow != null) createApiKeyWindow.style.display = DisplayStyle.None;
            if (securityWarning != null) securityWarning.style.display = DisplayStyle.Flex;
        }

        private void ConfigureMfaFlow()
        {
            if (settingsBtn != null) settingsBtn.style.display = DisplayStyle.None;
            SetMenuVisibility(apiKey: false, changeGame: false, logout: true);
        }
        #endregion

        private void OnDestroy()
        {
            LootLockerHTTPClient.ResetInstance();
        }
    }
}
#endif
