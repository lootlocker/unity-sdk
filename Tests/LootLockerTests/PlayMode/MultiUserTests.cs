using System.Collections;
using LootLocker.Requests;
using LootLocker;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System;


namespace LootLockerTests.PlayMode
{
#if LOOTLOCKER_ENABLE_OVERRIDABLE_STATE_WRITER
    public class InMemoryTestStateWriter : ILootLockerStateWriter
    {
        private Dictionary<string, string> _storage = new Dictionary<string, string>();

        public void DeleteKey(string key)
        {
            if (_storage.ContainsKey(key))
            {
                _storage.Remove(key);
            }
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (_storage.ContainsKey(key))
            {
                return _storage[key];
            }
            return defaultValue;
        }

        public void SetString(string key, string value)
        {
            _storage[key] = value;
        }

        public bool HasKey(string key)
        {
            return _storage.ContainsKey(key);
        }
    }
    
#endif //LOOTLOCKER_ENABLE_OVERRIDABLE_STATE_WRITER

    public class MultiUserTests
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestCounter++;
            configCopy = LootLockerConfig.current;
            #if LOOTLOCKER_ENABLE_OVERRIDABLE_STATE_WRITER
            LootLockerSDKManager.SetStateWriter(new InMemoryTestStateWriter());
            #endif
            Debug.Log($"##### Start of {this.GetType().Name} test no.{TestCounter} setup #####");
            if (!LootLockerConfig.ClearSettings())
            {
                Debug.LogError("Could not clear LootLocker config");
            }

            // Create game
            bool gameCreationCallCompleted = false;
            LootLockerTestGame.CreateGame(testName: this.GetType().Name + TestCounter + " ",
                onComplete: (success, errorMessage, game) =>
                {
                    if (!success)
                    {
                        gameCreationCallCompleted = true;
                        Debug.LogError(errorMessage);
                        SetupFailed = true;
                    }

                    gameUnderTest = game;
                    gameCreationCallCompleted = true;
                });
            yield return new WaitUntil(() => gameCreationCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }

            gameUnderTest?.SwitchToStageEnvironment();

            // Enable guest platform
            bool enableGuestLoginCallCompleted = false;
            gameUnderTest?.EnableGuestLogin((success, errorMessage) =>
            {
                if (!success)
                {
                    Debug.LogError(errorMessage);
                    SetupFailed = true;
                }

                enableGuestLoginCallCompleted = true;
            });
            yield return new WaitUntil(() => enableGuestLoginCallCompleted);
            if (SetupFailed)
            {
                yield break;
            }

            gameUnderTest?.InitializeLootLockerSDK();

