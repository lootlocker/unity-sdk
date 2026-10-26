using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif
using UnityEngine;

namespace LootLocker
{
    /// <summary>
    /// Controls how the SDK handles multiple player sessions when a new authentication succeeds.
    /// This determines which player is the "default" for API calls that do not explicitly specify a player ULID.
    /// </summary>
    public enum LootLockerMultiUserSessionMode
    {
        /// <summary>
        /// [Not yet configured] The SDK automatically sets this to <see cref="SingleSession"/> on new installs
        /// or <see cref="Hotseat"/> on existing installs the first time the Unity Editor loads this project.
        /// This value should never be set manually — it exists solely for pre-migration compatibility.
        /// </summary>
#if UNITY_2020_1_OR_NEWER
        [InspectorName(null)]
#endif
        NotSet = 0,

        /// <summary>
        /// Multiple active sessions are allowed simultaneously.
        /// The first player to authenticate in a game session becomes the default player.
        /// Subsequent authentications are additive: they join the active pool but do not change the default.
        /// All player data is retained in persistent cache between sessions.
        /// Best for: local multiplayer, couch co-op, or any game where multiple players share a device at the same time.
        /// </summary>
        Hotseat = 1,

        /// <summary>
        /// Only one player session exists at any given time.
        /// Each new authentication completely wipes all previous session data before saving the new player as the sole active default.
        /// There is always exactly one player in the system; no historical data is kept.
        /// Best for: standard single-player games where only one account should ever exist on the device.
        /// </summary>
        SingleSession = 2,

        /// <summary>
        /// Only one player is active at a time, but historical player sessions are retained in a cold cache.
        /// Each new authentication makes that player the sole active and default player while all previously active
        /// players are deactivated — but their session data remains on-device.
        /// Developers can switch back to a previously-authenticated player without re-authenticating.
        /// Best for: games with an account selection screen, or games where players switch between accounts.
        /// </summary>
        ProfileSwitching = 3,
    }

    /// <summary>
    /// JSON field names understood by <see cref="ExternalFileConfig"/>.
    ///
    /// A config file is sparse: a field that is absent has to be distinguishable from a field that
    /// is present and set to its default value. That distinction is what lets the local override
    /// file change one setting without resetting every other one.
    /// </summary>
    internal static class LootLockerConfigFields
    {
        public const string ApiKey = "api_key";
        public const string DomainKey = "domain_key";
        public const string GameVersion = "game_version";
        public const string EnablePresence = "enable_presence";
        public const string EnablePresenceAutoConnect = "enable_presence_autoconnect";
        public const string EnablePresenceAutoDisconnectOnFocusChange = "enable_presence_autodisconnect_on_focus_change";
        public const string EnablePresenceInEditor = "enable_presence_in_editor";
        public const string SdkVersion = "sdk_version";
        public const string LogLevel = "log_level";
        public const string LogErrorsAsWarnings = "log_errors_as_warnings";
        public const string LogInBuilds = "log_in_builds";
        public const string PrettifyJson = "prettify_json";
        public const string ObfuscateLogs = "obfuscate_logs";
        public const string AllowTokenRefresh = "allow_token_refresh";
        public const string MultiUserSessionMode = "multi_user_session_mode";
        public const string EnableEditorAdminExtension = "enable_editor_admin_extension";
    }

    [Serializable]
    public class LootLockerConfig : ScriptableObject
    {
        public static readonly string PackageShortName = "LL"; // Standard is LL
        public const string PackageName = "LootLocker"; // Standard is LootLocker
        private static readonly string SettingsName = $"{PackageName}Config";
        private static readonly string ConfigFolderName = $"{PackageName}SDK";
        private static readonly string ConfigAssetName = $"{SettingsName}.asset";
        private static readonly string ConfigFileExtension = ".bytes";

        /// <summary>
        /// File name (without extension) of the gitignored local override file.
        ///
        /// Deliberately not of the form "LootLockerConfig-{id}", so it can never be mistaken for a
        /// selectable profile and never shows up in the dropdown.
        /// </summary>
        private static readonly string LocalOverrideName = $"{SettingsName}.local";

        /// <summary>
        /// Name of the config file currently in effect, derived from
        /// <see cref="selectedFileConfigId"/>. The empty identifier maps to the legacy unsuffixed
        /// name, which is what keeps existing projects working unchanged.
        /// </summary>
        private static string ConfigFileName => GetConfigFileNameForId(_current != null ? _current.selectedFileConfigId : null);

#if UNITY_EDITOR
        private static string _cachedSdkRootPath = null;

        // Locates the SDK root by finding this script in the AssetDatabase and walking up four
        // directory levels: LootLockerConfig.cs -> Resources/ -> Game/ -> Runtime/ -> SDK root.
        // This works regardless of install location (Assets/, Packages/, or any custom path).
        // But it only works in the editor
        private static string GetSdkRootAssetsPath(bool requireWritable = false)
        {
            if (_cachedSdkRootPath == null)
            {
                string[] guids = AssetDatabase.FindAssets($"{nameof(LootLockerConfig)} t:MonoScript");
                foreach (string guid in guids)
                {
                    string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (scriptPath.EndsWith($"/{nameof(LootLockerConfig)}.cs", StringComparison.OrdinalIgnoreCase))
                    {
                        // Walk up 4 levels: Resources/ -> Game/ -> Runtime/ -> SDK root
                        string dir = Path.GetDirectoryName(scriptPath); // .../Resources
                        dir = Path.GetDirectoryName(dir);               // .../Game
                        dir = Path.GetDirectoryName(dir);               // .../Runtime
                        dir = Path.GetDirectoryName(dir);               // SDK root
                        if (!string.IsNullOrEmpty(dir))
                        {
                            _cachedSdkRootPath = dir.Replace('\\', '/');
                            break;
                        }
                    }
                }
                if (string.IsNullOrEmpty(_cachedSdkRootPath))
                    _cachedSdkRootPath = $"Assets/{ConfigFolderName}";
            }
            // Packages/ is read-only — writable content (e.g. asset creation) must go under Assets/
            if (requireWritable && _cachedSdkRootPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                return $"Assets/{ConfigFolderName}";
            return _cachedSdkRootPath;
        }

        private static string ConfigAssetsPath => $"{GetSdkRootAssetsPath(requireWritable: true)}/Resources/Config/{ConfigAssetName}";
        private static string ConfigResourceFolder
        {
            get
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(projectRoot, GetSdkRootAssetsPath(requireWritable: true), "Resources", "Config");
            }
        }
        private static string ConfigFilePath => $"{GetSdkRootAssetsPath()}/Resources/Config/{ConfigFileName}{ConfigFileExtension}";
#endif


#region User Config Variables
        /** 
         * User Config Variables
         * These are the variables that are exposed and recommended to change by the user
         */
        public string apiKey;
#if UNITY_EDITOR
        [HideInInspector] public string adminToken = null;
#endif
        [HideInInspector] public string domainKey;

        /// <summary>
        /// Which file config profile to use.
        /// <para>null - auto-detect. Uses Resources/Config/LootLockerConfig.bytes if present (legacy behaviour).</para>
        /// <para>"" - none. Use the values stored in this asset only.</para>
        /// <para>"id" - use Resources/Config/LootLockerConfig-{id}.bytes</para>
        /// Assets created before this field existed deserialize as null, so existing projects keep
        /// their current behaviour.
        /// </summary>
        [HideInInspector] public string selectedFileConfigId = null;

