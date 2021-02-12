using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using LootLocker.Requests;

namespace LootLockerDemoApp
{
    public class ClassSelectDropDown : MonoBehaviour
    {
        Dropdown dropdown;
        InputPopupCharacter inputPopupCharacter;
        LootLockerCharacter_Types[] options;
        private void Awake()
        {
            dropdown = GetComponent<Dropdown>();
            dropdown.onValueChanged.AddListener(Clicked);
        }
        // Update is called once per frame
        public void Init(string current, LootLockerCharacter_Types[] options, InputPopupCharacter inputPopupCharacter)
        {
            this.inputPopupCharacter = inputPopupCharacter;
            this.options = options;
            Dropdown.OptionDataList optionData = new Dropdown.OptionDataList();
            optionData.options.Add(new Dropdown.OptionData("Choose Class"));

            for (int i = 0; i < options.Length; i++)
            {
                optionData.options.Add(new Dropdown.OptionData(options[i].name));
            }
            dropdown.ClearOptions();
            dropdown.AddOptions(optionData.options);
            LootLockerCharacter_Types op = options.FirstOrDefault(x => x.name == current);
            dropdown.interactable = !(op != null);
            int index = op != null ? Array.IndexOf(options, op) : -1;
            dropdown.value = index + 1;
        }

        public void Clicked(int index)
        {
            if (index > 0)
            {
                LootLockerCharacter_Types option = options[index - 1];
                inputPopupCharacter?.UpdateClassId(option);
            }
        }


    }
}