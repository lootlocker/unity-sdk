using System.IO;
using NUnit.Framework;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Smoke test proving the edit-mode test assembly is discovered and runs, and that the
    /// temp-folder fixture behaves (creates, writes, cleans up).
    /// </summary>
    public class EditModeAssemblyTests : FileConfigTestBase
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void EditModeAssembly_Loads()
        {
            Assert.IsTrue(Directory.Exists(TempFolder), "Temp folder was not created");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void EditModeAssembly_TempFile_CanBeWritten()
        {
            // Given / When
            string path = WriteTempFile("sample.bytes", "{\"api_key\":\"abc\"}");

            // Then
            Assert.IsTrue(File.Exists(path), "Temp file was not written");
            Assert.AreEqual("{\"api_key\":\"abc\"}", File.ReadAllText(path), "Temp file contents differ");
        }
    }
}
