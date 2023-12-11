using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
#endif
using UnityEngine;

namespace LootLocker
{

    public class LootLockerConfig : ScriptableObject
    {

        private static LootLockerConfig settingsInstance;

        public virtual string SettingName { get { return "LootLockerConfig"; } }

        public static LootLockerConfig Get()
        {
            if (settingsInstance != null)
            {
                settingsInstance.ConstructUrls();
#if LOOTLOCKER_COMMANDLINE_SETTINGS
                settingsInstance.CheckForSettingOverrides();
#endif
                return settingsInstance;
            }

            //Try to load it
            settingsInstance = Resources.Load<LootLockerConfig>("Config/LootLockerConfig");

#if UNITY_EDITOR
            // Could not be loaded, create it
            if (settingsInstance == null)
            {
                // Create a new Config
                LootLockerConfig newConfig = ScriptableObject.CreateInstance<LootLockerConfig>();

                // Folder needs to exist for Unity to be able to create an asset in it
                string dir = Application.dataPath+ "/LootLockerSDK/Resources/Config";

                // If directory does not exist, create it
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Create config asset
                string configAssetPath = "Assets/LootLockerSDK/Resources/Config/LootLockerConfig.asset";
                AssetDatabase.CreateAsset(newConfig, configAssetPath);
                EditorApplication.delayCall += AssetDatabase.SaveAssets;
                AssetDatabase.Refresh();
                settingsInstance = newConfig;
            }

#else
            if (settingsInstance == null)
            {
                throw new ArgumentException("LootLocker config does not exist. To fix this, play once in the Unity Editor before making a build.");
            }
#endif
            settingsInstance.ConstructUrls();
#if LOOTLOCKER_COMMANDLINE_SETTINGS
            settingsInstance.CheckForSettingOverrides();
#endif
            return settingsInstance;
        }

        private void CheckForSettingOverrides()
        {
#if LOOTLOCKER_COMMANDLINE_SETTINGS
            string[] args = System.Environment.GetCommandLineArgs();
            string _apiKey = null;
            string _domainKey = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-apikey")
                {
                    _apiKey = args[i + 1];
                }
                else if (args[i] == "-domainkey")
                {
                    _domainKey = args[i + 1];
                }
            }

            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_domainKey))
            {
                return;
            }
            apiKey = _apiKey;
            domainKey = _domainKey;
#endif
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void CreateConfigFile()
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
        }

        protected static ListRequest ListInstalledPackagesRequest;

        [InitializeOnLoadMethod]
        static void StoreSDKVersion()
        {
            if ((!string.IsNullOrEmpty(LootLockerConfig.current.sdk_version) &&
                 !LootLockerConfig.current.sdk_version.Equals("N/A")) || ListInstalledPackagesRequest != null)
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
                        return;
                    }
                }

                if (File.Exists("Assets/LootLockerSDK/package.json"))
                {
                    LootLockerConfig.current.sdk_version = LootLockerJson.DeserializeObject<LLPackageDescription>(File.ReadAllText("Assets/LootLockerSDK/package.json")).version;
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
                            return;
                        }
                    }
                }

                LootLockerConfig.current.sdk_version = "N/A";
            }
        }
#endif
        public static bool CreateNewSettings(string apiKey, string gameVersion, string domainKey, LootLockerConfig.DebugLevel debugLevel = DebugLevel.All, bool allowTokenRefresh = false)
        {
            _current = Get();

            _current.apiKey = apiKey;
            _current.game_version = gameVersion;
            _current.currentDebugLevel = debugLevel;
            _current.allowTokenRefresh = allowTokenRefresh;
            _current.domainKey = domainKey;
            _current.ConstructUrls();
            return true;
        }

        private void ConstructUrls()
        {
            string startOfUrl = UrlProtocol;
            if (!string.IsNullOrEmpty(domainKey))
            {
                startOfUrl += domainKey + ".";
            }
            adminUrl = startOfUrl + GetUrlCore() + AdminUrlAppendage;
            playerUrl = startOfUrl + GetUrlCore() + PlayerUrlAppendage;
            userUrl = startOfUrl + GetUrlCore() + UserUrlAppendage;
            baseUrl = startOfUrl + GetUrlCore();
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
        public string apiKey;
        [HideInInspector]
        public string token;
#if UNITY_EDITOR
        [HideInInspector]
        public string adminToken;
#endif
        [HideInInspector]
        public string refreshToken;
        [HideInInspector]
        public string domainKey;
        [HideInInspector]
        public int gameID;
        public string game_version = "1.0.0.0";
        [HideInInspector] 
        public string sdk_version = "";
        [HideInInspector]
        public string deviceID = "defaultPlayerId";

        [HideInInspector] private static readonly string UrlProtocol = "https://";
        [HideInInspector] private static readonly string UrlCore = "api.lootlocker.io";
        [HideInInspector] private static readonly string UrlCoreOverride =
#if LOOTLOCKER_TARGET_STAGE_ENV
           "api.stage.internal.dev.lootlocker.cloud";
#else
            null;
#endif
        private static string GetUrlCore() { return string.IsNullOrEmpty(UrlCoreOverride) ? UrlCore : UrlCoreOverride; }
        [HideInInspector] private static readonly string UrlAppendage = "/v1";
        [HideInInspector] private static readonly string AdminUrlAppendage = "/admin";
        [HideInInspector] private static readonly string PlayerUrlAppendage = "/player";
        [HideInInspector] private static readonly string UserUrlAppendage = "/game";

        [HideInInspector] public string url = UrlProtocol + GetUrlCore() + UrlAppendage;

        [HideInInspector] public string adminUrl = UrlProtocol + GetUrlCore() + AdminUrlAppendage;
        [HideInInspector] public string playerUrl = UrlProtocol + GetUrlCore() + PlayerUrlAppendage;
        [HideInInspector] public string userUrl = UrlProtocol + GetUrlCore() + UserUrlAppendage;
        [HideInInspector] public string baseUrl = UrlProtocol + GetUrlCore();
        [HideInInspector] public float clientSideRequestTimeOut = 5f;
        public enum DebugLevel { All, ErrorOnly, NormalOnly, Off , AllAsNormal}
        public DebugLevel currentDebugLevel = DebugLevel.All;
        public bool allowTokenRefresh = true;

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            _current = null;
        }
#endif
    }
}