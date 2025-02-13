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
            listener.Log(LogLevel.Verbose, "LootLocker debugger prefab is awake and listening");
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
            listener.Log(LogLevel.Info, $"--- Replaying latest {logRecords.Length} messages from before listener connected");
            int actuallyReplayedMessages = 0;
            for(int i = nextLogRecordWrite; i < logRecords.Length; i++)
            {
                if(logRecords[i] == null || string.IsNullOrEmpty(logRecords[i].message))
                {
                    continue;
                }
                listener.Log(logRecords[i].logLevel, logRecords[i].message);
                actuallyReplayedMessages++;
            }
            for (int i = 0; i < nextLogRecordWrite; i++)
            {
                if (logRecords[i] == null || string.IsNullOrEmpty(logRecords[i].message))
                {
                    continue;
                }
                listener.Log(logRecords[i].logLevel, logRecords[i].message);
                actuallyReplayedMessages++;
            }
            listener.Log(LogLevel.Info, $"--- Replayed {actuallyReplayedMessages} messages from before listener connected");
        }
    }

    public interface LootLockerLogListener
    {
        void Log(LootLockerLogger.LogLevel logLevel, string message);
    }
}