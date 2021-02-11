using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    [CreateAssetMenu(fileName = "LootLockerEndPointsAdmin", menuName = "ScriptableObjects/LootLockerEndPointsAdmin", order = 3)]
    public class LootLockerEndPointsAdmin : RuntimeProjectSettings<LootLockerEndPointsAdmin>
    {
        public override string SettingName { get { return "LootLockerEndPointsAdmin"; } }

        private static LootLockerEndPointsAdmin _current;

        public static LootLockerEndPointsAdmin current
        {
            get
            {
                if (_current == null)
                {
                    _current = Get();
                }

                return _current;
            }
        }

        //Authentication
        [Header("Authentication Endpoints")]
        public EndPointClass initialAuthenticationRequest = new EndPointClass("v1/session", LootLockerHTTPMethod.POST);
        public EndPointClass twoFactorAuthenticationCodeVerification= new EndPointClass("v1/2fa", LootLockerHTTPMethod.POST);
        public EndPointClass subsequentRequests = new EndPointClass("v1/games", LootLockerHTTPMethod.GET);

        //Games
        [Header("Games Endpoints")]
        [Header("---------------------------")]
        public EndPointClass getAllGamesToTheCurrentUser = new EndPointClass("v1/games", LootLockerHTTPMethod.GET);
        public EndPointClass creatingAGame = new EndPointClass("v1/game", LootLockerHTTPMethod.POST);
        public EndPointClass getDetailedInformationAboutAGame = new EndPointClass("v1/game/{0}", LootLockerHTTPMethod.GET);
        public EndPointClass updatingInformationAboutAGame = new EndPointClass("v1/game/{0}", LootLockerHTTPMethod.PATCH);
        public EndPointClass deletingGames = new EndPointClass("v1/game/{0}", LootLockerHTTPMethod.DELETE);

        //Players
        [Header("Players Endpoints")]
        [Header("---------------------------")]
        public EndPointClass searchingForPlayers = new EndPointClass("v1/game/{0}/players", LootLockerHTTPMethod.GET);

        //Maps
        [Header("Maps Endpoints")]
        [Header("---------------------------")]
        public EndPointClass gettingAllMapsToAGame = new EndPointClass("v1/maps/game/{0}", LootLockerHTTPMethod.GET);
        public EndPointClass creatingMaps = new EndPointClass("v1/maps", LootLockerHTTPMethod.POST);
        public EndPointClass updatingMaps = new EndPointClass("v1/maps/{0}", LootLockerHTTPMethod.PATCH);

        //Events
        [Header("Events Endpoints")]
        [Header("---------------------------")]
        public EndPointClass creatingEvent = new EndPointClass("v1/events", LootLockerHTTPMethod.POST);
        public EndPointClass updatingEvent = new EndPointClass("v1/events/{0}", LootLockerHTTPMethod.PATCH);
        public EndPointClass gettingAllEvents = new EndPointClass("v1/events/game/{0}", LootLockerHTTPMethod.GET);

        //Triggers
        [Header("Triggers")]
        [Header("---------------------------")]
        public EndPointClass listTriggers = new EndPointClass("v1/game/{0}/triggers", LootLockerHTTPMethod.GET);
        public EndPointClass createTriggers = new EndPointClass("v1/game/{0}/triggers", LootLockerHTTPMethod.POST);
        public EndPointClass updateTriggers = new EndPointClass("v1/game/{0}/triggers/{1}", LootLockerHTTPMethod.PATCH);
        public EndPointClass deleteTriggers = new EndPointClass("v1/game/{0}/triggers/{1}", LootLockerHTTPMethod.DELETE);

        //Files
        [Header("Files Endpoints")]
        [Header("---------------------------")]
        public EndPointClass uploadFile = new EndPointClass("v1/upload", LootLockerHTTPMethod.UPLOAD);
        public EndPointClass getFiles = new EndPointClass("v1/game/{0}/files", LootLockerHTTPMethod.GET);
        public EndPointClass updateFile = new EndPointClass("v1/game/{0}/files/{1}", LootLockerHTTPMethod.PATCH);
        public EndPointClass deleteFile = new EndPointClass("v1/game/{0}/files/{1}", LootLockerHTTPMethod.DELETE);

        //Assets
        [Header("Assets Endpoints")]
        [Header("---------------------------")]
        public EndPointClass createAsset = new EndPointClass("v1/asset", LootLockerHTTPMethod.POST);
        public EndPointClass getContexts = new EndPointClass("v1/game/{0}/assets/contexts", LootLockerHTTPMethod.GET);
        public EndPointClass getAllAssets = new EndPointClass("v1/game/{0}/assets", LootLockerHTTPMethod.GET);

        //User
        [Header("User Endpoints")]
        [Header("---------------------------")]
        public EndPointClass setupTwoFactorAuthentication = new EndPointClass("v1/user/2fa", LootLockerHTTPMethod.POST);
        public EndPointClass verifyTwoFactorAuthenticationSetup = new EndPointClass("v1/user/2fa", LootLockerHTTPMethod.PATCH);
        public EndPointClass removeTwoFactorAuthentication = new EndPointClass("v1/user/2fa", LootLockerHTTPMethod.DELETE);

        //Organisations
        [Header("Organisations")]
        public EndPointClass getUsersToAnOrganisation = new EndPointClass("v1/organisation/{0}/users", LootLockerHTTPMethod.GET);

    }
}
