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
    private bool isRequestDone = false;
    private bool isRequestInProgress = false;
    private bool isLoggedIn = false;
    
    void Start()
    {
        login("Start");
    }

    void FixedUpdate() {
        //login("FixedUpdate");
    }

    public void login(string source) {
        if(isLoggedIn || isRequestInProgress) {
            Debug.Log("Not in a state to log in, skipping login request from " + source);
            return;
        }
        Debug.Log("Starting log in request from " + source);
        
        isRequestDone = false;
        isRequestInProgress = true;
        initSDK();

        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if(response.success)
            {
                loginInformationText.text = "Guest session started";
                Debug.Log(loginInformationText.text);
                playerIdText.text = "Player ID: "+response.player_id.ToString();
                Debug.Log(playerIdText.text);
                isRequestDone = true;
                isLoggedIn = true;
                isRequestInProgress = false;
            }
            else
            {
                loginInformationText.text = "Error: " + response.Error;
                Debug.Log(loginInformationText.text);
                isRequestDone = true;
                isRequestInProgress = false;
            }
        });
    }

    private void initSDK() {
        string[] args = System.Environment.GetCommandLineArgs ();
        for(int i = 0; i < args.Length; i++) {
            if(args[i] == "-apikey") {
                apiKey = args[i+1];
            } else if (args[i] == "-domainkey") {
                domainKey = args[i+1];
            }
        }
        if((string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(domainKey))) {
            Debug.LogError("Can't run because no api key or domain key supplied");
            return;
        }
        Debug.Log("Making log in request with apiKey: " + apiKey + " and domainKey: " + domainKey);
        LootLockerSDKManager.Init(apiKey, "0.0.0.1", LootLocker.LootLockerConfig.platformType.Android, true, domainKey);
        LootLocker.LootLockerConfig.current.currentDebugLevel = LootLocker.LootLockerConfig.DebugLevel.All;
    }

    public bool isDone() {
        return isRequestDone;
    }
}