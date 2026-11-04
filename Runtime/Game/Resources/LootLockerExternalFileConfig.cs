using System;

namespace LootLocker
{
[Serializable]
public class ExternalFileConfig
{
    public string api_key { get; set; }
    public string domain_key { get; set; }
    public string game_version { get; set; }
    public bool enable_presence { get; set; }
    public bool enable_presence_autoconnect { get; set; }
    public bool enable_presence_autodisconnect_on_focus_change { get; set; }
    public bool enable_presence_in_editor { get; set; }
    /// <summary>
    /// Legacy and ignored. The SDK version always resolves from the installed package, so a config
    /// file cannot pin a stale version. Kept so existing config files still parse.
    /// </summary>
    public string sdk_version { get; set; }
    public LootLockerLogger.LogLevel log_level { get; set; }
    public bool log_errors_as_warnings { get; set; }
    public bool log_in_builds { get; set; }
    public bool prettify_json { get; set; }
    public bool obfuscate_logs { get; set; }
    public bool allow_token_refresh { get; set; }
    public LootLockerMultiUserSessionMode multi_user_session_mode { get; set; }
    public bool enable_editor_admin_extension { get; set; } = true;
    /// <summary>
    /// Identifier of the config a build selected. Only ever set in the generated build pointer
    /// file, never in a hand-authored config.
    /// </summary>
    public string selected_file_config_id { get; set; }
}
}