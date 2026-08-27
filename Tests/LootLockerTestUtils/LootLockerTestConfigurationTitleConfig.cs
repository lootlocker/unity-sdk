using LootLocker;
using System;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestConfigurationTitleConfig
    {

        public enum TitleConfigKeys
        {
            global_player_presence,
            white_label_custom_signup_fields
        }

        public class PresenceTitleConfigRequest
        {
            public bool enabled { get; set; }
            public bool advanced_mode { get; set; }
        }

        public class WhiteLabelCustomSignUpFieldDefinition
        {
            public string question_text { get; set; }
            public string metadata_key { get; set; }
            public string field_type { get; set; }
            public bool required { get; set; }
            public bool sensitive { get; set; }
            public int sort_order { get; set; }
        }

        public class WhiteLabelCustomSignUpFieldsConfigRequest
        {
            public WhiteLabelCustomSignUpFieldDefinition[] fields { get; set; }
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

        public static void UpdateGameConfig(TitleConfigKeys ConfigKey, bool Enabled, bool EnableRichPresence, Action<LootLockerResponse> onComplete)
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
                advanced_mode = EnableRichPresence
            };
            string json = LootLockerJson.SerializeObject(request);
            LootLockerAdminRequest.Send(endpoint, LootLockerTestConfigurationEndpoints.updateTitleConfig.httpMethod, json, onComplete: (serverResponse) =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }

        public static void SetCustomSignUpFields(WhiteLabelCustomSignUpFieldDefinition[] fields, Action<bool /*success*/, string /*errorMessage*/> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(false, "Not logged in");
                return;
            }

            var request = new WhiteLabelCustomSignUpFieldsConfigRequest
            {
                fields = fields
            };
            string json = LootLockerJson.SerializeObject(request);
            LootLockerTestGameAdmin.SetGameConfig("white_label_custom_signup_fields", json, response =>
            {
                if (response == null)
                {
                    onComplete?.Invoke(false, "Null response from SetGameConfig");
                    return;
                }
                onComplete?.Invoke(response.success, response.errorData?.message);
            });
        }
    }
}
