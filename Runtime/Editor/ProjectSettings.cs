#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LootLocker.Admin
{
    public class ProjectSettings : SettingsProvider
    {
        private static LootLockerConfig gameSettings;
        private SerializedObject m_CustomSettings;

        public delegate void SendAttributionDelegate();
        public static event SendAttributionDelegate APIKeyEnteredEvent;
        internal static SerializedObject GetSerializedSettings()
        {
            if (gameSettings == null)
            {
                gameSettings = LootLockerConfig.Get();
            }
            return new SerializedObject(gameSettings);
        }
        public ProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            if (gameSettings == null)
            {
                gameSettings = LootLockerConfig.Get();
            }
            // This function is called when the user clicks on the MyCustom element in the Settings window.
            m_CustomSettings = GetSerializedSettings();

        }

        public override void OnGUI(string searchContext)
        {
            if (gameSettings == null)
            {
                gameSettings = LootLockerConfig.Get();
            }
            m_CustomSettings.Update();

            // For Unity Attribution
            if (string.IsNullOrEmpty(gameSettings.apiKey) == false && gameSettings.apiKey.Length > 20)
            {
                if (EditorPrefs.GetBool("attributionChecked") == false)
                {
                    EditorPrefs.SetBool("attributionChecked", true);
                    APIKeyEnteredEvent?.Invoke();
                }
            }

            if (gameSettings == null)
            {
                gameSettings = LootLockerConfig.Get();
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(6f);

                using (new GUILayout.VerticalScope())
                {
                    DrawGameSettings();
                }
            }
            m_CustomSettings.ApplyModifiedProperties();
        }

        private void DrawGameSettings()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("apiKey"));
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.apiKey = m_CustomSettings.FindProperty("apiKey").stringValue;
            }

            bool deprecatedApiKey = !gameSettings.IsPrefixedApiKey();
            if (deprecatedApiKey)
            {
                EditorGUILayout.HelpBox(
                    "WARNING: this is a legacy API Key, please visit https://console.lootlocker.com/settings/api-keys and generate a new one",
                    MessageType.Warning, false);
            }

            var content = new GUIContent();
            content.text = "API key can be found in `Settings > API Keys` in the Web Console: https://console.lootlocker.com/settings/api-keys";
            EditorGUILayout.HelpBox(content, false);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("domainKey"));
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.domainKey = m_CustomSettings.FindProperty("domainKey").stringValue;
            }
            var domainContent = new GUIContent();
            domainContent.text = "Domain key can be found in `Settings > API Keys` in the Web Console: https://console.lootlocker.com/settings/api-keys";
            EditorGUILayout.HelpBox(domainContent, false);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("game_version"));
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.game_version = m_CustomSettings.FindProperty("game_version").stringValue;
            }
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("currentDebugLevel"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.currentDebugLevel = (LootLockerConfig.DebugLevel)m_CustomSettings.FindProperty("currentDebugLevel").enumValueIndex;
            }
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("allowTokenRefresh"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.allowTokenRefresh = m_CustomSettings.FindProperty("allowTokenRefresh").boolValue; 
            }
            EditorGUILayout.Space();

            if (deprecatedApiKey)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("developmentMode"));

                if (EditorGUI.EndChangeCheck())
                {
                    gameSettings.developmentMode = m_CustomSettings.FindProperty("developmentMode").boolValue;
                }

                EditorGUILayout.Space();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new ProjectSettings("Project/LootLocker SDK", SettingsScope.Project)
            {
                label = "LootLocker SDK"
            };
        }
    }
}
#endif
