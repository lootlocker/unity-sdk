using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ScreenCloser : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }
    [ContextMenu("TestCloseMenu")]
    void Close()
    {
        Close(null);
    }

    public void Close(Action onClose = null)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        onClose?.Invoke();
        canvasGroup.interactable = canvasGroup.blocksRaycasts = false;
    }
}

