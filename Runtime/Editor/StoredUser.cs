using LootLocker;
using LootLocker.Extension.Requests;
using System.IO;
using UnityEditor;
using UnityEngine;

public class StoredUser : ScriptableObject
{
    [HideInInspector]
    public User user = null;

    [HideInInspector]
    public string serializedUser;
 
    private void Deserialize()
    {
        if(user == null || user.id == 0)
        {
            user = LootLockerJson.DeserializeObject<User>(serializedUser);
        }

    }

    private void Serialize()
    {
        if(user == null)
        {
            serializedUser = null;
            return;
        }
        serializedUser = LootLockerJson.SerializeObject(user);
    }

    private static StoredUser Get()
    {
        if (_current != null)
        {
            return _current;
        }
        _current = Resources.Load<StoredUser>("Config/LootLockerUser");

#if UNITY_EDITOR

        if (_current == null)
        {
            StoredUser newUser = CreateInstance<StoredUser>();

            string dir = Application.dataPath + "/LootLockerSDK/Resources/Config";

            // If directory does not exist, create it
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string configAssetPath = "Assets/LootLockerSDK/Resources/Config/LootLockerUser.asset";
            AssetDatabase.CreateAsset(newUser, configAssetPath);
            EditorApplication.delayCall += AssetDatabase.SaveAssets;
            AssetDatabase.Refresh();
        }
#endif
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

    public static bool CreateNewUser(User _user)
    {
        current.user = _user;
        current.Serialize();
        return true;
    }

}
