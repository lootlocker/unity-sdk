
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class PlayerFilesTest
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
            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Successfully created test game and initialized LootLocker");

            // Sign in client
            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                SetupFailed |= !response.success;
                guestLoginCompleted = true;
            });
            yield return new WaitUntil(() => guestLoginCompleted);

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

            LootLockerConfig.CreateNewSettings(configCopy);
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} tear down #####");
        }

        // --- Helpers ---

        private string CreateTempFile(string content, string suffix = ".txt")
        {
            string path = Application.temporaryCachePath + $"/{this.GetType().Name}{TestCounter}-{Guid.NewGuid()}{suffix}";
            using (TextWriter writer = new StreamWriter(path))
            {
                writer.WriteLine(content);
            }
            return path;
        }

        // Revision created_at has second granularity and is stamped by two different clocks
        // (the app server for uploads, MySQL for updates), so two revisions created close
        // together can share a timestamp. Wait for a full elapsed second so the revisions are
        // guaranteed to be distinguishable. Comparing only the Second component is not enough:
        // called at ...999ms it would return almost immediately and both revisions could still
        // land in the same second.
        private static IEnumerator WaitForNextSecond()
        {
            DateTime start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalSeconds < 1.0)
            {
                yield return null;
            }
        }

        // ================================================================
        // Phase 1: Core Upload & Key Tests
        // ================================================================

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator PlayerFiles_UploadSimplePublicFile_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string path = CreateTempFile("First added line");

            // When
            LootLockerPlayerFile actualResponse = new LootLockerPlayerFile();
            bool setToPublic = true;
            bool playerFileUploadCompleted = false;
            LootLockerSDKManager.UploadPlayerFile(path, "test", setToPublic, fileResponse =>
            {
                actualResponse = fileResponse;
                playerFileUploadCompleted = true;
            });

            yield return new WaitUntil(() => playerFileUploadCompleted);

            // Then
            Assert.IsTrue(actualResponse.success, "File upload failed");
            Assert.Greater(actualResponse.size, 0, "File Size was 0");
            Assert.AreEqual(setToPublic, actualResponse.is_public, "File does not have the same public setting");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_UploadWithKey_ReturnsKeyInResponse()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string path = CreateTempFile("Content with key");
            string fileKey = "test-key-" + TestCounter;

            // When
            LootLockerPlayerFile actualResponse = new LootLockerPlayerFile();
            bool completed = false;
            LootLockerSDKManager.UploadPlayerFileByKey(path, "test", true, fileKey, fileResponse =>
            {
                actualResponse = fileResponse;
                completed = true;
            });

            yield return new WaitUntil(() => completed);

            // Then
            Assert.IsTrue(actualResponse.success, "File upload with key failed");
            Assert.AreEqual(fileKey, actualResponse.key, "Key in response does not match");
            Assert.Greater(actualResponse.size, 0, "File Size was 0");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_UploadWithSameKeyTwice_UpdatesExistingFile()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string fileKey = "upsert-key-" + TestCounter;
            string pathA = CreateTempFile("Original content");
            string pathB = CreateTempFile("Updated content that is longer");

            // When — first upload
            LootLockerPlayerFile firstResponse = new LootLockerPlayerFile();
            bool firstDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathA, "test", true, fileKey, fileResponse =>
            {
                firstResponse = fileResponse;
                firstDone = true;
            });
            yield return new WaitUntil(() => firstDone);
            Assert.IsTrue(firstResponse.success, "First upload failed");

            // When — second upload with same key
            LootLockerPlayerFile secondResponse = new LootLockerPlayerFile();
            bool secondDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathB, "test", true, fileKey, fileResponse =>
            {
                secondResponse = fileResponse;
                secondDone = true;
            });
            yield return new WaitUntil(() => secondDone);

            // Then
            Assert.IsTrue(secondResponse.success, "Second upload (upsert) failed");
            Assert.AreEqual(firstResponse.id, secondResponse.id, "File ID should be the same after upsert");
            Assert.AreNotEqual(firstResponse.size, secondResponse.size, "File size should differ after upsert with different content");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_UploadWithoutKey_ReturnsEmptyKey()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string path = CreateTempFile("No key content");

            // When
            LootLockerPlayerFile actualResponse = new LootLockerPlayerFile();
            bool completed = false;
            LootLockerSDKManager.UploadPlayerFile(path, "test", true, fileResponse =>
            {
                actualResponse = fileResponse;
                completed = true;
            });

            yield return new WaitUntil(() => completed);

            // Then
            Assert.IsTrue(actualResponse.success, "File upload without key failed");
            Assert.IsTrue(string.IsNullOrEmpty(actualResponse.key), "Key should be null or empty when not provided");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_UploadPrivateFile_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string path = CreateTempFile("Private content");

            // When
            LootLockerPlayerFile actualResponse = new LootLockerPlayerFile();
            bool completed = false;
            LootLockerSDKManager.UploadPlayerFile(path, "test", false, fileResponse =>
            {
                actualResponse = fileResponse;
                completed = true;
            });

            yield return new WaitUntil(() => completed);

            // Then
            Assert.IsTrue(actualResponse.success, "Private file upload failed");
            Assert.IsFalse(actualResponse.is_public, "File should not be public");
        }

        // ================================================================
        // Phase 2: Key-Based Lookup & Delete
        // ================================================================

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_GetFileByKey_ReturnsCorrectFile()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string fileKey = "lookup-key-" + TestCounter;
            string path = CreateTempFile("Lookup by key content");
            LootLockerPlayerFile uploadedFile = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(path, "test", true, fileKey, fileResponse =>
            {
                uploadedFile = fileResponse;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(uploadedFile.success, "Upload for lookup test failed");

            // When
            LootLockerPlayerFile fetchedFile = new LootLockerPlayerFile();
            bool fetchDone = false;
            LootLockerSDKManager.GetPlayerFileByKey(fileKey, fileResponse =>
            {
                fetchedFile = fileResponse;
                fetchDone = true;
            });
            yield return new WaitUntil(() => fetchDone);

            // Then
            Assert.IsTrue(fetchedFile.success, "GetPlayerFileByKey failed");
            Assert.AreEqual(uploadedFile.id, fetchedFile.id, "File ID should match");
            Assert.AreEqual(fileKey, fetchedFile.key, "Key should match");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_GetFileByKey_NonExistentKey_Fails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // When
            bool preLogErrorsAsWarningsSetting = LootLockerConfig.current.logErrorsAsWarnings;
            LootLockerConfig.current.logErrorsAsWarnings = true; // Suppress error logs for expected failure
            LootLockerPlayerFile fetchedFile = new LootLockerPlayerFile();
            bool fetchDone = false;
            LootLockerSDKManager.GetPlayerFileByKey("nonexistent-key-" + TestCounter, fileResponse =>
            {
                fetchedFile = fileResponse;
                fetchDone = true;
            });
            yield return new WaitUntil(() => fetchDone);
            LootLockerConfig.current.logErrorsAsWarnings = preLogErrorsAsWarningsSetting;

            // Then
            Assert.IsFalse(fetchedFile.success, "GetPlayerFileByKey should fail for non-existent key");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_DeleteFileByKey_RemovesFile()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string fileKey = "delete-key-" + TestCounter;
            string path = CreateTempFile("To be deleted by key");
            LootLockerPlayerFile uploadedFile = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(path, "test", true, fileKey, fileResponse =>
            {
                uploadedFile = fileResponse;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(uploadedFile.success, "Upload for delete-by-key test failed");

            // When — delete by key
            LootLockerResponse deleteResponse = new LootLockerResponse();
            bool deleteDone = false;
            LootLockerSDKManager.DeletePlayerFileByKey(fileKey, response =>
            {
                deleteResponse = response;
                deleteDone = true;
            });
            yield return new WaitUntil(() => deleteDone);

            // Then — verify deletion
            Assert.IsTrue(deleteResponse.success, "DeletePlayerFileByKey failed");

            bool preLogErrorsAsWarningsSetting = LootLockerConfig.current.logErrorsAsWarnings;
            LootLockerConfig.current.logErrorsAsWarnings = true; // Suppress error logs for expected failure
            LootLockerPlayerFile fetchedFileResponse = new LootLockerPlayerFile();
            bool fetchDone = false;
            LootLockerSDKManager.GetPlayerFileByKey(fileKey, fileResponse =>
            {
                fetchedFileResponse = fileResponse;
                fetchDone = true;
            });
            yield return new WaitUntil(() => fetchDone);
            Assert.IsFalse(fetchedFileResponse.success, "File should no longer exist after deletion by key");
            LootLockerConfig.current.logErrorsAsWarnings = preLogErrorsAsWarningsSetting;
        }

        // ================================================================
        // Phase 3: Revisions by ID
        // ================================================================

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_UpdateFile_CreatesNewRevision()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string pathA = CreateTempFile("Original revision content");
            string pathB = CreateTempFile("Updated revision content");

            LootLockerPlayerFile uploadedFile = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFile(pathA, "test", true, fileResponse =>
            {
                uploadedFile = fileResponse;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(uploadedFile.success, "Initial upload failed");

            yield return WaitForNextSecond();

            // When — update the file
            LootLockerPlayerFile updatedFile = new LootLockerPlayerFile();
            bool updateDone = false;
            LootLockerSDKManager.UpdatePlayerFile(uploadedFile.id, pathB, fileResponse =>
            {
                updatedFile = fileResponse;
                updateDone = true;
            });
            yield return new WaitUntil(() => updateDone);
            Assert.IsTrue(updatedFile.success, "Update failed");

            // Then — list revisions
            LootLockerPlayerFileRevisionsResponse revisionsResponse = new LootLockerPlayerFileRevisionsResponse();
            bool revisionsDone = false;
            LootLockerSDKManager.GetPlayerFileRevisions(uploadedFile.id, response =>
            {
                revisionsResponse = response;
                revisionsDone = true;
            });
            yield return new WaitUntil(() => revisionsDone);

            Assert.IsTrue(revisionsResponse.success, "List revisions failed");
            Assert.GreaterOrEqual(revisionsResponse.revisions.Length, 2, "Should have at least 2 revisions after update");
            Assert.IsNotNull(revisionsResponse.current_revision_id, "Current revision ID should be set");
            // Revisions are returned newest-first (created_at DESC), so the current revision is the first element.
            Assert.AreEqual(revisionsResponse.current_revision_id, revisionsResponse.revisions[0].id,
                "Current revision should be the latest");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_GetFileRevision_ReturnsSpecificRevision()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string pathA = CreateTempFile("First revision");
            string pathB = CreateTempFile("Second revision");

            LootLockerPlayerFile uploadedFile = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFile(pathA, "test", true, fileResponse =>
            {
                uploadedFile = fileResponse;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(uploadedFile.success, "Initial upload failed");

            yield return WaitForNextSecond();

            // Update to create a second revision
            LootLockerPlayerFile updateResponse = new LootLockerPlayerFile();
            bool updateDone = false;
            LootLockerSDKManager.UpdatePlayerFile(uploadedFile.id, pathB, response =>
            {
                updateResponse = response;
                updateDone = true;
            });
            yield return new WaitUntil(() => updateDone);
            Assert.IsTrue(updateResponse.success, "Update to create revision 2 failed");

            // Get revision list to find the first revision ID
            LootLockerPlayerFileRevisionsResponse revisionsResponse = new LootLockerPlayerFileRevisionsResponse();
            bool revisionsDone = false;
            LootLockerSDKManager.GetPlayerFileRevisions(uploadedFile.id, response =>
            {
                revisionsResponse = response;
                revisionsDone = true;
            });
            yield return new WaitUntil(() => revisionsDone);
            Assert.IsTrue(revisionsResponse.success, "List revisions failed");
            Assert.GreaterOrEqual(revisionsResponse.revisions.Length, 2, "Should have at least 2 revisions");

            // When — get a specific revision (revisions are newest-first, so index 0 is the current one)
            string firstRevisionId = revisionsResponse.revisions[0].id;
            LootLockerPlayerFileContent revisionContent = new LootLockerPlayerFileContent();
            bool getRevisionDone = false;
            LootLockerSDKManager.GetPlayerFileRevision(uploadedFile.id, firstRevisionId, response =>
            {
                revisionContent = response;
                getRevisionDone = true;
            });
            yield return new WaitUntil(() => getRevisionDone);

            // Then
            Assert.IsTrue(revisionContent.success, "GetPlayerFileRevision failed");
            Assert.AreEqual(firstRevisionId, revisionContent.id, "Revision ID should match");
            Assert.Greater(revisionContent.size, 0, "Revision size should be > 0");
            // Note: the URL is only populated when a CDN/file storage backend is configured
            // (e.g. production). In local CI it may be empty, so we don't assert on it here.
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_PromoteFileRevision_RestoresOldRevision()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string pathA = CreateTempFile("First revision content");
            string pathB = CreateTempFile("Second revision content");

            LootLockerPlayerFile uploadedFile = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFile(pathA, "test", true, fileResponse =>
            {
                uploadedFile = fileResponse;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(uploadedFile.success, "Initial upload failed");

            yield return WaitForNextSecond();

            // Update to create revision 2
            LootLockerPlayerFile updateResponse = new LootLockerPlayerFile();
            bool updateDone = false;
            LootLockerSDKManager.UpdatePlayerFile(uploadedFile.id, pathB, response =>
            {
                updateResponse = response;
                updateDone = true;
            });
            yield return new WaitUntil(() => updateDone);
            Assert.IsTrue(updateResponse.success, "Update to create revision 2 failed");

            // Get revision list to find the first revision ID
            LootLockerPlayerFileRevisionsResponse revisionsResponse = new LootLockerPlayerFileRevisionsResponse();
            bool revisionsDone = false;
            LootLockerSDKManager.GetPlayerFileRevisions(uploadedFile.id, response =>
            {
                revisionsResponse = response;
                revisionsDone = true;
            });
            yield return new WaitUntil(() => revisionsDone);
            Assert.IsTrue(revisionsResponse.success, "List revisions failed");
            // Revisions are returned newest-first (created_at DESC), so the oldest
            // (original) revision is the last element.
            string firstRevisionId = revisionsResponse.revisions[revisionsResponse.revisions.Length - 1].id;

            // When — promote the first revision back to current
            LootLockerResponse promoteResponse = new LootLockerResponse();
            bool promoteDone = false;
            LootLockerSDKManager.PromotePlayerFileRevision(uploadedFile.id, firstRevisionId, response =>
            {
                promoteResponse = response;
                promoteDone = true;
            });
            yield return new WaitUntil(() => promoteDone);

            // Then
            Assert.IsTrue(promoteResponse.success, "Promote revision failed");

            // Verify the current revision changed
            LootLockerPlayerFile refreshedFile = new LootLockerPlayerFile();
            bool refreshDone = false;
            LootLockerSDKManager.GetPlayerFile(uploadedFile.id, fileResponse =>
            {
                refreshedFile = fileResponse;
                refreshDone = true;
            });
            yield return new WaitUntil(() => refreshDone);
            Assert.IsTrue(refreshedFile.success, "GetPlayerFile after promote failed");
            Assert.AreEqual(firstRevisionId, refreshedFile.revision_id, "Current revision should be the promoted one");
        }

        // ================================================================
        // Phase 4: Revisions by Key
        // ================================================================

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_GetFileRevisionsByKey_ReturnsRevisions()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string fileKey = "rev-key-" + TestCounter;
            string pathA = CreateTempFile("Revision A by key");
            string pathB = CreateTempFile("Revision B by key");

            // Upload with key (creates revision 1)
            LootLockerPlayerFile firstUpload = new LootLockerPlayerFile();
            bool firstDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathA, "test", true, fileKey, response =>
            {
                firstUpload = response;
                firstDone = true;
            });
            yield return new WaitUntil(() => firstDone);
            Assert.IsTrue(firstUpload.success, "First upload for revisions by key test failed");

            // Upsert with same key (creates revision 2)
            LootLockerPlayerFile secondUpload = new LootLockerPlayerFile();
            bool secondDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathB, "test", true, fileKey, response =>
            {
                secondUpload = response;
                secondDone = true;
            });
            yield return new WaitUntil(() => secondDone);
            Assert.IsTrue(secondUpload.success, "Second upload for revisions by key test failed");

            // When
            LootLockerPlayerFileRevisionsResponse revisionsResponse = new LootLockerPlayerFileRevisionsResponse();
            bool revisionsDone = false;
            LootLockerSDKManager.GetPlayerFileRevisionsByKey(fileKey, response =>
            {
                revisionsResponse = response;
                revisionsDone = true;
            });
            yield return new WaitUntil(() => revisionsDone);

            // Then
            Assert.IsTrue(revisionsResponse.success, "GetPlayerFileRevisionsByKey failed");
            Assert.GreaterOrEqual(revisionsResponse.revisions.Length, 2, "Should have at least 2 revisions");
            Assert.AreEqual(fileKey, revisionsResponse.file.key, "File metadata key should match");
            Assert.IsNotNull(revisionsResponse.current_revision_id, "Current revision ID should be set");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_GetFileRevisionByKey_ReturnsSpecificRevision()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string fileKey = "rev-get-key-" + TestCounter;
            string pathA = CreateTempFile("First revision by key");
            string pathB = CreateTempFile("Second revision by key");

            LootLockerPlayerFile firstUpload = new LootLockerPlayerFile();
            bool firstDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathA, "test", true, fileKey, response =>
            {
                firstUpload = response;
                firstDone = true;
            });
            yield return new WaitUntil(() => firstDone);
            Assert.IsTrue(firstUpload.success, "First upload for get revision by key test failed");

            yield return WaitForNextSecond();

            LootLockerPlayerFile secondUpload = new LootLockerPlayerFile();
            bool secondDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathB, "test", true, fileKey, response =>
            {
                secondUpload = response;
                secondDone = true;
            });
            yield return new WaitUntil(() => secondDone);
            Assert.IsTrue(secondUpload.success, "Second upload for get revision by key test failed");

            // Get revision list to find a revision ID
            LootLockerPlayerFileRevisionsResponse revisionsResponse = new LootLockerPlayerFileRevisionsResponse();
            bool revisionsDone = false;
            LootLockerSDKManager.GetPlayerFileRevisionsByKey(fileKey, response =>
            {
                revisionsResponse = response;
                revisionsDone = true;
            });
            yield return new WaitUntil(() => revisionsDone);
            Assert.IsTrue(revisionsResponse.success, "List revisions by key failed");
            string firstRevisionId = revisionsResponse.revisions[0].id;

            // When
            LootLockerPlayerFileContent revisionContent = new LootLockerPlayerFileContent();
            bool getRevisionDone = false;
            LootLockerSDKManager.GetPlayerFileRevisionByKey(fileKey, firstRevisionId, response =>
            {
                revisionContent = response;
                getRevisionDone = true;
            });
            yield return new WaitUntil(() => getRevisionDone);

            // Then
            Assert.IsTrue(revisionContent.success, "GetPlayerFileRevisionByKey failed");
            Assert.AreEqual(firstRevisionId, revisionContent.id, "Revision ID should match");
            Assert.Greater(revisionContent.size, 0, "Revision size should be > 0");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_PromoteFileRevisionByKey_PromotesRevision()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string fileKey = "promote-key-" + TestCounter;
            string pathA = CreateTempFile("First revision for promote by key");
            string pathB = CreateTempFile("Second revision for promote by key");

            LootLockerPlayerFile firstUpload = new LootLockerPlayerFile();
            bool firstDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathA, "test", true, fileKey, response =>
            {
                firstUpload = response;
                firstDone = true;
            });
            yield return new WaitUntil(() => firstDone);
            Assert.IsTrue(firstUpload.success, "First upload for promote by key test failed");

            yield return WaitForNextSecond();

            LootLockerPlayerFile secondUpload = new LootLockerPlayerFile();
            bool secondDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathB, "test", true, fileKey, response =>
            {
                secondUpload = response;
                secondDone = true;
            });
            yield return new WaitUntil(() => secondDone);
            Assert.IsTrue(secondUpload.success, "Second upload for promote by key test failed");

            // Get revision list to find the first revision ID
            LootLockerPlayerFileRevisionsResponse revisionsResponse = new LootLockerPlayerFileRevisionsResponse();
            bool revisionsDone = false;
            LootLockerSDKManager.GetPlayerFileRevisionsByKey(fileKey, response =>
            {
                revisionsResponse = response;
                revisionsDone = true;
            });
            yield return new WaitUntil(() => revisionsDone);
            Assert.IsTrue(revisionsResponse.success, "List revisions by key failed");
            // Revisions are returned newest-first (created_at DESC), so the oldest
            // (original) revision is the last element.
            string firstRevisionId = revisionsResponse.revisions[revisionsResponse.revisions.Length - 1].id;

            // When — promote the first revision
            LootLockerResponse promoteResponse = new LootLockerResponse();
            bool promoteDone = false;
            LootLockerSDKManager.PromotePlayerFileRevisionByKey(fileKey, firstRevisionId, response =>
            {
                promoteResponse = response;
                promoteDone = true;
            });
            yield return new WaitUntil(() => promoteDone);

            // Then
            Assert.IsTrue(promoteResponse.success, "PromotePlayerFileRevisionByKey failed");

            // Verify the current revision changed
            LootLockerPlayerFile refreshedFile = new LootLockerPlayerFile();
            bool refreshDone = false;
            LootLockerSDKManager.GetPlayerFileByKey(fileKey, fileResponse =>
            {
                refreshedFile = fileResponse;
                refreshDone = true;
            });
            yield return new WaitUntil(() => refreshDone);
            Assert.IsTrue(refreshedFile.success, "GetPlayerFileByKey after promote failed");
            Assert.AreEqual(firstRevisionId, refreshedFile.revision_id, "Current revision should be the promoted one");
        }

        // ================================================================
        // Phase 5: Existing Operations Backfill
        // ================================================================

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator PlayerFiles_GetPlayerFile_ReturnsCorrectFile()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string path = CreateTempFile("Get by ID content");
            LootLockerPlayerFile uploadedFile = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFile(path, "test", true, fileResponse =>
            {
                uploadedFile = fileResponse;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(uploadedFile.success, "Upload for get test failed");

            // When
            LootLockerPlayerFile fetchedFile = new LootLockerPlayerFile();
            bool fetchDone = false;
            LootLockerSDKManager.GetPlayerFile(uploadedFile.id, fileResponse =>
            {
                fetchedFile = fileResponse;
                fetchDone = true;
            });
            yield return new WaitUntil(() => fetchDone);

            // Then
            Assert.IsTrue(fetchedFile.success, "GetPlayerFile failed");
            Assert.AreEqual(uploadedFile.id, fetchedFile.id, "File ID should match");
            Assert.AreEqual(uploadedFile.name, fetchedFile.name, "File name should match");
            Assert.Greater(fetchedFile.size, 0, "File size should be > 0");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_GetAllPlayerFiles_ReturnsFiles()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given — upload two files
            string pathA = CreateTempFile("First list file");
            string pathB = CreateTempFile("Second list file");

            LootLockerPlayerFile uploadA = new LootLockerPlayerFile();
            bool uploadADone = false;
            LootLockerSDKManager.UploadPlayerFile(pathA, "test", true, response =>
            {
                uploadA = response;
                uploadADone = true;
            });
            yield return new WaitUntil(() => uploadADone);
            Assert.IsTrue(uploadA.success, "First upload for list test failed");

            LootLockerPlayerFile uploadB = new LootLockerPlayerFile();
            bool uploadBDone = false;
            LootLockerSDKManager.UploadPlayerFile(pathB, "test", true, response =>
            {
                uploadB = response;
                uploadBDone = true;
            });
            yield return new WaitUntil(() => uploadBDone);
            Assert.IsTrue(uploadB.success, "Second upload for list test failed");

            // When
            LootLockerPlayerFilesResponse listResponse = new LootLockerPlayerFilesResponse();
            bool listDone = false;
            LootLockerSDKManager.GetAllPlayerFiles(response =>
            {
                listResponse = response;
                listDone = true;
            });
            yield return new WaitUntil(() => listDone);

            // Then
            Assert.IsTrue(listResponse.success, "GetAllPlayerFiles failed");
            Assert.GreaterOrEqual(listResponse.items.Length, 2, "Should have at least 2 files");
            foreach (var item in listResponse.items)
            {
                Assert.Greater(item.id, 0, "Each file should have a positive ID");
                Assert.IsFalse(string.IsNullOrEmpty(item.name), "Each file should have a name");
                // Note: the URL is only populated when a CDN/file storage backend is configured
                // (e.g. production). In local CI it may be empty, so we don't assert on it here.
            }
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_DeletePlayerFile_RemovesFile()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string path = CreateTempFile("To be deleted");
            LootLockerPlayerFile uploadedFile = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFile(path, "test", true, fileResponse =>
            {
                uploadedFile = fileResponse;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(uploadedFile.success, "Upload for delete test failed");

            // When
            LootLockerResponse deleteResponse = new LootLockerResponse();
            bool deleteDone = false;
            LootLockerSDKManager.DeletePlayerFile(uploadedFile.id, response =>
            {
                deleteResponse = response;
                deleteDone = true;
            });
            yield return new WaitUntil(() => deleteDone);

            // Then
            Assert.IsTrue(deleteResponse.success, "DeletePlayerFile failed");

            // Verify deletion
            bool preLogErrorsAsWarningsSetting = LootLockerConfig.current.logErrorsAsWarnings;
            LootLockerConfig.current.logErrorsAsWarnings = true; // Suppress error logs for expected failure
            LootLockerPlayerFile fetchedFile = new LootLockerPlayerFile();
            bool fetchDone = false;
            LootLockerSDKManager.GetPlayerFile(uploadedFile.id, fileResponse =>
            {
                fetchedFile = fileResponse;
                fetchDone = true;
            });
            yield return new WaitUntil(() => fetchDone);
            Assert.IsFalse(fetchedFile.success, "File should no longer exist after deletion");
            LootLockerConfig.current.logErrorsAsWarnings = preLogErrorsAsWarningsSetting;
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_UpdatePlayerFile_ChangesContent()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string pathA = CreateTempFile("Original content for update");
            string pathB = CreateTempFile("Updated content for update");

            LootLockerPlayerFile uploadedFile = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFile(pathA, "test", true, fileResponse =>
            {
                uploadedFile = fileResponse;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(uploadedFile.success, "Initial upload failed");
            int originalSize = uploadedFile.size;
            string originalRevisionId = uploadedFile.revision_id;

            // When
            LootLockerPlayerFile updatedFile = new LootLockerPlayerFile();
            bool updateDone = false;
            LootLockerSDKManager.UpdatePlayerFile(uploadedFile.id, pathB, fileResponse =>
            {
                updatedFile = fileResponse;
                updateDone = true;
            });
            yield return new WaitUntil(() => updateDone);

            // Then
            Assert.IsTrue(updatedFile.success, "UpdatePlayerFile failed");
            Assert.AreNotEqual(originalRevisionId, updatedFile.revision_id, "Revision ID should change after update");
            Assert.AreNotEqual(originalSize, updatedFile.size, "File size should change after update with different content");
        }

        // ================================================================
        // Phase 6: Response Field Verification
        // ================================================================

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_ListResponse_IncludesKeyField()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string fileKey = "list-key-" + TestCounter;
            string path = CreateTempFile("List response key check");

            LootLockerPlayerFile uploadedFile = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(path, "test", true, fileKey, response =>
            {
                uploadedFile = response;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(uploadedFile.success, "Upload for list response key test failed");

            // When
            LootLockerPlayerFilesResponse listResponse = new LootLockerPlayerFilesResponse();
            bool listDone = false;
            LootLockerSDKManager.GetAllPlayerFiles(response =>
            {
                listResponse = response;
                listDone = true;
            });
            yield return new WaitUntil(() => listDone);

            // Then
            Assert.IsTrue(listResponse.success, "GetAllPlayerFiles failed");
            bool foundKey = false;
            foreach (var item in listResponse.items)
            {
                if (item.key == fileKey)
                {
                    foundKey = true;
                    break;
                }
            }
            Assert.IsTrue(foundKey, "List response should contain an item with the uploaded key");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_RevisionsResponse_FileMetadataHasKey()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string fileKey = "meta-key-" + TestCounter;
            string pathA = CreateTempFile("Metadata key revision A");
            string pathB = CreateTempFile("Metadata key revision B");

            LootLockerPlayerFile firstUpload = new LootLockerPlayerFile();
            bool firstDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathA, "test", true, fileKey, response =>
            {
                firstUpload = response;
                firstDone = true;
            });
            yield return new WaitUntil(() => firstDone);
            Assert.IsTrue(firstUpload.success, "First upload for metadata key test failed");

            LootLockerPlayerFile secondUpload = new LootLockerPlayerFile();
            bool secondDone = false;
            LootLockerSDKManager.UploadPlayerFileByKey(pathB, "test", true, fileKey, response =>
            {
                secondUpload = response;
                secondDone = true;
            });
            yield return new WaitUntil(() => secondDone);
            Assert.IsTrue(secondUpload.success, "Second upload for metadata key test failed");

            // Get file ID for the ID-based revisions call
            LootLockerPlayerFile fetchedFile = new LootLockerPlayerFile();
            bool fetchDone = false;
            LootLockerSDKManager.GetPlayerFileByKey(fileKey, fileResponse =>
            {
                fetchedFile = fileResponse;
                fetchDone = true;
            });
            yield return new WaitUntil(() => fetchDone);
            Assert.IsTrue(fetchedFile.success, "GetPlayerFileByKey failed");

            // When — get revisions by ID
            LootLockerPlayerFileRevisionsResponse revisionsResponse = new LootLockerPlayerFileRevisionsResponse();
            bool revisionsDone = false;
            LootLockerSDKManager.GetPlayerFileRevisions(fetchedFile.id, response =>
            {
                revisionsResponse = response;
                revisionsDone = true;
            });
            yield return new WaitUntil(() => revisionsDone);

            // Then
            Assert.IsTrue(revisionsResponse.success, "GetPlayerFileRevisions failed");
            Assert.AreEqual(fileKey, revisionsResponse.file.key, "File metadata should contain the key");
            Assert.AreEqual(fetchedFile.id, revisionsResponse.file.id, "File metadata ID should match");
            Assert.IsFalse(string.IsNullOrEmpty(revisionsResponse.file.name), "File metadata should have a name");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_CreateDownloadLink_ReturnsUrl()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            string path = CreateTempFile("Download link content");

            LootLockerPlayerFile upload = new LootLockerPlayerFile();
            bool uploadDone = false;
            LootLockerSDKManager.UploadPlayerFile(path, "test", false, response =>
            {
                upload = response;
                uploadDone = true;
            });
            yield return new WaitUntil(() => uploadDone);
            Assert.IsTrue(upload.success, "File upload failed");

            // When
            LootLockerPlayerFile linkResponse = new LootLockerPlayerFile();
            bool linkDone = false;
            LootLockerSDKManager.CreatePlayerFileDownloadLink(upload.id, response =>
            {
                linkResponse = response;
                linkDone = true;
            });
            yield return new WaitUntil(() => linkDone);

            // Then
            Assert.IsTrue(linkResponse.success, "CreatePlayerFileDownloadLink failed");
            Assert.AreEqual(upload.id, linkResponse.id, "Download link response should describe the requested file");
            // The local file store used by CI is a no-op signer, so it returns an empty url. Only
            // assert that a url was minted when the backend actually signs (non-local storage).
            if (!string.IsNullOrEmpty(linkResponse.url))
            {
                Assert.IsFalse(linkResponse.requires_download_link, "A freshly minted link should not require another download link");
            }
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI")]
        public IEnumerator PlayerFiles_CreateDownloadLink_NonExistentFile_Fails()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");
            // Given
            int nonExistentFileId = 999999999;

            // When
            LootLockerPlayerFile linkResponse = new LootLockerPlayerFile();
            bool linkDone = false;
            // A 404 is the expected outcome, so the SDK's error log must be expected rather than
            // treated as an unhandled log message.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("file not found"));
            LootLockerSDKManager.CreatePlayerFileDownloadLink(nonExistentFileId, response =>
            {
                linkResponse = response;
                linkDone = true;
            });
            yield return new WaitUntil(() => linkDone);
            // Then
            Assert.IsFalse(linkResponse.success, "Creating a download link for a non-existent file should fail");
            Assert.AreEqual(404, linkResponse.statusCode, "Expected a 404 for a non-existent file");
        }

        // A gated file (such as a private file reward) is returned with requires_download_link set and no url.
        // There is no admin endpoint that creates one, so the response shape is verified by deserializing the
        // documented payloads for all three models that carry the flag.
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void PlayerFiles_GatedFileResponse_DeserializesRequiresDownloadLink()
        {
            // Given - a gated file as returned by list/single-file endpoints
            const string gatedFileJson = "{\"id\":42,\"revision_id\":\"01J8Z0X9K4\",\"name\":\"reward.txt\"," +
                "\"key\":\"reward-key\",\"size\":128,\"purpose\":\"reward\",\"public\":false," +
                "\"url\":\"\",\"url_expires_at\":\"0001-01-01T00:00:00+00:00\"," +
                "\"requires_download_link\":true,\"created_at\":\"2026-01-02T03:04:05+00:00\"}";

            // When
            LootLockerPlayerFile file = LootLockerJson.DeserializeObject<LootLockerPlayerFile>(gatedFileJson);

            // Then
            Assert.IsNotNull(file, "Gated file response should deserialize");
            Assert.AreEqual(42, file.id, "File id should deserialize");
            Assert.IsTrue(file.requires_download_link, "requires_download_link should deserialize as true for a gated file");
            Assert.IsTrue(string.IsNullOrEmpty(file.url), "A gated file should not carry a download url");

            // Given - the metadata block of a revisions response for the same gated file
            const string gatedMetadataJson = "{\"created_at\":\"2026-01-02T03:04:05+00:00\",\"name\":\"reward.txt\"," +
                "\"key\":\"reward-key\",\"purpose\":\"reward\",\"id\":42,\"public\":false,\"requires_download_link\":true}";

            // When
            LootLockerPlayerFileMetadata metadata = LootLockerJson.DeserializeObject<LootLockerPlayerFileMetadata>(gatedMetadataJson);

            // Then
            Assert.IsNotNull(metadata, "Gated file metadata should deserialize");
            Assert.AreEqual(42, metadata.id, "Metadata id should deserialize");
            Assert.IsTrue(metadata.requires_download_link, "requires_download_link should deserialize as true on metadata");

            // Given - a single revision of the same gated file
            const string gatedRevisionJson = "{\"id\":\"01J8Z0X9K4\",\"url\":\"\",\"size\":128," +
                "\"created_at\":\"2026-01-02T03:04:05+00:00\",\"requires_download_link\":true}";

            // When
            LootLockerPlayerFileContent revision = LootLockerJson.DeserializeObject<LootLockerPlayerFileContent>(gatedRevisionJson);

            // Then
            Assert.IsNotNull(revision, "Gated file revision should deserialize");
            Assert.AreEqual("01J8Z0X9K4", revision.id, "Revision id should deserialize");
            Assert.IsTrue(revision.requires_download_link, "requires_download_link should deserialize as true on a revision");
            Assert.IsTrue(string.IsNullOrEmpty(revision.url), "A gated revision should not carry a download url");
        }

        // Ordinary file responses carry requires_download_link as false. Keying redaction on the
        // presence of that field would redact usable public URLs, so only a true marker counts.
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void PlayerFiles_RedactSignedDownloadUrls_LeavesOrdinaryUrlsIntact()
        {
            // Given - an ordinary (non-gated) file response
            const string ordinaryFileJson = "{\"id\":42,\"name\":\"public.txt\",\"public\":true," +
                "\"url\":\"https://cdn.example.com/public.txt\",\"requires_download_link\":false}";

            // When
            string redacted = LootLockerObfuscator.RedactSignedDownloadUrlsForLogging(ordinaryFileJson);

            // Then - the usable public url survives
            StringAssert.Contains("https://cdn.example.com/public.txt", redacted,
                "An ordinary public url must not be redacted");
            StringAssert.DoesNotContain("[REDACTED]", redacted,
                "An ordinary public url must not be redacted");

            // Given - a gated file response, which carries a true marker
            const string gatedFileJson = "{\"id\":42,\"name\":\"reward.txt\",\"public\":false," +
                "\"url\":\"https://cdn.example.com/signed?token=abc\",\"requires_download_link\":true}";

            // When
            string gatedRedacted = LootLockerObfuscator.RedactSignedDownloadUrlsForLogging(gatedFileJson);

            // Then - the signed url is redacted
            StringAssert.Contains("[REDACTED]", gatedRedacted, "A gated signed url must be redacted");
            StringAssert.DoesNotContain("token=abc", gatedRedacted, "A gated signed url must be redacted");

            // Given - a response carrying an explicit expiry
            const string expiringJson = "{\"url\":\"https://cdn.example.com/signed?token=xyz\"," +
                "\"url_expires_at\":\"2026-01-02T03:04:05+00:00\"}";

            // When
            string expiringRedacted = LootLockerObfuscator.RedactSignedDownloadUrlsForLogging(expiringJson);

            // Then - the signed url is redacted
            StringAssert.Contains("[REDACTED]", expiringRedacted, "A url with an expiry must be redacted");
            StringAssert.DoesNotContain("token=xyz", expiringRedacted, "A url with an expiry must be redacted");

            // Given - a normal (non-gated) file revision, which carries a signed url with neither
            // an expiry nor a true requires_download_link marker
            const string revisionJson = "{\"id\":\"01J8Z0X9K4ABCDEFGHJKMNPQRS\",\"url\":\"https://cdn.example.com/rev?token=rev\",\"size\":128," +
                "\"created_at\":\"2026-01-02T03:04:05+00:00\",\"requires_download_link\":false}";

            // When
            string revisionRedacted = LootLockerObfuscator.RedactSignedDownloadUrlsForLogging(revisionJson);

            // Then - the revision's signed url is redacted
            StringAssert.Contains("[REDACTED]", revisionRedacted, "A file revision url must be redacted");
            StringAssert.DoesNotContain("token=rev", revisionRedacted, "A file revision url must be redacted");

            // Given - a response body stored as a JSON string, as LootLockerResponse.text is
            const string nestedJson = "{\"text\":\"{\\\"url\\\":\\\"https://cdn.example.com/n?token=nested\\\",\\\"url_expires_at\\\":\\\"2026-01-02T03:04:05+00:00\\\"}\"}";

            // When
            string nestedRedacted = LootLockerObfuscator.RedactSignedDownloadUrlsForLogging(nestedJson);

            // Then - the embedded payload is redacted too
            StringAssert.DoesNotContain("token=nested", nestedRedacted, "A signed url inside a JSON string must be redacted");

            // Given - a body with no url field at all, which is the common case
            const string noUrlJson = "{\"id\":42,\"name\":\"public.txt\",\"size\":128,\"public\":true}";

            // When
            string untouched = LootLockerObfuscator.RedactSignedDownloadUrlsForLogging(noUrlJson);

            // Then - the pre-check short-circuits and the body is returned unchanged
            Assert.AreEqual(noUrlJson, untouched, "A body without a url field must be returned unchanged");
        }
    }
}
