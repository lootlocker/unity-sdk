using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace LootLocker.Utilities.HTTP
{
    public class QueryParamaterBuilder
    {
        private List<KeyValuePair<string, string>> _queryParams = null;

        public QueryParamaterBuilder()
        {
            _queryParams = new List<KeyValuePair<string, string>>();
        }

        public QueryParamaterBuilder(List<KeyValuePair<string, string>> queryParams)
        {
            _queryParams = queryParams ?? new List<KeyValuePair<string, string>>();
        }

        public QueryParamaterBuilder(Dictionary<string, string> queryParams)
        {
            _queryParams = queryParams != null ? queryParams.ToList() : new List<KeyValuePair<string, string>>();
        }

        public void Add(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                return;
            }
            _queryParams?.Add(new KeyValuePair<string, string>(key, WebUtility.UrlEncode(value)));
        }

        public void Add(string key, int value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            _queryParams?.Add(new KeyValuePair<string, string>(key, WebUtility.UrlEncode($"{value}")));
        }

        public void Add(string key, ulong value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            _queryParams?.Add(new KeyValuePair<string, string>(key, WebUtility.UrlEncode($"{value}")));
        }

        public string Build()
        {
            if (_queryParams == null || _queryParams.Count == 0) return string.Empty;

            string query = "?";

            foreach (KeyValuePair<string, string> pair in _queryParams)
            {
                if (string.IsNullOrEmpty(pair.Key) || string.IsNullOrEmpty(pair.Value))
                    continue;

                if (query.Length > 1)
                    query += "&";

                query += pair.Key + "=" + pair.Value;
            }

            if (query.Equals("?"))
                query = string.Empty;
            return query;
        }

        public override string ToString()
        {
            return Build();
        }
    }
}