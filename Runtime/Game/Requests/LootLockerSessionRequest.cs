﻿using Newtonsoft.Json;
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
        public string platform => LootLockerConfig.current.platform.ToString();
        public string player_identifier { get; private set; }
        public string game_version => LootLockerConfig.current.game_version;
        public bool development_mode => LootLockerConfig.current.developmentMode;

        public LootLockerSessionRequest(string player_identifier)
        {
            this.player_identifier = player_identifier;
        }

        public LootLockerSessionRequest()
        {
        }
    }

    [System.Serializable]
    public class LootLockerSteamSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string platform => "Steam";
        public string player_identifier { get; private set; }
        public string game_version => LootLockerConfig.current.game_version;
        public bool development_mode => LootLockerConfig.current.developmentMode;

        public LootLockerSteamSessionRequest(string player_identifier)
        {
            this.player_identifier = player_identifier;
        }

        public LootLockerSteamSessionRequest()
        {
        }
    }

    [System.Serializable]
    public class LootLockerWhiteLabelSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string email { get; set; }
        public string password { get; set; }
        public string token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;
        public bool development_mode => LootLockerConfig.current.developmentMode;

        public LootLockerWhiteLabelSessionRequest(string email, string password, string token)
        {
            this.email = email;
            this.password = password;
            this.token = token;
        }

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
        public bool development_mode => LootLockerConfig.current.developmentMode;

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
        public bool development_mode => LootLockerConfig.current.developmentMode;

        public LootLockerXboxOneSessionRequest(string xbox_user_token)
        {
            this.xbox_user_token = xbox_user_token;
        }
    }

    public class LootLockerAppleSignInSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string apple_user_token { get; private set; }
        public string game_version => LootLockerConfig.current.game_version;
        public bool development_mode => LootLockerConfig.current.developmentMode;

        public LootLockerAppleSignInSessionRequest(string apple_user_token)
        {
            this.apple_user_token = apple_user_token;
        }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void Session(LootLockerGetRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.authenticationRequest;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Serialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, (data as LootLockerSessionRequest)?.player_identifier);
                onComplete?.Invoke(response);
            }, false);
        }

        public static void WhiteLabelSession(LootLockerWhiteLabelSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelLoginSessionRequest;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Serialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, "");
                onComplete?.Invoke(response);
            }, false);
        }

        public static void GuestSession(LootLockerGetRequest data, Action<LootLockerGuestSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.guestSessionRequest;

            string json = "";
            if (data == null)
            {
                return;
            }

            json = JsonConvert.SerializeObject(data);
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
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Serialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, "");
                onComplete?.Invoke(response);
            }, false);
        }

        public static void AppleSession(LootLockerAppleSignInSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.appleSessionRequest;

            string json = "";
            if (data == null)
            {
                return;
            }

            json = JsonConvert.SerializeObject(data);
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Serialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.UpdateToken(response.session_token, "");
                onComplete?.Invoke(response);
            }, false);
        }

        public static void EndSession(LootLockerGetRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.endingSession;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); });
        }
    }
}