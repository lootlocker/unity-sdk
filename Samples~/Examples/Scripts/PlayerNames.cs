using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;

public class PlayerNames : MonoBehaviour
{
    public Text informationText;
    public Text playerIdText;
    public Text playerNameText;

    public InputField playerNameInputField;
    // Start is called before the first frame update
    void Start()
    {
        /* 
         * Override settings to use the Example game setup
         */
        LootLockerSettingsOverrider.OverrideSettings();

        /* Start guest session without an identifier.
         * LootLocker will create an identifier for the user and store it in PlayerPrefs.
         * If you want to create a new player when testing, you can use PlayerPrefs.DeleteKey("LootLockerGuestPlayerID");
         */
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success)
            {
                informationText.text = "Guest session started";
                playerIdText.text = "Player ID:" + response.player_id.ToString();
                GetPlayerName();
            }
            else
            {
                informationText.text = "Error" + response.Error;
            }
        });
    }

    public void SetPlayerName()
    {
        LootLockerSDKManager.SetPlayerName(playerNameInputField.text, (response) =>
        {
            if(response.success)
            {
                informationText.text = "Successfully set player name";
                UpdateNameText(response.name);
            }
            else
            {
                informationText.text = "Could not set player name:"+response.Error;
            }
        });
    }

    public void GetPlayerName()
    {
        LootLockerSDKManager.GetPlayerName((response) =>
        {
            if(response.success)
            {
                UpdateNameText(response.name);
                informationText.text = "Got player name";
            }
            else
            {
                informationText.text = "Could not set player name:"+response.Error;
            }
        });
    }

    public void UpdateNameText(string name)
    {
        playerNameText.text = "Player name:"+name;
    }
}
