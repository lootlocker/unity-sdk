using System.Collections;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;

namespace LootLocker
{
    public class GuestLoginLootLockerIdentifier : MonoBehaviour
{
    public Text loginInformationText;
    public Text playerIdText;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DoGuestLogin());
    }

    IEnumerator DoGuestLogin()
    {
        // Wait until end of frame to ensure that the UI has been loaded
        yield return new WaitForEndOfFrame();
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
            if(response.success)
            {
                loginInformationText.text = "Guest session started";
                playerIdText.text = "Player ID:"+response.player_id.ToString();
            }
            else
            {
                loginInformationText.text = "Error" + response.errorData.ToString();
            }
        });
    }
}
}
