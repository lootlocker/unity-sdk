using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;
using enums;

namespace enums
{
    public enum FilePurpose { primary_thumbnail, thumbnail, file }
}


namespace LootLockerRequests
{
    //public class FilePurpose
    //{
    //    public string purpose { get; private set; }

    //    private FilePurpose(string purpose)
    //    {
    //        this.purpose = purpose;
    //    }

    //    public static FilePurpose PRIMARY_THUMBNAIL = new FilePurpose("PRIMARY_THUMBNAIL");
    //    public static FilePurpose THUMBNAIL = new FilePurpose("THUMBNAIL");
    //    public static FilePurpose FILE = new FilePurpose("FILE");
    //}

    #region Asset Candidate Properties
    public class AssetKVPair
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class DataEntity
    {
        public string name { get; set; }
        public string data { get; set; }
    }

    public class Asset_Candidates
    {
        public int? id { get; set; }
        public int? asset_id { get; set; }
        public string status { get; set; }
        public string review_feedback { get; set; }
        public AssetData data { get; set; }
        public AssetFile[] files { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
    }

    public class AssetData
    {
        public int context_id { get; set; } = -1;
        public string name { get; set; }
        public AssetKVPair[] kv_storage { get; set; }
        public AssetKVPair[] filters { get; set; }
        public DataEntity[] data_entities { get; set; }

        public bool ShouldSerializecontext_id()
        {
            // don't serialize the context_id property if it is not set.
            return (context_id >= 0);
        }
        public bool ShouldSerializename()
        {
            // don't serialize the name property if it is not set.
            return (name != null && name.Length > 0);
        }
        public bool ShouldSerializekv_storage()
        {
            // don't serialize the kv_storage property if it is not set.
            return (kv_storage != null && kv_storage.Length > 0);
        }
        public bool ShouldSerializefilters()
        {
            // don't serialize the filters property if it is not set.
            return (filters != null && filters.Length > 0);
        }
        public bool ShouldSerializedata_entities()
        {
            // don't serialize the data_entities property if it is not set.
            return (data_entities != null && data_entities.Length > 0);
        }

    }

    public class AssetFile
    {
        public int id { get; set; }
        public string url { get; set; }
        public int size { get; set; }
        public string name { get; set; }
        public string purpose { get; set; }
    }
    #endregion

    public class CreatingOrUpdatingAnAssetCandidateRequest
    {
        public AssetData data { get; set; }
        public bool completed { get; set; } = false;
        public bool ShouldSerializecompleted()
        {
            // don't serialize the completed property if it is not set.
            return completed;
        }
    }

    public class AddingFilesToAssetCandidatesRequest
    {
        public string filePath { get; set; }
        public string fileName { get; set; }
        public string fileContentType { get; set; }
        public string filePurpose { get; set; }
    }

    public class UserGenerateContentResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public int asset_candidate_id { get; set; }
    }

    public class ListingAssetCandidatesResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Asset_Candidates[] asset_candidates { get; set; }
    }
}


namespace LootLocker
{

    public partial class LootLockerAPIManager
    {
        public static void CreatingAnAssetCandidate(CreatingOrUpdatingAnAssetCandidateRequest data, Action<UserGenerateContentResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.creatingAnAssetCandidate;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                UserGenerateContentResponse response = new UserGenerateContentResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<UserGenerateContentResponse>(serverResponse.text);
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, false, enums.CallerRole.User);
        }

        public static void UpdatingAnAssetCandidate(CreatingOrUpdatingAnAssetCandidateRequest data, LootLockerGetRequest getRequests, Action<UserGenerateContentResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.current.updatingAnAssetCandidate;
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.getRequests[0]);

            ServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, (serverResponse) =>
            {
                UserGenerateContentResponse response = new UserGenerateContentResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<UserGenerateContentResponse>(serverResponse.text);
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, false, enums.CallerRole.User);
        }

        public static void DeletingAnAssetCandidate(LootLockerGetRequest data, Action<UserGenerateContentResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.current.deletingAnAssetCandidate;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            ServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) =>
            {
                UserGenerateContentResponse response = new UserGenerateContentResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<UserGenerateContentResponse>(serverResponse.text);
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: false, callerRole: enums.CallerRole.User);
        }

        public static void ListingAssetCandidates(Action<ListingAssetCandidatesResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.current.listingAssetCandidates;

            ServerRequest.CallAPI(requestEndPoint.endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) =>
            {
                ListingAssetCandidatesResponse response = new ListingAssetCandidatesResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<ListingAssetCandidatesResponse>(serverResponse.text);
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: false, callerRole: enums.CallerRole.User);
        }

        public static void AddingFilesToAssetCandidates(AddingFilesToAssetCandidatesRequest data, LootLockerGetRequest getRequests, Action<UserGenerateContentResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.current.addingFilesToAssetCandidates;

            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.getRequests[0]);

            Dictionary<string, string> formData = new Dictionary<string, string>();

            formData.Add("purpose", data.filePurpose.ToString());

            if (string.IsNullOrEmpty(data.fileName))
            {
                string[] splitFilePath = data.filePath.Split(new char[] { '\\', '/' });
                string defaultFileName = splitFilePath[splitFilePath.Length - 1];
                data.fileName = defaultFileName;
            }

            if (string.IsNullOrEmpty(data.fileContentType))
            {
                data.fileContentType = "multipart/form-data";
            }
            byte[] fileData = System.IO.File.ReadAllBytes(data.filePath);

            ServerRequest.UploadFile(endPoint, requestEndPoint.httpMethod, fileData, data.fileName, data.fileContentType,
                formData, (serverResponse) =>
            {
                UserGenerateContentResponse response = new UserGenerateContentResponse();

                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<UserGenerateContentResponse>(serverResponse.text);
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: true, callerRole: enums.CallerRole.User);
        }

        public static void RemovingFilesFromAssetCandidates(LootLockerGetRequest data, Action<UserGenerateContentResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.current.removingFilesFromAssetCandidates;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            ServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) =>
            {
                UserGenerateContentResponse response = new UserGenerateContentResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    LootLockerSDKManager.DebugMessage(serverResponse.text);
                    response = JsonConvert.DeserializeObject<UserGenerateContentResponse>(serverResponse.text);
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, useAuthToken: false, callerRole: enums.CallerRole.User);
        }
    }
}
