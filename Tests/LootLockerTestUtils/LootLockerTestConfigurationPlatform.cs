using System;
using LootLocker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLockerTestConfigurationUtils
{
    public class LootLockerTestPlatform
    {
        public string name { get; set; }

        public static void UpdatePlatform(string _platformName, bool _enabled, Dictionary<string, object> _settings,
            Action<bool, string, LootLockerTestPlatform> onComplete /*success*/ /*errorMessage*/ /*Platform*/)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Not signed in
                onComplete?.Invoke(false, "Not logged in", null);
                return;
            }

            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.UpdatePlatform;
            var formattedEndpoint = string.Format(endPoint.endPoint, _platformName);

            UpdatePlatformRequest request = new UpdatePlatformRequest
            {
                enabled = _enabled,
                settings = _settings
            };

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(formattedEndpoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) =>
                {
                    var updatePlatformResponse = LootLockerResponse.Deserialize<UpdatePlatformResponse>(serverResponse);
                    onComplete?.Invoke(updatePlatformResponse.success, updatePlatformResponse?.errorData?.message, new LootLockerTestPlatform{ name = _platformName });
                }, true);
        }
    }
    public class UpdatePlatformRequest
    {
        public bool enabled { get; set; }
        public Dictionary<string, object> settings { get; set; } = new Dictionary<string, object>();
    }

    public class UpdatePlatformResponse : LootLockerResponse
    {

    }


}