using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LootLockerSceneLoader : MonoBehaviour
{
    public static LootLockerSceneLoader instance;

    public GameObject goBackCanvas;
    private void Awake()
    {
        if (instance != null)
        {
            DestroyImmediate(this.gameObject);
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(this);
    }
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        goBackCanvas.SetActive(true);
    }

    public void BackToStartScene()
    {
        goBackCanvas.SetActive(false);
        SceneManager.LoadScene("Examples");
    }
}
