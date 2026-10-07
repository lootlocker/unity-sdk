using System;
using LootLocker;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestPlayerBan
    {
        /// <summary>
        /// Request body for the admin ban endpoint.
        /// </summary>
        private class BanPlayerRequest
        {
            public string ban_detail { get; set; }
        }

        /// <summary>
        /// Bans a player using the admin API. Creates a permanent manual ban.
        /// </summary>
        /// <param name="playerUlid">The ULID of the player to ban.</param>
        /// <param name="onComplete">Called with the raw response when the request completes.</param>
        /// <param name="banDetail">Optional player-facing ban message to store with the ban.</param>
        public static void BanPlayer(string playerUlid, Action<LootLockerResponse> onComplete, string banDetail = null)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.banPlayer;
            string formattedEndpoint = string.Format(endpoint.endPoint, playerUlid);

            string json = string.IsNullOrEmpty(banDetail)
                ? "{}"
                : LootLockerJson.SerializeObject(new BanPlayerRequest { ban_detail = banDetail });

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, json, serverResponse =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }

        /// <summary>
        /// Unbans a player using the admin API.
        /// </summary>
        /// <param name="playerUlid">The ULID of the player to unban.</param>
        /// <param name="onComplete">Called with the raw response when the request completes.</param>
        public static void UnbanPlayer(string playerUlid, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.unbanPlayer;
            string formattedEndpoint = string.Format(endpoint.endPoint, playerUlid);

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, null, serverResponse =>
            {
                onComplete?.Invoke(serverResponse);
            }, true);
        }
    }
}
