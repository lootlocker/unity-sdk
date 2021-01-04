using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLockerDemoApp
{
    public class LoadingManager : MonoBehaviour
    {
        public static LoadingManager Instance;
        public Transform spinner;
        public float spinnerRotateSpeed = 300;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Debug.LogError("Found Another" + gameObject.name);
                Destroy(this.transform.parent.gameObject);
            }
        }

        public static void ShowLoadingScreen()
        {
            Instance.GetComponent<ScreenOpener>()?.Open();
        }

        public static void HideLoadingScreen()
        {
            Instance.GetComponent<ScreenCloser>()?.Close();
        }

        private void Update()
        {
            spinner.Rotate(0, 0, -spinnerRotateSpeed * Time.deltaTime);
        }
    }
}
