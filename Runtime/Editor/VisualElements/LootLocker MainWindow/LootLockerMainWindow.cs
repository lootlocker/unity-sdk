using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && LOOTLOCKER_ENABLE_EXTENSION
using LootLocker.Extension.DataTypes;
using LootLocker.Extension.Responses;

namespace LootLocker.Extension
{
    public class LootLockerMainWindow : EditorWindow
    {

        Color LLGreen = new Color(0.10980392156862745f, 0.9098039215686274f, 0.42745098039215684f);
        Color LLForestGreen = new Color(5, 39, 20);

        enum Page
        {
            OrganisationPage,
            GamePage,
            GameOptionPage,
            ApiPage,
        }

        public enum ContentType
        {
            Organisations,
            Games,
            GameOptions,
        }

        Page currentPage;

        private User activeUser;
        private Organisation activeOrganisation;
        private Game activeGame;


        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        private VisualElement tabWindow, root;

        private StyleSheet sheet;

        private Label listHeader, gameTitle, userName, activeWindowLabel;

        private DropdownField keyEnvironment, gameEnvironment;

        private TextField keyName;

        private ScrollView keyList, optionList;

        private Button createKeyButton, returnBtn;

        [MenuItem("Tools/LootLocker/Logout", priority = 10)]
        public static void Logout()
        {
            StoredUser.current.RemoveUser();
        }

        [MenuItem("Tools/LootLocker/Settings", priority = 1)]
        public static void OpenMenu()
        {
            LootLockerMainWindow wnd = GetWindow<LootLockerMainWindow>();
            wnd.titleContent = new GUIContent("LootLocker");
            if (StoredUser.current.user != null)
            {
                LootLockerConfig.current.adminToken = EditorPrefs.GetString("LootLocker.AdminToken");
                wnd.LoadLootLockerMainMenu(StoredUser.current.user);
            }
            else
            {
                LootLockerWizard.LoadLogin();
            }
        }

        public void LoadLootLockerMainMenu(User user)
        {
            LootLockerConfig.current.adminToken = EditorPrefs.GetString("LootLocker.AdminToken");

            LootLockerServerManager.CheckInit();

            activeUser = user;
            StoredUser.CreateNewUser(activeUser);

            userName.text = activeUser.name;

            EditorApplication.update += OnEditorUpdate;

            LootLockerAdminManager.GetUserRole(activeUser.id.ToString(), (onComplete) =>
            {
                if (onComplete.success)
                {
                    foreach (var perm in onComplete.permissions)
                    {
                        userName.text += "\n" + " - " + perm;
                    }
                }

                EditorApplication.update -= OnEditorUpdate;
            });

            activeOrganisation = activeUser.GetOrganisationByID(EditorPrefs.GetInt("LootLocker.ActiveOrgID"));
            bool hasOrganisationBeenConfigured = activeOrganisation != null;

            if (!hasOrganisationBeenConfigured)
            {
                activeOrganisation = activeUser.organisations[0];
            }

            activeGame = activeOrganisation.GetGameByID(EditorPrefs.GetInt("LootLocker.ActiveGameID"));

            if (activeGame != null)
            {
                gameTitle.text = activeGame.name;
                PopulateList(ContentType.GameOptions);
                OpenAPIKeyTab();
                return;
            }

            if (user.organisations.Length <= 1 || hasOrganisationBeenConfigured)
            {
                PopulateList(ContentType.Games);
            }
            else
            {
                PopulateList(ContentType.Organisations);
            }
        }

        public void CreateGUI()
        {
            root = rootVisualElement;
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            var temp = root.Children().First();
            temp.AddToClassList("rootobj");

            optionList = root.Q<ScrollView>("ContentSideBar");

            listHeader = root.Q<Label>("ListHeader");
            gameTitle = root.Q<Label>("GameTitle");
            gameTitle.text = "";

            activeWindowLabel = root.Q<Label>("ActiveWindow");

            userName = root.Q<Label>("UserName");

            tabWindow = root.Q<VisualElement>("TabView");

            tabWindow.style.display = DisplayStyle.None;

            returnBtn = root.Q<Button>("returnBtn");
            returnBtn.style.display = DisplayStyle.Flex;
            returnBtn.clickable.clickedWithEventInfo += Return;

            keyList = root.Q<ScrollView>("KeyList");
            keyList.AddToClassList("keylist");

            keyEnvironment = root.Q<DropdownField>("keyEnvironment");
            gameEnvironment = root.Q<DropdownField>("gameEnvironment");

            keyName = root.Q<TextField>("apikeyname");

            createKeyButton = root.Q<Button>("create");

            createKeyButton.clickable.clicked += CreateKey;

            sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/LootLockerSDK/Runtime/Editor/VisualElements/LootLocker MainWindow/LootLockerMainWindow.uss");
            root.styleSheets.Add(sheet);
        }

