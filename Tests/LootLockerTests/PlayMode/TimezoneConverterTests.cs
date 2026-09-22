using LootLocker;
using NUnit.Framework;

namespace LootLockerTests.PlayMode
{
    /// <summary>
    /// Guards against regressions in the IANA timezone strings produced by the SDK.
    /// The backend rejects invalid IANA timezones (e.g. wrong casing such as "ETC/GMT")
    /// with "invalid JSON input" errors when starting a session.
    /// </summary>
    public class TimezoneConverterTests
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void ConvertUTCOffsetToIana_UsesCorrectCasingAndSign()
        {
            // IANA sign convention is reversed: Etc/GMT+X means UTC-X
            Assert.AreEqual("Etc/GMT+5", LootLockerTimezoneConverter.convertUTCOffsetToIanaTzString(-5));
            Assert.AreEqual("Etc/GMT-2", LootLockerTimezoneConverter.convertUTCOffsetToIanaTzString(2));
            Assert.AreEqual("Etc/UTC", LootLockerTimezoneConverter.convertUTCOffsetToIanaTzString(0));
        #if UNITY_2021_1_OR_NEWER
                    // Out-of-range offsets are clamped (the converter only clamps from UNITY_2021_1_OR_NEWER)
                    Assert.AreEqual("Etc/GMT+12", LootLockerTimezoneConverter.convertUTCOffsetToIanaTzString(-999));
                    Assert.AreEqual("Etc/GMT-14", LootLockerTimezoneConverter.convertUTCOffsetToIanaTzString(999));
        #endif
                }

                [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
                public void ConvertGMTOffsetToIana_UsesCorrectCasingAndSign()
                {
                    Assert.AreEqual("Etc/GMT-5", LootLockerTimezoneConverter.convertGMTOffsetToIanaTzString(-5));
                    Assert.AreEqual("Etc/GMT+2", LootLockerTimezoneConverter.convertGMTOffsetToIanaTzString(2));
                    Assert.AreEqual("Etc/UTC", LootLockerTimezoneConverter.convertGMTOffsetToIanaTzString(0));
                }

                [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
                public void TryConvertStringToIana_HandlesOffsetsWindowsAndIanaInputs()
        {
            // Numeric offset string
            Assert.IsTrue(LootLockerTimezoneConverter.TryConvertStringToIanaTzString("2", out string fromOffset));
            Assert.AreEqual("Etc/GMT-2", fromOffset);

            // IANA passthrough
            Assert.IsTrue(LootLockerTimezoneConverter.TryConvertStringToIanaTzString("Asia/Tokyo", out string iana));
            Assert.AreEqual("Asia/Tokyo", iana);

            // Windows timezone converted
            Assert.IsTrue(LootLockerTimezoneConverter.TryConvertStringToIanaTzString("Romance Standard Time", out string fromWindows));
            Assert.AreEqual("Europe/Paris", fromWindows);

            // Unknown input falls back to Etc/UTC (correctly cased)
            Assert.IsFalse(LootLockerTimezoneConverter.TryConvertStringToIanaTzString("NotATimezone", out string fallback));
            Assert.AreEqual("Etc/UTC", fallback);

            // Empty input falls back to Etc/UTC
            Assert.IsFalse(LootLockerTimezoneConverter.TryConvertStringToIanaTzString("", out string emptyFallback));
            Assert.AreEqual("Etc/UTC", emptyFallback);
        }
    }
}
