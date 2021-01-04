using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;

namespace LootLockerDemoApp
{
    public class StagesManager : MonoBehaviour
    {

        public static StagesManager instance;

        [Serializable]
        public enum StageID { App, Player, Home, Inventory, Store, GameSystem, Settings, Messages, ReadMessages, SwapClass, CreatePlayer, SelectPlayer, Collectables, Files, Storage };

        [Serializable]
        public struct Stage
        {
            public GameObject stageObject;
            public StageID stageID;
        }

        public List<Stage> stages;
        public Stage activeStage;

        private void Awake()
        {
            instance = this;
        }

        public void GoToStage(GameObject newStageObject, IStageData stageData)
        {

            Stage stage = stages.Find(s => s.stageObject == newStageObject);

            activeStage = stage;

            foreach (Stage loopStage in stages)
                loopStage.stageObject?.GetComponent<ScreenCloser>()?.Close();

            //update the screen with data
            stage.stageObject.GetComponent<IStageOwner>()?.UpdateScreenData(stageData);

            stage.stageObject?.GetComponent<ScreenOpener>()?.Open();

        }

        public void GoToStage(GameObject newStageObject)
        {

            Stage stage = stages.Find(s => s.stageObject == newStageObject);

            activeStage = stage;

            foreach (Stage loopStage in stages)
                loopStage.stageObject?.GetComponent<ScreenCloser>()?.Close();
            //update the screen with data
            stage.stageObject.GetComponent<IStageOwner>()?.UpdateScreenData(null);
            stage.stageObject?.GetComponent<ScreenOpener>()?.Open();

        }

        public void GoToStage(StageID newStageID, IStageData stageData)
        {
            Stage stage = stages.Find(s => s.stageID == newStageID);

            activeStage = stage;

            foreach (Stage loopStage in stages)
                loopStage.stageObject?.GetComponent<ScreenCloser>()?.Close();
            //update the screen with data
            stage.stageObject?.GetComponent<IStageOwner>()?.UpdateScreenData(stageData);
            stage.stageObject?.GetComponent<ScreenOpener>()?.Open();

        }

    }
}