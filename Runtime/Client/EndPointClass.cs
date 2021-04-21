using LootLocker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    [System.Serializable]
    public class EndPointClass
    {
        public string endPoint;
        public LootLockerHTTPMethod httpMethod;

        public EndPointClass() { }

        public EndPointClass(string endPoint, LootLockerHTTPMethod httpMethod)
        {
            this.endPoint = endPoint;
            this.httpMethod = httpMethod;
        }
    }
}
