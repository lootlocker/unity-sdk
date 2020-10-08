using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottomNavigator : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<ScreenCloser>()?.Close();
    }

    public void ViewInventory()
    {
        LoadingManager.ShowLoadingScreen();
        StagesManager.instance.GoToStage(StagesManager.StageID.Inventory, null);
    }

    public void ViewStore()
    {
        LoadingManager.ShowLoadingScreen();
        StagesManager.instance.GoToStage(StagesManager.StageID.Store, null);
    }

    public void ViewCollectables()
    {
        LoadingManager.ShowLoadingScreen();
        StagesManager.instance.GoToStage(StagesManager.StageID.Collectables, null);
    }

    public void ViewHome()
    {
        StagesManager.instance.GoToStage(StagesManager.StageID.Home, null);
    }

    public void ViewGameSystem()
    {
          StagesManager.instance.GoToStage(StagesManager.StageID.GameSystem, null);
    }

    public void ViewSettings()
    {
          StagesManager.instance.GoToStage(StagesManager.StageID.Settings, null);
    }

    public void ViewMessages()
    {
        StagesManager.instance.GoToStage(StagesManager.StageID.Messages, null);
    }

    public void ViewStorage()
    {
        StagesManager.instance.GoToStage(StagesManager.StageID.Storage, null);
    }
}
