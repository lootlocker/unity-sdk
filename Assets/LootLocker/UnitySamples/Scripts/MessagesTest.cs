using LootLocker.Requests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker.Example
{
    public class MessagesTest : MonoBehaviour
    {

        public string labelText;
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

            if (GUILayout.Button("Get Messages", GUILayout.ExpandWidth(true)))
                GetMessages();

            GUILayout.EndHorizontal();

            GUILayout.Label(labelText);

            GUILayout.EndVertical();

        }
        /// <summary>
        /// 
        /// </summary>
        [ContextMenu("Get Messages")]
        public void GetMessages()
        {
            LootLockerSDKManager.GetMessages((response) =>
            {
                if (response.success)
                {
                    labelText = "Successfully got messages:\n" + response.text;
                }
                else
                {
                    labelText = "failed to get messages :\n" + response.Error;
                }
            });
        }

    }

}