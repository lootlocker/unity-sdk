using LootLocker;
using LootLocker.Requests;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LootLockerDemoApp
{
    public class ContextControllerCollectables : MonoBehaviour
    {
        public string context;
        public Transform inventoryParent;
        public GameObject inventoryPrefab;
        public Text header;
        List<InventoryItemElement> elements = new List<InventoryItemElement>();
        string defaultAssetId;
        public Color inActiveColor;

        public void Init(AppDemoLootLockerRequests.Group[] groups, string name, CollectableImage[] images)
        {
            header.text = name;
            for (int j = 0; j < groups.Length; j++)
            {
                for (int k = 0; k < groups[j].items.Length; k++)
                {
                    GameObject newCollectableObject = Instantiate(inventoryPrefab, inventoryParent);
                    string nameOfCollectableObject = name + "." + groups[j].name + "." + groups[j].items[k].name;
                    CollectableRecord collectableRecord = newCollectableObject.GetComponent<CollectableRecord>();
                    newCollectableObject.GetComponent<CollectableRecord>().Init(nameOfCollectableObject, groups[j].items[k]);
                }
            }
        }

    }
}
