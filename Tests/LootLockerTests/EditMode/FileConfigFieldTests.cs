using System.Collections.Generic;
using LootLocker;
using NUnit.Framework;
using UnityEngine;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Tests for which fields a config file is allowed to govern.
    ///
    /// Two rules are pinned here: the SDK version always comes from the installed package, and the
    /// log settings do come from the file (they are what makes a "debug config" possible).
    ///
    /// These assert against the real applier rather than a hand-maintained list, so re-adding a
    /// field to <c>ApplyFileConfig</c> actually fails the test that forbids it.
    /// </summary>
    public class FileConfigFieldTests
    {
        private LootLockerConfig _original;

        [SetUp]
        public void CaptureOriginalConfig()
        {
            LootLockerConfig live = LootLockerConfig.current;
            if (live == null)
            {
                return;
            }

            _original = ScriptableObject.CreateInstance<LootLockerConfig>();
            _original.hideFlags = HideFlags.HideAndDontSave;
            _original.apiKey = live.apiKey;
            _original.domainKey = live.domainKey;
            _original.game_version = live.game_version;
            _original.logLevel = live.logLevel;
            _original.logErrorsAsWarnings = live.logErrorsAsWarnings;
            _original.logInBuilds = live.logInBuilds;
            _original.enablePresence = live.enablePresence;
            _original.enablePresenceAutoConnect = live.enablePresenceAutoConnect;
            _original.enablePresenceAutoDisconnectOnFocusChange = live.enablePresenceAutoDisconnectOnFocusChange;
            _original.enablePresenceInEditor = live.enablePresenceInEditor;
            _original.allowTokenRefresh = live.allowTokenRefresh;
            _original.prettifyJson = live.prettifyJson;
            _original.obfuscateLogs = live.obfuscateLogs;
            _original.multiUserSessionMode = live.multiUserSessionMode;
            _original.enableEditorAdminExtension = live.enableEditorAdminExtension;
        }

        [TearDown]
        public void RestoreOriginalConfig()
        {
            if (_original != null)
            {
                LootLockerConfig.CreateNewSettings(_original);
                Object.DestroyImmediate(_original);
                _original = null;
            }
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SdkVersion_IsNotAppliedFromFileConfig()
        {
            // Given: a config file that claims a version
            const string text = @"{
  ""api_key"": ""key"",
  ""sdk_version"": ""0.0.1-stale""
}";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out ExternalFileConfig config, out var fields, out string error);

            // Then: the field still parses (existing files must keep working) but is not applied
            Assert.IsTrue(parsed, error);
            Assert.IsTrue(fields.Contains(LootLockerConfigFields.SdkVersion),
                "The field must still be recognised so old files parse");

            HashSet<string> applied = LootLockerConfig.ApplyFileConfig(config, null);
            Assert.IsFalse(applied.Contains(LootLockerConfigFields.SdkVersion),
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

            HashSet<string> applied = LootLockerConfig.ApplyFileConfig(config, null);
            Assert.IsTrue(applied.Contains(LootLockerConfigFields.LogLevel));
            Assert.IsTrue(applied.Contains(LootLockerConfigFields.LogErrorsAsWarnings));
            Assert.IsTrue(applied.Contains(LootLockerConfigFields.LogInBuilds));

            // And the values really reached the live config, not just the returned set.
            Assert.AreEqual(LootLockerLogger.LogLevel.Verbose, LootLockerConfig.current.logLevel);
            Assert.IsTrue(LootLockerConfig.current.logInBuilds);
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

            string before = LootLockerConfig.current.game_version;
            HashSet<string> applied = LootLockerConfig.ApplyFileConfig(config, null);
            Assert.IsFalse(applied.Contains(LootLockerConfigFields.GameVersion),
                "Game version must stay editable in Project Settings");
            Assert.AreEqual(before, LootLockerConfig.current.game_version,
                "Applying a file config must not change the game version");
        }
    }
}