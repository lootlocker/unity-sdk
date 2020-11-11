using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootLockerEndPoints", menuName = "ScriptableObjects/LootLockerEndPoints", order = 2)]
public class LootLockerEndPoints : ScriptableObject
{
    public static LootLockerEndPoints current;
    //Admin 
    [Header("Demo Game Admin Endpoints")]
    public EndPointClass creatingAGame;
    public EndPointClass getDetailedInformationAboutAGame;
    public EndPointClass listTriggers;
    public EndPointClass createTriggers;

    //Authentication
    [Header("Authentication Endpoints")]
    public EndPointClass playerVerification;
    public EndPointClass authenticationRequest;
    public EndPointClass endingSession;
    public EndPointClass initialAuthenticationRequest;
    public EndPointClass twoFactorAuthenticationCodeVerification;
    public EndPointClass subsequentRequests;

    //Player
    [Header("Player Endpoints")]
    public EndPointClass getPlayerInfo;
    public EndPointClass getInventory;
    public EndPointClass getCurrencyBalance;
    public EndPointClass submitXp;
    public EndPointClass getXpAndLevel;
    public EndPointClass playerAssetNotifications;
    public EndPointClass playerAssetDeactivationNotification;
    public EndPointClass initiateDlcMigration;
    public EndPointClass getDlcMigration;
    public EndPointClass setProfilePrivate;
    public EndPointClass setProfilePublic;

    //Character
    [Header("Character Endpoints")]
    public EndPointClass characterLoadouts;
    public EndPointClass getOtherPlayersCharacterLoadouts;
    public EndPointClass updateCharacter;
    public EndPointClass equipIDAssetToDefaultCharacter;
    public EndPointClass equipGlobalAssetToDefaultCharacter;
    public EndPointClass equipIDAssetToCharacter;
    public EndPointClass equipGlobalAssetToCharacter;
    public EndPointClass unEquipIDAssetToDefaultCharacter;
    public EndPointClass unEquipIDAssetToCharacter;
    public EndPointClass getCurrentLoadoutToDefaultCharacter;
    public EndPointClass getOtherPlayersLoadoutToDefaultCharacter;
    public EndPointClass getEquipableContextToDefaultCharacter;
    public EndPointClass getEquipableContextbyCharacter;

    //Persistentplayer storage 
    [Header("Persitent Player Storage")]
    public EndPointClass getEntirePersistentStorage;
    public EndPointClass getSingleKeyFromPersitenctStorage;
    public EndPointClass updateOrCreateKeyValue;
    public EndPointClass deleteKeyValue;
    public EndPointClass getOtherPlayersPublicKeyValuePairs;

    //Asset storage 
    [Header("Assets")]
    public EndPointClass gettingContexts;
    public EndPointClass gettingAssetListWithCount;
    public EndPointClass gettingAssetListWithAfterAndCount;
    public EndPointClass getAssetsById;
    public EndPointClass gettingAllAssets;
    public EndPointClass gettingAssetInformationForOneorMoreAssets;
    public EndPointClass gettingAssetBoneInformation;
    public EndPointClass listingFavouriteAssets;
    public EndPointClass addingFavouriteAssets;
    public EndPointClass removingFavouriteAssets;

    //Asset storage 
    [Header("Asset Instances")]
    public EndPointClass getAllKeyValuePairs;
    public EndPointClass getAllKeyValuePairsToAnInstance;
    public EndPointClass getAKeyValuePairById;
    public EndPointClass createKeyValuePair;
    public EndPointClass updateOneOrMoreKeyValuePair;
    public EndPointClass updateKeyValuePairById;
    public EndPointClass deleteKeyValuePair;
    public EndPointClass inspectALootBox;
    public EndPointClass openALootBox;

    //UGC
    [Header("UGC")]
    public EndPointClass creatingAnAssetCandidate;
    public EndPointClass updatingAnAssetCandidate;
    public EndPointClass deletingAnAssetCandidate;
    public EndPointClass listingAssetCandidates;
    public EndPointClass addingFilesToAssetCandidates;
    public EndPointClass removingFilesFromAssetCandidates;

    //Events
    [Header("Events")]
    public EndPointClass gettingAllEvents;
    public EndPointClass gettingASingleEvent;
    public EndPointClass startingEvent;
    public EndPointClass finishingEvent;

    //UGC
    [Header("Missions")]
    public EndPointClass gettingAllMissions;
    public EndPointClass gettingASingleMission;
    public EndPointClass startingMission;
    public EndPointClass finishingMission;

    //Maps
    [Header("Maps")]
    public EndPointClass gettingAllMaps;

    //Purchase
    [Header("Purchase")]
    public EndPointClass normalPurchaseCall;
    public EndPointClass rentalPurchaseCall;
    public EndPointClass iosPurchaseVerification;
    public EndPointClass androidPurchaseVerification;
    public EndPointClass pollingOrderStatus;
    public EndPointClass activatingARentalAsset;

    //EventTrigger
    [Header("EventTrigger")]
    public EndPointClass triggeringAnEvent;
    public EndPointClass listingTriggeredTriggerEvents;

    //Maps
    [Header("Collectables")]
    public EndPointClass gettingCollectables;
    public EndPointClass collectingAnItem;

    //Messages
    [Header("Messages")]
    public EndPointClass getMessages;

    //Crashes
    [Header("Crashes")]
    public EndPointClass submittingACrashLog;
}