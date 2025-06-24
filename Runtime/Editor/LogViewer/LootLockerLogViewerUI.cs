#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using LootLocker;

namespace LootLocker.LogViewer
{
    public class LogViewerUI : LootLockerLogListener, LootLockerLogger.ILootLockerHttpLogListener
    {
        private Button logViewerBackBtn, clearLogsBtn, exportLogsBtn;
        private Toggle autoScrollToggle, showAdminToggle;
        private TextField logSearchField;
        private DropdownField logLevelDropdown;
        private ScrollView logScrollView;
        private VisualElement logContainer;
        private Label logStatusLabel;
        private List<LogEntry> logEntries = new List<LogEntry>();
        private List<ILogViewerEntry> filteredLogEntries = new List<ILogViewerEntry>();
        private string logListenerIdentifier;
        private string searchFilter = "";
        private LootLockerLogger.LogLevel logLevelFilter = LootLockerLogger.LogLevel.Debug;
        private bool autoScroll = true;
        private const int MAX_LOG_ENTRIES = 1000;
        private bool showAllLogLevels = true;
        private bool showAdminRequests = false;
        private List<LootLockerLogger.LootLockerHttpLogEntry> httpLogEntries = new List<LootLockerLogger.LootLockerHttpLogEntry>();
        private List<ILogViewerEntry> allLogEntries = new List<ILogViewerEntry>();
        public enum NetworkRequestType { Request, Response, Error }
        public interface ILogViewerEntry { DateTime Timestamp { get; } }
        public class LogEntry : ILogViewerEntry
        {
            public LootLockerLogger.LogLevel level;
            public string message;
            public Color color;
            public NetworkRequestType? networkType;
            public DateTime Timestamp { get; set; } = DateTime.Now;
        }

        public class HttpLogEntry : ILogViewerEntry
        {
            public LootLockerLogger.LootLockerHttpLogEntry http;
            public DateTime Timestamp { get; set; } = DateTime.Now;
        }

        public void InitializeLogViewerUI(VisualElement root, Action onBack = null)
        {
            logViewerBackBtn = root.Q<Button>("LogViewerBackBtn");
            clearLogsBtn = root.Q<Button>("ClearLogsBtn");
            exportLogsBtn = root.Q<Button>("ExportLogsBtn");
            autoScrollToggle = root.Q<Toggle>("AutoScrollToggle");
            logSearchField = root.Q<TextField>("LogSearchField");
            logLevelDropdown = root.Q<DropdownField>("LogLevelDropdown");
            logScrollView = root.Q<ScrollView>("LogScrollView");
            logContainer = root.Q<VisualElement>("LogContainer");
            logStatusLabel = root.Q<Label>("LogStatusLabel");
            showAdminToggle = root.Q<Toggle>("ShowAdminToggle");
            InitializeLogViewerEventHandlers(onBack);
            SetupLogLevelDropdown();
            RegisterLogListener();
            showAllLogLevels = true;
            showAdminRequests = false;
            FilterLogs();
        }

        public void RemoveLogViewerUI()
        {
            UnregisterLogListener();
        }

