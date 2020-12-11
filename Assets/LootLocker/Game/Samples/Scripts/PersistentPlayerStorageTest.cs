using LootLockerRequests;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class PersistentPlayerStorageTest : MonoBehaviour
{
    [Header("Save Data")]
    public List<Payload> dataToSave;
    [Header("Delete Data")]
    public string keyToDelete;
    [Header("single key Data")]
    public string keyToGet;
    public string labelText;
    Vector2 scrollPosition, scrollPosition2;
    public string otherPlayerId;
    bool started;

    private void Awake()
    {
        GetEntirePersistentStorage();
    }

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
            dataToSave.Add(new Payload { key = "", value = "1", order = 1 });

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2);

        List<Payload> dataToDelete = new List<Payload>();

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


           bool isPublic = GUILayout.Toggle(dataToSave[i].is_public,"Make Public", GUILayout.MaxWidth(1000));
           dataToSave[i].is_public = isPublic;


            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Delete Data #" + i.ToString(), GUILayout.ExpandWidth(true)))
                DeleteKeyValue(dataToSave[i].key);
            //if (GUILayout.Button("Update #" + i.ToString(), GUILayout.ExpandWidth(true)))
            //    DeleteKeyValue(dataToSave[i].key);

            GUILayout.EndHorizontal();

        }

        //for (int i = 0; i < dataToDelete.Count; i++)
        //    dataToSave.Remove(dataToDelete[i]);

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
                dataToSave.Clear();
                for (int i = 0; i < getPersistentStoragResponse.payload.Length; i++) 
                {
                    dataToSave.Add(getPersistentStoragResponse.payload[i]);
                }
                dataToSave = getPersistentStoragResponse.payload.ToList();
                if (!started)
                {
                    started = true;
                    if (dataToSave.Count > 0)
                    {
                        keyToDelete = dataToSave[0].key;
                    }
                }
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
        LootLockerSDKManager.GetSingleKeyPersistentStorage(keyToGet,(getPersistentStoragResponse) =>
        {
            if (getPersistentStoragResponse.success)
            {
                labelText = "Success\n" + getPersistentStoragResponse.text;
                // dataToSave = getPersistentStoragResponse.payload;
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

        GetPersistentStoragRequest data = new GetPersistentStoragRequest();

        for (int i = 0; i < dataToSave.Count; i++)
        {
            data.AddToPayload(dataToSave[i]);
        }

        LootLockerSDKManager.UpdateOrCreateKeyValue(data, (getPersistentStoragResponse) =>
        {
            if (getPersistentStoragResponse.success)
            {
                labelText = "Success\n" + getPersistentStoragResponse.text;
                dataToSave = getPersistentStoragResponse.payload.ToList();
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
                dataToSave = getPersistentStoragResponse.payload.ToList();
            }
            else
            {
                labelText = "Failed\n" + getPersistentStoragResponse.text;
            }

        });
    }

    public void DeleteKeyValue(string key)
    {
        LootLockerSDKManager.DeleteKeyValue(key, (getPersistentStoragResponse) =>
        {
            if (getPersistentStoragResponse.success)
            {
                labelText = "Success\n" + getPersistentStoragResponse.text;
                dataToSave = getPersistentStoragResponse.payload.ToList();
            }
            else
            {
                labelText = "Failed\n" + getPersistentStoragResponse.text;
            }

        });
    }

    [ContextMenu("GetOtherPlayerKeyValue")]
    public void GetOtherPlayersPublicKeyValuePairs()
    {
        LootLockerSDKManager.GetOtherPlayersPublicKeyValuePairs(otherPlayerId, (getPersistentStoragResponse) =>
        {
            if (getPersistentStoragResponse.success)
            {
                labelText = "Success\n" + getPersistentStoragResponse.text;
                dataToSave = getPersistentStoragResponse.payload.ToList();
            }
            else
            {
                labelText = "Failed\n" + getPersistentStoragResponse.text;
            }
        });
    }
}
