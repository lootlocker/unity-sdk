using System;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    public class LootLockerLogger
    {
        public static LootLockerLogger _instance = null;

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

            Action<string> logger = null;
            switch (logLevel)
            {
                case LogLevel.None:
                    return;
                case LogLevel.Error:
                    logger = LootLockerConfig.current.logErrorsAsWarnings ? Debug.LogWarning : Debug.LogError;
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

            if (_instance != null && _instance.logListeners.Count > 0)
            {
                foreach(var listener in _instance.logListeners.Values)
                {
                    if(listener != null)
                    {
                        listener.Log(logLevel, message);
                    }
                }
            }
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

        public Dictionary<string, LootLockerLogListener> logListeners = new Dictionary<string, LootLockerLogListener>();

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
    }

    public interface LootLockerLogListener
    {
        public void Log(LootLockerLogger.LogLevel logLevel, string message);
    }
}