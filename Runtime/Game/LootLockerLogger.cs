using System;
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
                    return Debug.Log;
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

        private static void SilentLogger(string ignored)
        {
            //Intentionally empty
        }
    }
}