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
    }
}
