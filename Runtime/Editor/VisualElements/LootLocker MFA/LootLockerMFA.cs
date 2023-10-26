using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LootLocker;
#if UNITY_2021_3_OR_NEWER && UNITY_EDITOR && LOOTLOCKER_ENABLE_EXTENSION

namespace LootLocker.Extension 
{
public class LootLockerMFA : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    private Button btnsend;

    private TextField i_secretcode;
    
    public static void LoadMFAWindow()
    {
        LootLockerMFA wnd = GetWindow<LootLockerMFA>();
        wnd.titleContent = new GUIContent("MFA Verification");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        btnsend = root.Q<Button>("btnsend");

        i_secretcode = root.Q<TextField>("inputmfa");
        
        btnsend.clickable.clickedWithEventInfo += RunMFA;

    }

    public void RunMFA(EventBase e)
    {
        EditorApplication.update = OnEditorUpdate;

        LootLockerAdminManager.MFAAuthenticate(EditorPrefs.GetString("LootLocker.mfaKey"), i_secretcode.value, (onComplete) =>
        {
            if (onComplete.success)
            {
                LootLockerConfig.current.adminToken = onComplete.auth_token;
                EditorPrefs.SetString("LootLocker.AdminToken", onComplete.auth_token);

                LootLockerMainWindow wnd = GetWindow<LootLockerMainWindow>();
                wnd.titleContent = new GUIContent("LootLocker");
                wnd.LoadLootLockerMainMenu(onComplete.user);
                EditorPrefs.DeleteKey("LootLocker.mfaKey");
                Close();
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