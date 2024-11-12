using System;
using LootLocker.LootLockerEnums;

namespace LootLocker.LootLockerEnums
{
    public enum LootLockerCallerRole { User, Admin, Player, Base };
}

namespace LootLocker
{
    [Serializable]
    public class EndPointClass
    {
        public string endPoint { get; set; }
        public LootLockerHTTPMethod httpMethod { get; set; }
        public LootLockerCallerRole callerRole { get; set; }

        public EndPointClass() { }

        public EndPointClass(string endPoint, LootLockerHTTPMethod httpMethod, LootLockerCallerRole callerRole = LootLockerCallerRole.User)
        {
            this.endPoint = endPoint;
            this.httpMethod = httpMethod;
            this.callerRole = callerRole;
        }
    }
}
