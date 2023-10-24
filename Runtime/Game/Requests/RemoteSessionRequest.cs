using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        /// The current status of this lease process. If this is not of the status Authorized, the rest of the fields in this object will be empty.
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
        private static readonly double _leasingProcessTimeoutLimitInMinutes = 5.0d;
        private static readonly int _leasingProcessPollingIntervalMilliseconds = 2500;

        private class LootLockerRemoteSessionProcess
        {
            public string LeaseCode;
            public string LeaseNonce;
            public LootLockerRemoteSessionLeaseStatus LastUpdatedStatus;
            public DateTime LeasingProcessTimeoutTime;
            public DateTime LastUpdatedAt;
            public bool ShouldCancel;
            public Action<LootLockerRemoteSessionStatusPollingResponse> UpdateCallbackAction;
            public Action<LootLockerStartRemoteSessionResponse> ProcessCompletedCallbackAction;
        }

        private static readonly Dictionary<Guid, LootLockerRemoteSessionProcess> _remoteSessionsProcesses =
            new Dictionary<Guid, LootLockerRemoteSessionProcess>();

        private static readonly Action<Guid> _continualPollingAction = (processGuid) =>
            {
                if (!_remoteSessionsProcesses.TryGetValue(processGuid, out var process))
                {
                    return;
                }

                if (process.ShouldCancel)
                {
                    LootLockerStartRemoteSessionResponse canceledResponse = new LootLockerStartRemoteSessionResponse
                    {
                        lease_status = LootLockerRemoteSessionLeaseStatus.Cancelled
                    };
                    process.ProcessCompletedCallbackAction?.Invoke(canceledResponse);
                    _remoteSessionsProcesses.Remove(processGuid);
                    return;
                }

                if (process.LeasingProcessTimeoutTime <= DateTime.UtcNow)
                {
                    LootLockerStartRemoteSessionResponse timedOutResponse = new LootLockerStartRemoteSessionResponse
                    {
                        lease_status = LootLockerRemoteSessionLeaseStatus.Timed_out
                    };
                    process.ProcessCompletedCallbackAction?.Invoke(timedOutResponse);
                    _remoteSessionsProcesses.Remove(processGuid);
                    return;
                }

                StartRemoteSession(process.LeaseCode, process.LeaseNonce, response =>
                {
                    if (!_remoteSessionsProcesses.TryGetValue(processGuid, out var processAfterStatusCheck))
                    {
                        return;
                    }
                    if (!response.success)
                    {
                        response.lease_status = LootLockerRemoteSessionLeaseStatus.Failed;
                        processAfterStatusCheck.ProcessCompletedCallbackAction?.Invoke(response);
                        _remoteSessionsProcesses.Remove(processGuid);
                        return;
                    }
                    if (response.lease_status == LootLockerRemoteSessionLeaseStatus.Authorized)
                    {
                        processAfterStatusCheck.ProcessCompletedCallbackAction?.Invoke(response);
                        _remoteSessionsProcesses.Remove(processGuid);
                        return;
                    }

                    LootLockerRemoteSessionStatusPollingResponse pollingResponse =
                        LootLockerResponse.Deserialize<LootLockerRemoteSessionStatusPollingResponse>(response);
                    processAfterStatusCheck.UpdateCallbackAction?.Invoke(pollingResponse);
                    processAfterStatusCheck.LastUpdatedAt = DateTime.UtcNow;

                    Task.Delay(_leasingProcessPollingIntervalMilliseconds).ContinueWith(task =>
                    {
                        _continualPollingAction?.Invoke(processGuid);
                    });
                });
            };

        public static Guid StartRemoteSessionWithContinualPolling(
            Action<LootLockerLeaseRemoteSessionResponse> remoteSessionLeaseInformation,
            Action<LootLockerRemoteSessionStatusPollingResponse> remoteSessionLeaseStatusUpdateCallback,
            Action<LootLockerStartRemoteSessionResponse> remoteSessionCompleted)
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
                LeasingProcessTimeoutTime = DateTime.UtcNow.AddMinutes(_leasingProcessTimeoutLimitInMinutes),
                UpdateCallbackAction = remoteSessionLeaseStatusUpdateCallback,
                ProcessCompletedCallbackAction = remoteSessionCompleted
            };
            _remoteSessionsProcesses.Add(processGuid, lootLockerRemoteSessionProcess);

            LeaseRemoteSession(leaseRemoteSessionResponse =>
            {
                if (!leaseRemoteSessionResponse.success)
                {
                    _remoteSessionsProcesses.Remove(processGuid);
                    remoteSessionLeaseInformation?.Invoke(leaseRemoteSessionResponse);
                    return;
                }

                _continualPollingAction?.Invoke(processGuid);
            });
            return processGuid;
        }

        public static void CancelRemoteSessionProcess(Guid processGuid)
        {
            if (_remoteSessionsProcesses.TryGetValue(processGuid, out var process))
            {
                process.ShouldCancel = true;
            }
        }

        private static void LeaseRemoteSession(Action<LootLockerLeaseRemoteSessionResponse> onComplete)
        {
            LootLockerLeaseRemoteSessionRequest leaseRemoteSessionRequest = new LootLockerLeaseRemoteSessionRequest();

            EndPointClass endPoint = LootLockerEndPoints.leaseRemoteSession;
            LootLockerServerRequest.CallAPI(endPoint.endPoint,
                endPoint.httpMethod,
                LootLockerJson.SerializeObject(leaseRemoteSessionRequest),
                (serverResponse) => onComplete?.Invoke(LootLockerResponse.Deserialize<LootLockerLeaseRemoteSessionResponse>(serverResponse)),
                false);
        }

        private static void StartRemoteSession(string leaseCode, string nonce, Action<LootLockerStartRemoteSessionResponse> onComplete)
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
                    LootLockerConfig.current.deviceID = response.player_identifier;
                }

                onComplete?.Invoke(response);
            }, false);
        }
    }
}
