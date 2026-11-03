using System;
using System.Collections.Generic;
using System.IO;

namespace LootLocker
{
#if UNITY_EDITOR
    /// <summary>
    /// The local, per-machine override layer.
    ///
    /// Split out of <see cref="LootLockerConfig"/> to keep the asset class readable.
    ///
    /// Editor only: the file lives outside Resources/ so it never ships, and it is a developer
    /// convenience rather than a build input.
    /// </summary>
    internal static class LootLockerConfigFileLocalOverride
    {
        /// <summary>
        /// Folder that holds the local override file (excluded from version control).
        ///
        /// Deliberately outside Resources/. Anything under Resources/ is packed into every build
        /// made on this machine, and guarding the read path with UNITY_EDITOR only stops the SDK
        /// from *using* it - the bytes would still be in the build data for anyone to extract. The
        /// file is a developer convenience, so it must not ship at all.
        /// </summary>
        internal static string LocalOverrideFolder =>
            $"{LootLockerConfig.GetSdkRootAssetsPath(requireWritable: true)}/{LootLockerConfig.LocalOverrideFolderName}";

        /// <summary>
        /// Absolute path of the local override file (excluded from version control).
        /// </summary>
        internal static string LocalOverridePath =>
            $"{LocalOverrideFolder}/{LootLockerConfig.LocalOverrideName}{LootLockerConfig.ConfigFileExtension}";

        /// <summary>
        /// Reads the local override file's text straight from disk.
        ///
        /// Not Resources.Load: the file is outside Resources/ by design, and it is a plain file
        /// rather than a TextAsset the AssetDatabase imports.
        /// </summary>
        internal static string ReadLocalOverrideText()
        {
            try
            {
                string path = LocalOverridePath;
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Could not read the local override file: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                return null;
            }
        }

        /// <summary>
        /// Writes the local override file with the given JSON body, replacing any existing one.
        /// </summary>
        /// <returns>True when the file was written.</returns>
        internal static bool WriteLocalOverride(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                string path = LocalOverridePath;
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // No AssetDatabase import: the file is outside Resources/ and is read straight from
                // disk, so importing it would add it to the build for no benefit.
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Could not write the local override file: {ex.Message}", LootLockerLogger.LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Deletes the local override file, if present, and re-applies the shared config.
        /// </summary>
        /// <returns>True when no local override file remains.</returns>
        internal static bool DeleteLocalOverride()
        {
            try
            {
                string path = LocalOverridePath;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                LootLockerConfig.RefreshFileConfigState(force: true);
                return true;
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Could not delete the local override file: {ex.Message}", LootLockerLogger.LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Reads the local override file as a mutable field map, or an empty map when there is none.
        ///
        /// Values are kept as raw JSON text so a field can be written back without knowing its
        /// type, and so a field this SDK version does not understand survives a round trip.
        /// </summary>
        internal static Dictionary<string, string> ReadLocalOverrideFields()
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string text = ReadLocalOverrideText();
            if (string.IsNullOrEmpty(text))
            {
                return fields;
            }

            try
            {
                string json = text;
                if (LootLocker.Utilities.Encryption.LootLockerEncryptionUtilities.IsValidBase64String(text))
                {
                    json = LootLocker.Utilities.Encryption.LootLockerEncryptionUtilities.SimpleDecryptFromBase64(text);
                }

                Dictionary<string, object> raw = LootLockerJson.DeserializeObject<Dictionary<string, object>>(json);
                if (raw == null)
                {
                    return fields;
                }

                foreach (KeyValuePair<string, object> pair in raw)
                {
                    fields[pair.Key] = SerializeOverrideValue(pair.Value);
                }
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log(
                    $"Local override file could not be read ({ex.Message}). Treating it as empty.",
                    LootLockerLogger.LogLevel.Warning);
            }

            return fields;
        }

        /// <summary>
        /// Writes a field map back to the local override file, deleting the file when the map is
        /// empty so an empty override never lingers on disk.
        /// </summary>
        internal static bool WriteLocalOverrideFields(Dictionary<string, string> fields)
        {
            if (fields == null || fields.Count == 0)
            {
                return DeleteLocalOverride();
            }

            var builder = new System.Text.StringBuilder();
            builder.Append("{\n");
            int index = 0;
            foreach (KeyValuePair<string, string> pair in fields)
            {
                builder.Append("  \"").Append(pair.Key).Append("\": ").Append(pair.Value);
                if (++index < fields.Count)
                {
                    builder.Append(',');
                }
                builder.Append('\n');
            }
            builder.Append("}\n");

            return WriteLocalOverride(builder.ToString());
        }

        /// <summary>
        /// Adds or replaces one field in the local override file.
        /// </summary>
        /// <param name="fieldName">JSON field name, see <see cref="LootLockerConfigFields"/>.</param>
        /// <param name="jsonValue">The value as JSON text, e.g. <c>"Verbose"</c> or <c>true</c>.</param>
        internal static bool SetLocalOverrideField(string fieldName, string jsonValue)
        {
            if (string.IsNullOrEmpty(fieldName) || jsonValue == null)
            {
                return false;
            }

            Dictionary<string, string> fields = ReadLocalOverrideFields();
            fields[fieldName] = jsonValue;
            return WriteLocalOverrideFields(fields);
        }

        /// <summary>
        /// Removes one field from the local override file, deleting the file when it was the last
        /// one. Removing a field that is not overridden is a no-op.
        /// </summary>
        internal static bool RemoveLocalOverrideField(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            Dictionary<string, string> fields = ReadLocalOverrideFields();
            if (!fields.Remove(fieldName))
            {
                return true;
            }

            return WriteLocalOverrideFields(fields);
        }

        /// <summary>
        /// Serializes a value read from the override file back to JSON text.
        /// </summary>
        private static string SerializeOverrideValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is string text)
            {
                return LootLockerJson.SerializeObject(text);
            }

            if (value is bool flag)
            {
                return flag ? "true" : "false";
            }

            // Numbers and anything else round trip through the serializer unchanged.
            return LootLockerJson.SerializeObject(value);
        }
    }
#endif
}
