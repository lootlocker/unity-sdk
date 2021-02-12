using LootLocker.Requests;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker;

namespace LootLockerDemoApp
{
    public class InputPopupCharacter : MonoBehaviour
    {
        public InputField characterName;
        public Toggle defaultCharacter;
        public ClassSelectDropDown selectClass;
        public Text[] buttonsTxt;
        public string keySucces = "keySucces";
        List<Action> actions = new List<Action>();
        public Button closeBtn;
        LootLockerCharacter character;
        LootLockerCharacter_Types currentClassTypeSelected;
        CharacterPrefabClass classPrefab;
        LootLockerCharacter_Types[] characterClassTypes;
        public PlayerDataObject dataObject;

        void Awake()
        {
            closeBtn.onClick.AddListener(Close);
        }

        public void Init(CharacterPrefabClass classPrefab, LootLockerCharacter character, LootLockerCharacter_Types[] characterClassTypes, string[] btnText)
        {
            this.classPrefab = classPrefab;
            this.character = character;
            this.characterClassTypes = characterClassTypes;
            for (int i = 0; i < buttonsTxt.Length; i++)
            {
                buttonsTxt[i].transform.parent.gameObject.SetActive(false);
            }

            this.characterName.text = character != null ? character.name : "";
            this.defaultCharacter.isOn = character != null ? character.is_default : false;
            selectClass.Init(character != null ? character.type : "", characterClassTypes, this);

            for (int i = 0; i < btnText.Length; i++)
            {
                buttonsTxt[i].text = btnText[i];
                buttonsTxt[i].transform.parent.gameObject.SetActive(true);
            }

            GetComponent<ScreenOpener>()?.Open();
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(characterName.text))
            {
                PopupSystem.ShowPopup("Enter valid text for characterName", null, "Close", () =>
                {

                }, url: keySucces, isError: true);

                return;
            }
            LoadingManager.ShowLoadingScreen();

            if (character != null)
            {
                LootLockerSDKManager.UpdateCharacter(character.id.ToString(), this.characterName.text, defaultCharacter.isOn, (response) =>
                {
                    if (response.success)
                    {
                        CharacterUpdateCompleted("Updated Character", response);
                    }
                    else
                    {
                        CharacterUpdateFailed();
                    }
                });
            }
            else
            {
                LootLockerSDKManager.CreateCharacter(currentClassTypeSelected.id.ToString(), this.characterName.text, defaultCharacter.isOn, (response) =>
                {
                    if (response.success)
                    {
                        CharacterUpdateCompleted("Created Character", response);
                    }
                    else
                    {
                        CharacterUpdateFailed();
                    }
                });
            }
        }

        public void CharacterUpdateCompleted(string message, LootLockerCharacterLoadoutResponse lootLockerCharacter)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("Character", this.characterName.text);

            PopupSystem.ShowPopup(message, data, "Close", () =>
            {
                if (!string.IsNullOrEmpty(this.characterName.text))
                {
                    LootLockerCharacter character = lootLockerCharacter.GetCharacter(this.characterName.text);
                    character.is_default = defaultCharacter.isOn;
                    if (defaultCharacter.isOn)
                    {
                        dataObject?.SaveCharacter(character);
                    }

                    this.classPrefab.Init(character, characterClassTypes, this, true);
                    StagesManager.instance.GoToStage(StagesManager.StageID.CreateCharacter, PlayerManager.localPlayer);
                    GetComponent<ScreenCloser>()?.Close();
                    PopupSystem.CloseNow();
                }
            }, url: keySucces);

            LoadingManager.HideLoadingScreen();
        }

        public void CharacterUpdateFailed()
        {
            LoadingManager.HideLoadingScreen();
            PopupSystem.ShowPopup("Character Update Failed", null, "Close", () =>
            {
                PopupSystem.CloseNow();
            }, url: keySucces);
        }

        public void Close()
        {
            GetComponent<ScreenCloser>()?.Close();
        }

        public void Delete()
        {
            if (string.IsNullOrEmpty(characterName.text) || string.IsNullOrEmpty(characterName.text))
            {
                PopupSystem.ShowPopup("Please enter valid text for characterName and value", null, "Close", () =>
                {

                }, url: keySucces, isError: true);

                return;
            }

        }
        public void UpdateClassId(LootLockerCharacter_Types currentClassTypeSelected)
        {
            this.currentClassTypeSelected = currentClassTypeSelected;
        }
    }
}