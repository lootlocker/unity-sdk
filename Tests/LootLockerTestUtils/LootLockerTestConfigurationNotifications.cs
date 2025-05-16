using LootLocker;
using System;

namespace LootLockerTestConfigurationUtils
{
    public class LootLockerTestCustomNotificationProperty
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class LootLockerTestCustomNotification
    {
        public string notification_type { get; set; }
        public string priority { get; set; }
        public string recipient { get; set; }

        public object content { get; set; }

        public LootLockerTestCustomNotificationProperty[] properties { get; set; }
    }

    public static class LootLockerTestConfigurationNotifications
    {
        public static void SendCustomNotification(string notificationType, string priority, string recipientPlayerUlid, object content, LootLockerTestCustomNotificationProperty[] properties, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var request = new LootLockerTestCustomNotification
            {
                notification_type = notificationType,
                priority = priority,
                recipient = recipientPlayerUlid,
                content = content,
                properties = properties
            };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(LootLockerTestConfigurationEndpoints.sendCustomNotification.endPoint, LootLockerTestConfigurationEndpoints.sendCustomNotification.httpMethod, json, onComplete: (serverResponse) =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }
    }
}
