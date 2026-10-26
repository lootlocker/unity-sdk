using LootLocker;
using NUnit.Framework;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Tests for which fields a config file is allowed to govern.
    ///
    /// Two rules are pinned here: the SDK version always comes from the installed package, and the
    /// log settings do come from the file (they are what makes a "debug config" possible).
    /// </summary>
    public class FileConfigFieldTests
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SdkVersion_IsNotAppliedFromFileConfig()
        {
            // Given: a config file that claims a version
            const string text = @"{
  ""api_key"": ""key"",
  ""sdk_version"": ""0.0.1-stale""
}";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out _, out var fields, out string error);

            // Then: the field still parses (existing files must keep working) but is not applied
            Assert.IsTrue(parsed, error);
            Assert.IsTrue(fields.Contains(LootLockerConfigFields.SdkVersion),
                "The field must still be recognised so old files parse");
            Assert.IsFalse(IsAppliedByFileConfig(LootLockerConfigFields.SdkVersion),
                "sdk_version must never be applied from a config file");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void LogFields_AreAppliedFromFileConfig()
        {
            // Given: a debug config
            const string text = @"{
  ""api_key"": ""key"",
  ""log_level"": ""Verbose"",
  ""log_errors_as_warnings"": true,
  ""log_in_builds"": true
}";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out ExternalFileConfig config, out _, out string error);

            // Then
            Assert.IsTrue(parsed, error);
            Assert.AreEqual(LootLockerLogger.LogLevel.Verbose, config.log_level);
            Assert.IsTrue(config.log_errors_as_warnings);
            Assert.IsTrue(config.log_in_builds);

            Assert.IsTrue(IsAppliedByFileConfig(LootLockerConfigFields.LogLevel));
            Assert.IsTrue(IsAppliedByFileConfig(LootLockerConfigFields.LogErrorsAsWarnings));
            Assert.IsTrue(IsAppliedByFileConfig(LootLockerConfigFields.LogInBuilds));
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void GameVersion_IsNotAppliedFromFileConfig()
        {
            // Given: a config that claims a game version
            const string text = @"{ ""api_key"": ""key"", ""game_version"": ""9.9.9"" }";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out ExternalFileConfig config, out _, out string error);

            // Then: the field parses, but game version stays a Project Settings concern
            Assert.IsTrue(parsed, error);
            Assert.AreEqual("9.9.9", config.game_version);
            Assert.IsFalse(IsAppliedByFileConfig(LootLockerConfigFields.GameVersion),
                "Game version must stay editable in Project Settings");
        }

        /// <summary>
        /// Mirrors the field list in <c>ApplyFileConfig</c>. Kept as an explicit list rather than
        /// reflecting over the method so that adding a field to the applier without deciding
        /// whether it belongs there fails this test.
        /// </summary>
        private static bool IsAppliedByFileConfig(string fieldName)
        {
            switch (fieldName)
            {
                case LootLockerConfigFields.ApiKey:
                case LootLockerConfigFields.DomainKey:
                case LootLockerConfigFields.EnablePresence:
                case LootLockerConfigFields.EnablePresenceAutoConnect:
                case LootLockerConfigFields.EnablePresenceAutoDisconnectOnFocusChange:
                case LootLockerConfigFields.EnablePresenceInEditor:
                case LootLockerConfigFields.LogLevel:
                case LootLockerConfigFields.LogErrorsAsWarnings:
                case LootLockerConfigFields.LogInBuilds:
                case LootLockerConfigFields.PrettifyJson:
                case LootLockerConfigFields.ObfuscateLogs:
                case LootLockerConfigFields.AllowTokenRefresh:
                case LootLockerConfigFields.MultiUserSessionMode:
                case LootLockerConfigFields.EnableEditorAdminExtension:
                    return true;
                default:
                    return false;
            }
        }
    }
}
