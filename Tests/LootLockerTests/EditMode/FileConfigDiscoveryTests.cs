using System;
using System.Collections.Generic;
using System.Linq;
using LootLocker;
using NUnit.Framework;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Unit tests for config file discovery and classification.
    ///
    /// Discovery is a local parse only - no network calls, no key verification. These tests use
    /// injected file listings so they never touch the real Resources/Config folder.
    /// </summary>
    public class FileConfigDiscoveryTests
    {
        private static Func<IEnumerable<string>> Listing(params string[] names)
        {
            return () => names;
        }

        private static Func<string, string> Reader(Dictionary<string, string> contents)
        {
            return name => contents.TryGetValue(name, out string text) ? text : null;
        }

        private const string ValidJson = "{\"api_key\":\"key123\"}";

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_EmptyFolder_ReturnsNoProfiles()
        {
            // Given / When
            List<LootLockerConfig.FileConfigDescriptor> found =
                LootLockerConfig.DiscoverFileConfigs(Listing(), Reader(new Dictionary<string, string>()));

            // Then
            Assert.IsEmpty(found, "An empty folder must yield no profiles");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_FindsAllProfiles_Sorted()
        {
            // Given
            var contents = new Dictionary<string, string>
            {
                ["LootLockerConfig-prod.bytes"] = ValidJson,
                ["LootLockerConfig-debug.bytes"] = ValidJson,
                ["LootLockerConfig-alpha.bytes"] = ValidJson,
            };

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing(contents.Keys.ToArray()), Reader(contents));

            // Then
            CollectionAssert.AreEqual(new[] { "alpha", "debug", "prod" }, found.Select(d => d.Id).ToArray(),
                "Profiles must be listed in a stable, sorted order");
            Assert.IsTrue(found.All(d => d.State == LootLockerConfig.FileConfigState.Valid), "All should be valid");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_IgnoresUnrelatedTextAssets()
        {
            // Given
            var contents = new Dictionary<string, string>
            {
                ["notes.bytes"] = "hello",
                ["LootLockerConfig-prod.bytes"] = ValidJson,
                ["LootLockerConfig-prod.json"] = ValidJson,
                ["SomethingElse.bytes"] = ValidJson,
            };

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing(contents.Keys.ToArray()), Reader(contents));

            // Then
            CollectionAssert.AreEqual(new[] { "prod" }, found.Select(d => d.Id).ToArray(),
                "Only files matching the config naming pattern may be listed");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_IgnoresTildeFiles()
        {
            // Given: '~' suffixed files are excluded from builds by the preprocess step
            var contents = new Dictionary<string, string>
            {
                ["LootLockerConfig-prod.bytes"] = ValidJson,
                ["LootLockerConfig-debug.bytes~"] = ValidJson,
            };

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing(contents.Keys.ToArray()), Reader(contents));

            // Then
            CollectionAssert.AreEqual(new[] { "prod" }, found.Select(d => d.Id).ToArray(),
                "Excluded ('~') files must never appear in the dropdown");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_LegacyFileListedWithEmptyId()
        {
            // Given
            var contents = new Dictionary<string, string> { ["LootLockerConfig.bytes"] = ValidJson };

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing(contents.Keys.ToArray()), Reader(contents));

            // Then
            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("", found[0].Id, "The legacy file must be identified by the empty id");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_MalformedFile_ListedAsInvalid()
        {
            // Given
            var contents = new Dictionary<string, string>
            {
                ["LootLockerConfig-broken.bytes"] = "{not json",
                ["LootLockerConfig-prod.bytes"] = ValidJson,
            };

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing(contents.Keys.ToArray()), Reader(contents));

            // Then: shown, not hidden - a broken file the developer needs to know about
            Assert.AreEqual(2, found.Count, "A broken file must still be listed");
            LootLockerConfig.FileConfigDescriptor broken = found.Single(d => d.Id == "broken");
            Assert.AreEqual(LootLockerConfig.FileConfigState.Invalid, broken.State);
            Assert.IsNotEmpty(broken.Reason, "The reason must be surfaced");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_MissingApiKey_ListedAsInvalid()
        {
            // Given
            var contents = new Dictionary<string, string>
            {
                ["LootLockerConfig-keyless.bytes"] = "{\"domain_key\":\"d\"}",
            };

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing(contents.Keys.ToArray()), Reader(contents));

            // Then
            Assert.AreEqual(LootLockerConfig.FileConfigState.Invalid, found.Single().State,
                "A parseable file without an api_key is still unusable");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_UnreadableFile_ListedAsUnreadable()
        {
            // Given: the reader returns null, simulating an I/O failure
            var contents = new Dictionary<string, string>();

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing("LootLockerConfig-locked.bytes"), Reader(contents));

            // Then
            Assert.AreEqual(LootLockerConfig.FileConfigState.Unreadable, found.Single().State,
                "An unreadable file must be classified, not thrown");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_CaseInsensitiveDuplicate_Deduped()
        {
            // Given: on case-insensitive filesystems both cannot coexist, but a listing may still
            // contain both spellings.
            var contents = new Dictionary<string, string>
            {
                ["LootLockerConfig-Prod.bytes"] = ValidJson,
                ["LootLockerConfig-prod.bytes"] = ValidJson,
            };

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing(contents.Keys.ToArray()), Reader(contents));

            // Then
            Assert.AreEqual(1, found.Count, "Ids differing only by case must be deduped");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_InvalidIdInFileName_Ignored()
        {
            // Given: a file whose suffix is not a legal identifier
            var contents = new Dictionary<string, string>
            {
                ["LootLockerConfig-..evil.bytes"] = ValidJson,
            };

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing(contents.Keys.ToArray()), Reader(contents));

            // Then
            Assert.IsEmpty(found, "File names with illegal identifiers must not be listed");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_EmptyIdSuffix_Ignored()
        {
            // Given: "LootLockerConfig-.bytes". The prefix check passes and the extracted id is
            // empty, which IsValidFileConfigId accepts because "" is the legacy file's identifier.
            // Without an explicit guard this stray file is classified as the legacy config and can
            // collide with the real LootLockerConfig.bytes during dedupe.
            var contents = new Dictionary<string, string>
            {
                ["LootLockerConfig-.bytes"] = ValidJson,
            };

            // When
            List<LootLockerConfig.FileConfigDescriptor> found = LootLockerConfig.DiscoverFileConfigs(
                Listing(contents.Keys.ToArray()), Reader(contents));

            // Then
            Assert.IsEmpty(found, "A file with an empty id suffix must not be listed as the legacy config");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Discovery_ReaderReceivesTheSameNameThatWasListed()
        {
            // Regression: discovery lists real file names (with the .bytes extension) while the
            // load path works with bare names. If the reader is handed a name it cannot resolve,
            // every config shows up as "could not be read".
            var listed = new List<string>();
            var read = new List<string>();

            LootLockerConfig.DiscoverFileConfigs(
                () => { listed.Add("LootLockerConfig-prod.bytes"); return listed; },
                name => { read.Add(name); return ValidJson; });

            CollectionAssert.AreEqual(listed, read,
                "The reader must be given exactly the names that were listed");
        }
    }
}
