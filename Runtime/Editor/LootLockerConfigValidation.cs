#if UNITY_EDITOR
using LootLocker.Requests;

namespace LootLocker.Admin
{
    /// <summary>
    /// Outcome of validating the currently effective API key against the LootLocker API.
    /// </summary>
    public enum LootLockerConfigValidationOutcome
    {
        /// <summary>No API key is configured, so there is nothing to validate.</summary>
        NoKey = 0,
        /// <summary>The key was accepted and the game information was returned.</summary>
        Success = 1,
        /// <summary>The server rejected the key (401/403).</summary>
        KeyRejected = 2,
        /// <summary>The server could not be reached, or the request failed for a non-auth reason.</summary>
        Unreachable = 3,
        /// <summary>The server answered successfully but returned no game information.</summary>
        EmptyResponse = 4,
    }

    /// <summary>
    /// Result of a config validation attempt, including the game the key belongs to on success.
    /// </summary>
    public struct LootLockerConfigValidationResult
    {
        public LootLockerConfigValidationOutcome Outcome;
        public string GameName;
        public string TitleId;
        public string EnvironmentId;
        public string Message;

        public bool IsSuccess => Outcome == LootLockerConfigValidationOutcome.Success;
    }

    /// <summary>
    /// Classifies the response of <see cref="LootLocker.Requests.LootLockerSDKManager.GetGameInfo"/>
    /// into a user-facing outcome.
    ///
    /// Kept pure and free of Unity API calls so it can be unit tested without a network request.
    /// </summary>
    public static class LootLockerConfigValidation
    {
        /// <summary>
        /// Classifies a game info response.
        /// </summary>
        /// <param name="response">The response from GetGameInfo, or null if the call never completed.</param>
        /// <param name="apiKey">The API key that was validated. Used to detect the "nothing to validate" case.</param>
        public static LootLockerConfigValidationResult Classify(LootLockerGameInfoResponse response, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return new LootLockerConfigValidationResult
                {
                    Outcome = LootLockerConfigValidationOutcome.NoKey,
                    Message = "No API key is configured.",
                };
            }

            if (response == null)
            {
                return new LootLockerConfigValidationResult
                {
                    Outcome = LootLockerConfigValidationOutcome.Unreachable,
                    Message = "Could not reach LootLocker.",
                };
            }

            if (response.success && response.info != null)
            {
                return new LootLockerConfigValidationResult
                {
                    Outcome = LootLockerConfigValidationOutcome.Success,
                    GameName = response.info.name,
                    TitleId = response.info.title_id,
                    EnvironmentId = response.info.environment_id,
                    Message = "API key accepted.",
                };
            }

            // The server answered and accepted the request, but sent no game information. That is
            // not a connectivity problem, and reporting it as one sends developers after the wrong
            // issue.
            if (response.success)
            {
                return new LootLockerConfigValidationResult
                {
                    Outcome = LootLockerConfigValidationOutcome.EmptyResponse,
                    Message = "API key accepted, but the server returned no game information.",
                };
            }

            // A rejected key is an auth failure. Anything else (timeout, DNS, 5xx) is a
            // connectivity problem, and conflating the two sends developers after the wrong issue.
            if (response.statusCode == 401 || response.statusCode == 403)
            {
                return new LootLockerConfigValidationResult
                {
                    Outcome = LootLockerConfigValidationOutcome.KeyRejected,
                    Message = "API key rejected.",
                };
            }

            return new LootLockerConfigValidationResult
            {
                Outcome = LootLockerConfigValidationOutcome.Unreachable,
                Message = string.IsNullOrEmpty(response.errorData?.message)
                    ? "Could not reach LootLocker."
                    : $"Could not reach LootLocker: {response.errorData.message}",
            };
        }
    }
}
#endif
