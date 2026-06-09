using System;
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

    [Serializable]
    public class LootLockerConfig : ScriptableObject
    {
        public static readonly string PackageShortName = "LL"; // Standard is LL
        public const string PackageName = "Pantaloon"; // Standard is LootLocker
        private static readonly string SettingsName = $"{PackageName}Config";
        private static readonly string ConfigFolderName = $"{PackageName}SDK";
        private static readonly string ConfigAssetName = $"{SettingsName}.asset";
        private static readonly string ConfigFileIdentifier = ""; // Optional extra package id if you want to be able to switch between multiple configurations
        private static readonly string ConfigFileExtension = ".bytes";
        private static readonly string ConfigFileName = $"{SettingsName}" + (string.IsNullOrEmpty(ConfigFileIdentifier) ? "" : $"-{ConfigFileIdentifier}");

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
        
        [Tooltip("Automatically connect presence when sessions are started. Can be controlled at runtime via SetPresenceAutoConnectEnabled().")]
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

            var fileConfig = CheckForFileConfig();
            if (fileConfig != null && !string.IsNullOrEmpty(fileConfig.api_key))
            {
                _current.sdk_version = fileConfig.sdk_version;
                _current.apiKey = fileConfig.api_key;
                _current.game_version = fileConfig.game_version;
                _current.domainKey = fileConfig.domain_key;
                _current.enablePresence = fileConfig.enable_presence;
                _current.enablePresenceAutoConnect = fileConfig.enable_presence_autoconnect;
                _current.enablePresenceAutoDisconnectOnFocusChange = fileConfig.enable_presence_autodisconnect_on_focus_change;
                _current.enablePresenceInEditor = fileConfig.enable_presence_in_editor;
                _current.logLevel = fileConfig.log_level;
                _current.logErrorsAsWarnings = fileConfig.log_errors_as_warnings;
                _current.logInBuilds = fileConfig.log_in_builds;
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

#if LOOTLOCKER_COMMANDLINE_SETTINGS
            _current.CheckForSettingOverrides();
#endif
            _current.ConstructUrls();
            return _current;
        }
        
        private static ExternalFileConfig CheckForFileConfig()
        {
            ExternalFileConfig fileConfig = null;
            try
            {
                string configText = null;

                // 1. Primary: Resources.Load — works in any install location in both editor and builds.
                //    The .bytes file must be inside a Resources/ folder for this to work.
                TextAsset resourcesTextAsset = Resources.Load<TextAsset>($"Config/{ConfigFileName}");
                if (resourcesTextAsset != null)
                {
                    configText = resourcesTextAsset.text;
                }

#if UNITY_EDITOR
                // 2. Editor fallback A: search AssetDatabase by name — finds the file regardless of
                //    where it lives in the project (handles race where Resources DB is not yet warmed up)
                if (configText == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"{ConfigFileName} t:TextAsset");
                    foreach (string guid in guids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        // Match exact filename including extension to avoid false positives from
                        // other TextAssets (e.g. .json/.txt) that share the same base name
                        if (assetPath.EndsWith($"/{ConfigFileName}{ConfigFileExtension}", StringComparison.OrdinalIgnoreCase))
                        {
                            TextAsset foundAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                            if (foundAsset != null)
                            {
                                configText = foundAsset.text;
                                break;
                            }
                        }
                    }
                }

                // 3. Editor fallback B: SDK-root-relative path with forced re-import; also reads
                //    the file directly from disk if the asset database still hasn't picked it up
                if (configText == null && File.Exists(ConfigFilePath))
                {
                    AssetDatabase.ImportAsset(ConfigFilePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    AssetDatabase.Refresh();
                    TextAsset configTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigFilePath);
                    if (configTextAsset != null)
                    {
                        configText = configTextAsset.text;
                    }
                    else
                    {
                        // Direct file read as last resort when the asset database is still not ready
                        configText = File.ReadAllText(ConfigFilePath);
                    }
                }
#endif

                if (!string.IsNullOrEmpty(configText))
                {
                    var encryptedBase64 = configText;
                    if (!LootLocker.Utilities.Encryption.LootLockerEncryptionUtilities.IsValidBase64String(encryptedBase64))
                    {
                        fileConfig = LootLockerJson.DeserializeObject<LootLocker.ExternalFileConfig>(configText);
                        if (fileConfig != null && !string.IsNullOrEmpty(fileConfig.api_key))
                        {
                            return fileConfig;
                        }
                        return null;
                    }
                    var decryptedConfigJson = LootLocker.Utilities.Encryption.LootLockerEncryptionUtilities.SimpleDecryptFromBase64(encryptedBase64);
                    fileConfig = LootLockerJson.DeserializeObject<LootLocker.ExternalFileConfig>(decryptedConfigJson);
                    if (fileConfig != null && !string.IsNullOrEmpty(fileConfig.api_key))
                    {
                        return fileConfig;
                    }
                }
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log("Error while checking for file config: " + ex.Message, LootLockerLogger.LogLevel.Error);
                return fileConfig;
            }
            return fileConfig;
        }

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
                    if (System.Enum.TryParse<LootLockerMultiUserSessionMode>(args[i + 1], true, out LootLockerMultiUserSessionMode multiUserSessionMode)
                        && multiUserSessionMode != LootLockerMultiUserSessionMode.NotSet)
                    {
                        this.multiUserSessionMode = multiUserSessionMode;
                    }
                }
                else if (args[i] == "-enableeditoradminextension")
                {
                    if (bool.TryParse(args[i + 1], out bool enableEditorAdminExtension))
                    {
                        this.enableEditorAdminExtension = enableEditorAdminExtension;
                    }
                }
            }
