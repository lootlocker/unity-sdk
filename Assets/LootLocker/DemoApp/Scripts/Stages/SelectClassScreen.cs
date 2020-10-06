using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using LootLockerRequests;
using Newtonsoft.Json;
using System;
using LootLocker;


public class SelectClassScreen : MonoBehaviour, IStageOwner
{
    public Transform parent;
    public GameObject characterClassPrefab;
    CreatePlayerRequest createPlayerRequest;
    public Loadout loadout;
    SessionResponse sessionResponse;
    Guid guid;
    public Button button;
    Action failResponse;

    public void UpdateScreenData(IStageData stageData)
    {
        if (stageData != null)
        {
            createPlayerRequest = stageData as CreatePlayerRequest;
            LootLockerConfig.current.playerName = createPlayerRequest.playerName;
            LoadingManager.ShowLoadingScreen();
            failResponse = () => { StagesManager.instance.GoToStage(StagesManager.StageID.Player, null); };
            //Starting session first before character is chosen
            StartSession(() =>
            {

                foreach (Transform tr in parent)
                    Destroy(tr.gameObject);

                LootLockerSDKManager.GetCharacterLoadout((response) =>
                {
                    if (response.success)
                    {
                        foreach (Loadout loadout in response.loadouts)
                        {
                            GameObject selectionButton = Instantiate(characterClassPrefab, parent);
                            selectionButton.GetComponent<ClassSelectionButton>()?.Init(loadout);
                        }
                    }
                    else
                    {
                        StagesManager.instance.GoToStage(StagesManager.StageID.CreatePlayer, null); 
                    }
                    LoadingManager.HideLoadingScreen();
                });
            });
            //if we are creating a new character then we want to set character details once it is created
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                UpdateDefaultCharacterClass(() =>
                {
                    LocalPlayer localPlayer = new LocalPlayer { playerName = createPlayerRequest.playerName, uniqueID = guid.ToString(), characterClass = loadout?.character };
                    List<LocalPlayer> localPlayers = JsonConvert.DeserializeObject<List<LocalPlayer>>(PlayerPrefs.GetString("localplayers"));
                    localPlayers.Add(localPlayer);
                    PlayerPrefs.SetString("localplayers", JsonConvert.SerializeObject(localPlayers));
                    LootLockerConfig.current.deviceID = localPlayer.uniqueID;
                    LootLockerConfig.current.playerClass = loadout.character.type.ToString();
                    //Character has been set, we can now load the home page
                    StagesManager.instance.GoToStage(StagesManager.StageID.Home, sessionResponse);
                });
            });
        }
        else
        {
            failResponse = () => { StagesManager.instance.GoToStage(StagesManager.StageID.Settings, null); };

            foreach (Transform tr in parent)
                Destroy(tr.gameObject);

            LootLockerSDKManager.GetCharacterLoadout((response) =>
            {
                if (response.success)
                {
                    foreach (Loadout loadout in response.loadouts)
                    {
                        GameObject selectionButton = Instantiate(characterClassPrefab, parent);
                        selectionButton.GetComponent<ClassSelectionButton>()?.Init(loadout);
                    }
                }
                else
                {
                    StagesManager.instance.GoToStage(StagesManager.StageID.Settings, null);
                }
                LoadingManager.HideLoadingScreen();
            });
            //if we are just updating the character class for player, then after it is completed. We want to return to the inventory screen
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                UpdateDefaultCharacterClass(() =>
                {
                    //Character has been set, we can now load inventory
                    StagesManager.instance.GoToStage(StagesManager.StageID.Settings, null);
                });
            });
        }
    }

    public void UpdateDefaultCharacterClass(Action onCompletedUpdate)
    {
        ////now that we have a new player created, we need to set the default character of this player to the one that was selected

        LootLockerSDKManager.UpdateCharacter(loadout.character.id.ToString(), LootLockerConfig.current.playerName, true, (updateResponse) =>
        {
            if (updateResponse.success)
            {
                LootLockerConfig.current.playerClass = loadout.character.type;
                Debug.Log("Updated character info successfully: " + updateResponse.text);
                onCompletedUpdate?.Invoke();
                LoadingManager.HideLoadingScreen();
            }
            else
            {
                failResponse?.Invoke();
                Debug.LogError("Failed to update character info: " + updateResponse.text);
                LoadingManager.HideLoadingScreen();
            }

        });
    }

    public void StartSession(Action OnCompletedSessionStart)
    {
        guid = Guid.NewGuid();

        LoadingManager.ShowLoadingScreen();
        //Starting a new session using the new id that has been created
        LootLockerSDKManager.StartSession(guid.ToString(), (response) =>
        {
            if (response.success)
            {
                sessionResponse = response;
                Debug.Log("Session success: " + response.text);
                OnCompletedSessionStart?.Invoke();
            }
            else
            {
                failResponse?.Invoke();
                Debug.LogError("Session failure: " + response.text);
                LoadingManager.HideLoadingScreen();
            }

        });
    }

}