        void Return(EventBase e)
        {
            if (e.propagationPhase != PropagationPhase.AtTarget)
                return;
            var target = e.target as Button;

            switch (currentPage)
            {
                case Page.GamePage:
                    optionList.Clear();
                    PopulateList(ContentType.Organisations);
                    break;
                case Page.GameOptionPage:
                    optionList.Clear();
                    optionList.style.display = DisplayStyle.Flex;
                    tabWindow.style.display = DisplayStyle.None;
                    PopulateList(ContentType.Games);
                    break;
                case Page.ApiPage:
                    optionList.Clear();
                    gameTitle.text = "";
                    optionList.style.display = DisplayStyle.Flex;
                    tabWindow.style.display = DisplayStyle.None;
                    activeWindowLabel.style.display = DisplayStyle.None;
                    PopulateList(ContentType.Games);
                    break;
                default:
                    target.style.display = DisplayStyle.None;
                    break;
            }
        }

        private void OpenAPIKeyTab()
        {
            keyList.Clear();

            tabWindow.style.display = DisplayStyle.Flex;
            currentPage = Page.ApiPage;

            activeWindowLabel.style.display = DisplayStyle.Flex;
            activeWindowLabel.text = " - API Keys";

            EditorApplication.update += OnEditorUpdate;
            LootLockerAdminManager.GetAllKeys(activeGame.id.ToString(), (onComplete) =>
            {
                if (onComplete.success)
                {
                    foreach (var key in onComplete.api_keys)
                    {
                        CreateAPIKeyTemplate(key);
                    }
                }

                EditorApplication.update -= OnEditorUpdate;
            });
        }

        public void CreateKey()
        {
            int gameEnv = activeGame.id;

            if (gameEnvironment.value == "Stage")
            {
                gameEnv = activeGame.development.id;
            }

            if (keyName.value == string.Empty)
            {
                LootLockerLogger.GetForLogLevel(LootLockerLogger.LogLevel.Verbose)(
                    "You must give a name to your API Key!");
                return;
            }

            EditorApplication.update += OnEditorUpdate;

            LootLockerAdminManager.GenerateKey(gameEnv.ToString(), keyName.value, keyEnvironment.value.ToLower(),
                (onComplete) =>
                {
                    if (onComplete.success)
                    {
                        CreateAPIKeyTemplate(onComplete);
                    }

                    EditorApplication.update -= OnEditorUpdate;
                });
        }

        void OnEditorUpdate()
        {
            EditorApplication.QueuePlayerLoopUpdate();
        }

        public void ApplyKeyClicked(EventBase e)
        {
            Button target = e.currentTarget as Button;

            var parent = target.parent;

            var code = parent.Q<Label>("keycode");

            LootLockerConfig.current.apiKey = code.text;

            foreach (var element in keyList.Children())
            {
                var apikey = element.Q<Label>("keycode");
                var button = element.Q<Button>("applykeybtn");
                if (code.text == apikey.text)
                {
                    button.text = "Applied!";
                    button.style.borderRightColor =
                        button.style.borderLeftColor =
                            button.style.borderBottomColor =
                                button.style.borderTopColor = LLGreen;
                }
                else
                {
                    button.text = "Apply";
                    button.style.borderRightColor =
                        button.style.borderLeftColor =
                            button.style.borderBottomColor =
                                button.style.borderTopColor = LLForestGreen;
                }
            }
        }

        public void CreateAPIKeyTemplate(KeyResponse key)
        {
            foreach (var existingKey in keyList.Children())
            {
                if (key.name == existingKey.name)
                {
                    return;
                }
            }

            VisualElement keyTemplate = new VisualElement();
            keyTemplate.name = key.name;
            keyTemplate.style.flexDirection = FlexDirection.Row;
            VisualElement keyInfo = new VisualElement();
            keyTemplate.Add(keyInfo);
            keyInfo.AddToClassList("keyinfo");
            Label keyTitle = new Label();
            keyTitle.text = key.name;
            keyInfo.Add(keyTitle);
            Label keyCode = new Label();
            keyCode.name = "keycode";
            keyCode.text = key.api_key;
            keyInfo.Add(keyCode);
            Button keyApply = new Button();
            keyApply.name = "applykeybtn";
            if (LootLockerConfig.current.apiKey == key.api_key)
            {
                keyApply.text = "Applied!";
                keyApply.style.borderRightColor =
                    keyApply.style.borderLeftColor =
                        keyApply.style.borderBottomColor =
                            keyApply.style.borderTopColor = LLGreen;
            }
            else
            {
                keyApply.text = "Apply";
                keyApply.style.borderRightColor =
                    keyApply.style.borderLeftColor =
                        keyApply.style.borderBottomColor =
                            keyApply.style.borderTopColor = LLForestGreen;
            }

            keyApply.clickable.clickedWithEventInfo += ApplyKeyClicked;
            keyTemplate.Add(keyApply);
            keyTemplate.AddToClassList("apikey");
            keyList.Add(keyTemplate);
        }

