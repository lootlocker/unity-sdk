using System.Collections.Generic;
using LootLocker;
using NUnit.Framework;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Unit tests for the pure file-config resolution core.
    ///
    /// These lock down the precedence order and the "fail loudly, never silently fall back"
    /// contract. No Unity I/O happens here.
    /// </summary>
    public class FileConfigResolutionTests
    {
        private static readonly string[] NoIds = new string[0];

        #region Identifier validation

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void IsValidFileConfigId_EmptyIsValid()
        {
            Assert.IsTrue(LootLockerConfig.IsValidFileConfigId(""), "Empty id means the legacy file and must be valid");
            Assert.IsTrue(LootLockerConfig.IsValidFileConfigId(null), "Null id means auto-detect and must be valid");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void IsValidFileConfigId_AcceptsOrdinaryIdentifiers()
        {
            Assert.IsTrue(LootLockerConfig.IsValidFileConfigId("prod"));
            Assert.IsTrue(LootLockerConfig.IsValidFileConfigId("prod-ps5"));
            Assert.IsTrue(LootLockerConfig.IsValidFileConfigId("debug.v2"));
            Assert.IsTrue(LootLockerConfig.IsValidFileConfigId("Release_2024"));
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void IsValidFileConfigId_RejectsPathTraversal()
        {
            // The identifier becomes part of a filename, so these must never reach the filesystem.
            Assert.IsFalse(LootLockerConfig.IsValidFileConfigId("../evil"), "Path traversal must be rejected");
            Assert.IsFalse(LootLockerConfig.IsValidFileConfigId("a/b"), "Path separators must be rejected");
            Assert.IsFalse(LootLockerConfig.IsValidFileConfigId("a\\b"), "Backslashes must be rejected");
            Assert.IsFalse(LootLockerConfig.IsValidFileConfigId(".."), "Dot-dot must be rejected");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void IsValidFileConfigId_RejectsLeadingPunctuationAndOverlongIds()
        {
            Assert.IsFalse(LootLockerConfig.IsValidFileConfigId("-leading"), "Must start alphanumeric");
            Assert.IsFalse(LootLockerConfig.IsValidFileConfigId(".leading"), "Must start alphanumeric");
            Assert.IsFalse(LootLockerConfig.IsValidFileConfigId(new string('a', 65)), "65 chars exceeds the limit");
            Assert.IsTrue(LootLockerConfig.IsValidFileConfigId(new string('a', 64)), "64 chars is the limit");
        }

        #endregion

        #region File name derivation

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void GetConfigFileNameForId_EmptyIdYieldsLegacyName()
        {
            Assert.AreEqual("LootLockerConfig", LootLockerConfig.GetConfigFileNameForId(""),
                "The empty id must map to the legacy unsuffixed file");
            Assert.AreEqual("LootLockerConfig", LootLockerConfig.GetConfigFileNameForId(null),
                "A null id must map to the legacy unsuffixed file");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void GetConfigFileNameForId_SuffixesIdentifiers()
        {
            Assert.AreEqual("LootLockerConfig-prod", LootLockerConfig.GetConfigFileNameForId("prod"));
            Assert.AreEqual("LootLockerConfig-prod-ps5", LootLockerConfig.GetConfigFileNameForId("prod-ps5"));
        }

        #endregion

        #region Resolution precedence

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_CommandLineBeatsEverything()
        {
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: "from-asset",
                commandLineId: "from-cli",
                buildPointerId: "from-build",
                legacyFilePresent: true,
                availableIds: new[] { "from-asset", "from-cli", "from-build" });

            Assert.AreEqual("from-cli", resolved, "The command line must win over asset selection and build pointer");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_CommandLineNoneDisablesFileConfig()
        {
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: "from-asset",
                commandLineId: "none",
                buildPointerId: null,
                legacyFilePresent: true,
                availableIds: new[] { "from-asset" });

            Assert.IsNull(resolved, "'none' on the command line must disable file config entirely");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_BuildPointerBeatsAssetSelection()
        {
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: "from-asset",
                commandLineId: null,
                buildPointerId: "from-build",
                legacyFilePresent: false,
                availableIds: new[] { "from-asset", "from-build" });

            Assert.AreEqual("from-build", resolved, "A build pointer must override the editor selection");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_AssetSelectionBeatsLegacyAutoDetect()
        {
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: "from-asset",
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: true,
                availableIds: new[] { "from-asset" });

            Assert.AreEqual("from-asset", resolved, "An explicit selection must beat legacy auto-detect");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_ExplicitEmptySelectionDisablesFileConfig()
        {
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: "",
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: true,
                availableIds: new[] { "prod" });

            Assert.IsNull(resolved, "An explicit empty selection must disable file config even when a legacy file exists");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_LegacyFileIsAutoDetectedWhenNothingSelected()
        {
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: null,
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: true,
                availableIds: NoIds);

            Assert.AreEqual("", resolved, "The legacy file must still be picked up with no explicit selection");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_SingleDiscoveredProfileIsUsedWhenNothingSelected()
        {
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: null,
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: false,
                availableIds: new[] { "prod" });

            Assert.AreEqual("prod", resolved, "A single unambiguous profile should be used without explicit selection");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_MultipleDiscoveredProfilesAreNotGuessed()
        {
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: null,
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: false,
                availableIds: new[] { "prod", "debug" });

            Assert.IsNull(resolved, "With several profiles and no selection the SDK must not guess");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_NothingConfiguredYieldsNoFileConfig()
        {
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: null,
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: false,
                availableIds: NoIds);

            Assert.IsNull(resolved, "With no file config at all the SDK must fall back to the asset");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_InvalidIdentifierIsRejected()
        {
            Assert.IsNull(
                LootLockerConfig.ResolveFileConfigId(null, "../evil", null, false, NoIds),
                "An invalid command line id must not reach the filesystem");
            Assert.IsNull(
                LootLockerConfig.ResolveFileConfigId(null, null, "../evil", false, NoIds),
                "An invalid build pointer id must not reach the filesystem");
            Assert.IsNull(
                LootLockerConfig.ResolveFileConfigId("../evil", null, null, false, NoIds),
                "An invalid asset selection must not reach the filesystem");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_StaleSelectionStillResolvesSoTheCallerCanReportIt()
        {
            // A selection that is no longer on disk must still resolve, so the load path can warn
            // about it by name instead of silently behaving as if nothing was ever selected.
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: "deleted",
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: false,
                availableIds: new[] { "prod" });

            Assert.AreEqual("deleted", resolved,
                "A stale selection must resolve so the caller can report which config is missing");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Resolve_AllConfigsRemovedBehavesLikeNoConfigEverExisted()
        {
            // The user moved every config file away. With no selection and nothing on disk the
            // result must be "no file config", i.e. the asset values, exactly as a fresh project.
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: null,
                commandLineId: null,
                buildPointerId: null,
                legacyFilePresent: false,
                availableIds: NoIds);

            Assert.IsNull(resolved, "With no config files at all the SDK must use the asset values");
        }

        #endregion

        #region Parsing

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void TryParseFileConfig_PlainJsonSucceeds()
        {
            const string json = "{\"api_key\":\"key123\",\"domain_key\":\"domain123\"}";

            bool ok = LootLockerConfig.TryParseFileConfig(json, out ExternalFileConfig config, out string error);

            Assert.IsTrue(ok, $"Expected parse to succeed but got: {error}");
            Assert.AreEqual("key123", config.api_key);
            Assert.AreEqual("domain123", config.domain_key);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void TryParseFileConfig_EmptyTextFails()
        {
            bool ok = LootLockerConfig.TryParseFileConfig("", out ExternalFileConfig config, out string error);

            Assert.IsFalse(ok, "Empty text must not parse");
            Assert.IsNull(config);
            Assert.IsNotEmpty(error, "A failure must explain itself");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void TryParseFileConfig_MissingApiKeyFails()
        {
            const string json = "{\"domain_key\":\"domain123\"}";

            bool ok = LootLockerConfig.TryParseFileConfig(json, out ExternalFileConfig config, out string error);

            Assert.IsFalse(ok, "A config without an api_key is unusable");
            Assert.IsNull(config);
            StringAssert.Contains("api_key", error);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void TryParseFileConfig_MalformedJsonFails()
        {
            bool ok = LootLockerConfig.TryParseFileConfig("{not json", out ExternalFileConfig config, out string error);

            Assert.IsFalse(ok, "Malformed JSON must not parse");
            Assert.IsNull(config);
            Assert.IsNotEmpty(error);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void TryParseFileConfig_TruncatedJsonFails()
        {
            // A truncated file is the realistic failure (interrupted download, bad merge).
            const string truncated = "{\n  \"api_key\": \"key123\",\n  \"log_level\": \"Verbose\"\n";

            bool ok = LootLockerConfig.TryParseFileConfig(truncated, out ExternalFileConfig config, out string error);

            Assert.IsFalse(ok, "Truncated JSON must not parse");
            Assert.IsNull(config);
            Assert.IsNotEmpty(error);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void TryParseFileConfig_MissingCommaIsAcceptedByTheParser()
        {
            // Documents a real property of the bundled parser: ReadDictionary treats ',' as a
            // no-op, so a missing separator is tolerated. This is asserted rather than assumed so
            // that a future parser change is noticed, and so nobody relies on "missing comma" as a
            // way to produce an invalid config.
            const string missingComma = "{\n  \"api_key\": \"key123\"\n  \"log_level\": \"Verbose\"\n}";

            bool ok = LootLockerConfig.TryParseFileConfig(missingComma, out ExternalFileConfig config, out string error);

            Assert.IsTrue(ok, $"The bundled parser tolerates a missing comma; got: {error}");
            Assert.AreEqual("key123", config.api_key);
        }

        #endregion
    }
}
