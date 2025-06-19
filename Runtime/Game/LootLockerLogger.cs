using System;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    public class LootLockerLogger
    {
        public static LootLockerLogger _instance = null;

        private Dictionary<string, LootLockerLogListener> logListeners = new Dictionary<string, LootLockerLogListener>();
        private class LogRecord
        {
            public string message { get; set; }
            public LogLevel logLevel { get; set; }

            public LogRecord(string msg, LogLevel lvl) { message = msg; logLevel = lvl; }
        }
        private LogRecord[] logRecords = new LogRecord[100];
        private int nextLogRecordWrite = 0;

        private const int HttpLogBacklogSize = 100;
        private LootLockerHttpLogEntry[] httpLogRecords = new LootLockerHttpLogEntry[HttpLogBacklogSize];
        private int nextHttpLogRecordWrite = 0;

        public enum LogLevel
        {
            Debug
            , Verbose
            , Info
            , Warning
            , Error
            , None
        }

        /// <summary>
        /// Log message with the specified loglevel
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="logLevel">What level should this be logged as</param>
        public static void Log(string message, LogLevel logLevel = LogLevel.Info)
        {
            if (!ShouldLog(logLevel))
            {
                return;
            }

            Action<string> logger;
            switch (logLevel)
            {
                case LogLevel.None:
                    return;
                case LogLevel.Error:
                    if(LootLockerConfig.current.logErrorsAsWarnings)
                    {
                        logger = Debug.LogWarning;
                    }
                    else
                    {
                        logger = Debug.LogError;
                    }
                    break;
                case LogLevel.Warning:
                    logger = Debug.LogWarning;
                    break;
                case LogLevel.Verbose:
                case LogLevel.Info:
                case LogLevel.Debug:
                default:
                    logger = Debug.Log;
                    break;
            }

            if(logger != null)
            {
                logger(message);
            }

            if(_instance == null)
            {
                _instance = new LootLockerLogger();
            }
            _instance.RecordAndBroadcastMessage(message, logLevel);
        }

        private static bool ShouldLog(LogLevel logLevel)
        {
            if(logLevel == LogLevel.None)
            {
                return false;
            }
#if !UNITY_EDITOR
            if(!LootLockerConfig.current.logInBuilds)
            {
                return false;
            }
#endif
            return LootLockerConfig.current == null || LootLockerConfig.current.logLevel <= logLevel;
        }

        public static string RegisterListener(LootLockerLogListener listener)
        {
            if(listener == null)
            {
                return "";
            }

            if (_instance == null)
            {
                _instance = new LootLockerLogger();
            }
            string identifier = Guid.NewGuid().ToString();
            _instance.logListeners.Add(identifier, listener);
            if(!string.IsNullOrEmpty(LootLockerConfig.current.sdk_version) && LootLockerConfig.current.sdk_version != "N/A")
            {
                listener.Log(LootLockerLogger.LogLevel.Verbose, $"LootLocker Version v{LootLockerConfig.current.sdk_version}");
            }

            _instance.ReplayLogRecord(listener);

            return identifier;
        }

        public static bool UnregisterListener(string identifier)
        {
            if (_instance == null)
            {
                _instance = new LootLockerLogger();
            }
            bool bRemovedListener = _instance.logListeners.Remove(identifier);
            if(_instance.logListeners.Count == 0)
            {
                _instance = null;
            }
            return bRemovedListener;
        }

        private void RecordAndBroadcastMessage(string message, LogLevel logLevel)
        {
            logRecords[nextLogRecordWrite] = new LogRecord(message, logLevel);
            nextLogRecordWrite = (nextLogRecordWrite + 1) % logRecords.Length;

            if (logListeners.Count > 0)
            {
                foreach (var listener in logListeners.Values)
                {
                    if (listener != null)
                    {
                        listener.Log(logLevel, message);
                    }
                }
            }
        }

        private void ReplayLogRecord(LootLockerLogListener listener)
        {
            // Build the backlog in order from oldest to newest
            List<LogRecord> backlog = new List<LogRecord>();
            for (int i = nextLogRecordWrite; i < logRecords.Length; i++)
            {
                if (logRecords[i] != null && !string.IsNullOrEmpty(logRecords[i].message))
                {
                    backlog.Add(logRecords[i]);
                }
            }
            for (int i = 0; i < nextLogRecordWrite; i++)
            {
                if (logRecords[i] != null && !string.IsNullOrEmpty(logRecords[i].message))
                {
                    backlog.Add(logRecords[i]);
                }
            }

            if (backlog.Count > 0)
            {
                listener.Log(LogLevel.Verbose, $"--- Replaying latest {backlog.Count} messages from before listener connected");
                foreach (var record in backlog)
                {
                    listener.Log(record.logLevel, record.message);
                }
            }
            else
            {
                listener.Log(LogLevel.Verbose, "--- No messages to replay from before listener connected");
            }
        }

        public class LootLockerHttpLogEntry
        {
            public string Method;
            public string Url;
            public Dictionary<string, string> RequestHeaders;
            public string RequestBody;
            public int StatusCode;
            public Dictionary<string, string> ResponseHeaders;
            public LootLockerResponse Response;
            public float DurationSeconds;
            public DateTime Timestamp;
        }

        public interface ILootLockerHttpLogListener
        {
            void OnHttpLog(LootLockerHttpLogEntry entry);
        }

        public static void LogHttpRequestResponse(LootLockerHttpLogEntry entry)
        {
            if (_instance == null)
            {
                _instance = new LootLockerLogger();
            }
            // Store in HTTP log backlog
            _instance.httpLogRecords[_instance.nextHttpLogRecordWrite] = entry;
            _instance.nextHttpLogRecordWrite = (_instance.nextHttpLogRecordWrite + 1) % HttpLogBacklogSize;

            // Construct log string for Unity log
            var sb = new System.Text.StringBuilder();
            if (entry.Response?.success ?? false)
            {
                sb.AppendLine($"[LL HTTP] {entry.Method} request to {entry.Url} succeeded");
            }
            else if (!string.IsNullOrEmpty(entry.Response?.errorData?.message) && entry.Response?.errorData?.message.Length < 40)
            {
                sb.AppendLine($"[LL HTTP] {entry.Method} request to {entry.Url} failed with message {entry.Response.errorData.message} ({entry.StatusCode})");
            }
            else
            {
                sb.AppendLine($"[LL HTTP] {entry.Method} request to {entry.Url} failed (details in expanded log) ({entry.StatusCode})");
            }
            sb.AppendLine($"Duration: {entry.DurationSeconds:n4}s");
            sb.AppendLine("Request Headers:");
            foreach (var h in entry.RequestHeaders ?? new Dictionary<string, string>())
                sb.AppendLine($"  {h.Key}: {h.Value}");
            if (!string.IsNullOrEmpty(entry.RequestBody))
            {
                sb.AppendLine("Request Body:");
                sb.AppendLine(entry.RequestBody);
            }
            sb.AppendLine("Response Headers:");
            foreach (var h in entry.ResponseHeaders ?? new Dictionary<string, string>())
                sb.AppendLine($"  {h.Key}: {h.Value}");
            if (!string.IsNullOrEmpty(entry.Response?.text))
            {
                sb.AppendLine("Response Body:");
                sb.AppendLine(entry.Response.text);
            }

            LogLevel level = entry.Response?.success ?? false ? LogLevel.Verbose : LogLevel.Error;
            Log(sb.ToString(), level);

            // Notify HTTP listeners
            if (_instance != null && _instance.httpLogListeners.Count > 0)
            {
                foreach (var listener in _instance.httpLogListeners)
                    listener.OnHttpLog(entry);
            }
        }

        private List<ILootLockerHttpLogListener> httpLogListeners = new List<ILootLockerHttpLogListener>();
        public static void RegisterHttpLogListener(ILootLockerHttpLogListener listener)
        {
            if (_instance == null) _instance = new LootLockerLogger();
            if (!_instance.httpLogListeners.Contains(listener))
                _instance.httpLogListeners.Add(listener);
            // Replay HTTP log backlog
            _instance.ReplayHttpLogRecord(listener);
        }
        public static void UnregisterHttpLogListener(ILootLockerHttpLogListener listener)
        {
            if (_instance == null) return;
            _instance.httpLogListeners.Remove(listener);
        }
        private void ReplayHttpLogRecord(ILootLockerHttpLogListener listener)
        {
            List<LootLockerHttpLogEntry> backlog = new List<LootLockerHttpLogEntry>();
            for (int i = nextHttpLogRecordWrite; i < httpLogRecords.Length; i++)
            {
                if (httpLogRecords[i] != null)
                    backlog.Add(httpLogRecords[i]);
            }
            for (int i = 0; i < nextHttpLogRecordWrite; i++)
            {
                if (httpLogRecords[i] != null)
                    backlog.Add(httpLogRecords[i]);
            }
            if (backlog.Count > 0)
            {
                foreach (var record in backlog)
                {
                    listener.OnHttpLog(record);
                }
            }
        }
    }

    public interface LootLockerLogListener
    {
        void Log(LootLockerLogger.LogLevel logLevel, string message);
    }
}