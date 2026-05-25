using System;
using LootLocker;

namespace LootLockerTestConfigurationUtils
{
    public static class LootLockerTestPlayerBan
    {
        /// <summary>
        /// Bans a player using the admin API. Creates a permanent manual ban.
        /// </summary>
        /// <param name="playerUlid">The ULID of the player to ban.</param>
        /// <param name="onComplete">Called with the raw response when the request completes.</param>
        public static void BanPlayer(string playerUlid, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                onComplete?.Invoke(new LootLockerResponse { success = false, errorData = new LootLockerErrorData { message = "Not logged in" } });
                return;
            }

            var endpoint = LootLockerTestConfigurationEndpoints.banPlayer;
            string formattedEndpoint = string.Format(endpoint.endPoint, playerUlid);

            LootLockerAdminRequest.Send(formattedEndpoint, endpoint.httpMethod, "{}", serverResponse =>
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
