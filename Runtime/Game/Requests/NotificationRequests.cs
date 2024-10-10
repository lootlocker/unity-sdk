﻿using System.Collections.Generic;
using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// Enum of the different available priorities for notifications
    /// </summary>
    public enum LootLockerNotificationPriority
    {
        low = 0,
        medium = 1,
        high = 2,
        emergency = 3,
    };

    /// <summary>
    /// Enum of the different kinds of notification bodies possible, use this to figure out how to parse the notification body
    /// </summary>
    public enum LootLockerNotificationContentKind
    {
        group_reward = 0,
        currency_reward = 1,
        asset_reward = 2,
        progression_reset_reward = 3,
        progression_points_reward = 4,
    };
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerNotificationContextEntry
    {
        /// <summary>
        /// The key for this context entry
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The value of this context entry
        /// </summary>
        public string Value { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerNotificationRewardCurrency
    {
        /// <summary>
        /// The date the Currency reward was created.
        /// </summary>
        public string Created_at { get; set; }
        /// <summary>
        /// The date the Currency reward was last updated.
        /// </summary>
        public string Updated_at { get; set; }
        /// <summary>
        /// The amount of Currency to be rewarded.
        /// </summary>
        public string Amount { get; set; }
        /// <summary>
        /// The details on the Currency.
        /// </summary>
        public LootLockerNotificationRewardCurrencyDetails Details { get; set; }
        /// <summary>
        /// The ID of a reward.
        /// </summary>
        public string Reward_id { get; set; }
        /// <summary>
        /// The ID of the Currency.
        /// </summary>
        public string Currency_id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationRewardCurrencyDetails
    {
        /// <summary>
        /// The name of the Currency.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The code of the Currency.
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// The amount of the Currency.
        /// </summary>
        public string Amount { get; set; }
        /// <summary>
        /// The ID of the Currency.
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationRewardProgression
    {
        /// <summary>
        /// The date the Progression Points reward was created.
        /// </summary>
        public string Created_at { get; set; }
        /// <summary>
        /// The date the Progression Points was last updated.
        /// </summary>
        public string Updated_at { get; set; }
        /// <summary>
        /// The details of the Progression.
        /// </summary>
        public LootLockerNotificationRewardProgressionDetails Details { get; set; }
        /// <summary>
        /// The amount of Progression Points to be rewarded.
        /// </summary>
        public int Amount { get; set; }
        /// <summary>
        /// The ID of the Progression.
        /// </summary>
        public string Progression_id { get; set; }
        /// <summary>
        /// The ID of the reward.
        /// </summary>
        public string Reward_id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationRewardProgressionDetails
    {
        /// <summary>
        /// The key of the Progression.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The name of the Progression.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The amount of Progression Points to be rewarded.
        /// </summary>
        public int Amount { get; set; }
        /// <summary>
        /// The ID of the Progression.
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationRewardProgressionReset
    {
        /// <summary>
        /// The date the Progression Reset reward was created.
        /// </summary>
        public string Created_at { get; set; }
        /// <summary>
        /// The date the Progression Reset reward was last updated.
        /// </summary>
        public string Updated_at { get; set; }
        /// <summary>
        /// The details of the Progression reward.
        /// </summary>
        public LootLockerNotificationRewardProgressionResetDetails Details { get; set; }
        /// <summary>
        /// The ID of the Progression.
        /// </summary>
        public string Progression_id { get; set; }
        /// <summary>
        /// The ID of the reward.
        /// </summary>
        public string Reward_id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationRewardProgressionResetDetails
    {
        /// <summary>
        /// The key of the Progression.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The name of the Progression.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The ID of the Progression.
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationRewardAsset
    {
        /// <summary>
        /// The date the Asset reward was created.
        /// </summary>
        public string Created_at { get; set; }
        /// <summary>
        /// The date the Asset reward was last updated.
        /// </summary>
        public string Updated_at { get; set; }
        /// <summary>
        /// The details on the Asset.
        /// </summary>
        public LootLockerNotificationRewardAssetDetails Details { get; set; }
        /// <summary>
        /// The Asset variation ID, will be null if it's not a variation.
        /// </summary>
        public int? Asset_variation_id { get; set; }
        /// <summary>
        /// The Asset rental option ID, will be null if it's not a rental.
        /// </summary>
        public int? Asset_rental_option_id { get; set; }
        /// <summary>
        /// The ID of the Asset.
        /// </summary>
        public int Asset_id { get; set; }
        /// <summary>
        /// The ID of the reward.
        /// </summary>
        public string Reward_id { get; set; }
        /// <summary>
        /// The ULID of the Asset.
        /// </summary>
        public string Asset_ulid { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationRewardAssetDetails
    {
        /// <summary>
        /// The name of the Asset.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The url to the thumbnail, will be null if it's not set in the LootLocker console.
        /// </summary>
        public string Thumbnail { get; set; }
        /// <summary>
        /// The name of the Variation Asset, will be null if it's not a Variation Asset.
        /// </summary>
        public string Variation_name { get; set; }
        /// <summary>
        /// The name of the Rental Asset, will be null if it's not a Variation Asset.
        /// </summary>
        public string Rental_option_name { get; set; }
        /// <summary>
        /// The ID of the Variation, will be null if it's not a Variation Asset.
        /// </summary>
        public int? Variation_id { get; set; }
        /// <summary>
        /// The ID of the rental option, will be null if it's not a Rental Asset.
        /// </summary>
        public int? Rental_option_id { get; set; }
        /// <summary>
        /// The ID of the Asset.
        /// </summary>
        public int Legacy_id { get; set; }
        /// <summary>
        /// the ULID of the Asset.
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationRewardGroup
    {
        /// <summary>
        /// The date the Group reward was created.
        /// </summary>
        public string Created_at { get; set; }

        /// <summary>
        /// The name of the Group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the Group.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Associations for the Group reward.
        /// </summary>
        public LootLockerNotificationGroupRewardAssociations[] Associations { get; set; }

        /// <summary>
        /// The ID of the reward.
        /// </summary>
        public string Reward_id { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationGroupRewardAssociations
    {
        /// <summary>
        /// The kind of reward, (asset / currency / progression points / progression reset). Note that a group is not allowed to contain another group
        /// </summary>
        public LootLockerNotificationContentKind Kind { get; set; }

        /// <summary>
        /// The details on the Asset.
        /// </summary>
        public LootLockerNotificationRewardAssetDetails Asset { get; set; }

        /// <summary>
        /// The details on the Currency.
        /// </summary>
        public LootLockerNotificationRewardCurrencyDetails Currency { get; set; }

        /// <summary>
        /// The Progression Points reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerNotificationRewardProgression Progression_points { get; set; }

        /// <summary>
        /// The Progression Reset reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerNotificationRewardProgressionReset Progression_reset { get; set; }

    }

    /// <summary>
    /// </summary>
    public class LootLockerNotificationContentBody
    {
        /// <summary>
        /// The kind of notification body this contains. Use it to know which field in this object will be populated. If the kind is asset_reward for example, the asset field will be populated, the rest will be null.
        /// </summary>
        public LootLockerNotificationContentKind Kind { get; set; }

        /// <summary>
        /// The currency reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerNotificationRewardCurrency Currency { get; set; } = null;
        /// <summary>
        /// The Progression Reset reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerNotificationRewardProgressionReset Progression_reset { get; set; } = null;
        /// <summary>
        /// The Progression Points reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerNotificationRewardProgression Progression_points { get; set; } = null;
        /// <summary>
        /// The Asset reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerNotificationRewardAsset Asset { get; set; } = null;
        /// <summary>
        /// The Group reward, will be null if the reward is of another type.
        /// </summary>
        public LootLockerNotificationRewardGroup Group { get; set; } = null;
    };

    /// <summary>
    /// </summary>
    public class LootLockerNotificationContent
    {
        /// <summary>
        /// The context for this content. This is a set of key value pairs that hold additional context information about this notification.
        /// </summary>
        public LootLockerNotificationContextEntry[] Context { get; set; }
        /// <summary>
        /// The context for this content. This is a set of key value pairs that hold additional context information about this notification.
        /// </summary>
        public Dictionary<string, string> ContextAsDictionary { get; set; }
        /// <summary>
        /// The body for this notification content, use the kind variable to know which field will be filled with data.
        /// </summary>
        public LootLockerNotificationContentBody Body { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerNotification
    {
        /// <summary>
        /// The time that this notification was created
        /// </summary>
        public string Created_at { get; set; }
        /// <summary>
        /// At what time that this notification expires, after this time, the notification is no longer returned
        /// </summary>
        public string Expiration_date { get; set; }
        /// <summary>
        /// The time that this notification was acknowledged. Will be empty if the notification has not been acknowledged
        /// </summary>
        public string Acknowledged_at { get; set; }
        /// <summary>
        /// The type of this notification
        /// </summary>
        public string Notification_type { get; set; }
        /// <summary>
        /// The priority of this notification (default: medium)
        /// </summary>
        public LootLockerNotificationPriority Priority { get; set; }
        /// <summary>
        /// The originating source of this notification (for example, did it originate from a purchase, a leaderboard reward, or a trigger?)
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// The actual content of this notification
        /// </summary>
        public LootLockerNotificationContent Content { get; set; }
        /// <summary>
        /// The id of the notification, use this when acknowledging and dismissing
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The id of the player that this notification is for
        /// </summary>
        public string Player_id { get; set; }
        /// <summary>
        /// Whether this notification has been acknowledged or not
        /// </summary>
        public bool Acknowledged { get; set; }
    };

    //==================================================
    // Request Definitions
    //==================================================
    
    /// <summary>
    /// </summary>
    public class LootLockerDismissNotificationsRequest
    {
        /// <summary>
        /// List of ids of the notifications to dismiss
        /// </summary>
        public string[] Notifications { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerListNotificationsResponse : LootLockerResponse
    {
        /// <summary>
        /// List of the requested notifications according to pagination settins
        /// </summary>
        public LootLockerNotification[] Notifications { get; set; }
        /// <summary>
        /// Pagination data for this set of notifications
        /// </summary>
        public LootLockerExtendedPagination Pagination { get; set; }
    };

    /// <summary>
    /// </summary>
    public class LootLockerAcknowledgeNotificationsResponse : LootLockerResponse
    {
        // Empty unless there are errors
    };

    /// <summary>
    /// </summary>
    public class LootLockerDismissNotificationsResponse : LootLockerResponse
    {
        // Empty unless there are errors
    };
}