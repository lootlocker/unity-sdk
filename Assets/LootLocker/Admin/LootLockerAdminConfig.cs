#if UNITY_EDITOR
using System.IO;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    public class LootLockerAdminConfig : LootLockerGenericConfig
    {
#if UNITY_EDITOR
        static LootLockerAdminConfig()
        {
            ProjectSettingsBuildProcessor.OnBuild += OnProjectSettingsBuild;
        }

        private static void OnProjectSettingsBuild(List<ScriptableObject> list, List<string> names)
        {
            list.Add(Get());
            names.Add("LootLockerAdminConfig");
        }
#endif

        private static LootLockerAdminConfig settingsInstance;

        public string SettingsPath
        {
            get
            {
#if UNITY_EDITOR
                return $"{ProjectSettingsConsts.ROOT_FOLDER}/{SettingName}.asset";
#else
                return $"{ProjectSettingsConsts.PACKAGE_NAME}/{SettingName}";
#endif
            }
        }

        public virtual string SettingName { get { return "LootLockerAdminConfig"; } }

        public static LootLockerAdminConfig Get()
        {
            if (settingsInstance != null)
            {
                return settingsInstance;
            }

            LootLockerAdminConfig tempInstance = CreateInstance<LootLockerAdminConfig>();
#if UNITY_EDITOR
            string path = tempInstance.SettingsPath;

            if (!File.Exists(path))
            {
                settingsInstance = CreateInstance<LootLockerAdminConfig>();
                ProjectSettingsHelper.Save(settingsInstance, path);
            }
            else
            {
                settingsInstance = ProjectSettingsHelper.Load<LootLockerAdminConfig>(path);
            }

            settingsInstance.hideFlags = HideFlags.HideAndDontSave;
            return settingsInstance;
#else
            settingsInstance = Resources.Load<LootLockerAdminConfig>(tempInstance.SettingsPath);
            return settingsInstance;
#endif
        }

#if UNITY_EDITOR
        public void EditorSave()
        {
            ProjectSettingsHelper.Save(settingsInstance, SettingsPath);
        }
#endif

        private static LootLockerAdminConfig _current;

        public static LootLockerAdminConfig current
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
    }
}
