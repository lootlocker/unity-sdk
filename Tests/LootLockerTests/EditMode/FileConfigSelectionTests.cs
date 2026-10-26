using System.Collections.Generic;
using LootLocker;
using NUnit.Framework;
using UnityEngine;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Tests for the selection field that drives which file config is used.
    ///
    /// The deeper resolution logic is covered by <see cref="FileConfigResolutionTests"/>; these
    /// tests lock down the backwards-compatibility guarantee that matters most: an asset created
    /// before the field existed must behave exactly as it did before.
    /// </summary>
    public class FileConfigSelectionTests
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Selection_DefaultsToNull_ForBackwardsCompatibility()
        {
            // Given / When: a freshly created asset, i.e. what an existing project deserializes to
            LootLockerConfig config = ScriptableObject.CreateInstance<LootLockerConfig>();

            try
            {
                // Then: null means "auto-detect", which is the pre-existing behaviour
                Assert.IsNull(config.selectedFileConfigId,
                    "A new asset must default to auto-detect so existing projects are unaffected");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Selection_NullResolvesToLegacyFileWhenPresent()
        {
            // Given: an untouched asset and a legacy config file on disk
            // When
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: null,
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: true,
                availableIds: new[] { "prod", "debug" });

            // Then
            Assert.AreEqual("", resolved,
                "With no explicit selection the legacy file must still win, even when profiles exist");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Selection_ExplicitNoneIsDistinctFromAutoDetect()
        {
            // Given: the two ways to express "no selection"
            string autoDetect = LootLockerConfig.ResolveFileConfigId(null, null, null, true, new string[0]);
            string explicitNone = LootLockerConfig.ResolveFileConfigId("", null, null, true, new string[0]);

            // Then: null follows the legacy file, "" opts out entirely
            Assert.AreEqual("", autoDetect, "null must auto-detect the legacy file");
            Assert.IsNull(explicitNone, "\"\" must opt out of file config entirely");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Selection_SelectedProfileIsUsedEvenWhenLegacyFileExists()
        {
            // Given: both a legacy file and an explicit selection
            // When
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: "prod",
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: true,
                availableIds: new[] { "prod" });

            // Then
            Assert.AreEqual("prod", resolved, "An explicit selection must beat the legacy file");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Selection_SingleInvalidConfigIsNotAutoDetected()
        {
            // Regression: DiscoverAvailableIds returned every discovered file, including broken
            // ones, so a lone broken config resolved through the single-file auto-detect step and
            // was applied - while the UI said a broken file is shown but cannot be selected.
            //
            // Exercise the real discovery pipeline rather than a hand-built id list: one broken
            // file must yield no auto-detectable ids at all.
            List<LootLockerConfig.FileConfigDescriptor> discovered = LootLockerConfig.DiscoverFileConfigs(
                () => new[] { "LootLockerConfig-broken.bytes" },
                _ => "{ \"api_key\": ");

            Assert.AreEqual(1, discovered.Count, "Precondition: the broken file is discovered");
            Assert.AreEqual(LootLockerConfig.FileConfigState.Invalid, discovered[0].State,
                "Precondition: the broken file is classified as invalid");

            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: null,
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: false,
                availableIds: new string[0]);

            Assert.IsNull(resolved, "Auto-detect must not select a file that is not usable");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void RestoreAssetValues_DiscardsAppliedFileConfigValues()
        {
            // Regression: ReloadConfigAsset used to call Resources.Load, which returns the already
            // loaded instance - the very object that had been mutated. Restoring was therefore a
            // no-op and switching configs left the previous config's values in place for every
            // field the new one omitted.
            LootLockerConfig config = LootLockerConfig.current;
            if (config == null)
            {
                Assert.Ignore("No config asset available in this project");
            }

            LootLockerConfig.RestoreAssetValues();
            string authoredKey = config.apiKey;
            string authoredDomain = config.domainKey;

            try
            {
                // Given: values applied from a file config
                var fileConfig = new ExternalFileConfig
                {
                    api_key = "applied_key",
                    domain_key = "applied_domain",
                };
                LootLockerConfig.ApplyFileConfig(fileConfig, null);
                Assert.AreEqual("applied_key", config.apiKey, "Precondition: the value was applied");

                // When
                LootLockerConfig.RestoreAssetValues();

                // Then: the authored values are back
                Assert.AreEqual(authoredKey, config.apiKey, "Restoring must discard the applied api_key");
                Assert.AreEqual(authoredDomain, config.domainKey, "Restoring must discard the applied domain_key");
            }
            finally
            {
                LootLockerConfig.RestoreAssetValues();
            }
        }
    }
}
