#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LootLocker.Admin
{
    public class ProjectSettings : SettingsProvider
    {
        private LootLockerConfig gameSettings;
        public ProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
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
        }

        private void DrawGameSettings()
        {
            string apiKey = gameSettings.apiKey;
            EditorGUI.BeginChangeCheck();
            apiKey = EditorGUILayout.TextField("API Key", apiKey);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.apiKey = apiKey;
            }

            string gameVersion = gameSettings.game_version;
            EditorGUI.BeginChangeCheck();
            gameVersion = EditorGUILayout.TextField("Game Version", gameVersion);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.game_version = gameVersion;
            }

            LootLockerConfig.platformType platform = gameSettings.platform;
            EditorGUI.BeginChangeCheck();
            platform = (LootLockerConfig.platformType)EditorGUILayout.EnumPopup("Platform", platform);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.platform = platform;
            }

            bool onDevelopmentMode = gameSettings.developmentMode;
            EditorGUI.BeginChangeCheck();
            onDevelopmentMode = EditorGUILayout.Toggle("On Development Mode", onDevelopmentMode);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.developmentMode = onDevelopmentMode;
            }

            LootLockerConfig.DebugLevel debugLevel = gameSettings.currentDebugLevel;
            EditorGUI.BeginChangeCheck();
            debugLevel = (LootLockerConfig.DebugLevel)EditorGUILayout.EnumPopup("Current Debug Level", debugLevel);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.currentDebugLevel = debugLevel;
            }

            bool allowTokenRefresh = gameSettings.allowTokenRefresh;
            EditorGUI.BeginChangeCheck();
            allowTokenRefresh = EditorGUILayout.Toggle("Allow Token Refresh", allowTokenRefresh);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.allowTokenRefresh = allowTokenRefresh;
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
