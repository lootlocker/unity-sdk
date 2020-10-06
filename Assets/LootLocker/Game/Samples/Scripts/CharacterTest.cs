using LootLockerRequests;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class CharacterTest : MonoBehaviour
{
    [Header("Character Details")]
    public string characterID = "2852418";
    public string newCharacterName = "Black Mamba";
    public bool isDefault = true;

    [Header("Test Asset Details")]
    public string assetInstanceId = "5711";
    public string assetId = "571";
    public string assetVariationId = "5711";
    public string labelText;
    Vector2 scrollPosition;

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

        GUILayout.Label("Character ID");

        characterID = GUILayout.TextField(characterID, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("New Character Name");

        newCharacterName = GUILayout.TextField(newCharacterName, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Asset Instance ID");

        assetInstanceId = GUILayout.TextField(assetInstanceId, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
        assetInstanceId = Regex.Replace(assetInstanceId, @"[^0-9 ]", "");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Asset ID");

        assetId = GUILayout.TextField(assetId, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
        assetId = Regex.Replace(assetId, @"[^0-9 ]", "");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Asset Variation ID");

        assetVariationId = GUILayout.TextField(assetVariationId, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
        assetVariationId = Regex.Replace(assetVariationId, @"[^0-9 ]", "");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Get Character Loadout", GUILayout.ExpandWidth(true)))
        {
            GetCharacterLoadout();
        }

        if (GUILayout.Button("Get Other Player Character Loadout", GUILayout.ExpandWidth(true)))
        {
            GetOtherPlayersCharacterLoadout();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Update Character", GUILayout.ExpandWidth(true)))
        {
            UpdateCharacter();
        }

        if (GUILayout.Button("Equip ID Asset To Default Character", GUILayout.ExpandWidth(true)))
        {
            EquipIdAssetToDefaultCharacter();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Equip Global Asset To Default Character", GUILayout.ExpandWidth(true)))
        {
            EquipGlobalAssetToDefaultCharacter();
        }

        if (GUILayout.Button("Equip ID Asset To Character", GUILayout.ExpandWidth(true)))
        {
            EquipIdAssetToCharacter();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Equip Global Asset To Character", GUILayout.ExpandWidth(true)))
        {
            EquipGlobalAssetToCharacter();
        }

        if (GUILayout.Button("UnEquip ID Asset To Default Character", GUILayout.ExpandWidth(true)))
        {
            UnEquipIdAssetToDefaultCharacter();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("UnEquip ID Asset To Character", GUILayout.ExpandWidth(true)))
        {
            UnEquipIdAssetToCharacter();
        }

        if (GUILayout.Button("Get Current LoadOut To Default Character", GUILayout.ExpandWidth(true)))
        {
            GetCurrentLoadOutToDefaultCharacter();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label(labelText);

        GUILayout.EndScrollView();

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

    }

    [ContextMenu("GetCharacterLoadout")]
    public void GetCharacterLoadout()
    {
        LootLockerSDKManager.GetCharacterLoadout((response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }

        });
    }
    [ContextMenu("GetOtherPlayersCharacterLoadout")]
    public void GetOtherPlayersCharacterLoadout()
    {
        LootLockerSDKManager.GetOtherPlayersCharacterLoadout(characterID, (response) =>
         {
             if (response.success)
             {
                 labelText = "Success\n" + response.text;
             }
             else
             {
                 labelText = "Failed\n" + response.text;
             }

         });
    }
    [ContextMenu("UpdateCharacter")]
    public void UpdateCharacter()
    {
        LootLockerSDKManager.UpdateCharacter(characterID, newCharacterName, isDefault, (response) =>
         {
             if (response.success)
             {
                 labelText = "Success\n" + response.text;
             }
             else
             {
                 labelText = "Failed\n" + response.text;
             }

         });
    }
    [ContextMenu("EquipIdAssetToDefaultCharacter")]
    public void EquipIdAssetToDefaultCharacter()
    {
        LootLockerSDKManager.EquipIdAssetToDefaultCharacter(assetInstanceId, (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }

        });
    }

    [ContextMenu("EquipGlobalAssetToDefaultCharacter")]
    public void EquipGlobalAssetToDefaultCharacter()
    {
        LootLockerSDKManager.EquipGlobalAssetToDefaultCharacter(assetId, assetVariationId, (response) =>
         {
             if (response.success)
             {
                 labelText = "Success\n" + response.text;
             }
             else
             {
                 labelText = "Failed\n" + response.text;
             }

         });
    }
    [ContextMenu("EquipIdAssetToCharacter")]
    public void EquipIdAssetToCharacter()
    {
        LootLockerSDKManager.EquipIdAssetToCharacter(characterID, assetInstanceId, (response) =>
         {
             if (response.success)
             {
                 labelText = "Success\n" + response.text;
             }
             else
             {
                 labelText = "Failed\n" + response.text;
             }

         });
    }
    [ContextMenu("EquipGlobalAssetToCharacter")]
    public void EquipGlobalAssetToCharacter()
    {
        LootLockerSDKManager.EquipGlobalAssetToCharacter(assetId, assetVariationId, characterID, (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }

        });
    }
    [ContextMenu("UnEquipIdAssetToDefaultCharacter")]
    public void UnEquipIdAssetToDefaultCharacter()
    {
        LootLockerSDKManager.UnEquipIdAssetToDefaultCharacter(assetId, (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }

        });
    }
    [ContextMenu("UnEquipIdAssetToCharacter")]
    public void UnEquipIdAssetToCharacter()
    {
        LootLockerSDKManager.UnEquipIdAssetToDefaultCharacter(assetId, (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }

        });
    }
    [ContextMenu("GetCurrentLoadOutToDefaultCharacter")]
    public void GetCurrentLoadOutToDefaultCharacter()
    {
        LootLockerSDKManager.GetCurrentLoadOutToDefaultCharacter((response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }

        });
    }
    [ContextMenu("GetCurrentLoadOutToOtherCharacter")]
    public void GetCurrentLoadOutToOtherCharacter()
    {
        LootLockerSDKManager.GetCurrentLoadOutToOtherCharacter(characterID, (response) =>
        {
            if (response.success)
            {
                labelText = "Success\n" + response.text;
            }
            else
            {
                labelText = "Failed\n" + response.text;
            }

        });
    }
}