        private void InitializeLogViewerEventHandlers(Action onBack)
        {
            if (logViewerBackBtn != null)
            {
                if (onBack != null)
                    logViewerBackBtn.clickable.clicked += () => onBack();
                else
                    logViewerBackBtn.style.display = DisplayStyle.None;
            }
            if (clearLogsBtn != null)
                clearLogsBtn.clickable.clicked += ClearLogs;
            if (exportLogsBtn != null)
                exportLogsBtn.clickable.clicked += ExportLogs;
            if (autoScrollToggle != null)
                autoScrollToggle.RegisterValueChangedCallback(evt => { autoScroll = evt.newValue; });
            if (logSearchField != null)
                logSearchField.RegisterValueChangedCallback(evt => { searchFilter = evt.newValue; FilterLogs(); });
            if (logLevelDropdown != null)
                logLevelDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == "All")
                    {
                        showAllLogLevels = true;
                        FilterLogs();
                    }
                    else if (Enum.TryParse<LootLockerLogger.LogLevel>(evt.newValue, out var level))
                    {
                        logLevelFilter = level;
                        showAllLogLevels = false;
                        FilterLogs();
                    }
                });
            if (showAdminToggle != null)
            {
                showAdminToggle.value = false;
                showAdminToggle.RegisterValueChangedCallback(evt =>
                {
                    showAdminRequests = evt.newValue;
                    FilterLogs();
                });
            }
        }
        private void SetupLogLevelDropdown()
        {
            if (logLevelDropdown == null) return;
            var choices = new List<string> { "All" };
            choices.AddRange(Enum.GetNames(typeof(LootLockerLogger.LogLevel)).Where(name => name != "None"));
            logLevelDropdown.choices = choices;
            logLevelDropdown.value = "All";
        }
        public void Log(LootLockerLogger.LogLevel logLevel, string message)
        {
            // Skip regular HTTP log lines (let the enriched HTTP log handle it)
            if (message.StartsWith("[LL HTTP]") || message.StartsWith("[LL HTTP RESPONSE]"))
                return;
            var logEntry = new LogEntry { level = logLevel, message = message, Timestamp = DateTime.Now };
            logEntries.Add(logEntry);
            allLogEntries.Add(logEntry);
            if (logEntries.Count > MAX_LOG_ENTRIES)
                logEntries.RemoveAt(0);
            if (allLogEntries.Count > MAX_LOG_ENTRIES)
                allLogEntries.RemoveAt(0);
            FilterLogs(); // Always refresh UI when a new log is added
        }
        public void RegisterLogListener()
        {
            if (string.IsNullOrEmpty(logListenerIdentifier))
            {
                logListenerIdentifier = LootLockerLogger.RegisterListener(this);
                LootLockerLogger.RegisterHttpLogListener(this);
            }
        }
        public void UnregisterLogListener()
        {
            if (!string.IsNullOrEmpty(logListenerIdentifier))
            {
                LootLockerLogger.UnregisterListener(logListenerIdentifier);
                LootLockerLogger.UnregisterHttpLogListener(this);
                logListenerIdentifier = null;
            }
        }

        public void OnHttpLog(LootLockerLogger.LootLockerHttpLogEntry entry)
        {
            var httpEntry = new HttpLogEntry { http = entry, Timestamp = DateTime.Now };
            httpLogEntries.Add(entry);
            allLogEntries.Add(httpEntry);
            if (httpLogEntries.Count > MAX_LOG_ENTRIES)
                httpLogEntries.RemoveAt(0);
            if (allLogEntries.Count > MAX_LOG_ENTRIES)
                allLogEntries.RemoveAt(0);
            FilterLogs(); // Always refresh UI when a new HTTP log is added
        }
        private void ClearLogs()
        {
            logEntries.Clear();
            filteredLogEntries.Clear();
            httpLogEntries.Clear();
            allLogEntries.Clear();
            if (logContainer != null) logContainer.Clear();
            UpdateLogStatus();
        }
        private void ExportLogs()
        {
            try
            {
                string filename = $"LootLockerLogs_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                string filepath = EditorUtility.SaveFilePanel("Export Logs", "", filename, "log");
                if (string.IsNullOrEmpty(filepath)) return;
                using (var writer = new System.IO.StreamWriter(filepath))
                {
                    writer.WriteLine($"LootLocker Log Export");
                    writer.WriteLine($"Generated: {DateTime.Now}");
                    writer.WriteLine($"Total Entries: {filteredLogEntries.Count}");
                    writer.WriteLine(new string('=', 50));
                    writer.WriteLine();
                    foreach (var entry in filteredLogEntries.OrderBy(e => e.Timestamp))
                    {
                        if (entry is LogEntry logEntry)
                        {
                            writer.WriteLine($"[{logEntry.level}] {logEntry.message}");
                        }
                        else if (entry is HttpLogEntry httpEntry)
                        {
                            var http = httpEntry.http;
                            var sb = new System.Text.StringBuilder();
                            if (http.Response?.success ?? false)
                            {
                                sb.AppendLine($"[LL HTTP] {http.Method} request to {http.Url} succeeded");
                            }
                            else if (!string.IsNullOrEmpty(http.Response?.errorData?.message) && http.Response?.errorData?.message.Length < 40)
                            {
                                sb.AppendLine($"[LL HTTP] {http.Method} request to {http.Url} failed with message {http.Response.errorData.message} ({http.StatusCode})");
                            }
                            else
                            {
                                sb.AppendLine($"[LL HTTP] {http.Method} request to {http.Url} failed (details in expanded log) ({http.StatusCode})");
                            }
                            sb.AppendLine($"Duration: {http.DurationSeconds:n4}s");
                            sb.AppendLine("Request Headers:");
                            foreach (var h in http.RequestHeaders ?? new Dictionary<string, string>())
                                sb.AppendLine($"  {h.Key}: {h.Value}");
                            if (!string.IsNullOrEmpty(http.RequestBody))
                            {
                                sb.AppendLine("Request Body:");
                                sb.AppendLine(http.RequestBody);
                            }
                            sb.AppendLine("Response Headers:");
                            foreach (var h in http.ResponseHeaders ?? new Dictionary<string, string>())
                                sb.AppendLine($"  {h.Key}: {h.Value}");
                            if (!string.IsNullOrEmpty(http.Response?.text))
                            {
                                sb.AppendLine("Response Body:");
                                sb.AppendLine(http.Response.text);
                            }
                            writer.Write(sb.ToString());
                        }
                    }
                }
                EditorUtility.DisplayDialog("Export Complete", $"Logs exported to:\n{filepath}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export Error", $"Failed to export logs:\n{ex.Message}", "OK");
            }
        }
        private void FilterLogs()
        {
            filteredLogEntries.Clear();
            foreach (var entry in allLogEntries)
            {
                if (ShouldShowLogViewerEntry(entry))
                    filteredLogEntries.Add(entry);
            }
            UpdateLogDisplay();
        }

        private bool ShouldShowLogViewerEntry(ILogViewerEntry entry)
        {
            // Admin filter
            string adminUrl = null;
            if (LootLocker.LootLockerConfig.current != null)
                adminUrl = LootLocker.LootLockerConfig.current.adminUrl;
            adminUrl = adminUrl?.Substring(18) ?? "";

            if (entry is LogEntry logEntry)
            {
                if (!showAdminRequests && !string.IsNullOrEmpty(adminUrl) && logEntry.message.Contains(adminUrl))
                    return false;
                if (!showAllLogLevels && logLevelFilter != LootLockerLogger.LogLevel.Debug && logEntry.level < logLevelFilter)
                    return false;
                if (!string.IsNullOrEmpty(searchFilter) && logEntry.message.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
                return true;
            }
            else if (entry is HttpLogEntry httpEntry)
            {
                // Admin filter for HTTP logs (filter by URL)
                if (!showAdminRequests && !string.IsNullOrEmpty(adminUrl) && httpEntry.http.Url.Contains(adminUrl))
                    return false;
                LootLockerLogger.LogLevel httpLevel = httpEntry.http.Response?.success ?? false ? LootLockerLogger.LogLevel.Verbose : LootLockerLogger.LogLevel.Error;
                int code = httpEntry.http.StatusCode;
                if (code >= 400) httpLevel = LootLockerLogger.LogLevel.Error;
                else if (code >= 300) httpLevel = LootLockerLogger.LogLevel.Verbose;
                if (!showAllLogLevels && logLevelFilter != LootLockerLogger.LogLevel.Debug && httpLevel < logLevelFilter)
                    return false;
                // Search filter for HTTP logs (search in URL, request/response bodies)
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    if (!(httpEntry.http.Url.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                        || (httpEntry.http.RequestBody != null && httpEntry.http.RequestBody.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (httpEntry.http.Response?.text != null && httpEntry.http.Response.text.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)))
                        return false;
                }
                return true;
            }
            return false;
        }

        private void UpdateLogDisplay()
        {
            if (logContainer == null) return;
            logContainer.Clear();
            foreach (var entry in filteredLogEntries.OrderBy(e => e.Timestamp))
            {
                if (entry is HttpLogEntry http)
                    AddHttpLogEntryToUI(http.http);
                else if (entry is LogEntry log)
                    AddLogEntryToUI(log);
            }
            UpdateLogStatus();
            if (autoScroll) ScrollToBottom();
        }

        private void AddLogEntryToUI(LogEntry entry)
        {
            if (logContainer == null) return;
            var logElement = new VisualElement();
            logElement.AddToClassList("log-entry");
            var levelBadge = new Label(entry.level.ToString().ToUpper());
            levelBadge.AddToClassList("log-level-badge");
            levelBadge.AddToClassList($"log-{entry.level.ToString().ToLower()}");
            logElement.Add(levelBadge);
            var message = new TextField { value = entry.message, isReadOnly = true };
            message.AddToClassList("log-message-field");
            logElement.Add(message);
            logElement.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.AppendAction("Copy Message", a => GUIUtility.systemCopyBuffer = entry.message);
                evt.menu.AppendAction("Copy Full Entry", a => GUIUtility.systemCopyBuffer = $"[{entry.level}] {entry.message}");
            });
            logContainer.Add(logElement);
        }

        private void AddHttpLogEntryToUI(LootLockerLogger.LootLockerHttpLogEntry entry)
        {
            var summaryText = "";
            if (entry.Response?.success ?? false)
            {
                summaryText = $"{entry.Method} request to {entry.Url} completed successfully [{entry.StatusCode}] ({entry.DurationSeconds:n2}s)";
            }
            else
            {
                summaryText = $"{entry.Method} request to {entry.Url} failed [{entry.StatusCode}] ({entry.DurationSeconds:n2}s)";
            }

            var foldout = new Foldout { text = string.Empty, value = false };

            // Create a horizontal container for summary and icon
            var summaryRow = new VisualElement();
            summaryRow.style.flexDirection = FlexDirection.Row;
            summaryRow.style.justifyContent = Justify.SpaceBetween;
            summaryRow.style.alignItems = Align.Center;
            summaryRow.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

            var summaryLabel = new Label(summaryText);
            summaryLabel.AddToClassList("unity-foldout__text");
            summaryLabel.style.flexGrow = 1;

            summaryRow.Add(summaryLabel);

            // Add clickable question mark icon to summary row if doc_url exists
            if (entry.StatusCode >= 400 && !string.IsNullOrEmpty(entry.Response?.errorData?.doc_url))
            {
                var docUrl = entry.Response.errorData.doc_url;
                var icon = new Label("?") { tooltip = "More info" };
                icon.AddToClassList("log-doc-icon");
                icon.style.unityFontStyleAndWeight = FontStyle.Bold;
                icon.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL(docUrl));
                summaryRow.Add(icon);
            }

            // Insert the summaryRow into the Foldout's Toggle
            foldout.schedule.Execute(() =>
            {
                var toggle = foldout.Q<Toggle>(className: "unity-foldout__toggle");
                if (toggle != null)
                {
                    // Remove any existing label
                    var oldLabel = toggle.Q<Label>(className: "unity-foldout__text");
                    if (oldLabel != null)
                        toggle.Remove(oldLabel);
                    toggle.Add(summaryRow);
                }
            });

            foldout.schedule.Execute(() =>
            {
                var headerLabel = summaryLabel;
                if (headerLabel != null)
                {
                    headerLabel.RemoveFromClassList("log-error");
                    headerLabel.RemoveFromClassList("log-warning");
                    headerLabel.RemoveFromClassList("log-success");
                    headerLabel.RemoveFromClassList("log-info");
                    if (entry.Response?.success ?? false)
                    {
                        headerLabel.AddToClassList("log-success");
                    }
                    else
                    {
                        headerLabel.AddToClassList("log-error");
                    }
                }
            });

            var details = new VisualElement();
            var reqHeaders = new TextField { value = $"Request Headers: {FormatHeaders(entry.RequestHeaders)}", isReadOnly = true };
            reqHeaders.AddToClassList("log-message-field");
            details.Add(reqHeaders);
            if (!string.IsNullOrEmpty(entry.RequestBody))
            {
                var reqBody = new TextField { value = $"Request Body: {entry.RequestBody}", isReadOnly = true };
                reqBody.AddToClassList("log-message-field");
                details.Add(reqBody);
            }
            var respHeaders = new TextField { value = $"Response Headers: {FormatHeaders(entry.ResponseHeaders)}", isReadOnly = true };
            respHeaders.AddToClassList("log-message-field");
            details.Add(respHeaders);
            if (!string.IsNullOrEmpty(entry.Response?.text))
            {
                var respBody = new TextField { value = $"Response Body: {entry.Response.text}", isReadOnly = true };
                respBody.AddToClassList("log-message-field");
                details.Add(respBody);
            }
            foldout.Add(details);
            logContainer.Add(foldout);
        }

        private string FormatHeaders(Dictionary<string, string> headers)
        {
            if (headers == null) return "";
            return string.Join(", ", headers.Select(kv => $"{kv.Key}: {kv.Value}"));
        }

        private void ScrollToBottom()
        {
            if (logScrollView != null)
            {
                EditorApplication.delayCall += () =>
                {
                    logScrollView.scrollOffset = new Vector2(0, logScrollView.contentContainer.layout.height);
                };
            }
        }

        private void UpdateLogStatus()
        {
            if (logStatusLabel == null) return;
            string status = $"{filteredLogEntries.Count} total messages";
            bool isFiltering = !showAllLogLevels || !string.IsNullOrEmpty(searchFilter);
            if (isFiltering && filteredLogEntries.Count != allLogEntries.Count)
                status += $" | {filteredLogEntries.Count} filtered";
            int networkRequests = filteredLogEntries.OfType<LogEntry>().Count(e => e.networkType == NetworkRequestType.Request);
            int networkResponses = filteredLogEntries.OfType<LogEntry>().Count(e => e.networkType == NetworkRequestType.Response);
            int networkErrors = filteredLogEntries.OfType<LogEntry>().Count(e => e.networkType == NetworkRequestType.Error);
            status += $" | HTTP: {filteredLogEntries.OfType<HttpLogEntry>().Count()}";
            if (networkRequests > 0 || networkResponses > 0 || networkErrors > 0)
                status += $" | Network: {networkRequests} req, {networkResponses} resp, {networkErrors} errors";
            logStatusLabel.text = status;
        }

        public void Dispose()
        {
            UnregisterLogListener();
        }
    }
}
#endif