        public void PopulateList(ContentType contentType)
        {
            optionList.Clear();

            switch (contentType)
            {
                case ContentType.Organisations:
                    currentPage = Page.OrganisationPage;
                    returnBtn.style.display = DisplayStyle.None;
                    listHeader.text = "Organisations:";
                    foreach (var org in activeUser.organisations)
                    {
                        var btn = GenerateOrganisationButton(org);
                        optionList.Add(btn);
                    }

                    break;
                case ContentType.Games:
                    currentPage = Page.GamePage;
                    listHeader.text = "Games:";
                    foreach (var game in activeOrganisation.games)
                    {
                        var btn = GenerateGameButton(game);
                        optionList.Add(btn);
                    }

                    returnBtn.style.display = DisplayStyle.Flex;
                    returnBtn.text = "Change Organisation";
                    break;
                case ContentType.GameOptions:
                    currentPage = Page.GameOptionPage;
                    Button apikeybtn = GenerateGameOptionButtons();
                    optionList.Add(apikeybtn);
                    returnBtn.style.display = DisplayStyle.Flex;
                    returnBtn.text = "Change Game";
                    break;
            }
        }

        public Button GenerateGameOptionButtons()
        {
            Button button = new Button();
            button.text = "API Keys";
            button.clickable.clicked += OpenAPIKeyTab;
            button.style.flexGrow = 1;
            button.style.flexDirection = FlexDirection.Row;
            button.style.width = 200;
            button.style.height = 50;
            button.AddToClassList("optionbtn");
            return button;
        }

        public Button GenerateGameButton(Game game)
        {
            Button newButton = new Button();
            newButton.style.flexGrow = 1;
            newButton.style.flexDirection = FlexDirection.Row;
            newButton.name = game.id.ToString();
            newButton.style.width = 200;
            newButton.style.height = 50;

            Label buttonLabel = new Label();
            buttonLabel.name = "name";
            buttonLabel.text = game.name;
            buttonLabel.style.fontSize = 15;

            newButton.Add(buttonLabel);
            buttonLabel.AddToClassList("optiontext");

            Label buttonImage = new Label();

            buttonImage.style.backgroundImage =
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LootLockerSDK/Runtime/Editor/Icons/caret.png");

            newButton.style.justifyContent = Justify.SpaceBetween;

            newButton.Add(buttonImage);
            buttonImage.AddToClassList("caret");

            newButton.AddToClassList("gamebtn");
            newButton.clickable.clickedWithEventInfo += GameButtonClicked;

            return newButton;
        }

        public Button GenerateOrganisationButton(Organisation org)
        {
            Button newButton = new Button();
            newButton.style.flexGrow = 1;
            newButton.style.flexDirection = FlexDirection.Row;
            newButton.name = org.id.ToString();
            newButton.style.width = 200;
            newButton.style.height = 50;

            Label buttonLabel = new Label();
            buttonLabel.name = "name";
            buttonLabel.text = org.name;
            buttonLabel.style.fontSize = 15;

            newButton.Add(buttonLabel);
            buttonLabel.AddToClassList("optiontext");

            newButton.AddToClassList("orgbtn");
            newButton.clickable.clickedWithEventInfo += OrgButtonClicked;

            return newButton;
        }

        public void OrgButtonClicked(EventBase e)
        {
            if (e.propagationPhase != PropagationPhase.AtTarget)
                return;

            var target = e.target as Button;
            foreach (var org in activeUser.organisations)
            {
                if (org.name == target.name)
                {
                    activeOrganisation = org;
                    EditorPrefs.SetInt("LootLocker.ActiveOrgID", activeOrganisation.id);
                }
            }

            PopulateList(ContentType.Games);
        }

        public void GameButtonClicked(EventBase e)
        {
            if (e.propagationPhase != PropagationPhase.AtTarget)
                return;

            var target = e.target as Button;
            foreach (var game in activeOrganisation.games)
            {
                if (game.id.ToString() == target.name)
                {
                    activeGame = game;
                    EditorPrefs.SetInt("LootLocker.ActiveGameID", activeGame.id);
                }
            }

            gameTitle.text = target.Q<Label>("name").text;
            PopulateList(ContentType.GameOptions);
            OpenAPIKeyTab();
        }
    }
}

#endif