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

        public static bool logSettingsFoldout = true;
        public static bool presenceSettingsFoldout = true;

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
            gameSettings = LootLockerConfig.Get();
            LootLockerConfig.RefreshFileConfigState(force: true);

            // This function is called when the user clicks on the MyCustom element in the Settings window.
            m_CustomSettings = GetSerializedSettings();

        }

        public override void OnGUI(string searchContext)
        {
            gameSettings = LootLockerConfig.Get();
            LootLockerConfig.RefreshFileConfigState();

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
#pragma warning disable 0162
            if (LootLockerConfig.PackageName != "LootLocker")
            {
                EditorGUILayout.HelpBox(LootLockerConfig.PackageName + " SDK is powered by LootLocker. Settings here configure the underlying LootLocker integration.", MessageType.Info);
                EditorGUILayout.Space();
            }
#pragma warning restore 0162

            DrawFileConfigSelection();

            if (LootLockerConfig.IsFileConfigActive)
            {
                EditorGUILayout.HelpBox("Most settings are controlled by the external config file and cannot be edited here. Game version and log settings remain editable. Edit the config file to change the other values.", MessageType.Warning);
                EditorGUILayout.Space();
            }

            DrawValidation();

            EditorGUI.BeginDisabledGroup(LootLockerConfig.IsFileConfigActive);

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
                    LootLockerLogger.Log("You accidentally used the domain url instead of the domain key,\nWe took the domain key from the url.: " + regexKey, LootLockerLogger.LogLevel.Info);
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
            EditorGUI.EndDisabledGroup();

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
            EditorGUI.BeginDisabledGroup(LootLockerConfig.IsFileConfigActive);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("allowTokenRefresh"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.allowTokenRefresh = m_CustomSettings.FindProperty("allowTokenRefresh").boolValue; 
            }
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("multiUserSessionMode"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.multiUserSessionMode = (LootLockerMultiUserSessionMode)m_CustomSettings.FindProperty("multiUserSessionMode").enumValueIndex;
            }
            EditorGUILayout.Space();
            EditorGUI.EndDisabledGroup();

            DrawLogSettings();

            EditorGUI.BeginDisabledGroup(LootLockerConfig.IsFileConfigActive);

            DrawPresenceSettings();

            EditorGUI.EndDisabledGroup();
        }

        #region File config selection

        private const string NoneOption = "(None — use Project Settings)";

        /// <summary>
        /// Draws the file config dropdown. Only shown when there is something to choose between:
        /// with zero or one config file the selection is unambiguous and a dropdown would be noise.
        /// </summary>
        private void DrawFileConfigSelection()
        {
            List<LootLockerConfig.FileConfigDescriptor> discovered = LootLockerConfig.DiscoverFileConfigsForEditor();
            string selectedId = gameSettings.selectedFileConfigId;

            if (discovered.Count == 0)
            {
                EditorGUILayout.LabelField("Config File", "No file config in use — settings below apply.");
                EditorGUILayout.Space();
                return;
            }

            if (discovered.Count == 1 && selectedId == null)
            {
                LootLockerConfig.FileConfigDescriptor only = discovered[0];
                EditorGUILayout.LabelField("Config File", $"Using {DescribeId(only.Id)}");
                EditorGUILayout.Space();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Config File", GUILayout.Width(EditorGUIUtility.labelWidth));

                var options = new List<string> { NoneOption };
                var ids = new List<string> { "" };
                int currentIndex = 0;

                foreach (LootLockerConfig.FileConfigDescriptor descriptor in discovered)
                {
                    string label = DescribeId(descriptor.Id);
                    if (descriptor.State != LootLockerConfig.FileConfigState.Valid)
                    {
                        label += $" — {descriptor.Reason}";
                    }

                    options.Add(label);
                    ids.Add(descriptor.Id);

                    if (descriptor.Id == selectedId)
                    {
                        currentIndex = options.Count - 1;
                    }
                }

                // A selection that is no longer on disk must be visible, not silently dropped.
                bool selectionMissing = !string.IsNullOrEmpty(selectedId) && !ids.Contains(selectedId);
                if (selectionMissing)
                {
                    options.Add($"Missing: {selectedId}");
                    ids.Add(selectedId);
                    currentIndex = options.Count - 1;
                }

                int newIndex = EditorGUILayout.Popup(currentIndex, options.ToArray());
                if (newIndex != currentIndex)
                {
                    ApplyFileConfigSelection(ids[newIndex]);
                }

                if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                {
                    LootLockerConfig.RefreshFileConfigState(force: true);
                }
            }

            if (!string.IsNullOrEmpty(selectedId))
            {
                bool found = discovered.Exists(d => d.Id == selectedId);
                if (!found)
                {
                    EditorGUILayout.HelpBox(
                        $"The selected config '{selectedId}' was not found. Expected Resources/Config/{LootLockerConfig.GetConfigFileNameForId(selectedId)}.bytes.",
                        MessageType.Error);
                }
                else
                {
                    EditorGUILayout.LabelField("", $"Using Resources/Config/{LootLockerConfig.GetConfigFileNameForId(selectedId)}.bytes", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space();
        }

        private static string DescribeId(string id)
        {
            return string.IsNullOrEmpty(id) ? "Default (LootLockerConfig.bytes)" : id;
        }

        private static void ApplyFileConfigSelection(string id)
        {
            gameSettings.selectedFileConfigId = id;
            EditorUtility.SetDirty(gameSettings);
            AssetDatabase.SaveAssets();

            LootLockerConfig.RefreshFileConfigState(force: true);
        }

        #endregion

        private static bool IsSemverString(string str)
        {
            return Regex.IsMatch(str,
                @"^(0|[1-9]\d*)\.(0|[1-9]\d*)(?:\.(0|[1-9]\d*))?(?:\.(0|[1-9]\d*))?$");
        }

        #region Validation

        private static LootLockerConfigValidationResult? validationResult;
        private static bool validationInProgress;

        /// <summary>
        /// Draws the "Validate" affordance, which proves the currently effective API key works and
        /// shows which game it belongs to.
        ///
        /// This validates whatever is currently applied (LootLockerConfig.current.apiKey), not an
        /// arbitrary unselected config.
        /// </summary>
        private void DrawValidation()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Connection", GUILayout.Width(EditorGUIUtility.labelWidth));

                using (new EditorGUI.DisabledScope(validationInProgress || string.IsNullOrEmpty(gameSettings?.apiKey)))
                {
                    if (GUILayout.Button(validationInProgress ? "Validating..." : "Validate", GUILayout.Width(100)))
                    {
                        ValidateCurrentConfig();
                    }
                }

                DrawValidationResult();
            }

            EditorGUILayout.Space();
        }

        private static void DrawValidationResult()
        {
            if (!validationResult.HasValue)
            {
                return;
            }

            LootLockerConfigValidationResult result = validationResult.Value;
            switch (result.Outcome)
            {
                case LootLockerConfigValidationOutcome.Success:
                    EditorGUILayout.LabelField(
                        $"\u2713 {result.GameName} (title {result.TitleId}, env {result.EnvironmentId})",
                        EditorStyles.miniLabel);
                    break;
                case LootLockerConfigValidationOutcome.KeyRejected:
                    EditorGUILayout.LabelField($"\u2717 {result.Message}", EditorStyles.miniLabel);
                    break;
                case LootLockerConfigValidationOutcome.Unreachable:
                    EditorGUILayout.LabelField($"! {result.Message}", EditorStyles.miniLabel);
                    break;
                default:
                    EditorGUILayout.LabelField(result.Message, EditorStyles.miniLabel);
                    break;
            }
        }

        private static void ValidateCurrentConfig()
        {
            string apiKey = LootLockerConfig.current?.apiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                validationResult = LootLockerConfigValidation.Classify(null, apiKey);
                return;
            }

            validationInProgress = true;
            validationResult = null;

            LootLocker.Requests.LootLockerSDKManager.GetGameInfo(response =>
            {
                validationResult = LootLockerConfigValidation.Classify(response, apiKey);
                validationInProgress = false;
            });
        }

        #endregion

        private void DrawLogSettings()
        {
#if UNITY_2019_3_OR_NEWER
            logSettingsFoldout = EditorGUILayout.Foldout(logSettingsFoldout, "Log Settings", true, EditorStyles.foldoutHeader);
#else
            logSettingsFoldout = EditorGUILayout.Foldout(logSettingsFoldout, "Log Settings", true, EditorStyles.foldout);
#endif
            if (!logSettingsFoldout) return;
            EditorGUILayout.Space();

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
            EditorGUI.BeginDisabledGroup(LootLockerConfig.IsFileConfigActive);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("prettifyJson"), new GUIContent("Log JSON Formatted"));

            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.prettifyJson = m_CustomSettings.FindProperty("prettifyJson").boolValue;
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
        }

        private void DrawPresenceSettings()
        {
#if UNITY_2019_3_OR_NEWER
            presenceSettingsFoldout = EditorGUILayout.Foldout(presenceSettingsFoldout, "Presence Settings", true, EditorStyles.foldoutHeader);
#else
            presenceSettingsFoldout = EditorGUILayout.Foldout(presenceSettingsFoldout, "Presence Settings", true, EditorStyles.foldout);
#endif
            if (!presenceSettingsFoldout) return;
            EditorGUILayout.Space();

            if(gameSettings.enablePresence) 
            {
                EditorGUILayout.HelpBox("This may incur additional costs and needs to be enabled for your game. \nContact us to enable presence features.", MessageType.Info);
                EditorGUILayout.Space();
            }

            // Enable presence toggle
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("enablePresence"));
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.enablePresence = m_CustomSettings.FindProperty("enablePresence").boolValue;
            }
            EditorGUILayout.Space();
            
            // Auto-connect toggle
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("enablePresenceAutoConnect"), new GUIContent("Auto Connect", "Automatically connect presence when sessions are started. Once connected (automatically or manually), presence is maintained until explicitly disconnected."));
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.enablePresenceAutoConnect = m_CustomSettings.FindProperty("enablePresenceAutoConnect").boolValue;
            }
            
            // Auto-disconnect on focus change toggle
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("enablePresenceAutoDisconnectOnFocusChange"), new GUIContent("Auto Pause Presence"));
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.enablePresenceAutoDisconnectOnFocusChange = m_CustomSettings.FindProperty("enablePresenceAutoDisconnectOnFocusChange").boolValue;
            }

            EditorGUILayout.Space();
            
            // Enable presence in editor toggle
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("enablePresenceInEditor"), new GUIContent("Enable Presence in Editor"));
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.enablePresenceInEditor = m_CustomSettings.FindProperty("enablePresenceInEditor").boolValue;
            }

            EditorGUILayout.Space();
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new ProjectSettings("Project/" + LootLockerConfig.PackageName + " SDK", SettingsScope.Project)
            {
                label = LootLockerConfig.PackageName + " SDK"
            };
        }
    }
}
#endif
