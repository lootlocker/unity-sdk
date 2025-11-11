#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

            var content = new GUIContent();
            content.text = "API key can be found in `Settings > API Keys` in the Web Console: https://console.lootlocker.com/settings/api-keys";
            EditorGUILayout.HelpBox(content, false);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("domainKey"));
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.domainKey = m_CustomSettings.FindProperty("domainKey").stringValue;

                string domainkey = m_CustomSettings.FindProperty("domainKey").stringValue;

                string pattern = @"(?<=https://)\w+(?=\.api\.lootlocker\.io/)";
                Regex regex = new Regex(pattern);
                Match match = regex.Match(domainkey);

                if (match.Success)
                {
                    string regexKey = match.Value;
                    Debug.LogWarning("You accidentally used the domain url instead of the domain key,\nWe took the domain key from the url.: " + regexKey);
                    gameSettings.domainKey = regexKey;
                    m_CustomSettings.FindProperty("domainKey").stringValue = regexKey;
                }
                else
                {
                    gameSettings.domainKey = domainkey;
                    m_CustomSettings.FindProperty("domainKey").stringValue = domainkey;
                }

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

            if (!IsSemverString(m_CustomSettings.FindProperty("game_version").stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Game version needs to follow a numeric Semantic Versioning pattern: X.Y.Z.B with the sections denoting MAJOR.MINOR.PATCH.BUILD and the last two being optional. Read more at https://docs.lootlocker.com/the-basics/core-concepts/glossary#game-version",
                    MessageType.Warning, false);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("logLevel"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.logLevel = (LootLockerLogger.LogLevel)m_CustomSettings.FindProperty("logLevel").enumValueIndex;
            }
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("logErrorsAsWarnings"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.logErrorsAsWarnings = m_CustomSettings.FindProperty("logErrorsAsWarnings").boolValue;
            }
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("logInBuilds"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.logInBuilds = m_CustomSettings.FindProperty("logInBuilds").boolValue;
            }
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("prettifyJson"), new GUIContent("Log JSON Formatted"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.prettifyJson = m_CustomSettings.FindProperty("prettifyJson").boolValue;
            }
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("allowTokenRefresh"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.allowTokenRefresh = m_CustomSettings.FindProperty("allowTokenRefresh").boolValue; 
            }
            EditorGUILayout.Space();

#if LOOTLOCKER_ENABLE_PRESENCE
            DrawPresenceSettings();
#endif
        }

        private static bool IsSemverString(string str)
        {
            return Regex.IsMatch(str,
                @"^(0|[1-9]\d*)\.(0|[1-9]\d*)(?:\.(0|[1-9]\d*))?(?:\.(0|[1-9]\d*))?$");
        }

#if LOOTLOCKER_ENABLE_PRESENCE
        private void DrawPresenceSettings()
        {
            EditorGUILayout.LabelField("Presence Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Enable presence toggle
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("enablePresence"));
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.enablePresence = m_CustomSettings.FindProperty("enablePresence").boolValue;
            }

            if (!gameSettings.enablePresence)
            {
                EditorGUILayout.HelpBox("Presence system is disabled. Enable it to configure platform-specific settings.", MessageType.Info);
                EditorGUILayout.Space();
                return;
            }

            // Platform selection
            EditorGUI.BeginChangeCheck();
            var platformsProp = m_CustomSettings.FindProperty("enabledPresencePlatforms");
            LootLockerPresencePlatforms currentFlags = (LootLockerPresencePlatforms)platformsProp.enumValueFlag;

            // Use Unity's built-in EnumFlagsField for a much cleaner multi-select UI
            EditorGUILayout.LabelField("Enabled Platforms", EditorStyles.label);
            currentFlags = (LootLockerPresencePlatforms)EditorGUILayout.EnumFlagsField("Select Platforms", currentFlags);

            // Quick selection buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Selection", EditorStyles.label);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("All", GUILayout.Width(60)))
                {
                    currentFlags = LootLockerPresencePlatforms.AllPlatforms;
                }
                if (GUILayout.Button("Recommended", GUILayout.Width(100)))
                {
                    currentFlags = LootLockerPresencePlatforms.RecommendedPlatforms;
                }
                if (GUILayout.Button("Desktop Only", GUILayout.Width(100)))
                {
                    currentFlags = LootLockerPresencePlatforms.AllDesktop | LootLockerPresencePlatforms.UnityEditor;
                }
                if (GUILayout.Button("None", GUILayout.Width(60)))
                {
                    currentFlags = LootLockerPresencePlatforms.None;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                platformsProp.enumValueFlag = (int)currentFlags;
                gameSettings.enabledPresencePlatforms = currentFlags;
            }

            // Show warning for problematic platforms
            if ((currentFlags & LootLockerPresencePlatforms.WebGL) != 0)
            {
                EditorGUILayout.HelpBox("WebGL: WebSocket support varies by browser. Consider implementing fallback mechanisms.", MessageType.Warning);
            }
            if ((currentFlags & LootLockerPresencePlatforms.AllMobile) != 0)
            {
                EditorGUILayout.HelpBox("Mobile: WebSockets may impact battery life. Battery optimizations will disconnect/reconnect presence when app goes to background/foreground.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Mobile battery optimizations
            if ((currentFlags & LootLockerPresencePlatforms.AllMobile) != 0)
            {
                EditorGUILayout.LabelField("Mobile Battery Optimizations", EditorStyles.label);
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("enableMobileBatteryOptimizations"));
                if (EditorGUI.EndChangeCheck())
                {
                    gameSettings.enableMobileBatteryOptimizations = m_CustomSettings.FindProperty("enableMobileBatteryOptimizations").boolValue;
                }

                if (gameSettings.enableMobileBatteryOptimizations)
                {
                    EditorGUI.BeginChangeCheck();
                    
                    // Custom slider for update interval with full steps between 5-55 seconds
                    EditorGUILayout.LabelField("Mobile Presence Update Interval (seconds)");
                    float currentInterval = gameSettings.mobilePresenceUpdateInterval;
                    float newInterval = EditorGUILayout.IntSlider(
                        "Update Interval", 
                        Mathf.RoundToInt(currentInterval), 
                        5, 
                        55
                    );
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        gameSettings.mobilePresenceUpdateInterval = newInterval;
                        m_CustomSettings.FindProperty("mobilePresenceUpdateInterval").floatValue = newInterval;
                    }
                    
                    if (gameSettings.mobilePresenceUpdateInterval > 0)
                    {
                        EditorGUILayout.HelpBox($"Mobile battery optimizations enabled:\n• Presence connections will disconnect when app goes to background\n• Ping intervals set to {gameSettings.mobilePresenceUpdateInterval} seconds when active\n• Automatic reconnection when app returns to foreground", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Mobile battery optimizations enabled:\n• Presence connections will disconnect when app goes to background\n• No ping throttling (uses standard 25-second intervals)\n• Automatic reconnection when app returns to foreground", MessageType.Info);
                    }
                }

                EditorGUILayout.Space();
            }
        }
#endif

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
