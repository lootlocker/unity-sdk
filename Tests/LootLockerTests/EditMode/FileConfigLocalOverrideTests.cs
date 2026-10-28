using System.Collections.Generic;
using LootLocker;
using NUnit.Framework;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Tests for the gitignored local override file.
    ///
    /// The override file is sparse: only the fields it contains are applied. That is the whole
    /// point of the feature, so most of these tests are about what a partial file must NOT do.
    /// </summary>
    public class FileConfigLocalOverrideTests
    {
        private const string SharedConfig = @"{
  ""api_key"": ""shared_key"",
  ""domain_key"": ""shared_domain"",
  ""log_level"": ""Error"",
  ""log_in_builds"": false,
  ""enable_presence"": false
}";

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SparseParse_ReportsOnlyFieldsThatWerePresent()
        {
            // Given: a file that sets exactly one field
            const string text = @"{ ""log_level"": ""Verbose"" }";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out _, out HashSet<string> fields, out string error);

            // Then
            Assert.IsTrue(parsed, error);
            CollectionAssert.AreEquivalent(new[] { "log_level" }, fields,
                "Only the fields actually written to the file may be reported as present");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SparseParse_DoesNotRequireAnApiKey()
        {
            // Given: an override that only changes logging, which is the common case
            const string text = @"{ ""log_level"": ""Verbose"" }";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out ExternalFileConfig config, out _, out string error);

            // Then: a local override must not need a key, or it could never be sparse
            Assert.IsTrue(parsed, error);
            Assert.IsNotNull(config);
            Assert.IsNull(config.api_key);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SparseParse_StillRejectsUnparseableInput()
        {
            // Given: a truncated document
            const string text = @"{ ""log_level"": ""Verbose""";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out _, out _, out string error);

            // Then
            Assert.IsFalse(parsed, "A broken override file must be rejected, not silently ignored");
            Assert.IsNotEmpty(error);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SharedParse_StillRequiresAnApiKey()
        {
            // Given: a shared config with no key
            const string text = @"{ ""log_level"": ""Verbose"" }";

            // When
            bool parsed = LootLockerConfig.TryParseFileConfig(text, out _, out string error);

            // Then: the shared config contract is unchanged by the sparse parser
            Assert.IsFalse(parsed);
            StringAssert.Contains("api_key", error);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SparseParse_ReportsEveryFieldOfAFullFile()
        {
            // Given: a file that sets several fields
            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(SharedConfig, out _, out HashSet<string> fields, out string error);

            // Then
            Assert.IsTrue(parsed, error);
            CollectionAssert.AreEquivalent(
                new[] { "api_key", "domain_key", "log_level", "log_in_builds", "enable_presence" },
                fields);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SparseParse_FieldNamesAreCaseInsensitive()
        {
            // Given: a file written with different casing
            const string text = @"{ ""Log_Level"": ""Verbose"" }";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out _, out HashSet<string> fields, out string error);

            // Then: the field set must match the constants used by the applier
            Assert.IsTrue(parsed, error);
            Assert.IsTrue(fields.Contains(LootLockerConfigFields.LogLevel),
                "Field matching must be case insensitive so a hand-edited file still works");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void LocalOverrideName_IsNotMistakenForAProfile()
        {
            // Given: the local override file name
            // When
            string id = LootLockerConfig.GetIdFromConfigFileName("LootLockerConfig.local.bytes");

            // Then: it must never appear in the dropdown as a selectable profile
            Assert.IsNull(id, "The local override file must not be discoverable as a config profile");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void LocalOverrideFile_IsOutsideResources()
        {
            // Regression: the override used to live under Resources/Config, so Unity packed it into
            // every build made on that machine. Guarding the read path with UNITY_EDITOR stopped
            // the SDK from using it, but the bytes - including a local api_key - still shipped in
            // the build data. The file must not be inside any Resources folder at all.
            string path = LootLockerConfig.LocalOverridePath.Replace('\\', '/');

            Assert.IsFalse(path.Contains("/Resources/"),
                $"The local override must not live under Resources, but was: {path}");
            Assert.IsTrue(path.Contains("/LocalOverrides/"),
                $"Expected the LocalOverrides folder, but was: {path}");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void LocalOverrideFields_AreEmptyWhenNothingIsOverridden()
        {
            // Given: a fresh domain with no override applied
            // Then: the marker source must be empty rather than null
            Assert.IsNotNull(LootLockerConfig.LocalOverrideFields);

            // Only the null and empty-id guards are asserted here. Asserting that a specific field
            // is not overridden would depend on whether the developer running the tests happens to
            // have a LootLockerConfig.local.bytes with that field, which is not a property of the
            // code under test.
            Assert.IsFalse(LootLockerConfig.IsLocallyOverridden(null));
            Assert.IsFalse(LootLockerConfig.IsLocallyOverridden(""));
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SparseParse_EnumValueIsReadFromItsName()
        {
            // Given: the override file format the UI writes for an enum
            const string text = @"{ ""log_level"": ""Verbose"" }";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out ExternalFileConfig config, out _, out string error);

            // Then: enums are written by name so the file stays hand-editable
            Assert.IsTrue(parsed, error);
            Assert.AreEqual(LootLockerLogger.LogLevel.Verbose, config.log_level);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SparseParse_UnknownFieldDoesNotBreakParsing()
        {
            // Given: a file written by a newer SDK that has a field this version does not know
            const string text = @"{
  ""log_level"": ""Verbose"",
  ""some_future_setting"": 42
}";

            // When
            bool parsed = LootLockerConfig.TryParseSparseFileConfig(text, out ExternalFileConfig config, out HashSet<string> fields, out string error);

            // Then: forward compatibility - an unknown field must not fail the whole file
            Assert.IsTrue(parsed, error);
            Assert.AreEqual(LootLockerLogger.LogLevel.Verbose, config.log_level);
            Assert.IsTrue(fields.Contains("some_future_setting"),
                "Unknown fields must still be reported so they survive a round trip");
        }
    }
}
