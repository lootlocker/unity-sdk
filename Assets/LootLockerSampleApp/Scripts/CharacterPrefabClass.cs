using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker;
using LootLocker.Requests;
using LootLockerDemoApp;
using UnityEngine.Events;

namespace LootLockerDemoApp
{
    public class CharacterPrefabClass : MonoBehaviour
    {
        public Text characterName;
        Button button;
        LootLockerCharacter character;
        LootLockerCharacter_Types[] characterClassTypes;
        InputPopupCharacter inputPopupCharacter;
        CreateCharacterScreen createCharacterScreen;
        bool isActive;
        [SerializeField]
        GameObject defaultCharacter;
        int count;
        // Start is called before the first frame update
        void Awake()
        {
            createCharacterScreen = GetComponentInParent<CreateCharacterScreen>();
            button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ClickedButton);
        }

        public void Init(LootLockerCharacter character, LootLockerCharacter_Types[] characterClassTypes, InputPopupCharacter inputPopupCharacter, bool isActive)
        {
            this.character = character;
            defaultCharacter.SetActive(character != null && character.is_default);
            this.characterClassTypes = characterClassTypes;
            this.inputPopupCharacter = inputPopupCharacter;
            characterName.text = this.character != null ? this.character.name : "Empty Character Slot " + (transform.GetSiblingIndex() + 1);
        }

        public void ClickedButton()
        {
            if (this.character != null)
            {
                createCharacterScreen?.UpdateActiveSlot(this);
            }
            else
            {
                createCharacterScreen?.UnselectPrevious();
                EditClass("Create");
            }
        }

        public void EditClass(string mainFunction)
        {
            inputPopupCharacter.Init(this, character, characterClassTypes, new string[] { mainFunction });
        }
    }
}