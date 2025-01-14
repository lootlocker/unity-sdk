using System;
using UnityEngine;

namespace LootLocker
{
    public class LootLockerLogger
    {
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
        /// Get logger for the specified level. Use like: GetForLevel(LogLevel::Info)(message);
        /// </summary>
        /// <param name="logLevel">What level should this be logged as</param>
        public static Action<string> GetForLogLevel(LogLevel logLevel = LogLevel.Info)
        {
            if (!ShouldLog(logLevel))
            {
                return SilentLogger;
            }

            switch (logLevel)
            {
                case LogLevel.None:
                    return SilentLogger;
                case LogLevel.Error:
                    return LootLockerConfig.current.logErrorsAsWarnings ? Debug.LogWarning : Debug.LogError;
                case LogLevel.Warning:
                    return Debug.LogWarning;
                case LogLevel.Verbose:
                case LogLevel.Info:
                case LogLevel.Debug:
                default:
                    return Debug.Log;
            }
        }

        private static bool ShouldLog(LogLevel logLevel)
        {
#if UNITY_EDITOR
            if(LootLockerConfig.current == null || LootLockerConfig.current.logLevel <= logLevel)
            {
                return true;
            }
#endif
            return false;
        }

        private static void SilentLogger(string ignored)
        {
            //Intentionally empty
        }
    }
}