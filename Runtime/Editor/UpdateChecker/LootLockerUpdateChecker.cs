#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;

namespace LootLocker.Extension
{
    [InitializeOnLoad]
    internal static class LootLockerUpdateChecker
    {
        // EditorPrefs keys — global per machine, not per project
        private const string PrefNeverNotify    = "com.lootlocker.sdk.updatecheck.nevernotify";
        private const string PrefSkippedVersion = "com.lootlocker.sdk.updatecheck.skippedversion";
        private const string PrefRemindAfter    = "com.lootlocker.sdk.updatecheck.remindafter";
        private const string PrefLastChecked    = "com.lootlocker.sdk.updatecheck.lastchecked";

        private const string GitHubReleasesUrl  = "https://api.github.com/repos/lootlocker/unity-sdk/releases/latest";
        private const double CheckIntervalHours = 24.0;

        private static UnityWebRequest _activeRequest;
        private static bool _isManualCheck;

        // How long to wait after editor startup before running the automatic update check.
        // Gives heavy projects time to finish loading before a dialog appears.
        // Does not affect manual checks via the menu item.
        // Adjust here in source (range: 30–300 seconds is reasonable).
        private const double StartupDelaySeconds = 180.0;
        private static double _startupCheckTime = -1.0;

        static LootLockerUpdateChecker()
        {
            // Delay initialization by the startup delay, then decide whether to check
            // immediately or wait for the SDK version to be resolved.
            _startupCheckTime = EditorApplication.timeSinceStartup + StartupDelaySeconds;
            EditorApplication.update += WaitForStartupDelay;
        }

        private static void WaitForStartupDelay()
        {
            if (EditorApplication.timeSinceStartup < _startupCheckTime)
                return;
            EditorApplication.update -= WaitForStartupDelay;
            _startupCheckTime = -1.0;

            if (LootLockerConfig.SdkVersionDetermined)
            {
                TriggerPeriodicCheck();
            }
            else
            {
                LootLockerConfig.OnSdkVersionDetermined += OnSdkVersionDetermined;
            }
        }

        private static void OnSdkVersionDetermined()
        {
            LootLockerConfig.OnSdkVersionDetermined -= OnSdkVersionDetermined;
            TriggerPeriodicCheck();
        }

        private static void TriggerPeriodicCheck()
        {
            if (Application.isBatchMode)
                return;

            if (EditorPrefs.GetBool(PrefNeverNotify, false))
                return;

            long lastCheckedTicks = 0;
            long.TryParse(EditorPrefs.GetString(PrefLastChecked, "0"), out lastCheckedTicks);
            if (lastCheckedTicks > 0 && (DateTime.UtcNow - new DateTime(lastCheckedTicks, DateTimeKind.Utc)).TotalHours < CheckIntervalHours)
                return;

            FetchLatestRelease(isManualCheck: false);
        }

        [MenuItem("Window/LootLocker/Check for Updates", false, 102)]
        public static void ManualCheck()
        {
            FetchLatestRelease(isManualCheck: true);
        }

        private static void FetchLatestRelease(bool isManualCheck)
        {
            if (_activeRequest != null)
                return;

            if (IsUpmManagedByRegistry())
            {
                if (isManualCheck)
                    EditorUtility.DisplayDialog(
                        "LootLocker Update Check",
                        "This SDK is managed by the Unity Package Manager registry. Use the Package Manager window (Window \u2192 Package Manager) to check for and apply updates.",
                        "OK");
                return;
            }

            _isManualCheck = isManualCheck;
            _activeRequest = UnityWebRequest.Get(GitHubReleasesUrl);
            _activeRequest.SetRequestHeader("User-Agent", "LootLocker Unity SDK Update Checker");
            _activeRequest.SendWebRequest();
            EditorApplication.update += PollRequest;
        }

        private static bool IsUpmManagedByRegistry()
        {
            try
            {
                var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(LootLockerConfig).Assembly);
                return pkg != null && pkg.source == PackageSource.Registry;
            }
            catch
            {
                return false;
            }
        }

