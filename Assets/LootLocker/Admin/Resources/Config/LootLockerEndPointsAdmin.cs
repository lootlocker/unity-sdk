using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootLockerEndPointsAdmin", menuName = "ScriptableObjects/LootLockerEndPointsAdmin", order = 3)]
public class LootLockerEndPointsAdmin : ScriptableObject
{
    public static LootLockerEndPointsAdmin current;
    //Authentication
    [Header("Authentication Endpoints")]
    public EndPointClass initialAuthenticationRequest;
    public EndPointClass twoFactorAuthenticationCodeVerification;
    public EndPointClass subsequentRequests;

    //Games
    [Header("Games Endpoints")]
    [Header("---------------------------")]
    public EndPointClass getAllGamesToTheCurrentUser;
    public EndPointClass creatingAGame;
    public EndPointClass getDetailedInformationAboutAGame;
    public EndPointClass updatingInformationAboutAGame;
    public EndPointClass deletingGames;

    //Players
    [Header("Players Endpoints")]
    [Header("---------------------------")]
    public EndPointClass searchingForPlayers;

    //Maps
    [Header("Maps Endpoints")]
    [Header("---------------------------")]
    public EndPointClass gettingAllMapsToAGame;
    public EndPointClass creatingMaps;
    public EndPointClass updatingMaps;

    //Events
    [Header("Events Endpoints")]
    [Header("---------------------------")]
    public EndPointClass creatingEvent;
    public EndPointClass updatingEvent;
    public EndPointClass gettingAllEvents;

    //Triggers
    [Header("Triggers")]
    [Header("---------------------------")]
    public EndPointClass listTriggers;
    public EndPointClass createTriggers;
    public EndPointClass updateTriggers;
    public EndPointClass deleteTriggers;

    //Files
    [Header("Files Endpoints")]
    [Header("---------------------------")]
    public EndPointClass uploadFile;
    public EndPointClass getFiles;
    public EndPointClass updateFile;
    public EndPointClass deleteFile;

    //Assets
    [Header("Assets Endpoints")]
    [Header("---------------------------")]
    public EndPointClass createAsset;
    public EndPointClass getContexts;
    public EndPointClass getAllAssets;

    //User
    [Header("User Endpoints")]
    [Header("---------------------------")]
    public EndPointClass setupTwoFactorAuthentication;
    public EndPointClass verifyTwoFactorAuthenticationSetup;
    public EndPointClass removeTwoFactorAuthentication;

    //Organisations
    [Header("Organisations")]
    public EndPointClass getUsersToAnOrganisation;

}