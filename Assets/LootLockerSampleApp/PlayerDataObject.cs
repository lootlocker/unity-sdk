using LootLocker;
using LootLocker.Requests;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LootLockerDemoApp
{

    [CreateAssetMenu(fileName = "PlayerData", menuName = "LootLockerDemoApp/PlayerData", order = 1)]
    public class PlayerDataObject : ScriptableObject
    {
        [SerializeField]
        string _playerName;
        [SerializeField]
        string _playerId;
        LootLockerSessionResponse _session;
        public LootLockerSessionResponse session => _session;
        public string playerName => _playerName;
        public string playerId => _playerId;
        public string playerStorageKeyNameToUse => "easyPrefabLocalplayers";
        [SerializeField]
        LootLockerCharacter _lootLockerCharacter;
        public LootLockerCharacter lootLockerCharacter => _lootLockerCharacter;

        public bool swappingCharacter;

        public void SaveCharacter(string playerName, LootLockerCharacter lootLockerCharacter = null)
        {
            this._playerName = !string.IsNullOrEmpty(playerName) ? playerName : this._playerName;
            if (lootLockerCharacter != null)
            {
                this._lootLockerCharacter = lootLockerCharacter;
                SavePlayerToDisk(lootLockerCharacter);
            }
        }

        public void SaveCharacter(LootLockerCharacter lootLockerCharacter = null)
        {
            this._lootLockerCharacter = lootLockerCharacter;
            if (lootLockerCharacter != null)
            {
                SavePlayerToDisk(lootLockerCharacter);
            }
        }

        public void SavePlayerToDisk(LootLockerCharacter lootLockerCharacter = null)
        {
            List<LocalPlayer> localPlayers = JsonConvert.DeserializeObject<List<LocalPlayer>>(PlayerPrefs.GetString(playerStorageKeyNameToUse));
            if (localPlayers == null) localPlayers = new List<LocalPlayer>();
            LocalPlayer tempLocal = new LocalPlayer { playerName = playerName, uniqueID = playerId, characterClass = lootLockerCharacter};
            if (!string.IsNullOrEmpty(playerName))
            {
                int index = -1;
                LocalPlayer local = localPlayers.FirstOrDefault(x => x.playerName == playerName);
                if (local != null)
                {
                    index = localPlayers.IndexOf(local);
                    localPlayers[index] = tempLocal;
                }
                else
                {
                    localPlayers.Add(tempLocal);
                }
            }
            PlayerPrefs.SetString(playerStorageKeyNameToUse, JsonConvert.SerializeObject(localPlayers));
        }

        public void SavePlayer(string playerName, string playerId = "")
        {
            this._playerName = !string.IsNullOrEmpty(playerName) ? playerName : this._playerName;
            this._playerId = !string.IsNullOrEmpty(playerId) ? playerId : this._playerId;
            SavePlayerToDisk();
        }

        public void SaveSession(LootLockerSessionResponse session)
        {
            this._session = session;
        }

        //public void SaveCharacter(string playerName, bool isDefaut, string playerClass = "", string characterName = "")
        //{
        //    this._playerName = !string.IsNullOrEmpty(playerName) ? playerName : this._playerName;
        //    this._playerClass = !string.IsNullOrEmpty(playerClass) ? playerClass : this._playerClass;
        //    this._currentCharacterName = !string.IsNullOrEmpty(characterName) ? characterName : this._currentCharacterName;
        //    this._isDefaut = isDefaut;
        //}

        //public void SaveCharacter(string playerName, string playerClass = "", string characterName = "")
        //{
        //    this._playerName = !string.IsNullOrEmpty(playerName) ? playerName : this._playerName;
        //    this._playerClass = !string.IsNullOrEmpty(playerClass) ? playerClass : this._playerClass;
        //    this._currentCharacterName = !string.IsNullOrEmpty(characterName) ? characterName : this._currentCharacterName;
        //}

    }
}