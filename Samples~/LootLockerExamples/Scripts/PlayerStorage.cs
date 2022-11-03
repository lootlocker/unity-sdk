using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;

public class PlayerStorage : MonoBehaviour
{
    public Text informationText;
    public InputField createKeyInputField;
    public InputField createValueInputField;

    public InputField deleteKeyValueInputField;

    public Text keyText;
    public Text valueText;
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
                GetPlayerStorage();
            }
            else
            {
                informationText.text = "Error" + response.Error;
            }
        });
    }

    void GetPlayerStorage()
    {
        LootLockerSDKManager.GetEntirePersistentStorage((response) =>
        {
            if (response.success)
            {
                informationText.text = "Got player storage";
                UpdateKeyValueText(response.payload);
            }
            else
            {
                informationText.text = "Error" + response.Error;
            }
        });
    }

    public void CreateOrUpdateKeyValue()
    {
        LootLockerSDKManager.UpdateOrCreateKeyValue(createKeyInputField.text, createValueInputField.text, (response) =>
        {
            if(response.success)
            {
                informationText.text = "Created key value";
                // Response is entire key/value-storage for player, so no need to do an extra call to update the text
                UpdateKeyValueText(response.payload);
            }
            else
            {
                informationText.text = "Error" + response.Error;
            }
        });
    }

    public void DeleteKeyValue()
    {
        LootLockerSDKManager.DeleteKeyValue(deleteKeyValueInputField.text, (response) =>
        {
            if (response.success)
            {
                informationText.text = "Deleted key value";
                // Response is empty when deleting, need to update manually
                GetPlayerStorage();
            }
            else
            {
                informationText.text = "Error" + response.Error;
            }
        });
    }

    private void UpdateKeyValueText(LootLockerPayload[] payload)
    {
        keyText.text = "";
        valueText.text = "";
        for (int i = 0; i < payload.Length; i++)
        {
            keyText.text += payload[i].key+"\n";
            valueText.text += payload[i].value + "\n";
        }
    }
}
