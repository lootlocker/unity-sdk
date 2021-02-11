#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LootLocker.Admin
{
    public class ProjectSettings : SettingsProvider
    {
        private LootLockerConfig gameSettings;
        private LootLockerEndPointsAdmin adminEndPoints;
        private LootLockerEndPoints endPoints;

        private SerializedObject adminEndPointsSerialized;
        private SerializedObject endPointsSerialized;

        private const string ADMIN_END_POINTS_EXPANDED = "LootLockerProjectSettingsAdminEndPointsExpanded";
        private const string END_POINTS_EXPANDED = "LootLockerProjectSettingsEndPointsExpanded";

        public ProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
            if (gameSettings == null)
            {
                gameSettings = LootLockerConfig.Get();
            }

            if (adminEndPoints == null)
            {
                adminEndPoints = LootLockerEndPointsAdmin.Get();
                adminEndPointsSerialized = new SerializedObject(adminEndPoints);
            }

            if (endPointsSerialized == null)
            {
                endPoints = LootLockerEndPoints.Get();
                endPointsSerialized = new SerializedObject(endPoints);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(6f);

                using (new GUILayout.VerticalScope())
                {
                    DrawGameSettings();

                    EditorGUILayout.Space();

                    bool expandEndPoints = EditorPrefs.GetBool(END_POINTS_EXPANDED, false);
                    EditorGUI.BeginChangeCheck();
                    expandEndPoints = EditorGUILayout.Foldout(expandEndPoints, "Game End Points", true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetBool(END_POINTS_EXPANDED, expandEndPoints);
                    }
                    if (expandEndPoints)
                    {
                        EditorGUI.indentLevel++;
                        DrawEndPoints();
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Space();

                    bool expandAdminEndPoints = EditorPrefs.GetBool(ADMIN_END_POINTS_EXPANDED, false);
                    EditorGUI.BeginChangeCheck();
                    expandAdminEndPoints = EditorGUILayout.Foldout(expandAdminEndPoints, "Admin End Points", true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetBool(ADMIN_END_POINTS_EXPANDED, expandAdminEndPoints);
                    }
                    if (expandAdminEndPoints)
                    {
                        EditorGUI.indentLevel++;
                        DrawAdminEndPoints();
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        private void DrawGameSettings()
        {
            string apiKey = gameSettings.apiKey;
            EditorGUI.BeginChangeCheck();
            apiKey = EditorGUILayout.TextField("API Key", apiKey);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.apiKey = apiKey;
                gameSettings.EditorSave();
            }

            string gameVersion = gameSettings.game_version;
            EditorGUI.BeginChangeCheck();
            gameVersion = EditorGUILayout.TextField("Game Version", gameVersion);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.game_version = gameVersion;
                gameSettings.EditorSave();
            }

            LootLockerGenericConfig.platformType platform = gameSettings.platform;
            EditorGUI.BeginChangeCheck();
            platform = (LootLockerGenericConfig.platformType)EditorGUILayout.EnumPopup("Platform", platform);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.platform = platform;
                gameSettings.EditorSave();
            }

            LootLockerGenericConfig.environmentType environment = gameSettings.environment;
            EditorGUI.BeginChangeCheck();
            environment = (LootLockerGenericConfig.environmentType)EditorGUILayout.EnumPopup("Environment", environment);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.environment = environment;
                gameSettings.EditorSave();
            }

            LootLockerGenericConfig.DebugLevel debugLevel = gameSettings.currentDebugLevel;
            EditorGUI.BeginChangeCheck();
            debugLevel = (LootLockerGenericConfig.DebugLevel)EditorGUILayout.EnumPopup("Current Debug Level", debugLevel);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.currentDebugLevel = debugLevel;
                gameSettings.EditorSave();
            }

            bool allowTokenRefresh = gameSettings.allowTokenRefresh;
            EditorGUI.BeginChangeCheck();
            allowTokenRefresh = EditorGUILayout.Toggle("Allow Token Refresh", allowTokenRefresh);
            if (EditorGUI.EndChangeCheck())
            {
                gameSettings.allowTokenRefresh = allowTokenRefresh;
                gameSettings.EditorSave();
            }
        }

        private void DrawAdminEndPoints()
        {
            DrawEndPointClass(adminEndPoints.initialAuthenticationRequest, adminEndPointsSerialized.FindProperty("initialAuthenticationRequest"));
            DrawEndPointClass(adminEndPoints.twoFactorAuthenticationCodeVerification, adminEndPointsSerialized.FindProperty("twoFactorAuthenticationCodeVerification"));
            DrawEndPointClass(adminEndPoints.subsequentRequests, adminEndPointsSerialized.FindProperty("subsequentRequests"));

            DrawEndPointClass(adminEndPoints.getAllGamesToTheCurrentUser, adminEndPointsSerialized.FindProperty("getAllGamesToTheCurrentUser"));
            DrawEndPointClass(adminEndPoints.creatingAGame, adminEndPointsSerialized.FindProperty("creatingAGame"));
            DrawEndPointClass(adminEndPoints.getDetailedInformationAboutAGame, adminEndPointsSerialized.FindProperty("getDetailedInformationAboutAGame"));
            DrawEndPointClass(adminEndPoints.updatingInformationAboutAGame, adminEndPointsSerialized.FindProperty("updatingInformationAboutAGame"));
            DrawEndPointClass(adminEndPoints.deletingGames, adminEndPointsSerialized.FindProperty("deletingGames"));

            DrawEndPointClass(adminEndPoints.searchingForPlayers, adminEndPointsSerialized.FindProperty("searchingForPlayers"));

            DrawEndPointClass(adminEndPoints.gettingAllMapsToAGame, adminEndPointsSerialized.FindProperty("gettingAllMapsToAGame"));
            DrawEndPointClass(adminEndPoints.creatingMaps, adminEndPointsSerialized.FindProperty("creatingMaps"));
            DrawEndPointClass(adminEndPoints.updatingMaps, adminEndPointsSerialized.FindProperty("updatingMaps"));

            DrawEndPointClass(adminEndPoints.creatingEvent, adminEndPointsSerialized.FindProperty("creatingEvent"));
            DrawEndPointClass(adminEndPoints.updatingEvent, adminEndPointsSerialized.FindProperty("updatingEvent"));
            DrawEndPointClass(adminEndPoints.gettingAllEvents, adminEndPointsSerialized.FindProperty("gettingAllEvents"));

            DrawEndPointClass(adminEndPoints.listTriggers, adminEndPointsSerialized.FindProperty("listTriggers"));
            DrawEndPointClass(adminEndPoints.createTriggers, adminEndPointsSerialized.FindProperty("createTriggers"));
            DrawEndPointClass(adminEndPoints.updateTriggers, adminEndPointsSerialized.FindProperty("updateTriggers"));
            DrawEndPointClass(adminEndPoints.deleteTriggers, adminEndPointsSerialized.FindProperty("deleteTriggers"));

            DrawEndPointClass(adminEndPoints.uploadFile, adminEndPointsSerialized.FindProperty("uploadFile"));
            DrawEndPointClass(adminEndPoints.getFiles, adminEndPointsSerialized.FindProperty("getFiles"));
            DrawEndPointClass(adminEndPoints.updateFile, adminEndPointsSerialized.FindProperty("updateFile"));
            DrawEndPointClass(adminEndPoints.deleteFile, adminEndPointsSerialized.FindProperty("deleteFile"));

            DrawEndPointClass(adminEndPoints.createAsset, adminEndPointsSerialized.FindProperty("createAsset"));
            DrawEndPointClass(adminEndPoints.getContexts, adminEndPointsSerialized.FindProperty("getContexts"));
            DrawEndPointClass(adminEndPoints.getAllAssets, adminEndPointsSerialized.FindProperty("getAllAssets"));

            DrawEndPointClass(adminEndPoints.setupTwoFactorAuthentication, adminEndPointsSerialized.FindProperty("setupTwoFactorAuthentication"));
            DrawEndPointClass(adminEndPoints.verifyTwoFactorAuthenticationSetup, adminEndPointsSerialized.FindProperty("verifyTwoFactorAuthenticationSetup"));
            DrawEndPointClass(adminEndPoints.removeTwoFactorAuthentication, adminEndPointsSerialized.FindProperty("removeTwoFactorAuthentication"));
        }

        private void DrawEndPoints()
        {
            DrawEndPointClass(endPoints.creatingAGame, endPointsSerialized.FindProperty("creatingAGame"));
            DrawEndPointClass(endPoints.getDetailedInformationAboutAGame, endPointsSerialized.FindProperty("getDetailedInformationAboutAGame"));
            DrawEndPointClass(endPoints.listTriggers, endPointsSerialized.FindProperty("listTriggers"));
            DrawEndPointClass(endPoints.createTriggers, endPointsSerialized.FindProperty("createTriggers"));

            DrawEndPointClass(endPoints.playerVerification, endPointsSerialized.FindProperty("playerVerification"));
            DrawEndPointClass(endPoints.authenticationRequest, endPointsSerialized.FindProperty("authenticationRequest"));
            DrawEndPointClass(endPoints.endingSession, endPointsSerialized.FindProperty("endingSession"));
            DrawEndPointClass(endPoints.initialAuthenticationRequest, endPointsSerialized.FindProperty("initialAuthenticationRequest"));
            DrawEndPointClass(endPoints.twoFactorAuthenticationCodeVerification, endPointsSerialized.FindProperty("twoFactorAuthenticationCodeVerification"));
            DrawEndPointClass(endPoints.subsequentRequests, endPointsSerialized.FindProperty("subsequentRequests"));

            DrawEndPointClass(endPoints.getPlayerInfo, endPointsSerialized.FindProperty("getPlayerInfo"));
            DrawEndPointClass(endPoints.getInventory, endPointsSerialized.FindProperty("getInventory"));
            DrawEndPointClass(endPoints.getCurrencyBalance, endPointsSerialized.FindProperty("getCurrencyBalance"));
            DrawEndPointClass(endPoints.submitXp, endPointsSerialized.FindProperty("submitXp"));
            DrawEndPointClass(endPoints.getXpAndLevel, endPointsSerialized.FindProperty("getXpAndLevel"));
            DrawEndPointClass(endPoints.playerAssetNotifications, endPointsSerialized.FindProperty("playerAssetNotifications"));
            DrawEndPointClass(endPoints.playerAssetDeactivationNotification, endPointsSerialized.FindProperty("playerAssetDeactivationNotification"));
            DrawEndPointClass(endPoints.initiateDlcMigration, endPointsSerialized.FindProperty("initiateDlcMigration"));
            DrawEndPointClass(endPoints.getDlcMigration, endPointsSerialized.FindProperty("getDlcMigration"));
            DrawEndPointClass(endPoints.setProfilePrivate, endPointsSerialized.FindProperty("setProfilePrivate"));
            DrawEndPointClass(endPoints.setProfilePublic, endPointsSerialized.FindProperty("setProfilePublic"));

            DrawEndPointClass(endPoints.characterLoadouts, endPointsSerialized.FindProperty("characterLoadouts"));
            DrawEndPointClass(endPoints.getOtherPlayersCharacterLoadouts, endPointsSerialized.FindProperty("getOtherPlayersCharacterLoadouts"));
            DrawEndPointClass(endPoints.updateCharacter, endPointsSerialized.FindProperty("updateCharacter"));
            DrawEndPointClass(endPoints.equipIDAssetToDefaultCharacter, endPointsSerialized.FindProperty("equipIDAssetToDefaultCharacter"));
            DrawEndPointClass(endPoints.equipGlobalAssetToDefaultCharacter, endPointsSerialized.FindProperty("equipGlobalAssetToDefaultCharacter"));
            DrawEndPointClass(endPoints.equipIDAssetToCharacter, endPointsSerialized.FindProperty("equipIDAssetToCharacter"));
            DrawEndPointClass(endPoints.equipGlobalAssetToCharacter, endPointsSerialized.FindProperty("equipGlobalAssetToCharacter"));
            DrawEndPointClass(endPoints.unEquipIDAssetToDefaultCharacter, endPointsSerialized.FindProperty("unEquipIDAssetToDefaultCharacter"));
            DrawEndPointClass(endPoints.unEquipIDAssetToCharacter, endPointsSerialized.FindProperty("unEquipIDAssetToCharacter"));
            DrawEndPointClass(endPoints.getCurrentLoadoutToDefaultCharacter, endPointsSerialized.FindProperty("getCurrentLoadoutToDefaultCharacter"));
            DrawEndPointClass(endPoints.getOtherPlayersLoadoutToDefaultCharacter, endPointsSerialized.FindProperty("getOtherPlayersLoadoutToDefaultCharacter"));
            DrawEndPointClass(endPoints.getEquipableContextToDefaultCharacter, endPointsSerialized.FindProperty("getEquipableContextToDefaultCharacter"));
            DrawEndPointClass(endPoints.getEquipableContextbyCharacter, endPointsSerialized.FindProperty("getEquipableContextbyCharacter"));

            DrawEndPointClass(endPoints.getEntirePersistentStorage, endPointsSerialized.FindProperty("getEntirePersistentStorage"));
            DrawEndPointClass(endPoints.getSingleKeyFromPersitenctStorage, endPointsSerialized.FindProperty("getSingleKeyFromPersitenctStorage"));
            DrawEndPointClass(endPoints.updateOrCreateKeyValue, endPointsSerialized.FindProperty("updateOrCreateKeyValue"));
            DrawEndPointClass(endPoints.deleteKeyValue, endPointsSerialized.FindProperty("deleteKeyValue"));
            DrawEndPointClass(endPoints.getOtherPlayersPublicKeyValuePairs, endPointsSerialized.FindProperty("getOtherPlayersPublicKeyValuePairs"));

            DrawEndPointClass(endPoints.gettingContexts, endPointsSerialized.FindProperty("gettingContexts"));
            DrawEndPointClass(endPoints.gettingAssetListWithCount, endPointsSerialized.FindProperty("gettingAssetListWithCount"));
            DrawEndPointClass(endPoints.gettingAssetListOriginal, endPointsSerialized.FindProperty("gettingAssetListOriginal"));
            DrawEndPointClass(endPoints.gettingAssetListWithAfterAndCount, endPointsSerialized.FindProperty("gettingAssetListWithAfterAndCount"));
            DrawEndPointClass(endPoints.getAssetsById, endPointsSerialized.FindProperty("getAssetsById"));
            DrawEndPointClass(endPoints.gettingAllAssets, endPointsSerialized.FindProperty("gettingAllAssets"));
            DrawEndPointClass(endPoints.gettingAssetInformationForOneorMoreAssets, endPointsSerialized.FindProperty("gettingAssetInformationForOneorMoreAssets"));
            DrawEndPointClass(endPoints.gettingAssetBoneInformation, endPointsSerialized.FindProperty("gettingAssetBoneInformation"));
            DrawEndPointClass(endPoints.listingFavouriteAssets, endPointsSerialized.FindProperty("listingFavouriteAssets"));
            DrawEndPointClass(endPoints.addingFavouriteAssets, endPointsSerialized.FindProperty("addingFavouriteAssets"));
            DrawEndPointClass(endPoints.removingFavouriteAssets, endPointsSerialized.FindProperty("removingFavouriteAssets"));

            DrawEndPointClass(endPoints.getAllKeyValuePairs, endPointsSerialized.FindProperty("getAllKeyValuePairs"));
            DrawEndPointClass(endPoints.getAllKeyValuePairsToAnInstance, endPointsSerialized.FindProperty("getAllKeyValuePairsToAnInstance"));
            DrawEndPointClass(endPoints.getAKeyValuePairById, endPointsSerialized.FindProperty("getAKeyValuePairById"));
            DrawEndPointClass(endPoints.createKeyValuePair, endPointsSerialized.FindProperty("createKeyValuePair"));
            DrawEndPointClass(endPoints.updateOneOrMoreKeyValuePair, endPointsSerialized.FindProperty("updateOneOrMoreKeyValuePair"));
            DrawEndPointClass(endPoints.updateKeyValuePairById, endPointsSerialized.FindProperty("updateKeyValuePairById"));
            DrawEndPointClass(endPoints.deleteKeyValuePair, endPointsSerialized.FindProperty("deleteKeyValuePair"));
            DrawEndPointClass(endPoints.inspectALootBox, endPointsSerialized.FindProperty("inspectALootBox"));
            DrawEndPointClass(endPoints.openALootBox, endPointsSerialized.FindProperty("openALootBox"));

            DrawEndPointClass(endPoints.creatingAnAssetCandidate, endPointsSerialized.FindProperty("creatingAnAssetCandidate"));
            DrawEndPointClass(endPoints.updatingAnAssetCandidate, endPointsSerialized.FindProperty("updatingAnAssetCandidate"));
            DrawEndPointClass(endPoints.gettingASingleAssetCandidate, endPointsSerialized.FindProperty("gettingASingleAssetCandidate"));
            DrawEndPointClass(endPoints.deletingAnAssetCandidate, endPointsSerialized.FindProperty("deletingAnAssetCandidate"));
            DrawEndPointClass(endPoints.listingAssetCandidates, endPointsSerialized.FindProperty("listingAssetCandidates"));
            DrawEndPointClass(endPoints.addingFilesToAssetCandidates, endPointsSerialized.FindProperty("addingFilesToAssetCandidates"));
            DrawEndPointClass(endPoints.removingFilesFromAssetCandidates, endPointsSerialized.FindProperty("removingFilesFromAssetCandidates"));

            DrawEndPointClass(endPoints.gettingAllEvents, endPointsSerialized.FindProperty("gettingAllEvents"));
            DrawEndPointClass(endPoints.gettingASingleEvent, endPointsSerialized.FindProperty("gettingASingleEvent"));
            DrawEndPointClass(endPoints.startingEvent, endPointsSerialized.FindProperty("startingEvent"));
            DrawEndPointClass(endPoints.finishingEvent, endPointsSerialized.FindProperty("finishingEvent"));

            DrawEndPointClass(endPoints.gettingAllMissions, endPointsSerialized.FindProperty("gettingAllMissions"));
            DrawEndPointClass(endPoints.gettingASingleMission, endPointsSerialized.FindProperty("gettingASingleMission"));
            DrawEndPointClass(endPoints.startingMission, endPointsSerialized.FindProperty("startingMission"));
            DrawEndPointClass(endPoints.finishingMission, endPointsSerialized.FindProperty("finishingMission"));

            DrawEndPointClass(endPoints.gettingAllMaps, endPointsSerialized.FindProperty("gettingAllMaps"));

            DrawEndPointClass(endPoints.normalPurchaseCall, endPointsSerialized.FindProperty("normalPurchaseCall"));
            DrawEndPointClass(endPoints.rentalPurchaseCall, endPointsSerialized.FindProperty("rentalPurchaseCall"));
            DrawEndPointClass(endPoints.iosPurchaseVerification, endPointsSerialized.FindProperty("iosPurchaseVerification"));
            DrawEndPointClass(endPoints.androidPurchaseVerification, endPointsSerialized.FindProperty("androidPurchaseVerification"));
            DrawEndPointClass(endPoints.pollingOrderStatus, endPointsSerialized.FindProperty("pollingOrderStatus"));
            DrawEndPointClass(endPoints.activatingARentalAsset, endPointsSerialized.FindProperty("activatingARentalAsset"));

            DrawEndPointClass(endPoints.triggeringAnEvent, endPointsSerialized.FindProperty("triggeringAnEvent"));
            DrawEndPointClass(endPoints.listingTriggeredTriggerEvents, endPointsSerialized.FindProperty("listingTriggeredTriggerEvents"));

            DrawEndPointClass(endPoints.gettingCollectables, endPointsSerialized.FindProperty("gettingCollectables"));
            DrawEndPointClass(endPoints.collectingAnItem, endPointsSerialized.FindProperty("collectingAnItem"));

            DrawEndPointClass(endPoints.getMessages, endPointsSerialized.FindProperty("getMessages"));

            DrawEndPointClass(endPoints.submittingACrashLog, endPointsSerialized.FindProperty("submittingACrashLog"));
        }

        private void DrawEndPointClass(EndPointClass endpoint, SerializedProperty property)
        {
            bool expanded = property.isExpanded;
            EditorGUI.BeginChangeCheck();
            expanded = EditorGUILayout.Foldout(expanded, property.displayName, true);
            if (EditorGUI.EndChangeCheck())
            {
                property.isExpanded = expanded;
                property.serializedObject.ApplyModifiedProperties();
            }

            if (expanded)
            {
                EditorGUI.indentLevel++;

                string endPoint = endpoint.endPoint;
                EditorGUI.BeginChangeCheck();
                endPoint = EditorGUILayout.TextField("End Point", endPoint);
                if (EditorGUI.EndChangeCheck())
                {
                    endpoint.endPoint = endPoint;
                    adminEndPoints.EditorSave();
                }

                LootLockerHTTPMethod http = endpoint.httpMethod;
                EditorGUI.BeginChangeCheck();
                http = (LootLockerHTTPMethod)EditorGUILayout.EnumPopup("Http Method", http);
                if (EditorGUI.EndChangeCheck())
                {
                    endpoint.httpMethod = http;
                    adminEndPoints.EditorSave();
                }

                EditorGUI.indentLevel--;
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new ProjectSettings("Project/LootLocker SDK", SettingsScope.Project)
            {
                label = "LootLocker SDK"
            };
        }
    }
}
#endif
