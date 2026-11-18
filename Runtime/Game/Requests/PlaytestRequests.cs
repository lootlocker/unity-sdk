using System;
using LootLocker.LootLockerEnums;

namespace LootLocker.Requests
{
    //==================================================
    // Request Definitions
    //==================================================

    /// <summary>
    /// Request to acknowledge a playtest document. Send every statement id in the document.
    /// Omit the request entirely for documents that have no statements.
    /// </summary>
    public class LootLockerAcknowledgePlaytestDocumentRequest
    {
        /// <summary>
        /// Every statement id in the document. Required when the document has statements.
        /// </summary>
        public string[] statement_ids { get; set; }
    }

    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// A document the calling player has acknowledged in a playtest.
    /// </summary>
    public class LootLockerPlaytestAcknowledgedDocument
    {
        /// <summary>
        /// The ULID of the document.
        /// </summary>
        public string document_id { get; set; }
        /// <summary>
        /// The content hash of the document at the time it was acknowledged. A document whose
        /// content changed must be acknowledged again before onboarding can be completed.
        /// </summary>
        public string content_hash { get; set; }
        /// <summary>
        /// The statement ids that were acknowledged. Null when the document has no statements.
        /// </summary>
        public string[] statement_ids { get; set; }
        /// <summary>
        /// When the document was acknowledged.
        /// </summary>
        public DateTime acknowledged_at { get; set; }
    }

    /// <summary>
    /// An answer the calling player has stored for a playtest question.
    /// </summary>
    public class LootLockerPlaytestAnswer
    {
        /// <summary>
        /// The player metadata key the answer is stored under.
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// The serialized metadata entry. A string answer arrives JSON-quoted, for example "\"yes\"".
        /// </summary>
        public string value { get; set; }
    }

    /// <summary>
    /// A single answer to a playtest question, submitted through SubmitPlaytestAnswers.
    /// </summary>
    public class LootLockerPlaytestAnswerSubmission
    {
        /// <summary>
        /// The metadata_key of the question this answers.
        /// </summary>
        public string metadata_key { get; set; }
        /// <summary>
        /// The answer value. Pass it as its native C# type (string, int, bool, etc.) and set type to match.
        /// </summary>
        public object value { get; set; }
        /// <summary>
        /// The metadata type of the value. Defaults to String.
        /// </summary>
        public LootLockerMetadataTypes type { get; set; } = LootLockerMetadataTypes.String;
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response containing the calling player's onboarding progress for a playtest.
    /// </summary>
    public class LootLockerPlaytestOnboardingStatusResponse : LootLockerResponse
    {
        /// <summary>
        /// Whether the player has completed onboarding for this playtest.
        /// </summary>
        public bool completed { get; set; }
        /// <summary>
        /// When the player completed onboarding.
        /// Default (DateTime.MinValue) until the player completes it.
        /// </summary>
        public DateTime completed_at { get; set; }
        /// <summary>
        /// The documents the player has acknowledged.
        /// </summary>
        public LootLockerPlaytestAcknowledgedDocument[] documents { get; set; }
        /// <summary>
        /// The answers the player has stored for this playtest's questions.
        /// </summary>
        public LootLockerPlaytestAnswer[] answers { get; set; }
    }

    /// <summary>
    /// Response returned after completing playtest onboarding for the calling player.
    /// </summary>
    public class LootLockerCompletePlaytestOnboardingResponse : LootLockerResponse
    {
        /// <summary>
        /// Whether onboarding is now complete. A successful response always reports true;
        /// failures (for example an unacknowledged required document) are returned as an error response instead.
        /// </summary>
        public bool complete { get; set; }
        /// <summary>
        /// When onboarding was completed.
        /// </summary>
        public DateTime completed_at { get; set; }
        /// <summary>
        /// The ULID of the playtest that was completed.
        /// </summary>
        public string playtest_id { get; set; }
    }

    /// <summary>
    /// Response returned after acknowledging a playtest document.
    /// </summary>
    public class LootLockerAcknowledgePlaytestDocumentResponse : LootLockerResponse
    {
        /// <summary>
        /// The ULID of the acknowledged document.
        /// </summary>
        public string document_id { get; set; }
        /// <summary>
        /// The content hash of the document that was acknowledged.
        /// </summary>
        public string content_hash { get; set; }
    }
}

