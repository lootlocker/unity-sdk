using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace LootLocker
{
    /// <summary>
    /// Deciding which config file applies, and where config files live.
    ///
    /// Part of <see cref="LootLockerConfig"/>; split out to keep the asset class readable.
    /// </summary>
    public partial class LootLockerConfig
    {
        /// <summary>
        /// Allowed characters for a file config identifier. The identifier becomes part of a
        /// filename, so path separators, traversal sequences and characters that are illegal on
        /// Windows must be rejected.
        ///
        /// Anchored with \z rather than $: in .NET, $ also matches before a trailing newline, so
        /// "prod\n" would pass and then reach the file name and the Resources lookup.
        /// </summary>
        internal const string FileConfigIdPattern = @"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}\z";

        /// <summary>
        /// True when <paramref name="id"/> is a syntactically valid file config identifier.
        /// The empty string is valid and means "the legacy, unsuffixed config file".
        /// </summary>
        internal static bool IsValidFileConfigId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return true;
            }

            return Regex.IsMatch(id, FileConfigIdPattern);
        }

        /// <summary>
        /// Builds the Resources-relative name of the config file for an identifier.
        /// The empty identifier maps to the legacy unsuffixed name.
        /// </summary>
        internal static string GetConfigFileNameForId(string id)
        {
            return string.IsNullOrEmpty(id) ? SettingsName : $"{SettingsName}-{id}";
        }

        /// <summary>
        /// Decides which file config identifier should be used.
        ///
        /// Pure: no Unity I/O and no static state, so it can be unit tested directly.
        /// </summary>
        /// <param name="selectedId">
        /// The identifier stored in the config asset. null means "auto-detect" (legacy behaviour),
        /// the empty string means "explicitly none".
        /// </param>
        /// <param name="buildPointerId">Identifier written by a build preprocess step, if any.</param>
        /// <param name="commandLineId">Identifier supplied on the command line, if any.</param>
        /// <param name="legacyFilePresent">Whether the legacy unsuffixed config file exists.</param>
        /// <param name="availableIds">Identifiers that were discovered on disk.</param>
        /// <returns>
        /// The identifier to use, or null when no file config should be applied.
        /// </returns>
        internal static string ResolveFileConfigId(
            string selectedId,
            string commandLineId,
            string buildPointerId,
            bool legacyFilePresent,
            IReadOnlyCollection<string> availableIds)
        {
            // 1. Command line wins: CI and release injection.
            if (commandLineId != null)
            {
                if (commandLineId == "none")
                {
                    return null;
                }

                return IsValidFileConfigId(commandLineId) ? commandLineId : null;
            }

            // 2. A build pointer is authoritative for the artifact it was written for.
            if (!string.IsNullOrEmpty(buildPointerId))
            {
                return IsValidFileConfigId(buildPointerId) ? buildPointerId : null;
            }

            // 3. Explicit selection in the config asset.
            if (selectedId != null)
            {
                // Empty string means "explicitly no file config".
                if (selectedId.Length == 0)
                {
                    return null;
                }

                return IsValidFileConfigId(selectedId) ? selectedId : null;
            }

            // 4. Auto-detect: legacy behaviour, unchanged for existing projects.
            if (legacyFilePresent)
            {
                return string.Empty;
            }

            // 5. A single discovered profile is unambiguous, so use it.
            if (availableIds != null && availableIds.Count == 1)
            {
                foreach (string id in availableIds)
                {
                    return id;
                }
            }

            return null;
        }


        /// <summary>
        /// State of a discovered config file.
        /// </summary>
        internal enum FileConfigState
        {
            /// <summary>Parses and has an api_key. Selectable.</summary>
            Valid = 0,
            /// <summary>Exists but does not parse, or has no api_key. Shown but not selectable.</summary>
            Invalid = 1,
            /// <summary>Could not be read at all. Shown but not selectable.</summary>
            Unreadable = 2,
        }

        /// <summary>
        /// A config file found on disk, with its classification.
        /// </summary>
        internal struct FileConfigDescriptor
        {
            /// <summary>Identifier, or the empty string for the legacy unsuffixed file.</summary>
            public string Id;
            /// <summary>Whether the file can actually be used.</summary>
            public FileConfigState State;
            /// <summary>Why the file is not usable, when it is not.</summary>
            public string Reason;
        }

        /// <summary>
        /// Extracts the identifier from a config file name, or null when the name is not a config file.
        /// </summary>
        /// <param name="fileName">File name including extension, e.g. "LootLockerConfig-prod.bytes".</param>
        internal static string GetIdFromConfigFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            if (!fileName.EndsWith(ConfigFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string withoutExtension = fileName.Substring(0, fileName.Length - ConfigFileExtension.Length);

            // Files ending in '~' are excluded from the build by the preprocess step; never list them.
            if (withoutExtension.EndsWith("~", StringComparison.Ordinal))
            {
                return null;
            }

            if (string.Equals(withoutExtension, SettingsName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            // The build pointer is generated, never hand-authored, and must not be offered as a
            // selectable profile.
            if (string.Equals(withoutExtension, BuildPointerName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string prefix = SettingsName + "-";
            if (!withoutExtension.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string id = withoutExtension.Substring(prefix.Length);

            // "LootLockerConfig-.bytes" has an empty suffix. IsValidFileConfigId accepts "" because
            // that is the legacy file's identifier, so without this check the stray file would be
            // classified as the legacy config and could collide with the real one during dedupe.
            if (id.Length == 0)
            {
                return null;
            }

            return IsValidFileConfigId(id) ? id : null;
        }

        /// <summary>
        /// Discovers and classifies the config files available on disk.
        ///
        /// The I/O is injected so this can be unit tested without an AssetDatabase. Classification
        /// is a local parse only: no network calls and no key verification.
        /// </summary>
        /// <param name="listFileNames">Returns the file names present in the config folder.</param>
        /// <param name="readText">Reads a file's contents by file name, or returns null on failure.</param>
        internal static List<FileConfigDescriptor> DiscoverFileConfigs(
            Func<IEnumerable<string>> listFileNames,
            Func<string, string> readText)
        {
            var descriptors = new List<FileConfigDescriptor>();
            if (listFileNames == null)
            {
                return descriptors;
            }

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string fileName in listFileNames())
            {
                string id = GetIdFromConfigFileName(fileName);
                if (id == null)
                {
                    continue;
                }

                // Case-insensitive dedupe: "Prod.bytes" and "prod.bytes" must not both be listed.
                if (!seenIds.Add(id))
                {
                    continue;
                }

                var descriptor = new FileConfigDescriptor { Id = id, State = FileConfigState.Valid };

                string text = null;
                try
                {
                    text = readText?.Invoke(fileName);
                }
                catch (Exception ex)
                {
                    descriptor.State = FileConfigState.Unreadable;
                    descriptor.Reason = ex.Message;
                    descriptors.Add(descriptor);
                    continue;
                }

                if (text == null)
                {
                    descriptor.State = FileConfigState.Unreadable;
                    descriptor.Reason = "File could not be read.";
                }
                else if (!TryParseFileConfig(text, out _, out string error))
                {
                    descriptor.State = FileConfigState.Invalid;
                    descriptor.Reason = error;
                }

                descriptors.Add(descriptor);
            }

            descriptors.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));
            return descriptors;
        }

    }
}
