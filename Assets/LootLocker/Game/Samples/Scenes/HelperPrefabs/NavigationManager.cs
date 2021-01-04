using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LootLockerExample
{
    public class NavigationManager : MonoBehaviour
    {

        [Serializable]
        public struct Scene
        {

            public string buttonName, sceneName;

        };

        public List<Scene> scenes;

        private void OnGUI()
        {

            GUIStyle centeredTextStyle = new GUIStyle();
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;
            centeredTextStyle.normal.textColor = Color.white;
            centeredTextStyle.fontStyle = FontStyle.Bold;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Choose a test scene to load", centeredTextStyle);

            GUILayout.EndHorizontal();

            foreach (Scene scene in scenes)
            {

                GUILayout.BeginHorizontal();

                if (GUILayout.Button(scene.buttonName, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000)))
                    SceneManager.LoadScene(scene.sceneName);

                GUILayout.EndHorizontal();

            }

            GUILayout.EndVertical();

        }
    }
}