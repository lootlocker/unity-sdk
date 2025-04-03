using System;
using System.Net;
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

        public string WithPathParameter(object arg0)
        {
            return string.Format(endPoint, WebUtility.UrlEncode(arg0.ToString()));
        }

        public string WithPathParameters(object arg0, object arg1)
        {
            return string.Format(endPoint, WebUtility.UrlEncode(arg0.ToString()), WebUtility.UrlEncode(arg1.ToString()));
        }

        public string WithPathParameters(object arg0, object arg1, object arg2)
        {
            return string.Format(endPoint, WebUtility.UrlEncode(arg0.ToString()), WebUtility.UrlEncode(arg1.ToString()), WebUtility.UrlEncode(arg2.ToString()));
        }

        public string WithPathParameters(object arg0, object arg1, object arg2, object arg3)
        {
            return string.Format(endPoint, WebUtility.UrlEncode(arg0.ToString()), WebUtility.UrlEncode(arg1.ToString()), WebUtility.UrlEncode(arg2.ToString()), WebUtility.UrlEncode(arg3.ToString()));
        }
    }
}
