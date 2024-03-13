
#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER
using UnityEditor;
namespace LootLocker.Extension
{

    public class LootLockerEditorData
    {
        private static string prefix = PlayerSettings.productGUID.ToString() + ".LootLocker.";

        private static string adminToken = prefix + "AdminToken";
        private static string selectedGameID = prefix + "SelectedGameID";
        private static string selectedGameName = prefix + "SelectedGameName";
        private static string environment = prefix + "Environment";
        private static string firstTimeWelcome = prefix + "FirstTimeWelcome";
        private static string newSession = prefix + "NewSession";

        public static void ClearLootLockerPrefs()
        {
            EditorPrefs.DeleteKey(adminToken);
            EditorPrefs.DeleteKey(selectedGameID);
            EditorPrefs.DeleteKey(selectedGameName);
            EditorPrefs.DeleteKey(environment);
            EditorPrefs.DeleteKey(firstTimeWelcome);
            EditorPrefs.DeleteKey(newSession);
        }

        public static void SetAdminToken(string _adminToken)
        {
            EditorPrefs.SetString(adminToken, _adminToken);
            EditorPrefs.SetBool(firstTimeWelcome, false);
            EditorPrefs.SetBool(newSession, true);
        }

        public static string GetAdminToken()
        {
            return EditorPrefs.GetString(adminToken);
        }

        public static void SetSelectedGame(string _selectedGame)
        {
            EditorPrefs.SetInt(selectedGameID, int.Parse(_selectedGame));
        }
        public static void SetSelectedGameName(string _selectedGameName)
        {
            EditorPrefs.SetString(selectedGameName, _selectedGameName);
        }

        public static int GetSelectedGame()
        {
            return EditorPrefs.GetInt(selectedGameID);
        }

        public static string GetSelectedGameName()
        {
            return EditorPrefs.GetString(selectedGameName);
        }

        public static void SetEnvironmentStage()
        {
            EditorPrefs.SetString(environment, "Stage");
        }

        public static void SetEnvironmentLive()
        {
            EditorPrefs.SetString(environment, "Live");
        }

        public static bool IsEnvironmentStage()
        {
            return EditorPrefs.GetString(environment).Equals("Stage");
        }

        public static bool ShouldAutoShowWindow()
        {
            var result = EditorPrefs.GetBool(firstTimeWelcome, true);
            EditorPrefs.SetBool(firstTimeWelcome, false);
            return result;
        }

        public static bool IsNewSession()
        {
            bool result = EditorPrefs.GetBool(newSession, false);
            EditorPrefs.SetBool(newSession, false);
            return result;
        }
    }
}
#endif