using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLockerRequests;

namespace LootLockerExample
{
    public class SessionTest : MonoBehaviour
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

            if (GUILayout.Button("Check Session", GUILayout.ExpandWidth(true)))
            {
                Session();
            }

            if (GUILayout.Button("End Session", GUILayout.ExpandWidth(true)))
            {
                EndSession();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(labelText);

            GUILayout.EndVertical();

        }

        [ContextMenu("Check Session")]
        public void Session()
        {

            LootLockerSDKManager.StartSession(deviceId, (response) =>
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

        [ContextMenu("End Session")]
        public void EndSession()
        {

            LootLockerSDKManager.EndSession(deviceId, (response) =>
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