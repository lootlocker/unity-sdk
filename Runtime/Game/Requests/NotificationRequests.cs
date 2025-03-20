using System;
using System.Collections.Generic;
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
        group = 0,
        currency = 1,
        asset = 2,
        progression_reset = 3,
        progression_points = 4,
    };
}

namespace LootLocker.LootLockerStaticStrings
{
    /// <summary>
    /// Possible types of notifications
    /// </summary>
    public struct LootLockerNotificationTypes
    {
        public static readonly string PullRewardAcquired = "pull.reward.acquired";
    }

    /// <summary>
    /// Possible sources for notifications
    /// </summary>
    public struct LootLockerNotificationSources
    {
        public static readonly string Triggers = "triggers";
        public struct Purchasing
        {
            public static readonly string SteamStore = "purchasing.steam_store";
            public static readonly string AppleAppStore = "purchasing.apple_app_store";
            public static readonly string GooglePlayStore = "purchasing.google_play_store";
            public static readonly string LootLocker = "purchasing.lootlocker";
        }
        public static readonly string TwitchDrop = "twitch_drop";
    }

    /// <summary>
    /// The standard context keys to expect for different notification sources
    /// </summary>
    public struct LootLockerStandardContextKeys
    {
        /// <summary>
        /// Standard context keys to expect when source is triggers
        /// </summary>
        public struct Triggers
        {
            public static readonly string Id = "trigger_id";
            public static readonly string Key = "trigger_key";
            public static readonly string Limit = "trigger_limit";
        }

        /// <summary>
        /// Standard context keys to expect when source is purchasing
        /// </summary>
        public struct Purchasing
        {
            /// <summary>
            /// Standard context keys to expect when source is purchasing from the Steam store
            /// </summary>
            public struct SteamStore
            {
                public static readonly string CatalogId = "catalog_id";
                public static readonly string CatalogItemId = "catalog_item_id";
                public static readonly string EntitlementId = "entitlement_id";
                public static readonly string CharacterId = "character_id";
            }
            /// <summary>
            /// Standard context keys to expect when source is purchasing from the Apple app store
            /// </summary>
            public struct AppleAppStore
            {
                public static readonly string CatalogId = "catalog_id";
                public static readonly string CatalogItemId = "catalog_item_id";
                public static readonly string TransactionId = "transaction_id";
            }
            /// <summary>
            /// Standard context keys to expect when source is purchasing from the GooglePlay store
            /// </summary>
            public struct GooglePlayStore
            {
                public static readonly string CatalogId = "catalog_id";
                public static readonly string CatalogItemId = "catalog_item_id";
                public static readonly string ProductId = "product_id";
            }
            /// <summary>
            /// Standard context keys to expect when source is purchasing from LootLocker
            /// </summary>
            public struct LootLocker
            {
                public static readonly string CatalogId = "catalog_id";
                public static readonly string CatalogItemId = "catalog_item_id";
            }
        }
        
