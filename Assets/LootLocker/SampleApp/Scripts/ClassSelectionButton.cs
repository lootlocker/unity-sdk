using LootLockerRequests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace LootLockerDemoApp
{
    [Serializable]
    public class SpriteName
    {
        public string name;
        public Sprite sprite;
    }
    public class ClassSelectionButtonEvent : UnityEvent<ClassSelectionButton> { }
    public class ClassSelectionButton : MonoBehaviour
    {
        public static ClassSelectionButtonEvent buttonClicked = new ClassSelectionButtonEvent();
        public SpriteName[] classesIcon;
        public GameObject SelectedButton;
        public GameObject UnselectedButton;
        Loadout loadout;
        Button button;
        public Image[] spriteOwner;
        public Text[] characterName;

        public void Init(Loadout loadout)
        {
            this.loadout = loadout;
            Sprite spr = classesIcon.FirstOrDefault(x => x.name == loadout.character.type)?.sprite;
            for (int i = 0; i < spriteOwner.Length; i++)
            {
                spriteOwner[i].sprite = spr != null ? spr : spriteOwner[i].sprite;
            }
            for (int i = 0; i < characterName.Length; i++)
            {
                characterName[i].text = loadout.character.type;
            }
            if (loadout.character.is_default)
            {
                ClickedButton();
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(ClickedButton);
            buttonClicked.AddListener(ClickedEvent);
        }
        public void ClickedButton()
        {
            buttonClicked?.Invoke(this);
        }
        public void ClickedEvent(ClassSelectionButton buttonExtentionEvent)
        {
            if (buttonExtentionEvent == this)
            {
                SelectCharacter();
                SelectedButton.SetActive(true);
                UnselectedButton.SetActive(false);
            }
            else
            {
                SelectedButton.SetActive(false);
                UnselectedButton.SetActive(true);
            }

        }
        public void SelectCharacter()
        {
            GetComponentInParent<SelectClassScreen>().loadout = this.loadout;
        }


    }
}