using System;
using LootLocker.Requests;

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================
    /// <summary>
    /// Information about the currently configured game in LootLocker.
    /// </summary>
    public class LootLockerGameInfo
    {
        // The title ID of the game (uniquely identifies the game in LootLocker)
        public string title_id { get; set; }
        // The environment ID of the game (identifies which environment instance of the title this game refers to in LootLocker)
        public string environment_id { get; set; }
        // The id of the game (uniquely identifies the game in LootLocker)
        public int game_id { get; set; }
        // The name of the game as configured in LootLocker
        public string name { get; set; }
    };

    //==================================================
    // Response Definitions
    //==================================================
    public class LootLockerPingResponse : LootLockerResponse
    {
        public string date { get; set; }
    }

    /// <summary>
    /// Represents a response from the LootLocker API containing game information.
    /// </summary>
    public class LootLockerGameInfoResponse : LootLockerResponse
    {
        public LootLockerGameInfo info { get; set; }
    }

    /// <summary>
    /// The state of a player's connection and session with the LootLocker backend.
    /// Returned by <see cref="LootLocker.Requests.LootLockerSDKManager.CheckConnectionStatus"/>.
    /// </summary>
    public enum LootLockerConnectionState
    {
        /// <summary>The SDK has not been initialized.</summary>
        NotInitialized,
        /// <summary>No session token exists for the specified player – they have not signed in.</summary>
        NotSignedIn,
        /// <summary>The session token is valid and the server is reachable.</summary>
        SignedInAndConnected,
        /// <summary>A session token exists but the server returned 401 – the token has expired.</summary>
        SessionExpired,
        /// <summary>A session token exists but the player is currently banned.</summary>
        Banned,
        /// <summary>The server could not be reached (no network, timeout, or status code 0).</summary>
        NoConnection,
        /// <summary>The server returned a 5xx error, or an unexpected non-success status code.</summary>
        ServerError,
    }

    /// <summary>
    /// Response for <see cref="LootLocker.Requests.LootLockerSDKManager.CheckConnectionStatus"/>.
    /// </summary>
    public class LootLockerConnectionStateResponse : LootLockerResponse
    {
        /// <summary>
        /// The determined connection state for the player.
        /// </summary>
        public LootLockerConnectionState State { get; set; }

        /// <summary>
        /// The server timestamp returned by the ping. Populated only when <see cref="State"/> is
        /// <see cref="LootLockerConnectionState.SignedInAndConnected"/>.
        /// </summary>
        public string ServerTime { get; set; }

        /// <summary>
        /// Details about the active ban. Populated only when <see cref="State"/> is
        /// <see cref="LootLockerConnectionState.Banned"/>.
        /// </summary>
        public LootLockerBanInfo BanDetails { get; set; }
    }

    //==================================================
    // Request Definitions
    //==================================================
    /// <summary>
    /// Represents a request to get game information from the LootLocker API.
    /// </summary>
    [Serializable]
    public class LootLockerGameInfoRequest
    {
        public string api_key { get; set; }
    };
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void Ping(string forPlayerWithUlid, Action<LootLockerPingResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.ping;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetGameInfo(Action<LootLockerGameInfoResponse> onComplete)
        {
            string body = LootLockerJson.SerializeObject(new LootLockerGameInfoRequest { api_key = LootLockerConfig.current.apiKey });
            LootLockerServerRequest.CallAPI("", LootLockerEndPoints.gameInfo.endPoint, LootLockerEndPoints.gameInfo.httpMethod, body, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: false);
        }

        public static void CheckConnectionStatus(string forPlayerWithUlid, Action<LootLockerConnectionStateResponse> onComplete)
        {
            EndPointClass pingEndpoint = LootLockerEndPoints.ping;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, pingEndpoint.endPoint, pingEndpoint.httpMethod, null, (pingResponse) =>
            {
                if (pingResponse == null || pingResponse.statusCode == 0)
                {
                    onComplete?.Invoke(new LootLockerConnectionStateResponse
                    {
                        success = false,
                        State = LootLockerConnectionState.NoConnection,
                        statusCode = pingResponse?.statusCode ?? 0,
                        text = pingResponse?.text,
                        errorData = pingResponse?.errorData,
                        requestContext = pingResponse?.requestContext,
                    });
                    return;
                }

                if (pingResponse.success)
                {
                    var successPing = LootLockerResponse.Deserialize<LootLockerPingResponse>(pingResponse);
                    onComplete?.Invoke(new LootLockerConnectionStateResponse
                    {
                        success = true,
                        State = LootLockerConnectionState.SignedInAndConnected,
                        ServerTime = successPing?.date,
                        statusCode = pingResponse.statusCode,
                        text = pingResponse.text,
                        requestContext = pingResponse.requestContext,
                    });
                    return;
                }

                int statusCode = pingResponse.statusCode;

                if (statusCode >= 500)
                {
                    onComplete?.Invoke(new LootLockerConnectionStateResponse
                    {
                        success = false,
                        State = LootLockerConnectionState.ServerError,
                        statusCode = statusCode,
                        text = pingResponse.text,
                        errorData = pingResponse.errorData,
                        requestContext = pingResponse.requestContext,
                    });
                    return;
                }

                // 401 = token expired (or auto-refresh of a banned player's session failed and synthesized a 401).
                // 403 = forbidden – could also be a ban.
                // In both cases, call ban-status to check whether the player is banned.
                if (statusCode == 401 || statusCode == 403)
                {
                    LootLockerServerRequest.CallAPI(null,
                        LootLockerEndPoints.banStatusRequest.endPoint,
                        LootLockerEndPoints.banStatusRequest.httpMethod,
                        LootLockerJson.SerializeObject(new LootLockerBanStatusRequest { player_id = forPlayerWithUlid }),
                        (banResponse) =>
                        {
                            var banStatus = LootLockerResponse.Deserialize<LootLockerBanStatusResponse>(banResponse);
                            if (banStatus != null && banStatus.success && banStatus.is_banned)
                            {
                                onComplete?.Invoke(new LootLockerConnectionStateResponse
                                {
                                    success = false,
                                    State = LootLockerConnectionState.Banned,
                                    BanDetails = banStatus.ban,
                                    statusCode = statusCode,
                                    text = pingResponse.text,
                                    errorData = pingResponse.errorData,
                                    requestContext = pingResponse.requestContext,
                                });
                            }
                            else
                            {
                                onComplete?.Invoke(new LootLockerConnectionStateResponse
                                {
                                    success = false,
                                    State = LootLockerConnectionState.SessionExpired,
                                    statusCode = statusCode,
                                    text = pingResponse.text,
                                    errorData = pingResponse.errorData,
                                    requestContext = pingResponse.requestContext,
                                });
                            }
                        },
                        useAuthToken: false);
                    return;
                }

                // Any other failure (e.g., 400, 404, 429 — unexpected for a ping, but handled conservatively).
                // Map to ServerError rather than SessionExpired so State is consistent with statusCode.
                onComplete?.Invoke(new LootLockerConnectionStateResponse
                {
                    success = false,
                    State = LootLockerConnectionState.ServerError,
                    statusCode = statusCode,
                    text = pingResponse.text,
                    errorData = pingResponse.errorData,
                    requestContext = pingResponse.requestContext,
                });
            });
        }
    }
}
