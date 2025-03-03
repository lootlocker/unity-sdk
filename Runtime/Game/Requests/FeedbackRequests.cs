namespace LootLocker.Requests
{
    /// <summary>
    /// Enum with the supported feedback types that are avaliable for feedback
    /// </summary>
    public enum LootLockerFeedbackTypes
    {
        player = 0,
        game = 2,
        ugc = 3
    }


    public class ListLootLockerFeedbackCategoryResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of categories made for the game
        /// </summary>
        public LootLockerFeedbackCategory[] categories { get; set; }
    }

    public class LootLockerFeedbackCategory
    {
        /// <summary>
        /// The unique identifier of a feedback category
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The type of a feedback category (Player, Game, UGC)
        /// </summary>
        public LootLockerFeedbackTypes entity { get; set; }

        /// <summary>
        /// The name of a feedback category
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The description of a feedback category
        /// </summary>
        public string description { get; set; }
    }


    public class LootLockerFeedbackRequest
    {
        /// <summary>
        /// A string representation of the type of feedback
        /// </summary>
        public LootLockerFeedbackTypes entity { get; set; }

        /// <summary>
        /// The Ulid of what you're sending feedback about
        /// </summary>
        public string entity_id { get; set; }

        /// <summary>
        /// The category id of what kind of feedback you're sending, use ListFeedbackCategories for all the ids
        /// </summary>
        public string category_id { get; set; }

        /// <summary>
        /// The description of feedback you're sending, this will be the reason
        /// </summary>
        public string description { get; set; }
    }

}