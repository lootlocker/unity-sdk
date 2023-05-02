using System.Collections.Generic;
#if !LOOTLOCKER_USE_NEWTONSOFTJSON
using LLlibs.ZeroDepJson;
#endif

namespace LootLocker.Requests
{
    public class LootLockerGetRequest
    {

#if !LOOTLOCKER_USE_NEWTONSOFTJSON
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