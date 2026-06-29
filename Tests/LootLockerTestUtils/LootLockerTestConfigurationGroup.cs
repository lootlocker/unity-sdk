using System;
using LootLocker;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestGroup
    {
        /// <summary>
        /// Create a group reward with associations (asset, currency, progression, etc.)
        /// POST /admin/game/{game_id}/reward/group
        /// </summary>
        public static void CreateGroupReward(string name, string description, LootLockerTestGroupRewardAssociation[] associations, Action<LootLockerRewardResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(null);
                return;
            }

            var request = new LootLockerTestCreateGroupRewardRequest
            {
                name = name,
                description = description,
                associations = associations
            };

            var endpoint = LootLockerTestConfigurationEndpoints.createGroupReward;
            string json = LootLockerJson.SerializeObject(request);
            LootLockerAdminRequest.Send(endpoint.endPoint, endpoint.httpMethod, json, serverResponse =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerRewardResponse>(serverResponse);
                onComplete?.Invoke(response);
            }, true);
        }
    }

    public class LootLockerTestGroupRewardAssociation
    {
        /// <summary>
        /// The kind of entity: "asset", "currency", "progression_points", "progression_reset", "reward"
        /// </summary>
        public string entity_kind { get; set; }

        /// <summary>
        /// The ULID of the existing entity (asset, currency, progression, etc.)
        /// When entity_kind is "reward", this is an existing reward ULID to add to the group.
        /// </summary>
        public string entity_id { get; set; }

        /// <summary>
        /// Optional metadata (e.g., asset_variation_id, purchased_amount for currency)
        /// </summary>
        public LootLockerTestGroupRewardAssociationMetadata[] metadata { get; set; }
    }

    public class LootLockerTestGroupRewardAssociationMetadata
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class LootLockerTestCreateGroupRewardRequest
    {
        public string name { get; set; }
        public string description { get; set; }
        public LootLockerTestGroupRewardAssociation[] associations { get; set; }
    }
}
