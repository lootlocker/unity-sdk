using LootLocker.Admin;
using LootLocker.Requests;
using NUnit.Framework;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Unit tests for the pure response classifier behind the Project Settings "Validate" button.
    /// No network calls are made here.
    /// </summary>
    public class ConfigValidationTests
    {
        private static LootLockerGameInfoResponse SuccessResponse()
        {
            return new LootLockerGameInfoResponse
            {
                success = true,
                statusCode = 200,
                info = new LootLockerGameInfo
                {
                    name = "Production",
                    title_id = "1234",
                    environment_id = "5678",
                    game_id = 42,
                },
            };
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Validation_SuccessfulResponse_ReportsGameDetails()
        {
            // Given / When
            LootLockerConfigValidationResult result =
                LootLockerConfigValidation.Classify(SuccessResponse(), "some-key");

            // Then
            Assert.AreEqual(LootLockerConfigValidationOutcome.Success, result.Outcome, "Expected success");
            Assert.AreEqual("Production", result.GameName, "Game name not surfaced");
            Assert.AreEqual("1234", result.TitleId, "Title id not surfaced");
            Assert.AreEqual("5678", result.EnvironmentId, "Environment id not surfaced");
            Assert.IsTrue(result.IsSuccess, "IsSuccess should be true");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Validation_SuccessWithoutPayload_ReportsEmptyResponse()
        {
            // Given: the server answered and accepted the request, but sent no game information
            var response = new LootLockerGameInfoResponse { success = true, statusCode = 200, info = null };

            // When
            LootLockerConfigValidationResult result =
                LootLockerConfigValidation.Classify(response, "some-key");

            // Then: this is not a connectivity problem, and reporting it as one sends developers
            // after the wrong issue
            Assert.AreEqual(LootLockerConfigValidationOutcome.EmptyResponse, result.Outcome,
                "A successful response with no payload must not be reported as unreachable");
            Assert.IsFalse(result.IsSuccess);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Validation_Unauthorized_ReportsKeyRejected()
        {
            // Given
            var response = new LootLockerGameInfoResponse { success = false, statusCode = 401 };

            // When
            LootLockerConfigValidationResult result = LootLockerConfigValidation.Classify(response, "bad-key");

            // Then
            Assert.AreEqual(LootLockerConfigValidationOutcome.KeyRejected, result.Outcome,
                "A 401 must be reported as a rejected key, not a connectivity problem");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Validation_Forbidden_ReportsKeyRejected()
        {
            // Given
            var response = new LootLockerGameInfoResponse { success = false, statusCode = 403 };

            // When
            LootLockerConfigValidationResult result = LootLockerConfigValidation.Classify(response, "bad-key");

            // Then
            Assert.AreEqual(LootLockerConfigValidationOutcome.KeyRejected, result.Outcome, "A 403 is an auth failure");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Validation_ServerError_ReportsUnreachable()
        {
            // Given
            var response = new LootLockerGameInfoResponse { success = false, statusCode = 503 };

            // When
            LootLockerConfigValidationResult result = LootLockerConfigValidation.Classify(response, "some-key");

            // Then
            Assert.AreEqual(LootLockerConfigValidationOutcome.Unreachable, result.Outcome,
                "A 5xx must not be reported as a rejected key");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Validation_NullResponse_ReportsUnreachable()
        {
            // Given / When
            LootLockerConfigValidationResult result = LootLockerConfigValidation.Classify(null, "some-key");

            // Then
            Assert.AreEqual(LootLockerConfigValidationOutcome.Unreachable, result.Outcome,
                "A null response means the call never completed");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Validation_NoApiKey_ReportsNoKey()
        {
            // Given / When
            LootLockerConfigValidationResult result = LootLockerConfigValidation.Classify(null, "");

            // Then
            Assert.AreEqual(LootLockerConfigValidationOutcome.NoKey, result.Outcome,
                "An empty key should short-circuit before any request");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Validation_SuccessWithoutInfo_ReportsUnreachable()
        {
            // Given: success flag set but no payload — treat as a failed validation, not a success
            var response = new LootLockerGameInfoResponse { success = true, statusCode = 200, info = null };

            // When
            LootLockerConfigValidationResult result = LootLockerConfigValidation.Classify(response, "some-key");

            // Then
            Assert.AreNotEqual(LootLockerConfigValidationOutcome.Success, result.Outcome,
                "A success response without game info must not be reported as validated");
        }
    }
}
