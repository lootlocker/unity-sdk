using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;


namespace LootLockerRequests
{

    [System.Serializable]
    public class SessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current?.apiKey?.ToString();
        public string platform => LootLockerConfig.current?.platform.ToString();
        public string player_identifier { get; private set; }
        public string game_version => LootLockerConfig.current?.game_version;
        public bool development_mode => LootLockerConfig.current.developmentMode;
        public SessionRequest(string player_identifier)
        {
            this.player_identifier = player_identifier;
        }
    }

    [System.Serializable]
    public class SessionResponse : LootLockerResponse, IStageData
    {
        public bool success { get; set; }
        public string session_token { get; set; }
        public int player_id { get; set; }
        public bool seen_before { get; set; }
        public string public_uid { get; set; }
        public bool check_grant_notifications { get; set; }
        public bool check_deactivation_notifications { get; set; }
        public int[] check_dlcs { get; set; }
        public int xp { get; set; }
        public int level { get; set; }
        public Level_Thresholds level_thresholds { get; set; }
        public int account_balance { get; set; }
    }
    [System.Serializable]
    public class Level_Thresholds
    {
        public int current { get; set; }
        public bool current_is_prestige { get; set; }
        public int next { get; set; }
        public bool next_is_prestige { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void Session(LootLockerGetRequest data, Action<SessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.authenticationRequest;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);
            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
             {
                 SessionResponse response = new SessionResponse();
                 if (string.IsNullOrEmpty(serverResponse.Error))
                 {
                     LootLockerSDKManager.DebugMessage(serverResponse.text);
                     response = JsonConvert.DeserializeObject<SessionResponse>(serverResponse.text);
                     LootLockerConfig.current.UpdateToken(response.session_token, (data as SessionRequest)?.player_identifier);
                     onComplete?.Invoke(response);
                 }
                 else
                 {
                     response.message = serverResponse.message;
                     response.Error = serverResponse.Error;
                     onComplete?.Invoke(response);
                 }
             }, false);
        }

        public static void EndSession(LootLockerGetRequest data, Action<SessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.endingSession;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);
            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                SessionResponse response = new SessionResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<SessionResponse>(serverResponse.text);
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

    }

}