using LootLockerRequests;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[System.Serializable]
public class TestDataToSave
{
    public string key;
    public string value;
    public int order;
    public TestSubDataToSave testSubDataToSave;
}
[System.Serializable]
public class TestSubDataToSave
{
    public string secondTest;
    public string roomWaterCount;
}

public class PersistentPlayerStorageTest : MonoBehaviour
{
    [Header("Save Data")]
    public List<TestDataToSave> dataToSave;
    [Header("Delete Data")]
    public string keyToDelete;
    public string labelText;
    Vector2 scrollPosition, scrollPosition2;

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

        GUILayout.Label("Key to delete");
        keyToDelete = GUILayout.TextField(keyToDelete, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Data To Create/Update");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("New Data", GUILayout.ExpandWidth(true)))
            dataToSave.Add(new TestDataToSave { key = "", value = "1", order = 1, testSubDataToSave = new TestSubDataToSave { roomWaterCount = "1", secondTest = "test" } });

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2);

        List<TestDataToSave> dataToDelete = new List<TestDataToSave>();

        for (int i = 0; i < dataToSave.Count; i++)
        {

            GUILayout.Label("Data To Create #" + i.ToString());

            GUILayout.BeginHorizontal();

            GUILayout.Label("Key");

            string key = GUILayout.TextField(dataToSave[i].key, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            dataToSave[i].key = key;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Value");

            string rc = GUILayout.TextField(dataToSave[i].value, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            dataToSave[i].value = rc;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Order");

            string o = GUILayout.TextField(dataToSave[i].order.ToString(), GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            o = Regex.Replace(o, @"[^0-9 ]", "");
            dataToSave[i].order = int.Parse(o);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Delete Data #" + i.ToString(), GUILayout.ExpandWidth(true)))
                dataToDelete.Add(dataToSave[i]);

            GUILayout.EndHorizontal();

        }

        for (int i = 0; i < dataToDelete.Count; i++)
            dataToSave.Remove(dataToDelete[i]);

        GUILayout.EndScrollView();

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Get Entire Persistent Storage", GUILayout.ExpandWidth(true)))
        {
            GetEntirePersistentStorage();
        }

        if (GUILayout.Button("Get Single Key Persistent Storage", GUILayout.ExpandWidth(true)))
        {
            GetSingleKeyPersistentStorage();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Update Or Create Key Value", GUILayout.ExpandWidth(true)))
        {
            UpdateOrCreateKeyValue();
        }

        if (GUILayout.Button("Delete Key Value", GUILayout.ExpandWidth(true)))
        {
            DeleteKeyValue();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label(labelText);

        GUILayout.EndScrollView();

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

    }

    [ContextMenu("GetEntirePersistentStorage")]
    public void GetEntirePersistentStorage()
    {
        LootLockerSDKManager.GetEntirePersistentStorage((getPersistentStoragResponse) =>
        {
            if (getPersistentStoragResponse.success)
            {
                labelText = "Success\n" + getPersistentStoragResponse.text;
            }
            else
            {
                labelText = "Failed\n" + getPersistentStoragResponse.text;
            }
        });
    }

    [ContextMenu("GetSingleKeyPersistentStorage")]
    public void GetSingleKeyPersistentStorage()
    {
        LootLockerSDKManager.GetSingleKeyPersistentStorage((getPersistentStoragResponse) =>
        {
            if (getPersistentStoragResponse.success)
            {
                labelText = "Success\n" + getPersistentStoragResponse.text;
            }
            else
            {
                labelText = "Failed\n" + getPersistentStoragResponse.text;
            }

        });
    }

    [ContextMenu("UpdateOrCreateKeyValue")]
    public void UpdateOrCreateKeyValue()
    {
        string json = JsonConvert.SerializeObject(dataToSave);
        GetPersistentStoragRequest data = new GetPersistentStoragRequest();
        for (int i = 0; i < dataToSave.Count; i++)
        {
            data.AddToPayload(new Payload { key = dataToSave[i].key, value = dataToSave[i].value });
        }

        LootLockerSDKManager.UpdateOrCreateKeyValue(data, (getPersistentStoragResponse) =>
        {
            if (getPersistentStoragResponse.success)
            {
                labelText = "Success\n" + getPersistentStoragResponse.text;
            }
            else
            {
                labelText = "Failed\n" + getPersistentStoragResponse.text;
            }
        });
    }

    [ContextMenu("DeleteKeyValue")]
    public void DeleteKeyValue()
    {
        LootLockerSDKManager.DeleteKeyValue(keyToDelete, (getPersistentStoragResponse) =>
        {
            if (getPersistentStoragResponse.success)
            {
                labelText = "Success\n" + getPersistentStoragResponse.text;
            }
            else
            {
                labelText = "Failed\n" + getPersistentStoragResponse.text;
            }

        });
    }
}
