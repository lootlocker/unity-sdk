using LootLocker;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER
using LootLocker.Extension.Requests;
using LootLocker.Extension.DataTypes;

namespace LootLocker.Extension
{
    public class StoredUser : ScriptableObject
    {
        [HideInInspector]
        public User user = null;

        [HideInInspector]
        public Game lastActiveGame;

        [HideInInspector]
        public string serializedUser;

        [HideInInspector]
        public string serializedGame;
        private void Deserialize()
        {
            if (user == null || user.id == 0)
            {
                user = LootLockerJson.DeserializeObject<User>(serializedUser);
            }

            if (lastActiveGame == null || lastActiveGame.id == 0)
            {
                lastActiveGame = LootLockerJson.DeserializeObject<Game>(serializedGame);
            }

        }

        private void Serialize()
        {
            if (user == null)
            {
                serializedUser = null;
                serializedGame = null;
                return;
            }
            serializedUser = LootLockerJson.SerializeObject(user);
            serializedGame = LootLockerJson.SerializeObject(lastActiveGame);



        }

        private static StoredUser Get()
        {
            if (_current != null)
            {
                return _current;
            }
            _current = Resources.Load<StoredUser>("/LootLockerSDK/Runtime/Editor/Resources/Config/LootLockerUser");

            if (_current == null)
            {
                StoredUser newUser = CreateInstance<StoredUser>();

                string dir = Application.dataPath + "/LootLockerSDK/Runtime/Editor/Resources/Config";

                // If directory does not exist, create it
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string configAssetPath = "Assets/LootLockerSDK/Runtime/Editor/Resources/Config/LootLockerUser.asset";
                AssetDatabase.CreateAsset(newUser, configAssetPath);
                EditorApplication.delayCall += AssetDatabase.SaveAssets;
                AssetDatabase.Refresh();

                _current = newUser;
            }

            _current.Deserialize();
            return _current;
        }

        private static StoredUser _current;

        public static StoredUser current
        {
            get
            {
                if (_current == null)
                {
                    return Get();
                }

                return _current;
            }
        }

        public bool RemoveUser()
        {
            DeleteLootLockerPrefs();

            _current.serializedUser = null;
            _current.user = null;
            _current.lastActiveGame = null;
            _current = null;

            string configAssetPath = Application.dataPath + "/LootLockerSDK/Runtime/Editor/Resources/Config/LootLockerUser.asset";
            string configAssetMetaPath = Application.dataPath + "/LootLockerSDK/Runtime/Editor/Resources/Config/LootLockerUser.asset.meta";

            if (Directory.Exists(configAssetPath))
            {
                File.Delete(configAssetPath);
            }
            if (Directory.Exists(configAssetMetaPath))
            {
                File.Delete(configAssetMetaPath);
            }
            AssetDatabase.Refresh();

            return true;
        }

        public void DeleteLootLockerPrefs()
        {
            EditorPrefs.DeleteKey("LootLocker.AdminToken");

        }

        public static bool CreateNewUser(User _user)
        {
            current.user = _user;
            current.Serialize();
            return true;
        }
    }
}

#endif