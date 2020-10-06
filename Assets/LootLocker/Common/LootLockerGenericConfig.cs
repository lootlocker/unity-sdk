using LootLocker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Config", menuName = "ScriptableObjects/Config", order = 1)]
public class LootLockerGenericConfig : ScriptableObject
{
    public string gameName;
    public string apiKey;
    public string token;
    public int gameID;
    public string game_version = "1.0";
    public string deviceID;
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
    public string url = "https://api.lootlocker.io/game/v1/";
    [HideInInspector]
    public string adminUrl = "https://api.lootlocker.io/admin";
    [HideInInspector]
    public string userUrl = "https://api.lootlocker.io/game";

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
        BaseServerAPI.activeConfig = this;
    }

}
