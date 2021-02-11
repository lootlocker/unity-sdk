using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    [CreateAssetMenu(fileName = "LootLockerEndPoints", menuName = "ScriptableObjects/LootLockerEndPoints", order = 2)]
    public class LootLockerEndPoints : RuntimeProjectSettings<LootLockerEndPoints>
    {
        public override string SettingName { get { return "LootLockerEndPoints"; } }

        private static LootLockerEndPoints _current;

        public static LootLockerEndPoints current
        {
            get
            {
                if (_current == null)
                {
                    _current = Get();
                }

                return _current;
            }
        }

        //Admin 
        [Header("Demo Game Admin Endpoints")]
        public EndPointClass creatingAGame = new EndPointClass("v1/game", LootLockerHTTPMethod.POST);
        public EndPointClass getDetailedInformationAboutAGame = new EndPointClass("v1/game/{0}", LootLockerHTTPMethod.GET);
        public EndPointClass listTriggers = new EndPointClass("v1/game/{0}/triggers", LootLockerHTTPMethod.GET);
        public EndPointClass createTriggers = new EndPointClass("v1/game/{0}/triggers", LootLockerHTTPMethod.POST);

        //Authentication
        [Header("Authentication Endpoints")]
        public EndPointClass playerVerification = new EndPointClass("v1/player/verify", LootLockerHTTPMethod.POST);
        public EndPointClass authenticationRequest = new EndPointClass("v2/session", LootLockerHTTPMethod.POST);
        public EndPointClass endingSession = new EndPointClass("v1/session", LootLockerHTTPMethod.DELETE);
        public EndPointClass initialAuthenticationRequest = new EndPointClass("v1/session", LootLockerHTTPMethod.POST);
        public EndPointClass twoFactorAuthenticationCodeVerification = new EndPointClass("v1/2fa", LootLockerHTTPMethod.POST);
        public EndPointClass subsequentRequests = new EndPointClass("v1/games", LootLockerHTTPMethod.GET);

        //Player
        [Header("Player Endpoints")]
        public EndPointClass getPlayerInfo = new EndPointClass("v1/player/info", LootLockerHTTPMethod.GET);
        public EndPointClass getInventory = new EndPointClass("v1/player/inventory/list", LootLockerHTTPMethod.GET);
        public EndPointClass getCurrencyBalance = new EndPointClass("v1/player/balance", LootLockerHTTPMethod.GET);
        public EndPointClass submitXp = new EndPointClass("v1/player/score", LootLockerHTTPMethod.POST);
        public EndPointClass getXpAndLevel = new EndPointClass("v1/player/score/{0}?platform={1}", LootLockerHTTPMethod.GET);
        public EndPointClass playerAssetNotifications = new EndPointClass("v1/player/notification/assets", LootLockerHTTPMethod.GET);
        public EndPointClass playerAssetDeactivationNotification = new EndPointClass("v1/player/notification/deactivations", LootLockerHTTPMethod.GET);
        public EndPointClass initiateDlcMigration = new EndPointClass("v1/player/dlcs", LootLockerHTTPMethod.POST);
        public EndPointClass getDlcMigration = new EndPointClass("v1/player/dlcs", LootLockerHTTPMethod.GET);
        public EndPointClass setProfilePrivate = new EndPointClass("v1/player/profile/public", LootLockerHTTPMethod.DELETE);
        public EndPointClass setProfilePublic = new EndPointClass("v1/player/profile/public", LootLockerHTTPMethod.POST);

        //Character
        [Header("Character Endpoints")]
        public EndPointClass characterLoadouts = new EndPointClass("v1/player/character/loadout", LootLockerHTTPMethod.GET);
        public EndPointClass getOtherPlayersCharacterLoadouts = new EndPointClass("v1/player/character/loadout/{0}?platform={1}", LootLockerHTTPMethod.GET);
        public EndPointClass updateCharacter = new EndPointClass("v1/player/character/{0}", LootLockerHTTPMethod.PUT);
        public EndPointClass equipIDAssetToDefaultCharacter = new EndPointClass("v1/player/equip", LootLockerHTTPMethod.POST);
        public EndPointClass equipGlobalAssetToDefaultCharacter = new EndPointClass("v1/player/equip", LootLockerHTTPMethod.POST);
        public EndPointClass equipIDAssetToCharacter = new EndPointClass("v1/player/character/{0}/equip", LootLockerHTTPMethod.POST);
        public EndPointClass equipGlobalAssetToCharacter = new EndPointClass("v1/player/character/{0}/equip", LootLockerHTTPMethod.POST);
        public EndPointClass unEquipIDAssetToDefaultCharacter = new EndPointClass("v1/player/equip/{0}", LootLockerHTTPMethod.DELETE);
        public EndPointClass unEquipIDAssetToCharacter = new EndPointClass("v1/player/character/{0}/equip/{1}", LootLockerHTTPMethod.DELETE);
        public EndPointClass getCurrentLoadoutToDefaultCharacter = new EndPointClass("v1/player/loadout", LootLockerHTTPMethod.GET);
        public EndPointClass getOtherPlayersLoadoutToDefaultCharacter = new EndPointClass("v1/player/loadout/{0}?platform={1}", LootLockerHTTPMethod.GET);
        public EndPointClass getEquipableContextToDefaultCharacter = new EndPointClass("v1/player/character/contexts", LootLockerHTTPMethod.GET);
        public EndPointClass getEquipableContextbyCharacter = new EndPointClass("v1/player/character/{0}/contexts", LootLockerHTTPMethod.GET);

        //Persistentplayer storage 
        [Header("Persitent Player Storage")]
        public EndPointClass getEntirePersistentStorage = new EndPointClass("v1/player/storage", LootLockerHTTPMethod.GET);
        public EndPointClass getSingleKeyFromPersitenctStorage = new EndPointClass("v1/player/storage?key={0}", LootLockerHTTPMethod.GET);
        public EndPointClass updateOrCreateKeyValue = new EndPointClass("v1/player/storage", LootLockerHTTPMethod.POST);
        public EndPointClass deleteKeyValue = new EndPointClass("v1/player/storage?key={0}", LootLockerHTTPMethod.DELETE);
        public EndPointClass getOtherPlayersPublicKeyValuePairs = new EndPointClass("v1/player/{0}/storage", LootLockerHTTPMethod.GET);

        //Asset storage 
        [Header("Assets")]
        public EndPointClass gettingContexts = new EndPointClass("v1/contexts", LootLockerHTTPMethod.GET);
        public EndPointClass gettingAssetListWithCount = new EndPointClass("v1/assets/list?count={0}", LootLockerHTTPMethod.GET);
        public EndPointClass gettingAssetListOriginal = new EndPointClass("v1/assets/list?after={0}&count={1}&filter={2}", LootLockerHTTPMethod.GET);
        public EndPointClass gettingAssetListWithAfterAndCount = new EndPointClass("v1/assets/list?after={0}&count={1}", LootLockerHTTPMethod.GET);
        public EndPointClass getAssetsById = new EndPointClass("v1/assets/by/id?asset_ids={0}", LootLockerHTTPMethod.GET);
        public EndPointClass gettingAllAssets = new EndPointClass("v1/assets", LootLockerHTTPMethod.GET);
        public EndPointClass gettingAssetInformationForOneorMoreAssets = new EndPointClass("v1/asset/{0}", LootLockerHTTPMethod.GET);
        public EndPointClass gettingAssetBoneInformation = new EndPointClass("v1/asset/bones", LootLockerHTTPMethod.GET);
        public EndPointClass listingFavouriteAssets = new EndPointClass("v1/asset/favourites", LootLockerHTTPMethod.GET);
        public EndPointClass addingFavouriteAssets = new EndPointClass("v1/asset/{0}/favourite", LootLockerHTTPMethod.POST);
        public EndPointClass removingFavouriteAssets = new EndPointClass("v1/asset/258/favourite", LootLockerHTTPMethod.DELETE);

        //Asset storage 
        [Header("Asset Instances")]
        public EndPointClass getAllKeyValuePairs = new EndPointClass("v1/asset/instance/storage", LootLockerHTTPMethod.GET);
        public EndPointClass getAllKeyValuePairsToAnInstance = new EndPointClass("v1/asset/instance/{0}/storage", LootLockerHTTPMethod.GET);
        public EndPointClass getAKeyValuePairById = new EndPointClass("v1/asset/instance/{0}/storage/{1}", LootLockerHTTPMethod.GET);
        public EndPointClass createKeyValuePair = new EndPointClass("v1/asset/instance/{0}/storage", LootLockerHTTPMethod.POST);
        public EndPointClass updateOneOrMoreKeyValuePair = new EndPointClass("v1/asset/instance/{0}/storage", LootLockerHTTPMethod.PUT);
        public EndPointClass updateKeyValuePairById = new EndPointClass("v1/asset/instance/{0}/storage/{1}", LootLockerHTTPMethod.PUT);
        public EndPointClass deleteKeyValuePair = new EndPointClass("v1/asset/instance/{0}/storage/{1}", LootLockerHTTPMethod.GET);
        public EndPointClass inspectALootBox = new EndPointClass("v1/asset/instance/{0}/inspect", LootLockerHTTPMethod.GET);
        public EndPointClass openALootBox = new EndPointClass("v1/player/asset/instance/{0}/open", LootLockerHTTPMethod.PUT);

        //UGC
        [Header("UGC")]
        public EndPointClass creatingAnAssetCandidate = new EndPointClass("v1/player/assets/candidates", LootLockerHTTPMethod.POST);
        public EndPointClass updatingAnAssetCandidate = new EndPointClass("v1/player/assets/candidates/{0}", LootLockerHTTPMethod.PUT);
        public EndPointClass gettingASingleAssetCandidate = new EndPointClass("v1/player/assets/candidates/{0}", LootLockerHTTPMethod.GET);
        public EndPointClass deletingAnAssetCandidate = new EndPointClass("v1/player/assets/candidates/{0}", LootLockerHTTPMethod.DELETE);
        public EndPointClass listingAssetCandidates = new EndPointClass("v1/player/assets/candidates", LootLockerHTTPMethod.GET);
        public EndPointClass addingFilesToAssetCandidates = new EndPointClass("v1/player/assets/candidates/{0}/file", LootLockerHTTPMethod.UPLOAD);
        public EndPointClass removingFilesFromAssetCandidates = new EndPointClass("v1/player/assets/candidates/{0}/file/{1}", LootLockerHTTPMethod.DELETE);

        //Events
        [Header("Events")]
        public EndPointClass gettingAllEvents = new EndPointClass("v1/missions", LootLockerHTTPMethod.GET);
        public EndPointClass gettingASingleEvent = new EndPointClass("v1/mission/{0}", LootLockerHTTPMethod.GET);
        public EndPointClass startingEvent = new EndPointClass("v1/mission/{0}/start", LootLockerHTTPMethod.POST);
        public EndPointClass finishingEvent = new EndPointClass("v1/mission/{0}/end", LootLockerHTTPMethod.POST);

        //UGC
        [Header("Missions")]
        public EndPointClass gettingAllMissions = new EndPointClass("v1/missions", LootLockerHTTPMethod.GET);
        public EndPointClass gettingASingleMission = new EndPointClass("v1/mission/{0}", LootLockerHTTPMethod.GET);
        public EndPointClass startingMission = new EndPointClass("v1/mission/{0}/start", LootLockerHTTPMethod.POST);
        public EndPointClass finishingMission = new EndPointClass("v1/mission/{0}/end", LootLockerHTTPMethod.POST);

        //Maps
        [Header("Maps")]
        public EndPointClass gettingAllMaps = new EndPointClass("v1/maps", LootLockerHTTPMethod.GET);

        //Purchase
        [Header("Purchase")]
        public EndPointClass normalPurchaseCall = new EndPointClass("v1/purchase", LootLockerHTTPMethod.POST);
        public EndPointClass rentalPurchaseCall = new EndPointClass("v1/purchase", LootLockerHTTPMethod.POST);
        public EndPointClass iosPurchaseVerification = new EndPointClass("v1/purchase", LootLockerHTTPMethod.POST);
        public EndPointClass androidPurchaseVerification = new EndPointClass("v1/purchase", LootLockerHTTPMethod.POST);
        public EndPointClass pollingOrderStatus = new EndPointClass("v1/purchase/{0}", LootLockerHTTPMethod.GET);
        public EndPointClass activatingARentalAsset = new EndPointClass("v1/asset/instance/{0}/activate", LootLockerHTTPMethod.GET);

        //EventTrigger
        [Header("EventTrigger")]
        public EndPointClass triggeringAnEvent = new EndPointClass("v1/player/trigger", LootLockerHTTPMethod.POST);
        public EndPointClass listingTriggeredTriggerEvents = new EndPointClass("v1/player/triggers", LootLockerHTTPMethod.GET);

        //Maps
        [Header("Collectables")]
        public EndPointClass gettingCollectables = new EndPointClass("v1/collectable", LootLockerHTTPMethod.GET);
        public EndPointClass collectingAnItem = new EndPointClass("v1/collectable", LootLockerHTTPMethod.POST);

        //Messages
        [Header("Messages")]
        public EndPointClass getMessages = new EndPointClass("v1/messages", LootLockerHTTPMethod.GET);

        //Crashes
        [Header("Crashes")]
        public EndPointClass submittingACrashLog = new EndPointClass("v1/crash", LootLockerHTTPMethod.POST);
    }
}
