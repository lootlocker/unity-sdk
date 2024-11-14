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

public class LootLockerHTTPExecutionQueueItem
{

    public LootLockerHTTPRequestData RequestData { get; set; } = null;

    public UnityWebRequest WebRequest { get; set; } = null;

    public UnityWebRequestAsyncOperation AsyncOperation { get; set; } = null;

    public float RequestStartTime { get; set; } = float.MinValue;

    public bool IsWaitingForSessionRefresh { get; set; } = false;

    public bool Done { get; set; } = false;

    public LootLockerResponse Response { get; set; } = null;

}
