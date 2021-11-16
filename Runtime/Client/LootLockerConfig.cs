using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            settingsInstance = Resources.Load<LootLockerConfig>("Config/LootLockerConfig");
            return settingsInstance;
        }

        public static bool CreateNewSettings(string apiKey, string gameVersion, platformType platform, bool onDevelopmentMode, string domainKey, DebugLevel debugLevel = DebugLevel.Off, bool allowTokenRefresh = false)
        {
            settingsInstance = Resources.Load<LootLockerConfig>("Config/LootLockerConfig");

            if (settingsInstance == null)
                settingsInstance = CreateInstance<LootLockerConfig>();

            settingsInstance.apiKey = apiKey;
            settingsInstance.game_version = gameVersion;
            settingsInstance.platform = platform;
            settingsInstance.developmentMode = onDevelopmentMode;
            settingsInstance.currentDebugLevel = debugLevel;
            settingsInstance.allowTokenRefresh = allowTokenRefresh;
            settingsInstance.domainKey = domainKey;

            return true;
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
        public (string key, string value) dateVersion = ( "LL-Version", "2021-03-01");
        public string apiKey;
        [HideInInspector]
        public string token;
        [HideInInspector]
        public string adminToken;
        [HideInInspector]
        public string domainKey;
        [HideInInspector]
        public int gameID;
        public string game_version = "1.0";
        [HideInInspector]
        public string deviceID = "defaultPlayerId";
        public platformType platform;
        public enum platformType { Android, iOS, Steam, PlayStationNetwork }
        public bool developmentMode;
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
        public enum DebugLevel { All, ErrorOnly, NormalOnly, Off }
        public DebugLevel currentDebugLevel;
        public bool allowTokenRefresh = true;

        public void UpdateToken(string _token, string _player_identifier)
        {
            token = _token;
            deviceID = _player_identifier;
        }

    }
}