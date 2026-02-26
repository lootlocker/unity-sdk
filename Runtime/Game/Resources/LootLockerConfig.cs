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
    [Serializable]
    public class LootLockerConfig : ScriptableObject
    {
        public static readonly string PackageShortName = "LL"; // Standard is LL
        private static readonly string PackageName = "LootLocker"; // Standard is LootLocker
        private static readonly string SettingsName = $"{PackageName}Config";
        private static readonly string ConfigFolderName = $"{PackageName}SDK";
        private static readonly string ConfigAssetName = $"{SettingsName}.asset";
        private static readonly string ConfigAssetsPath = $"Assets/{ConfigFolderName}/Resources/Config/{ConfigAssetName}";
        private static readonly string ConfigResourceFolder = $"{Application.dataPath}/{ConfigFolderName}/Resources/Config";
        private static readonly string ConfigFileIdentifier = ""; // Optional extra package id if you want to be able to switch between multiple configurations
        private static readonly string ConfigFileExtension = ".bytes";
        private static readonly string ConfigFileName = $"{SettingsName}" + (string.IsNullOrEmpty(ConfigFileIdentifier) ? "" : $"-{ConfigFileIdentifier}");
        private static readonly string ConfigFilePath = $"Assets/{ConfigFolderName}/Resources/Config/{ConfigFileName}{ConfigFileExtension}";


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

            var fileConfig = CheckForFileConfig();
            if (fileConfig != null && !string.IsNullOrEmpty(fileConfig.api_key))
            {
                if(!File.Exists(ConfigAssetsPath))
                {
                    CreateConfigFile();                    
                }
                else
                {
                    _current = Resources.Load<LootLockerConfig>($"Config/{SettingsName}");
                }
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
                _current.adminToken = null;
                
                return _current;
            }

            //Try to load it
            _current = Resources.Load<LootLockerConfig>($"Config/{SettingsName}");

#if UNITY_EDITOR
            // Could not be loaded, create it
            if (_current == null)
            {
                CreateConfigFile();
            }

#else
            if (_current == null)
            {
                throw new ArgumentException($"{ConfigFolderName} config does not exist. To fix this, play once in the Unity Editor before making a build.");
            }
#endif
#if LOOTLOCKER_COMMANDLINE_SETTINGS
            _current.CheckForSettingOverrides();
#endif
            _current.ConstructUrls();
            return _current;
        }
        
        private static ExternalFileConfig CheckForFileConfig()
        {
            ExternalFileConfig fileConfig = null;
            try {
                if(!File.Exists(ConfigFilePath))
                {
                    return fileConfig;
                }
                AssetDatabase.ImportAsset(ConfigFilePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.Refresh();
                
                TextAsset configTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigFilePath);
                
                // Fallback: if Unity asset system isn't ready, read file directly from disk
                string configText = null;
                if(configTextAsset != null)
                {
                    configText = configTextAsset.text;
                }
                else
                {
                    // Direct file read as fallback
                    configText = File.ReadAllText(ConfigFilePath);
                }
                
                if(!string.IsNullOrEmpty(configText))
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
                else
                {
                }
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log("Error while checking for file config: " + ex.Message, LootLockerLogger.LogLevel.Error);
                return fileConfig;
            }
            return fileConfig;            
        }

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

            StoreSDKVersion();
        }

        protected static ListRequest ListInstalledPackagesRequest;

        static void StoreSDKVersion()
        {
            if ((!string.IsNullOrEmpty(LootLockerConfig.current.sdk_version) && !LootLockerConfig.current.sdk_version.Equals("N/A")) 
                || ListInstalledPackagesRequest != null
                || File.Exists(ConfigFilePath) /*Configured through config file, read sdk version there*/)
            {
                return;
            }
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
                        return;
                    }
                }

                if (File.Exists("Assets/LootLockerSDK/package.json"))
                {
                    LootLockerConfig.current.sdk_version = LootLockerJson.DeserializeObject<LLPackageDescription>(File.ReadAllText("Assets/LootLockerSDK/package.json")).version;
                    LootLockerLogger.Log($"SDK Version v{LootLockerConfig.current.sdk_version}", LootLockerLogger.LogLevel.Verbose);
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
                            return;
                        }
                    }
                }

                LootLockerConfig.current.sdk_version = "N/A";
            }
        }
#endif

        public static bool CreateNewSettings(string apiKey, string gameVersion, string domainKey, LootLockerLogger.LogLevel logLevel = LootLockerLogger.LogLevel.Info, 
            bool logInBuilds = false, bool errorsAsWarnings = false, bool allowTokenRefresh = false, bool prettifyJson = false, bool obfuscateLogs = true, 
            bool enablePresence = false, bool enablePresenceAutoConnect = true, bool enablePresenceAutoDisconnectOnFocusChange = false, bool enablePresenceInEditor = true)
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
#if LOOTLOCKER_TARGET_STAGE_ENV
           "api.stage.internal.dev.lootlocker.cloud";
#elif LOOTLOCKER_TARGET_LOCAL_ENV
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
        }
#endif
#endregion
    }
}