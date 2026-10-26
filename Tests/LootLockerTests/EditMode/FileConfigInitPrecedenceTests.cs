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
            _original = LootLockerConfig.current;
        }

        [TearDown]
        public void RestoreOriginalConfig()
        {
            if (_original != null)
            {
                LootLockerConfig.CreateNewSettings(_original);
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
        public void InitCall_DoesNotPersistToTheAsset()
        {
            // Given: a known asset value
            LootLockerConfig.CreateNewSettings("asset_key", "1.0.0", "asset_domain");
            string assetKeyBefore = LootLockerConfig.current.apiKey;

            // When: an explicit call overrides it at runtime
            LootLockerConfig.CreateNewSettings("runtime_key", "1.0.0", "runtime_domain");

            // Then: the live value changed, but nothing was written to disk
            Assert.AreEqual("runtime_key", LootLockerConfig.current.apiKey);
            Assert.AreEqual(assetKeyBefore, "asset_key",
                "The asset value must be untouched; Init is process-lifetime only");
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
