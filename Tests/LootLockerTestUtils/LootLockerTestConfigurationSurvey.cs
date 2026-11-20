using System;
using LootLocker;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestSurvey
    {
        /// <summary>
        /// Request body for the admin create survey endpoint.
        /// </summary>
        private class CreateSurveyRequest
        {
            public string name { get; set; }
            public bool standalone { get; set; }
            public CreateSurveyQuestionRequest[] questions { get; set; }
        }

        /// <summary>
        /// A single question in the admin create survey request.
        /// </summary>
        private class CreateSurveyQuestionRequest
        {
            public string question_text { get; set; }
            public string metadata_key { get; set; }
            public string field_type { get; set; }
            public bool required { get; set; }
            public int sort_order { get; set; }
        }

        /// <summary>
        /// Response from the admin create survey endpoint.
        /// </summary>
        public class LootLockerTestCreateSurveyResponse : LootLockerResponse
        {
            public string id { get; set; }
            public long game_id { get; set; }
            public string name { get; set; }
            public bool standalone { get; set; }
            public string created_at { get; set; }
        }

        /// <summary>
        /// Creates a standalone survey with a single required question using the admin API.
        /// </summary>
        /// <param name="name">The name of the survey.</param>
        /// <param name="metadataKey">The player metadata key the answer is stored under.</param>
        /// <param name="onComplete">Called with the created survey when the request completes.</param>
        public static void CreateSurveyWithRequiredQuestion(string name, string metadataKey, Action<LootLockerTestCreateSurveyResponse> onComplete)
        {
            CreateSurveyWithRequiredQuestion(name, metadataKey, "short_text", onComplete);
        }

        /// <summary>
        /// Creates a standalone survey with a single required question of the given field type.
        /// </summary>
        public static void CreateSurveyWithRequiredQuestion(string name, string metadataKey, string fieldType, Action<LootLockerTestCreateSurveyResponse> onComplete)
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
                        field_type = fieldType,
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
    }
}
