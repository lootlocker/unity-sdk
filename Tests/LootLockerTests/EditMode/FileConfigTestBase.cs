using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LootLockerTests.EditMode
{
    /// <summary>
    /// Shared fixture for edit-mode tests that need a throwaway folder registered with the
    /// AssetDatabase.
    ///
    /// IMPORTANT: these tests must NEVER read from or write to the real
    /// "Resources/Config" folder of the project. Doing so would silently change the active
    /// configuration of whoever is running the tests. Always go through a temp folder created
    /// by this fixture.
    /// </summary>
    public abstract class FileConfigTestBase
    {
        private string _tempFolder;

        /// <summary>
        /// Absolute project-relative path (e.g. "Assets/TempFileConfigTests_xxxx") of the
        /// throwaway folder for the current test.
        /// </summary>
        protected string TempFolder => _tempFolder;

        [SetUp]
        public void CreateTempFolder()
        {
            _tempFolder = $"Assets/TempFileConfigTests_{Guid.NewGuid():N}";
            Directory.CreateDirectory(_tempFolder);
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void DeleteTempFolder()
        {
            if (string.IsNullOrEmpty(_tempFolder))
            {
                return;
            }

            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }

            // Remove the orphaned .meta that Unity creates for the folder itself.
            string folderMeta = _tempFolder + ".meta";
            if (File.Exists(folderMeta))
            {
                File.Delete(folderMeta);
            }

            AssetDatabase.Refresh();
            _tempFolder = null;
        }

        /// <summary>
        /// Writes a text file into the temp folder and returns its project-relative path.
        /// </summary>
        protected string WriteTempFile(string fileName, string contents)
        {
            string path = $"{_tempFolder}/{fileName}";
            File.WriteAllText(path, contents);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return path;
        }
    }
}
