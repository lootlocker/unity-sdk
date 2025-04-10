using System;
using System.Collections.Generic;

namespace LootLocker.Requests
{
    public enum LL_AuthPlatforms
    {
        None
        ,Guest
        ,WhiteLabel
        ,Steam
        ,PlayStationNetwork
        ,XboxOne
        ,NintendoSwitch
        ,AmazonLuna
        ,AppleSignIn
        ,AppleGameCenter
        ,Android
        ,Google
        ,Epic
        ,Meta
        ,Remote
    }
    public struct LL_AuthPlatformRepresentation
    {
        public LL_AuthPlatforms Platform { get; set; }
        public string PlatformString { get; set; }
        public string PlatformFriendlyString { get; set; }
    }

    public class LootLockerAuthPlatformSettings
    {
        public static List<LL_AuthPlatforms> PlatformsWithRefreshTokens = new List<LL_AuthPlatforms> { LL_AuthPlatforms.AppleGameCenter, LL_AuthPlatforms.AppleSignIn, LL_AuthPlatforms.Epic, LL_AuthPlatforms.Google, LL_AuthPlatforms.Remote };
        public static List<LL_AuthPlatforms> PlatformsWithStoredAuthData = new List<LL_AuthPlatforms> { LL_AuthPlatforms.Guest, LL_AuthPlatforms.WhiteLabel, LL_AuthPlatforms.AmazonLuna, LL_AuthPlatforms.PlayStationNetwork, LL_AuthPlatforms.XboxOne };
    }

    public class LootLockerAuthPlatform
    {
        static LootLockerAuthPlatform()
        {
            if (PlatformStrings.Length != Enum.GetNames(typeof(LL_AuthPlatforms)).Length)
            {
                throw new ArrayTypeMismatchException($"A Platform is missing a string representation, {PlatformStrings.Length} vs {Enum.GetNames(typeof(LL_AuthPlatforms)).Length}");
            }

            if (PlatformFriendlyStrings.Length != Enum.GetNames(typeof(LL_AuthPlatforms)).Length)
            {
                throw new ArrayTypeMismatchException($"A Platform is missing a friendly name, {PlatformFriendlyStrings.Length} vs {Enum.GetNames(typeof(LL_AuthPlatforms)).Length}");
            }
        }

        private static readonly string[] PlatformStrings = new[]
        {
            "" // None
            ,"guest" // Guest
            ,"white_label_login" // WhiteLabel
            ,"steam" // Steam
            ,"psn" // PSN
            ,"xbox_one" // XboxOne
            ,"nintendo_switch" // NintendoSwitch
            ,"amazon_luna" // AmazonLuna
            ,"apple_sign_in" // AppleSignIn
            ,"apple_game_center" // Apple Game Center
            ,"android" // Android
            ,"google_sign_in" // Google
            ,"epic_games" // Epic Online Services / Epic Games
            ,"meta" // Meta
            ,"remote" // Remote (leased) session
        };

        private static readonly string[] PlatformFriendlyStrings = new[]
        {
            "None" // None
            ,"Guest" // Guest
            ,"White Label" // WhiteLabel
            ,"Steam" // Steam
            ,"Playstation Network" // PSN
            ,"Xbox One" // XboxOne
            ,"Nintendo Switch" // NintendoSwitch
            ,"Amazon Luna" // AmazonLuna
            ,"Apple Sign In" // AppleSignIn
            ,"Apple Game Center" // Apple Game Center
            ,"Android" // Android
            ,"Google" // Google
            ,"Epic Online Services" // Epic Online Services / Epic Games
            ,"Meta" // Meta
            ,"Remote" // Remote (leased) session
        };

        public static LL_AuthPlatformRepresentation GetPlatformRepresentation(LL_AuthPlatforms platform)
        {
            return new LL_AuthPlatformRepresentation
            {
                Platform = platform,
                PlatformString = PlatformStrings[(int)platform],
                PlatformFriendlyString = PlatformFriendlyStrings[(int)platform]
            };
        }
    }
}