        /// <summary>
        /// Standard context keys to expect when source is twitch drop
        /// </summary>
        public struct TwitchDrop
        {
            public static readonly string TwitchRewardId = "twitch_reward_id";
        }
    }
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
        public DateTime Created_at { get; set; }
        /// <summary>
        /// The date the Currency reward was last updated.
        /// </summary>
        public DateTime Updated_at { get; set; }
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
        public DateTime Created_at { get; set; }
        /// <summary>
        /// The date the Progression Points was last updated.
        /// </summary>
        public DateTime Updated_at { get; set; }
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
        public DateTime Created_at { get; set; }
        /// <summary>
        /// The date the Progression Reset reward was last updated.
        /// </summary>
        public DateTime Updated_at { get; set; }
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
        public DateTime Created_at { get; set; }
        /// <summary>
        /// The date the Asset reward was last updated.
        /// </summary>
        public DateTime Updated_at { get; set; }
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
        public DateTime Created_at { get; set; }

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
        /// The context for this content. This is a set of key value pairs that hold additional context information about this notification. Use the static defines in LootLockerStaticStrings.LootLockerStandardContextKeys know what standard values will be in the context depending on the type and source.
        /// </summary>
        public LootLockerNotificationContextEntry[] Context { get; set; }
        /// <summary>
        /// The context for this content. This is a set of key value pairs that hold additional context information about this notification. Use the static defines in LootLockerStaticStrings.LootLockerStandardContextKeys know what standard values will be in the context depending on the type and source.
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
        public DateTime Created_at { get; set; }
        /// <summary>
        /// At what time that this notification expires, after this time, the notification is no longer returned
        /// </summary>
        public string Expiration_date { get; set; }
        /// <summary>
        /// The time that this notification was read. Will be empty if the notification has not been read
        /// </summary>
        public DateTime? Read_at { get; set; }
        /// <summary>
        /// The type of this notification. Use the static defines in LootLockerStaticStrings.LootLockerNotificationTypes know what possible values this can be.
        /// </summary>
        public string Notification_type { get; set; }
        /// <summary>
        /// The priority of this notification (default: medium)
        /// </summary>
        public LootLockerNotificationPriority Priority { get; set; }
        /// <summary>
        /// The originating source of this notification (for example, did it originate from a purchase, a leaderboard reward, or a trigger?). Use the static defines in LootLockerStaticStrings.LootLockerNotificationSources know what possible values this can be
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// The actual content of this notification
        /// </summary>
        public LootLockerNotificationContent Content { get; set; }
        /// <summary>
        /// The id of the notification, use this when marking as read
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The id of the player that this notification is for
        /// </summary>
        public string Player_id { get; set; }
        /// <summary>
        /// Whether this notification has been read or not
        /// </summary>
        public bool Read { get; set; }
        /// <summary>
        /// Will mark this notification as read in LootLocker (though remember to check the response if it succeeded)
        /// </summary>
        /// <param name="onComplete">Action for handling the server response</param>
        public void MarkThisNotificationAsRead(Action<LootLockerReadNotificationsResponse> onComplete)
        {
            LootLockerSDKManager.MarkNotificationsAsRead(new string[]{Id}, onComplete);
        }
    };

    //==================================================
    // Request Definitions
    //==================================================
    
    /// <summary>
    /// </summary>
    public class LootLockerReadNotificationsRequest
    {
        /// <summary>
        /// List of ids of the notifications to mark as read
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
        /// List of the requested notifications according to pagination settings
        /// </summary>
        public LootLockerNotification[] Notifications { get; set; }
        /// <summary>
        /// Pagination data for this set of notifications
        /// </summary>
        public LootLockerExtendedPagination Pagination { get; set; }

        /// <summary>
        /// Will mark all unread notifications in this response as read in LootLocker (though remember to check the response if it succeeded)
        /// </summary>
        /// <param name="onComplete">Action for handling the server response</param>
        public void MarkUnreadNotificationsAsRead(Action<LootLockerReadNotificationsResponse> onComplete)
        {
            if (UnreadNotifications == null || UnreadNotifications.Count <= 0)
            {
                onComplete?.Invoke(new LootLockerReadNotificationsResponse { errorData = null, EventId = "", statusCode = 204, success = true, text = "{}" });
                return;
            }
            LootLockerSDKManager.MarkNotificationsAsRead(UnreadNotifications.ToArray(), onComplete);
        }

        /// <summary>
        /// Get notifications by their identifying value. The out is an array because many notifications are not unique. For example triggers that can be triggered multiple times.
        /// For Triggers the identifying value is the key of the trigger
        /// For Google Play Store purchases it is the product id
        /// For Apple App Store purchases it is the transaction id
        /// For LootLocker virtual purchases it is the catalog item id
        /// Twitch Drops have no uniquely identifying information, so sending in "twitch_drop" returns all twitch drop notifications
        /// </summary>
        /// <param name="identifyingValue">The identifying value of the notification you want to fetch.</param>
        /// <param name="notifications">A list of notifications that were found for the given identifying value or null if none were found.</param>
        /// <returns>True if notifications were found for the identifying value. False if notifications couldn't be found for this value or if the underlying lookup table is corrupt.</returns>
        public bool TryGetNotificationsByIdentifyingValue(string identifyingValue, out LootLockerNotification[] notifications)
        {
            notifications = null;
            if (!NotificationLookupTable.TryGetValue(identifyingValue, out var lookupEntries))
            {
                return false;
            }

            var foundNotifications = new List<LootLockerNotification>();
            foreach (var lookupEntry in lookupEntries)
            {
                if (lookupEntry.NotificationIndex < 0 || lookupEntry.NotificationIndex >= Notifications.Length)
                {
                    // The notifications array is not the same as when the lookup table was populated
                    return false;
                }
                var notification = Notifications[lookupEntry.NotificationIndex];
                if (notification == null 
                    || !notification.Id.Equals(lookupEntry.NotificationId, StringComparison.OrdinalIgnoreCase) 
                    || (!notification.Content.ContextAsDictionary.TryGetValue(lookupEntry.IdentifyingKey, out string actualContextValue) 
                        || actualContextValue == null 
                        || !actualContextValue.Equals(identifyingValue, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                {
                    // The notifications array is not the same as when the lookup table was populated
                    return false;
                }
                foundNotifications.Add(notification);
            }

            notifications = foundNotifications.ToArray();
            return true;
        }

        /// <summary>
        /// Populate convenience structures
        /// </summary>
        public void PopulateConvenienceStructures()
        {
            if (Notifications == null || Notifications.Length <= 0)
            {
                return;
            }

            int i = 0;
            foreach (var notification in Notifications)
            {
                notification.Content.ContextAsDictionary = new Dictionary<string, string>();
                if (!notification.Read)
                {
                    UnreadNotifications.Add(notification.Id);
                }
                foreach (var contextEntry in notification.Content.Context)
                {
                    notification.Content.ContextAsDictionary.Add(contextEntry.Key, contextEntry.Value);
                }

                string identifyingKey = null;
                if (notification.Source.Equals(LootLockerStaticStrings.LootLockerNotificationSources.Triggers, StringComparison.OrdinalIgnoreCase))
                {
                    identifyingKey = LootLockerStaticStrings.LootLockerStandardContextKeys.Triggers.Key;
                }
                else if (notification.Source.Equals(LootLockerStaticStrings.LootLockerNotificationSources.Purchasing.LootLocker, StringComparison.OrdinalIgnoreCase))
                {
                    identifyingKey = LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.LootLocker.CatalogItemId;
                }
                else if (notification.Source.Equals(LootLockerStaticStrings.LootLockerNotificationSources.Purchasing.GooglePlayStore, StringComparison.OrdinalIgnoreCase))
                {
                    identifyingKey = LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.GooglePlayStore.ProductId;
                }
                else if (notification.Source.Equals(LootLockerStaticStrings.LootLockerNotificationSources.Purchasing.AppleAppStore, StringComparison.OrdinalIgnoreCase))
                {
                    identifyingKey = LootLockerStaticStrings.LootLockerStandardContextKeys.Purchasing.AppleAppStore.TransactionId;
                }
                else if (notification.Source.Equals(LootLockerStaticStrings.LootLockerNotificationSources.TwitchDrop, StringComparison.OrdinalIgnoreCase))
                {
                    identifyingKey = LootLockerStaticStrings.LootLockerStandardContextKeys.TwitchDrop.TwitchRewardId;
                }

                if (identifyingKey != null && notification.Content.ContextAsDictionary.TryGetValue(identifyingKey, out var value) && value != null)
                {
                    var lookupEntry = new LootLockerNotificationLookupTableEntry
                    {
                        IdentifyingKey = identifyingKey,
                        NotificationId = notification.Id,
                        NotificationIndex = i
                    };
                    if (NotificationLookupTable.TryGetValue(value, out var indexes))
                    {
                        indexes.Add(lookupEntry);
                    }
                    else
                    {
                        NotificationLookupTable.Add(value, new List<LootLockerNotificationLookupTableEntry> { lookupEntry });
                    }
                }
                ++i;
            }
        }

        private readonly List<string> UnreadNotifications = new List<string>();

        /// <summary>
        /// </summary>
        private struct LootLockerNotificationLookupTableEntry
        {
            public string IdentifyingKey { get; set; }
            public string NotificationId { get; set; }
            public int NotificationIndex { get; set; }
        };
        private readonly Dictionary<string, List<LootLockerNotificationLookupTableEntry>> NotificationLookupTable = new Dictionary<string, List<LootLockerNotificationLookupTableEntry>>();
    };

    /// <summary>
    /// </summary>
    public class LootLockerReadNotificationsResponse : LootLockerResponse
    {
        // Empty unless there are errors
    };
}
