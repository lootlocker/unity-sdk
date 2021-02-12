using LootLocker;
using LootLocker.Requests;
using LootLockerDemoApp;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace LootLockerDemoApp
{
    public class CreateCharacterScreen : MonoBehaviour, ILootLockerStageOwner
    {
        public Transform parent;
        public Button button;
        public Button editButton;
        Action failResponse;
        [Header("Easy Prefab Setup")]
        public bool isEasyPrefab;
        public InputPopupCharacter inputPopupCharacter;
        public CharacterPrefabClass[] slots;
        CharacterPrefabClass activeSlot;
        public PlayerDataObject playerDataObject;
        private void Awake()
        {
            StartEasyPrefab();
        }

        public void UpdateActiveSlot(CharacterPrefabClass activeSlot)
        {
            this.activeSlot = activeSlot;
            if (activeSlot != null)
            {
                editButton.interactable = button.interactable = true;
                editButton?.onClick.RemoveAllListeners();
                editButton?.onClick.AddListener(EditCharacter);
            }
        }

        public void UnselectPrevious()
        {
            editButton.interactable = button.interactable = false;
        }

        public void StartEasyPrefab()
        {
            if (isEasyPrefab)
            {
                SetUpEasyPrefab();
                ListAllCharacterClasses();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    ListAllCharacterClasses();
                });
            }
        }

        public void EditCharacter()
        {
            activeSlot?.EditClass("Edit");
        }

        public void ListAllCharacterClasses()
        {
            //Loadouts are the default items that are associated with each character class. This is why we can use this to list all character classes and then display to user
            LootLockerSDKManager.ListCharacterTypes((res) =>
            {
                if (res.success)
                {
                    SetUpSlots(res.character_types);
                }
                else
                {
                    failResponse?.Invoke();
                }
            });
        }


        public void SetUpSlots(LootLockerCharacter_Types[] lootLockerCharacter_Types)
        {
            LoadingManager.ShowLoadingScreen();
            LootLockerSDKManager.GetCharacterLoadout((response) =>
            {
                if (response.success)
                {

                    if (response.loadouts.Length > 0)
                    {
                        if (response.loadouts.Length < slots.Length)
                        {
                            List<LootLockerCharacter> lootLockerCharacter = new List<LootLockerCharacter>(new LootLockerCharacter[slots.Length - response.loadouts.Length]);
                            for (int i = 0; i < response.loadouts.Length; i++)
                            {
                                lootLockerCharacter.Add(response.loadouts[i].character);
                            }
                            lootLockerCharacter.Reverse();
                            for (int i = 0; i < lootLockerCharacter.Count; i++)
                            {
                                slots[i].Init(lootLockerCharacter[i], lootLockerCharacter_Types, inputPopupCharacter, false);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < slots.Length; i++)
                            {
                                slots[i].Init(response.loadouts[i].character, lootLockerCharacter_Types, inputPopupCharacter, false);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < slots.Length; i++)
                        {
                            slots[i].Init(null, lootLockerCharacter_Types, inputPopupCharacter, false);
                        }
                    }
                }
                string currentClassName = playerDataObject?.lootLockerCharacter?.name;
                if (!string.IsNullOrEmpty(currentClassName) && playerDataObject.swappingCharacter)
                {
                    playerDataObject.swappingCharacter = false;
                    CharacterPrefabClass characterPrefabClass = slots.FirstOrDefault(x => x.characterName.text == currentClassName);
                    if (characterPrefabClass != null)
                        characterPrefabClass?.EditClass("Edit");
                }
                LoadingManager.HideLoadingScreen();
            });
        }

        public void SetUpEasyPrefab()
        {
            if (TexturesSaver.Instance == null)
            {
                GameObject saver = Resources.Load("EasyPrefabsResources/TextureSaver") as GameObject;
                Instantiate(saver);
            }

            if (LoadingManager.Instance == null)
            {
                GameObject loading = Resources.Load("EasyPrefabsResources/LoadingPrefab") as GameObject;
                Instantiate(loading);
            }

            if (PopupSystem.Instance == null)
            {
                GameObject popup = Resources.Load("EasyPrefabsResources/PopupPrefab") as GameObject;
                Instantiate(popup);
            }
        }


        public void UpdateScreenData(ILootLockerStageData stageData)
        {
            ListAllCharacterClasses();
            button.onClick.RemoveAllListeners();
            if (!playerDataObject.swappingCharacter)
            {
                button.onClick.AddListener(() =>
                {
                    OnNextClicked(StagesManager.StageID.Home);
                });
                failResponse = () => { StagesManager.instance.GoToStage(StagesManager.StageID.Home, null); };
            }
            else
            {
                button.onClick.AddListener(() =>
                {
                    OnNextClicked(StagesManager.StageID.Settings);
                });
                failResponse = () => { StagesManager.instance.GoToStage(StagesManager.StageID.Settings, null); };
            }
        }

        public void OnNextClicked(StagesManager.StageID stageID)
        {
            StagesManager.instance.GoToStage(stageID, null);
        }

        public void UpdateDefaultCharacterClass(Action onCompletedUpdate)
        {

        }

    }
}