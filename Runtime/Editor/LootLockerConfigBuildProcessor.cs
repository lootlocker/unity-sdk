#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LootLocker.Admin
{
    /// <summary>
    /// Strips every config file except the one the build targets, so a project can keep several
    /// API keys on disk without shipping all of them.
    ///
    /// Files under Resources/ are packed into the build and are trivially readable from a shipped
    /// game, so "the SDK only reads one of them" is not key hygiene: the others are still in the
    /// build data. Selecting a config has to decide what <b>ships</b>, not just what is read.
    ///
    /// The mechanism is a rename with a trailing '~', which Unity ignores at import. Verified on a
    /// real project: a tilde-suffixed .bytes inside Resources/Config is absent from both
    /// Resource.LoadAll and AssetDatabase.
    ///
    /// File.Move is used rather than AssetDatabase.MoveAsset, for two reasons found by testing:
    /// MoveAsset <b>cannot move a file back</b> from a '~' path ("is not a valid path"), so a
    /// MoveAsset-based strip would be a one-way trip, and it discards the .meta, changing the
    /// asset's GUID. Moving the file directly and carrying the .meta along as data keeps the GUID
    /// stable across a build.
    ///
    /// Restore is belt and braces: a finally block, a post-build hook, and a startup recovery that
    /// runs if the editor died mid-build. Every step is idempotent, so a double-run cannot lose a
    /// file.
    /// </summary>
    public class LootLockerConfigBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        /// <summary>
        /// Runs late so anything else that enumerates Resources has already had its say.
        /// </summary>
        public int callbackOrder => 1000;

        /// <summary>
        /// Suffix used to hide a config file from the build.
        ///
        /// Unity ignores files ending in '~' at import, so a renamed config is never packed into a
        /// build and never gets a .meta. Verified against a real project: a tilde-suffixed .bytes
        /// inside Resources/Config is absent from both Resources.LoadAll and AssetDatabase.
        ///
        /// Lives here rather than on LootLockerConfig because only the build processor uses it;
        /// the runtime never needs to know how a file was hidden.
        /// </summary>
        internal const string BuildExcludeSuffix = "~";

        /// <summary>
        /// Records what was renamed, so a restore can be driven from disk after a crash.
        ///
        /// Kept in Library/ because it is transient, machine-local, and already gitignored.
        /// </summary>
        private static string MarkerPath
        {
            get
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(projectRoot, "Library", "LootLockerConfigBuildMarker.txt");
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                Prepare();
            }
            catch (Exception ex)
            {
                // Never leave a build half-stripped. If preparation fails, put everything back and
                // fail the build rather than shipping an unknown set of configs.
                RestoreAll();
                throw new BuildFailedException($"LootLocker: could not prepare file configs for the build. {ex.Message}");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            RestoreAll();
        }

        /// <summary>
        /// Strips every config file except the resolved one.
        ///
        /// Returns the absolute paths that were renamed, for tests and for logging.
        /// </summary>
        internal static List<string> Prepare()
        {
            return Prepare(ConfigFolderAbsolutePaths(), overrideResolvedId: null);
        }

        /// <summary>
        /// Strips every config file in <paramref name="folders"/> except the resolved one.
        ///
        /// Separated from the build callback so tests can drive it against a throwaway folder:
        /// the tilde-rename behaviour is worth pinning without ever running a build, and a test
        /// must never touch the project's real config folder.
        /// </summary>
        /// <param name="folders">
        /// Absolute paths of the folders holding config files. All of them are searched, because
        /// discovery reads from every one - a config that is read but not stripped would ship.
        /// </param>
        /// <param name="overrideResolvedId">
        /// Forces a resolved id instead of resolving one, so tests do not depend on the state of
        /// the config asset. null means resolve normally.
        /// </param>
        internal static List<string> Prepare(string[] folders, string overrideResolvedId)
        {
            var renamed = new List<string>();

            // A previous crash may have left files suffixed. Recover before doing anything else so
            // the enumeration below sees the real set of files.
            RestoreAll(folders);

            if (folders == null || folders.Length == 0)
            {
                return renamed;
            }

            // Resolve across every folder: the id space is shared, so a config in either location
            // is a candidate.
            var available = new List<string>();
            foreach (string folder in folders)
            {
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    available.AddRange(ListConfigFiles(folder));
                }
            }

            string resolvedId = overrideResolvedId ?? LootLockerConfig.ResolveFileConfigId(
                selectedId: LootLockerConfig.current != null ? LootLockerConfig.current.selectedFileConfigId : null,
                commandLineId: LootLockerConfig.GetCommandLineConfigId(Environment.GetCommandLineArgs()),
                buildPointerId: null,
                legacyFilePresent: LegacyFileExists(folders),
                availableIds: IdsOf(available));

            // Nothing to strip when no config file is in effect, and nothing to read either.
            if (resolvedId == null)
            {
                DeleteBuildPointer(folders);
                return renamed;
            }

            string keepFileName = LootLockerConfig.GetConfigFileNameForId(resolvedId) + LootLockerConfig.ConfigFileExtensionForBuild;

            if (!available.Exists(p => string.Equals(Path.GetFileName(p), keepFileName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BuildFailedException(
                    $"LootLocker: the selected file config '{resolvedId}' does not exist (expected {keepFileName}). Refusing to build with no usable config.");
            }

            // Record intent before mutating, so a crash is recoverable from disk alone.
            WriteMarker(renamed);

            foreach (string path in available)
            {
                if (string.Equals(Path.GetFileName(path), keepFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                HideFile(path);
                renamed.Add(path);
                WriteMarker(renamed);
            }

            AssetDatabase.Refresh();

            WriteBuildPointer(folders, resolvedId);

            return renamed;
        }

        private static bool LegacyFileExists(string[] folders)
        {
            string legacyName = LootLockerConfig.ConfigFileNameForLegacy + LootLockerConfig.ConfigFileExtensionForBuild;
            foreach (string folder in folders)
            {
                if (!string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, legacyName)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Hides a file from the build by suffixing it with '~', and hides its .meta the same way.
        ///
        /// The .meta is suffixed as <c>name.bytes.meta~</c>, not <c>name.bytes~.meta</c>. The
        /// latter looks equivalent but is not: Unity sees an orphaned .meta for a file that no
        /// longer exists and deletes it on the next refresh, so the GUID is lost and every asset
        /// reference to the config breaks. Suffixing the whole .meta file name keeps it out of
        /// Unity's sight entirely, so it survives untouched.
        /// </summary>
        private static void HideFile(string path)
        {
            string meta = path + ".meta";

            if (File.Exists(meta))
            {
                File.Move(meta, meta + BuildExcludeSuffix);
            }

            File.Move(path, path + BuildExcludeSuffix);
        }

        /// <summary>
        /// Un-hides a file hidden by <see cref="HideFile"/>.
        /// </summary>
        private static void UnhideFile(string suffixed)
        {
            string original = suffixed.Substring(0, suffixed.Length - BuildExcludeSuffix.Length);
            string meta = original + ".meta";

            File.Move(suffixed, original);

            if (File.Exists(meta + BuildExcludeSuffix))
            {
                File.Move(meta + BuildExcludeSuffix, meta);
            }
        }

        /// <summary>
        /// Undoes every rename recorded in the marker and deletes the build pointer.
        ///
        /// Idempotent: safe to call when nothing was stripped, and safe to call twice.
        /// </summary>
        internal static void RestoreAll()
        {
            RestoreAll(ConfigFolderAbsolutePaths());
        }

        /// <summary>
        /// Reverts every rename recorded in the marker for <paramref name="folders"/>.
        /// </summary>
        internal static void RestoreAll(string[] folders)
        {
            foreach (string original in ReadMarker())
            {
                string suffixed = original + BuildExcludeSuffix;

                if (!File.Exists(suffixed))
                {
                    // Already restored, or the file was removed out from under us.
                    continue;
                }

                if (File.Exists(original))
                {
                    // Both exist, which should not happen. Leave the suffixed copy alone rather
                    // than overwriting a file we did not create.
                    continue;
                }

                UnhideFile(suffixed);
            }

            DeleteMarker();

            DeleteBuildPointer(folders);

            if (folders != null && folders.Length > 0)
            {
                AssetDatabase.Refresh();
            }
        }

        private static void WriteBuildPointer(string[] folders, string id)
        {
            string folder = WritableFolder(folders);
            if (folder == null)
            {
                return;
            }

            string path = Path.Combine(folder, LootLockerConfig.BuildPointerName + LootLockerConfig.ConfigFileExtensionForBuild);
            string json = "{\n  \"selected_file_config_id\": " + LootLockerJson.SerializeObject(id) + "\n}\n";
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(ToAssetPath(path), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private static void DeleteBuildPointer(string[] folders)
        {
            if (folders == null)
            {
                return;
            }

            foreach (string folder in folders)
            {
                if (string.IsNullOrEmpty(folder))
                {
                    continue;
                }

                string path = Path.Combine(folder, LootLockerConfig.BuildPointerName + LootLockerConfig.ConfigFileExtensionForBuild);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                string meta = path + ".meta";
                if (File.Exists(meta))
                {
                    File.Delete(meta);
                }
            }
        }

        /// <summary>
        /// The folder the pointer is written to: the first one that exists, which is the writable
        /// location because discovery lists it first.
        /// </summary>
        private static string WritableFolder(string[] folders)
        {
            if (folders == null)
            {
                return null;
            }

            foreach (string folder in folders)
            {
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    return folder;
                }
            }

            return null;
        }

        private static List<string> ListConfigFiles(string folder)
        {
            var files = new List<string>();
            foreach (string path in Directory.GetFiles(folder, "*" + LootLockerConfig.ConfigFileExtensionForBuild))
            {
                // Already-hidden leftovers belong to a crashed run and were restored above.
                if (Path.GetFileName(path).EndsWith(BuildExcludeSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (LootLockerConfig.GetIdFromConfigFileName(Path.GetFileName(path)) == null)
                {
                    continue;
                }

                files.Add(path);
            }

            return files;
        }

        private static List<string> IdsOf(List<string> files)
        {
            var ids = new List<string>();
            foreach (string path in files)
            {
                string id = LootLockerConfig.GetIdFromConfigFileName(Path.GetFileName(path));
                if (id != null)
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private static void WriteMarker(List<string> renamed)
        {
            try
            {
                string directory = Path.GetDirectoryName(MarkerPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllLines(MarkerPath, renamed);
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Could not write the LootLocker build marker: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        private static List<string> ReadMarker()
        {
            var paths = new List<string>();
            try
            {
                if (File.Exists(MarkerPath))
                {
                    paths.AddRange(File.ReadAllLines(MarkerPath));
                }
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Could not read the LootLocker build marker: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }

            return paths;
        }

        private static void DeleteMarker()
        {
            try
            {
                if (File.Exists(MarkerPath))
                {
                    File.Delete(MarkerPath);
                }
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Could not delete the LootLocker build marker: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        private static string[] ConfigFolderAbsolutePaths()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var folders = new List<string>();

            foreach (string relative in LootLockerConfig.ConfigFolderAssetsPathsForBuild)
            {
                if (string.IsNullOrEmpty(relative))
                {
                    continue;
                }

                folders.Add(Path.Combine(projectRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
            }

            return folders.ToArray();
        }

        private static string ToAssetPath(string absolutePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string normalized = absolutePath.Replace('\\', '/');
            string root = projectRoot.Replace('\\', '/') + "/";

            if (normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring(root.Length);
            }

            return normalized;
        }

        /// <summary>
        /// Restores anything a killed editor left behind, on the next editor start.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RecoverFromInterruptedBuild()
        {
            // Deferred: AssetDatabase is not reliable during static initialisation.
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(MarkerPath))
                {
                    return;
                }

                LootLockerLogger.Log(
                    "LootLocker: a previous build was interrupted, so stripped file configs are being restored.",
                    LootLockerLogger.LogLevel.Warning);
                RestoreAll();
            };
        }
    }
}
#endif
