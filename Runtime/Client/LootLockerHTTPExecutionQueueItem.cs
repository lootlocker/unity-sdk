using System;
using LootLocker;
using LootLocker.HTTP;
using UnityEngine.Networking;

namespace LootLocker.LootLockerEnums
{
    public enum HTTPExecutionQueueProcessingResult
    {
        None = 0,
        WaitForNextTick = 1,
        Completed_Success = 2,
        Completed_Failed = 3,
        Completed_TimedOut = 4,
        ShouldBeRetried = 5,
        NeedsSessionRefresh = 6
    }
}

namespace LootLocker
{
public class LootLockerHTTPExecutionQueueItem
{

    public LootLockerHTTPRequestData RequestData { get; set; } = null;

    public UnityWebRequest WebRequest { get; set; } = null;

    public UnityWebRequestAsyncOperation AsyncOperation { get; set; } = null;

    public float RequestStartTime { get; set; } = float.MinValue;

    public DateTime? RetryAfter { get; set; } = null;

    public bool IsWaitingForSessionRefresh { get; set; } = false;

    public bool Done { get; set; } = false;

    public LootLockerResponse Response { get; set; } = null;

    public void OnDestroy()
    {
        AbortRequest();
    }

    public void AbortRequest()
    {
        if(WebRequest != null)
        {
            WebRequest.Abort();
            WebRequest.Dispose();
        }
        WebRequest = null;
        AsyncOperation = null;
    }
}
}