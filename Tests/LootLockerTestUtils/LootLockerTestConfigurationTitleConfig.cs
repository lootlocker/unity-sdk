using LootLocker;
using System;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestConfigurationTitleConfig
    {

        public enum TitleConfigKeys
        {
            global_player_presence
        }

        public class PresenceTitleConfigRequest
        {
            public bool enabled { get; set; }
            public bool advanced_mode { get; set; }
        }

        public static void GetGameConfig(TitleConfigKeys ConfigKey, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            string endpoint = LootLockerTestConfigurationEndpoints.getTitleConfig.WithPathParameter(ConfigKey.ToString());
            LootLockerAdminRequest.Send(endpoint, LootLockerTestConfigurationEndpoints.getTitleConfig.httpMethod, null, onComplete: (serverResponse) =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }

        public static void UpdateGameConfig(TitleConfigKeys ConfigKey, bool Enabled, bool AdvancedMode, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            string endpoint = LootLockerTestConfigurationEndpoints.updateTitleConfig.WithPathParameter(ConfigKey.ToString());
            LootLockerTestConfigurationTitleConfig.PresenceTitleConfigRequest request = new LootLockerTestConfigurationTitleConfig.PresenceTitleConfigRequest
            {
                enabled = Enabled,
                advanced_mode = AdvancedMode
            };
            string json = LootLockerJson.SerializeObject(request);
            LootLockerAdminRequest.Send(endpoint, LootLockerTestConfigurationEndpoints.updateTitleConfig.httpMethod, json, onComplete: (serverResponse) =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }
    }
}
