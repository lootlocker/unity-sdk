using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLockerRequests;
using LootLocker;
using System.Linq;
using System;
using Newtonsoft.Json;

public class CollectablesScreen : MonoBehaviour, IStageOwner
{
    public Transform scrollViewContent;
    public GameObject collectableRecordPrefab;
    public CollectableImage[] imagesForCollectables;
    public List<GameObject> objects = new List<GameObject>();

    public void ViewCollectables()
    {
        objects.Clear();
        LootLockerSDKManager.GettingCollectables((response) =>
        {
            LoadingManager.HideLoadingScreen();
            AppDemoLootLockerRequests.GettingCollectablesResponse mainResponse = JsonConvert.DeserializeObject<AppDemoLootLockerRequests.GettingCollectablesResponse>(response.text);

            if (mainResponse.success)
            {
                Debug.Log("Successfuly got collectables: " + mainResponse.text);

                for (int i = scrollViewContent.childCount - 1; i >= 0; i--)
                {
                    Destroy(scrollViewContent.GetChild(i).gameObject);
                }

                Collectable[] collectables = response.collectables;

                for (int i = 0; i < collectables.Length; i++)
                {
                    GameObject newCollectableObject = Instantiate(collectableRecordPrefab);
                    objects.Add(newCollectableObject);
                    newCollectableObject.GetComponent<ContextControllerCollectables>()?.Init(mainResponse.collectables[i].groups, mainResponse.collectables[i].name, imagesForCollectables);
                }
                StartCoroutine(DelayShowing());
            }
            else
            {
                Debug.Log(response.Error);
            }
        });
    }

    IEnumerator DelayShowing()
    {
        LoadingManager.ShowLoadingScreen();
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < objects.Count; i++)
        {
            objects[i].transform.SetParent(scrollViewContent);
            objects[i].transform.localScale = new Vector3(1, 1, 1);
        }
        LoadingManager.HideLoadingScreen();
    }

    public void UpdateScreenData(IStageData stageData)
    {
        ViewCollectables();
    }
}

[System.Serializable]
public class CollectableImage
{
    public string name;
    public Sprite activeSprite;
    public Sprite inactiveSprite;
}
