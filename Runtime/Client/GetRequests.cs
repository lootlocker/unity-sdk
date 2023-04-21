using System.Collections.Generic;
#if USE_LOOTLOCKER_ZERODEPJSON
using LootLocker.ZeroDepJson;
#endif

namespace LootLocker.Requests
{
    public class LootLockerGetRequest
    {

#if USE_LOOTLOCKER_ZERODEPJSON
        [Json(IgnoreWhenSerializing = true, IgnoreWhenDeserializing = true)]
        public List<string> getRequests = new List<string>();
#else
        public List<string> getRequests = new List<string>();

        public bool ShouldSerializegetRequests()
        {
            // don't serialize the getRequests property.
            return false;
        }
#endif
    }
}