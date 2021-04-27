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
        public string platform => LootLockerConfig.current.platform.ToString();
        public string player_identifier { get; private set; }
        public string game_version => LootLockerConfig.current.game_version;
        public bool development_mode => LootLockerConfig.current.developmentMode;
        public LootLockerSessionRequest(string player_identifier)
        {
            this.player_identifier = player_identifier;
        }
    }

    [System.Serializable]
    public class LootLockerSessionResponse : LootLockerResponse
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
        public LootLockerLevel_Thresholds level_thresholds { get; set; }
        public int account_balance { get; set; }
    }
    [System.Serializable]
    public class LootLockerLevel_Thresholds
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
        public static void Session(LootLockerGetRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.authenticationRequest;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
             {
                 LootLockerSessionResponse response = new LootLockerSessionResponse();
                 if (string.IsNullOrEmpty(serverResponse.Error))
                 {
                     response = JsonConvert.DeserializeObject<LootLockerSessionResponse>(serverResponse.text);
                     LootLockerConfig.current.UpdateToken(response.session_token, (data as LootLockerSessionRequest)?.player_identifier);
                 }

                 //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                 response.text = serverResponse.text;
                      response.status = serverResponse.status;
             response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                 onComplete?.Invoke(response);

             }, false);
        }

        public static void EndSession(LootLockerGetRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.endingSession;

            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerSessionResponse response = new LootLockerSessionResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerSessionResponse>(serverResponse.text);
                
                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.status = serverResponse.status;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

    }

}