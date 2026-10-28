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
            return Prepare(ConfigFolderAbsolutePath(), overrideResolvedId: null);
        }

        /// <summary>
        /// Strips every config file in <paramref name="folder"/> except the resolved one.
        ///
        /// Separated from the build callback so tests can drive it against a throwaway folder:
        /// the tilde-rename behaviour is worth pinning without ever running a build, and a test
        /// must never touch the project's real config folder.
        /// </summary>
        /// <param name="folder">Absolute path of the folder holding the config files.</param>
        /// <param name="overrideResolvedId">
        /// Forces a resolved id instead of resolving one, so tests do not depend on the state of
        /// the config asset. null means resolve normally.
        /// </param>
        internal static List<string> Prepare(string folder, string overrideResolvedId)
        {
            var renamed = new List<string>();

            // A previous crash may have left files suffixed. Recover before doing anything else so
            // the enumeration below sees the real set of files.
            RestoreAll(folder);

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return renamed;
            }

            List<string> available = ListConfigFiles(folder);
            string resolvedId = overrideResolvedId ?? LootLockerConfig.ResolveFileConfigId(
                selectedId: LootLockerConfig.current != null ? LootLockerConfig.current.selectedFileConfigId : null,
                commandLineId: LootLockerConfig.GetCommandLineConfigId(Environment.GetCommandLineArgs()),
                buildPointerId: null,
                legacyFilePresent: File.Exists(Path.Combine(folder, $"{LootLockerConfig.ConfigFileNameForLegacy}{LootLockerConfig.ConfigFileExtensionForBuild}")),
                availableIds: IdsOf(available));

            // Nothing to strip when no config file is in effect, and nothing to read either.
            if (resolvedId == null)
            {
                DeleteBuildPointer(folder);
                return renamed;
            }

            string keepFileName = LootLockerConfig.GetConfigFileNameForId(resolvedId) + LootLockerConfig.ConfigFileExtensionForBuild;
            string keepPath = Path.Combine(folder, keepFileName);

            if (!File.Exists(keepPath))
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

            WriteBuildPointer(folder, resolvedId);

            return renamed;
        }

        /// <summary>
        /// Hides a file from the build by suffixing it with '~' and moving its .meta alongside as
        /// plain data, so the GUID survives the round trip.
        /// </summary>
        private static void HideFile(string path)
        {
            string suffixed = path + LootLockerConfig.BuildExcludeSuffix;
            string meta = path + ".meta";

            string suffixedMeta = suffixed + ".meta";

            if (File.Exists(meta))
            {
                File.Move(meta, suffixedMeta);
            }

            File.Move(path, suffixed);
        }

        /// <summary>
        /// Un-hides a file hidden by <see cref="HideFile"/>.
        /// </summary>
        private static void UnhideFile(string suffixed)
        {
            string original = suffixed.Substring(0, suffixed.Length - LootLockerConfig.BuildExcludeSuffix.Length);
            string suffixedMeta = suffixed + ".meta";
            string meta = original + ".meta";

            if (File.Exists(meta))
            {
                File.Delete(meta);
            }

            File.Move(suffixed, original);

            if (File.Exists(suffixedMeta))
            {
                File.Move(suffixedMeta, meta);
            }
        }

        /// <summary>
        /// Undoes every rename recorded in the marker and deletes the build pointer.
        ///
        /// Idempotent: safe to call when nothing was stripped, and safe to call twice.
        /// </summary>
        internal static void RestoreAll()
        {
            RestoreAll(ConfigFolderAbsolutePath());
        }

        /// <summary>
        /// Reverts every rename recorded in the marker for <paramref name="folder"/>.
        /// </summary>
        internal static void RestoreAll(string folder)
        {
            foreach (string original in ReadMarker())
            {
                string suffixed = original + LootLockerConfig.BuildExcludeSuffix;

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

            if (!string.IsNullOrEmpty(folder))
            {
                DeleteBuildPointer(folder);
            }

            if (folder != null)
            {
                AssetDatabase.Refresh();
            }
        }

        private static void WriteBuildPointer(string folder, string id)
        {
            string path = Path.Combine(folder, LootLockerConfig.BuildPointerName + LootLockerConfig.ConfigFileExtensionForBuild);
            string json = "{\n  \"selected_file_config_id\": " + LootLockerJson.SerializeObject(id) + "\n}\n";
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(ToAssetPath(path), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private static void DeleteBuildPointer(string folder)
        {
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

        private static List<string> ListConfigFiles(string folder)
        {
            var files = new List<string>();
            foreach (string path in Directory.GetFiles(folder, "*" + LootLockerConfig.ConfigFileExtensionForBuild))
            {
                // Already-hidden leftovers belong to a crashed run and were restored above.
                if (Path.GetFileName(path).EndsWith(LootLockerConfig.BuildExcludeSuffix, StringComparison.Ordinal))
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

        private static string ConfigFolderAbsolutePath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string relative = LootLockerConfig.ConfigFolderAssetsPathForBuild;
            if (string.IsNullOrEmpty(relative))
            {
                return null;
            }

            return Path.Combine(projectRoot, relative.Replace('/', Path.DirectorySeparatorChar));
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
