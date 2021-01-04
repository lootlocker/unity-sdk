using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LootLockerDemoApp
{
    public class SettingsScreen : MonoBehaviour
    {
        public ScreenCloser bottomNavigator;
        public ButtonExtention buttonExtentionEvent;
        public Button changeClass;

        private void Awake()
        {
            changeClass.onClick.AddListener(ViewClassSelectScreen);
        }

        public void RefreshGameData()
        {
            StagesManager.instance.GoToStage(StagesManager.StageID.Home, null);
            ButtonExtention.buttonClicked?.Invoke(buttonExtentionEvent);
        }

        public void ChangePlayer()
        {
            StagesManager.instance.GoToStage(StagesManager.StageID.Player, null);
            ButtonExtention.buttonClicked?.Invoke(buttonExtentionEvent);
            bottomNavigator?.Close();

        }

        public void Logout()
        {
            StagesManager.instance.GoToStage(StagesManager.StageID.App, null);
            ButtonExtention.buttonClicked?.Invoke(buttonExtentionEvent);
            bottomNavigator?.Close();
        }

        public void ViewClassSelectScreen()
        {
            StagesManager.instance.GoToStage(StagesManager.StageID.SwapClass, null);
        }
    }
}