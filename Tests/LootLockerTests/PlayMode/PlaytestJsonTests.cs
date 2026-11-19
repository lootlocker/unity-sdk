using System;
using LootLocker;
using LootLocker.Requests;
using NUnit.Framework;

namespace LootLockerTests.PlayMode
{
    public class PlaytestJsonTests
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Playtest_DeserializingOnboardingStatus_MapsDocumentsAndAnswers()
        {
            // Given
            const string json = "{\"completed\":true,\"completed_at\":\"2026-01-02T03:04:05Z\",\"documents\":[{\"document_id\":\"01K9AAAAAAAAAAAAAAAAAAAAAA\",\"content_hash\":\"abc123\",\"statement_ids\":[\"s1\",\"s2\"],\"acknowledged_at\":\"2026-01-02T03:00:00Z\"}],\"answers\":[{\"key\":\"how_did_you_hear\",\"value\":\"\\\"a friend\\\"\"}]}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerPlaytestOnboardingStatusResponse>(json);

            // Then
            Assert.IsNotNull(response);
            Assert.IsTrue(response.completed);
            Assert.AreEqual(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), response.completed_at);
            Assert.AreEqual(1, response.documents.Length);
            Assert.AreEqual("01K9AAAAAAAAAAAAAAAAAAAAAA", response.documents[0].document_id);
            Assert.AreEqual("abc123", response.documents[0].content_hash);
            Assert.AreEqual(2, response.documents[0].statement_ids.Length);
            Assert.AreEqual("s2", response.documents[0].statement_ids[1]);
            Assert.AreEqual(1, response.answers.Length);
            Assert.AreEqual("how_did_you_hear", response.answers[0].key);
            Assert.AreEqual("\"a friend\"", response.answers[0].value, "A string answer arrives JSON-quoted");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Playtest_DeserializingOnboardingStatusWithoutCompletion_LeavesCompletedAtUnset()
        {
            // Given: completed_at is omitted until the player completes onboarding.
            const string json = "{\"completed\":false,\"documents\":[],\"answers\":[]}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerPlaytestOnboardingStatusResponse>(json);

            // Then
            Assert.IsFalse(response.completed);
            Assert.AreEqual(default(DateTime), response.completed_at, "completed_at is unset when not completed");
            Assert.AreEqual(0, response.documents.Length);
            Assert.AreEqual(0, response.answers.Length);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Playtest_DeserializingCompleteOnboarding_MapsPlaytestId()
        {
            // Given
            const string json = "{\"complete\":true,\"completed_at\":\"2026-01-02T03:04:05Z\",\"playtest_id\":\"01K9BBBBBBBBBBBBBBBBBBBBBB\"}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerCompletePlaytestOnboardingResponse>(json);

            // Then
            Assert.IsTrue(response.complete);
            Assert.AreEqual(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), response.completed_at);
            Assert.AreEqual("01K9BBBBBBBBBBBBBBBBBBBBBB", response.playtest_id);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Playtest_SerializingAcknowledgeRequest_SendsStatementIds()
        {
            // Given
            var request = new LootLockerAcknowledgePlaytestDocumentRequest { statement_ids = new[] { "s1", "s2" } };

            // When
            var json = LootLockerJson.SerializeObject(request).Replace(" ", "");

            // Then
            StringAssert.Contains("\"statement_ids\":[\"s1\",\"s2\"]", json);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Playtest_SerializingEmptyAcknowledgeRequest_SendsNoStatementIds()
        {
            // Given: a document without statements. The backend rejects non-empty statement_ids
            // for such documents, so the empty request must not carry any.
            var request = new LootLockerAcknowledgePlaytestDocumentRequest();

            // When
            var json = LootLockerJson.SerializeObject(request).Replace(" ", "");

            // Then
            Assert.IsFalse(json.Contains("\"s1\""), "No statement ids should be sent");
            StringAssert.DoesNotContain("\"statement_ids\":[\"", json);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Playtest_NullAcknowledgeRequest_BecomesEmptyJsonObject()
        {
            // Given: the SDK sends "{}" rather than null for a document without statements,
            // because CallAPI routes POSTs through MakeJsonRequest which maps null to "{}".
            LootLockerAcknowledgePlaytestDocumentRequest request = null;

            // When
            var json = request?.statement_ids != null && request.statement_ids.Length > 0
                ? LootLockerJson.SerializeObject(request)
                : "{}";

            // Then
            Assert.AreEqual("{}", json, "A missing request must serialize to an empty JSON object");
        }
    }
}
