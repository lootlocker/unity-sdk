using System;

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
        };

        private static Platforms currentPlatform;
        private static string currentPlatformString;
        private static string currentPlatformFriendlyString;

        public override string ToString()
        {
            return currentPlatformString;
        }

        public static Platforms Get()
        {
            return currentPlatform;
        }

        public static string GetString()
        {
            return currentPlatformString;
        }

        public static string GetFriendlyString()
        {
            return currentPlatformFriendlyString;
        }

        public static void Set(Platforms platform)
        {
            currentPlatform = platform;
            currentPlatformString = PlatformStrings[(int)currentPlatform];
            currentPlatformFriendlyString = PlatformFriendlyStrings[(int)currentPlatform];
        }

        public static void Reset()
        {
            currentPlatform = Platforms.None;
            currentPlatformString = PlatformStrings[(int)currentPlatform];
            currentPlatformFriendlyString = PlatformFriendlyStrings[(int)currentPlatform];
        }

        // TODO: Deprecated, remove in version 1.2.0
        public static void Set(LootLockerConfig.platformType platform)
        {
            switch (platform)
            {
                case LootLockerConfig.platformType.Android:
                    Set(Platforms.Guest);
                    break;
                case LootLockerConfig.platformType.iOS:
                    Set(Platforms.AppleSignIn);
                    break;
                case LootLockerConfig.platformType.Steam:
                    Set(Platforms.Steam);
                    break;
                case LootLockerConfig.platformType.PlayStationNetwork:
                    Set(Platforms.PlayStationNetwork);
                    break;
                case LootLockerConfig.platformType.Unused:
                default:
                    Set(Platforms.None);
                    break;
            }
        }
    }
}