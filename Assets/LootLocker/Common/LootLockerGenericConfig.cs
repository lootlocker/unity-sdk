using LootLocker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    [CreateAssetMenu(fileName = "Config", menuName = "ScriptableObjects/Config", order = 1)]
    public class LootLockerGenericConfig : ScriptableObject
    {
        [HideInInspector]
        public string gameName;
        public string apiKey;
        [HideInInspector]
        public string token;
        [HideInInspector]
        public int gameID;
        public string game_version = "1.0";
        [HideInInspector]
        public string deviceID = "defaultPlayerId";
        [HideInInspector]
        public string email, password;
        [HideInInspector]
        public string playerName;
        [HideInInspector]
        public string playerClass;
        public platformType platform;
        public environmentType environment;
        public enum environmentType { Development, Live }
        public enum platformType { android, ios, Steam, Windows }
        public bool developmentMode => environment == environmentType.Development ? true : false;
        [HideInInspector]
        public string url = "https://api.lootlocker.io/game/v1";
        [HideInInspector]
        public string adminUrl = "https://api.lootlocker.io/admin";
        [HideInInspector]
        public string playerUrl = "https://api.lootlocker.io/player";
        [HideInInspector]
        public string userUrl = "https://api.lootlocker.io/game";

        public enum DebugLevel { All, ErrorOnly, NormalOnly, Off }

        public DebugLevel currentDebugLevel;

        public bool allowTokenRefresh = true;

        public void UpdateToken(string token, string deviceid)
        {
            this.deviceID = deviceid;
            this.token = token;
        }

        public void UpdateToken(string token)
        {
            this.token = token;
        }

        public void UpdateAPIKey(string key)
        {
            this.apiKey = key;
        }

        public void UpdateDeviceId(string deviceid)
        {
            this.deviceID = deviceid;
        }

        public void UpdateUrl(bool isAdmin)
        {
            url = isAdmin ? adminUrl : userUrl;
        }
        public LootLockerGenericConfig()
        {
            LootLockerBaseServerAPI.activeConfig = this;
        }

    }
}