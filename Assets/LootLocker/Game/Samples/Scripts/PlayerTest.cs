using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLockerRequests;
using Newtonsoft.Json;

public class PlayerTest : MonoBehaviour
{

    public static string labelText;

    private void OnGUI()
    {

        GUIStyle centeredTextStyle = new GUIStyle();
        centeredTextStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Back", GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000)))
            UnityEngine.SceneManagement.SceneManager.LoadScene("NavigationScene");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Get Inventory", GUILayout.ExpandWidth(true)))
        {
            GetInventory();
        }

        if (GUILayout.Button("Get Balance", GUILayout.ExpandWidth(true)))
        {
            GetBalance();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Submit XP", GUILayout.ExpandWidth(true)))
        {
            SubmitXp();
        }

        if (GUILayout.Button("Get XP And Level", GUILayout.ExpandWidth(true)))
        {
            GetXpAndLevel();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Get Asset Notification", GUILayout.ExpandWidth(true)))
        {
            GetAssetNotification();
        }

        if (GUILayout.Button("Get Deactivated Asset Notification", GUILayout.ExpandWidth(true)))
        {
            GetDeactivatedAssetNotification();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Initiate DLC Migration", GUILayout.ExpandWidth(true)))
        {
            InitiateDLCMigration();
        }

        if (GUILayout.Button("Get DLC Migrated", GUILayout.ExpandWidth(true)))
        {
            GetDLCMigrated();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Set Profile Private", GUILayout.ExpandWidth(true)))
        {
            SetProfilePrivate();
        }

        if (GUILayout.Button("Set Profile Public", GUILayout.ExpandWidth(true)))
        {
            SetProfilePublic();
        }

        GUILayout.EndHorizontal();

        GUILayout.Label(labelText);

        GUILayout.EndVertical();

    }

    [ContextMenu("Get PLayer Info")]
    public void GetPlayerInfo()
    {
        LootLockerSDKManager.GetPlayerInfo((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    [ContextMenu("Get Inventory")]
    public void GetInventory()
    {
        LootLockerSDKManager.GetInventory((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    [ContextMenu("Get Balance")]
    public void GetBalance()
    {
        LootLockerSDKManager.GetBalance((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    [ContextMenu("Submit XP")]
    public void SubmitXp()
    {


        LootLockerSDKManager.SubmitXp(Random.Range(1, 5), (response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    [ContextMenu("Get Xp and Level")]
    public void GetXpAndLevel()
    {
        LootLockerSDKManager.GetXpAndLevel((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    [ContextMenu("GetAssetNotification")]
    public void GetAssetNotification()
    {
        LootLockerSDKManager.GetAssetNotification((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    public void GetDeactivatedAssetNotification()
    {
        LootLockerSDKManager.GetDeactivatedAssetNotification((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    public void InitiateDLCMigration()
    {
        LootLockerSDKManager.InitiateDLCMigration((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    public static void GetDLCMigrated()
    {
        LootLockerSDKManager.GetDLCMigrated((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    public static void SetProfilePrivate()
    {
        LootLockerSDKManager.SetProfilePrivate((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

    public static void SetProfilePublic()
    {
        LootLockerSDKManager.SetProfilePublic((response) =>
        {
            if (response.success)
            {
                labelText = "Successful " + response.text;
            }
            else
            {
                labelText = "Failed: " + response.Error;
            }
        });
    }

}
