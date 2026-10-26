#if UNITY_EDITOR
using System;
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
            InvalidateDiscoveredConfigs();

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

            DrawFileConfigSection();

            if (LootLockerConfig.IsFileConfigActive)
            {
                EditorGUILayout.HelpBox("Settings are controlled by the external config file. Click the closed padlock next to a setting to override it for this machine only, or edit the config file to change it for everyone.", MessageType.Warning);                EditorGUILayout.Space();
            }

            DrawValidation();

            DrawOverridableField("apiKey", "API Key", LootLockerConfigFields.ApiKey,
                () => gameSettings.apiKey = m_CustomSettings.FindProperty("apiKey").stringValue);

            var content = new GUIContent();
            content.text = "API key can be found in `Settings > API Keys` in the Web Console: https://console.lootlocker.com/settings/api-keys";
            EditorGUILayout.HelpBox(content, false);
            EditorGUILayout.Space();

            DrawOverridableField("domainKey", "Domain Key", LootLockerConfigFields.DomainKey, () =>
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
            });
            var domainContent = new GUIContent();
            domainContent.text = "Domain key can be found in `Settings > API Keys` in the Web Console: https://console.lootlocker.com/settings/api-keys";
            EditorGUILayout.HelpBox(domainContent, false);
            EditorGUILayout.Space();

            // Game version is never governed by a file config, so it has no override affordance.
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("game_version"), new GUIContent("Game Version"));
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

            DrawOverridableField("allowTokenRefresh", "Allow Token Refresh", LootLockerConfigFields.AllowTokenRefresh,
                () => gameSettings.allowTokenRefresh = m_CustomSettings.FindProperty("allowTokenRefresh").boolValue);
            EditorGUILayout.Space();

            DrawOverridableField("multiUserSessionMode", "Multi User Session Mode", LootLockerConfigFields.MultiUserSessionMode,
                () => gameSettings.multiUserSessionMode = (LootLockerMultiUserSessionMode)m_CustomSettings.FindProperty("multiUserSessionMode").enumValueIndex);
            EditorGUILayout.Space();

            DrawLogSettings();

            DrawPresenceSettings();
        }

        #region File config selection

        private const string NoneOption = "(None — use Project Settings)";

        private static GUIContent _lockedIcon;
        private static GUIContent _unlockedIcon;

        /// <summary>
        /// Builds a label for a setting, appending a marker when the current value came from the
        /// gitignored local override file.
        ///
        /// Without this marker a developer cannot tell a shared value from one they changed
        /// locally, which is exactly the confusion the override file would otherwise introduce.
        /// </summary>
        private static GUIContent Label(string text, string fieldName)
        {
            if (!LootLockerConfig.IsLocallyOverridden(fieldName))
            {
                return new GUIContent(text);
            }

            return new GUIContent($"{text} (local override)",
                "This value comes from the gitignored local override file, not from the shared config. Click the open padlock to reset it.");
        }

        /// <summary>
        /// Draws one setting together with its local override affordance.
        ///
        /// The control is editable when the value is not governed by a file config, or when the
        /// developer has explicitly overridden it. Editing an overridden field writes straight
        /// through to the local override file, so what is on screen is always what is in effect.
        /// </summary>
        /// <param name="propertyName">Serialized property name on the config asset.</param>
        /// <param name="label">Label text.</param>
        /// <param name="fieldName">JSON field name, see <see cref="LootLockerConfigFields"/>.</param>
        /// <param name="onChanged">Applies the edited value to the live config.</param>
        private void DrawOverridableField(string propertyName, string label, string fieldName, Action onChanged)
        {
            bool overridden = LootLockerConfig.IsLocallyOverridden(fieldName);
            bool locked = LootLockerConfig.IsFileConfigActive && !overridden;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(locked))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_CustomSettings.FindProperty(propertyName), Label(label, fieldName));
                    if (EditorGUI.EndChangeCheck())
                    {
                        onChanged?.Invoke();

                        // An overridden field is written straight back to the override file, so the
                        // value survives a domain reload and stays in effect for this machine only.
                        if (overridden)
                        {
                            PersistLocalOverrideField(fieldName, m_CustomSettings.FindProperty(propertyName));
                        }
                    }
                }

                if (LootLockerConfig.IsFileConfigActive)
                {
                    if (GUILayout.Button(LockToggleContent(overridden), GUILayout.Width(24), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    {
                        if (overridden)
                        {
                            ResetLocalOverrideField(fieldName);
                        }
                        else
                        {
                            OverrideField(fieldName, m_CustomSettings.FindProperty(propertyName));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Content for the per-field local override toggle.
        ///
        /// The padlock shows the state rather than the action - closed while the shared value is
        /// in effect, open once this machine has its own value - so a single compact button both
        /// explains why a field is read-only and toggles it. Text buttons were used before but
        /// they cost 56-70px on every governed setting, which is a lot of width for a rarely used
        /// affordance.
        /// </summary>
        private static GUIContent LockToggleContent(bool overridden)
        {
            if (overridden)
            {
                if (_unlockedIcon == null)
                {
                    _unlockedIcon = BuildIconContent("LockIcon", "Reset", "Reset to the shared value");
                }

                return _unlockedIcon;
            }

            if (_lockedIcon == null)
            {
                _lockedIcon = BuildIconContent("LockIcon-On", "Override", "Override this setting for this machine only");
            }

            return _lockedIcon;
        }

        /// <summary>
        /// Resolves a built-in editor icon, falling back to text when the icon is unavailable so
        /// the control never renders as an empty button.
        /// </summary>
        private static GUIContent BuildIconContent(string iconName, string fallbackText, string tooltip)
        {
            GUIContent icon = EditorGUIUtility.IconContent(iconName);
            if (icon == null || icon.image == null)
            {
                return new GUIContent(fallbackText, tooltip);
            }

            return new GUIContent(icon.image, tooltip);
        }

        /// <summary>
        /// Writes the current value of a property into the local override file, making the field
        /// editable and pinning it to the value the developer is looking at.
        /// </summary>
        private static void OverrideField(string fieldName, SerializedProperty property)
        {
            if (!LootLockerConfig.SetLocalOverrideField(fieldName, SerializePropertyValue(property)))
            {
                return;
            }

            ClearValidationResult();
            InvalidateDiscoveredConfigs();
            LootLockerConfig.RefreshFileConfigState(force: true);
        }

        /// <summary>
        /// Removes one field from the local override file, restoring the shared value.
        /// </summary>
        private static void ResetLocalOverrideField(string fieldName)
        {
            if (!LootLockerConfig.RemoveLocalOverrideField(fieldName))
            {
                return;
            }

            ClearValidationResult();
            InvalidateDiscoveredConfigs();
            LootLockerConfig.RefreshFileConfigState(force: true);
        }

        /// <summary>
        /// Persists an edited overridden field back to the override file.
        /// </summary>
        private static void PersistLocalOverrideField(string fieldName, SerializedProperty property)
        {
            LootLockerConfig.SetLocalOverrideField(fieldName, SerializePropertyValue(property));
            LootLockerConfig.RefreshFileConfigState(force: true);
        }

        /// <summary>
        /// Renders a serialized property as JSON text for the override file.
        /// </summary>
        private static string SerializePropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return property.boolValue ? "true" : "false";
                case SerializedPropertyType.Enum:
                    // Enums are written by name: the file is meant to be readable and hand-editable.
                    return LootLockerJson.SerializeObject(property.enumNames[property.enumValueIndex]);
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                default:
                    return LootLockerJson.SerializeObject(property.stringValue);
            }
        }

        /// <summary>
        /// Cached result of the last discovery. Discovery lists folders, reads and parses every
        /// config file, and may fall back to the AssetDatabase, so running it on every OnGUI
        /// repaint is far too expensive. Invalidated explicitly whenever something could have
        /// changed: activation, the Refresh button, a selection change, and override edits.
        /// </summary>
        private static List<LootLockerConfig.FileConfigDescriptor> _discoveredConfigs;

        private static List<LootLockerConfig.FileConfigDescriptor> GetDiscoveredConfigs()
        {
            if (_discoveredConfigs == null)
            {
                _discoveredConfigs = LootLockerConfig.DiscoverFileConfigsForEditor();
            }

            return _discoveredConfigs;
        }

        /// <summary>
        /// Drops the cached discovery so the next repaint re-reads the config folder.
        /// </summary>
        private static void InvalidateDiscoveredConfigs()
        {
            _discoveredConfigs = null;
        }

        /// <summary>
        /// Draws the config file selection and refresh controls.
        ///
        /// Nothing is drawn when there are no config files: a project without file configs should
        /// look exactly like one that never had any. The same applies when there is exactly one
        /// config and it is the one in effect - there is nothing to choose and nothing to report,
        /// so the row would be pure noise.
        /// </summary>
        private void DrawFileConfigSection()
        {
            List<LootLockerConfig.FileConfigDescriptor> discovered = GetDiscoveredConfigs();
            if (discovered.Count == 0)
            {
                return;
            }

            string selectedId = gameSettings.selectedFileConfigId;
            bool selectionMissing = !string.IsNullOrEmpty(selectedId) && !discovered.Exists(d => d.Id == selectedId);

            // A dropdown is only needed when there is a real choice to make. That is the case when
            // several configs exist, when the selection is stale, or when the user explicitly
            // opted out ("") while configs are available - otherwise they could never opt back in.
            bool needsChoice = discovered.Count > 1 || selectionMissing || selectedId == "";

            if (!needsChoice)
            {
                // Exactly one config, and it is the one in effect. Nothing to show.
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Config File", GUILayout.Width(EditorGUIUtility.labelWidth));

                // A Popup cannot disable individual entries, so use a menu: invalid configs
                // are shown greyed out and unclickable rather than selectable-but-broken.
                if (EditorGUILayout.DropdownButton(new GUIContent(DescribeSelection(selectedId, discovered)), FocusType.Keyboard))
                {
                    ShowFileConfigMenu(selectedId, discovered);
                }

                if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                {
                    ClearValidationResult();
                    InvalidateDiscoveredConfigs();
                    LootLockerConfig.RefreshFileConfigState(force: true);
                }
            }

            if (selectionMissing)
            {
                EditorGUILayout.HelpBox(
                    $"The selected config '{selectedId}' is no longer available, so the settings stored in the config asset are being used. Select another config or (None) to clear this.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
        }

        /// <summary>
        /// Builds the dropdown menu. Invalid entries are added disabled, with the reason appended,
        /// so a broken file is visible but cannot be selected.
        /// </summary>
        private void ShowFileConfigMenu(string selectedId, List<LootLockerConfig.FileConfigDescriptor> discovered)
        {
            var menu = new GenericMenu();

            // null means auto-detect, "" means explicitly none. They are different states and must
            // not share a checkmark: comparing both against "" made None look selected whenever
            // Default was.
            menu.AddItem(new GUIContent(NoneOption), selectedId == "",
                () => ApplyFileConfigSelection(""));

            foreach (LootLockerConfig.FileConfigDescriptor descriptor in discovered)
            {
                bool isValid = descriptor.State == LootLockerConfig.FileConfigState.Valid;
                string label = DescribeId(descriptor.Id);
                if (!isValid)
                {
                    label += $" — {descriptor.Reason}";
                }

                var content = new GUIContent(label);
                bool isSelected = descriptor.Id == selectedId;

                if (isValid)
                {
                    string id = descriptor.Id;
                    menu.AddItem(content, isSelected, () => ApplyFileConfigSelection(id));
                }
                else
                {
                    menu.AddDisabledItem(content, isSelected);
                }
            }

            // A selection that is no longer on disk must be visible, not silently dropped.
            if (!string.IsNullOrEmpty(selectedId) && !discovered.Exists(d => d.Id == selectedId))
            {
                menu.AddDisabledItem(new GUIContent($"Missing: {selectedId}"), true);
            }

            menu.ShowAsContext();
        }

        private static string DescribeSelection(string selectedId, List<LootLockerConfig.FileConfigDescriptor> discovered)
        {
            if (string.IsNullOrEmpty(selectedId))
            {
                return NoneOption;
            }

            foreach (LootLockerConfig.FileConfigDescriptor descriptor in discovered)
            {
                if (descriptor.Id == selectedId)
                {
                    return DescribeId(descriptor.Id);
                }
            }

            return $"Missing: {selectedId}";
        }

        private static string DescribeId(string id)
        {
            return string.IsNullOrEmpty(id) ? "Default (LootLockerConfig.bytes)" : id;
        }

        private static void ApplyFileConfigSelection(string id)
        {
            // An override written against one config is meaningless against another, so switching
            // configs clears it. Confirm first: silently deleting a developer's local tweaks would
            // be worse than the stale value.
            if (LootLockerConfig.LocalOverrideExists()
                && !EditorUtility.DisplayDialog(
                    "Switch config and reset local overrides?",
                    "Switching config file deletes the gitignored local override file, because an override written for one config does not apply to another.",
                    "Switch and reset", "Cancel"))
            {
                return;
            }

            if (LootLockerConfig.LocalOverrideExists())
            {
                LootLockerConfig.DeleteLocalOverride();
            }

            // Save only the selection. gameSettings is the live asset and already holds the values
            // applied from the previous config, so marking it dirty here would write those values
            // into the committed config. Restore the authored values first, then persist the id.
            LootLockerConfig.RestoreAssetValues();
            gameSettings.selectedFileConfigId = id;
            EditorUtility.SetDirty(gameSettings);
            AssetDatabase.SaveAssets();

            // The previous validation result described a different key, so it is now meaningless.
            ClearValidationResult();
            InvalidateDiscoveredConfigs();

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
        /// Incremented whenever the effective configuration changes. A validation callback captures
        /// the value it started with and discards its result if the generation has moved on, so a
        /// slow request for the previous key cannot overwrite the result for the current one.
        /// </summary>
        private static int validationGeneration;

        /// <summary>
        /// Discards the last validation result. Called whenever the effective configuration
        /// changes, because a result describing the previous key is misleading.
        /// </summary>
        private static void ClearValidationResult()
        {
            validationResult = null;
            validationInProgress = false;
            validationGeneration++;
        }

        /// <summary>
        /// Draws the "Validate" affordance, which proves the currently effective API key works and
        /// shows which game it belongs to.
        ///
        /// This validates whatever is currently applied (LootLockerConfig.current.apiKey), not an
        /// arbitrary unselected config, so it is independent of file config selection.
        /// </summary>
        private void DrawValidation()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Connection", GUILayout.Width(EditorGUIUtility.labelWidth));

                using (new EditorGUI.DisabledScope(validationInProgress || string.IsNullOrEmpty(gameSettings?.apiKey)))
                {
                    if (GUILayout.Button(validationInProgress ? "Validating..." : "Validate", GUILayout.Width(90)))
                    {
                        ValidateCurrentConfig();
                    }
                }
            }

            DrawValidationResult();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Draws the outcome of the last validation on its own line, indented to line up with the
        /// controls above it.
        /// </summary>
        private static void DrawValidationResult()
        {
            if (!validationResult.HasValue)
            {
                return;
            }

            LootLockerConfigValidationResult result = validationResult.Value;
            string text;
            switch (result.Outcome)
            {
                case LootLockerConfigValidationOutcome.Success:
                    text = $"\u2713 {result.GameName} (title {result.TitleId}, env {result.EnvironmentId})";
                    break;
                case LootLockerConfigValidationOutcome.KeyRejected:
                    text = $"\u2717 {result.Message}";
                    break;
                case LootLockerConfigValidationOutcome.Unreachable:
                    text = $"! {result.Message}";
                    break;
                case LootLockerConfigValidationOutcome.EmptyResponse:
                    text = $"! {result.Message}";
                    break;
                default:
                    text = result.Message;
                    break;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth);
                EditorGUILayout.LabelField(text, EditorStyles.miniLabel);
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

            int generation = validationGeneration;

            LootLocker.Requests.LootLockerSDKManager.GetGameInfo(response =>
            {
                // The user switched config or reset an override while this request was in flight.
                // Writing the result now would show a verdict for a key that is no longer in
                // effect, which is exactly what ClearValidationResult was meant to prevent.
                if (generation != validationGeneration)
                {
                    return;
                }

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

            DrawOverridableField("logLevel", "Log Level", LootLockerConfigFields.LogLevel,
                () => gameSettings.logLevel = (LootLockerLogger.LogLevel)m_CustomSettings.FindProperty("logLevel").enumValueIndex);
            EditorGUILayout.Space();

            DrawOverridableField("logErrorsAsWarnings", "Log Errors As Warnings", LootLockerConfigFields.LogErrorsAsWarnings,
                () => gameSettings.logErrorsAsWarnings = m_CustomSettings.FindProperty("logErrorsAsWarnings").boolValue);
            EditorGUILayout.Space();

            DrawOverridableField("logInBuilds", "Log In Builds", LootLockerConfigFields.LogInBuilds,
                () => gameSettings.logInBuilds = m_CustomSettings.FindProperty("logInBuilds").boolValue);
            EditorGUILayout.Space();

            DrawOverridableField("prettifyJson", "Log JSON Formatted", LootLockerConfigFields.PrettifyJson,
                () => gameSettings.prettifyJson = m_CustomSettings.FindProperty("prettifyJson").boolValue);

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
            DrawOverridableField("enablePresence", "Enable Presence", LootLockerConfigFields.EnablePresence,
                () => gameSettings.enablePresence = m_CustomSettings.FindProperty("enablePresence").boolValue);
            EditorGUILayout.Space();

            // Auto-connect toggle
            DrawOverridableField("enablePresenceAutoConnect", "Auto Connect", LootLockerConfigFields.EnablePresenceAutoConnect,
                () => gameSettings.enablePresenceAutoConnect = m_CustomSettings.FindProperty("enablePresenceAutoConnect").boolValue);

            // Auto-disconnect on focus change toggle
            DrawOverridableField("enablePresenceAutoDisconnectOnFocusChange", "Auto Pause Presence", LootLockerConfigFields.EnablePresenceAutoDisconnectOnFocusChange,
                () => gameSettings.enablePresenceAutoDisconnectOnFocusChange = m_CustomSettings.FindProperty("enablePresenceAutoDisconnectOnFocusChange").boolValue);

            EditorGUILayout.Space();

            // Enable presence in editor toggle
            DrawOverridableField("enablePresenceInEditor", "Enable Presence in Editor", LootLockerConfigFields.EnablePresenceInEditor,
                () => gameSettings.enablePresenceInEditor = m_CustomSettings.FindProperty("enablePresenceInEditor").boolValue);

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
