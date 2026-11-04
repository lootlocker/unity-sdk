using System;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    /// <summary>
    /// Applying config layers onto the asset, and restoring the authored values.
    ///
    /// Split out of <see cref="LootLockerConfig"/> to keep the asset class readable.
    /// </summary>
    internal static class LootLockerConfigFileLayering
    {
        /// <summary>
        /// Applies the local override file (excluded from version control), if one exists.
        ///
        /// The file is sparse: only the fields it contains are applied, so a developer can flip one
        /// setting for an afternoon without forking the shared config. A file that cannot be read
        /// or parsed is reported once and otherwise ignored - it must never break loading.
        ///
        /// Editor only. See the call site in <see cref="RefreshFileConfigState"/> for why.
        /// </summary>
#if UNITY_EDITOR
        internal static void ApplyLocalOverride()
        {
            string text = LootLockerConfigFileLocalOverride.ReadLocalOverrideText();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!LootLockerConfigFileParsing.TryParseSparseFileConfig(text, out ExternalFileConfig localConfig, out HashSet<string> presentFields, out string error))
            {
                LootLockerConfig.WarnOnceForMissingConfig(LootLockerConfig.LocalOverrideName,
                    $"Local override file '{LootLockerConfig.LocalOverrideName}{LootLockerConfig.ConfigFileExtension}' is not usable ({error}). Ignoring it.");
                return;
            }

            LootLockerConfig.SetLocalOverrideFields(ApplyFileConfig(localConfig, OverridableFieldsOnly(presentFields)));
        }

        /// <summary>
        /// Removes the fields that may never be overridden locally from a sparse field set.
        ///
        /// The settings UI hides the override toggle for these, but the override file is a plain
        /// file a developer can hand-edit, so the restriction has to be enforced where the file is
        /// applied. Otherwise a stray <c>api_key</c> in the local file would silently point the
        /// game at another environment - exactly the trap the UI restriction exists to prevent.
        /// </summary>
        internal static HashSet<string> OverridableFieldsOnly(HashSet<string> fields)
        {
            if (fields == null)
            {
                return null;
            }

            var allowed = new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
            allowed.Remove(LootLockerConfigFields.ApiKey);
            allowed.Remove(LootLockerConfigFields.DomainKey);
            return allowed;
        }
