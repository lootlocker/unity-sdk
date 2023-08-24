using System;
using System.Collections.Generic;
using LootLocker.Requests;


namespace LootLocker.Requests
{
    public class LootLockerSubmittingACrashLogRequest
    {
        public string logFileName { get; set; }
        public string logFilePath { get; set; }
        public string game_version { get; set; }
        public string type_identifier { get; set; }
        public string local_crash_time { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void SubmittingACrashLog(LootLockerSubmittingACrashLogRequest data, Action<LootLockerResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.submittingACrashLog;

            Dictionary<string, string> formData = new Dictionary<string, string>();

            if(!string.IsNullOrEmpty(data.game_version)) formData.Add("game_version", data.game_version);
            if(!string.IsNullOrEmpty(data.type_identifier)) formData.Add("type_identifier", data.type_identifier);
            if(!string.IsNullOrEmpty(data.local_crash_time)) formData.Add("local_crash_time", data.local_crash_time);

            if (string.IsNullOrEmpty(data.logFileName))
            {
                string[] splitFilePath = data.logFilePath.Split(new char[] { '\\', '/' });
                string defaultFileName = splitFilePath[splitFilePath.Length - 1];
                data.logFileName = defaultFileName;
            }

            LootLockerServerRequest.UploadFile(requestEndPoint.endPoint, requestEndPoint.httpMethod, System.IO.File.ReadAllBytes(data.logFilePath), 
                data.logFileName, "application/zip", formData, onComplete: onComplete, useAuthToken: false);
        }
    }
}
