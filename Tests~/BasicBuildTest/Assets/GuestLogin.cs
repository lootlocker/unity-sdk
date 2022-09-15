using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;

public class GuestLogin : MonoBehaviour
{
    public Text loginInformationText;
    public Text playerIdText;
    public string apiKey;
    public string domainKey;
    private bool isRequestDone;
    private bool isLoggedIn;
    
    void Start()
    {
        //login("Start");
    }

    void FixedUpdate() {
        login("FixedUpdate");
    }

    public void login(string source) {
        if(isLoggedIn) {
            return;
        }
        isRequestDone = false;
        initSDK();

        //Debug.LogError("From " + source);
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if(response.success)
            {
                loginInformationText.text = "Guest session started";
                playerIdText.text = "Player ID: "+response.player_id.ToString();
                isRequestDone = true;
                isLoggedIn = true;
            }
            else
            {
                loginInformationText.text = "Error: " + response.Error;
                isRequestDone = true;
            }
        });
    }

    private void initSDK() {
        string[] args = System.Environment.GetCommandLineArgs ();
        if(args.Length != 2 && (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(domainKey))) {
            Debug.LogError("Can't run because no api key or domain key supplied");
            return;
        }
        var localApiKey = args.Length != 2 ? apiKey : args[0];
        var localDomainKey = args.Length != 2 ? domainKey : args[1];
        LootLockerSDKManager.Init(localApiKey, "0.0.0.1", LootLocker.LootLockerConfig.platformType.Android, true, localDomainKey);
        LootLocker.LootLockerConfig.current.currentDebugLevel = LootLocker.LootLockerConfig.DebugLevel.All;
    }

    public bool isDone() {
        return isRequestDone;
    }
}