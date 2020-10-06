using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ButtonExtentionEvent: UnityEvent<ButtonExtention> {}
public class ButtonExtention : MonoBehaviour
{
    Button button;
    public static ButtonExtentionEvent buttonClicked = new ButtonExtentionEvent();
    public GameObject UnselectedButton;
    public GameObject SelectedButton;
    public bool isMainMenu;

    // Start is called before the first frame update
    void Awake()
    {
        UnselectedButton.SetActive(true);
        SelectedButton.SetActive(isMainMenu);
        button = GetComponent<Button>();
        button.onClick.AddListener(ClickedButton);
        buttonClicked.AddListener(ClickedEvent);
    }
    public void ClickedButton()
    {
        buttonClicked?.Invoke(this);
    }
    private void OnDestroy()
    {
        buttonClicked.RemoveListener(ClickedEvent);
    }

    // Update is called once per frame
    public void ClickedEvent(ButtonExtention buttonExtentionEvent)
    {
        if(buttonExtentionEvent == this)
        {
            SelectedButton.SetActive(true);
            UnselectedButton.SetActive(false);
        }
        else
        {
            SelectedButton.SetActive(false);
            UnselectedButton.SetActive(true);
        }
    }
}
