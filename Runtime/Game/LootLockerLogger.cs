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
        /// LootLocker Log messages, only visible in the Unity Editor.
        /// </summary>
        /// <param name="message">A message as a string</param>
        /// <param name="logLevel">What level should this be logged as</param>
        public static void EditorMessage(string message, LogLevel logLevel = LogLevel.Info)
        {
#if UNITY_EDITOR
            if (!ShouldLog(logLevel))
            {
                return;
            }

            AdjustLogLevelToSettings(ref logLevel);

            switch (logLevel)
            {
                case LogLevel.Error:
                    Debug.LogError(message);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Verbose:
                case LogLevel.Info:
                default:
                    Debug.Log(message);
                    break;
            }
#endif
        }

        private static bool ShouldLog(LogLevel logLevel)
        {
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

            return false;
        }

        private static void AdjustLogLevelToSettings(ref LogLevel logLevel)
        {
            if (LootLockerConfig.current != null &&
                LootLockerConfig.DebugLevel.AllAsNormal == LootLockerConfig.current.currentDebugLevel)
            {
                logLevel = LogLevel.Info;
            }
        }
    }
}