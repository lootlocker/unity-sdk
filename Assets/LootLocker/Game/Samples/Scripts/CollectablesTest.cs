using LootLockerRequests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectablesTest : MonoBehaviour
{

    public string labelText;
    public string itemToCollect;
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

        GUILayout.Label("Item To Collect");

        itemToCollect = GUILayout.TextField(itemToCollect, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Get Collectables", GUILayout.ExpandWidth(true)))
        {
            GettingCollectables();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Collect An Item", GUILayout.ExpandWidth(true)))
        {
            CollectAnItem();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label(labelText);

        GUILayout.EndScrollView();

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

    }

    [ContextMenu("Get Collectables")]
    public void GettingCollectables()
    {
        LootLockerSDKManager.GettingCollectables((response) =>
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

    [ContextMenu("Collect An Item")]
    public void CollectAnItem()
    {
        LootLockerSDKManager.CollectingAnItem(itemToCollect, (response) =>
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

