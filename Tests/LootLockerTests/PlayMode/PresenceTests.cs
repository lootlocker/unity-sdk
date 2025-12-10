using System;
using System.Collections;
using System.Linq;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class PresenceTests
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
            Debug.Log($"##### Start of {this.GetType().Name} test no.{TestCounter} setup #####");

            if (!LootLockerConfig.ClearSettings())
            {
                Debug.LogError("Could not clear LootLocker config");
            }

            LootLockerConfig.current.logLevel = LootLockerLogger.LogLevel.Debug;

            // Create game
            bool gameCreationCallCompleted = false;
            LootLockerTestGame.CreateGame(testName: this.GetType().Name + TestCounter + " ", onComplete: (success, errorMessage, game) =>
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

            bool enablePresenceCompleted = false;
            gameUnderTest?.EnablePresence(true, (success, errorMessage) =>
            {
                if (!success)
                {
                    Debug.LogError(errorMessage);
                    SetupFailed = true;
                }
                enablePresenceCompleted = true;
            });

            yield return new WaitUntil(() => enablePresenceCompleted);
            if (SetupFailed)
            {
                yield break;
            }

            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Successfully created test game and initialized LootLocker");
            int i = 0;
            yield return new WaitUntil(() => LootLockerSDKManager.CheckInitialized(true) || ++i > 20_000);

            Debug.Log($"##### Start of {this.GetType().Name} test no.{TestCounter} test case #####");
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Debug.Log($"##### Start of {this.GetType().Name} test no.{TestCounter} teardown #####");
            
            // Cleanup presence connections
            LootLockerSDKManager.SetPresenceEnabled(false);
            
            // End session if active
            bool sessionEnded = false;
            LootLockerSDKManager.EndSession((response) =>
            {
                sessionEnded = true;
            });

            yield return new WaitUntil(() => sessionEnded);
            LootLockerSDKManager.ResetSDK();
            yield return LootLockerLifecycleManager.CleanUpOldInstances();

            LootLockerConfig.CreateNewSettings(configCopy.apiKey, configCopy.game_version, configCopy.domainKey,
                configCopy.logLevel, configCopy.logInBuilds, configCopy.logErrorsAsWarnings, configCopy.allowTokenRefresh);
            
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} teardown #####");

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PresenceConnection_WithValidSessionAndPresenceEnabled_ConnectsSuccessfully()
        {
            if (SetupFailed)
            {
                yield break;
            }

            // Ensure presence is enabled
            LootLockerSDKManager.SetPresenceEnabled(true);
            LootLockerSDKManager.SetPresenceAutoConnectEnabled(false); // Manual control for testing

            // Start session
            bool sessionStarted = false;
            LootLockerGuestSessionResponse sessionResponse = null;

            LootLockerSDKManager.StartGuestSession((response) =>
            {
                sessionResponse = response;
                sessionStarted = true;
            });

            yield return new WaitUntil(() => sessionStarted);
            Assert.IsTrue(sessionResponse.success, $"Session should start successfully. Error: {sessionResponse.errorData?.message}");

            // Test presence connection
            bool presenceConnectCallCompleted = false;
            bool connectionSuccess = false;
            string connectionError = null;

            LootLockerSDKManager.ForceStartPresenceConnection((success, error) =>
            {
                connectionSuccess = success;
                connectionError = error;
                presenceConnectCallCompleted = true;
            });

            yield return new WaitUntil(() => presenceConnectCallCompleted);
            Assert.IsTrue(connectionSuccess, $"Presence connection should succeed. Error: {connectionError}");

            // Wait a moment for connection to stabilize
            yield return new WaitForSeconds(2f);

            // Verify connection state
            Assert.IsTrue(LootLockerSDKManager.IsPresenceConnected(), "Presence should be connected");

            // Verify client is tracked
            var activeClients = LootLockerSDKManager.ListPresenceConnections().ToList();
            Assert.IsTrue(activeClients.Count > 0, "Should have at least one active presence client");

            // Get connection stats
            var stats = LootLockerSDKManager.GetPresenceConnectionStats(null);
            Assert.IsNotNull(stats, "Connection stats should be available");
            Assert.GreaterOrEqual(stats.currentLatencyMs, 0, "Current latency should be non-negative");

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PresenceConnection_UpdateStatus_UpdatesSuccessfully()
        {
            if (SetupFailed)
            {
                yield break;
            }

            // Setup session and presence connection
            LootLockerSDKManager.SetPresenceEnabled(true);
            LootLockerSDKManager.SetPresenceAutoConnectEnabled(false);

            bool sessionStarted = false;
            LootLockerGuestSessionResponse sessionResponse = null;

            LootLockerSDKManager.StartGuestSession((response) =>
            {
                sessionResponse = response;
                sessionStarted = true;
            });

            yield return new WaitUntil(() => sessionStarted);
            Assert.IsTrue(sessionResponse.success, "Session should start successfully");

            // Connect presence
            bool presenceConnected = false;
            bool connectionSuccess = false;

            LootLockerSDKManager.ForceStartPresenceConnection((success, error) =>
            {
                connectionSuccess = success;
                presenceConnected = true;
            });

            yield return new WaitUntil(() => presenceConnected);
            Assert.IsTrue(connectionSuccess, "Presence should connect successfully");

            // Wait for connection to stabilize
            yield return new WaitForSeconds(2f);

            // Test status update
            bool statusUpdated = false;
            bool updateSuccess = false;
            const string testStatus = "testing_status";

            LootLockerSDKManager.UpdatePresenceStatus(testStatus, null, (success) =>
            {
                updateSuccess = success;
                statusUpdated = true;
            });

            yield return new WaitUntil(() => statusUpdated);
            Assert.IsTrue(updateSuccess, "Status update should succeed");

            // Verify the status was set via connection stats
            var statsAfterUpdate = LootLockerSDKManager.GetPresenceConnectionStats(null);
            Assert.IsNotNull(statsAfterUpdate, "Should be able to get stats after status update");
            Assert.AreEqual(testStatus, statsAfterUpdate.lastSentStatus, "Last sent status should match the test status");

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PresenceConnection_DisconnectPresence_DisconnectsCleanly()
        {
            if (SetupFailed)
            {
                yield break;
            }

            // Setup session and presence connection
            LootLockerSDKManager.SetPresenceEnabled(true);
            LootLockerSDKManager.SetPresenceAutoConnectEnabled(false);

            bool sessionStarted = false;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                sessionStarted = true;
            });

            yield return new WaitUntil(() => sessionStarted);

            // Connect presence
            bool presenceConnected = false;
            LootLockerSDKManager.ForceStartPresenceConnection((success, error) =>
            {
                presenceConnected = true;
            });

            yield return new WaitUntil(() => presenceConnected);
            yield return new WaitForSeconds(1f);

            // Verify connection
            Assert.IsTrue(LootLockerSDKManager.IsPresenceConnected(), "Should be connected before disconnect test");

            // Test disconnection
            bool presenceDisconnected = false;
            bool disconnectSuccess = false;
            string disconnectError = null;

            LootLockerSDKManager.ForceStopPresenceConnection((success, error) =>
            {
                disconnectSuccess = success;
                disconnectError = error;
                presenceDisconnected = true;
            });

            yield return new WaitUntil(() => presenceDisconnected);
            Assert.IsTrue(disconnectSuccess, $"Presence disconnection should succeed. Error: {disconnectError}");

            // Wait a moment for disconnection to process
            yield return new WaitForSeconds(1f);

            // Verify disconnection
            Assert.IsFalse(LootLockerSDKManager.IsPresenceConnected(), "Presence should be disconnected");

            // Verify no active clients
            var activeClients = LootLockerSDKManager.ListPresenceConnections().ToList();
            Assert.AreEqual(0, activeClients.Count, "Should have no active presence clients after disconnect");

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PresenceConnection_WithAutoConnect_ConnectsOnSessionStart()
        {
            if (SetupFailed)
            {
                yield break;
            }

            // Enable auto-connect
            LootLockerSDKManager.SetPresenceEnabled(true);
            LootLockerSDKManager.SetPresenceAutoConnectEnabled(true);

            // Start session (should auto-connect presence)
            bool sessionStarted = false;
            LootLockerGuestSessionResponse sessionResponse = null;

            LootLockerSDKManager.StartGuestSession((response) =>
            {
                sessionResponse = response;
                sessionStarted = true;
            });

            yield return new WaitUntil(() => sessionStarted);
            Assert.IsTrue(sessionResponse.success, "Session should start successfully");

            // Wait for auto-connect to complete
            yield return new WaitForSeconds(3f);

            // Verify presence auto-connected
            Assert.IsTrue(LootLockerSDKManager.IsPresenceConnected(), "Presence should auto-connect when enabled");

            var activeClients = LootLockerSDKManager.ListPresenceConnections().ToList();
            Assert.IsTrue(activeClients.Count > 0, "Should have active presence clients after auto-connect");

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PresenceConnection_WithoutSession_FailsGracefully()
        {
            if (SetupFailed)
            {
                yield break;
            }

            // Ensure no active session
            bool sessionEnded = false;
            LootLockerSDKManager.EndSession((response) =>
            {
                sessionEnded = true;
            });
            yield return new WaitUntil(() => sessionEnded);

            // Try to connect presence without session
            bool presenceAttempted = false;
            bool connectionSuccess = false;
            string connectionError = null;

            LootLockerSDKManager.ForceStartPresenceConnection((success, error) =>
            {
                connectionSuccess = success;
                connectionError = error;
                presenceAttempted = true;
            });

            yield return new WaitUntil(() => presenceAttempted);
            Assert.IsFalse(connectionSuccess, "Presence connection should fail without valid session");
            Assert.IsNotNull(connectionError, "Should have error message when connection fails");
            Assert.IsFalse(LootLockerSDKManager.IsPresenceConnected(), "Presence should not be connected");

            yield return null;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PresenceConnection_WhenDisabled_DoesNotConnect()
        {
            if (SetupFailed)
            {
                yield break;
            }

            // Disable presence system
            LootLockerSDKManager.SetPresenceEnabled(false);

            // Start session
            bool sessionStarted = false;
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                sessionStarted = true;
            });

            yield return new WaitUntil(() => sessionStarted);

            // Try to connect presence while disabled
            bool presenceAttempted = false;
            bool connectionSuccess = false;
            string connectionError = null;

            LootLockerSDKManager.ForceStartPresenceConnection((success, error) =>
            {
                connectionSuccess = success;
                connectionError = error;
                presenceAttempted = true;
            });

            yield return new WaitUntil(() => presenceAttempted);
            Assert.IsFalse(connectionSuccess, "Presence connection should fail when system is disabled");
            Assert.IsNotNull(connectionError, "Should have error message explaining system is disabled");
            Assert.IsFalse(LootLockerSDKManager.IsPresenceConnected(), "Presence should not be connected when disabled");

            yield return null;
        }
    }
}
