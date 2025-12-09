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

    /// <summary>
    /// The intent for a remote session leasing process
    /// </summary>
    public enum LootLockerRemoteSessionLeaseIntent
    {
        login = 0,
        link = 1
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
        /// The Title ID of the game
        /// </summary>
        public string title_id { get; set; }
        /// <summary>
        /// The Environment ID of the game and environment
        /// </summary>
        public string environment_id { get; set; }
        /// <summary>
        /// The Game Version configured for the game
        /// </summary>
        public string game_version { get; set; }

        public LootLockerLeaseRemoteSessionRequest(string titleId, string environmentId)
        {
            title_id = titleId;
            environment_id = environmentId;
            game_version = LootLockerConfig.current.game_version;
        }
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

    /// <summary>
    /// </summary>
    public class LootLockerRefreshRemoteSessionRequest : LootLockerGetRequest
    {
        /// <summary>
        /// The api key configured for this game
        /// </summary>
        public string game_key => LootLockerConfig.current.apiKey?.ToString();
        /// <summary>
        /// The refresh token used to refresh this session
        /// </summary>
        public string refresh_token { get; set; }
        /// <summary>
        /// The game version configured in LootLocker settings
        /// </summary>
        public string game_version => LootLockerConfig.current.game_version;

        public LootLockerRefreshRemoteSessionRequest(string refreshToken)
        {
            this.refresh_token = refreshToken;
        }
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

    /// <summary>
    /// </summary>
    public class LootLockerRefreshRemoteSessionResponse : LootLockerSessionResponse
    {
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
        public class RemoteSessionPoller : MonoBehaviour, ILootLockerService
        {
            #region ILootLockerService Implementation

            public bool IsInitialized { get; private set; }
            public string ServiceName => "RemoteSessionPoller";

            void ILootLockerService.Initialize()
            {
                if (IsInitialized) return;
                IsInitialized = true;
            }

            void ILootLockerService.Reset()
            {
                
                // Cancel all ongoing processes
                if (_remoteSessionsProcesses != null)
                {
                    foreach (var process in _remoteSessionsProcesses.Values)
                    {
                        if (process != null)
                        {
                            process.ShouldCancel = true;
                        }
                    }
                    _remoteSessionsProcesses.Clear();
                }

                IsInitialized = false;
                _instance = null;
            }

            void ILootLockerService.HandleApplicationPause(bool pauseStatus)
            {
                // RemoteSessionPoller doesn't need special pause handling
            }

            void ILootLockerService.HandleApplicationFocus(bool hasFocus)
            {
                // RemoteSessionPoller doesn't need special focus handling
            }

            void ILootLockerService.HandleApplicationQuit()
            {
                ((ILootLockerService)this).Reset();
            }

            #endregion

            #region Hybrid Singleton Pattern

            private static RemoteSessionPoller _instance;
            private static readonly object _instanceLock = new object();

            protected static RemoteSessionPoller GetInstance()
            {
                if (_instance != null)
                {
                    return _instance;
                }

                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        // Register the service on-demand if not already registered
                        if (!LootLockerLifecycleManager.HasService<RemoteSessionPoller>())
                        {
                            LootLockerLifecycleManager.RegisterService<RemoteSessionPoller>();
                        }
                        
                        // Get service from LifecycleManager
                        _instance = LootLockerLifecycleManager.GetService<RemoteSessionPoller>();
                    }
                }

                return _instance;
            }

            #endregion

            #region Public Methods
            public static Guid StartRemoteSessionWithContinualPolling(
                LootLockerRemoteSessionLeaseIntent leaseIntent,
                Action<LootLockerLeaseRemoteSessionResponse> remoteSessionLeaseInformation,
                Action<LootLockerRemoteSessionStatusPollingResponse> remoteSessionLeaseStatusUpdateCallback,
                Action<LootLockerStartRemoteSessionResponse> remoteSessionCompleted,
                float pollingIntervalSeconds = 1.0f,
                float timeOutAfterMinutes = 5.0f,
                string forPlayerWithUlid = null)
            {
                var instance = GetInstance();
                if (instance == null)
                {
                    remoteSessionCompleted?.Invoke(new LootLockerStartRemoteSessionResponse
                    {
                        success = false,
                        lease_status = LootLockerRemoteSessionLeaseStatus.Failed,
                        errorData = new LootLockerErrorData { message = "Failed to start remote session with continual polling: RemoteSessionPoller instance could not be created." }
                    });
                    return Guid.Empty;
                }
                return instance._StartRemoteSessionWithContinualPolling(leaseIntent, remoteSessionLeaseInformation,
                    remoteSessionLeaseStatusUpdateCallback, remoteSessionCompleted, pollingIntervalSeconds,
                    timeOutAfterMinutes, forPlayerWithUlid);
            }

            public static void CancelRemoteSessionProcess(Guid processGuid)
            {
                GetInstance()?._CancelRemoteSessionProcess(processGuid);
            }

            #endregion

            #region Internal Workings
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
                public LootLockerRemoteSessionLeaseIntent Intent;
                public string forPlayerWithUlid;
                public Action<LootLockerRemoteSessionStatusPollingResponse> UpdateCallbackAction;
                public Action<LootLockerStartRemoteSessionResponse> ProcessCompletedCallbackAction;
            }

            private readonly Dictionary<Guid, LootLockerRemoteSessionProcess> _remoteSessionsProcesses =
                new Dictionary<Guid, LootLockerRemoteSessionProcess>();
                
            private static void RemoveRemoteSessionProcess(Guid processGuid)
            {
                var i = GetInstance();
                if (i == null) return;
                i._remoteSessionsProcesses.Remove(processGuid);
                
                // Auto-cleanup: if no more processes are running, unregister the service
                if (i._remoteSessionsProcesses.Count <= 0)
                {
                    CleanupServiceWhenDone();
                }
            }

            /// <summary>
            /// Cleanup and unregister the RemoteSessionPoller service when all processes are complete
            /// </summary>
            private static void CleanupServiceWhenDone()
            {
                if (LootLockerLifecycleManager.HasService<RemoteSessionPoller>())
                {
                    LootLockerLogger.Log("All remote session processes complete - cleaning up RemoteSessionPoller", LootLockerLogger.LogLevel.Debug);
                    
                    // Reset our local cache first
                    _instance = null;
                    
                    // Remove the service from LifecycleManager
                    LootLockerLifecycleManager.UnregisterService<RemoteSessionPoller>();
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

            private Guid _StartRemoteSessionWithContinualPolling(
                LootLockerRemoteSessionLeaseIntent leaseIntent,
                Action<LootLockerLeaseRemoteSessionResponse> remoteSessionLeaseInformation,
                Action<LootLockerRemoteSessionStatusPollingResponse> remoteSessionLeaseStatusUpdateCallback,
                Action<LootLockerStartRemoteSessionResponse> remoteSessionCompleted,
                float pollingIntervalSeconds = 1.0f,
                float timeOutAfterMinutes = 5.0f,
                string forPlayerWithUlid = null)
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
                    ProcessCompletedCallbackAction = remoteSessionCompleted,
                    Intent = leaseIntent,
                    forPlayerWithUlid = forPlayerWithUlid
                };
                _remoteSessionsProcesses.Add(processGuid, lootLockerRemoteSessionProcess);

                LootLockerAPIManager.GetGameInfo(gameInfoResponse =>
                {
                    if (!gameInfoResponse.success)
                    {
                        if (!_remoteSessionsProcesses.TryGetValue(processGuid, out var process))
                        {
                            return;
                        }
                        RemoveRemoteSessionProcess(processGuid);
                        remoteSessionLeaseInformation?.Invoke(new LootLockerLeaseRemoteSessionResponse
                        {
                            success = gameInfoResponse.success,
                            statusCode = gameInfoResponse.statusCode,
                            text = gameInfoResponse.text,
                            errorData = gameInfoResponse.errorData,
                            requestContext = gameInfoResponse.requestContext,
                            status = LootLockerRemoteSessionLeaseStatus.Failed
                        });
                        return;
                    }

                    LeaseRemoteSession(leaseIntent, gameInfoResponse.info.title_id, gameInfoResponse.info.environment_id, forPlayerWithUlid, leaseRemoteSessionResponse =>
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
                });
                return processGuid;
            }

            private void _CancelRemoteSessionProcess(Guid processGuid)
            {
                if (_remoteSessionsProcesses.TryGetValue(processGuid, out var process))
                {
                    process.ShouldCancel = true;
                }
            }

            private void LeaseRemoteSession(
                LootLockerRemoteSessionLeaseIntent leaseIntent,
                string titleId,
                string environmentId,
                string forPlayerWithUlid,
                Action<LootLockerLeaseRemoteSessionResponse> onComplete)
            {
                LootLockerLeaseRemoteSessionRequest leaseRemoteSessionRequest =
                    new LootLockerLeaseRemoteSessionRequest(titleId, environmentId);

                EndPointClass endPoint = leaseIntent == LootLockerRemoteSessionLeaseIntent.login ? LootLockerEndPoints.leaseRemoteSession : LootLockerEndPoints.leaseRemoteSessionForLinking;
                LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint,
                    endPoint.httpMethod,
                    LootLockerJson.SerializeObject(leaseRemoteSessionRequest),
                    (serverResponse) =>
                        onComplete?.Invoke(
                            LootLockerResponse.Deserialize<LootLockerLeaseRemoteSessionResponse>(serverResponse)),
                    leaseIntent == LootLockerRemoteSessionLeaseIntent.link);
            }
            private void StartRemoteSession(string leaseCode, string nonce, Action<LootLockerStartRemoteSessionResponse> onComplete)
            {
                LootLockerStartRemoteSessionRequest remoteSessionRequest = new LootLockerStartRemoteSessionRequest
                {
                    lease_code = leaseCode,
                    nonce = nonce,
                };

                EndPointClass endPoint = LootLockerEndPoints.startRemoteSession;
                LootLockerServerRequest.CallAPI(null, endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(remoteSessionRequest), (serverResponse) =>
                {
                    var response = LootLockerResponse.Deserialize<LootLockerStartRemoteSessionResponse>(serverResponse);
                    if (!response.success)
                    {
                        onComplete?.Invoke(response);
                        return;
                    }

                    if (response.lease_status == LootLockerRemoteSessionLeaseStatus.Authorized)
                    {
                        LootLockerEventSystem.TriggerSessionStarted(new LootLockerPlayerData
                        {
                            SessionToken = response.session_token,
                            RefreshToken = response.refresh_token,
                            ULID = response.player_ulid,
                            Identifier = response.player_identifier,
                            PublicUID = response.public_uid,
                            LegacyID = response.player_id,
                            Name = response.player_name,
                            WhiteLabelEmail = "",
                            WhiteLabelToken = "",
                            CurrentPlatform = LootLockerAuthPlatform.GetPlatformRepresentation(LL_AuthPlatforms.Remote),
                            LastSignIn = DateTime.Now,
                            CreatedAt = response.player_created_at,
                            WalletID = response.wallet_id,
                        });
                    }

                    onComplete?.Invoke(response);
                }, false);
            }
            #endregion
        }
    }
}
