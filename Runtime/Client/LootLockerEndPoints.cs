using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    public class LootLockerEndPoints 
    {
        // Authentication
        [Header("Authentication")]
        public static EndPointClass playerVerification = new EndPointClass("v1/player/verify", LootLockerHTTPMethod.POST);
        public static EndPointClass authenticationRequest = new EndPointClass("v2/session", LootLockerHTTPMethod.POST);
        public static EndPointClass guestSessionRequest = new EndPointClass("v2/session/guest", LootLockerHTTPMethod.POST);
        public static EndPointClass endingSession = new EndPointClass("v1/session", LootLockerHTTPMethod.DELETE);
        public static EndPointClass initialAuthenticationRequest = new EndPointClass("v1/session", LootLockerHTTPMethod.POST);
        public static EndPointClass twoFactorAuthenticationCodeVerification = new EndPointClass("v1/2fa", LootLockerHTTPMethod.POST);
        public static EndPointClass subsequentRequests = new EndPointClass("v1/games", LootLockerHTTPMethod.GET);
        public static EndPointClass nintendoSwitchSessionRequest = new EndPointClass("session/nintendo-switch", LootLockerHTTPMethod.POST);
        public static EndPointClass epicSessionRequest = new EndPointClass("session/epic", LootLockerHTTPMethod.POST);
        public static EndPointClass metaSessionRequest = new EndPointClass("session/meta", LootLockerHTTPMethod.POST);
        public static EndPointClass xboxSessionRequest = new EndPointClass("session/xbox-one", LootLockerHTTPMethod.POST);
        public static EndPointClass appleSessionRequest = new EndPointClass("session/apple", LootLockerHTTPMethod.POST);
        public static EndPointClass appleGameCenterSessionRequest = new EndPointClass("session/apple/game-center", LootLockerHTTPMethod.POST);
        public static EndPointClass googleSessionRequest = new EndPointClass("session/google", LootLockerHTTPMethod.POST);

        // Remote Sessions
        [Header("Remote Sessions")]
        public static EndPointClass leaseRemoteSession = new EndPointClass("session/remote/lease", LootLockerHTTPMethod.POST);
        public static EndPointClass startRemoteSession = new EndPointClass("session/remote", LootLockerHTTPMethod.POST);

        // White Label Login
        [Header("White Label Login")]
        public static EndPointClass whiteLabelSignUp = new EndPointClass("white-label-login/sign-up", LootLockerHTTPMethod.POST);
        public static EndPointClass whiteLabelLogin = new EndPointClass("white-label-login/login", LootLockerHTTPMethod.POST);
        public static EndPointClass whiteLabelVerifySession = new EndPointClass("white-label-login/verify-session", LootLockerHTTPMethod.POST);
        public static EndPointClass whiteLabelRequestPasswordReset = new EndPointClass("white-label-login/request-reset-password", LootLockerHTTPMethod.POST);
        public static EndPointClass whiteLabelRequestAccountVerification = new EndPointClass("white-label-login/request-verification", LootLockerHTTPMethod.POST);
        public static EndPointClass whiteLabelLoginSessionRequest = new EndPointClass("v2/session/white-label", LootLockerHTTPMethod.POST);

        // Player
        [Header("Player")]
        public static EndPointClass getPlayerInfo = new EndPointClass("v1/player/info", LootLockerHTTPMethod.GET);
        public static EndPointClass getInventory = new EndPointClass("v1/player/inventory/list", LootLockerHTTPMethod.GET);
        public static EndPointClass getCurrencyBalance = new EndPointClass("v1/player/balance", LootLockerHTTPMethod.GET);
        public static EndPointClass submitXp = new EndPointClass("v1/player/score", LootLockerHTTPMethod.POST);
        public static EndPointClass getXpAndLevel = new EndPointClass("v1/player/score/{0}?platform={1}", LootLockerHTTPMethod.GET);
        public static EndPointClass playerAssetNotifications = new EndPointClass("v1/player/notification/assets", LootLockerHTTPMethod.GET);
        public static EndPointClass playerAssetDeactivationNotification = new EndPointClass("v1/player/notification/deactivations", LootLockerHTTPMethod.GET);
        public static EndPointClass initiateDlcMigration = new EndPointClass("v1/player/dlcs", LootLockerHTTPMethod.POST);
        public static EndPointClass getDlcMigration = new EndPointClass("v1/player/dlcs", LootLockerHTTPMethod.GET);
        public static EndPointClass setProfilePrivate = new EndPointClass("v1/player/profile/public", LootLockerHTTPMethod.DELETE);
        public static EndPointClass setProfilePublic = new EndPointClass("v1/player/profile/public", LootLockerHTTPMethod.POST);
        public static EndPointClass getPlayerName = new EndPointClass("player/name", LootLockerHTTPMethod.GET);
        public static EndPointClass setPlayerName = new EndPointClass("player/name", LootLockerHTTPMethod.PATCH);
        public static EndPointClass deletePlayer = new EndPointClass("player", LootLockerHTTPMethod.DELETE);
        public static EndPointClass lookupPlayerNames = new EndPointClass("player/lookup/name", LootLockerHTTPMethod.GET);
        public static EndPointClass lookupPlayer1stPartyPlatformIDs = new EndPointClass("player/lookup/ids", LootLockerHTTPMethod.GET);
        public static EndPointClass getPlayerFiles = new EndPointClass("player/files", LootLockerHTTPMethod.GET);
        public static EndPointClass getPlayerFilesByPlayerId = new EndPointClass("player/{0}/files", LootLockerHTTPMethod.GET);
        public static EndPointClass getSingleplayerFile = new EndPointClass("player/files/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass uploadPlayerFile = new EndPointClass("player/files", LootLockerHTTPMethod.UPLOAD_FILE);
        public static EndPointClass updatePlayerFile = new EndPointClass("/player/files/{0}", LootLockerHTTPMethod.UPDATE_FILE);
        public static EndPointClass deletePlayerFile = new EndPointClass("/player/files/{0}", LootLockerHTTPMethod.DELETE);

        // Player Progressions
        [Header("Player Progressions")]
        public static EndPointClass getAllPlayerProgressions = new EndPointClass("player/progressions", LootLockerHTTPMethod.GET);
        public static EndPointClass getSinglePlayerProgression = new EndPointClass("player/progressions/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass addPointsToPlayerProgression = new EndPointClass("player/progressions/{0}/points/add", LootLockerHTTPMethod.POST);
        public static EndPointClass subtractPointsFromPlayerProgression = new EndPointClass("player/progressions/{0}/points/subtract", LootLockerHTTPMethod.POST);
        public static EndPointClass resetPlayerProgression = new EndPointClass("player/progressions/{0}/reset", LootLockerHTTPMethod.POST);
        public static EndPointClass deletePlayerProgression = new EndPointClass("player/progressions/{0}", LootLockerHTTPMethod.DELETE);

        // Class
        [Header("Class")]
        public static EndPointClass classLoadouts = new EndPointClass("v1/player/character/loadout", LootLockerHTTPMethod.GET);
        public static EndPointClass getOtherPlayersClassLoadouts = new EndPointClass("v1/player/character/loadout/{0}?platform={1}", LootLockerHTTPMethod.GET);
        public static EndPointClass updateClass = new EndPointClass("v1/player/character/{0}", LootLockerHTTPMethod.PUT);
        public static EndPointClass equipIDAssetToDefaultClass = new EndPointClass("v1/player/equip", LootLockerHTTPMethod.POST);
        public static EndPointClass equipGlobalAssetToDefaultClass = new EndPointClass("v1/player/equip", LootLockerHTTPMethod.POST);
        public static EndPointClass equipIDAssetToClass = new EndPointClass("v1/player/character/{0}/equip", LootLockerHTTPMethod.POST);
        public static EndPointClass equipGlobalAssetToClass = new EndPointClass("v1/player/character/{0}/equip", LootLockerHTTPMethod.POST);
        public static EndPointClass unEquipIDAssetToDefaultClass = new EndPointClass("v1/player/equip/{0}", LootLockerHTTPMethod.DELETE);
        public static EndPointClass unEquipIDAssetToClass = new EndPointClass("v1/player/character/{0}/equip/{1}", LootLockerHTTPMethod.DELETE);
        public static EndPointClass getCurrentLoadoutToDefaultClass = new EndPointClass("v1/player/loadout", LootLockerHTTPMethod.GET);
        public static EndPointClass getOtherPlayersLoadoutToDefaultClass = new EndPointClass("v1/player/loadout/{0}?platform={1}", LootLockerHTTPMethod.GET);
        public static EndPointClass getEquipableContextToDefaultClass = new EndPointClass("v1/player/character/contexts", LootLockerHTTPMethod.GET);
        public static EndPointClass getEquipableContextbyClass = new EndPointClass("v1/player/character/{0}/contexts", LootLockerHTTPMethod.GET);
        public static EndPointClass createClass = new EndPointClass("v1/player/character", LootLockerHTTPMethod.POST);
        public static EndPointClass listClassTypes = new EndPointClass("v1/player/character/types", LootLockerHTTPMethod.GET);
        public static EndPointClass listPlayerClasses = new EndPointClass("v1/player/character/list", LootLockerHTTPMethod.GET);

        // Hero
        [Header("Hero")]
        public static EndPointClass getGameHeroes = new EndPointClass("v1/heroes", LootLockerHTTPMethod.GET);
        public static EndPointClass getHero = new EndPointClass("v1/player/heroes/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass listPlayerHeroes = new EndPointClass("v1/player/heroes", LootLockerHTTPMethod.GET);
        public static EndPointClass listOtherPlayersHeroesBySteamID64 = new EndPointClass("v1/heroes/list/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass getOtherPlayersDefaultHeroBySteamID64 = new EndPointClass("v1/player/heroes/default/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass createHero = new EndPointClass("v1/player/heroes/", LootLockerHTTPMethod.POST);
        public static EndPointClass updateHero = new EndPointClass("v1/player/heroes/{0}", LootLockerHTTPMethod.PUT);
        public static EndPointClass deleteHero = new EndPointClass("v1/player/heroes/{0}", LootLockerHTTPMethod.DELETE);
        public static EndPointClass getHeroInventory = new EndPointClass("v1/player/heroes/{0}/inventory", LootLockerHTTPMethod.GET);
        public static EndPointClass getHeroLoadout = new EndPointClass("v1/player/heroes/{0}/loadout", LootLockerHTTPMethod.GET);
        public static EndPointClass getOtherPlayersHeroLoadout = new EndPointClass("v1/heroes/{0}/loadout", LootLockerHTTPMethod.GET);
        public static EndPointClass addAssetToHeroLoadout = new EndPointClass("v1/player/heroes/{0}/loadout", LootLockerHTTPMethod.POST);
        public static EndPointClass addAssetVariationToHeroLoadout = new EndPointClass("v1/player/heroes/{0}/loadout", LootLockerHTTPMethod.POST);
        public static EndPointClass removeAssetFromHeroLoadout = new EndPointClass("v1/player/heroes/{0}/loadout/{1}", LootLockerHTTPMethod.DELETE);

        // Character Progressions
        [Header("Character Progressions")]
        public static EndPointClass getAllCharacterProgressions = new EndPointClass("player/characters/{0}/progressions", LootLockerHTTPMethod.GET);
        public static EndPointClass getSingleCharacterProgression = new EndPointClass("player/characters/{0}/progressions/{1}", LootLockerHTTPMethod.GET);
        public static EndPointClass addPointsToCharacterProgression = new EndPointClass("player/characters/{0}/progressions/{1}/points/add", LootLockerHTTPMethod.POST);
        public static EndPointClass subtractPointsFromCharacterProgression = new EndPointClass("player/characters/{0}/progressions/{1}/points/subtract", LootLockerHTTPMethod.POST);
        public static EndPointClass resetCharacterProgression = new EndPointClass("player/characters/{0}/progressions/{1}/reset", LootLockerHTTPMethod.POST);
        public static EndPointClass deleteCharacterProgression = new EndPointClass("player/characters/{0}/progressions/{1}", LootLockerHTTPMethod.DELETE);

        // Persistentplayer storage 
        [Header("Persitent Player Storage")]
        public static EndPointClass getEntirePersistentStorage = new EndPointClass("v1/player/storage", LootLockerHTTPMethod.GET);
        public static EndPointClass getSingleKeyFromPersitenctStorage = new EndPointClass("v1/player/storage?key={0}", LootLockerHTTPMethod.GET);
        public static EndPointClass updateOrCreateKeyValue = new EndPointClass("v1/player/storage", LootLockerHTTPMethod.POST);
        public static EndPointClass deleteKeyValue = new EndPointClass("v1/player/storage?key={0}", LootLockerHTTPMethod.DELETE);
        public static EndPointClass getOtherPlayersPublicKeyValuePairs = new EndPointClass("v1/player/{0}/storage", LootLockerHTTPMethod.GET);

        // Asset storage 
        [Header("Assets")]
        public static EndPointClass gettingContexts = new EndPointClass("v1/contexts", LootLockerHTTPMethod.GET);
        public static EndPointClass gettingAssetListWithCount = new EndPointClass("v1/assets/list?count={0}", LootLockerHTTPMethod.GET);
        public static EndPointClass getAssetsById = new EndPointClass("v1/assets/by/id?asset_ids={0}", LootLockerHTTPMethod.GET);
        public static EndPointClass gettingAllAssets = new EndPointClass("v1/assets", LootLockerHTTPMethod.GET);
        public static EndPointClass gettingAssetInformationForOneorMoreAssets = new EndPointClass("v1/asset/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass gettingAssetBoneInformation = new EndPointClass("v1/asset/bones", LootLockerHTTPMethod.GET);
        public static EndPointClass listingFavouriteAssets = new EndPointClass("v1/asset/favourites", LootLockerHTTPMethod.GET);
        public static EndPointClass addingFavouriteAssets = new EndPointClass("v1/asset/{0}/favourite", LootLockerHTTPMethod.POST);
        public static EndPointClass removingFavouriteAssets = new EndPointClass("v1/asset/{0}/favourite", LootLockerHTTPMethod.DELETE);

        // Asset storage 
        [Header("Asset Instances")]
        public static EndPointClass getAllKeyValuePairs = new EndPointClass("v1/asset/instance/storage", LootLockerHTTPMethod.GET);
        public static EndPointClass getAllKeyValuePairsToAnInstance = new EndPointClass("v1/asset/instance/{0}/storage", LootLockerHTTPMethod.GET);
        public static EndPointClass getAKeyValuePairById = new EndPointClass("v1/asset/instance/{0}/storage/{1}", LootLockerHTTPMethod.GET);
        public static EndPointClass createKeyValuePair = new EndPointClass("v1/asset/instance/{0}/storage", LootLockerHTTPMethod.POST);
        public static EndPointClass updateOneOrMoreKeyValuePair = new EndPointClass("v1/asset/instance/{0}/storage", LootLockerHTTPMethod.PUT);
        public static EndPointClass updateKeyValuePairById = new EndPointClass("v1/asset/instance/{0}/storage/{1}", LootLockerHTTPMethod.PUT);
        public static EndPointClass deleteKeyValuePair = new EndPointClass("v1/asset/instance/{0}/storage/{1}", LootLockerHTTPMethod.DELETE);
        public static EndPointClass inspectALootBox = new EndPointClass("v1/asset/instance/{0}/inspect", LootLockerHTTPMethod.GET);
        public static EndPointClass openALootBox = new EndPointClass("v1/asset/instance/{0}/open", LootLockerHTTPMethod.PUT);
        
        // Asset instance progressions
        [Header("Asset instance progressions")]
        public static EndPointClass getAllAssetInstanceProgressions = new EndPointClass("player/assets/instances/{0}/progressions", LootLockerHTTPMethod.GET);
        public static EndPointClass getSingleAssetInstanceProgression = new EndPointClass("player/assets/instances/{0}/progressions/{1}", LootLockerHTTPMethod.GET);
        public static EndPointClass addPointsToAssetInstanceProgression = new EndPointClass("player/assets/instances/{0}/progressions/{1}/points/add", LootLockerHTTPMethod.POST);
        public static EndPointClass subtractPointsFromAssetInstanceProgression = new EndPointClass("player/assets/instances/{0}/progressions/{1}/points/subtract", LootLockerHTTPMethod.POST);
        public static EndPointClass resetAssetInstanceProgression = new EndPointClass("player/assets/instances/{0}/progressions/{1}/reset", LootLockerHTTPMethod.POST);
        public static EndPointClass deleteAssetInstanceProgression = new EndPointClass("player/assets/instances/{0}/progressions/{1}", LootLockerHTTPMethod.DELETE);
        
        // UGC
        [Header("UGC")]
        public static EndPointClass creatingAnAssetCandidate = new EndPointClass("v1/player/assets/candidates", LootLockerHTTPMethod.POST);
        public static EndPointClass updatingAnAssetCandidate = new EndPointClass("v1/player/assets/candidates/{0}", LootLockerHTTPMethod.PUT);
        public static EndPointClass gettingASingleAssetCandidate = new EndPointClass("v1/player/assets/candidates/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass deletingAnAssetCandidate = new EndPointClass("v1/player/assets/candidates/{0}", LootLockerHTTPMethod.DELETE);
        public static EndPointClass listingAssetCandidates = new EndPointClass("v1/player/assets/candidates", LootLockerHTTPMethod.GET);
        public static EndPointClass addingFilesToAssetCandidates = new EndPointClass("v1/player/assets/candidates/{0}/file", LootLockerHTTPMethod.UPLOAD_FILE);
        public static EndPointClass removingFilesFromAssetCandidates = new EndPointClass("v1/player/assets/candidates/{0}/file/{1}", LootLockerHTTPMethod.DELETE);

        // Events
        [Header("Events")]
        public static EndPointClass gettingAllEvents = new EndPointClass("v1/missions", LootLockerHTTPMethod.GET);
        public static EndPointClass gettingASingleEvent = new EndPointClass("v1/mission/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass startingEvent = new EndPointClass("v1/mission/{0}/start", LootLockerHTTPMethod.POST);
        public static EndPointClass finishingEvent = new EndPointClass("v1/mission/{0}/end", LootLockerHTTPMethod.POST);

        // UGC
        [Header("Missions")]
        public static EndPointClass gettingAllMissions = new EndPointClass("v1/missions", LootLockerHTTPMethod.GET);
        public static EndPointClass gettingASingleMission = new EndPointClass("v1/mission/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass startingMission = new EndPointClass("v1/mission/{0}/start", LootLockerHTTPMethod.POST);
        public static EndPointClass finishingMission = new EndPointClass("v1/mission/{0}/end", LootLockerHTTPMethod.POST);

        // Maps
        [Header("Maps")]
        public static EndPointClass gettingAllMaps = new EndPointClass("v1/maps", LootLockerHTTPMethod.GET);

        // Purchase
        [Header("Purchase")]
        public static EndPointClass normalPurchaseCall = new EndPointClass("v1/purchase", LootLockerHTTPMethod.POST);
        public static EndPointClass rentalPurchaseCall = new EndPointClass("v1/purchase", LootLockerHTTPMethod.POST);
        public static EndPointClass iosPurchaseVerification = new EndPointClass("v1/purchase", LootLockerHTTPMethod.POST);
        public static EndPointClass androidPurchaseVerification = new EndPointClass("v1/purchase", LootLockerHTTPMethod.POST);
        public static EndPointClass pollingOrderStatus = new EndPointClass("v1/purchase/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass activatingARentalAsset = new EndPointClass("v1/asset/instance/{0}/activate", LootLockerHTTPMethod.POST);
        public static EndPointClass purchaseCatalogItem = new EndPointClass("purchase", LootLockerHTTPMethod.POST);

        // EventTrigger
        [Header("EventTrigger")]
        public static EndPointClass triggeringAnEvent = new EndPointClass("v1/player/trigger", LootLockerHTTPMethod.POST);
        public static EndPointClass listingTriggeredTriggerEvents = new EndPointClass("v1/player/triggers", LootLockerHTTPMethod.GET);

        // Maps
        [Header("Collectables")]
        public static EndPointClass gettingCollectables = new EndPointClass("v1/collectable", LootLockerHTTPMethod.GET);
        public static EndPointClass collectingAnItem = new EndPointClass("v1/collectable", LootLockerHTTPMethod.POST);

        // Messages
        [Header("Messages")]
        public static EndPointClass getMessages = new EndPointClass("v1/messages", LootLockerHTTPMethod.GET);

        // Crashes
        [Header("Crashes")]
        public static EndPointClass submittingACrashLog = new EndPointClass("v1/crash", LootLockerHTTPMethod.POST);

        // Leaderboards
        [Header("Leaderboards")]
        public static EndPointClass getMemberRank = new EndPointClass("leaderboards/{0}/member/{1}", LootLockerHTTPMethod.GET);
        public static EndPointClass getByListOfMembers = new EndPointClass("leaderboards/{0}/members", LootLockerHTTPMethod.POST);
        public static EndPointClass getAllMemberRanks = new EndPointClass("leaderboards/member/{0}?count={1}", LootLockerHTTPMethod.GET);
        public static EndPointClass getScoreList = new EndPointClass("leaderboards/{0}/list?count={1}", LootLockerHTTPMethod.GET);
        public static EndPointClass submitScore = new EndPointClass("leaderboards/{0}/submit", LootLockerHTTPMethod.POST);
        
        // Progressions
        [Header("Progressions")]
        public static EndPointClass getAllProgressions = new EndPointClass("progressions", LootLockerHTTPMethod.GET);
        public static EndPointClass getSingleProgression = new EndPointClass("progressions/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass getProgressionTiers = new EndPointClass("progressions/{0}/tiers", LootLockerHTTPMethod.GET);
        public static EndPointClass getProgressionTier = new EndPointClass("progressions/{0}/tiers/{1}", LootLockerHTTPMethod.GET);

        // Drop Tables
        [Header("Drop Tables")]
        public static EndPointClass ComputeAndLockDropTable = new EndPointClass("v1/player/droptables/{0}/compute?asset_details={1}", LootLockerHTTPMethod.POST);
        public static EndPointClass PickDropsFromDropTable = new EndPointClass("v1/player/droptables/{0}/pick", LootLockerHTTPMethod.POST);

        // Currencies
        [Header("Currencies")]
        public static EndPointClass listCurrencies = new EndPointClass("currencies", LootLockerHTTPMethod.GET);
        public static EndPointClass getCurrencyDenominationsByCode = new EndPointClass("currency/code/{0}/denominations", LootLockerHTTPMethod.GET);

        // Balances
        [Header("Balances")]
        public static EndPointClass listBalancesInWallet = new EndPointClass("balances/wallet/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass getWalletByWalletId = new EndPointClass("wallet/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass getWalletByHolderId = new EndPointClass("wallet/holder/{0}", LootLockerHTTPMethod.GET);
        public static EndPointClass creditBalanceToWallet = new EndPointClass("balances/credit", LootLockerHTTPMethod.POST);
        public static EndPointClass debitBalanceToWallet = new EndPointClass("balances/debit", LootLockerHTTPMethod.POST);
        public static EndPointClass createWallet = new EndPointClass("wallet", LootLockerHTTPMethod.POST);

        // Catalogs
        [Header("Catalogs")]
        public static EndPointClass listCatalogs = new EndPointClass("catalogs", LootLockerHTTPMethod.GET);
        public static EndPointClass listCatalogItemsByKey = new EndPointClass("catalog/key/{0}/prices", LootLockerHTTPMethod.GET);

        // Misc
        [Header("Misc")]
        public static EndPointClass ping = new EndPointClass("ping", LootLockerHTTPMethod.GET);

        // Reports
        [Header("Reports")]
        public static EndPointClass reportsGetTypes = new EndPointClass("reports/types", LootLockerHTTPMethod.GET);
        public static EndPointClass reportsGetRemovedUGCForPlayer = new EndPointClass("player/ugc/removed", LootLockerHTTPMethod.GET);
        public static EndPointClass reportsCreatePlayer = new EndPointClass("reports/player", LootLockerHTTPMethod.POST);
        public static EndPointClass reportsCreateAsset = new EndPointClass("reports/asset", LootLockerHTTPMethod.POST);

    }
}