using LootLockerRequests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapsTest : MonoBehaviour
{
    public void GettingAllMaps()
    {
        LootLockerSDKManager.GettingAllMaps((response) =>
        {
            if (response.success)
            {
                Debug.LogError("Successful got assets" + response.text);
            }
            else
            {
                Debug.LogError("failed to get assets : " + response.Error);
            }
        });
    }
}
