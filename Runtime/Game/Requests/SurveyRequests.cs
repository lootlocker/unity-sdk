using System;
using System.Collections.Generic;

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// A single question in a survey. Answers are submitted through the player metadata API using the question's metadata_key.
    /// </summary>
    public class LootLockerSurveyQuestion
    {
        /// <summary>
        /// The unique identifier of the question
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The unique identifier of the survey this question belongs to
        /// </summary>
        public string survey_id { get; set; }

        /// <summary>
        /// The text shown to the player for this question
        /// </summary>
        public string question_text { get; set; }

        /// <summary>
        /// The player metadata key that the answer to this question is stored under
        /// </summary>
        public string metadata_key { get; set; }

        /// <summary>
        /// The type of field to render for this question (for example short_text, long_text, select)
        /// </summary>
        public string field_type { get; set; }

        /// <summary>
        /// Opaque JSON parameters for the field type, for example the available options for a select field.
        /// Null when the question has no parameters.
        /// </summary>
        public object @params { get; set; }

        /// <summary>
        /// Whether an answer to this question is required before the survey can be completed
        /// </summary>
        public bool required { get; set; }

        /// <summary>
        /// The order in which this question should be presented, ascending
        /// </summary>
        public int sort_order { get; set; }

        /// <summary>
        /// When the question was created
        /// </summary>
        public string created_at { get; set; }

        /// <summary>
        /// When true, answers for this question's metadata_key are masked in game metadata reads.
        /// </summary>
        public bool sensitive { get; set; }
    }

    /// <summary>
    /// A survey that a player can see and complete.
    /// </summary>
    public class LootLockerSurvey
    {
        /// <summary>
        /// The unique identifier of the survey
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The legacy id of the game this survey belongs to
        /// </summary>
        public long game_id { get; set; }

        /// <summary>
        /// The name of the survey
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Whether this survey is standalone (listed and completable through the Game API).
        /// Non-standalone surveys are playtest-only and never returned by these endpoints.
        /// </summary>
        public bool standalone { get; set; }

        /// <summary>
        /// When the survey was created
        /// </summary>
        public string created_at { get; set; }

        /// <summary>
        /// Whether the current player has already completed this survey.
        /// Only populated by ListSurveys.
        /// </summary>
        public bool completed { get; set; }

        /// <summary>
        /// When the current player completed this survey, as an RFC 3339 timestamp.
        /// Empty when the survey has not been completed. Only populated by ListSurveys.
        /// </summary>
        public string completed_at { get; set; }

        /// <summary>
        /// The questions in this survey, ordered by sort_order
        /// </summary>
        public LootLockerSurveyQuestion[] questions { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response containing the standalone surveys the current player can see.
    /// </summary>
    public class LootLockerListSurveysResponse : LootLockerResponse
    {
        /// <summary>
        /// The surveys visible to the current player, filtered by each survey's segments
        /// </summary>
        public LootLockerSurvey[] surveys { get; set; }
    }

    /// <summary>
    /// Response containing a single survey and its questions.
    /// </summary>
    public class LootLockerGetSurveyResponse : LootLockerResponse
    {
        /// <summary>
        /// The unique identifier of the survey
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The legacy id of the game this survey belongs to
        /// </summary>
        public long game_id { get; set; }

        /// <summary>
        /// The name of the survey
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Whether this survey is standalone (listed and completable through the Game API)
        /// </summary>
        public bool standalone { get; set; }

        /// <summary>
        /// When the survey was created
        /// </summary>
        public string created_at { get; set; }

        /// <summary>
        /// The questions in this survey, ordered by sort_order
        /// </summary>
        public LootLockerSurveyQuestion[] questions { get; set; }
    }

    /// <summary>
    /// Response returned after recording a survey completion for the current player.
    /// </summary>
    public class LootLockerCompleteSurveyResponse : LootLockerResponse
    {
        /// <summary>
        /// Whether the survey is now completed
        /// </summary>
        public bool complete { get; set; }

        /// <summary>
        /// When the survey was completed, as an RFC 3339 timestamp
        /// </summary>
        public string completed_at { get; set; }

        /// <summary>
        /// The unique identifier of the completed survey
        /// </summary>
        public string survey_id { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void ListSurveys(string forPlayerWithUlid, Action<LootLockerListSurveysResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listSurveys;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetSurvey(string forPlayerWithUlid, string surveyId, Action<LootLockerGetSurveyResponse> onComplete)
        {
            if (string.IsNullOrEmpty(surveyId))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGetSurveyResponse>(forPlayerWithUlid));
                return;
            }

            EndPointClass endPoint = LootLockerEndPoints.getSurvey;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.WithPathParameter(surveyId), endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CompleteSurvey(string forPlayerWithUlid, string surveyId, Action<LootLockerCompleteSurveyResponse> onComplete)
        {
            if (string.IsNullOrEmpty(surveyId))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerCompleteSurveyResponse>(forPlayerWithUlid));
                return;
            }

            EndPointClass endPoint = LootLockerEndPoints.completeSurvey;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.WithPathParameter(surveyId), endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}
