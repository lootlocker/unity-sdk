using System;
using LootLocker.Requests;

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
        public string acknowledged_at { get; set; }
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
        /// When the player completed onboarding. Null until the player completes it.
        /// </summary>
        public string completed_at { get; set; }
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
        /// Whether onboarding is now complete.
        /// </summary>
        public bool complete { get; set; }
        /// <summary>
        /// When onboarding was completed.
        /// </summary>
        public string completed_at { get; set; }
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

//==================================================
// API Class Definition
//==================================================
namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetPlaytestOnboardingStatus(string forPlayerWithUlid, string subdomain, Action<LootLockerPlaytestOnboardingStatusResponse> onComplete)
        {
            if (string.IsNullOrEmpty(subdomain))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerPlaytestOnboardingStatusResponse>(forPlayerWithUlid));
                return;
            }

            EndPointClass endPoint = LootLockerEndPoints.getPlaytestOnboardingStatus;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.WithPathParameter(subdomain), endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CompletePlaytestOnboarding(string forPlayerWithUlid, string subdomain, Action<LootLockerCompletePlaytestOnboardingResponse> onComplete)
        {
            if (string.IsNullOrEmpty(subdomain))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerCompletePlaytestOnboardingResponse>(forPlayerWithUlid));
                return;
            }

            EndPointClass endPoint = LootLockerEndPoints.completePlaytestOnboarding;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.WithPathParameter(subdomain), endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void AcknowledgePlaytestDocument(string forPlayerWithUlid, string subdomain, string documentId, LootLockerAcknowledgePlaytestDocumentRequest request, Action<LootLockerAcknowledgePlaytestDocumentResponse> onComplete)
        {
            if (string.IsNullOrEmpty(subdomain) || string.IsNullOrEmpty(documentId))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAcknowledgePlaytestDocumentResponse>(forPlayerWithUlid));
                return;
            }

            EndPointClass endPoint = LootLockerEndPoints.acknowledgePlaytestDocument;

            // The body is optional for documents without statements, so only serialize it when
            // statement ids were supplied. Sending an empty object would be rejected.
            string json = request?.statement_ids != null && request.statement_ids.Length > 0
                ? LootLockerJson.SerializeObject(request)
                : null;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.WithPathParameters(subdomain, documentId), endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}
