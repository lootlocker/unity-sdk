using System.Collections.Generic;
using System.IO;
using System.Linq;
using LootLocker;
using LootLocker.Admin;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Tests for the build-time stripping of config files.
    ///
    /// Files under Resources/ are packed into the build and are readable from a shipped game, so
    /// selecting a config has to decide what ships, not only what is read. These tests pin that
    /// without ever running a build: the whole mechanism is a reversible rename, so it can be
    /// driven directly against a throwaway folder.
    ///
    /// They must never touch the project's real Resources/Config folder.
    /// </summary>
    public class FileConfigBuildStripTests : FileConfigTestBase
    {
        private const string ValidJson = "{ \"api_key\": \"key\" }";

        /// <summary>
        /// Absolute path of the throwaway folder, which is where the strip logic operates.
        /// </summary>
        private string AbsoluteTempFolder =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                TempFolder.Replace('/', Path.DirectorySeparatorChar));

        private void WriteConfig(string id)
        {
            string fileName = id == null
                ? $"LootLockerConfig{BuildExtension}"
                : $"LootLockerConfig-{id}{BuildExtension}";
            WriteTempFile(fileName, ValidJson);
        }

        private const string BuildExtension = ".bytes";

        /// <summary>
        /// Config files visible to Unity, i.e. not hidden behind a '~'. The generated build
        /// pointer is excluded because it is build output rather than a selectable config.
        /// </summary>
        private string[] VisibleConfigFiles()
        {
            return Directory.GetFiles(AbsoluteTempFolder, "*" + BuildExtension)
                .Select(Path.GetFileName)
                .Where(n => !n.StartsWith(LootLockerConfig.BuildPointerName, System.StringComparison.Ordinal))
                .OrderBy(n => n)
                .ToArray();
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_OnlyTheResolvedConfigRemainsVisible()
        {
            // Given: three configs on disk
            WriteConfig("dev");
            WriteConfig("prod");
            WriteConfig("prod-ps5");

            // When: a build targets "prod"
            LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod");

            // Then: only the targeted file is still visible to Unity
            Assert.AreEqual(new[] { "LootLockerConfig-prod.bytes" }, VisibleConfigFiles(),
                "Every config except the targeted one must be excluded from the build");

            LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_ExcludedConfigsAreStillOnDiskWithTheirContents()
        {
            // Given
            WriteConfig("dev");
            WriteConfig("prod");
            string devPath = Path.Combine(AbsoluteTempFolder, "LootLockerConfig-dev.bytes");

            // When
            LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod");

            // Then: stripping must hide, never delete - a cancelled build must not lose a key
            string suffixed = devPath + LootLockerConfigBuildProcessor.BuildExcludeSuffix;
            Assert.IsTrue(File.Exists(suffixed), "The excluded config must still exist on disk");
            Assert.AreEqual(ValidJson, File.ReadAllText(suffixed), "Contents must be untouched");

            LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_RestoreIsByteIdentical()
        {
            // Given
            WriteConfig("dev");
            WriteConfig("prod");

            // The .meta files are Unity's, not ours, and the temp fixture does not import these
            // files, so only the config files themselves are compared.
            var before = Directory.GetFiles(AbsoluteTempFolder, "*" + BuildExtension)
                .ToDictionary(p => Path.GetFileName(p), File.ReadAllBytes);

            // When
            LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod");
            LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });

            // Then: the folder is exactly as it was, with no leftover pointer or hidden files
            var after = Directory.GetFiles(AbsoluteTempFolder, "*" + BuildExtension)
                .ToDictionary(p => Path.GetFileName(p), File.ReadAllBytes);

            CollectionAssert.AreEquivalent(before.Keys, after.Keys, "No file may be lost, renamed or left over");
            foreach (var pair in before)
            {
                CollectionAssert.AreEqual(pair.Value, after[pair.Key], $"{pair.Key} contents changed");
            }
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_RestoreIsIdempotent()
        {
            // Given: a completed strip
            WriteConfig("dev");
            WriteConfig("prod");
            LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod");

            // When: restore runs twice, which happens if postprocess follows startup recovery
            LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
            LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });

            // Then: both files are present and intact
            Assert.AreEqual(
                new[] { "LootLockerConfig-dev.bytes", "LootLockerConfig-prod.bytes" },
                VisibleConfigFiles(),
                "A double restore must not lose a file");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_SingleConfigIsNoOp()
        {
            // Given: the common case, one config and nothing to hide
            WriteConfig("dev");

            // When
            List<string> renamed = LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "dev");

            // Then: nothing was renamed
            Assert.IsEmpty(renamed, "A single-config project must take on no stripping");
            Assert.AreEqual(new[] { "LootLockerConfig-dev.bytes" }, VisibleConfigFiles());

            LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_MissingTargetFailsTheBuild()
        {
            // Given: configs on disk, but the resolved id names a file that does not exist
            WriteConfig("dev");

            // When / Then: shipping with no usable config is worse than failing the build
            Assert.Throws<UnityEditor.Build.BuildFailedException>(
                () => LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod"),
                "A build must fail loudly rather than ship without the config it was told to use");

            // And nothing may be left stripped
            Assert.AreEqual(new[] { "LootLockerConfig-dev.bytes" }, VisibleConfigFiles(),
                "A failed prepare must not leave configs hidden");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_WritesAndDeletesTheBuildPointer()
        {
            // Given
            WriteConfig("dev");
            WriteConfig("prod");
            string pointerPath = Path.Combine(AbsoluteTempFolder, "LootLockerConfig.build.bytes");

            // When
            LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod");

            // Then: the player needs to be told which file survived
            Assert.IsTrue(File.Exists(pointerPath), "The build must record which config it chose");
            StringAssert.Contains("prod", File.ReadAllText(pointerPath));

            LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
            Assert.IsFalse(File.Exists(pointerPath), "The pointer is build output and must not survive");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_StripsAcrossEveryConfigFolder()
        {
            // Regression: the processor used to strip in exactly one folder, while discovery reads
            // from every candidate folder (the writable Assets/ location and the read-only
            // Packages/ one). A config in the second folder was therefore read but never stripped,
            // so it shipped - the exact leak this feature exists to prevent.
            string secondFolder = $"Assets/TempFileConfigTests_{System.Guid.NewGuid():N}";
            Directory.CreateDirectory(secondFolder);
            string secondAbsolute = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                secondFolder.Replace('/', Path.DirectorySeparatorChar));

            try
            {
                WriteConfig("dev");
                File.WriteAllText(Path.Combine(secondAbsolute, "LootLockerConfig-prod.bytes"), ValidJson);

                // When: a build targets dev, which lives in the first folder
                LootLockerConfigBuildProcessor.Prepare(
                    new[] { AbsoluteTempFolder, secondAbsolute }, overrideResolvedId: "dev");

                // Then: the config in the *other* folder is stripped too
                Assert.IsFalse(File.Exists(Path.Combine(secondAbsolute, "LootLockerConfig-prod.bytes")),
                    "A config in any discovered folder must be stripped, not just the first one");
                Assert.IsTrue(File.Exists(Path.Combine(secondAbsolute, "LootLockerConfig-prod.bytes" + LootLockerConfigBuildProcessor.BuildExcludeSuffix)),
                    "It must be hidden, not deleted");
            }
            finally
            {
                LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder, secondAbsolute });
                if (Directory.Exists(secondFolder))
                {
                    Directory.Delete(secondFolder, true);
                }
            }
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_BuildPointerIsNotADiscoverableProfile()
        {
            // The pointer lives in Resources/Config so the player can read it, which means
            // discovery would otherwise offer it as a selectable config.
            Assert.IsNull(LootLockerConfigFileResolution.GetIdFromConfigFileName("LootLockerConfig.build.bytes"),
                "The generated build pointer must never appear as a config profile");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_HiddenConfigIsInvisibleToAssetDatabase()
        {
            // The whole mechanism rests on Unity ignoring '~' files. Verified against a real
            // project during development; pinned here so a Unity behaviour change is caught.
            WriteConfig("dev");
            WriteConfig("prod");
            string devAssetPath = $"{TempFolder}/LootLockerConfig-dev.bytes";

            try
            {
                LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod");
                AssetDatabase.Refresh();

                string[] found = AssetDatabase.FindAssets("t:TextAsset", new[] { TempFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(Path.GetFileName)
                    .Where(n => !n.StartsWith(LootLockerConfig.BuildPointerName, System.StringComparison.Ordinal))
                    .OrderBy(n => n)
                    .ToArray();

                Assert.AreEqual(new[] { "LootLockerConfig-prod.bytes" }, found,
                    "A '~'-suffixed config must not be an asset at all, so only the targeted file is left");
            }
            finally
            {
                LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
            }

            Assert.IsTrue(System.IO.File.Exists(Path.Combine(AbsoluteTempFolder, "LootLockerConfig-dev.bytes")),
                "Restore must bring the config back");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_MetaAndGuidSurviveTheRoundTrip()
        {
            // Regression: the .meta must be hidden as "name.bytes.meta~", not "name.bytes~.meta".
            // The latter leaves an orphaned .meta for a file that no longer exists, which Unity
            // deletes on the next refresh - losing the GUID and breaking every reference to the
            // config. This test refreshes between hide and restore so Unity's reaction is actually
            // exercised; without the refresh it would only be testing our own file moves.
            WriteConfig("dev");
            WriteConfig("prod");
            string devPath = Path.Combine(AbsoluteTempFolder, "LootLockerConfig-dev.bytes");
            string metaPath = devPath + ".meta";
            const string guid = "0123456789abcdef0123456789abcdef";
            File.WriteAllText(metaPath, $"fileFormatVersion: 2\nguid: {guid}\n");

            try
            {
                // Target prod, so dev is the one that gets hidden.
                LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod");

                // Let Unity see the hidden state, which is what deletes an orphaned .meta.
                AssetDatabase.Refresh();

                Assert.IsTrue(File.Exists(metaPath + LootLockerConfigBuildProcessor.BuildExcludeSuffix),
                    "The .meta must be hidden as name.bytes.meta~ so Unity never sees it as orphaned");

                LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
                AssetDatabase.Refresh();

                Assert.IsTrue(File.Exists(metaPath), "The .meta must be restored alongside the config");
                StringAssert.Contains(guid, File.ReadAllText(metaPath),
                    "The GUID must be preserved so asset references survive a build");
            }
            finally
            {
                LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
            }
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_NoResolvedConfig_StripsEveryConfig()
        {
            // Regression: a null resolution - including an explicit "-lootlockerconfig none" - used
            // to return early without hiding anything, so every profile and every API key in them
            // still shipped. "No config selected" must mean "no config ships", not "all of them do".
            WriteConfig("dev");
            WriteConfig("prod");

            try
            {
                // An invalid id resolves to null, which is the same path as "none".
                LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: null);

                Assert.IsEmpty(VisibleConfigFiles(),
                    "When no config is in effect, no config file may remain visible to the build");
            }
            finally
            {
                LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
            }
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_UnusableSelectedConfig_FailsTheBuild()
        {
            // Regression: only the file name was checked, so a malformed or keyless profile built
            // successfully, shipped, and then fell back to the asset values at runtime - a build
            // that silently differs from the profile that was requested.
            WriteConfig("dev");
            WriteTempFile("LootLockerConfig-prod.bytes", "{ \"log_level\": \"Verbose\" }");

            try
            {
                Assert.Throws<BuildFailedException>(
                    () => LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod"),
                    "A selected config with no api_key must fail the build rather than ship");
            }
            finally
            {
                LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
            }
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void BuildStrip_MalformedSelectedConfig_FailsTheBuild()
        {
            WriteConfig("dev");
            WriteTempFile("LootLockerConfig-prod.bytes", "this is not json at all {{{");

            try
            {
                Assert.Throws<BuildFailedException>(
                    () => LootLockerConfigBuildProcessor.Prepare(new[] { AbsoluteTempFolder }, overrideResolvedId: "prod"),
                    "A selected config that does not parse must fail the build rather than ship");
            }
            finally
            {
                LootLockerConfigBuildProcessor.RestoreAll(new[] { AbsoluteTempFolder });
            }
        }
    }
}
