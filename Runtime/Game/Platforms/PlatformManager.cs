using System;

namespace LootLocker.Requests
{
    public static class Platforms
    {
        public static string NintendoSwitch = "nintendo_switch";
        public static string Guest = "guest";
        public static string WhiteLabel = "white_label";
        public static string Steam = "white_label";
    }

    public static class PlatformManager
    {
        private static string CurrentPlatform { get; set; }
    }
}