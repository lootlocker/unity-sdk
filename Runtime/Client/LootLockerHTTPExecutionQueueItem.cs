using LootLocker;
using LootLocker.HTTP;
using UnityEngine.Networking;

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
