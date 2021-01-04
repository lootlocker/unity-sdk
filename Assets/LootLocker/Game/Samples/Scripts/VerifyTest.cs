using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;

namespace LootLockerExample
{
    public class VerifyTest : MonoBehaviour
    {
        public string deviceId;
        string labelText = "";

        private void OnGUI()
        {

            GUIStyle centeredTextStyle = new GUIStyle();
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Back", GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000)))
                UnityEngine.SceneManagement.SceneManager.LoadScene("NavigationScene");

            GUILayout.EndHorizontal();

            deviceId = GUILayout.TextField(deviceId, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Verify", GUILayout.ExpandWidth(true)))
            {
                Verify();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(labelText);

            GUILayout.EndVertical();

        }

        [ContextMenu("Verify")]
        public void Verify()
        {

            LootLockerSDKManager.VerifyID(deviceId, (response) =>
            {
                if (response.success)
                {
                    labelText = "Successful";
                }
                else
                {
                    labelText = "failed: " + response.Error;
                }

            });

        }
    }
}