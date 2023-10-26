using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LootLocker;
#if (UNITY_EDITOR) && UNITY_2021_3_OR_NEWER && LOOTLOCKER_ENABLE_EXTENSION

namespace LootLocker.Extension
{
public class LootLockerWizard : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    private TextField i_email, i_password;
    private Label l_signup, l_forgotpassword; 
    private Button btn_login;
    
    public static void LoadLogin()
    {
        LootLockerWizard wnd = GetWindow<LootLockerWizard>();
        wnd.titleContent = new GUIContent("LootLocker"); 
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
        
        i_email = root.Q<TextField>("inputemail");
        i_password = root.Q<TextField>("inputpassword");

        btn_login = root.Q<Button>("btnlogin");

        l_signup = root.Q<Label>("hyperlinksignup");
        l_forgotpassword = root.Q<Label>("hyperlinkforgotpass");
        
        l_signup.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL("https://lootlocker.com/sign-up"));
        l_forgotpassword.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL(("https://console.lootlocker.com/forgot-password")));
        
        btn_login.clickable.clickedWithEventInfo += AdminExtensionLogin;
    }
    
    
    public void AdminExtensionLogin(EventBase e)
    {
        if (!LootLockerServerManager.I)
        {
            Debug.Log("SDK Not initialized");
            return;
        }

        EditorApplication.update += OnEditorUpdate;

        LootLockerAdminManager.AdminLogin(i_email.value, i_password.value, (onComplete) =>
        {
            if (onComplete.success)
            {
                LootLockerConfig.current.adminToken = onComplete.auth_token;
                EditorPrefs.SetString("LootLocker.AdminToken", onComplete.auth_token);

                if (onComplete.mfa_key != null)
                {
                    EditorPrefs.SetString("LootLocker.mfaKey", onComplete.mfa_key);
                    LootLockerMFA.LoadMFAWindow();
                    Close();
                }
                else
                {
                    LootLockerMainWindow wnd = GetWindow<LootLockerMainWindow>();
                    wnd.titleContent = new GUIContent("LootLocker");
                    wnd.LoadLootLockerMainMenu(onComplete.user);
                    Close();
                }
            }
            EditorApplication.update -= OnEditorUpdate;
        });
    }

    private void OnEditorUpdate()
    {
        EditorApplication.QueuePlayerLoopUpdate();
    }
}    
    
}

#endif