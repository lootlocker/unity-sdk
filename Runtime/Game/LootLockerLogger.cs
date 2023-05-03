using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    public class LootLockerLogger
    {
        public enum LogLevel
        {
            Verbose
            , Info
            , Warning
            , Error
        }

        /// <summary>
        /// Get logger for the specified level. Use like: GetForLevel(LogLevel::Info)(message);
        /// </summary>
        /// <param name="logLevel">What level should this be logged as</param>
        public static Action<string> GetForLogLevel(LogLevel logLevel = LogLevel.Info)
        {
            if (!ShouldLog(logLevel))
            {
                return ignored => { };
            }

            AdjustLogLevelToSettings(ref logLevel);

            switch (logLevel)
            {
                case LogLevel.Error:
                    return Debug.LogError;
                case LogLevel.Warning:
                    return Debug.LogWarning;
                case LogLevel.Verbose:
                case LogLevel.Info:
                default:
                    return Debug.Log;
            }
        }

        private static bool ShouldLog(LogLevel logLevel)
        {
#if UNITY_EDITOR
            switch (logLevel)
            {
                case LogLevel.Error:
                {
                    if (LootLockerConfig.current == null ||
                        (new List<LootLockerConfig.DebugLevel>
                        {
                            LootLockerConfig.DebugLevel.All, 
                            LootLockerConfig.DebugLevel.AllAsNormal,
                            LootLockerConfig.DebugLevel.ErrorOnly
                        }).Contains(LootLockerConfig.current.currentDebugLevel))
                    {
                        return true;
                    }

                    break;
                }
                case LogLevel.Warning:
                {
                    if (LootLockerConfig.current == null ||
                        (new List<LootLockerConfig.DebugLevel>
                        {
                            LootLockerConfig.DebugLevel.All,
                            LootLockerConfig.DebugLevel.AllAsNormal
                        })
                        .Contains(LootLockerConfig.current.currentDebugLevel))
                    {
                        return true;
                    }

                    break;
                }
                case LogLevel.Verbose:
                {
                    if (LootLockerConfig.current == null ||
                        (new List<LootLockerConfig.DebugLevel>
                        {
                            LootLockerConfig.DebugLevel.All, 
                            LootLockerConfig.DebugLevel.AllAsNormal
                        })
                        .Contains(LootLockerConfig.current.currentDebugLevel))
                    {
                        return true;
                    }

                    break;
                }
                case LogLevel.Info:
                default:
                {
                    if (LootLockerConfig.current == null ||
                        (new List<LootLockerConfig.DebugLevel>
                        {
                            LootLockerConfig.DebugLevel.All, 
                            LootLockerConfig.DebugLevel.AllAsNormal,
                            LootLockerConfig.DebugLevel.NormalOnly
                        }).Contains(LootLockerConfig.current.currentDebugLevel))
                    {
                        return true;
                    }

                    break;
                }
            }
#endif

            return false;
        }

        private static void AdjustLogLevelToSettings(ref LogLevel logLevel)
        {
            if (LootLockerConfig.current != null && LootLockerConfig.DebugLevel.AllAsNormal == LootLockerConfig.current.currentDebugLevel)
            {
                logLevel = LogLevel.Info;
            }
        }
    }
}