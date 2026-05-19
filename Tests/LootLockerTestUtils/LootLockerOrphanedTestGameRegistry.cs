using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

namespace LootLockerTestConfigurationUtils
{
    /// <summary>
    /// Registry for test games created during a test run. LootLockerTestGame.CreateGame()
    /// automatically Register()s each game, and DeleteGame() automatically Unregister()s it.
    /// CleanupAll() is called automatically via Application.quitting when Unity shuts down,
    /// ensuring orphaned games are deleted even when only a subset of tests ran (e.g. CIFast).
    /// </summary>
    public static class LootLockerOrphanedTestGameRegistry
    {
        // Singleton HttpClient — never disposed to avoid canceling in-flight requests on exit.
        private static readonly HttpClient _httpClient = new HttpClient();

        static LootLockerOrphanedTestGameRegistry()
        {
            Application.quitting += CleanupAll;
        }

        private sealed class PendingCleanup
        {
            public int GameId { get; }
            public string AdminToken { get; }
            public string AdminUrl { get; }

            public PendingCleanup(int gameId, string adminToken, string adminUrl)
            {
                GameId = gameId;
                AdminToken = adminToken;
                AdminUrl = adminUrl;
            }
        }

        private static readonly List<PendingCleanup> _pending = new List<PendingCleanup>();

        /// <summary>
        /// Register a game for safety-net cleanup. Call this immediately after the test game is
        /// created and the admin token has been captured, before any ClearSettings() call.
        /// </summary>
        public static void Register(int gameId, string adminToken, string adminUrl)
        {
            _pending.Add(new PendingCleanup(gameId, adminToken, adminUrl));
        }

        /// <summary>
        /// Unregister a game. Call this in TearDown just before normally deleting the game, so
        /// CleanupAll() does not double-delete it.
        /// </summary>
        public static void Unregister(int gameId)
        {
            _pending.RemoveAll(entry => entry.GameId == gameId);
        }

        /// <summary>
        /// Fire-and-forget DELETE for every registered game. Called automatically via
        /// Application.quitting when Unity shuts down. Requests are sent asynchronously and
        /// not awaited — Unity's process does not exit immediately, giving requests time to
        /// reach the server. Uses HttpClient because Unity's WebRequest is not available
        /// outside coroutines.
        /// </summary>
        public static void CleanupAll()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            Debug.Log($"[LootLockerTestCleanup] Sending fire-and-forget deletion for {_pending.Count} orphaned test game(s).");

            foreach (var entry in _pending)
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"{entry.AdminUrl}/v1/game/{entry.GameId}");
                request.Headers.Add("x-auth-token", entry.AdminToken);
                _ = _httpClient.SendAsync(request);
            }

            _pending.Clear();
        }
    }
}
