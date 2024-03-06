
#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER
using UnityEditor;
namespace LootLocker.Extension {


public class LootLockerEditorData
{
    private static string prefix = PlayerSettings.productGUID.ToString() + ".LootLocker.";

    private static string adminToken = "AdminToken";
    private static string selectedGameID = "SelectedGameID";
    private static string environment = "Environment";
    private static string firstTimeWelcome = "FirstTimeWelcome";
    private static string newSession = "NewSession";

    public static void ClearLootLockerPrefs()
    {
        EditorPrefs.DeleteKey(prefix + adminToken);
        EditorPrefs.DeleteKey(prefix + selectedGameID);
        EditorPrefs.DeleteKey(prefix + environment);
        EditorPrefs.DeleteKey(prefix + firstTimeWelcome);
        EditorPrefs.DeleteKey(prefix + newSession);
    }

    public static void SetAdminToken(string _adminToken)
    {
        EditorPrefs.SetString(prefix + adminToken, _adminToken);
        EditorPrefs.SetBool(prefix + firstTimeWelcome, false);
        EditorPrefs.SetBool(prefix + newSession, true);
    }

    public static string GetAdminToken()
    {
        return EditorPrefs.GetString(prefix + adminToken);
    }

    public static void SetSelectedGame(string _selectedGame)
    {
        EditorPrefs.SetInt(prefix + selectedGameID, int.Parse(_selectedGame));
    }

    public static int GetSelectedGame()
    {
       return EditorPrefs.GetInt(prefix + selectedGameID);
    }

    public static void SetEnvironmentStage()
    {
        EditorPrefs.SetString(prefix + environment, "Stage");
    }

    public static void SetEnvironmentLive()
    {
        EditorPrefs.SetString(prefix + environment, "Live");
    }

    public static bool IsEnvironmentStage()
    {
        return EditorPrefs.GetString(prefix + environment).Equals("Stage");
    }

    public static bool ShouldAutoShowWindow()
    {
        return EditorPrefs.GetBool(prefix + firstTimeWelcome);
    }

    public static bool IsNewSession()
    {
        bool result = EditorPrefs.GetBool(prefix + newSession, false);
        EditorPrefs.SetBool(prefix + newSession, false);
        return result;
    }
}
}
#endif