using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LootLocker;
using LootLocker.Requests;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER

using LootLocker.Extension;
using LootLocker.Extension.DataTypes;
using LootLocker.Extension.Responses;

public class LootLockerAdminExtension : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    StyleColor stage;
    StyleColor live;

    User activeUser;

    Game activeGame;

    private VisualElement activeFlow;

    private VisualElement loginFlow, mfaFlow, gameSelectorFlow, apiKeyFlow;

    private Button changeGameBtn;

    private VisualElement environmentBackground, environmentHandle;

    private Label gameName;
    private Label activeKey;

    private Foldout menu;
    private VisualElement menuPop;

    private VisualElement popup;

    private Label popupTitle, popupMessage;

    private Button popupBtn;
    private Button menuAPIKeyBtn;
    private Button menuChangeGameBtn;
    private Button menuLogoutBtn;

    private Label infoText;

    private Label newApiKeyCancel;
    private VisualElement newApiKeyWindow;
    private TextField newApiKeyName;

    private Button createNewApiKeyBtn;

    private Button createApiKeyBtn;

    //Login Flow Start
    private TextField emailField, passwordField;
    private Label signupLink, introGuideLink, gettingStartedLink, forgotPasswordLink;
    private Button loginBtn;
    //Login Flow End

    //MFA Flow Begin

    private TextField codeField;
    private Button signInBtn;
    private string mfaKey;
    //MFA Flow End

    //Game Selector Flow Begin

    VisualElement gameSelectorList;
    //Game Selector Flow End

    //API Key Flow Begin
    VisualElement apiKeyList;

    bool isStage = true;

    [MenuItem("Window/LootLocker Extension")]
    public static void ShowExample()
    {
        LootLockerAdminExtension wnd = GetWindow<LootLockerAdminExtension>();
        wnd.titleContent = new GUIContent("LootLockerAdminExtension");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        live.value = new Color(0.749f, 0.325f, 0.098f, 1);
        stage.value = new Color(0.094f, 0.749f, 0.352f, 1);

        menuLogoutBtn = root.Q<Button>("LogoutBtn");

        menuLogoutBtn.style.display = DisplayStyle.None;

        environmentBackground = root.Q<VisualElement>("SwitchBackground");

        environmentBackground.AddManipulator(new Clickable(evt => SwapEnvironment()));

        environmentHandle = root.Q<VisualElement>("Handle");

        environmentHandle.AddManipulator(new Clickable(evt => SwapEnvironment()));

        environmentBackground.tooltip = "Stage";

        gameName = root.Q<Label>("GameName");

        activeKey = root.Q<Label>("ActiveKey");

        if(LootLockerConfig.current.apiKey != null || LootLockerConfig.current.apiKey != "")
        {
            activeKey.text = "Active Key: " + LootLockerConfig.current.apiKey;
        } else
        {
            activeKey.text = "Active Key: Not selected";
        }

        menu = root.Q<Foldout>("Menu");
        menuPop = root.Q<VisualElement>("MenuOut");

        changeGameBtn = root.Q<Button>("ChangeGameBtn");

        infoText = root.Q<Label>("LearnMore");

        infoText.style.display = DisplayStyle.None;

        changeGameBtn.clickable.clicked += () =>
        {
            menu.style.display = DisplayStyle.None;
            menuPop.style.display = DisplayStyle.None;
            SwapFlows(activeFlow, gameSelectorFlow);
        };

        menuPop.style.display = DisplayStyle.None;

        menu.style.display = DisplayStyle.None;

        menu.RegisterValueChangedCallback(e =>
        {
            if (menu.value)
            {
                menuPop.style.display = DisplayStyle.Flex;
            }
            else
            {
                menuPop.style.display = DisplayStyle.None;
            }
        });

        popup = root.Q<VisualElement>("PopUp");

        popupTitle = root.Q<Label>("popupTitle");
        popupMessage = root.Q<Label>("popupMessage");

        popupBtn = root.Q<Button>("popupCloseBtn");

        popupBtn.clickable.clickedWithEventInfo += ClosePopup;

        menuAPIKeyBtn = root.Q<Button>("APIKeyBtn");
        menuChangeGameBtn = root.Q<Button>("ChangeGameBtn");

        //Login Flow Start

        loginFlow = root.Q<VisualElement>("LoginFlow");

        emailField = root.Q<TextField>("EmailField");
        passwordField = root.Q<TextField>("PasswordField");

        signupLink = root.Q <Label>("newUserLink");
        gettingStartedLink = root.Q<Label>("gettingStartedLink");
        introGuideLink = root.Q<Label>("introGuideLink");
        forgotPasswordLink = root.Q<Label>("forgotPasswordLink");

        loginBtn = root.Q<Button>("LoginBtn");

        signupLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://lootlocker.com/sign-up"));
        forgotPasswordLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://console.lootlocker.com/forgot-password"));
        gettingStartedLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://docs.lootlocker.com/the-basics/readme"));
        introGuideLink.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://lootlocker.com")); ; //TODO

        loginBtn.clickable.clickedWithEventInfo += Login;

        loginFlow.style.display = DisplayStyle.Flex;
        //Login Flow End

        //MFA Flow Begin

        mfaFlow = root.Q<VisualElement>("MFAFlow");

        codeField = root.Q<TextField>("CodeField");

        signInBtn = root.Q<Button>("SignInBtn");

        signInBtn.clickable.clickedWithEventInfo += SignIn;

        mfaFlow.style.display = DisplayStyle.None;
        //MFA Flow End


        //Game Selector Flow Begin

        gameSelectorFlow = root.Q<VisualElement>("GameSelectorFlow");

        gameSelectorList = root.Q<VisualElement>("GamesList");

        gameSelectorFlow.style.display = DisplayStyle.None;

        //Game Selector Flow End

        //API Key Flow Begin
        apiKeyFlow = root.Q<VisualElement>("APIKeyFlow");

        apiKeyFlow.style.display = DisplayStyle.None;

        apiKeyList = root.Q<VisualElement>("APIKeyList");

        newApiKeyWindow = root.Q<VisualElement>("CreateAPIKeyWindow");

        newApiKeyName = root.Q<TextField>("newApiKeyName");

        newApiKeyCancel = root.Q<Label>("APINewKeyCancel");

        newApiKeyCancel.RegisterCallback<MouseDownEvent>(_ => {

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
            menuPop.style.display = DisplayStyle.None;
            menu.value = false;
            newApiKeyWindow.style.display = DisplayStyle.Flex;
        };

        //API Key Flow End


        if (StoredUser.current.user != null)
        {
            LootLockerConfig.current.adminToken = EditorPrefs.GetString("LootLocker.AdminToken");
            activeUser = StoredUser.current.user;
            activeGame = StoredUser.current.lastActiveGame;
            gameName.text = activeGame.name;
            menuLogoutBtn.style.display = DisplayStyle.Flex;
            SetAPIKeys(activeGame.development.id);
            SwapFlows(loginFlow, apiKeyFlow);
        }

    }

    void SwapEnvironment()
    {
       
        isStage = !isStage;
        if (isStage)
        {
            environmentHandle.style.alignSelf = Align.FlexStart;
            environmentBackground.style.backgroundColor = stage;
            environmentBackground.tooltip = "Stage";
            SetAPIKeys(activeGame.development.id);
        }
        else
        {
            environmentHandle.style.alignSelf = Align.FlexEnd;
            environmentBackground.style.backgroundColor = live;
            environmentBackground.tooltip = "Live";
            SetAPIKeys(activeGame.id);
        }
    }

    void SwapFlows(VisualElement old, VisualElement New)
    {
        if(old == New) return;

        activeFlow = New;

        old.style.display = DisplayStyle.None;
        New.style.display = DisplayStyle.Flex;

        menu.value = false;

        if(old == apiKeyFlow)
        {
            apiKeyList.Clear();
        }

        if (activeFlow == gameSelectorFlow)
        {
            gameName.text = "LootLocker";
            menuAPIKeyBtn.style.display = DisplayStyle.None;
            menuChangeGameBtn.style.display = DisplayStyle.None;
            infoText.style.display = DisplayStyle.None;
            CreateGameButtons();

        }
        if (activeFlow == apiKeyFlow)
        {
            menu.style.display = DisplayStyle.Flex;
            menuAPIKeyBtn.style.display = DisplayStyle.Flex;
            menuChangeGameBtn.style.display = DisplayStyle.Flex;
            infoText.style.display = DisplayStyle.Flex;
        }

    }

    void CheckMenuItems()
    {
        
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

    //Login Flow Start
    public void Login(EventBase e)
    {

        if (string.IsNullOrEmpty(emailField.value) || string.IsNullOrEmpty(passwordField.value))
        {
            ShowPopup("Error", "Email or Password is empty.");
            return;
        }

        EditorApplication.update += OnEditorUpdate;

        LootLockerAdminManager.AdminLogin(emailField.value, passwordField.value, (onComplete) =>
        {
            if (!onComplete.success)
            {
                ShowPopup("Error", "We couldn't recognize your information or there is no user with this email, please check and try again!");
                return;
            }

            if (onComplete.success)
            {

                if (onComplete.mfa_key != null)
                {
                    mfaKey = onComplete.mfa_key;
                    menu.style.display = DisplayStyle.Flex;
                    SwapFlows(loginFlow, mfaFlow);
                }
                else
                {
                    menu.style.display = DisplayStyle.Flex;
                    LootLockerConfig.current.adminToken = onComplete.auth_token;
                    EditorPrefs.SetString("LootLocker.AdminToken", onComplete.auth_token);

                    activeUser = onComplete.user;
                    StoredUser.CreateNewUser(activeUser);
                    
                    SwapFlows(loginFlow, gameSelectorFlow);
                }
            }
            menuLogoutBtn.style.display = DisplayStyle.Flex;
            EditorApplication.update -= OnEditorUpdate;
        });
    }

    //Login Flow End

    //MFA Flow Start

    public void SignIn(EventBase e)
    {
        EditorApplication.update += OnEditorUpdate;

        LootLockerAdminManager.MFAAuthenticate(mfaKey, codeField.value, (onComplete) =>
        {
            if (!onComplete.success)
            {
                ShowPopup("Error", "Could not authenticate MFA!");

            }

            LootLockerConfig.current.adminToken = onComplete.auth_token;
            activeUser = onComplete.user;

            StoredUser.CreateNewUser(activeUser);
            SwapFlows(mfaFlow, gameSelectorFlow);

            EditorApplication.update -= OnEditorUpdate;
        });

    }
    //MFA Flow End

    //Game Selector Flow Start
    public void CreateGameButtons()
    {
    
        foreach(var org in activeUser.organisations)
        {
            foreach(var game in org.games)
            {
                GameButtonTemplate(game, org.name);
            }
        }

    }

    public void GameButtonTemplate(Game game, string orgName)
    {
        //Parent
        Button button = new Button();

        button.style.flexDirection = FlexDirection.Column;
        button.name = game.id.ToString();

        button.AddToClassList("gameButton");

        //Game title
        Label gameTitle = new Label();
        gameTitle.text = game.name;

        gameTitle.AddToClassList("gameButtonTitle");

        //Org title
        Label gameOrg = new Label();
        gameOrg.text = orgName;

        gameOrg.AddToClassList("gameButtonOrgTitle");

        button.Add(gameTitle);
        button.Add(gameOrg);

        button.clickable.clickedWithEventInfo += OnGameSelected;

        gameSelectorList.Add(button);
    }

    void OnGameSelected(EventBase e)
    {
        if (e.propagationPhase != PropagationPhase.AtTarget)
            return;

        var target = e.target as Button;

        bool hasFoundGame = false;

        foreach(var org in activeUser.organisations)
        {
            foreach(var game in org.games)
            {
                if(target.name == game.id.ToString())
                {
                    activeGame = game;
                    StoredUser.current.lastActiveGame = activeGame;
                    hasFoundGame = true;
                }
            }
        }

        if(hasFoundGame)
        {
            gameName.text = activeGame.name;
            SwapFlows(gameSelectorFlow, apiKeyFlow);
            SetAPIKeys(activeGame.development.id);
        }

    }

    //Game Selector Flow End

    //API Key Flow Start


    void CreateNewAPIKey()
    {

        string gameId = activeGame.id.ToString();
        if (isStage)
        {
            gameId = activeGame.development.id.ToString();
        }

        EditorApplication.update += OnEditorUpdate;

        LootLockerAdminManager.GenerateKey(gameId, newApiKeyName.value, "game", (onComplete) =>
        {
            if (!onComplete.success)
            {
                ShowPopup("Error", "Could not create a new API Key!");

            }

            APIKeyTemplate(onComplete);

        });

        EditorApplication.update -= OnEditorUpdate;


        newApiKeyName.value = "";

    }

    void SetAPIKeys(int gameID)
    {

        apiKeyList.Clear();

        EditorApplication.update += OnEditorUpdate;


        LootLockerAdminManager.GetAllKeys(gameID.ToString(), (onComplete) =>
        {
            if(!onComplete.success)
            {
                ShowPopup("Error", "Could not find API Keys!");

            }

            foreach (var key in onComplete.api_keys)
            {
                APIKeyTemplate(key);
            }
        });

        EditorApplication.update -= OnEditorUpdate;

    }

    void APIKeyTemplate(KeyResponse key)
    {
        Button button = new Button();

        button.name = key.api_key;

        button.AddToClassList("apikey");

        Label keyName = new Label();
        keyName.text = key.name;
        if(!string.IsNullOrEmpty(key.name))
        {
            keyName.text += "  -  ";
        }
        keyName.AddToClassList("apikeyName");

        Label apiKey = new Label();
        apiKey.text = key.api_key;
        apiKey.AddToClassList("apikeyKey");
    
        button.Add(keyName); 
        button.Add(apiKey);

        button.clickable.clickedWithEventInfo += OnAPIKeySelected;

        apiKeyList.Add(button);

    }

    void OnAPIKeySelected(EventBase e)
    {
        if (e.propagationPhase != PropagationPhase.AtTarget)
            return;

        var target = e.target as Button;

        LootLockerConfig.current.apiKey = target.name;

        activeKey.text = "Active Key: " + LootLockerConfig.current.apiKey;


    }

}
#endif