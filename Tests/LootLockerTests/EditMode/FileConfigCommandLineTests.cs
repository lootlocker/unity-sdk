using System.Text.RegularExpressions;
using LootLocker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Tests for the <c>-lootlockerconfig</c> command line switch, which lets CI select a config
    /// without touching the editor.
    /// </summary>
    public class FileConfigCommandLineTests
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void CommandLine_ReturnsNullWhenSwitchIsAbsent()
        {
            // Given: a command line without the switch
            string[] args = { "Unity.exe", "-batchmode", "-quit" };

            // When / Then
            Assert.IsNull(LootLockerConfig.GetCommandLineConfigId(args),
                "Absent switch must be distinguishable from an explicit choice");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void CommandLine_ReturnsTheRequestedId()
        {
            // Given
            string[] args = { "Unity.exe", "-batchmode", "-lootlockerconfig", "prod-ps5" };

            // When / Then
            Assert.AreEqual("prod-ps5", LootLockerConfig.GetCommandLineConfigId(args));
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void CommandLine_IsCaseInsensitiveForTheSwitchName()
        {
            // Given: a differently cased switch
            string[] args = { "Unity.exe", "-LootLockerConfig", "prod" };

            // When / Then
            Assert.AreEqual("prod", LootLockerConfig.GetCommandLineConfigId(args));
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void CommandLine_NoneDisablesFileConfig()
        {
            // Given
            string[] args = { "Unity.exe", "-lootlockerconfig", "none" };

            // When
            string id = LootLockerConfig.GetCommandLineConfigId(args);

            // Then: "none" is a sentinel, not an identifier
            Assert.AreEqual("none", id);
            Assert.IsNull(LootLockerConfig.ResolveFileConfigId("prod", id, null, true, new[] { "prod" }),
                "none must opt out of file config entirely");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void CommandLine_InvalidIdIsTreatedAsNone()
        {
            // Given: an identifier that could never be a file name
            string[] args = { "Unity.exe", "-lootlockerconfig", "../../etc/passwd" };

            // The rejection is logged as an error on purpose: silently ignoring a bad switch would
            // leave CI running against the wrong config with no signal.
            LogAssert.Expect(LogType.Error,
                new Regex("Ignoring -lootlockerconfig"));

            // When
            string id = LootLockerConfig.GetCommandLineConfigId(args);

            // Then: never guess, and never fall back to a different config
            Assert.AreEqual("none", id);
            Assert.IsNull(LootLockerConfig.ResolveFileConfigId("prod", id, null, true, new[] { "prod" }));
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void CommandLine_BeatsTheSelectedConfig()
        {
            // Given: a selection in the asset and a different one on the command line
            // When
            string resolved = LootLockerConfig.ResolveFileConfigId(
                selectedId: "dev",
                commandLineId: "prod-ps5",
                buildPointerId: null,
                legacyFilePresent: true,
                availableIds: new[] { "dev", "prod-ps5" });

            // Then
            Assert.AreEqual("prod-ps5", resolved, "The command line is the highest precedence source");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void CommandLine_MissingValueIsIgnored()
        {
            // Given: the switch with nothing after it
            string[] args = { "Unity.exe", "-lootlockerconfig" };

            // When / Then
            Assert.IsNull(LootLockerConfig.GetCommandLineConfigId(args),
                "A switch with no value must not be treated as an explicit opt-out");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void CommandLine_NullArgsIsSafe()
        {
            // Given / When / Then
            Assert.IsNull(LootLockerConfig.GetCommandLineConfigId(null));
        }
    }
}
