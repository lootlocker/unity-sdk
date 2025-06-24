#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LootLocker.LogViewer;

namespace LootLocker.Extension
{
    public class LootLockerLogViewerWindow : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset;
        private LogViewerUI logViewerUI;

        [MenuItem("Window/LootLocker/Log Viewer")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<LootLockerLogViewerWindow>();
            wnd.titleContent = new GUIContent("LootLocker Log Viewer");
            wnd.Show();
        }

        public void CreateGUI()
        {
            VisualTreeAsset visualTree = m_VisualTreeAsset;
            if (visualTree == null)
            {
                // Try to load from Assets/LootLockerSDK
                visualTree = EditorGUIUtility.Load("Assets/LootLockerSDK/Runtime/Editor/LogViewer/LootLockerLogViewerWindow.uxml") as VisualTreeAsset;
            }
            if (visualTree == null)
            {
                // Try to load from Assets/LootLocker
                visualTree = EditorGUIUtility.Load("Assets/LootLocker/Runtime/Editor/LogViewer/LootLockerLogViewerWindow.uxml") as VisualTreeAsset;
            }
            if (visualTree == null)
            {
                // Try to load from Packages
                visualTree = EditorGUIUtility.Load("Packages/com.lootlocker.lootlockersdk/Runtime/Editor/LogViewer/LootLockerLogViewerWindow.uxml") as VisualTreeAsset;
            }
            if (visualTree == null)
            {
                Debug.LogError("LootLockerLogViewerWindow: LootLocker not found in `Assets/LootLocker` or in Packages. Non standard install locations not supported.");
                return;
            }
            var root = rootVisualElement;
            root.Clear();
            var uxml = visualTree.Instantiate();
            root.Add(uxml);
            if (logViewerUI == null)
                logViewerUI = new LogViewerUI();
            logViewerUI.InitializeLogViewerUI(root);
        }

        public void OnDestroy()
        {
            if (logViewerUI != null)
            {
                logViewerUI.Dispose();
                logViewerUI = null;
            }
        }
    }
}
#endif
