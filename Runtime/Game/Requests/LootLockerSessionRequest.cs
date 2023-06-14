using System;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    [Serializable]
    public class LootLockerSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string platform => CurrentPlatform.GetString();
        public string player_identifier { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerSessionRequest(string player_identifier)
        {
            this.player_identifier = player_identifier;
        }

        public LootLockerSessionRequest()
        {
        }
    }

    [Serializable]
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

    [Serializable]
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

    [Serializable]
    public class LootLockerGoogleSessionResponse : LootLockerSessionResponse
    {
        public string player_identifier { get; set; }
        public string refresh_token { get; set; }
    }

    [Serializable]
    public class LootLockerAppleSessionResponse : LootLockerSessionResponse
    {
        public string player_identifier { get; set; }
        public string refresh_token { get; set; }
    }

    [System.Serializable]
    public class LootLockerAppleGameCenterSessionResponse : LootLockerSessionResponse
    {
        public string player_name { get; set; }
        public string refresh_token { get; set; }
    }

    [System.Serializable]
    public class LootLockerEpicSessionResponse : LootLockerSessionResponse
    {
        public string refresh_token { get; set; }
    }

    [Serializable]
    public class LootLockerLevel_Thresholds
    {
        public int current { get; set; }
        public bool current_is_prestige { get; set; }
        public int? next { get; set; }
        public bool next_is_prestige { get; set; }
    }

    [Serializable]
    public class LootLockerNintendoSwitchSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string nsa_id_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerNintendoSwitchSessionRequest(string nsa_id_token)
        {
            this.nsa_id_token = nsa_id_token;
        }
    }

    [Serializable]
    public class LootLockerEpicSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string id_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerEpicSessionRequest(string id_token)
        {
            this.id_token = id_token;
        }
    }

    [Serializable]
    public class LootLockerEpicRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string refresh_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerEpicRefreshSessionRequest(string refresh_token)
        {
            this.refresh_token = refresh_token;
        }
    }

    public class LootLockerXboxOneSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string xbox_user_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerXboxOneSessionRequest(string xbox_user_token)
        {
            this.xbox_user_token = xbox_user_token;
        }
    }

    public class LootLockerGoogleSignInSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string id_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerGoogleSignInSessionRequest(string id_token)
        {
            this.id_token = id_token;
        }
    }
    
    public enum GooglePlatform
    {
        web, android, ios, desktop
    }

    public class LootLockerGoogleSignInWithPlatformSessionRequest : LootLockerGoogleSignInSessionRequest
    {
        public string platform { get; set; }

        public LootLockerGoogleSignInWithPlatformSessionRequest(string id_token) : base(id_token)
        {
            this.id_token = id_token;
        }
    }

    public class LootLockerGoogleRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string refresh_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerGoogleRefreshSessionRequest(string refresh_token)
        {
            this.refresh_token = refresh_token;
        }
    }

    public class LootLockerAppleSignInSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string apple_authorization_code { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerAppleSignInSessionRequest(string apple_authorization_code)
        {
            this.apple_authorization_code = apple_authorization_code;
        }
    }

    public class LootLockerAppleRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string refresh_token { get; set; }
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerAppleRefreshSessionRequest(string refresh_token)
        {
            this.refresh_token = refresh_token;
        }
    }

    public class LootLockerAppleGameCenterSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string game_version => LootLockerConfig.current.game_version;
        public string bundle_id { get; private set; }
        public string player_id { get; private set; }
        public string public_key_url { get; private set; }
        public string signature { get; private set; }
        public string salt { get; private set; }
        public long timestamp { get; private set; }

        public LootLockerAppleGameCenterSessionRequest(string bundleId, string playerId, string publicKeyUrl, string signature, string salt, long timestamp)
        {
            this.bundle_id = bundleId;
            this.player_id = playerId;
            this.public_key_url = publicKeyUrl;
            this.signature = signature;
            this.salt = salt;
            this.timestamp = timestamp;
        }
    }

    public class LootLockerAppleGameCenterRefreshSessionRequest : LootLockerGetRequest
    {
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        public string game_version => LootLockerConfig.current.game_version;
        public string refresh_token { get; private set; }

        public LootLockerAppleGameCenterRefreshSessionRequest(string refresh_token)
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

            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerSessionResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.deviceID = data?.player_identifier;
                onComplete?.Invoke(response);
            }, false);
        }

        public static void WhiteLabelSession(LootLockerWhiteLabelSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.whiteLabelLoginSessionRequest;

            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerSessionResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.deviceID = "";
                onComplete?.Invoke(response);
            }, false);
        }

        public static void GuestSession(LootLockerSessionRequest data, Action<LootLockerGuestSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.guestSessionRequest;

            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGuestSessionResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerGuestSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.deviceID = (data as LootLockerSessionRequest)?.player_identifier;
                onComplete?.Invoke(response);
            }, false);
        }

        public static void GoogleSession(LootLockerGoogleSignInSessionRequest data, Action<LootLockerGoogleSessionResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGoogleSessionResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);
            GoogleSession(json, onComplete);
        }

        public static void GoogleSession(LootLockerGoogleRefreshSessionRequest data, Action<LootLockerGoogleSessionResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerGoogleSessionResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);
            GoogleSession(json, onComplete);
        }

        private static void GoogleSession(string json, Action<LootLockerGoogleSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.googleSessionRequest;
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerGoogleSessionResponse.Deserialize<LootLockerGoogleSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.deviceID = response.player_identifier;
                LootLockerConfig.current.refreshToken = response.refresh_token;
                onComplete?.Invoke(response);
            }, false);
        }

        public static void NintendoSwitchSession(LootLockerNintendoSwitchSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.nintendoSwitchSessionRequest;

            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerSessionResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.deviceID = "";
                onComplete?.Invoke(response);
            }, false);
        }

        public static void EpicSession(LootLockerEpicSessionRequest data, Action<LootLockerEpicSessionResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerEpicSessionResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);
            EpicSession(json, onComplete);
        }

        public static void EpicSession(LootLockerEpicRefreshSessionRequest data, Action<LootLockerEpicSessionResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerEpicSessionResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);
            EpicSession(json, onComplete);
        }

        private static void EpicSession(string json, Action<LootLockerEpicSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.epicSessionRequest;
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerEpicSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.refreshToken = response.refresh_token;
                LootLockerConfig.current.deviceID = "";
                onComplete?.Invoke(response);
            }, false);
        }

        public static void XboxOneSession(LootLockerXboxOneSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.xboxSessionRequest;

            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerSessionResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.deviceID = "";
                onComplete?.Invoke(response);
            }, false);
        }

        public static void AppleSession(LootLockerAppleSignInSessionRequest data, Action<LootLockerAppleSessionResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAppleSessionResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);
            AppleSession(json, onComplete);
        }

        public static void AppleSession(LootLockerAppleRefreshSessionRequest data, Action<LootLockerAppleSessionResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAppleSessionResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);
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
                var response = LootLockerAppleSessionResponse.Deserialize<LootLockerAppleSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.deviceID = response.player_identifier;
                LootLockerConfig.current.refreshToken = response.refresh_token;
                onComplete?.Invoke(response);
            }, false);
        }

        public static void AppleGameCenterSession(LootLockerAppleGameCenterSessionRequest data, Action<LootLockerAppleGameCenterSessionResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAppleGameCenterSessionResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);
            AppleGameCenterSession(json, onComplete);
        }

        public static void AppleGameCenterSession(LootLockerAppleGameCenterRefreshSessionRequest data, Action<LootLockerAppleGameCenterSessionResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAppleGameCenterSessionResponse>());
                return;
            }

            string json = LootLockerJson.SerializeObject(data);
            AppleGameCenterSession(json, onComplete);
        }

        private static void AppleGameCenterSession(string json, Action<LootLockerAppleGameCenterSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.appleGameCenterSessionRequest;
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0
            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                var response = LootLockerAppleGameCenterSessionResponse.Deserialize<LootLockerAppleGameCenterSessionResponse>(serverResponse);
                LootLockerConfig.current.token = response.session_token;
                LootLockerConfig.current.refreshToken = response.refresh_token;
                onComplete?.Invoke(response);
            }, false);
        }

        public static void EndSession(LootLockerSessionRequest data, Action<LootLockerSessionResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.endingSession;

            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerAppleSessionResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);
            LootLockerConfig.AddDevelopmentModeFieldToJsonStringIfNeeded(ref json); // TODO: Deprecated, remove in version 1.2.0

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}