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
using UnityEngine.Scripting;

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

    // The type is referenced directly by runtime code (e.g. LootLockerConfig.current), but the
    // instance used at runtime is created by Unity's serialization when the asset is loaded via
    // Resources.Load. That instantiation path is invisible to the linker, so without [Preserve]
    // the type and its default constructor can be stripped and the config asset fails to
    // deserialize in builds.
    [Preserve]
    [Serializable]
    public class LootLockerConfig : ScriptableObject
    {
        public static readonly string PackageShortName = "LL"; // Standard is LL
        public const string PackageName = "LootLocker"; // Standard is LootLocker
        private static readonly string SettingsName = $"{PackageName}Config";
        private static readonly string ConfigFolderName = $"{PackageName}SDK";
        private static readonly string ConfigAssetName = $"{SettingsName}.asset";
        internal static readonly string ConfigFileExtension = ".bytes";

        /// <summary>
        /// Resources-relative folder that holds the config asset and any config files.
        /// </summary>
        private const string ConfigResourcesFolder = "Config";

        /// <summary>
        /// File name (without extension) of the local override file (excluded from version control).
        ///
        /// Deliberately not of the form "LootLockerConfig-{id}", so it can never be mistaken for a
        /// selectable profile and never shows up in the dropdown.
        /// </summary>
        internal static readonly string LocalOverrideName = $"{SettingsName}.local";

        /// <summary>
        /// Folder, next to the config asset's Resources folder, holding the local override file.
        /// Outside Resources/ so the file is never packed into a build. See LocalOverrideFolder.
        /// </summary>
        internal const string LocalOverrideFolderName = "LocalOverrides";

        /// <summary>
        /// Name of the generated file a build writes to record which config it chose.
        ///
        /// Lives next to the config files so the player can read it with the same Resources call,
        /// but is never hand-authored and is excluded from version control. The build processor
        /// writes it before the build and deletes it afterwards.
        /// </summary>
        internal const string BuildPointerName = "LootLockerConfig.build";

        /// <summary>
        /// The config file extension, for the build processor, which lives in another assembly.
        /// </summary>
        internal static string ConfigFileExtensionForBuild => ConfigFileExtension;

        /// <summary>
        /// Identifier of the legacy unsuffixed config file.
        /// </summary>
        internal static string ConfigFileNameForLegacy => SettingsName;

        /// <summary>
        /// Project-relative folders that may hold config files, for the build processor.
        ///
        /// Plural on purpose: the SDK can be installed as a package (read-only, under Packages/)
        /// or as an asset folder, and discovery already searches both. The build processor must
        /// strip in every folder discovery reads from, or a config in the package folder would be
        /// read but never stripped - and would therefore ship.
        /// </summary>
        internal static string[] ConfigFolderAssetsPathsForBuild
        {
#if UNITY_EDITOR
            get
            {
                var paths = new List<string>();
                foreach (string folder in ConfigFolderCandidates())
                {
                    paths.Add(folder);
                }

                return paths.ToArray();
            }
#else
            get { return new string[0]; }
#endif
        }

        /// <summary>
        /// Name of the config file currently in effect, derived from
        /// <see cref="selectedFileConfigId"/>. The empty identifier maps to the legacy unsuffixed
        /// name, which is what keeps existing projects working unchanged.
        /// </summary>
        private static string ConfigFileName => LootLockerConfigFileResolution.GetConfigFileNameForId(_current != null ? _current.selectedFileConfigId : null);

#if UNITY_EDITOR
        private static string _cachedSdkRootPath = null;

        // Locates the SDK root by finding this script in the AssetDatabase and walking up four
        // directory levels: LootLockerConfig.cs -> Resources/ -> Game/ -> Runtime/ -> SDK root.
        // This works regardless of install location (Assets/, Packages/, or any custom path).
        // But it only works in the editor
        internal static string GetSdkRootAssetsPath(bool requireWritable = false)
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


            _current = Resources.Load<LootLockerConfig>($"{ConfigResourcesFolder}/{SettingsName}");
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

            // Capture the authored values before anything is applied, so a later reload can get
            // back to them. Must happen before RefreshFileConfigState.
            LootLockerConfigFileLayering.CaptureAssetSnapshot();

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
                string id = LootLockerConfigFileResolution.ResolveFileConfigId(
                    _current != null ? _current.selectedFileConfigId : null,
                    commandLineId: CommandLineConfigId,
                    buildPointerId: null,
                    legacyFilePresent: LegacyConfigFileExists(),
                    availableIds: DiscoverAvailableIds());

                if (id == null)
                {
                    return null;
                }

                string fileName = LootLockerConfigFileResolution.GetConfigFileNameForId(id);
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

                if (!LootLockerConfigFileParsing.TryParseFileConfig(configText, out ExternalFileConfig fileConfig, out string error))
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

        internal static void WarnOnceForMissingConfig(string id, string message)
        {
            // Only null is excluded. The empty string is the legacy unsuffixed config file, which
            // must be reported like any other when it is unusable.
            if (id == null || !_warnedConfigIds.Add(id))
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
            TextAsset resourcesTextAsset = Resources.Load<TextAsset>($"{ConfigResourcesFolder}/{fileName}");
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
            foreach (LootLockerConfigFileResolution.FileConfigDescriptor descriptor in LootLockerConfigFileResolution.DiscoverFileConfigs(ListConfigFileNames, ReadConfigFileText))
            {
                // Only usable files. A lone broken config must not be picked on the developer's
                // behalf by the single-file auto-detect step: the UI says a broken file is shown
                // but cannot be selected, and auto-selecting it would contradict that and log a
                // "not usable" warning on every load.
                if (descriptor.State == LootLockerConfigFileResolution.FileConfigState.Valid)
                {
                    ids.Add(descriptor.Id);
                }
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
        ///
        /// In a player there is no AssetDatabase, so the names come from the Resources bundle
        /// instead. Without that, auto-detection would work in the editor and silently do nothing
        /// in a build, which is the worst possible split: the developer sees one config and the
        /// players get another.
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
#else
            // Resources.LoadAll returns every TextAsset under Resources/Config, which is exactly
            // the set the editor discovers by listing the folder.
            TextAsset[] assets = Resources.LoadAll<TextAsset>(ConfigResourcesFolder);
            foreach (TextAsset asset in assets)
            {
                if (asset != null)
                {
                    names.Add(asset.name + ConfigFileExtension);
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
        internal static List<LootLockerConfigFileResolution.FileConfigDescriptor> DiscoverFileConfigsForEditor()
        {
            return LootLockerConfigFileResolution.DiscoverFileConfigs(ListConfigFileNames, ReadConfigFileText);
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

                if (LootLockerConfigFileResolution.IsValidFileConfigId(value))
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
            //
            // Recorded as well as applied, because a later forced refresh restores the authored
            // snapshot and would otherwise discard these values.
            LootLockerConfigFileLayering.RecordInitValues(overrides =>
            {
                overrides.apiKey = apiKey;
                overrides.game_version = gameVersion;
                overrides.domainKey = domainKey;
                overrides.logLevel = logLevel;
            });

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

            // See the string overload: an explicit Init(...) call outranks every file-based source,
            // and is recorded so a later forced refresh does not discard it.
            LootLockerConfigFileLayering.RecordInitValues(overrides => LootLockerConfigFileLayering.CopyConfigValues(newConfig, overrides));

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
        /// The live config instance, or null when it has not been loaded yet.
        ///
        /// Deliberately not <see cref="current"/>: that property lazily calls <see cref="Get"/>,
        /// and the layering code runs *during* loading, so using it there would re-enter
        /// <see cref="Get"/>. This is a plain read of the backing field.
        ///
        /// Named for its one caller rather than for what it returns, because "the config" is
        /// already <see cref="current"/> and a second, similar-sounding name would invite the
        /// wrong choice. Read-only, so the layering code can observe the live config without
        /// being able to replace it.
        /// </summary>
        internal static LootLockerConfig RawConfigForLayering => _current;

        /// <summary>
        /// True when an external config file is present and governing settings.
        /// All fields applied by <see cref="ApplyFileConfig"/> are read-only while this is true.
        /// Game version is intentionally not applied from the file config and stays editable in
        /// Project Settings.
        /// </summary>
        public static bool IsFileConfigActive => _activeFileConfig != null;

        /// <summary>
        /// JSON field names whose current value came from the local override file (excluded from version control).
        ///
        /// The settings UI uses this to mark overridden fields, so a developer can always tell a
        /// shared value from one they changed locally.
        /// </summary>
        public static IReadOnlyCollection<string> LocalOverrideFields => _localOverrideFields;

        /// <summary>
        /// Replaces the set of fields currently governed by the local override file.
        ///
        /// A setter rather than a mutable collection: the layering code computes the whole set in
        /// one pass, so handing over the finished set keeps the field private and makes it
        /// impossible for a caller to leave it half-updated.
        /// </summary>
        internal static void SetLocalOverrideFields(HashSet<string> fields)
        {
            _localOverrideFields = fields ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when the given JSON field name (see <see cref="LootLockerConfigFields"/>) is
        /// currently overridden by the local override file.
        /// </summary>
        public static bool IsLocallyOverridden(string fieldName)
        {
            return !string.IsNullOrEmpty(fieldName) && _localOverrideFields.Contains(fieldName);
        }

        /// <summary>
        /// The identifier that resolution currently selects, or null when no file config applies.
        ///
        /// The settings UI needs this to show what "auto-detect" resolved to, rather than
        /// displaying a selection state that does not match the settings below it.
        /// </summary>
        internal static string GetResolvedFileConfigId()
        {
            return LootLockerConfigFileResolution.ResolveFileConfigId(
                _current != null ? _current.selectedFileConfigId : null,
                commandLineId: CommandLineConfigId,
                buildPointerId: BuildPointerConfigId,
                legacyFilePresent: LegacyConfigFileExists(),
                availableIds: DiscoverAvailableIds());
        }

        /// <summary>
        /// The identifier a build recorded, or null when this is not a build or none was written.
        ///
        /// This is how a config chosen at build time reaches the player: the build strips every
        /// other config file, so the player could not auto-detect the survivor on its own without
        /// guessing. Read once per domain.
        /// </summary>
        private static string BuildPointerConfigId
        {
            get
            {
                if (_buildPointerRead)
                {
                    return _buildPointerConfigId;
                }

                _buildPointerRead = true;
                _buildPointerConfigId = ReadBuildPointer();

                return _buildPointerConfigId;
            }
        }

        private static bool _buildPointerRead;
        private static string _buildPointerConfigId;

        /// <summary>
        /// Reads and validates the build pointer file, returning null when absent or unusable.
        /// </summary>
        private static string ReadBuildPointer()
        {
            string text = ReadConfigFileText(BuildPointerName);
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            try
            {
                ExternalFileConfig pointer = LootLockerJson.DeserializeObject<ExternalFileConfig>(text);
                string id = pointer == null ? null : pointer.selected_file_config_id;
                return LootLockerConfigFileResolution.IsValidFileConfigId(id) ? id : null;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Could not read the build config pointer: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                return null;
            }
        }

        /// <summary>
        /// True when a local override file exists on disk for this project.
        ///
        /// Editor only: the file lives outside Resources/, so it is never present in a build, and
        /// callers are all editor-side anyway. A plain File.Exists rather than reading and parsing
        /// the file, because this is called from the admin request path and from every repaint.
        /// </summary>
#if UNITY_EDITOR
        internal static bool LocalOverrideExists()
        {
            try
            {
                return File.Exists(LootLockerConfigFileLocalOverride.LocalOverridePath);
            }
            catch (Exception)
            {
                return false;
            }
        }
#endif

        private static bool _fileConfigChecked;


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
                LootLockerConfigFileLayering.ReloadConfigAsset();
                _warnedConfigIds.Clear();
            }

            _localOverrideFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ExternalFileConfig fileConfig = CheckForFileConfig();
            if (fileConfig != null && !string.IsNullOrEmpty(fileConfig.api_key))
            {
                _activeFileConfig = fileConfig;
                LootLockerConfigFileLayering.ApplyFileConfig(fileConfig, null);
            }
            else
            {
                _activeFileConfig = null;
            }

            // The local override is layered on top of whatever the shared config produced, so it
            // applies even when no shared config is in effect.
            //
            // Editor only, deliberately. The override file lives outside Resources/ so it never
            // ships, and it is a developer convenience rather than a build input.
            //
            // Skipped when the command line explicitly asked for no file config: "-lootlockerconfig
            // none" is documented as disabling file config, and silently layering a local override
            // on top would make that flag a lie.
#if UNITY_EDITOR
            if (!IsFileConfigExplicitlyDisabled())
            {
                LootLockerConfigFileLayering.ApplyLocalOverride();
            }
#endif

            // An explicit Init call outranks every file layer, so put it back after the layers have
            // run. Without this, any forced refresh would restore the authored snapshot and quietly
            // drop the caller's values.
            LootLockerConfigFileLayering.ReapplyInitValues();
        }

        /// <summary>
        /// True when the command line explicitly opted out of file config with
        /// <c>-lootlockerconfig none</c>.
        /// </summary>
        private static bool IsFileConfigExplicitlyDisabled()
        {
            return string.Equals(CommandLineConfigId, "none", StringComparison.OrdinalIgnoreCase);
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
