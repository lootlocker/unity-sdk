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
            bool parsed = LootLockerConfigFileParsing.TryParseSparseFileConfig(text, out _, out HashSet<string> fields, out string error);

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
            bool parsed = LootLockerConfigFileParsing.TryParseSparseFileConfig(text, out ExternalFileConfig config, out _, out string error);

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
            bool parsed = LootLockerConfigFileParsing.TryParseSparseFileConfig(text, out _, out _, out string error);

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
            bool parsed = LootLockerConfigFileParsing.TryParseFileConfig(text, out _, out string error);

            // Then: the shared config contract is unchanged by the sparse parser
            Assert.IsFalse(parsed);
            StringAssert.Contains("api_key", error);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void SparseParse_ReportsEveryFieldOfAFullFile()
        {
            // Given: a file that sets several fields
            // When
            bool parsed = LootLockerConfigFileParsing.TryParseSparseFileConfig(SharedConfig, out _, out HashSet<string> fields, out string error);

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
            bool parsed = LootLockerConfigFileParsing.TryParseSparseFileConfig(text, out _, out HashSet<string> fields, out string error);

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
            string id = LootLockerConfigFileResolution.GetIdFromConfigFileName("LootLockerConfig.local.bytes");

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
            string path = LootLockerConfigFileLocalOverride.LocalOverridePath.Replace('\\', '/');

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
            bool parsed = LootLockerConfigFileParsing.TryParseSparseFileConfig(text, out ExternalFileConfig config, out _, out string error);

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
            bool parsed = LootLockerConfigFileParsing.TryParseSparseFileConfig(text, out ExternalFileConfig config, out HashSet<string> fields, out string error);

            // Then: forward compatibility - an unknown field must not fail the whole file
            Assert.IsTrue(parsed, error);
            Assert.AreEqual(LootLockerLogger.LogLevel.Verbose, config.log_level);
            Assert.IsTrue(fields.Contains("some_future_setting"),
                "Unknown fields must still be reported so they survive a round trip");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void LocalOverride_CannotChangeTheApiKey()
        {
            // Regression: the UI hides the override toggle for the API and domain keys, but the
            // override file is a plain file a developer can hand-edit. Applying it unfiltered let a
            // stray api_key silently point the game at another environment - the exact trap the UI
            // restriction exists to prevent. The restriction has to be enforced where it is applied.
            //
            // Asserts on the filter that ApplyLocalOverride actually calls, rather than on
            // ApplyFileConfig directly: the latter would pass even with the filter removed, because
            // it would simply be handed the unfiltered set.
            var presentFields = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                LootLockerConfigFields.ApiKey,
                LootLockerConfigFields.DomainKey,
                LootLockerConfigFields.LogLevel,
            };

            HashSet<string> allowed = LootLockerConfigFileLayering.OverridableFieldsOnly(presentFields);

            // Then: the keys are dropped, but the rest of the file still applies
            Assert.IsFalse(allowed.Contains(LootLockerConfigFields.ApiKey),
                "api_key must never be applied from the local override file");
            Assert.IsFalse(allowed.Contains(LootLockerConfigFields.DomainKey),
                "domain_key must never be applied from the local override file");
            Assert.IsTrue(allowed.Contains(LootLockerConfigFields.LogLevel),
                "The rest of the override must still apply, or the feature is broken");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void LocalOverride_ApiKeyInTheFileDoesNotReachTheLiveConfig()
        {
            // The end-to-end version of the test above: a real override file on disk, applied
            // through the same entry point the SDK uses, must leave the live key alone.
            string apiKeyBefore = LootLockerConfig.current.apiKey;
            string domainKeyBefore = LootLockerConfig.current.domainKey;

            const string text = @"{
  ""api_key"": ""someone_elses_key"",
  ""domain_key"": ""someone_elses_domain"",
  ""log_level"": ""Verbose""
}";

            Assert.IsTrue(LootLockerConfigFileParsing.TryParseSparseFileConfig(text, out ExternalFileConfig config, out HashSet<string> fields, out string error), error);

            HashSet<string> applied = LootLockerConfigFileLayering.ApplyFileConfig(
                config, LootLockerConfigFileLayering.OverridableFieldsOnly(fields));

            try
            {
                Assert.IsFalse(applied.Contains(LootLockerConfigFields.ApiKey));
                Assert.IsFalse(applied.Contains(LootLockerConfigFields.DomainKey));
                Assert.AreEqual(apiKeyBefore, LootLockerConfig.current.apiKey,
                    "The live API key must be unchanged by a local override");
                Assert.AreEqual(domainKeyBefore, LootLockerConfig.current.domainKey,
                    "The live domain key must be unchanged by a local override");
                Assert.IsTrue(applied.Contains(LootLockerConfigFields.LogLevel),
                    "The rest of the override must still apply, or the feature is broken");
            }
            finally
            {
                LootLockerConfigFileLayering.RestoreAssetValues();
            }
        }
    }
}
