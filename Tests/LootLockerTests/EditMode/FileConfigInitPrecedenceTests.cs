using LootLocker;
using NUnit.Framework;
using UnityEngine;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Tests for the precedence of an explicit <see cref="LootLockerConfig.CreateNewSettings(string, string, string, LootLockerLogger.LogLevel, bool, bool, bool, bool, bool, bool, bool, bool, bool, LootLockerMultiUserSessionMode, bool)"/>
    /// call over file-based configuration.
    ///
    /// These tests deliberately do not create a file config on disk. They pin the contract that
    /// matters: an explicit call must never be silently discarded, which is what used to happen
    /// whenever a file config was active.
    /// </summary>
    public class FileConfigInitPrecedenceTests
    {
        private LootLockerConfig _original;

        [SetUp]
        public void CaptureOriginalConfig()
        {
            // A copy, not the live object. LootLockerConfig.current is the asset instance itself,
            // so holding a reference to it and copying it back in TearDown would copy the object
            // onto itself and restore nothing - the values these tests set would leak into every
            // later test in the session.
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
        public void InitCall_AppliesItsValues()
        {
            // Given / When
            bool applied = LootLockerConfig.CreateNewSettings(
                apiKey: "init_key",
                gameVersion: "1.2.3",
                domainKey: "init_domain",
                logLevel: LootLockerLogger.LogLevel.Verbose);

            // Then
            Assert.IsTrue(applied, "An explicit Init call must report success");
            Assert.AreEqual("init_key", LootLockerConfig.current.apiKey);
            Assert.AreEqual("init_domain", LootLockerConfig.current.domainKey);
            Assert.AreEqual("1.2.3", LootLockerConfig.current.game_version);
            Assert.AreEqual(LootLockerLogger.LogLevel.Verbose, LootLockerConfig.current.logLevel);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void InitCall_IsNotBlockedByFileConfigState()
        {
            // Given: the guard that used to discard explicit calls is gone. This asserts the
            // contract directly rather than by constructing a file config, because the guard was
            // the bug: it returned false and threw the caller's values away.
            // When
            bool applied = LootLockerConfig.CreateNewSettings("init_key", "1.0.0", "init_domain");

            // Then
            Assert.IsTrue(applied);
            Assert.AreEqual("init_key", LootLockerConfig.current.apiKey);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void InitCall_AppliesToTheLiveConfigOnly()
        {
            // Given: the authored values of the asset, captured before anything is applied
            LootLockerConfig.RestoreAssetValues();
            string authoredKey = LootLockerConfig.current.apiKey;

            // When: an explicit call overrides it at runtime
            LootLockerConfig.CreateNewSettings("runtime_key", "1.0.0", "runtime_domain");

            // Then: the live value changed...
            Assert.AreEqual("runtime_key", LootLockerConfig.current.apiKey);

            // ...but the authored values are untouched, so restoring them discards the Init value.
            // This is the property that matters: Init is process-lifetime only and must never
            // become the value that gets saved back to the committed asset.
            LootLockerConfig.RestoreAssetValues();
            Assert.AreEqual(authoredKey, LootLockerConfig.current.apiKey,
                "Init must not overwrite the authored asset values");
            Assert.AreNotEqual("runtime_key", LootLockerConfig.current.apiKey,
                "The Init value must not survive a restore of the authored values");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void InitCall_WithNullConfig_ReturnsFalse()
        {
            // Given / When
            bool applied = LootLockerConfig.CreateNewSettings((LootLockerConfig)null);

            // Then
            Assert.IsFalse(applied, "A null config must be rejected rather than throwing");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void InitCall_WithConfigObject_AppliesItsValues()
        {
            // Given
            LootLockerConfig source = ScriptableObject.CreateInstance<LootLockerConfig>();
            source.apiKey = "object_key";
            source.domainKey = "object_domain";
            source.game_version = "2.0.0";
            source.logLevel = LootLockerLogger.LogLevel.Warning;

            try
            {
                // When
                bool applied = LootLockerConfig.CreateNewSettings(source);

                // Then
                Assert.IsTrue(applied);
                Assert.AreEqual("object_key", LootLockerConfig.current.apiKey);
                Assert.AreEqual("object_domain", LootLockerConfig.current.domainKey);
                Assert.AreEqual("2.0.0", LootLockerConfig.current.game_version);
                Assert.AreEqual(LootLockerLogger.LogLevel.Warning, LootLockerConfig.current.logLevel);
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }
    }
}
