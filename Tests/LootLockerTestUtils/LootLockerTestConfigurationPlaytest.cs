using System;
using LootLocker;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestPlaytest
    {
        /// <summary>
        /// Request body for the admin create playtest endpoint.
        /// </summary>
        private class CreatePlaytestRequest
        {
            public string name { get; set; }
            public string display_name { get; set; }
            public string subdomain { get; set; }
            public bool active { get; set; }
            public bool send_waitlist_email { get; set; }
        }

        /// <summary>
        /// Request body for the admin create playtest document endpoint.
        /// </summary>
        private class CreateDocumentRequest
        {
            public string name { get; set; }
            public string content { get; set; }
            public bool required { get; set; }
            public DocumentStatementRequest[] statements { get; set; }
        }

        private class DocumentStatementRequest
        {
            public string id { get; set; }
            public string text { get; set; }
        }

        /// <summary>
        /// Request body for the admin create survey endpoint. A playtest's questions come from a
        /// linked survey, so the tests create one to give the playtest a required question.
        /// </summary>
        private class CreateSurveyRequest
        {
            public string name { get; set; }
            public bool standalone { get; set; }
            public CreateSurveyQuestionRequest[] questions { get; set; }
        }

        private class CreateSurveyQuestionRequest
        {
            public string question_text { get; set; }
            public string metadata_key { get; set; }
            public string field_type { get; set; }
            public bool required { get; set; }
            public int sort_order { get; set; }
        }

        /// <summary>
        /// Request body for the admin update playtest endpoint.
        /// </summary>
        private class UpdatePlaytestRequest
        {
            public string name { get; set; }
            public string survey_id { get; set; }
        }

        /// <summary>
        /// Response from the admin create survey endpoint.
        /// </summary>
        public class LootLockerTestCreateSurveyResponse : LootLockerResponse
        {
            public string id { get; set; }
            public string name { get; set; }
            public string created_at { get; set; }
        }

        /// <summary>
        /// Response from the admin create playtest endpoint.
        /// </summary>
        public class LootLockerTestCreatePlaytestResponse : LootLockerResponse
        {
            public string id { get; set; }
            public long game_id { get; set; }
            public string name { get; set; }
            public string display_name { get; set; }
            public string subdomain { get; set; }
            public bool active { get; set; }
            public bool send_waitlist_email { get; set; }
            public string created_at { get; set; }
        }

        /// <summary>
        /// Response from the admin create playtest document endpoint.
        /// </summary>
        public class LootLockerTestCreateDocumentResponse : LootLockerResponse
        {
            public string id { get; set; }
            public string playtest_id { get; set; }
            public string name { get; set; }
            public string content { get; set; }
            public bool required { get; set; }
            public string content_hash { get; set; }
            public DocumentStatementResponse[] statements { get; set; }
        }

        public class DocumentStatementResponse
        {
            public string id { get; set; }
            public string text { get; set; }
        }

        /// <summary>
        /// Creates an active playtest with the given subdomain using the admin API.
        /// </summary>
        /// <param name="name">The name of the playtest.</param>
        /// <param name="subdomain">The public subdomain of the playtest.</param>
        /// <param name="onComplete">Called with the created playtest when the request completes.</param>
        public static void CreatePlaytest(string name, string subdomain, Action<LootLockerTestCreatePlaytestResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerTestCreatePlaytestResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var request = new CreatePlaytestRequest
            {
                name = name,
                display_name = name,
                subdomain = subdomain,
                active = true,
                send_waitlist_email = false
            };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(LootLockerTestConfigurationEndpoints.createPlaytest.endPoint, LootLockerTestConfigurationEndpoints.createPlaytest.httpMethod, json, serverResponse =>
            {
                LootLockerResponse.Deserialize(onComplete, serverResponse);
            }, true);
        }

        /// <summary>
        /// Creates a document on a playtest using the admin API.
        /// </summary>
        /// <param name="playtestId">The ULID of the playtest to add the document to.</param>
        /// <param name="name">The name of the document.</param>
        /// <param name="content">The document content.</param>
        /// <param name="required">Whether the document must be acknowledged before onboarding can be completed.</param>
        /// <param name="statementIds">Optional statement ids to attach to the document.</param>
        /// <param name="onComplete">Called with the created document when the request completes.</param>
        public static void CreateDocument(string playtestId, string name, string content, bool required, string[] statementIds, Action<LootLockerTestCreateDocumentResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerTestCreateDocumentResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            DocumentStatementRequest[] statements = null;
            if (statementIds != null && statementIds.Length > 0)
            {
                statements = new DocumentStatementRequest[statementIds.Length];
                for (int i = 0; i < statementIds.Length; i++)
                {
                    statements[i] = new DocumentStatementRequest { id = statementIds[i], text = "Statement " + (i + 1) };
                }
            }

            var request = new CreateDocumentRequest
            {
                name = name,
                content = content,
                required = required,
                statements = statements
            };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(LootLockerTestConfigurationEndpoints.createPlaytestDocument.WithPathParameter(playtestId), LootLockerTestConfigurationEndpoints.createPlaytestDocument.httpMethod, json, serverResponse =>
            {
                LootLockerResponse.Deserialize(onComplete, serverResponse);
            }, true);
        }

        /// <summary>
        /// Creates a standalone survey with a single required question using the admin API.
        /// A playtest's questions come from its linked survey, so the tests use this to give a
        /// playtest a required question.
        /// </summary>
        /// <param name="name">The name of the survey.</param>
        /// <param name="metadataKey">The player metadata key the answer is stored under.</param>
        /// <param name="onComplete">Called with the created survey when the request completes.</param>
        public static void CreateSurveyWithRequiredQuestion(string name, string metadataKey, Action<LootLockerTestCreateSurveyResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerTestCreateSurveyResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var request = new CreateSurveyRequest
            {
                name = name,
                standalone = true,
                questions = new[]
                {
                    new CreateSurveyQuestionRequest
                    {
                        question_text = "How did you hear about us?",
                        metadata_key = metadataKey,
                        field_type = "short_text",
                        required = true,
                        sort_order = 0
                    }
                }
            };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(LootLockerTestConfigurationEndpoints.createSurvey.endPoint, LootLockerTestConfigurationEndpoints.createSurvey.httpMethod, json, serverResponse =>
            {
                LootLockerResponse.Deserialize(onComplete, serverResponse);
            }, true);
        }

        /// <summary>
        /// Links a survey to a playtest, which is what gives the playtest its onboarding questions.
        /// </summary>
        /// <param name="playtestId">The ULID of the playtest to update.</param>
        /// <param name="name">The playtest name. The update endpoint requires it.</param>
        /// <param name="surveyId">The ULID of the survey to link.</param>
        /// <param name="onComplete">Called when the request completes.</param>
        public static void LinkSurveyToPlaytest(string playtestId, string name, string surveyId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var request = new UpdatePlaytestRequest { name = name, survey_id = surveyId };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(LootLockerTestConfigurationEndpoints.updatePlaytest.WithPathParameter(playtestId), LootLockerTestConfigurationEndpoints.updatePlaytest.httpMethod, json, serverResponse =>
            {
                LootLockerResponse.Deserialize(onComplete, serverResponse);
            }, true);
        }
    }
}
