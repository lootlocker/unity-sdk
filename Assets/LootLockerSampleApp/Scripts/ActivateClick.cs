using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LootLockerDemoApp
{
    public class ActivateClick : MonoBehaviour
    {
        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            ClassSelectionButton.buttonClicked.AddListener(Clicked);
        }

        private void Clicked(ClassSelectionButton arg0)
        {
            button.interactable = true;
        }
    }
}