        private static void PollRequest()
        {
            if (_activeRequest == null || !_activeRequest.isDone)
                return;

            EditorApplication.update -= PollRequest;

#if UNITY_2020_1_OR_NEWER
            bool failed = _activeRequest.result != UnityWebRequest.Result.Success;
#else
            bool failed = _activeRequest.isNetworkError || _activeRequest.isHttpError;
#endif
            string responseText = failed ? null : _activeRequest.downloadHandler.text;
            _activeRequest.Dispose();
            _activeRequest = null;

            if (failed || string.IsNullOrEmpty(responseText))
            {
                if (_isManualCheck)
                    EditorUtility.DisplayDialog(
                        "LootLocker Update Check",
                        "Could not reach GitHub to check for updates. Please try again later.",
                        "OK");
                // Don't stamp last-checked on network failure so the next editor open retries.
                return;
            }

            string tagName = ExtractJsonStringValue(responseText, "tag_name");
            string htmlUrl = ExtractJsonStringValue(responseText, "html_url");

            if (string.IsNullOrEmpty(tagName))
            {
                if (_isManualCheck)
                    EditorUtility.DisplayDialog(
                        "LootLocker Update Check",
                        "Could not parse release information. Visit https://github.com/lootlocker/unity-sdk/releases to check manually.",
                        "OK");
                // Don't stamp last-checked on parse failure so the next editor open retries.
                return;
            }

            // Stamp last-checked now that we have a valid response.
            EditorPrefs.SetString(PrefLastChecked, DateTime.UtcNow.Ticks.ToString());

            string latestVersion = tagName.TrimStart('v');
            string currentVersion = LootLockerConfig.current.sdk_version;
            if (string.IsNullOrEmpty(currentVersion) || currentVersion.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                currentVersion = null;

            if (!IsVersionNewer(latestVersion, currentVersion))
            {
                if (_isManualCheck)
                {
                    string displayCurrent = string.IsNullOrEmpty(currentVersion) ? "unknown" : $"v{currentVersion}";
                    EditorUtility.DisplayDialog(
                        "LootLocker Update Check",
                        $"You are on the latest version ({displayCurrent}).",
                        "OK");
                }
                return;
            }

            if (!_isManualCheck)
            {
                if (EditorPrefs.GetString(PrefSkippedVersion, "") == latestVersion)
                    return;

                long remindAfterTicks = 0;
                long.TryParse(EditorPrefs.GetString(PrefRemindAfter, "0"), out remindAfterTicks);
                if (remindAfterTicks > 0 && DateTime.UtcNow.Ticks < remindAfterTicks)
                    return;
            }

            LootLockerUpdateNotificationWindow.Show(currentVersion, latestVersion, htmlUrl ?? GitHubReleasesUrl);
        }

        // Minimal JSON string-value extractor — avoids a full JSON parse for a single field.
        // Finds the first occurrence of "key": "value" and returns the value.
        private static string ExtractJsonStringValue(string json, string key)
        {
            string searchKey = "\"" + key + "\"";
            int keyIndex = json.IndexOf(searchKey, StringComparison.Ordinal);
            if (keyIndex < 0) return null;

            int colonIndex = json.IndexOf(':', keyIndex + searchKey.Length);
            if (colonIndex < 0) return null;

            int quoteStart = json.IndexOf('"', colonIndex + 1);
            if (quoteStart < 0) return null;

            int quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) return null;

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        // Returns true if candidate is strictly newer than current (numeric major.minor.patch comparison).
        // If current is null/empty we can't confirm the user is up to date, so we return true
        // (conservative: show the notification rather than silently skip).
        internal static bool IsVersionNewer(string candidate, string current)
        {
            if (string.IsNullOrEmpty(candidate))
                return false;

            if (string.IsNullOrEmpty(current))
                return true; // Unknown installed version — surface the notification.

            int[] c   = ParseVersionParts(candidate);
            int[] cur = ParseVersionParts(current);
            int len = Math.Max(c.Length, cur.Length);
            for (int i = 0; i < len; i++)
            {
                int cv   = i < c.Length   ? c[i]   : 0;
                int curv = i < cur.Length ? cur[i] : 0;
                if (cv > curv) return true;
                if (cv < curv) return false;
            }
            return false;
        }

        private static int[] ParseVersionParts(string version)
        {
            string[] parts = version.Split('.');
            int[] result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                int.TryParse(parts[i], out result[i]);
            return result;
        }
    }

    internal class LootLockerUpdateNotificationWindow : EditorWindow
    {
        private string _currentVersion;
        private string _latestVersion;
        private string _releaseUrl;

        internal static void Show(string currentVersion, string latestVersion, string releaseUrl)
        {
            var window = GetWindow<LootLockerUpdateNotificationWindow>(true, "LootLocker Update Available", true);
            window._currentVersion = currentVersion;
            window._latestVersion  = latestVersion;
            window._releaseUrl     = releaseUrl;
            window.minSize = new Vector2(480, 170);
            window.maxSize = new Vector2(480, 170);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("LootLocker Unity SDK update available!", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Installed: v{_currentVersion}   \u2022   Latest: v{_latestVersion}");
            EditorGUILayout.Space(6);

            if (GUILayout.Button("See What's New \u2197"))
                Application.OpenURL(_releaseUrl);

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Update Now"))
            {
                Application.OpenURL(_releaseUrl);
                Close();
            }

            if (GUILayout.Button("Skip This Version"))
            {
                EditorPrefs.SetString("com.lootlocker.sdk.updatecheck.skippedversion", _latestVersion);
                Close();
            }

            if (GUILayout.Button("Remind in 7 Days"))
            {
                EditorPrefs.SetString("com.lootlocker.sdk.updatecheck.remindafter", DateTime.UtcNow.AddDays(7).Ticks.ToString());
                Close();
            }

            if (GUILayout.Button("Never Notify"))
            {
                EditorPrefs.SetBool("com.lootlocker.sdk.updatecheck.nevernotify", true);
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