#endif
        }

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
            // Version provided by an external config file — treat as authoritative and
            // don't overwrite it from the package manifest.
            if (Array.Exists(AssetDatabase.FindAssets($"{ConfigFileName} t:TextAsset"),
                g => AssetDatabase.GUIDToAssetPath(g).EndsWith($"/{ConfigFileName}{ConfigFileExtension}", StringComparison.OrdinalIgnoreCase)))
            {
                if (!SdkVersionDetermined)
                {
                    SdkVersionDetermined = true;
                    OnSdkVersionDetermined?.Invoke();
                }
                return;
            }
            // If we already have a cached version, still re-query to keep sdk_version
            // current after SDK updates on disk (fixes stale version on upgrade).
            // We only raise OnSdkVersionDetermined once the authoritative value has
            // been resolved by Client.List(), so subscribers never act on stale data.
            ListInstalledPackagesRequest = Client.List();
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
            if (ListInstalledPackagesRequest.IsCompleted)
            {
                EditorApplication.update -= ListInstalledPackagesRequestProgress;
                foreach (var package in ListInstalledPackagesRequest.Result)
                {
                    if (package.name.Equals("com.lootlocker.lootlockersdk"))
                    {
                        LootLockerConfig.current.sdk_version = package.version;
                        LootLockerLogger.Log($"SDK Version v{LootLockerConfig.current.sdk_version}", LootLockerLogger.LogLevel.Verbose);
                        SdkVersionDetermined = true;
                        OnSdkVersionDetermined?.Invoke();
                        return;
                    }
                }

                string sdkPackageJsonPath = $"{GetSdkRootAssetsPath()}/package.json";
                if (File.Exists(sdkPackageJsonPath))
                {
                    LootLockerConfig.current.sdk_version = LootLockerJson.DeserializeObject<LLPackageDescription>(File.ReadAllText(sdkPackageJsonPath)).version;
                    LootLockerLogger.Log($"SDK Version v{LootLockerConfig.current.sdk_version}", LootLockerLogger.LogLevel.Verbose);
                    SdkVersionDetermined = true;
                    OnSdkVersionDetermined?.Invoke();
                    return;
                }


                foreach (var assetPath in AssetDatabase.GetAllAssetPaths())
                {
                    if (assetPath.EndsWith("package.json"))
                    {
                        var packageDescription = LootLockerJson.DeserializeObject<LLPackageDescription>(File.ReadAllText(assetPath));
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
        }
#endif

        public static bool CreateNewSettings(string apiKey, string gameVersion, string domainKey, LootLockerLogger.LogLevel logLevel = LootLockerLogger.LogLevel.Info, 
            bool logInBuilds = false, bool errorsAsWarnings = false, bool allowTokenRefresh = false, bool prettifyJson = false, bool obfuscateLogs = true, 
            bool enablePresence = false, bool enablePresenceAutoConnect = true, bool enablePresenceAutoDisconnectOnFocusChange = false, bool enablePresenceInEditor = true,
            LootLockerMultiUserSessionMode multiUserSessionMode = LootLockerMultiUserSessionMode.NotSet, bool enableEditorAdminExtension = true)
        {
            _current = Get();

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
            if(newConfig == null)
            {
                return false;
            }
            _current = Get();
            if (_current == null)
            {
                return false;
            }

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
        }
#endif
#endregion
    }
}