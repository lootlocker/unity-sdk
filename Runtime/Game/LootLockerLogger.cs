using UnityEngine;

namespace LootLocker
{
    public class LootLockerLogger
    {
        /// <summary>
        /// LootLocker Debug-messages, only visible in the Unity Editor.
        /// </summary>
        /// <param name="message">A message as a string</param>
        /// <param name="IsError">Is this an error or not?</param>
        public static void EditorMessage(string message, bool IsError = false)
        {
#if UNITY_EDITOR
            if (LootLockerConfig.current == null)
            {
                if (IsError)
                    Debug.LogError(message);
                else
                    Debug.Log(message);
                return;
            }

            if (LootLockerConfig.current != null &&
                LootLockerConfig.current.currentDebugLevel == LootLockerConfig.DebugLevel.All)
            {
                if (IsError)
                    Debug.LogError(message);
                else
                    Debug.Log(message);
            }
            else if (LootLockerConfig.current.currentDebugLevel == LootLockerConfig.DebugLevel.ErrorOnly)
            {
                if (IsError)
                    Debug.LogError(message);
            }
            else if (LootLockerConfig.current.currentDebugLevel == LootLockerConfig.DebugLevel.NormalOnly)
            {
                if (!IsError)
                    Debug.LogError(message);
            }
            else if (LootLockerConfig.current.currentDebugLevel == LootLockerConfig.DebugLevel.AllAsNormal)
            {
                Debug.Log(message);
            }
#endif
        }
    }
}