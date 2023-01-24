using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    [System.Serializable]
    public class LootLockerSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string platform => CurrentPlatform.GetString();
        public string player_identifier { get; private set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerSessionRequest(string player_identifier)
        {
            this.player_identifier = player_identifier;
        }

        public LootLockerSessionRequest()
        {
        }
    }

    [System.Serializable]
    public class LootLockerWhiteLabelSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string email { get; set; }
        public string password { get; set; } // DEPRECATED PARAMETER
        public string token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        [Obsolete("StartWhiteLabelSession with password is deprecated")]
        public LootLockerWhiteLabelSessionRequest(string email, string password, string token)
        {
            this.email = email;
            this.password = password;
            this.token = token;
        }

        [Obsolete("StartWhiteLabelSession with password is deprecated")]
        public LootLockerWhiteLabelSessionRequest(string email, string password)
        {
            this.email = email;
            this.password = password;
            this.token = null;
        }

        public LootLockerWhiteLabelSessionRequest(string email)
        {
            this.email = email;
            this.password = null;
            this.token = null;
        }

        public LootLockerWhiteLabelSessionRequest()
        {
            this.email = null;
            this.password = null;
            this.token = null;
        }
    }

    [System.Serializable]
    public class LootLockerSessionResponse : LootLockerResponse
    {
        public string session_token { get; set; }
        public int player_id { get; set; }
        public bool seen_before { get; set; }
        public string public_uid { get; set; }
        public DateTime player_created_at { get; set; }
        public bool check_grant_notifications { get; set; }
        public bool check_deactivation_notifications { get; set; }
        public int[] check_dlcs { get; set; }
        public int xp { get; set; }
        public int level { get; set; }
        public LootLockerLevel_Thresholds level_thresholds { get; set; }
        public int account_balance { get; set; }
    }

    public class LootLockerGuestSessionResponse : LootLockerSessionResponse
    {
        public string player_identifier { get; set; }
    }

    [System.Serializable]
    public class LootLockerAppleSessionResponse : LootLockerSessionResponse
    {
        public string player_identifier { get; set; }
        public string refresh_token { get; set; }
    }

    [System.Serializable]
    public class LootLockerLevel_Thresholds
    {
        public int current { get; set; }
        public bool current_is_prestige { get; set; }
        public int? next { get; set; }
        public bool next_is_prestige { get; set; }
    }

    [System.Serializable]
    public class LootLockerNintendoSwitchSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string nsa_id_token { get; private set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerNintendoSwitchSessionRequest(string nsa_id_token)
        {
            this.nsa_id_token = nsa_id_token;
        }
    }

    public class LootLockerXboxOneSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string xbox_user_token { get; private set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerXboxOneSessionRequest(string xbox_user_token)
        {
            this.xbox_user_token = xbox_user_token;
        }
    }

    public class LootLockerAppleSignInSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string apple_authorization_code { get; private set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerAppleSignInSessionRequest(string apple_authorization_code)
        {
            this.apple_authorization_code = apple_authorization_code;
        }
    }

    public class LootLockerAppleRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string refresh_token { get; private set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerAppleRefreshSessionRequest(string refresh_token)
        {
            this.refresh_token = refresh_token;
        }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void Session(LootLockerSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.authenticationRequest;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Serialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, data?.player_identifier);
                onComplete?.Invoke(response);
            }, false);
        }

        public static void WhiteLabelSession(LootLockerWhiteLabelSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelLoginSessionRequest;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Serialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, "");
                onComplete?.Invoke(response);
            }, false);
        }

        public static void GuestSession(LootLockerSessionRequest data, Action<LootLockerGuestSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.guestSessionRequest;

            string json = "";
            if (data == null)
            {
                return;
            }

            json = JsonConvert.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Serialize<LootLockerGuestSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, (data as LootLockerSessionRequest)?.player_identifier);
                onComplete?.Invoke(response);
            }, false);
        }

        public static void NintendoSwitchSession(LootLockerNintendoSwitchSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.nintendoSwitchSessionRequest;

            string json = "";
            if (data == null)
            {
                return;
            }

            json = JsonConvert.SerializeObject(data);

            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Serialize<LootLockerGuestSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, "");
                onComplete?.Invoke(response);
            }, false);
        }

        public static void XboxOneSession(LootLockerXboxOneSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.xboxSessionRequest;

            string json = "";
            if (data == null)
            {
                return;
            }

            json = JsonConvert.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Serialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, "");
                onComplete?.Invoke(response);
            }, false);
        }

        public static void AppleSession(LootLockerAppleSignInSessionRequest data, Action<LootLockerAppleSessionResponse> onComplete)
        {
            if (data == null)
            {
                return;
            }

            string json = JsonConvert.SerializeObject(data);
            AppleSession(json, onComplete);
        }

        public static void AppleSession(LootLockerAppleRefreshSessionRequest data, Action<LootLockerAppleSessionResponse> onComplete)
        {
            if (data == null)
            {
                return;
            }

            string json = JsonConvert.SerializeObject(data);
            AppleSession(json, onComplete);
        }

        private static void AppleSession(string json, Action<LootLockerAppleSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.appleSessionRequest;
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerAppleSessionResponse.Serialize<LootLockerAppleSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, response.player_identifier);
                onComplete?.Invoke(response);
            }, false);
        }

        public static void EndSession(LootLockerSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.endingSession;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }
    }
}