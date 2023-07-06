using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;

namespace LootLocker {
public class GuestLoginUniqueIdentifier : MonoBehaviour
{
    public Text loginInformationText;
    public Text playerIdText;
    // Start is called before the first frame update
    void Start()
    {
        /* 
         * Override settings to use the Example games setup
         */
        LootLockerSettingsOverrider.OverrideSettings();

        /* Start guest session with an unique identifier tied to this device.
         * So if someone uninstall your game, they will be able to log in again when they reinstall to the 
         * same account as long as they are using the same device.
         */
        LootLockerSDKManager.StartGuestSession(SystemInfo.deviceUniqueIdentifier, (response) =>
        {
            if(response.success)
            {
                loginInformationText.text = "Guest session started";
                playerIdText.text = "Player ID:"+response.player_id.ToString();
            }
            else
            {
                loginInformationText.text = "Error" + response.Error;
            }
        });
    }
}
}
