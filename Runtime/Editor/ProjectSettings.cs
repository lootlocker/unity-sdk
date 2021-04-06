#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LootLocker.Admin
{
    public class ProjectSettings : SettingsProvider
    {
        private LootLockerConfig gameSettings;
        private LootLockerEndPoints endPoints;
        private SerializedObject endPointsSerialized;
        public ProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
            if (gameSettings == null)
            {
                gameSettings = LootLockerConfig.Get();
            }
            if (endPointsSerialized == null)
            {
                endPoints = LootLockerEndPoints.Get();
                endPointsSerialized = new SerializedObject(endPoints);
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
                gameSettings.EditorSave();
            }

            string gameVersion = gameSettings.game_version;
            EditorGUI.BeginChangeCheck();
            gameVersion = EditorGUILayout.TextField("Game Version", gameVersion);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.game_version = gameVersion;
                gameSettings.EditorSave();
            }

            LootLockerConfig.platformType platform = gameSettings.platform;
            EditorGUI.BeginChangeCheck();
            platform = (LootLockerConfig.platformType)EditorGUILayout.EnumPopup("Platform", platform);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.platform = platform;
                gameSettings.EditorSave();
            }

            LootLockerConfig.environmentType environment = gameSettings.environment;
            EditorGUI.BeginChangeCheck();
            environment = (LootLockerConfig.environmentType)EditorGUILayout.EnumPopup("Environment", environment);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.environment = environment;
                gameSettings.EditorSave();
            }

            LootLockerConfig.DebugLevel debugLevel = gameSettings.currentDebugLevel;
            EditorGUI.BeginChangeCheck();
            debugLevel = (LootLockerConfig.DebugLevel)EditorGUILayout.EnumPopup("Current Debug Level", debugLevel);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.currentDebugLevel = debugLevel;
                gameSettings.EditorSave();
            }

            bool allowTokenRefresh = gameSettings.allowTokenRefresh;
            EditorGUI.BeginChangeCheck();
            allowTokenRefresh = EditorGUILayout.Toggle("Allow Token Refresh", allowTokenRefresh);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.allowTokenRefresh = allowTokenRefresh;
                gameSettings.EditorSave();
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
