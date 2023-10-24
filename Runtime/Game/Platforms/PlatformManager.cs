using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LootLocker.Requests
{
    public enum Platforms
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

    public class CurrentPlatform
    {
        static CurrentPlatform()
        {
            if (PlatformStrings.Length != Enum.GetNames(typeof(Platforms)).Length)
            {
                throw new ArrayTypeMismatchException($"A Platform is missing a string representation, {PlatformStrings.Length} vs {Enum.GetNames(typeof(Platforms)).Length}");
            }

            if (PlatformFriendlyStrings.Length != Enum.GetNames(typeof(Platforms)).Length)
            {
                throw new ArrayTypeMismatchException($"A Platform is missing a friendly name, {PlatformFriendlyStrings.Length} vs {Enum.GetNames(typeof(Platforms)).Length}");
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

        public struct PlatformRepresentation
        {
            public Platforms Platform { get; set; }
            public string PlatformString { get; set; }
            public string PlatformFriendlyString { get; set; }
        }

        private static PlatformRepresentation current;

        public override string ToString()
        {
            return current.PlatformString;
        }

        public static Platforms Get()
        {
            return current.Platform;
        }

        public static string GetString()
        {
            return current.PlatformString;
        }

        public static string GetFriendlyString()
        {
            return current.PlatformFriendlyString;
        }

        public static void Set(Platforms platform)
        {
            PlayerPrefs.SetInt("LastActivePlatform", (int)platform);
            current = GetPlatformRepresentation(platform);
        }

        public static void Reset()
        {
            current = GetPlatformRepresentation(Platforms.None);
        }

        public static PlatformRepresentation GetPlatformRepresentation(Platforms platform)
        {
            return new PlatformRepresentation
            {
                Platform = platform,
                PlatformString = PlatformStrings[(int)platform],
                PlatformFriendlyString = PlatformFriendlyStrings[(int)platform]
            };
        }


#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            Reset();
        }
#endif
    }
}