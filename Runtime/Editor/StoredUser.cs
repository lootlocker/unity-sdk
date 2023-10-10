using LootLocker;
using LootLocker.Extension.Requests;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER
using LootLocker.Extension.DataTypes;

namespace LootLocker.Extension
{
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

        [InitializeOnLoadMethod]
        private static void FirstLoad()
        {
            EditorPrefs.DeleteAll();

            string projectPath = Application.dataPath;

            DateTime creationTime = Directory.GetCreationTime(projectPath);
            string configFileEditorPref = "StoredUserCreated" + creationTime.GetHashCode().ToString();

            if (EditorPrefs.GetBool(configFileEditorPref) == false)
            {

                if (Directory.Exists("Packages/com.lootlocker.lootlockersdk")) 
                {
                    if (Directory.Exists("Assets/LootLockerSDK/Runtime/Editor/VisualElements"))
                    {
                        Directory.Delete("Assets/LootLockerSDK/Runtime/Editor/VisualElements", recursive: true);
                    }

                    Directory.CreateDirectory("Assets/LootLockerSDK/Runtime/Editor/VisualElements/LootLocker MainWindow");

                    FileUtil.CopyFileOrDirectory("Packages/com.lootlocker.lootlockersdk/Runtime/Editor/VisualElements/LootLocker MainWindow/LootLockerMainWindow.uss", "Assets/LootLockerSDK/Runtime/Editor/VisualElements/LootLocker MainWindow/LootLockerMainWindow.uss");
                    //EditorApplication.delayCall += AssetDatabase.SaveAssets;
                    //AssetDatabase.Refresh();

                    if(Directory.Exists("Assets/LootLockerSDK/Runtime/Editor/Icons"))
                    {
                        Directory.Delete("Assets/LootLockerSDK/Runtime/Editor/Icons", recursive: true);
                    }
                    Directory.CreateDirectory("Assets/LootLockerSDK/Runtime/Editor/Icons");

                    FileUtil.CopyFileOrDirectory("Packages/com.lootlocker.lootlockersdk/Runtime/Editor/Icons/Hector_512x512g.png", "Assets/LootLockerSDK/Runtime/Editor/Icons/Hector_512x512g.png");
                    FileUtil.CopyFileOrDirectory("Packages/com.lootlocker.lootlockersdk/Runtime/Editor/Icons/caret.png","Assets/LootLockerSDK/Runtime/Editor/Icons/caret.png");
                    EditorApplication.delayCall += AssetDatabase.SaveAssets;
                    AssetDatabase.Refresh();
                }

                EditorPrefs.SetBool(configFileEditorPref, true);
            }

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
        _current = null;

        string configAssetPath =  Application.dataPath + "/LootLockerSDK/Runtime/Editor/Resources/Config/LootLockerUser.asset";
        string configAssetMetaPath =  Application.dataPath + "/LootLockerSDK/Runtime/Editor/Resources/Config/LootLockerUser.asset.meta";

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
        EditorPrefs.DeleteKey("LootLocker.mfaKey");
        EditorPrefs.DeleteKey("LootLocker.ActiveOrgID");
        EditorPrefs.DeleteKey("LootLocker.ActiveGameID");
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