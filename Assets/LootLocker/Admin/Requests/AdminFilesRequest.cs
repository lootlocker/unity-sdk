using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Admin;
using LootLocker.Admin.Requests;
using Newtonsoft.Json;
using System;
using UnityEngine.Networking;
using System.IO;
using System.Net.Http;

namespace LootLocker.Admin.Requests
{

    public enum LootLockerFileFilterType { none, asset, item, player }
    public class LootLockerGetFilesResponse : LootLockerResponse
    {

        public bool success { get; set; }
        public LootLockerFile[] files;

    }

    public class LootLockerUploadAFileRequest
    {
        public string asset_id;
    }
    public class LootLockerUploadAFileResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string url { get; set; }
        public int id { get; set; }
        public int size { get; set; }
        public string name { get; set; }
        public string updated_at { get; set; }
    }

    public class LootLockerDeleteFileResponse : LootLockerResponse
    {

        public bool success { get; set; }

    }

    public class LootLockerUpdateFileRequest
    {
        public string[] tags;
    }
    public class LootLockerUpdateFileResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerFile file;
    }

    public class LootLockerFile
    {
        public string url { get; set; }
        public int id { get; set; }
        public string attachment_type { get; set; }
        public int attachment_id { get; set; }
        public int size { get; set; }
        public string name { get; set; }
        public string[] tags { get; set; }
        public string updated_at { get; set; }
    }

}

namespace LootLocker.Admin
{

    public partial class LootLockerAPIManagerAdmin
    {
        public static string UploadAFile(string filePath, string id, int gameId, Action<LootLockerUploadAFileResponse> onComplete, string[] tags = null)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.uploadFile;

            Dictionary<string, string> formData = new Dictionary<string, string>();

            formData.Add("asset_id", id);
            formData.Add("game_id", gameId.ToString());
            if (tags != null && tags.Length > 0)
            {
                var tagsFormted = string.Empty;
                foreach (var tag in tags) tagsFormted += tag + ';';
                formData.Add("tags", tagsFormted);
            }

            var eventId = Guid.NewGuid().ToString();

            string[] splitFilePath = filePath.Split(new char[] { '\\', '/' });
            string defaultFileName = splitFilePath[splitFilePath.Length - 1];

            LootLockerServerRequest.UploadFile(endPoint.endPoint, endPoint.httpMethod, System.IO.File.ReadAllBytes(filePath), defaultFileName,
                body: formData, onComplete: (serverResponse) =>
            {
                LootLockerUploadAFileResponse response = new LootLockerUploadAFileResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    Debug.Log("Response text: " + serverResponse.text);
                    response = JsonConvert.DeserializeObject<LootLockerUploadAFileResponse>(serverResponse.text);
                    response.EventId = eventId;
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);

            return eventId;
        }

        public static void GetFiles(LootLockerFileFilterType filter, Action<LootLockerGetFilesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getFiles;

            string getVariable = string.Format(endPoint.endPoint, LootLockerBaseServerAPI.activeConfig.gameID);

            if (filter != LootLockerFileFilterType.none) getVariable += "?filter=" + filter;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                var response = new LootLockerGetFilesResponse();

                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerGetFilesResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }

        public static void DeleteFile(string fileId, Action<LootLockerDeleteFileResponse> onComplete)
        {
            var endPointInfo = LootLockerEndPointsAdmin.current.deleteFile;
            string getVariable = string.Format(endPointInfo.endPoint, LootLockerBaseServerAPI.activeConfig.gameID, fileId);

            LootLockerServerRequest.CallAPI(getVariable, endPointInfo.httpMethod, "", (serverResponse) =>
            {
                var response = new LootLockerDeleteFileResponse();

                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerDeleteFileResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }

        public static void UpdateFile(string fileId, LootLockerUpdateFileRequest request, Action<LootLockerUpdateFileResponse> onComplete)
        {
            var json = JsonConvert.SerializeObject(request);
            var endPointInfo = LootLockerEndPointsAdmin.current.updateFile;
            string getVariable = string.Format(endPointInfo.endPoint, LootLockerBaseServerAPI.activeConfig.gameID, fileId);

            LootLockerServerRequest.CallAPI(getVariable, endPointInfo.httpMethod, json, (serverResponse) =>
            {
                var response = new LootLockerUpdateFileResponse();

                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerUpdateFileResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLocker.LootLockerEnums.LootLockerCallerRole.Admin);
        }


    }

}