            Debug.Log($"##### Start of {this.GetType().Name} test no.{TestCounter} test case #####");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} test case #####");
            if (gameUnderTest != null)
            {
                bool gameDeletionCallCompleted = false;
                gameUnderTest.DeleteGame(((success, errorMessage) =>
                {
                    if (!success)
                    {
                        Debug.LogError(errorMessage);
                    }

                    gameUnderTest = null;
                    gameDeletionCallCompleted = true;
                }));
                yield return new WaitUntil(() => gameDeletionCallCompleted);
            }

            LootLockerStateData.ClearAllSavedStates();

            LootLockerConfig.CreateNewSettings(configCopy.apiKey, configCopy.game_version, configCopy.domainKey,
                configCopy.logLevel, configCopy.logInBuilds, configCopy.logErrorsAsWarnings,
                configCopy.allowTokenRefresh);
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} tear down #####");
            #if LOOTLOCKER_ENABLE_OVERRIDABLE_STATE_WRITER
            LootLockerSDKManager.SetStateWriter(new LootLockerPlayerPrefsStateWriter());
            #endif
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_MakingRequestsWithoutSpecifyingUser_UsesDefaultUser()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response => { guestLoginCompleted = true; });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            string defaultPlayerUlid = LootLockerStateData.GetDefaultPlayerULID();

            // When
            bool pingRequestCompleted = false;
            LootLockerPingResponse pingResponse = null;
            LootLockerSDKManager.Ping(_pingResponse =>
            {
                pingResponse = _pingResponse;
                pingRequestCompleted = true;
            });
            yield return new WaitUntil(() => pingRequestCompleted);

            // Then
            Assert.IsTrue(pingResponse.success, pingResponse.errorData?.ToString());
            Assert.AreEqual(defaultPlayerUlid, pingResponse.requestContext.player_ulid,
                "Default player was not used for request");
            Assert.AreEqual(guestUsersToCreate, LootLockerStateData.GetActivePlayerULIDs().Count,
                "The expected number of local players were not 'active'");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_MakingRequestsWithSpecifiedUser_UsesSpecifiedUser()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response => { guestLoginCompleted = true; });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            string defaultPlayerUlid = LootLockerStateData.GetDefaultPlayerULID();

            // When
            string usePlayerWithUlid = LootLockerStateData.GetActivePlayerULIDs()[2];
            bool pingRequestCompleted = false;
            LootLockerPingResponse pingResponse = null;
            LootLockerSDKManager.Ping(_pingResponse =>
            {
                pingResponse = _pingResponse;
                pingRequestCompleted = true;
            }, usePlayerWithUlid);
            yield return new WaitUntil(() => pingRequestCompleted);

            // Then
            Assert.IsTrue(pingResponse.success, pingResponse.errorData?.ToString());
            Assert.AreNotEqual(defaultPlayerUlid, pingResponse.requestContext.player_ulid,
                "Default player was used for request despite a player being specified");
            Assert.AreEqual(usePlayerWithUlid, pingResponse.requestContext.player_ulid,
                "The specified player was used");
            Assert.AreEqual(guestUsersToCreate, LootLockerStateData.GetActivePlayerULIDs().Count,
                "The expected number of local players were not 'active'");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_RequestCurrentPlayerDataForDefaultUser_GetsCorrectUserData()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            LootLockerGuestSessionResponse firstGuestSessionResponse = null;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    if (i == 0)
                    {
                        firstGuestSessionResponse = response;
                    }
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            // When
            LootLockerGetCurrentPlayerInfoResponse currentPlayerInfoResponse = null;
            bool getCurrentPlayerInfoCompleted = false;
            LootLockerSDKManager.GetCurrentPlayerInfo((response) =>
            {
                currentPlayerInfoResponse = response;
                getCurrentPlayerInfoCompleted = true;
            });

            yield return new WaitUntil(() => getCurrentPlayerInfoCompleted);

            // Then
            Assert.IsNotNull(firstGuestSessionResponse);
            Assert.IsNotNull(currentPlayerInfoResponse);
            Assert.IsTrue(currentPlayerInfoResponse.success, currentPlayerInfoResponse.errorData?.ToString());
            Assert.AreEqual(LootLockerStateData.GetDefaultPlayerULID(), currentPlayerInfoResponse.info.id);
            Assert.AreEqual(firstGuestSessionResponse.player_ulid, currentPlayerInfoResponse.info.id);
            Assert.AreEqual(firstGuestSessionResponse.player_id, currentPlayerInfoResponse.info.legacy_id);
            Assert.AreEqual(firstGuestSessionResponse.player_created_at.ToString(), currentPlayerInfoResponse.info.created_at.ToString());
            Assert.AreEqual(firstGuestSessionResponse.public_uid, currentPlayerInfoResponse.info.public_uid);
            Assert.AreEqual(firstGuestSessionResponse.player_name, currentPlayerInfoResponse.info.name ?? "");
            var defaultPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);
            Assert.AreEqual(defaultPlayerData.ULID, currentPlayerInfoResponse.info.id);
            Assert.AreEqual(defaultPlayerData.LegacyID, currentPlayerInfoResponse.info.legacy_id);
            Assert.AreEqual(defaultPlayerData.CreatedAt.ToString(), currentPlayerInfoResponse.info.created_at.ToString());
            Assert.AreEqual(defaultPlayerData.PublicUID, currentPlayerInfoResponse.info.public_uid);
            Assert.AreEqual(defaultPlayerData.Name, currentPlayerInfoResponse.info.name ?? "");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_SetDefaultPlayerULID_ChangesDefaultPlayerUsedForRequests()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            LootLockerGuestSessionResponse defaultGuestSessionResponse = null;
            LootLockerGuestSessionResponse nonDefaultGuestSessionResponse = null;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    if (i == 0)
                    {
                        defaultGuestSessionResponse = response;
                    }
                    if (i == 3)
                    {
                        nonDefaultGuestSessionResponse = response;
                    }
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            // When
            LootLockerGetCurrentPlayerInfoResponse currentPlayerInfoBeforeDefaultUserSwitch = null;
            bool getCurrentPlayerInfoCompleted = false;
            LootLockerSDKManager.GetCurrentPlayerInfo((response) =>
            {
                currentPlayerInfoBeforeDefaultUserSwitch = response;
                getCurrentPlayerInfoCompleted = true;
            });
            yield return new WaitUntil(() => getCurrentPlayerInfoCompleted);

            LootLockerStateData.SetDefaultPlayerULID(nonDefaultGuestSessionResponse.player_ulid);

            LootLockerGetCurrentPlayerInfoResponse currentPlayerInfoAfterDefaultUserSwitch = null;
            getCurrentPlayerInfoCompleted = false;
            LootLockerSDKManager.GetCurrentPlayerInfo((response) =>
            {
                currentPlayerInfoAfterDefaultUserSwitch = response;
                getCurrentPlayerInfoCompleted = true;
            });
            yield return new WaitUntil(() => getCurrentPlayerInfoCompleted);

            // Then
            Assert.IsNotNull(nonDefaultGuestSessionResponse);
            Assert.IsNotNull(defaultGuestSessionResponse);
            Assert.IsNotNull(currentPlayerInfoBeforeDefaultUserSwitch);
            Assert.IsNotNull(currentPlayerInfoAfterDefaultUserSwitch);
            Assert.IsTrue(currentPlayerInfoBeforeDefaultUserSwitch.success, currentPlayerInfoBeforeDefaultUserSwitch.errorData?.ToString());
            Assert.IsTrue(currentPlayerInfoAfterDefaultUserSwitch.success, currentPlayerInfoAfterDefaultUserSwitch.errorData?.ToString());

            Assert.AreEqual(nonDefaultGuestSessionResponse.player_ulid, LootLockerStateData.GetDefaultPlayerULID());

            Assert.AreEqual(defaultGuestSessionResponse.player_ulid, currentPlayerInfoBeforeDefaultUserSwitch.info.id);
            Assert.AreEqual(defaultGuestSessionResponse.player_id, currentPlayerInfoBeforeDefaultUserSwitch.info.legacy_id);
            Assert.AreEqual(defaultGuestSessionResponse.player_created_at.ToString(), currentPlayerInfoBeforeDefaultUserSwitch.info.created_at.ToString());
            Assert.AreEqual(defaultGuestSessionResponse.public_uid, currentPlayerInfoBeforeDefaultUserSwitch.info.public_uid);
            Assert.AreEqual(defaultGuestSessionResponse.player_name, currentPlayerInfoBeforeDefaultUserSwitch.info.name ?? "");
            var defaultPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(defaultGuestSessionResponse.player_ulid);
            Assert.AreEqual(defaultPlayerData.ULID, currentPlayerInfoBeforeDefaultUserSwitch.info.id);
            Assert.AreEqual(defaultPlayerData.LegacyID, currentPlayerInfoBeforeDefaultUserSwitch.info.legacy_id);
            Assert.AreEqual(defaultPlayerData.CreatedAt.ToString(), currentPlayerInfoBeforeDefaultUserSwitch.info.created_at.ToString());
            Assert.AreEqual(defaultPlayerData.PublicUID, currentPlayerInfoBeforeDefaultUserSwitch.info.public_uid);
            Assert.AreEqual(defaultPlayerData.Name, currentPlayerInfoBeforeDefaultUserSwitch.info.name ?? "");

            Assert.AreEqual(nonDefaultGuestSessionResponse.player_ulid, currentPlayerInfoAfterDefaultUserSwitch.info.id);
            Assert.AreEqual(nonDefaultGuestSessionResponse.player_id, currentPlayerInfoAfterDefaultUserSwitch.info.legacy_id);
            Assert.AreEqual(nonDefaultGuestSessionResponse.player_created_at.ToString(), currentPlayerInfoAfterDefaultUserSwitch.info.created_at.ToString());
            Assert.AreEqual(nonDefaultGuestSessionResponse.public_uid, currentPlayerInfoAfterDefaultUserSwitch.info.public_uid);
            Assert.AreEqual(nonDefaultGuestSessionResponse.player_name, currentPlayerInfoAfterDefaultUserSwitch.info.name ?? "");
            var nonDefaultPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(nonDefaultGuestSessionResponse.player_ulid);
            Assert.AreEqual(nonDefaultPlayerData.ULID, currentPlayerInfoAfterDefaultUserSwitch.info.id);
            Assert.AreEqual(nonDefaultPlayerData.LegacyID, currentPlayerInfoAfterDefaultUserSwitch.info.legacy_id);
            Assert.AreEqual(nonDefaultPlayerData.CreatedAt.ToString(), currentPlayerInfoAfterDefaultUserSwitch.info.created_at.ToString());
            Assert.AreEqual(nonDefaultPlayerData.PublicUID, currentPlayerInfoAfterDefaultUserSwitch.info.public_uid);
            Assert.AreEqual(nonDefaultPlayerData.Name, currentPlayerInfoAfterDefaultUserSwitch.info.name ?? "");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_RequestCurrentPlayerDataForNonDefaultUser_GetsCorrectUserData()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            LootLockerGuestSessionResponse nonDefaultGuestSessionResponse = null;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    if (i == 3)
                    {
                        nonDefaultGuestSessionResponse = response;
                    }
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            // When
            LootLockerGetCurrentPlayerInfoResponse currentPlayerInfoResponse = null;
            bool getCurrentPlayerInfoCompleted = false;
            LootLockerSDKManager.GetCurrentPlayerInfo((response) =>
            {
                currentPlayerInfoResponse = response;
                getCurrentPlayerInfoCompleted = true;
            }, nonDefaultGuestSessionResponse.player_ulid);

            yield return new WaitUntil(() => getCurrentPlayerInfoCompleted);

            // Then
            Assert.IsNotNull(nonDefaultGuestSessionResponse);
            Assert.IsNotNull(currentPlayerInfoResponse);
            Assert.IsTrue(currentPlayerInfoResponse.success, currentPlayerInfoResponse.errorData?.ToString());
            Assert.AreNotEqual(nonDefaultGuestSessionResponse.player_ulid, LootLockerStateData.GetDefaultPlayerULID());
            Assert.AreEqual(nonDefaultGuestSessionResponse.player_ulid, currentPlayerInfoResponse.info.id);
            Assert.AreEqual(nonDefaultGuestSessionResponse.player_id, currentPlayerInfoResponse.info.legacy_id);
            Assert.AreEqual(nonDefaultGuestSessionResponse.player_created_at.ToString(), currentPlayerInfoResponse.info.created_at.ToString());
            Assert.AreEqual(nonDefaultGuestSessionResponse.public_uid, currentPlayerInfoResponse.info.public_uid);
            Assert.AreEqual(nonDefaultGuestSessionResponse.player_name, currentPlayerInfoResponse.info.name ?? "");
            var nonDefaultPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(nonDefaultGuestSessionResponse.player_ulid);
            Assert.AreEqual(nonDefaultPlayerData.ULID, currentPlayerInfoResponse.info.id);
            Assert.AreEqual(nonDefaultPlayerData.LegacyID, currentPlayerInfoResponse.info.legacy_id);
            Assert.AreEqual(nonDefaultPlayerData.CreatedAt.ToString(), currentPlayerInfoResponse.info.created_at.ToString());
            Assert.AreEqual(nonDefaultPlayerData.PublicUID, currentPlayerInfoResponse.info.public_uid);
            Assert.AreEqual(nonDefaultPlayerData.Name, currentPlayerInfoResponse.info.name ?? "");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_SaveStateExistsForPlayerWhenPlayerExists_ReturnsTrue()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 2;
            LootLockerGuestSessionResponse nonDefaultGuestSessionResponse = null;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    if (i == 1)
                    {
                        nonDefaultGuestSessionResponse = response;
                    }
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            // When
            bool saveStateExists = LootLockerStateData.SaveStateExistsForPlayer(nonDefaultGuestSessionResponse.player_ulid);

            // Then
            Assert.IsNotNull(nonDefaultGuestSessionResponse);
            Assert.IsTrue(saveStateExists, "Save state did not exist");
            var playerData =
                LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(nonDefaultGuestSessionResponse.player_ulid);
            Assert.IsNotNull(playerData);
            Assert.AreEqual(nonDefaultGuestSessionResponse.player_ulid, playerData.ULID);
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_SaveStateExistsForPlayerWhenPlayerDoesNotExist_ReturnsFalse()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given

            // Intentionally Empty

            // When
            bool saveStateExists = LootLockerStateData.SaveStateExistsForPlayer("non-existant-ulid");

            // Then
            Assert.IsFalse(saveStateExists, "Save state existed when not expected");
            var playerData =
                LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty("non-existant-ulid");
            Assert.IsNull(playerData);
            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_GetActivePlayerUlid_ListsAllActivePlayers()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            List<string> ulids = new List<string>();
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    ulids.Add(response.player_ulid);
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            // When
            var activePlayerUlids = LootLockerStateData.GetActivePlayerULIDs();

            // Then
            int matches = 0;
            foreach (string expectedUlid in ulids)
            {
                Assert.Contains(expectedUlid, activePlayerUlids);
                matches++;
            }
            Assert.AreEqual(guestUsersToCreate, matches);
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_SetPlayerULIDToInactive_MakesThePlayerNotActiveButSaveStateStillExistsAndIsCached()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            List<string> ulids = new List<string>();
            string nonDefaultPlayerUlid = null;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    ulids.Add(response.player_ulid);
                    if (i == 2)
                    {
                        nonDefaultPlayerUlid = response.player_ulid;
                    }
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }
            var activePlayerUlidsBeforeDeactivation = LootLockerStateData.GetActivePlayerULIDs();
            var cachedPlayerUlidsBeforeDeactivation = LootLockerStateData.GetCachedPlayerULIDs();

            // When
            LootLockerStateData.SetPlayerULIDToInactive(nonDefaultPlayerUlid);

            // Then
            var activePlayerUlidsAfterDeactivation = LootLockerStateData.GetActivePlayerULIDs();
            var cachedPlayerUlidsAfterDeactivation = LootLockerStateData.GetCachedPlayerULIDs();

            Assert.AreEqual(activePlayerUlidsAfterDeactivation.Count, activePlayerUlidsBeforeDeactivation.Count - 1);

            foreach (string expectedUlid in ulids)
            {
                if (expectedUlid.Equals(nonDefaultPlayerUlid))
                {
                    Assert.IsFalse(activePlayerUlidsAfterDeactivation.Contains(expectedUlid));
                    Assert.Contains(expectedUlid, activePlayerUlidsBeforeDeactivation);
                    Assert.Contains(expectedUlid, cachedPlayerUlidsBeforeDeactivation);
                    Assert.Contains(expectedUlid, cachedPlayerUlidsAfterDeactivation);
                }
                else
                {
                    Assert.Contains(expectedUlid, activePlayerUlidsBeforeDeactivation);
                    Assert.Contains(expectedUlid, activePlayerUlidsAfterDeactivation);
                    Assert.Contains(expectedUlid, cachedPlayerUlidsBeforeDeactivation);
                    Assert.Contains(expectedUlid, cachedPlayerUlidsAfterDeactivation);
                }
            }
            Assert.IsTrue(LootLockerStateData.SaveStateExistsForPlayer(nonDefaultPlayerUlid));

            // Then When fetching the inactive player
            var playerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(nonDefaultPlayerUlid);

            // Then then
            var activePlayerUlidsAfterReactivation = LootLockerStateData.GetActivePlayerULIDs();
            var cachedPlayerUlidsAfterReactivation = LootLockerStateData.GetCachedPlayerULIDs();

            Assert.IsNotNull(playerData);
            Assert.AreEqual(nonDefaultPlayerUlid, playerData.ULID);

            foreach (string expectedUlid in ulids)
            {
                Assert.Contains(expectedUlid, activePlayerUlidsAfterReactivation);
                Assert.Contains(expectedUlid, cachedPlayerUlidsAfterReactivation);
            }
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_ClearSavedStateForPlayer_SaveStateIsRemoved()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            List<string> ulids = new List<string>();
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    ulids.Add(response.player_ulid);
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            var ulidToRemove = ulids[2];

            // When
            LootLockerStateData.ClearSavedStateForPlayerWithULID(ulidToRemove);

            // Then
            Assert.IsFalse(LootLockerStateData.SaveStateExistsForPlayer(ulidToRemove));
            Assert.AreEqual(guestUsersToCreate - 1, LootLockerStateData.GetActivePlayerULIDs().Count);
            Assert.AreEqual(guestUsersToCreate - 1, LootLockerStateData.GetCachedPlayerULIDs().Count);
            Assert.IsNull(LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(ulidToRemove));
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_ClearAllSavedStates_AllSaveStatesAreRemoved()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 5;
            List<string> ulids = new List<string>();
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    ulids.Add(response.player_ulid);
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            // When
            LootLockerStateData.ClearAllSavedStates();

            // Then
            Assert.AreEqual(0, LootLockerStateData.GetActivePlayerULIDs().Count);
            Assert.AreEqual(0, LootLockerStateData.GetCachedPlayerULIDs().Count);
            foreach (var ulid in ulids)
            {
                Assert.IsFalse(LootLockerStateData.SaveStateExistsForPlayer(ulid));
                Assert.IsNull(LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(ulid));
            }
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_SetPlayerDataWhenPlayerExists_UpdatesPlayerCache()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 1;
            List<string> ulids = new List<string>();
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    ulids.Add(response.player_ulid);
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            var preUpdatePlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);

            // When
            LootLockerStateData.SetPlayerData(new LootLockerPlayerData
            {
                CreatedAt = preUpdatePlayerData.CreatedAt,
                CurrentPlatform = preUpdatePlayerData.CurrentPlatform,
                Identifier = preUpdatePlayerData.Identifier,
                LastSignIn = preUpdatePlayerData.LastSignIn,
                LegacyID = preUpdatePlayerData.LegacyID,
                Name = "ChangedName",
                PublicUID = preUpdatePlayerData.PublicUID,
                RefreshToken = preUpdatePlayerData.RefreshToken,
                SessionToken = preUpdatePlayerData.SessionToken,
                ULID = preUpdatePlayerData.ULID,
                WalletID = preUpdatePlayerData.WalletID,
                WhiteLabelEmail = preUpdatePlayerData.WhiteLabelEmail,
                WhiteLabelToken = preUpdatePlayerData.WhiteLabelToken
            });

            // Then
            var postUpdatePlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);
            Assert.AreEqual(preUpdatePlayerData.ULID, postUpdatePlayerData.ULID);
            Assert.AreEqual(preUpdatePlayerData.Identifier, postUpdatePlayerData.Identifier);
            Assert.AreEqual(preUpdatePlayerData.LegacyID, postUpdatePlayerData.LegacyID);
            Assert.AreEqual(preUpdatePlayerData.CreatedAt.ToString(), postUpdatePlayerData.CreatedAt.ToString());
            Assert.AreNotEqual(preUpdatePlayerData.Name, postUpdatePlayerData.Name);
            Assert.AreEqual("ChangedName", postUpdatePlayerData.Name);
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_SetPlayerDataWhenPlayerDoesNotExistButOtherPlayersActive_CreatesPlayerCacheButDoesNotSetDefault()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int guestUsersToCreate = 2;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            int preSetPlayerDataPlayerCount = LootLockerStateData.GetActivePlayerULIDs().Count;
            var defaultPlayerPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);

            // When
            LootLockerStateData.SetPlayerData(new LootLockerPlayerData
            {
                CreatedAt = defaultPlayerPlayerData.CreatedAt,
                CurrentPlatform = defaultPlayerPlayerData.CurrentPlatform,
                Identifier = defaultPlayerPlayerData.Identifier,
                LastSignIn = defaultPlayerPlayerData.LastSignIn,
                LegacyID = defaultPlayerPlayerData.LegacyID,
                Name = "ChangedName",
                PublicUID = defaultPlayerPlayerData.PublicUID,
                RefreshToken = defaultPlayerPlayerData.RefreshToken,
                SessionToken = defaultPlayerPlayerData.SessionToken,
                ULID = "ANewUlidMeansANewPlayer",
                WalletID = defaultPlayerPlayerData.WalletID,
                WhiteLabelEmail = defaultPlayerPlayerData.WhiteLabelEmail,
                WhiteLabelToken = defaultPlayerPlayerData.WhiteLabelToken
            });

            // Then
            var postUpdateNewPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty("ANewUlidMeansANewPlayer");
            var postUpdateDefaultPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);

            Assert.AreEqual(preSetPlayerDataPlayerCount + 1, LootLockerStateData.GetActivePlayerULIDs().Count);
            Assert.IsNotNull(defaultPlayerPlayerData);
            Assert.IsNotNull(postUpdateNewPlayerData);
            Assert.IsNotNull(postUpdateDefaultPlayerData);
            Assert.AreEqual(defaultPlayerPlayerData.ULID, postUpdateDefaultPlayerData.ULID);
            Assert.AreEqual(defaultPlayerPlayerData.Name, postUpdateDefaultPlayerData.Name);
            Assert.AreEqual("ANewUlidMeansANewPlayer", postUpdateNewPlayerData.ULID);
            Assert.AreEqual("ChangedName", postUpdateNewPlayerData.Name);
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_SetPlayerDataWhenNoPlayerCachesExist_CreatesPlayerCacheAndSetsDefault()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            int preSetPlayerDataPlayerCount = LootLockerStateData.GetActivePlayerULIDs().Count;
            var defaultPlayerPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);
            var defaultPlayerUlid = LootLockerStateData.GetDefaultPlayerULID();

            // When
            LootLockerStateData.SetPlayerData(new LootLockerPlayerData
            {
                CreatedAt = DateTime.Now,
                LastSignIn = DateTime.Now,
                Identifier = "an-identifier",
                LegacyID = 1337,
                Name = "name",
                PublicUID = "UAUA11",
                RefreshToken = "Refresh-token",
                SessionToken = "Session-token",
                ULID = "HSDHSAJKLDLKASJDLK",
                WalletID = "OASKJHDHLKASJH",
                WhiteLabelEmail = "anemail",
                WhiteLabelToken = "atoken",
                CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Guest)
            });

            // Then
            int postSetPlayerDataPlayerCount = LootLockerStateData.GetActivePlayerULIDs().Count;
            var postSetDefaultPlayerPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);
            var postSetDefaultPlayerUlid = LootLockerStateData.GetDefaultPlayerULID();

            Assert.AreEqual(preSetPlayerDataPlayerCount + 1, postSetPlayerDataPlayerCount);
            Assert.IsNull(defaultPlayerPlayerData);
            Assert.IsNull(defaultPlayerUlid);
            Assert.IsNotNull(postSetDefaultPlayerUlid);
            Assert.IsNotNull(postSetDefaultPlayerPlayerData);
            Assert.AreEqual("HSDHSAJKLDLKASJDLK", postSetDefaultPlayerPlayerData.ULID);
            Assert.AreEqual("HSDHSAJKLDLKASJDLK", postSetDefaultPlayerUlid);

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_SetPlayerDataWhenPlayerCachesExistButNoPlayersAreActive_CreatesPlayerCacheAndSetsDefault()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            List<string> ulids = new List<string>();
            int guestUsersToCreate = 2;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    ulids.Add(response.player_ulid);
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            foreach (var ulid in ulids)
            {
                LootLockerStateData.SetPlayerULIDToInactive(ulid);
            }

            // When
            bool loginCompleted = false;
            LootLockerSDKManager.StartGuestSession("completely-novel-identifier", response =>
            {
                ulids.Add(response.player_ulid);
                loginCompleted = true;
            });
            yield return new WaitUntil(() => loginCompleted);

            // Then
            int postSetPlayerDataPlayerCount = LootLockerStateData.GetActivePlayerULIDs().Count;
            var postSetDefaultPlayerPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);
            var postSetDefaultPlayerUlid = LootLockerStateData.GetDefaultPlayerULID();

            Assert.AreEqual(1, postSetPlayerDataPlayerCount);
            Assert.IsNotNull(postSetDefaultPlayerUlid);
            Assert.IsNotNull(postSetDefaultPlayerPlayerData);
            Assert.AreEqual(ulids[ulids.Count - 1], postSetDefaultPlayerUlid);
            Assert.AreEqual(ulids[ulids.Count - 1], postSetDefaultPlayerPlayerData.ULID);

            yield return null;
        }
        
        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_GetPlayerDataWhenPlayerCachesExistButNoPlayersAreActive_GetsPlayerAndSetsDefault()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            List<string> ulids = new List<string>();
            int guestUsersToCreate = 3;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    ulids.Add(response.player_ulid);
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            foreach (var ulid in ulids)
            {
                LootLockerStateData.SetPlayerULIDToInactive(ulid);
            }

            // When
            bool pingCompleted = false;
            string pingUlid = null;
            LootLockerSDKManager.Ping(response =>
            {
                pingUlid = response.requestContext.player_ulid;
                pingCompleted = true;
            }, ulids[ulids.Count - 1]);
            yield return new WaitUntil(() => pingCompleted);

            // Then
            int postPingActivePlayerCount = LootLockerStateData.GetActivePlayerULIDs().Count;
            var postPingDefaultPlayerPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(null);
            var postPingDefaultPlayerUlid = LootLockerStateData.GetDefaultPlayerULID();

            Assert.AreEqual(1, postPingActivePlayerCount);
            Assert.IsNotNull(postPingDefaultPlayerUlid);
            Assert.IsNotNull(postPingDefaultPlayerPlayerData);
            Assert.AreEqual(ulids[ulids.Count - 1], postPingDefaultPlayerUlid);
            Assert.AreEqual(ulids[ulids.Count - 1], postPingDefaultPlayerPlayerData.ULID);

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_GetPlayerUlidFromWLEmailWhenPlayerIsCached_ReturnsCorrectULID()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            List<string> ulids = new List<string>();
            int guestUsersToCreate = 2;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    ulids.Add(response.player_ulid);
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            // When
            var wlPlayer = new LootLockerPlayerData
            {
                CreatedAt = DateTime.Now,
                LastSignIn = DateTime.Now,
                Identifier = "an-identifier",
                LegacyID = 1337,
                Name = "name",
                PublicUID = "UAUA11",
                RefreshToken = "Refresh-token",
                SessionToken = "Session-token",
                ULID = "HSDHSAJKLDLKASJDLK",
                WalletID = "OASKJHDHLKASJH",
                WhiteLabelEmail = "anemail",
                WhiteLabelToken = "atoken",
                CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Guest)
            };
            LootLockerStateData.SetPlayerData(wlPlayer);

            // Then
            var playerUlid = LootLockerStateData.GetPlayerUlidFromWLEmail(wlPlayer.WhiteLabelEmail);
            var wlPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);

            Assert.IsNotNull(wlPlayerData);
            Assert.AreEqual(wlPlayer.ULID, wlPlayerData.ULID);

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MultiUser_GetPlayerUlidFromWLEmailWhenPlayerIsNotCached_ReturnsNoULID()
        {
            // Setup Succeeded
            Assert.IsFalse(SetupFailed, "Setup did not succeed");

            // Given
            List<string> ulids = new List<string>();
            int guestUsersToCreate = 2;
            for (int i = 0; i < guestUsersToCreate; i++)
            {
                bool guestLoginCompleted = false;
                LootLockerSDKManager.StartGuestSession(response =>
                {
                    ulids.Add(response.player_ulid);
                    guestLoginCompleted = true;
                });
                yield return new WaitUntil(() => guestLoginCompleted);
            }

            // When
            var wlPlayer = new LootLockerPlayerData
            {
                CreatedAt = DateTime.Now,
                LastSignIn = DateTime.Now,
                Identifier = "an-identifier",
                LegacyID = 1337,
                Name = "name",
                PublicUID = "UAUA11",
                RefreshToken = "Refresh-token",
                SessionToken = "Session-token",
                ULID = "HSDHSAJKLDLKASJDLK",
                WalletID = "OASKJHDHLKASJH",
                WhiteLabelEmail = "anemail",
                WhiteLabelToken = "atoken",
                CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Guest)
            };
            LootLockerStateData.SetPlayerData(wlPlayer);

            // Then
            var playerUlid = LootLockerStateData.GetPlayerUlidFromWLEmail(wlPlayer.WhiteLabelEmail + "-jk");
            var notPlayerData = LootLockerStateData.GetStateForPlayerOrDefaultStateOrEmpty(playerUlid);

            Assert.IsNull(playerUlid);
            Assert.AreEqual(ulids[0], notPlayerData.ULID);

            yield return null;
        }
    }
}