        public string game_version = "1.0.0.0";
        [HideInInspector] public string sdk_version = "";
        public LootLockerLogger.LogLevel logLevel = LootLockerLogger.LogLevel.Info;
        // Write JSON in a pretty and indented format when logging
        public bool prettifyJson = true;
        [HideInInspector] public bool obfuscateLogs = true;
        public bool logErrorsAsWarnings = false;
        public bool logInBuilds = false;
        public bool allowTokenRefresh = true;
        [Tooltip("Enable WebSocket presence system by default. Can be controlled at runtime via SetPresenceEnabled().")]
        public bool enablePresence = false;
        
        [Tooltip("Automatically connect presence when sessions are started. This only controls whether connections are initiated automatically; once connected (automatically or manually), presence is maintained until explicitly disconnected. Can be controlled at runtime via SetPresenceAutoConnectEnabled().")]
        public bool enablePresenceAutoConnect = true;
        
        [Tooltip("Automatically disconnect presence when app loses focus or is paused (useful for battery saving). Can be controlled at runtime via SetPresenceAutoDisconnectOnFocusChangeEnabled().")]
        public bool enablePresenceAutoDisconnectOnFocusChange = false;
        
        [Tooltip("Enable presence functionality while in the Unity Editor. Disable this if you don't want development to affect presence data.")]
        public bool enablePresenceInEditor = true;

        [Tooltip(
            "Controls how the SDK handles multiple player sessions when a new authentication succeeds.\n\n" +
            "Hotseat: Multiple active sessions are allowed simultaneously. The first authentication in a game session is set as the default player. Subsequent authentications are additive — they join the active pool but do not change the default. Best for local multiplayer / couch co-op.\n\n" +
            "Single Session: Only one player session exists at any time. New authentications wipe all previous session data before saving the new player as the sole active default. Best for standard single-player games.\n\n" +
            "Profile Switching: Only one player is active at a time, but historical players are retained in cold cache. Each new authentication deactivates all others and becomes the sole active default. Developers can switch back to a cached player without re-authenticating. Best for games with account selection screens.")]
        public LootLockerMultiUserSessionMode multiUserSessionMode = LootLockerMultiUserSessionMode.NotSet;

        [Tooltip("Enable the LootLocker admin extension in the Unity Editor. Disable this if you do not want the editor admin tools to be available.")]
        public bool enableEditorAdminExtension = true;

#endregion

#region Internal logic and configuration
        public static LootLockerConfig Get()
        {
            if (_current != null)
            {
                // File config is applied once on cold load — reapplying on every
                // read is unnecessary since settings are UI-locked while active.
#if LOOTLOCKER_COMMANDLINE_SETTINGS
                _current.CheckForSettingOverrides();
#endif
                _current.ConstructUrls();
                return _current;
            }


            _current = Resources.Load<LootLockerConfig>($"Config/{SettingsName}");
            if (_current == null)
            {
#if UNITY_EDITOR
                CreateConfigFile();
                if (_current == null)
                {
                    throw new InvalidOperationException($"{ConfigFolderName} config could not be created or loaded. Please ensure the Resources/Config asset exists and try again.");
                }
#else
                throw new ArgumentException($"{ConfigFolderName} config does not exist. To fix this, play once in the Unity Editor before making a build.");
#endif
            }

            RefreshFileConfigState();

#if LOOTLOCKER_COMMANDLINE_SETTINGS
            _current.CheckForSettingOverrides();
#endif
            _current.ConstructUrls();
            return _current;
        }
        
