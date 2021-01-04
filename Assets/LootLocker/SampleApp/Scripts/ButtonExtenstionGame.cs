using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLockerDemoApp
{
    public class ButtonExtenstionGame : MonoBehaviour
    {
        public GameObject activeButton;
        public GameObject inActiveButton;

        public void ToggleButtonState(bool active)
        {
            activeButton.SetActive(active);
            inActiveButton.SetActive(!active);
        }
    }
}