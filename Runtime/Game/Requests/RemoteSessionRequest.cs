using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// The status of the remote session leasing process
    /// </summary>
    public enum LootLockerRemoteSessionLeaseStatus
    {
        Created = 0,
        Claimed = 1,
        Verified = 2,
        Authorized = 3,
        Cancelled = 4,
        Timed_out = 5,
        Failed = 6
    };
}

namespace LootLocker.Requests
{

    //==================================================
    // Request Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerLeaseRemoteSessionRequest
    {
        /// <summary>
        /// The Game Key configured for the game
        /// </summary>
        public string game_key { get; set; } = LootLockerConfig.current.apiKey;
        /// <summary>
        /// The Game Version configured for the game
        /// </summary>
        public string game_version { get; set; } = LootLockerConfig.current.game_version;
    }

    /// <summary>
    /// </summary>
    public class LootLockerStartRemoteSessionRequest
    {
        /// <summary>
        /// The Game Key configured for the game
        /// </summary>
        public string game_key { get; set; } = LootLockerConfig.current.apiKey;
        /// <summary>
        /// The Game Version configured for the game
        /// </summary>
        public string game_version { get; set; } = LootLockerConfig.current.game_version;
        /// <summary>
        /// The lease code returned with the response when starting a lease process
        /// </summary>
        public string lease_code { get; set; }
        /// <summary>
        /// The nonce returned with the response when starting a lease process
        /// </summary>
        public string nonce { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// </summary>
    public class LootLockerLeaseRemoteSessionResponse : LootLockerResponse
    {
        /// <summary>
        /// The unique code for this leasing process, this is what identifies the leasing process and that is used to interact with it
        /// </summary>
        public string code { get; set; }
        /// <summary>
        /// The nonce used to sign usage of the lease code
        /// </summary>
        public string nonce { get; set; }
        /// <summary>
        /// A url with the code and nonce baked in that can be used to immediately start the remote authentication process on the device that uses it
        /// </summary>
        public string redirect_url { get; set; }
        /// <summary>
        /// A QR code representation of the redirect_url encoded in Base64
        /// </summary>
        public string redirect_url_qr_base64 { get; set; }
        /// <summary>
        /// A clean version of the redirect_url without the code visible that you can use in your UI 
        /// </summary>
        public string display_url { get; set; }
        /// <summary>
        /// The status of this lease process
        /// </summary>
        public LootLockerRemoteSessionLeaseStatus status { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerRemoteSessionStatusPollingResponse : LootLockerResponse
    {
        /// <summary>
        /// The current status of this lease process.
        /// </summary>
        public LootLockerRemoteSessionLeaseStatus lease_status { get; set; }
    }

    /// <summary>
    /// </summary>
    public class LootLockerStartRemoteSessionResponse : LootLockerSessionResponse
    {
        /// <summary>
        /// The current status of this lease process. If this is not of the status Authorized, the rest of the fields in this object will be empty.
        /// </summary>
        public LootLockerRemoteSessionLeaseStatus lease_status { get; set; }
        /// <summary>
        /// A refresh token that can be used to refresh the remote session instead of signing in each time the session token expires
        /// </summary>
        public string refresh_token { get; set; }
        /// <summary>
        /// The player identifier of the player
        /// </summary>
        public string player_identifier { get; set; }
    }
}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public class RemoteSessionPoller : MonoBehaviour
        {
            private static RemoteSessionPoller _instance;
            public static RemoteSessionPoller GetInstance()
            {
                if (_instance == null)
                {
                    _instance = new GameObject("LootLockerRemoteSessionPoller").AddComponent<RemoteSessionPoller>();
                }

                if (Application.isPlaying)
                    DontDestroyOnLoad(_instance.gameObject);

                return _instance;
            }

            public static bool DestroyInstance()
            {
                if (_instance == null)
                    return false;
                Destroy(_instance.gameObject);
                _instance = null;
                return true;
            }

#if UNITY_EDITOR
            [InitializeOnEnterPlayMode]
            static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
            {
                DestroyInstance();
            }
#endif

            private static readonly int _leasingProcessPollingRetryLimit = 5;

            private class LootLockerRemoteSessionProcess
            {
                public string LeaseCode;
                public string LeaseNonce;
                public LootLockerRemoteSessionLeaseStatus LastUpdatedStatus;
                public DateTime LeasingProcessTimeoutTime;
                public float PollingIntervalSeconds = 1.0f;
                public DateTime LastUpdatedAt;
                public int Retries = 0;
                public bool ShouldCancel;
                public Action<LootLockerRemoteSessionStatusPollingResponse> UpdateCallbackAction;
                public Action<LootLockerStartRemoteSessionResponse> ProcessCompletedCallbackAction;
            }

            private readonly Dictionary<Guid, LootLockerRemoteSessionProcess> _remoteSessionsProcesses =
                new Dictionary<Guid, LootLockerRemoteSessionProcess>();

            private static void AddRemoteSessionProcess(Guid processGuid, LootLockerRemoteSessionProcess processData)
            {
                GetInstance()._remoteSessionsProcesses.Add(processGuid, processData);
            }
            private static void RemoveRemoteSessionProcess(Guid processGuid)
            {
                var i = GetInstance();
                i._remoteSessionsProcesses.Remove(processGuid);
                if (i._remoteSessionsProcesses.Count <= 0)
                {
                    DestroyInstance();
                }
            }

            protected IEnumerator ContinualPollingAction(Guid processGuid)
            {
                if (!_remoteSessionsProcesses.TryGetValue(processGuid, out var preProcess))
                {
                    yield break;
                }
                yield return new WaitForSeconds(preProcess.PollingIntervalSeconds);
                while (_remoteSessionsProcesses.TryGetValue(processGuid, out var process))
                {
                    // Check if we should continue the polling
                    if (process.LeasingProcessTimeoutTime <= DateTime.UtcNow)
                    {
                        LootLockerStartRemoteSessionResponse timedOutResponse = new LootLockerStartRemoteSessionResponse
                        {
                            lease_status = LootLockerRemoteSessionLeaseStatus.Timed_out
                        };
                        process.ProcessCompletedCallbackAction?.Invoke(timedOutResponse);
                        RemoveRemoteSessionProcess(processGuid);
                        yield break;
                    }

                    if (process.ShouldCancel)
                    {
                        LootLockerStartRemoteSessionResponse canceledResponse = new LootLockerStartRemoteSessionResponse
                        {
                            success = false,
                            lease_status = LootLockerRemoteSessionLeaseStatus.Cancelled
                        };
                        process.ProcessCompletedCallbackAction?.Invoke(canceledResponse);
                        RemoveRemoteSessionProcess(processGuid);
                        yield break;
                    }

                    // Get latest state (or start a session if it's finished)
                    LootLockerStartRemoteSessionResponse startSessionResponse = null;
                    StartRemoteSession(process.LeaseCode, process.LeaseNonce,
                        response => { startSessionResponse = response; });
                    yield return new WaitUntil(() => startSessionResponse != null);

                    // Evaluate response and act on it
                    if (!_remoteSessionsProcesses.TryGetValue(processGuid, out var processAfterStatusCheck))
                    {
                        yield break;
                    }

                    if (!startSessionResponse.success)
                    {
                        if (startSessionResponse.statusCode >= 500 && startSessionResponse.statusCode <= 599 && processAfterStatusCheck.Retries <= _leasingProcessPollingRetryLimit)
                        {
                            // Recoverable error
                            processAfterStatusCheck.Retries++;
                            yield return new WaitForSeconds(processAfterStatusCheck.PollingIntervalSeconds);
                            continue;
                        }

                        startSessionResponse.lease_status = LootLockerRemoteSessionLeaseStatus.Failed;
                        processAfterStatusCheck.ProcessCompletedCallbackAction?.Invoke(startSessionResponse);
                        RemoveRemoteSessionProcess(processGuid);
                        yield break;
                    }

                    // Authorized = We're done
                    if (startSessionResponse.lease_status == LootLockerRemoteSessionLeaseStatus.Authorized)
                    {
                        processAfterStatusCheck.ProcessCompletedCallbackAction?.Invoke(startSessionResponse);
                        RemoveRemoteSessionProcess(processGuid);
                        yield break;
                    }

                    // Trigger update callback
                    LootLockerRemoteSessionStatusPollingResponse pollingResponse =
                        LootLockerResponse.Deserialize<LootLockerRemoteSessionStatusPollingResponse>(
                            startSessionResponse);
                    processAfterStatusCheck.UpdateCallbackAction?.Invoke(pollingResponse);
                    processAfterStatusCheck.LastUpdatedAt = DateTime.UtcNow;
                    processAfterStatusCheck.LastUpdatedStatus = pollingResponse.lease_status;

                    // Sleep for a bit before checking again
                    yield return new WaitForSeconds(processAfterStatusCheck.PollingIntervalSeconds);
                }
            }

            public Guid StartRemoteSessionWithContinualPolling(
                Action<LootLockerLeaseRemoteSessionResponse> remoteSessionLeaseInformation,
                Action<LootLockerRemoteSessionStatusPollingResponse> remoteSessionLeaseStatusUpdateCallback,
                Action<LootLockerStartRemoteSessionResponse> remoteSessionCompleted,
                float pollingIntervalSeconds = 1.0f,
                float timeOutAfterMinutes = 5.0f)
            {
                if (_remoteSessionsProcesses.Count > 0)
                {
                    foreach (var ongoingProcess in _remoteSessionsProcesses)
                    {
                        ongoingProcess.Value.ShouldCancel = true;
                    }
                }

                Guid processGuid = Guid.NewGuid();
                LootLockerRemoteSessionProcess lootLockerRemoteSessionProcess = new LootLockerRemoteSessionProcess()
                {
                    LastUpdatedAt = DateTime.UtcNow,
                    LeasingProcessTimeoutTime = DateTime.UtcNow.AddMinutes(timeOutAfterMinutes),
                    PollingIntervalSeconds = pollingIntervalSeconds,
                    UpdateCallbackAction = remoteSessionLeaseStatusUpdateCallback,
                    ProcessCompletedCallbackAction = remoteSessionCompleted
                };
                AddRemoteSessionProcess(processGuid, lootLockerRemoteSessionProcess);

                LeaseRemoteSession(leaseRemoteSessionResponse =>
                {
                    if (!_remoteSessionsProcesses.TryGetValue(processGuid, out var process))
                    {
                        return;
                    }
                    if (!leaseRemoteSessionResponse.success)
                    {
                        RemoveRemoteSessionProcess(processGuid);
                        remoteSessionLeaseInformation?.Invoke(leaseRemoteSessionResponse);
                        return;
                    }
                    remoteSessionLeaseInformation?.Invoke(leaseRemoteSessionResponse);

                    process.LeaseCode = leaseRemoteSessionResponse.code;
                    process.LeaseNonce = leaseRemoteSessionResponse.nonce;
                    process.LastUpdatedStatus = leaseRemoteSessionResponse.status;
                    process.LastUpdatedAt = DateTime.UtcNow;
                    StartCoroutine(ContinualPollingAction(processGuid));
                });
                return processGuid;
            }

            public void CancelRemoteSessionProcess(Guid processGuid)
            {
                if (_remoteSessionsProcesses.TryGetValue(processGuid, out var process))
                {
                    process.ShouldCancel = true;
                }
            }

            private void LeaseRemoteSession(Action<LootLockerLeaseRemoteSessionResponse> onComplete)
            {
                LootLockerLeaseRemoteSessionRequest leaseRemoteSessionRequest =
                    new LootLockerLeaseRemoteSessionRequest();

                EndPointClass endPoint = LootLockerEndPoints.leaseRemoteSession;
                LootLockerServerRequest.CallAPI(endPoint.endPoint,
                    endPoint.httpMethod,
                    LootLockerJson.SerializeObject(leaseRemoteSessionRequest),
                    (serverResponse) =>
                        onComplete?.Invoke(
                            LootLockerResponse.Deserialize<LootLockerLeaseRemoteSessionResponse>(serverResponse)),
                    false);
            }
            private void StartRemoteSession(string leaseCode, string nonce, Action<LootLockerStartRemoteSessionResponse> onComplete)
            {
                LootLockerStartRemoteSessionRequest remoteSessionRequest = new LootLockerStartRemoteSessionRequest
                {
                    lease_code = leaseCode,
                    nonce = nonce,
                };

                EndPointClass endPoint = LootLockerEndPoints.startRemoteSession;
                LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(remoteSessionRequest), (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerStartRemoteSessionResponse>(serverResponse);
                    if (!response.success)
                    {
                        onComplete?.Invoke(response);
                        return;
                    }

                    if (response.lease_status == LootLockerRemoteSessionLeaseStatus.Authorized)
                    {
                        CurrentPlatform.Set(Platforms.Remote);
                        LootLockerConfig.current.token = response.session_token;
                        LootLockerConfig.current.refreshToken = response.refresh_token;
                        LootLockerConfig.current.deviceID = response.player_ulid;
                    }

                    onComplete?.Invoke(response);
                }, false);
            }
        }
    }
}
