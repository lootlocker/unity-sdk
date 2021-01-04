using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace LootLockerRequests
{
    public class LootLockerGetRequest
    {
        public List<string> getRequests = new List<string>();

        public bool ShouldSerializegetRequests()
        {
            // don't serialize the getRequests property.
            return false;
        }
    }
}