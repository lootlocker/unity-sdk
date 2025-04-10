using System;
using LootLocker.Requests;

namespace LootLocker.Requests
{
    public class LootLockerPingResponse : LootLockerResponse
    {
        public string date { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void Ping(string forPlayerWithUlid, Action<LootLockerPingResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.ping;

            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}
