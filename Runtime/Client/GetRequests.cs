using System.Collections.Generic;
using LootLocker.ZeroDepJson;

namespace LootLocker.Requests
{
    public class LootLockerGetRequest
    {
        [Json(IgnoreWhenSerializing = true, IgnoreWhenDeserializing = true)]
        public List<string> getRequests = new List<string>();
    }
}