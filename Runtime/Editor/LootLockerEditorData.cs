
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
        private static string selectedOrganisationID = prefix + "SelectedOrganisationID";
        private static string firstTimeWelcome = prefix + "FirstTimeWelcome";
        private static string newSession = prefix + "NewSession";

        public static void ClearLootLockerPrefs()
        {
            EditorPrefs.DeleteKey(adminToken);
            EditorPrefs.DeleteKey(selectedGameID);
            EditorPrefs.DeleteKey(selectedGameName);
            EditorPrefs.DeleteKey(selectedOrganisationID);
            EditorPrefs.DeleteKey(firstTimeWelcome);
            EditorPrefs.DeleteKey(newSession);
            // Delete legacy environment key used by older versions so resets fully clear LootLocker editor state
            EditorPrefs.DeleteKey(PlayerSettings.productGUID.ToString() + ".LootLocker.Environment");
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

        /// <summary>
        /// The organisation the signed in admin user is acting as.
        ///
        /// The admin API takes the acting organisation from the x-org-id header. A user can belong
        /// to several organisations, so this cannot be inferred from the token and has to be sent
        /// explicitly by every admin request.
        /// </summary>
        public static void SetSelectedOrganisation(int organisationId)
        {
            EditorPrefs.SetInt(selectedOrganisationID, organisationId);
        }

        public static int GetSelectedOrganisation()
        {
            return EditorPrefs.GetInt(selectedOrganisationID, 0);
        }

        public static bool ShouldAutoShowWindow()
        {
            if (!LootLockerConfig.current.enableEditorAdminExtension)
                return false;
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