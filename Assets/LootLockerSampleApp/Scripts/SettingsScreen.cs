using LootLocker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LootLockerDemoApp
{
    public class ClassData: ILootLockerStageData
    {
        public string classType;
    }
    public class SettingsScreen : MonoBehaviour
    {
        public ScreenCloser bottomNavigator;
        public ButtonExtention buttonExtentionEvent;
        public Button editDefaultCharacter;
        public Button changeCharacter;
        public PlayerDataObject dataObject;
        private void Awake()
        {
            editDefaultCharacter.onClick.AddListener(EditdefultCharacter);
            changeCharacter.onClick.AddListener(ChangeCharacter);
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

        public void EditdefultCharacter()
        {
            dataObject.swappingCharacter = true;
            StagesManager.instance.GoToStage(StagesManager.StageID.CreateCharacter, null);
        }

        public void ChangeCharacter()
        {
            StagesManager.instance.GoToStage(StagesManager.StageID.CreateCharacter, null);
        }
    }
}