using System;
using LootLocker.LootLockerEnums;

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
        public DateTime created_at { get; set; }

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
        /// The name of the survey
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// When the survey was created
        /// </summary>
        public DateTime created_at { get; set; }

        /// <summary>
        /// Whether the current player has already completed this survey.
        /// Only populated by ListSurveys.
        /// </summary>
        public bool completed { get; set; }

        /// <summary>
        /// When the current player completed this survey.
        /// Null when the survey has not been completed.
        /// Only populated by ListSurveys.
        /// </summary>
        public DateTime? completed_at { get; set; }

        /// <summary>
        /// The questions in this survey, ordered by sort_order
        /// </summary>
        public LootLockerSurveyQuestion[] questions { get; set; }
    }

    /// <summary>
    /// A single answer to a survey question, submitted through SubmitSurveyAnswers.
    /// </summary>
    public class LootLockerSurveyAnswer
    {
        /// <summary>
        /// The metadata_key of the question this answers
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
        /// The name of the survey
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// When the survey was created
        /// </summary>
        public DateTime created_at { get; set; }

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
        /// Whether the survey is now completed. A successful response always reports true;
        /// failures (for example an unanswered required question) are returned as an error response instead.
        /// </summary>
        public bool complete { get; set; }

        /// <summary>
        /// When the survey was completed
        /// </summary>
        public DateTime completed_at { get; set; }

        /// <summary>
        /// The unique identifier of the completed survey
        /// </summary>
        public string survey_id { get; set; }
    }
}
