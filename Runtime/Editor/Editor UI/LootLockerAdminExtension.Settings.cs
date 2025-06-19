using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
namespace LootLocker.Extension
{
    public partial class LootLockerAdminExtension : EditorWindow
    {
        // Settings save methods - now with immediate saving
        private void SaveGameVersion(string newVersion)
        {
            if (!IsSemverString(newVersion))
            {
                if (gameVersionWarning != null)
                {
                    gameVersionWarning.text = "Game version must be in format X.Y.Z.B (MAJOR.MINOR.PATCH.BUILD, last two optional)";
                    gameVersionWarning.style.display = DisplayStyle.Flex;
                }
                return;
            }
            
            if (gameVersionWarning != null) gameVersionWarning.style.display = DisplayStyle.None;
            LootLockerConfig.current.game_version = newVersion;
            SaveConfig();
        }

        private void SaveLogLevel(LootLockerLogger.LogLevel newLogLevel)
        {
            LootLockerConfig.current.logLevel = newLogLevel;
            SaveConfig();
        }

        private void SaveLogErrorsAsWarnings(bool newValue)
        {
            LootLockerConfig.current.logErrorsAsWarnings = newValue;
            SaveConfig();
        }

        private void SaveLogInBuilds(bool newValue)
        {
            LootLockerConfig.current.logInBuilds = newValue;
            SaveConfig();
        }

        private void SaveAllowTokenRefresh(bool newValue)
        {
            LootLockerConfig.current.allowTokenRefresh = newValue;
            SaveConfig();
        }

        private void SaveConfig()
        {
            EditorUtility.SetDirty(LootLockerConfig.current);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }        // Loads current LootLockerConfig values into the settings UI
        public void LoadSettingsUI()
        {
            var config = LootLockerConfig.current;
            
            if (gameVersionField != null) 
                gameVersionField.SetValueWithoutNotify(config.game_version);
            
            if (logLevelField != null) 
            {
                logLevelField.Init(config.logLevel);
                logLevelField.SetValueWithoutNotify(config.logLevel);
            }
            
            if (logErrorsAsWarningsToggle != null) 
                logErrorsAsWarningsToggle.SetValueWithoutNotify(config.logErrorsAsWarnings);
            
            if (logInBuildsToggle != null) 
                logInBuildsToggle.SetValueWithoutNotify(config.logInBuilds);
            
            if (allowTokenRefreshToggle != null) 
                allowTokenRefreshToggle.SetValueWithoutNotify(config.allowTokenRefresh);
            
            if (gameVersionWarning != null) 
                gameVersionWarning.style.display = DisplayStyle.None;
        }

        private static bool IsSemverString(string str)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(str,
                @"^(0|[1-9]\d*)\.(0|[1-9]\d*)(?:\.(0|[1-9]\d*))?(?:\.(0|[1-9]\d*))?$");
        }

        // Simple flow switching
        void RequestFlowSwitch(VisualElement targetFlow, System.Action onSuccess = null)
        {
            SwapFlows(targetFlow);
            onSuccess?.Invoke();
        }
    }
}
#endif
