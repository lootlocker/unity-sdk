using LootLockerRequests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LootLockerExample
{
    public class AssetInstanceTest : MonoBehaviour
    {

        public string labelText;
        public string instanceId = "0";
        public string assetId = "0";
        public string key;
        public string value;
        public string key1;
        public string value1;
        public string key2;
        public string value2;

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

            GUILayout.Label("Instance ID");

            instanceId = GUILayout.TextField(instanceId, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            instanceId = Regex.Replace(instanceId, @"[^0-9 ]", "");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Asset ID");

            assetId = GUILayout.TextField(assetId, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            assetId = Regex.Replace(assetId, @"[^0-9 ]", "");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Key");

            key = GUILayout.TextField(key, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Value");

            value = GUILayout.TextField(value, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Key1");

            key1 = GUILayout.TextField(key1, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Value1");

            value1 = GUILayout.TextField(value1, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Key2");

            key2 = GUILayout.TextField(key2, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Value2");

            value2 = GUILayout.TextField(value2, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Get All Key Value Pairs", GUILayout.ExpandWidth(true)))
            {
                GetAllKeyValuePairs();
            }

            if (GUILayout.Button("Get All Key Value Pairs To An Instance", GUILayout.ExpandWidth(true)))
            {
                GetAllKeyValuePairsToAnInstance();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Get A Key Value Pair By ID", GUILayout.ExpandWidth(true)))
            {
                GetAKeyValuePairById();
            }

            if (GUILayout.Button("Create Key Value Pair", GUILayout.ExpandWidth(true)))
            {
                CreateKeyValuePair();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Update One Or More Key Value Pair", GUILayout.ExpandWidth(true)))
            {
                UpdateOneOrMoreKeyValuePair();
            }

            if (GUILayout.Button("Update Key Value Pair By ID", GUILayout.ExpandWidth(true)))
            {
                UpdateKeyValuePairById();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Delete Key Value Pair", GUILayout.ExpandWidth(true)))
            {
                DeleteKeyValuePair();
            }

            if (GUILayout.Button("Inspect A Loot Box", GUILayout.ExpandWidth(true)))
            {
                InspectALootBox();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Open A Loot Box", GUILayout.ExpandWidth(true)))
            {
                OpenALootBox();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(labelText);

            GUILayout.EndVertical();

        }

        [ContextMenu("Get All Key Value Pairs")]
        public void GetAllKeyValuePairs()
        {
            LootLockerSDKManager.GetAllKeyValuePairsForAssetInstances((response) =>
            {
                Debug.Log("Response: " + response.text);

                if (response.success)
                {
                    labelText = "Success: " + response.text;
                }
                else
                {
                    labelText = response.text;
                }
            });
        }

        [ContextMenu("GetAllKeyValuePairsToAnInstance")]
        public void GetAllKeyValuePairsToAnInstance()
        {
            LootLockerSDKManager.GetAllKeyValuePairsToAnInstance(int.Parse(instanceId), (response) =>
             {
                 if (response.success)
                 {
                     labelText = "Success: " + response.text;
                 }
                 else
                 {
                     labelText = "Failed: " + response.text;
                 }
             });
        }

        [ContextMenu("GetAKeyValuePairById")]
        public void GetAKeyValuePairById()
        {
            LootLockerSDKManager.GetAKeyValuePairByIdForAssetInstances(int.Parse(assetId), int.Parse(instanceId), (response) =>
             {
                 if (response.success)
                 {
                     labelText = "Success: " + response.text;
                 }
                 else
                 {
                     labelText = "Failed: " + response.text;
                 }
             });
        }

        [ContextMenu("CreateKeyValuePairById")]
        public void CreateKeyValuePair()
        {
            LootLockerSDKManager.CreateKeyValuePairForAssetInstances(int.Parse(assetId), key, value, (response) =>
              {
                  if (response.success)
                  {
                      labelText = "Success: " + response.text;
                  }
                  else
                  {
                      labelText = "Failed: " + response.text;
                  }
              });
        }

        [ContextMenu("UpdateOneOrMoreKeyValuePair")]
        public void UpdateOneOrMoreKeyValuePair()
        {
            Dictionary<string, string> multipleTestKeys = new Dictionary<string, string>();

            multipleTestKeys.Add(key1, value1);
            multipleTestKeys.Add(key2, value2);

            LootLockerSDKManager.UpdateOneOrMoreKeyValuePairForAssetInstances(int.Parse(assetId), multipleTestKeys, (response) =>
            {
                if (response.success)
                {
                    labelText = "Success: " + response.text;
                }
                else
                {
                    labelText = "Failed: " + response.text;
                }
            });
        }

        [ContextMenu("UpdateKeyValuePairById")]
        public void UpdateKeyValuePairById()
        {
            LootLockerSDKManager.UpdateKeyValuePairByIdForAssetInstances(int.Parse(assetId), key, value, (response) =>
            {
                if (response.success)
                {
                    labelText = "Success: " + response.text;
                }
                else
                {
                    labelText = "Failed: " + response.text;
                }
            });
        }

        [ContextMenu("DeleteKeyValuePair")]
        public void DeleteKeyValuePair()
        {
            LootLockerSDKManager.DeleteKeyValuePairForAssetInstances(int.Parse(assetId), int.Parse(instanceId), (response) =>
             {
                 if (response.success)
                 {
                     labelText = "Success: " + response.text;
                 }
                 else
                 {
                     labelText = "Failed: " + response.text;
                 }
             });
        }

        [ContextMenu("InspectALootBox")]
        public void InspectALootBox()
        {
            LootLockerSDKManager.InspectALootBoxForAssetInstances(int.Parse(assetId), (response) =>
            {
                if (response.success)
                {
                    labelText = "Success: " + response.text;
                }
                else
                {
                    labelText = "Failed: " + response.text;
                }
            });
        }

        [ContextMenu("OpenALootBox")]
        public void OpenALootBox()
        {
            LootLockerSDKManager.OpenALootBoxForAssetInstances(int.Parse(assetId), (response) =>
            {
                if (response.success)
                {
                    labelText = "Success: " + response.text;
                }
                else
                {
                    labelText = "Failed: " + response.text;
                }
            });
        }
    }
}