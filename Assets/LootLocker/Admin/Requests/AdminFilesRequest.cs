using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerAdmin;
using LootLockerAdminRequests;
using Newtonsoft.Json;
using System;
using UnityEngine.Networking;
using System.IO;
using System.Net.Http;

namespace LootLockerAdminRequests
{

    public enum FileFilterType { none, asset, item, player }
    public class GetFilesResponse : LootLockerResponse
    {

        public bool success { get; set; }
        public File[] files;

    }

    public class UploadAFileRequest
    {
        public string asset_id;
    }
    public class UploadAFileResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string url { get; set; }
        public int id { get; set; }
        public int size { get; set; }
        public string name { get; set; }
        public string updated_at { get; set; }
    }

    public class DeleteFileResponse : LootLockerResponse
    {

        public bool success { get; set; }

    }

    public class UpdateFileRequest
    {
        public string[] tags;
    }
    public class UpdateFileResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public File file;
    }

    public class File
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

namespace LootLockerAdmin
{

    public partial class LootLockerAPIManagerAdmin
    {
        public static string UploadAFile(string filePath, string id, int gameId, Action<UploadAFileResponse> onComplete, string[] tags = null)
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

            ServerRequest.UploadFile(endPoint.endPoint, endPoint.httpMethod, System.IO.File.ReadAllBytes(filePath), defaultFileName,
                body: formData, onComplete: (serverResponse) =>
            {
                UploadAFileResponse response = new UploadAFileResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    Debug.Log("Response text: " + serverResponse.text);
                    response = JsonConvert.DeserializeObject<UploadAFileResponse>(serverResponse.text);
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
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);

            return eventId;
        }

        public static void GetFiles(FileFilterType filter, Action<GetFilesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPointsAdmin.current.getFiles;

            string getVariable = string.Format(endPoint.endPoint, BaseServerAPI.activeConfig.gameID);

            if (filter != FileFilterType.none) getVariable += "?filter=" + filter;

            ServerRequest.CallAPI(getVariable, endPoint.httpMethod, "", (serverResponse) =>
            {
                var response = new GetFilesResponse();

                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GetFilesResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);
        }

        public static void DeleteFile(string fileId, Action<DeleteFileResponse> onComplete)
        {
            var endPointInfo = LootLockerEndPointsAdmin.current.deleteFile;
            string getVariable = string.Format(endPointInfo.endPoint, BaseServerAPI.activeConfig.gameID, fileId);

            ServerRequest.CallAPI(getVariable, endPointInfo.httpMethod, "", (serverResponse) =>
            {
                var response = new DeleteFileResponse();

                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<DeleteFileResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);
        }

        public static void UpdateFile(string fileId, UpdateFileRequest request, Action<UpdateFileResponse> onComplete)
        {
            var json = JsonConvert.SerializeObject(request);
            var endPointInfo = LootLockerEndPointsAdmin.current.updateFile;
            string getVariable = string.Format(endPointInfo.endPoint, BaseServerAPI.activeConfig.gameID, fileId);

            ServerRequest.CallAPI(getVariable, endPointInfo.httpMethod, json, (serverResponse) =>
            {
                var response = new UpdateFileResponse();

                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<UpdateFileResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: LootLockerEnums.CallerRole.Admin);
        }


    }

}