        private static ExternalFileConfig CheckForFileConfig()
        {
            try
            {
                string id = ResolveFileConfigId(
                    _current != null ? _current.selectedFileConfigId : null,
                    commandLineId: CommandLineConfigId,
                    buildPointerId: null,
                    legacyFilePresent: LegacyConfigFileExists(),
                    availableIds: DiscoverAvailableIds());

                if (id == null)
                {
                    return null;
                }

                string fileName = GetConfigFileNameForId(id);
                string configText = ReadConfigFileText(fileName);

                if (string.IsNullOrEmpty(configText))
                {
                    // The selected config is gone (moved, deleted, not pulled). Degrade to the
                    // asset values, exactly as if no file config had ever existed, rather than
                    // failing. Warn once per id so a missing file does not spam the console on
                    // every refresh.
                    if (!string.IsNullOrEmpty(id))
                    {
                        WarnOnceForMissingConfig(id,
                            $"File config '{id}' was selected but could not be found. Falling back to the settings stored in the config asset. Expected Resources/Config/{fileName}{ConfigFileExtension}.");
                    }
                    return null;
                }

                if (!TryParseFileConfig(configText, out ExternalFileConfig fileConfig, out string error))
                {
                    WarnOnceForMissingConfig(id,
                        $"File config '{fileName}' is not usable ({error}). Falling back to the settings stored in the config asset.");
                    return null;
                }

                return fileConfig;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log("Error while checking for file config: " + ex.Message, LootLockerLogger.LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Ids already reported as unusable, so a missing or broken config warns once instead of on
        /// every refresh.
        /// </summary>
        private static readonly HashSet<string> _warnedConfigIds = new HashSet<string>();

        private static void WarnOnceForMissingConfig(string id, string message)
        {
            if (string.IsNullOrEmpty(id) || !_warnedConfigIds.Add(id))
            {
                return;
            }

            LootLockerLogger.Log(message, LootLockerLogger.LogLevel.Warning);
        }

        /// <summary>
        /// Reads a config file's text, trying Resources first and falling back to the AssetDatabase
        /// and then a direct disk read in the editor.
        /// </summary>
        /// <param name="fileName">
        /// File name with or without the config file extension. Discovery works with real file
        /// names (including the extension) while the load path works with bare names, so both are
        /// accepted here rather than forcing every caller to agree.
        /// </param>
        private static string ReadConfigFileText(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            if (fileName.EndsWith(ConfigFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - ConfigFileExtension.Length);
            }

            // 1. Primary: Resources.Load — works in any install location in both editor and builds.
            //    The .bytes file must be inside a Resources/ folder for this to work.
            TextAsset resourcesTextAsset = Resources.Load<TextAsset>($"Config/{fileName}");
            if (resourcesTextAsset != null)
            {
                return resourcesTextAsset.text;
            }

#if UNITY_EDITOR
            // 2. Editor fallback A: search AssetDatabase by name — finds the file regardless of
            //    where it lives in the project (handles race where Resources DB is not yet warmed up)
            string[] guids = AssetDatabase.FindAssets($"{fileName} t:TextAsset");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                // Match exact filename including extension to avoid false positives from
                // other TextAssets (e.g. .json/.txt) that share the same base name
                if (assetPath.EndsWith($"/{fileName}{ConfigFileExtension}", StringComparison.OrdinalIgnoreCase))
                {
                    TextAsset foundAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                    if (foundAsset != null)
                    {
                        return foundAsset.text;
                    }
                }
            }

            // 3. Editor fallback B: SDK-root-relative path with forced re-import; also reads
            //    the file directly from disk if the asset database still hasn't picked it up
            string configFilePath = $"{GetSdkRootAssetsPath()}/Resources/Config/{fileName}{ConfigFileExtension}";
            if (File.Exists(configFilePath))
            {
                AssetDatabase.ImportAsset(configFilePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.Refresh();
                TextAsset configTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(configFilePath);
                if (configTextAsset != null)
                {
                    return configTextAsset.text;
                }

                // Direct file read as last resort when the asset database is still not ready
                return File.ReadAllText(configFilePath);
            }
#endif

            return null;
        }

        /// <summary>
        /// Whether the legacy unsuffixed config file exists.
        /// </summary>
        private static bool LegacyConfigFileExists()
        {
            return ReadConfigFileText(SettingsName) != null;
        }

        /// <summary>
        /// Identifiers of the config files currently available.
        /// </summary>
        private static List<string> DiscoverAvailableIds()
        {
            var ids = new List<string>();
            foreach (FileConfigDescriptor descriptor in DiscoverFileConfigs(ListConfigFileNames, ReadConfigFileText))
            {
                ids.Add(descriptor.Id);
            }
            return ids;
        }

        /// <summary>
        /// File names present in the config folder, used by discovery.
        ///
        /// Two locations are searched because the SDK can be installed either as a package
        /// (read-only, under Packages/) or as an asset folder. The config asset is always created
        /// in the writable location, so that is where user-supplied config files live; the package
        /// location is still checked so a config shipped inside the package keeps working.
        /// </summary>
        private static IEnumerable<string> ListConfigFileNames()
        {
            var names = new List<string>();

#if UNITY_EDITOR
            foreach (string folder in ConfigFolderCandidates())
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                foreach (string path in Directory.GetFiles(folder))
                {
                    names.Add(Path.GetFileName(path));
                }
            }
#endif

            return names;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Folders that may contain config files, most specific first. The writable location comes
        /// first because that is where the config asset and user-supplied files live.
        /// </summary>
        private static IEnumerable<string> ConfigFolderCandidates()
        {
            var folders = new List<string>();

            string writable = $"{GetSdkRootAssetsPath(requireWritable: true)}/Resources/Config";
            folders.Add(writable);

            string packageRoot = GetSdkRootAssetsPath();
            string packageFolder = $"{packageRoot}/Resources/Config";
            if (!string.Equals(packageFolder, writable, StringComparison.OrdinalIgnoreCase))
            {
                folders.Add(packageFolder);
            }

            return folders;
        }

        /// <summary>
        /// Discovers the config files available in this project, for the Project Settings dropdown.
        /// </summary>
        internal static List<FileConfigDescriptor> DiscoverFileConfigsForEditor()
        {
            return DiscoverFileConfigs(ListConfigFileNames, ReadConfigFileText);
        }

        /// <summary>
        /// Absolute path of the gitignored local override file.
        /// </summary>
        internal static string LocalOverridePath =>
            $"{GetSdkRootAssetsPath(requireWritable: true)}/Resources/Config/{LocalOverrideName}{ConfigFileExtension}";

        /// <summary>
        /// Writes the local override file with the given JSON body, replacing any existing one.
        /// </summary>
        /// <returns>True when the file was written.</returns>
        internal static bool WriteLocalOverride(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                string path = LocalOverridePath;
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, json);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Could not write the local override file: {ex.Message}", LootLockerLogger.LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Deletes the local override file, if present, and re-applies the shared config.
        /// </summary>
        /// <returns>True when no local override file remains.</returns>
        internal static bool DeleteLocalOverride()
        {
            try
            {
                string path = LocalOverridePath;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                string metaPath = path + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }

                AssetDatabase.Refresh();
                RefreshFileConfigState(force: true);
                return true;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Could not delete the local override file: {ex.Message}", LootLockerLogger.LogLevel.Error);
                return false;
            }
        }
#endif

#if UNITY_EDITOR
        private static void CreateConfigFile()
        {
            // Create a new Config
            LootLockerConfig newConfig = ScriptableObject.CreateInstance<LootLockerConfig>();

            // If directory does not exist, create it
            if (!Directory.Exists(ConfigResourceFolder))
            {
                try
                {
                    Directory.CreateDirectory(ConfigResourceFolder);
                }
                catch (Exception ex)
                {
                    LootLockerLogger.Log($"Failed to create config directory at path '{ConfigResourceFolder}'. Config asset will not be created. Exception: {ex}", LootLockerLogger.LogLevel.Error);
                    return;
                }
            }

            // Create config asset
            AssetDatabase.CreateAsset(newConfig, ConfigAssetsPath);
            EditorApplication.delayCall += AssetDatabase.SaveAssets;
            AssetDatabase.Refresh();
            _current = newConfig;
        }
#endif

        private void CheckForSettingOverrides()
        {
#if LOOTLOCKER_COMMANDLINE_SETTINGS
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                // Ensure there's a next argument for value parameters
                if (i + 1 >= args.Length)
                {
                    continue;
                }

                if (args[i] == "-apikey")
                {
                    apiKey = args[i + 1];
                }
                else if (args[i] == "-domainkey")
                {
                    domainKey = args[i + 1];
                }
                else if (args[i] == "-lootlockerurl")
                {
                    UrlCoreOverride = args[i + 1];
                }
                else if (args[i] == "-gameversion")
                {
                    string versionValue = args[i + 1];
                    if (IsSemverString(versionValue))
                    {
                        game_version = versionValue;
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid game version format: '{versionValue}'. Game version must follow Semantic Versioning pattern X.Y.Z.B (e.g., 1.0.0 or 1.0.0.0). See https://docs.lootlocker.com/the-basics/core-concepts/glossary#game-version");
                    }
                }
                else if (args[i] == "-timeout")
                {
                    if (float.TryParse(args[i + 1], out float timeout))
                    {
                        clientSideRequestTimeOut = timeout;
                    }
                }
                else if (args[i] == "-loglevel")
                {
                    if (System.Enum.TryParse<LootLockerLogger.LogLevel>(args[i + 1], true, out LootLockerLogger.LogLevel level))
                    {
                        logLevel = level;
                    }
                }
                else if (args[i] == "-prettifyjson")
                {
                    if (bool.TryParse(args[i + 1], out bool prettify))
                    {
                        prettifyJson = prettify;
                    }
                }
                else if (args[i] == "-obfuscatelogs")
                {
                    if (bool.TryParse(args[i + 1], out bool obfuscate))
                    {
                        obfuscateLogs = obfuscate;
                    }
                }
                else if (args[i] == "-logerrorsaswarnings")
                {
                    if (bool.TryParse(args[i + 1], out bool errorsAsWarnings))
                    {
                        logErrorsAsWarnings = errorsAsWarnings;
                    }
                }
                else if (args[i] == "-loginbuilds")
                {
                    if (bool.TryParse(args[i + 1], out bool inBuilds))
                    {
                        logInBuilds = inBuilds;
                    }
                }
                else if (args[i] == "-allowtokenrefresh")
                {
                    if (bool.TryParse(args[i + 1], out bool allowRefresh))
                    {
                        allowTokenRefresh = allowRefresh;
                    }
                }
                else if (args[i] == "-enablepresence")
                {
                    if (bool.TryParse(args[i + 1], out bool enablePresence))
                    {
                        this.enablePresence = enablePresence;
                    }
                }
                else if (args[i] == "-enablepresenceautoconnect")
                {
                    if (bool.TryParse(args[i + 1], out bool enablePresenceAutoConnect))
                    {
                        this.enablePresenceAutoConnect = enablePresenceAutoConnect;
                    }
                }
                else if (args[i] == "-enablepresenceautodisconnectonfocuschange")
                {
                    if (bool.TryParse(args[i + 1], out bool enablePresenceAutoDisconnectOnFocusChange))
                    {
                        this.enablePresenceAutoDisconnectOnFocusChange = enablePresenceAutoDisconnectOnFocusChange;
                    }
                }
                else if (args[i] == "-multiusersessionmode")
                {
                    if (i + 1 < args.Length && System.Enum.TryParse<LootLockerMultiUserSessionMode>(args[i + 1], true, out LootLockerMultiUserSessionMode multiUserSessionMode)
                        && multiUserSessionMode != LootLockerMultiUserSessionMode.NotSet)
                    {
                        this.multiUserSessionMode = multiUserSessionMode;
                    }
                }
                else if (args[i] == "-enableeditoradminextension")
                {
                    if (i + 1 < args.Length && bool.TryParse(args[i + 1], out bool enableEditorAdminExtension))
                    {
                        this.enableEditorAdminExtension = enableEditorAdminExtension;
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Extracts the <c>-lootlockerconfig</c> value from a command line.
        ///
        /// Pure: the caller supplies the arguments, so this can be unit tested without a real
        /// process command line.
        /// </summary>
        /// <param name="args">The process arguments.</param>
        /// <returns>
        /// The requested identifier, <c>"none"</c> to disable file config, or null when the switch
        /// was not supplied at all. An invalid identifier is reported and treated as
        /// <c>"none"</c> rather than being guessed at.
        /// </returns>
        internal static string GetCommandLineConfigId(string[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-lootlockerconfig", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = args[i + 1];
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
                {
                    return "none";
                }

                if (IsValidFileConfigId(value))
                {
                    return value;
                }

                LootLockerLogger.Log(
                    $"Ignoring -lootlockerconfig '{value}': not a valid config identifier. No file config will be used.",
                    LootLockerLogger.LogLevel.Error);
                return "none";
            }

            return null;
        }

        /// <summary>
        /// The <c>-lootlockerconfig</c> value for this process, or null when the switch was not
        /// supplied.
        /// </summary>
        private static string CommandLineConfigId => GetCommandLineConfigId(Environment.GetCommandLineArgs());

        private static bool IsSemverString(string str)
        {
            return Regex.IsMatch(str, @"^(0|[1-9]\d*)\.(0|[1-9]\d*)(?:\.(0|[1-9]\d*))?(?:\.(0|[1-9]\d*))?$");
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            // Get the path to the project directory
            string projectPath = Application.dataPath;

            // Use the Directory class to get the creation time of the project directory
            DateTime creationTime = Directory.GetCreationTime(projectPath);
            string configFileEditorPref = "configFileCreated" + creationTime.GetHashCode().ToString();

            if (EditorPrefs.GetBool(configFileEditorPref) == false)
            {
                // Create config file instantly when SDK has been installed
                Get();
                EditorPrefs.SetBool(configFileEditorPref, true);
            }

            // Pre-migration: if multiUserSessionMode has never been set (NotSet), determine the correct
            // default based on whether this is a new install (no API key) or an existing project.
            // New installs default to SingleSession; existing projects default to Hotseat for
            // backwards compatibility with the previous behaviour.
            LootLockerConfig config = Get();
            if (config != null && config.multiUserSessionMode == LootLockerMultiUserSessionMode.NotSet)
            {
                config.multiUserSessionMode = string.IsNullOrEmpty(config.apiKey)
                    ? LootLockerMultiUserSessionMode.SingleSession
                    : LootLockerMultiUserSessionMode.Hotseat;
                EditorUtility.SetDirty(config);
                EditorApplication.delayCall += AssetDatabase.SaveAssets;
            }

            StoreSDKVersion();
        }

        protected static ListRequest ListInstalledPackagesRequest;

        // Raised once after the SDK version string has been resolved (either synchronously or
        // after the async Client.List() call completes). Editor-only subscribers should
        // unsubscribe inside their handler to avoid duplicate calls across domain reloads.
        public static event Action OnSdkVersionDetermined;

        // True once the SDK version has been resolved this editor session.
        // Check this in [InitializeOnLoad] static constructors to handle the case where the
        // event already fired before the subscriber registered (e.g. after a domain reload).
        public static bool SdkVersionDetermined { get; private set; }

        static void StoreSDKVersion()
        {
            // Don't re-queue if a request is already in flight.
            if (ListInstalledPackagesRequest != null)
                return;
            // The version always resolves from the installed package, never from a config file.
            // A config file that pinned a version would go stale the moment the SDK was updated,
            // and there is no reason for a config to know which SDK version is running.
            // If we already have a cached version, still re-query to keep sdk_version
            // current after SDK updates on disk (fixes stale version on upgrade).
            // We only raise OnSdkVersionDetermined once the authoritative value has
            // been resolved by Client.List(), so subscribers never act on stale data.
            try
            {
                ListInstalledPackagesRequest = Client.List();
            }
            catch (Exception exception)
            {
                // No Package Manager backend in this editor instance — resolve from files instead.
                LootLockerLogger.Log($"Could not list installed packages, falling back to file based SDK version detection: {exception.Message}", LootLockerLogger.LogLevel.Verbose);
                ListInstalledPackagesRequest = null;
                ResolveSdkVersionFromFiles();
                return;
            }
            EditorApplication.update += ListInstalledPackagesRequestProgress;
        }

        [Serializable]
        private class LLPackageDescription
        {
            public string name { get; set; }
            public string version { get; set; }
        }

        static void ListInstalledPackagesRequestProgress()
        {
            if (ListInstalledPackagesRequest == null)
            {
                EditorApplication.update -= ListInstalledPackagesRequestProgress;
                return;
            }

            if (!ListInstalledPackagesRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= ListInstalledPackagesRequestProgress;

            // The Package Manager backend is not available in every editor instance (for example
            // Multiplayer Play Mode virtual players). In that case the request completes with a
            // failure status and a null Result, so the package list must be treated as optional.
            if (ListInstalledPackagesRequest.Status == StatusCode.Success && ListInstalledPackagesRequest.Result != null)
            {
                foreach (UnityEditor.PackageManager.PackageInfo package in ListInstalledPackagesRequest.Result)
                {
                    if (package != null && package.name.Equals("com.lootlocker.lootlockersdk"))
                    {
                        LootLockerConfig.current.sdk_version = package.version;
                        LootLockerLogger.Log($"SDK Version v{LootLockerConfig.current.sdk_version}", LootLockerLogger.LogLevel.Verbose);
                        SdkVersionDetermined = true;
                        OnSdkVersionDetermined?.Invoke();
                        return;
                    }
                }
            }
            else
            {
                LootLockerLogger.Log($"Could not list installed packages, falling back to file based SDK version detection: {ListInstalledPackagesRequest.Error?.message}", LootLockerLogger.LogLevel.Verbose);
            }

            ResolveSdkVersionFromFiles();
        }

        // Resolves the SDK version without relying on the Package Manager backend by reading the
        // package manifest from disk. Used as a fallback whenever the package list is unavailable.
        static void ResolveSdkVersionFromFiles()
        {
            string sdkPackageJsonPath = $"{GetSdkRootAssetsPath()}/package.json";
            if (File.Exists(sdkPackageJsonPath))
            {
                LootLockerConfig.current.sdk_version = LootLockerJson.DeserializeObject<LLPackageDescription>(File.ReadAllText(sdkPackageJsonPath)).version;
                LootLockerLogger.Log($"SDK Version v{LootLockerConfig.current.sdk_version}", LootLockerLogger.LogLevel.Verbose);
                SdkVersionDetermined = true;
                OnSdkVersionDetermined?.Invoke();
                return;
            }

            foreach (string assetPath in AssetDatabase.GetAllAssetPaths())
            {
                if (assetPath.EndsWith("package.json"))
                {
                    LLPackageDescription packageDescription = LootLockerJson.DeserializeObject<LLPackageDescription>(File.ReadAllText(assetPath));
                    if (!string.IsNullOrEmpty(packageDescription.name) && packageDescription.name.Equals("com.lootlocker.lootlockersdk"))
                    {
                        LootLockerConfig.current.sdk_version = packageDescription.version;
                        LootLockerLogger.Log($"SDK Version v{LootLockerConfig.current.sdk_version}", LootLockerLogger.LogLevel.Verbose);
                        SdkVersionDetermined = true;
                        OnSdkVersionDetermined?.Invoke();
                        return;
                    }
                }
            }

            LootLockerConfig.current.sdk_version = "N/A";
            SdkVersionDetermined = true;
            OnSdkVersionDetermined?.Invoke();
        }
#endif

        public static bool CreateNewSettings(string apiKey, string gameVersion, string domainKey, LootLockerLogger.LogLevel logLevel = LootLockerLogger.LogLevel.Info, 
            bool logInBuilds = false, bool errorsAsWarnings = false, bool allowTokenRefresh = false, bool prettifyJson = false, bool obfuscateLogs = true, 
            bool enablePresence = false, bool enablePresenceAutoConnect = true, bool enablePresenceAutoDisconnectOnFocusChange = false, bool enablePresenceInEditor = true,
            LootLockerMultiUserSessionMode multiUserSessionMode = LootLockerMultiUserSessionMode.NotSet, bool enableEditorAdminExtension = true)
        {
            _current = Get();

            // An explicit Init(...) call is the most specific instruction the SDK can receive, so
            // it wins over every file-based source. It is deliberately not persisted: it applies
            // for the lifetime of the process and cannot rewrite the committed config asset.
            _current.apiKey = apiKey;
            _current.game_version = gameVersion;
            _current.domainKey = domainKey;
            _current.logLevel = logLevel;
            _current.logInBuilds = logInBuilds;
            _current.logErrorsAsWarnings = errorsAsWarnings;
            _current.allowTokenRefresh = allowTokenRefresh;
            _current.prettifyJson = prettifyJson;
            _current.obfuscateLogs = obfuscateLogs;
            _current.enablePresence = enablePresence;
            _current.enablePresenceAutoConnect = enablePresenceAutoConnect;
            _current.enablePresenceAutoDisconnectOnFocusChange = enablePresenceAutoDisconnectOnFocusChange;
            _current.enablePresenceInEditor = enablePresenceInEditor;
            if (multiUserSessionMode != LootLockerMultiUserSessionMode.NotSet)
            {
                _current.multiUserSessionMode = multiUserSessionMode;
            }
            _current.enableEditorAdminExtension = enableEditorAdminExtension;
#if UNITY_EDITOR
            _current.adminToken = null;
#endif //UNITY_EDITOR
#if LOOTLOCKER_COMMANDLINE_SETTINGS
            _current.CheckForSettingOverrides();
#endif
            _current.ConstructUrls();
            return true;
        }

        public static bool CreateNewSettings(LootLockerConfig newConfig)
        {
            _current = Get();

            if(newConfig == null)
            {
                return false;
            }
            _current = Get();
            if (_current == null)
            {
                return false;
            }

            // See the string overload: an explicit Init(...) call outranks every file-based source.
            _current.apiKey = newConfig.apiKey;
            _current.game_version = newConfig.game_version;
            _current.domainKey = newConfig.domainKey;
            _current.logLevel = newConfig.logLevel;
            _current.logInBuilds = newConfig.logInBuilds;
            _current.logErrorsAsWarnings = newConfig.logErrorsAsWarnings;
            _current.allowTokenRefresh = newConfig.allowTokenRefresh;
            _current.prettifyJson = newConfig.prettifyJson;
            _current.obfuscateLogs = newConfig.obfuscateLogs;
            _current.enablePresence = newConfig.enablePresence;
            _current.enablePresenceAutoConnect = newConfig.enablePresenceAutoConnect;
            _current.enablePresenceAutoDisconnectOnFocusChange = newConfig.enablePresenceAutoDisconnectOnFocusChange;
            _current.enablePresenceInEditor = newConfig.enablePresenceInEditor;
            _current.enableEditorAdminExtension = newConfig.enableEditorAdminExtension;
#if UNITY_EDITOR
            _current.adminToken = null;
#endif //UNITY_EDITOR
#if LOOTLOCKER_COMMANDLINE_SETTINGS
            _current.CheckForSettingOverrides();
#endif
            _current.ConstructUrls();
            return true;
        }

        /// <summary>
        /// Validate the current configuration settings
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public static bool ValidateSettings()
        {
            if (current == null)
            {
                LootLockerLogger.Log("SDK could not find settings, please contact support \n You can also set config manually by calling Init(string apiKey, string gameVersion, string domainKey)", LootLockerLogger.LogLevel.Error);
                return false;
            }
            if (string.IsNullOrEmpty(current.apiKey))
            {
                LootLockerLogger.Log("API Key has not been set, set it in project settings or manually calling Init(string apiKey, string gameVersion, string domainKey)", LootLockerLogger.LogLevel.Error);
                return false;
            }
            
            return true;
        }

        public static bool ClearSettings()
        {
            if (IsFileConfigActive)
            {
                return false;
            }

            _current.apiKey = null;
            _current.game_version = null;
            _current.logLevel = LootLockerLogger.LogLevel.Info;
            _current.prettifyJson = false;
            _current.logInBuilds = false;
            _current.logErrorsAsWarnings = false;
            _current.obfuscateLogs = true;
            _current.allowTokenRefresh = true;
            _current.domainKey = null;
            _current.enablePresence = false;
            _current.enablePresenceAutoConnect = true;
            _current.enablePresenceAutoDisconnectOnFocusChange = false;
            _current.enablePresenceInEditor = true;
            _current.enableEditorAdminExtension = true;
#if UNITY_EDITOR
            _current.adminToken = null;
#endif //UNITY_EDITOR
            return true;
        }

        private void ConstructUrls()
        {
            string urlCore = GetUrlCore();
            string startOfUrl = urlCore.Contains("localhost") ? "http://" : UrlProtocol;
            string wssStartOfUrl = urlCore.Contains("localhost") ? "ws://" : WssProtocol;
            if (!string.IsNullOrEmpty(domainKey))
            {
                startOfUrl += domainKey + ".";
                wssStartOfUrl += domainKey + ".";
            }
            adminUrl = startOfUrl + urlCore + AdminUrlAppendage;
            playerUrl = startOfUrl + urlCore + PlayerUrlAppendage;
            userUrl = startOfUrl + urlCore + UserUrlAppendage;
            webSocketBaseUrl = wssStartOfUrl + urlCore + UserUrlAppendage;
            baseUrl = startOfUrl + urlCore;
        }

        private static LootLockerConfig _current;
        private static ExternalFileConfig _activeFileConfig;
        private static HashSet<string> _localOverrideFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// True when an external config file is present and governing settings.
        /// All fields applied by <see cref="ApplyFileConfig"/> are read-only while this is true.
        /// Game version is intentionally not applied from the file config and stays editable in
        /// Project Settings; everything else, including the log settings, comes from the file.
        /// </summary>
        public static bool IsFileConfigActive => _activeFileConfig != null;

        /// <summary>
        /// JSON field names whose current value came from the gitignored local override file.
        ///
        /// The settings UI uses this to mark overridden fields, so a developer can always tell a
        /// shared value from one they changed locally.
        /// </summary>
        public static IReadOnlyCollection<string> LocalOverrideFields => _localOverrideFields;

        /// <summary>
        /// True when the given JSON field name (see <see cref="LootLockerConfigFields"/>) is
        /// currently overridden by the local override file.
        /// </summary>
        public static bool IsLocallyOverridden(string fieldName)
        {
            return !string.IsNullOrEmpty(fieldName) && _localOverrideFields.Contains(fieldName);
        }

        /// <summary>
        /// True when a local override file exists on disk for this project.
        /// </summary>
        public static bool LocalOverrideExists()
        {
            return ReadConfigFileText(LocalOverrideName) != null;
        }

        private static bool _fileConfigChecked;

        #region File config resolution

        /// <summary>
        /// Allowed characters for a file config identifier. The identifier becomes part of a
        /// filename, so path separators, traversal sequences and characters that are illegal on
        /// Windows must be rejected.
        /// </summary>
        internal const string FileConfigIdPattern = @"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$";

        /// <summary>
        /// True when <paramref name="id"/> is a syntactically valid file config identifier.
        /// The empty string is valid and means "the legacy, unsuffixed config file".
        /// </summary>
        internal static bool IsValidFileConfigId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return true;
            }

            return Regex.IsMatch(id, FileConfigIdPattern);
        }

        /// <summary>
        /// Builds the Resources-relative name of the config file for an identifier.
        /// The empty identifier maps to the legacy unsuffixed name.
        /// </summary>
        internal static string GetConfigFileNameForId(string id)
        {
            return string.IsNullOrEmpty(id) ? SettingsName : $"{SettingsName}-{id}";
        }

        /// <summary>
        /// Decides which file config identifier should be used.
        ///
        /// Pure: no Unity I/O and no static state, so it can be unit tested directly.
        /// </summary>
        /// <param name="selectedId">
        /// The identifier stored in the config asset. null means "auto-detect" (legacy behaviour),
        /// the empty string means "explicitly none".
        /// </param>
        /// <param name="buildPointerId">Identifier written by a build preprocess step, if any.</param>
        /// <param name="commandLineId">Identifier supplied on the command line, if any.</param>
        /// <param name="legacyFilePresent">Whether the legacy unsuffixed config file exists.</param>
        /// <param name="availableIds">Identifiers that were discovered on disk.</param>
        /// <returns>
        /// The identifier to use, or null when no file config should be applied.
        /// </returns>
        internal static string ResolveFileConfigId(
            string selectedId,
            string commandLineId,
            string buildPointerId,
            bool legacyFilePresent,
            IReadOnlyCollection<string> availableIds)
        {
            // 1. Command line wins: CI and release injection.
            if (commandLineId != null)
            {
                if (commandLineId == "none")
                {
                    return null;
                }

                return IsValidFileConfigId(commandLineId) ? commandLineId : null;
            }

            // 2. A build pointer is authoritative for the artifact it was written for.
            if (!string.IsNullOrEmpty(buildPointerId))
            {
                return IsValidFileConfigId(buildPointerId) ? buildPointerId : null;
            }

            // 3. Explicit selection in the config asset.
            if (selectedId != null)
            {
                // Empty string means "explicitly no file config".
                if (selectedId.Length == 0)
                {
                    return null;
                }

                return IsValidFileConfigId(selectedId) ? selectedId : null;
            }

            // 4. Auto-detect: legacy behaviour, unchanged for existing projects.
            if (legacyFilePresent)
            {
                return string.Empty;
            }

            // 5. A single discovered profile is unambiguous, so use it.
            if (availableIds != null && availableIds.Count == 1)
            {
                foreach (string id in availableIds)
                {
                    return id;
                }
            }

            return null;
        }

        /// <summary>
        /// Parses the contents of a config file into an <see cref="ExternalFileConfig"/>.
        ///
        /// Pure with respect to Unity: the caller supplies the text, so this can be unit tested
        /// without touching the AssetDatabase.
        /// </summary>
        /// <param name="configText">Raw file contents, either plain JSON or an encrypted base64 payload.</param>
        /// <param name="config">The parsed config, or null on failure.</param>
        /// <param name="error">A human readable reason when parsing fails.</param>
        /// <returns>True when a usable config (with a non-empty api_key) was produced.</returns>
        internal static bool TryParseFileConfig(string configText, out ExternalFileConfig config, out string error)
        {
            if (!TryParseSparseFileConfig(configText, out config, out _, out error))
            {
                return false;
            }

            if (string.IsNullOrEmpty(config.api_key))
            {
                error = "Config file has no api_key.";
                config = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses a config file without requiring an api_key, and reports which fields the file
        /// actually contained.
        ///
        /// The field set is what makes a sparse file meaningful: a field that is absent must be
        /// distinguishable from a field that is present and set to its default value, otherwise a
        /// local override of one setting would silently reset every other one.
        /// </summary>
        /// <param name="configText">Raw file contents, either plain JSON or an encrypted base64 payload.</param>
        /// <param name="config">The parsed config, or null on failure.</param>
        /// <param name="presentFields">JSON field names that appeared in the file.</param>
        /// <param name="error">A human readable reason when parsing fails.</param>
        internal static bool TryParseSparseFileConfig(
            string configText,
            out ExternalFileConfig config,
            out HashSet<string> presentFields,
            out string error)
        {
            config = null;
            presentFields = null;
            error = null;

            if (string.IsNullOrEmpty(configText))
            {
                error = "Config file is empty.";
                return false;
            }

            string json;
            try
            {
                json = configText;
                if (LootLocker.Utilities.Encryption.LootLockerEncryptionUtilities.IsValidBase64String(configText))
                {
                    json = LootLocker.Utilities.Encryption.LootLockerEncryptionUtilities.SimpleDecryptFromBase64(configText);
                }
            }
            catch (Exception ex)
            {
                error = $"Config file could not be decrypted: {ex.Message}";
                return false;
            }

            try
            {
                config = LootLockerJson.DeserializeObject<ExternalFileConfig>(json);
                presentFields = GetPresentFieldNames(json);
            }
            catch (Exception ex)
            {
                error = $"Config file could not be parsed: {ex.Message}";
                config = null;
                return false;
            }

            if (config == null)
            {
                error = "Config file could not be parsed.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Field names present in a JSON object. Returns an empty set rather than throwing when the
        /// document is not an object, so a caller that already parsed the config is never failed by
        /// this second pass.
        /// </summary>
        private static HashSet<string> GetPresentFieldNames(string json)
        {
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                Dictionary<string, object> raw = LootLockerJson.DeserializeObject<Dictionary<string, object>>(json);
                if (raw != null)
                {
                    foreach (string key in raw.Keys)
                    {
                        fields.Add(key);
                    }
                }
            }
            catch (Exception)
            {
                // Not an object document. The config itself parsed, so treat it as having no
                // explicitly present fields rather than failing the whole load.
            }

            return fields;
        }

        /// <summary>
        /// State of a discovered config file.
        /// </summary>
        internal enum FileConfigState
        {
            /// <summary>Parses and has an api_key. Selectable.</summary>
            Valid = 0,
            /// <summary>Exists but does not parse, or has no api_key. Shown but not selectable.</summary>
            Invalid = 1,
            /// <summary>Could not be read at all. Shown but not selectable.</summary>
            Unreadable = 2,
        }

        /// <summary>
        /// A config file found on disk, with its classification.
        /// </summary>
        internal struct FileConfigDescriptor
        {
            /// <summary>Identifier, or the empty string for the legacy unsuffixed file.</summary>
            public string Id;
            /// <summary>Whether the file can actually be used.</summary>
            public FileConfigState State;
            /// <summary>Why the file is not usable, when it is not.</summary>
            public string Reason;
        }

        /// <summary>
        /// Extracts the identifier from a config file name, or null when the name is not a config file.
        /// </summary>
        /// <param name="fileName">File name including extension, e.g. "LootLockerConfig-prod.bytes".</param>
        internal static string GetIdFromConfigFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            if (!fileName.EndsWith(ConfigFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string withoutExtension = fileName.Substring(0, fileName.Length - ConfigFileExtension.Length);

            // Files ending in '~' are excluded from the build by the preprocess step; never list them.
            if (withoutExtension.EndsWith("~", StringComparison.Ordinal))
            {
                return null;
            }

            if (string.Equals(withoutExtension, SettingsName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            string prefix = SettingsName + "-";
            if (!withoutExtension.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string id = withoutExtension.Substring(prefix.Length);
            return IsValidFileConfigId(id) ? id : null;
        }

        /// <summary>
        /// Discovers and classifies the config files available on disk.
        ///
        /// The I/O is injected so this can be unit tested without an AssetDatabase. Classification
        /// is a local parse only: no network calls and no key verification.
        /// </summary>
        /// <param name="listFileNames">Returns the file names present in the config folder.</param>
        /// <param name="readText">Reads a file's contents by file name, or returns null on failure.</param>
        internal static List<FileConfigDescriptor> DiscoverFileConfigs(
            Func<IEnumerable<string>> listFileNames,
            Func<string, string> readText)
        {
            var descriptors = new List<FileConfigDescriptor>();
            if (listFileNames == null)
            {
                return descriptors;
            }

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string fileName in listFileNames())
            {
                string id = GetIdFromConfigFileName(fileName);
                if (id == null)
                {
                    continue;
                }

                // Case-insensitive dedupe: "Prod.bytes" and "prod.bytes" must not both be listed.
                if (!seenIds.Add(id))
                {
                    continue;
                }

                var descriptor = new FileConfigDescriptor { Id = id, State = FileConfigState.Valid };

                string text = null;
                try
                {
                    text = readText?.Invoke(fileName);
                }
                catch (Exception ex)
                {
                    descriptor.State = FileConfigState.Unreadable;
                    descriptor.Reason = ex.Message;
                    descriptors.Add(descriptor);
                    continue;
                }

                if (text == null)
                {
                    descriptor.State = FileConfigState.Unreadable;
                    descriptor.Reason = "File could not be read.";
                }
                else if (!TryParseFileConfig(text, out _, out string error))
                {
                    descriptor.State = FileConfigState.Invalid;
                    descriptor.Reason = error;
                }

                descriptors.Add(descriptor);
            }

            descriptors.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));
            return descriptors;
        }

        #endregion

        /// <summary>
        /// Probes the external config file and applies it when present. Safe to call repeatedly;
        /// the probe only runs once per domain unless <paramref name="force"/> is true.
        /// </summary>
        public static void RefreshFileConfigState(bool force = false)
        {
            if (_current == null)
            {
                return;
            }
            if (_fileConfigChecked && !force)
            {
                return;
            }
            _fileConfigChecked = true;

            // Reset from the asset before applying, otherwise switching from one config to another
            // leaves the previous config's values in place for every field the new one omits.
            if (force)
            {
                ReloadConfigAsset();
                _warnedConfigIds.Clear();
            }

            _localOverrideFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ExternalFileConfig fileConfig = CheckForFileConfig();
            if (fileConfig != null && !string.IsNullOrEmpty(fileConfig.api_key))
            {
                _activeFileConfig = fileConfig;
                ApplyFileConfig(fileConfig, null);
            }
            else
            {
                _activeFileConfig = null;
            }

            // The local override is layered on top of whatever the shared config produced, so it
            // applies even when no shared config is in effect.
            ApplyLocalOverride();
        }

        /// <summary>
        /// Applies the gitignored local override file, if one exists.
        ///
        /// The file is sparse: only the fields it contains are applied, so a developer can flip one
        /// setting for an afternoon without forking the shared config. A file that cannot be read
        /// or parsed is reported once and otherwise ignored - it must never break loading.
        /// </summary>
        private static void ApplyLocalOverride()
        {
            string text = ReadConfigFileText(LocalOverrideName);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!TryParseSparseFileConfig(text, out ExternalFileConfig localConfig, out HashSet<string> presentFields, out string error))
            {
                WarnOnceForMissingConfig(LocalOverrideName,
                    $"Local override file '{LocalOverrideName}{ConfigFileExtension}' is not usable ({error}). Ignoring it.");
                return;
            }

            _localOverrideFields = ApplyFileConfig(localConfig, presentFields);
        }

        /// <summary>
        /// Copies the fields of a file config onto the live config.
        /// </summary>
        /// <param name="fileConfig">The parsed config to apply.</param>
        /// <param name="onlyFields">
        /// When non-null, only these JSON field names are applied. Used for the sparse local
        /// override file. When null every field is applied, which is the shared config behaviour.
        /// </param>
        /// <returns>The JSON field names that were actually applied.</returns>
        private static HashSet<string> ApplyFileConfig(ExternalFileConfig fileConfig, HashSet<string> onlyFields)
        {
            var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool Applies(string fieldName)
            {
                return onlyFields == null || onlyFields.Contains(fieldName);
            }

            if (Applies(LootLockerConfigFields.ApiKey))
            {
                _current.apiKey = fileConfig.api_key;
                applied.Add(LootLockerConfigFields.ApiKey);
            }
            if (Applies(LootLockerConfigFields.DomainKey))
            {
                _current.domainKey = fileConfig.domain_key;
                applied.Add(LootLockerConfigFields.DomainKey);
            }
            if (Applies(LootLockerConfigFields.GameVersion))
            {
                _current.game_version = fileConfig.game_version;
                applied.Add(LootLockerConfigFields.GameVersion);
            }
            if (Applies(LootLockerConfigFields.LogLevel))
            {
                _current.logLevel = fileConfig.log_level;
                applied.Add(LootLockerConfigFields.LogLevel);
            }
            if (Applies(LootLockerConfigFields.LogErrorsAsWarnings))
            {
                _current.logErrorsAsWarnings = fileConfig.log_errors_as_warnings;
                applied.Add(LootLockerConfigFields.LogErrorsAsWarnings);
            }
            if (Applies(LootLockerConfigFields.LogInBuilds))
            {
                _current.logInBuilds = fileConfig.log_in_builds;
                applied.Add(LootLockerConfigFields.LogInBuilds);
            }
            if (Applies(LootLockerConfigFields.EnablePresence))
            {
                _current.enablePresence = fileConfig.enable_presence;
                applied.Add(LootLockerConfigFields.EnablePresence);
            }
            if (Applies(LootLockerConfigFields.EnablePresenceAutoConnect))
            {
                _current.enablePresenceAutoConnect = fileConfig.enable_presence_autoconnect;
                applied.Add(LootLockerConfigFields.EnablePresenceAutoConnect);
            }
            if (Applies(LootLockerConfigFields.EnablePresenceAutoDisconnectOnFocusChange))
            {
                _current.enablePresenceAutoDisconnectOnFocusChange = fileConfig.enable_presence_autodisconnect_on_focus_change;
                applied.Add(LootLockerConfigFields.EnablePresenceAutoDisconnectOnFocusChange);
            }
            if (Applies(LootLockerConfigFields.EnablePresenceInEditor))
            {
                _current.enablePresenceInEditor = fileConfig.enable_presence_in_editor;
                applied.Add(LootLockerConfigFields.EnablePresenceInEditor);
            }
            if (Applies(LootLockerConfigFields.AllowTokenRefresh))
            {
                _current.allowTokenRefresh = fileConfig.allow_token_refresh;
                applied.Add(LootLockerConfigFields.AllowTokenRefresh);
            }
            if (Applies(LootLockerConfigFields.PrettifyJson))
            {
                _current.prettifyJson = fileConfig.prettify_json;
                applied.Add(LootLockerConfigFields.PrettifyJson);
            }
            if (Applies(LootLockerConfigFields.ObfuscateLogs))
            {
                _current.obfuscateLogs = fileConfig.obfuscate_logs;
                applied.Add(LootLockerConfigFields.ObfuscateLogs);
            }
            if (Applies(LootLockerConfigFields.MultiUserSessionMode)
                && fileConfig.multi_user_session_mode != LootLockerMultiUserSessionMode.NotSet)
            {
                _current.multiUserSessionMode = fileConfig.multi_user_session_mode;
                applied.Add(LootLockerConfigFields.MultiUserSessionMode);
            }
            if (Applies(LootLockerConfigFields.EnableEditorAdminExtension))
            {
                _current.enableEditorAdminExtension = fileConfig.enable_editor_admin_extension;
                applied.Add(LootLockerConfigFields.EnableEditorAdminExtension);
            }
#if UNITY_EDITOR
            _current.adminToken = null;
#endif
            return applied;
        }

        /// <summary>
        /// Re-reads the config asset from Resources so that file config values applied earlier are
        /// discarded. Without this, switching configs cannot revert fields the new config omits.
        /// </summary>
        private static void ReloadConfigAsset()
        {
            LootLockerConfig reloaded = Resources.Load<LootLockerConfig>($"Config/{SettingsName}");
            if (reloaded != null)
            {
                _current = reloaded;
            }
        }

        private static void ApplyFileConfig(ExternalFileConfig fileConfig)
        {
            _current.sdk_version = fileConfig.sdk_version;
            _current.apiKey = fileConfig.api_key;
            _current.domainKey = fileConfig.domain_key;
            _current.enablePresence = fileConfig.enable_presence;
            _current.enablePresenceAutoConnect = fileConfig.enable_presence_autoconnect;
            _current.enablePresenceAutoDisconnectOnFocusChange = fileConfig.enable_presence_autodisconnect_on_focus_change;
            _current.enablePresenceInEditor = fileConfig.enable_presence_in_editor;
            _current.allowTokenRefresh = fileConfig.allow_token_refresh;
            _current.prettifyJson = fileConfig.prettify_json;
            _current.obfuscateLogs = fileConfig.obfuscate_logs;
            if (fileConfig.multi_user_session_mode != LootLockerMultiUserSessionMode.NotSet)
            {
                _current.multiUserSessionMode = fileConfig.multi_user_session_mode;
            }
            _current.enableEditorAdminExtension = fileConfig.enable_editor_admin_extension;
#if UNITY_EDITOR
            _current.adminToken = null;
#endif
        }

        public static LootLockerConfig current
        {
            get
            {
                if (_current == null)
                {
                    _current = Get();
                }

                return _current;
            }
        }

        public (string key, string value) dateVersion = ("LL-Version", "2021-03-01");

        [HideInInspector] private static readonly string UrlProtocol = "https://";
        [HideInInspector] private static readonly string WssProtocol = "wss://";
        [HideInInspector] private static readonly string UrlCore = "api.lootlocker.com";
        [HideInInspector] private static string UrlCoreOverride =
#if LOOTLOCKER_TARGET_LOCAL_ENV
           "localhost:8080";
#else
            null;
#endif
        private static string GetUrlCore() { return string.IsNullOrEmpty(UrlCoreOverride) ? UrlCore : UrlCoreOverride; }

        public static bool IsTargetingProductionEnvironment()
        {
            return string.IsNullOrEmpty(UrlCoreOverride) || UrlCoreOverride.Equals(UrlCore);

        }
        [HideInInspector] private static readonly string UrlAppendage = "/v1";
        [HideInInspector] private static readonly string AdminUrlAppendage = "/admin";
        [HideInInspector] private static readonly string PlayerUrlAppendage = "/player";
        [HideInInspector] private static readonly string UserUrlAppendage = "/game";

        [HideInInspector] public string url = UrlProtocol + GetUrlCore() + UrlAppendage;

        [HideInInspector] public string adminUrl = UrlProtocol + GetUrlCore() + AdminUrlAppendage;
        [HideInInspector] public string playerUrl = UrlProtocol + GetUrlCore() + PlayerUrlAppendage;
        [HideInInspector] public string userUrl = UrlProtocol + GetUrlCore() + UserUrlAppendage;
        [HideInInspector] public string webSocketBaseUrl = WssProtocol + GetUrlCore() + UserUrlAppendage;
        [HideInInspector] public string baseUrl = UrlProtocol + GetUrlCore();
        [HideInInspector] public float clientSideRequestTimeOut = 180f;

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            _current = null;
            _cachedSdkRootPath = null;
            _activeFileConfig = null;
            _fileConfigChecked = false;
        }
#endif
#endregion
    }
}