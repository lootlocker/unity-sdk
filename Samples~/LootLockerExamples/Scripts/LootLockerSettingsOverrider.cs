using LootLocker.Requests;

namespace LootLocker {
public static class LootLockerSettingsOverrider
{
    public static void OverrideSettings()
    {
        LootLockerSDKManager.Init("dev_3a04ddea32464ca48226eb821a99e3e4", "0.0.0.1", "2112hpxu");
        LootLocker.LootLockerConfig.current.currentDebugLevel = LootLocker.LootLockerConfig.DebugLevel.All;
    }
}
}
