using System;
using System.Collections.Generic;
using LootLocker.Requests;

namespace LootLocker.LootLockerEnums
{
    public enum FilePurpose
    {
        primary_thumbnail,
        thumbnail,
        file
    }
}


namespace LootLocker.Requests
{
    #region Asset Candidate Properties

    public class LootLockerAssetKVPair
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class LootLockerDataEntity
    {
        public string name { get; set; }
        public string data { get; set; }
    }

    public class LootLockerAsset_Candidates
    {
        public int? id { get; set; }
        public int? asset_id { get; set; }
        public string status { get; set; }
        public string review_feedback { get; set; }
        public LootLockerAssetData data { get; set; }
        public LootLockerAssetFile[] files { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
    }

    public class LootLockerAssetData
    {
        public int context_id { get; set; } = -1;
        public string name { get; set; }
        public LootLockerAssetKVPair[] kv_storage { get; set; }
        public LootLockerAssetKVPair[] filters { get; set; }
        public LootLockerDataEntity[] data_entities { get; set; }

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

    public class LootLockerAssetFile
    {
        public int id { get; set; }
        public string url { get; set; }
        public int size { get; set; }
        public string name { get; set; }
        public string purpose { get; set; }
    }

    #endregion

    public class LootLockerCreatingOrUpdatingAnAssetCandidateRequest
    {
        public LootLockerAssetData data { get; set; }
        public bool completed { get; set; } = false;

        public bool ShouldSerializecompleted()
        {
            // don't serialize the completed property if it is not set.
            return completed;
        }
    }

    public class LootLockerAddingFilesToAssetCandidatesRequest
    {
        public string filePath { get; set; }
        public string fileName { get; set; }
        public string fileContentType { get; set; }
        public string filePurpose { get; set; }
    }

    public class LootLockerUserGenerateContentResponse : LootLockerResponse
    {
        public int asset_candidate_id { get; set; }
        public LootLockerAsset_Candidates asset_candidate { get; set; }
    }

    public class LootLockerListingAssetCandidatesResponse : LootLockerResponse
    {
        public LootLockerAsset_Candidates[] asset_candidates { get; set; }
    }
}


namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void CreatingAnAssetCandidate(LootLockerCreatingOrUpdatingAnAssetCandidateRequest data, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerUserGenerateContentResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.creatingAnAssetCandidate;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdatingAnAssetCandidate(LootLockerCreatingOrUpdatingAnAssetCandidateRequest data, LootLockerGetRequest getRequests, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.updatingAnAssetCandidate;
            if(data == null)
            {
            	onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerUserGenerateContentResponse>());
            	return;
            }

            string json = LootLockerJson.SerializeObject(data);

            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GettingASingleAssetCandidate(LootLockerGetRequest getRequests, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.gettingASingleAssetCandidate;

            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void DeletingAnAssetCandidate(LootLockerGetRequest data, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.deletingAnAssetCandidate;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListingAssetCandidates(Action<LootLockerListingAssetCandidatesResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.listingAssetCandidates;

            LootLockerServerRequest.CallAPI(requestEndPoint.endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void AddingFilesToAssetCandidates(LootLockerAddingFilesToAssetCandidatesRequest data, LootLockerGetRequest getRequests, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.addingFilesToAssetCandidates;

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

            LootLockerServerRequest.UploadFile(endPoint, requestEndPoint.httpMethod, fileData, data.fileName, data.fileContentType, formData, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void RemovingFilesFromAssetCandidates(LootLockerGetRequest data, Action<LootLockerUserGenerateContentResponse> onComplete)
        {
            EndPointClass requestEndPoint = LootLockerEndPoints.removingFilesFromAssetCandidates;

            string endPoint = string.Format(requestEndPoint.endPoint, data.getRequests[0], data.getRequests[1]);

            LootLockerServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}