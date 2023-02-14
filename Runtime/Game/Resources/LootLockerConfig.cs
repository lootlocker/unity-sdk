using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
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
                newConfig.platform = platformType.Unused;

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
            }

#else
            if (settingsInstance == null)
            {
                throw new ArgumentException("LootLocker config does not exist. To fix this, play once in the Unity Editor before making a build.");
            }
#endif
            return settingsInstance;
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
#endif
        public static bool CreateNewSettings(string apiKey, string gameVersion, string domainKey, bool onDevelopmentMode = false, platformType platform = platformType.Unused, DebugLevel debugLevel = DebugLevel.All, bool allowTokenRefresh = false)
        {
            _current = Get();

            _current.apiKey = apiKey;
            _current.game_version = gameVersion;
            if(platform != platformType.Unused) {
                _current.platform = platform;
            }
            _current.developmentMode = onDevelopmentMode;
            _current.currentDebugLevel = debugLevel;
            _current.allowTokenRefresh = allowTokenRefresh;
            _current.domainKey = domainKey;

            return true;
        }

        // TODO: Deprecated, remove in version 1.2.0
        public bool IsPrefixedApiKey()
        {
            return !string.IsNullOrEmpty(apiKey) && (apiKey.StartsWith("dev_") || apiKey.StartsWith("prod_"));
        }

        // TODO: Deprecated, remove in version 1.2.0
        public static void AddDevelopmentModeFieldToJsonStringIfNeeded(ref string json)
        {
            if (!current.IsPrefixedApiKey())
            {
                json = json.Remove(json.Length - 1, 1); // Remove '}'
                string devModeJsonString = ", \"development_mode\": " + current.developmentMode;
                json = json + devModeJsonString.ToLower() + "}";
            }
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
        [HideInInspector]
        public string adminToken;
        [HideInInspector]
        public string domainKey;
        [HideInInspector]
        public int gameID;
        public string game_version = "1.0.0.0";
        [HideInInspector]
        public string deviceID = "defaultPlayerId";
        [HideInInspector]
        public platformType platform; // TODO: Deprecated, remove in version 1.2.0
        public enum platformType { Android, iOS, Steam, PlayStationNetwork, Unused }
        [HideInInspector]
        public bool developmentMode = true;
        [HideInInspector]
        public string url = "https://api.lootlocker.io/game/v1";
        [HideInInspector]
        public string adminUrl = "https://api.lootlocker.io/admin";
        [HideInInspector]
        public string playerUrl = "https://api.lootlocker.io/player";
        [HideInInspector]
        public string userUrl = "https://api.lootlocker.io/game";
        [HideInInspector]
        public string baseUrl = "https://api.lootlocker.io";
        public enum DebugLevel { All, ErrorOnly, NormalOnly, Off , AllAsNormal}
        public DebugLevel currentDebugLevel = DebugLevel.All;
        public bool allowTokenRefresh = true;

        public void UpdateToken(string _token, string _player_identifier)
        {
            token = _token;
            deviceID = _player_identifier;
        }
    }
}