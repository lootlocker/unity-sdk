using System;


namespace LootLocker.Requests
{

    public enum LootLockerFeedbackTypes
    {
        player = 0,
        game = 2,
        ugc = 3
    }


    public class ListLootLockerFeedbackCategoryResponse : LootLockerResponse
    {
        public LootLockerFeedbackCategory[] categories { get; set; }
    }

    public class LootLockerFeedbackCategory
    {
        public string id { get; set; }
        public LootLockerFeedbackTypes entity { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }


    public class LootLockerFeedbackRequest
    {
        public string entity { get; set; }
        public string entity_id { get; set; }
        public string category_id { get; set; }
        public string description { get; set; }
    }

}