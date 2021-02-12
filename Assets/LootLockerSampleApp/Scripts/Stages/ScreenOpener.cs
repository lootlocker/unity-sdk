using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LootLockerDemoApp
{
    public class ScreenOpener : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        [ContextMenu("TestOpenMenu")]
        public void Open()
        {
            Open(null);
        }
        public void Open(Action onOpen = null)
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.alpha = 1;
            onOpen?.Invoke();
            canvasGroup.interactable = canvasGroup.blocksRaycasts = true;
        }

    }
}