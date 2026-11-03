using System;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    /// <summary>
    /// Parsing of config file text into <see cref="ExternalFileConfig"/>.
    ///
    /// Part of <see cref="LootLockerConfig"/>; split out to keep the asset class readable.
    /// </summary>
    public partial class LootLockerConfig
    {
        /// <summary>
        /// Parses the contents of a config file into an <see cref="ExternalFileConfig"/>.
        ///
        /// Pure with respect to Unity: the caller supplies the text, so this can be unit tested
        /// without touching the AssetDatabase.
        /// </summary>
        /// <param name="configText">Raw file contents, either plain JSON or an encrypted base64 payload.</param>
        /// <param name="config">The parsed config, or null on failure.</param>
        /// <param name="error">A human readable reason when parsing fails.</param>
        /// <returns>True when a usable config (with a non-empty api_key) was produced.</returns>
        internal static bool TryParseFileConfig(string configText, out ExternalFileConfig config, out string error)
        {
            if (!TryParseSparseFileConfig(configText, out config, out _, out error))
            {
                return false;
            }

            if (string.IsNullOrEmpty(config.api_key))
            {
                error = "Config file has no api_key.";
                config = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses a config file without requiring an api_key, and reports which fields the file
        /// actually contained.
        ///
        /// The field set is what makes a sparse file meaningful: a field that is absent must be
        /// distinguishable from a field that is present and set to its default value, otherwise a
        /// local override of one setting would silently reset every other one.
        /// </summary>
        /// <param name="configText">Raw file contents, either plain JSON or an encrypted base64 payload.</param>
        /// <param name="config">The parsed config, or null on failure.</param>
        /// <param name="presentFields">JSON field names that appeared in the file.</param>
        /// <param name="error">A human readable reason when parsing fails.</param>
        internal static bool TryParseSparseFileConfig(
            string configText,
            out ExternalFileConfig config,
            out HashSet<string> presentFields,
            out string error)
        {
            config = null;
            presentFields = null;
            error = null;

            if (string.IsNullOrEmpty(configText))
            {
                error = "Config file is empty.";
                return false;
            }

            string json;
            try
            {
                json = configText;
                if (LootLocker.Utilities.Encryption.LootLockerEncryptionUtilities.IsValidBase64String(configText))
                {
                    json = LootLocker.Utilities.Encryption.LootLockerEncryptionUtilities.SimpleDecryptFromBase64(configText);
                }
            }
            catch (Exception ex)
            {
                error = $"Config file could not be decrypted: {ex.Message}";
                return false;
            }

            try
            {
                config = LootLockerJson.DeserializeObject<ExternalFileConfig>(json);
                presentFields = GetPresentFieldNames(json);
            }
            catch (Exception ex)
            {
                error = $"Config file could not be parsed: {ex.Message}";
                config = null;
                return false;
            }

            if (config == null)
            {
                error = "Config file could not be parsed.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Field names present in a JSON object. Returns an empty set rather than throwing when the
        /// document is not an object, so a caller that already parsed the config is never failed by
        /// this second pass.
        /// </summary>
        private static HashSet<string> GetPresentFieldNames(string json)
        {
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                Dictionary<string, object> raw = LootLockerJson.DeserializeObject<Dictionary<string, object>>(json);
                if (raw != null)
                {
                    foreach (string key in raw.Keys)
                    {
                        fields.Add(key);
                    }
                }
            }
            catch (Exception)
            {
                // Not an object document. The config itself parsed, so treat it as having no
                // explicitly present fields rather than failing the whole load.
            }

            return fields;
        }
    }
}
