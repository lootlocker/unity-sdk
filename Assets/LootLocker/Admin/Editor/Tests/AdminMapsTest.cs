using LootLocker.Admin.Requests;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LootLocker.Admin
{
    public class AdminMapsTest : MonoBehaviour
    {

        [Header("Getting All Maps To A Game")]
        public int gameIDToGetMaps;


        [Header("Creating Maps")]
        [Header("---------------------------")]

        [SerializeField]
        public LootLockerCreatingMapsRequest map;

        public bool includeAssetID, includeSpawnPoints;

        [Header("Updating Maps")]
        [Header("---------------------------")]
        public int mapID;
        public LootLockerCreatingMapsRequest updatedMap;

        [ContextMenu("GettingAllMapsToAGame")]
        public void GettingAllMapsToAGAme()
        {
            LootLockerSDKAdminManager.GettingAllMapsToAGame(gameIDToGetMaps, (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful got all maps: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to get all maps: " + response.Error);
                }
            });
        }

        [ContextMenu("CreatingMaps")]
        public void CreatingMaps()
        {

            LootLockerSDKAdminManager.CreatingMaps(map, includeAssetID, includeSpawnPoints, (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful created map: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to create map: " + response.Error);
                }
            });
        }

        [ContextMenu("UpdatingMaps")]
        public void UpdatingMaps()
        {

            LootLockerSDKAdminManager.UpdatingMaps(updatedMap, mapID, (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful updated map: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to update map: " + response.Error);
                }
            });
        }

    }

}