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
        public static void Ping(Action<LootLockerPingResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.ping;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}
