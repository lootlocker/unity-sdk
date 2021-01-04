using LootLockerRequests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLockerExample
{
    public class CrashesTest : MonoBehaviour
    {
        public string logFilePath;
        public string game_version;
        public string type_identifier;
        public string local_crash_time;

        [ContextMenu("SubmittingACrashLog")]
        public void SubmittingACrashLog()
        {

            LootLockerSDKManager.SubmittingACrashLog(logFilePath, game_version, type_identifier, local_crash_time, (response) =>
            {
                if (!response.hasError)
                {
                    LootLockerSDKManager.DebugMessage("Successful");
                }
                else
                {
                    LootLockerSDKManager.DebugMessage("failed: " + response.Error, true);
                }
            });
        }
    }
}