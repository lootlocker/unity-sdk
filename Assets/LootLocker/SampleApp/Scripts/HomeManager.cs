using LootLockerRequests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HomeManager : MonoBehaviour, IStageOwner
{
    public ScreenOpener bottomOpener;

    SessionResponse sessionResponse;
    public void UpdateScreenData(IStageData stageData)
    {
        SessionResponse sessionResponse = stageData != null ? stageData as SessionResponse : this.sessionResponse;
        this.sessionResponse = sessionResponse;
        GetComponentInChildren<PlayerProfile>()?.UpdateScreen(sessionResponse);
        GetComponentInChildren<Progression>()?.UpdateScreen(sessionResponse);
        bottomOpener?.Open();
    }

}