using UnityEngine;
using LootLocker.LootLockerEnums;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER
namespace LootLocker
{
    public class LootLockerAdminEndPoints
    {
        [Header("API Keys")]
        public static EndPointClass adminExtensionGetAllKeys = new EndPointClass("game/{0}/api_keys", LootLockerHTTPMethod.GET, LootLockerCallerRole.Admin);
        public static EndPointClass adminExtensionCreateKey = new EndPointClass("game/{0}/api_keys", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);

        [Header("Admin Authentication")]
        public static EndPointClass adminExtensionLogin = new EndPointClass("v1/session", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);
        public static EndPointClass adminExtensionMFA = new EndPointClass("v1/2fa", LootLockerHTTPMethod.POST, LootLockerCallerRole.Admin);

        [Header("User Information")]
        public static EndPointClass adminExtensionUserInformation = new EndPointClass("v1/user/all", LootLockerHTTPMethod.GET, LootLockerCallerRole.Admin);
        public static EndPointClass adminExtensionGetUserRole = new EndPointClass("roles/{0}", LootLockerHTTPMethod.GET, LootLockerCallerRole.Admin);
        public static EndPointClass adminExtensionGetGameInformation = new EndPointClass("v1/game/{0}", LootLockerHTTPMethod.GET, LootLockerCallerRole.Admin);
    }
}
#endif