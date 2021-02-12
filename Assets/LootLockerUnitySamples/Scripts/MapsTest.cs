using LootLocker.Requests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker.Example
{
    public class MapsTest : MonoBehaviour
    {
        public void GettingAllMaps()
        {
            LootLockerSDKManager.GettingAllMaps((response) =>
            {
                if (response.success)
                {
                    LootLockerSDKManager.DebugMessage("Successful got assets" + response.text);
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("failed to get assets : " + response.Error, true);
                }
            });
        }
    }
}