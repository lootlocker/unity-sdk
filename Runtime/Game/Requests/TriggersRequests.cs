using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// Possible reasons for a trigger key to fail
    /// </summary>
    public enum LootLockerTriggerFailureReasons
    {
        Trigger_limit_reached = 0,
        Key_not_found = 1,
        Reward_not_found = 2,
    };
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerSuccessfulKey
    {
        /// <summary>
        /// The key that was successfully executed
        /// </summary>
        public string Key { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerFailedKey
    {
        /// <summary>
        /// The key that was successfully executed
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The key that was successfully executed
        /// </summary>
        public string LootLockerTriggerFailureReasons { get; set; }
    };

    //==================================================
    // Request Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerInvokeTriggersByKeyRequest
    {
        /// <summary>
        /// The keys of the triggers that should be invoked
        /// </summary>
        public string[] Keys { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// The result of the invoked triggers
    /// </summary>
    public class LootLockerInvokeTriggersByKeyResponse : LootLockerResponse
    {
        /// <summary>
        /// A list of keys that failed invocation together with the reason for failure
        /// </summary>
        public LootLockerFailedKey[] Failed_keys { get; set; }
        /// <summary>
        /// A list of keys that were successfully invoked
        /// </summary>
        public LootLockerFailedKey[] Successful_keys { get; set; }
    };
}
