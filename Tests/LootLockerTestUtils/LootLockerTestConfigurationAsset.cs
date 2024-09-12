using LootLocker;

namespace LootLockerTestConfigurationUtils
{

    public class LootLockerTestAssetResponse : LootLockerResponse
    {
        public LootLockerTestAsset asset { get; set; }
    }

    public class LootLockerTestAsset
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string ulid { get; set; }
        public string name { get; set; }
    }

    public class LootLockerRewardResponse : LootLockerResponse
    {
        public string id { get; set; }
    }

    public class LootLockerRewardRequest
    {
        public string entity_id { get; set; }
        public string entity_kind { get; set;}
    }

    public class LootLockerTestContextResponse : LootLockerResponse
    {
        public LootLockerTestContext[] contexts { get; set; }
    }

    public class LootLockerTestContext
    {
        public int id { get; set; }
        public string uuid { get; set; }
        public string name { get; set; }
    }

    public class CreateLootLockerTestAsset
    {
        public int context_id { get; set; }
        public string name { get; set; }
    }

}