#endif

        /// <summary>
        /// Copies the fields of a file config onto the live config.
        /// </summary>
        /// <param name="fileConfig">The parsed config to apply.</param>
        /// <param name="onlyFields">
        /// When non-null, only these JSON field names are applied. Used for the sparse local
        /// override file. When null every field is applied, which is the shared config behaviour.
        /// </param>
        /// <returns>The JSON field names that were actually applied.</returns>
        internal static HashSet<string> ApplyFileConfig(ExternalFileConfig fileConfig, HashSet<string> onlyFields)
        {
            var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool Applies(string fieldName)
            {
                return onlyFields == null || onlyFields.Contains(fieldName);
            }

            if (Applies(LootLockerConfigFields.ApiKey))
            {
                LootLockerConfig.RawConfigForLayering.apiKey = fileConfig.api_key;
                applied.Add(LootLockerConfigFields.ApiKey);
            }
            if (Applies(LootLockerConfigFields.DomainKey))
            {
                LootLockerConfig.RawConfigForLayering.domainKey = fileConfig.domain_key;
                applied.Add(LootLockerConfigFields.DomainKey);
            }
            if (Applies(LootLockerConfigFields.LogLevel))
            {
                LootLockerConfig.RawConfigForLayering.logLevel = fileConfig.log_level;
                applied.Add(LootLockerConfigFields.LogLevel);
            }
            if (Applies(LootLockerConfigFields.LogErrorsAsWarnings))
            {
                LootLockerConfig.RawConfigForLayering.logErrorsAsWarnings = fileConfig.log_errors_as_warnings;
                applied.Add(LootLockerConfigFields.LogErrorsAsWarnings);
            }
            if (Applies(LootLockerConfigFields.LogInBuilds))
            {
                LootLockerConfig.RawConfigForLayering.logInBuilds = fileConfig.log_in_builds;
                applied.Add(LootLockerConfigFields.LogInBuilds);
            }
            if (Applies(LootLockerConfigFields.EnablePresence))
            {
                LootLockerConfig.RawConfigForLayering.enablePresence = fileConfig.enable_presence;
                applied.Add(LootLockerConfigFields.EnablePresence);
            }
            if (Applies(LootLockerConfigFields.EnablePresenceAutoConnect))
            {
                LootLockerConfig.RawConfigForLayering.enablePresenceAutoConnect = fileConfig.enable_presence_autoconnect;
                applied.Add(LootLockerConfigFields.EnablePresenceAutoConnect);
            }
            if (Applies(LootLockerConfigFields.EnablePresenceAutoDisconnectOnFocusChange))
            {
                LootLockerConfig.RawConfigForLayering.enablePresenceAutoDisconnectOnFocusChange = fileConfig.enable_presence_autodisconnect_on_focus_change;
                applied.Add(LootLockerConfigFields.EnablePresenceAutoDisconnectOnFocusChange);
            }
            if (Applies(LootLockerConfigFields.EnablePresenceInEditor))
            {
                LootLockerConfig.RawConfigForLayering.enablePresenceInEditor = fileConfig.enable_presence_in_editor;
                applied.Add(LootLockerConfigFields.EnablePresenceInEditor);
            }
            if (Applies(LootLockerConfigFields.AllowTokenRefresh))
            {
                LootLockerConfig.RawConfigForLayering.allowTokenRefresh = fileConfig.allow_token_refresh;
                applied.Add(LootLockerConfigFields.AllowTokenRefresh);
            }
            if (Applies(LootLockerConfigFields.PrettifyJson))
            {
                LootLockerConfig.RawConfigForLayering.prettifyJson = fileConfig.prettify_json;
                applied.Add(LootLockerConfigFields.PrettifyJson);
            }
            if (Applies(LootLockerConfigFields.ObfuscateLogs))
            {
                LootLockerConfig.RawConfigForLayering.obfuscateLogs = fileConfig.obfuscate_logs;
                applied.Add(LootLockerConfigFields.ObfuscateLogs);
            }
            if (Applies(LootLockerConfigFields.MultiUserSessionMode)
                && fileConfig.multi_user_session_mode != LootLockerMultiUserSessionMode.NotSet)
            {
                LootLockerConfig.RawConfigForLayering.multiUserSessionMode = fileConfig.multi_user_session_mode;
                applied.Add(LootLockerConfigFields.MultiUserSessionMode);
            }
            if (Applies(LootLockerConfigFields.EnableEditorAdminExtension))
            {
                LootLockerConfig.RawConfigForLayering.enableEditorAdminExtension = fileConfig.enable_editor_admin_extension;
                applied.Add(LootLockerConfigFields.EnableEditorAdminExtension);
            }

#if UNITY_EDITOR
            // Only a full shared config invalidates the admin session. The sparse local override
            // path calls this too, and clearing the token there would sign the developer out just
            // for toggling one field.
            if (onlyFields == null)
            {
                LootLockerConfig.RawConfigForLayering.adminToken = null;
            }
#endif
            return applied;
        }

        /// <summary>
        /// Values the config asset itself holds, captured before any file config is applied.
        ///
        /// <see cref="Resources.Load{T}"/> returns the already-loaded instance, which is
        /// <see cref="LootLockerConfig.RawConfigForLayering"/> itself, so re-loading cannot undo an in-place mutation. Keeping a
        /// snapshot is the only way to get back to the authored values when switching configs.
        /// </summary>
        private static LootLockerConfig _assetSnapshot;

        /// <summary>
        /// Captures the authored values of the config asset, once per domain.
        /// </summary>
        internal static void CaptureAssetSnapshot()
        {
            if (_assetSnapshot != null || LootLockerConfig.RawConfigForLayering == null)
            {
                return;
            }

            _assetSnapshot = ScriptableObject.CreateInstance<LootLockerConfig>();
            _assetSnapshot.hideFlags = HideFlags.HideAndDontSave;
            CopyConfigValues(LootLockerConfig.RawConfigForLayering, _assetSnapshot);
        }

        /// <summary>
        /// Records a single value the developer just authored in the settings UI into the snapshot,
        /// so a later restore does not revert it.
        ///
        /// Needed because the UI edits the live asset directly. Without this the snapshot keeps
        /// whatever the asset held at load, and every reload - a Refresh, an override change, or
        /// selecting a config file - writes the stale value back over the fresh edit.
        /// </summary>
        /// <param name="fieldName">JSON field name, see <see cref="LootLockerConfigFields"/>.</param>
        internal static void CommitAuthoredValue(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName) || !IsFileConfigField(fieldName))
            {
                return;
            }

            CaptureAssetSnapshot();
            if (_assetSnapshot == null || LootLockerConfig.RawConfigForLayering == null)
            {
                return;
            }

            CopyOneConfigValue(LootLockerConfig.RawConfigForLayering, _assetSnapshot, fieldName);
        }

        /// <summary>
        /// Copies one governed field between configs, by JSON field name.
        /// </summary>
        private static void CopyOneConfigValue(LootLockerConfig from, LootLockerConfig to, string fieldName)
        {
            switch (fieldName)
            {
                case LootLockerConfigFields.ApiKey: to.apiKey = from.apiKey; break;
                case LootLockerConfigFields.DomainKey: to.domainKey = from.domainKey; break;
                case LootLockerConfigFields.GameVersion: to.game_version = from.game_version; break;
                case LootLockerConfigFields.LogLevel: to.logLevel = from.logLevel; break;
                case LootLockerConfigFields.LogErrorsAsWarnings: to.logErrorsAsWarnings = from.logErrorsAsWarnings; break;
                case LootLockerConfigFields.LogInBuilds: to.logInBuilds = from.logInBuilds; break;
                case LootLockerConfigFields.EnablePresence: to.enablePresence = from.enablePresence; break;
                case LootLockerConfigFields.EnablePresenceAutoConnect: to.enablePresenceAutoConnect = from.enablePresenceAutoConnect; break;
                case LootLockerConfigFields.EnablePresenceAutoDisconnectOnFocusChange: to.enablePresenceAutoDisconnectOnFocusChange = from.enablePresenceAutoDisconnectOnFocusChange; break;
                case LootLockerConfigFields.EnablePresenceInEditor: to.enablePresenceInEditor = from.enablePresenceInEditor; break;
                case LootLockerConfigFields.AllowTokenRefresh: to.allowTokenRefresh = from.allowTokenRefresh; break;
                case LootLockerConfigFields.PrettifyJson: to.prettifyJson = from.prettifyJson; break;
                case LootLockerConfigFields.ObfuscateLogs: to.obfuscateLogs = from.obfuscateLogs; break;
                case LootLockerConfigFields.MultiUserSessionMode: to.multiUserSessionMode = from.multiUserSessionMode; break;
                case LootLockerConfigFields.EnableEditorAdminExtension: to.enableEditorAdminExtension = from.enableEditorAdminExtension; break;
            }
        }

        /// <summary>
        /// True when the field is one a config file or the settings UI can govern.
        /// </summary>
        private static bool IsFileConfigField(string fieldName)
        {
            switch (fieldName)
            {
                case LootLockerConfigFields.ApiKey:
                case LootLockerConfigFields.DomainKey:
                case LootLockerConfigFields.GameVersion:
                case LootLockerConfigFields.LogLevel:
                case LootLockerConfigFields.LogErrorsAsWarnings:
                case LootLockerConfigFields.LogInBuilds:
                case LootLockerConfigFields.EnablePresence:
                case LootLockerConfigFields.EnablePresenceAutoConnect:
                case LootLockerConfigFields.EnablePresenceAutoDisconnectOnFocusChange:
                case LootLockerConfigFields.EnablePresenceInEditor:
                case LootLockerConfigFields.AllowTokenRefresh:
                case LootLockerConfigFields.PrettifyJson:
                case LootLockerConfigFields.ObfuscateLogs:
                case LootLockerConfigFields.MultiUserSessionMode:
                case LootLockerConfigFields.EnableEditorAdminExtension:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Restores the authored values of the config asset, discarding anything applied from a
        /// file config or local override.
        ///
        /// The settings UI must call this before saving the asset: the live object holds applied
        /// values, so saving it directly would copy a file config's api_key into the committed
        /// config.
        /// </summary>
        internal static void RestoreAssetValues()
        {
            ReloadConfigAsset();
        }

        /// <summary>
        /// Restores the authored values of the config asset so that file config values applied
        /// earlier are discarded. Without this, switching configs cannot revert fields the new
        /// config omits.
        /// </summary>
        internal static void ReloadConfigAsset()
        {
            CaptureAssetSnapshot();
            if (_assetSnapshot == null || LootLockerConfig.RawConfigForLayering == null)
            {
                return;
            }

            CopyConfigValues(_assetSnapshot, LootLockerConfig.RawConfigForLayering);
        }

        /// <summary>
        /// Values supplied by an explicit <c>Init(...)</c> call, or null when none was made.
        ///
        /// Init outranks every file-based source, but it writes onto the same live object the file
        /// layers are applied to. Without keeping the values here, any forced refresh - which
        /// restores the authored snapshot - would silently discard them.
        /// </summary>
        private static LootLockerConfig _initOverrides;

        /// <summary>
        /// Records the values from an explicit Init call so they survive a reload.
        /// </summary>
        internal static void RecordInitValues(Action<LootLockerConfig> record)
        {
            if (record == null)
            {
                return;
            }

            if (_initOverrides == null)
            {
                _initOverrides = ScriptableObject.CreateInstance<LootLockerConfig>();
                _initOverrides.hideFlags = HideFlags.HideAndDontSave;
            }

            record(_initOverrides);
        }

        /// <summary>
        /// Re-applies Init values over whatever the file layers produced.
        /// </summary>
        internal static void ReapplyInitValues()
        {
            if (_initOverrides == null || LootLockerConfig.RawConfigForLayering == null)
            {
                return;
            }

            CopyInitValues(_initOverrides, LootLockerConfig.RawConfigForLayering);
        }

        /// <summary>
        /// Copies recorded Init values onto the live config.
        ///
        /// Differs from <see cref="CopyConfigValues"/> in one place: a session mode of
        /// <see cref="LootLockerMultiUserSessionMode.NotSet"/> means the Init call did not specify
        /// one, so it must not be copied back. Copying it would overwrite a session mode that the
        /// config file legitimately set, turning "unspecified" into "reset to default".
        /// </summary>
        private static void CopyInitValues(LootLockerConfig from, LootLockerConfig to)
        {
            to.apiKey = from.apiKey;
            to.domainKey = from.domainKey;
            to.game_version = from.game_version;
            to.logLevel = from.logLevel;
            to.logErrorsAsWarnings = from.logErrorsAsWarnings;
            to.logInBuilds = from.logInBuilds;
            to.enablePresence = from.enablePresence;
            to.enablePresenceAutoConnect = from.enablePresenceAutoConnect;
            to.enablePresenceAutoDisconnectOnFocusChange = from.enablePresenceAutoDisconnectOnFocusChange;
            to.enablePresenceInEditor = from.enablePresenceInEditor;
            to.allowTokenRefresh = from.allowTokenRefresh;
            to.prettifyJson = from.prettifyJson;
            to.obfuscateLogs = from.obfuscateLogs;
            to.enableEditorAdminExtension = from.enableEditorAdminExtension;

            if (from.multiUserSessionMode != LootLockerMultiUserSessionMode.NotSet)
            {
                to.multiUserSessionMode = from.multiUserSessionMode;
            }
        }

        /// <summary>
        /// True when an explicit Init call is in effect, meaning the live values are not the ones
        /// stored in the asset and must not be saved back to it.
        /// </summary>
        internal static bool HasInitOverrides => _initOverrides != null;

        /// <summary>
        /// Forgets the recorded Init values, restoring file-based precedence.
        ///
        /// Needed because the recorder has no natural lifetime end: an editor session that calls
        /// Init once - including a test - would otherwise keep re-applying those values over every
        /// later config change.
        /// </summary>
        internal static void ClearInitValues()
        {
            if (_initOverrides == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(_initOverrides);
            _initOverrides = null;
        }

        /// <summary>
        /// Forgets the snapshot and any recorded Init values.
        ///
        /// Called when the cached config state is reset (entering play mode with domain reload
        /// disabled). Without it the statics outlive the reset: the next load would skip capturing
        /// the authored values because a snapshot already exists, and a previous Init call would be
        /// re-applied over the fresh load.
        /// </summary>
        internal static void ResetState()
        {
            if (_assetSnapshot != null)
            {
                UnityEngine.Object.DestroyImmediate(_assetSnapshot);
                _assetSnapshot = null;
            }

            ClearInitValues();
        }

        /// <summary>
        /// Copies every field a file config may govern from one config to another.
        ///
        /// Deliberately field-by-field rather than a blanket copy: the asset also holds editor-only
        /// state (the admin token) and the saved selection, which must survive a reload.
        /// </summary>
        internal static void CopyConfigValues(LootLockerConfig from, LootLockerConfig to)
        {
            to.apiKey = from.apiKey;
            to.domainKey = from.domainKey;
            to.game_version = from.game_version;
            to.logLevel = from.logLevel;
            to.logErrorsAsWarnings = from.logErrorsAsWarnings;
            to.logInBuilds = from.logInBuilds;
            to.enablePresence = from.enablePresence;
            to.enablePresenceAutoConnect = from.enablePresenceAutoConnect;
            to.enablePresenceAutoDisconnectOnFocusChange = from.enablePresenceAutoDisconnectOnFocusChange;
            to.enablePresenceInEditor = from.enablePresenceInEditor;
            to.allowTokenRefresh = from.allowTokenRefresh;
            to.prettifyJson = from.prettifyJson;
            to.obfuscateLogs = from.obfuscateLogs;
            to.multiUserSessionMode = from.multiUserSessionMode;
            to.enableEditorAdminExtension = from.enableEditorAdminExtension;
        }
    }
}
