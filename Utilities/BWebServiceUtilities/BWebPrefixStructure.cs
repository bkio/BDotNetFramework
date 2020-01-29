/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using BCommonUtilities;

namespace BWebServiceUtilities
{
    public class BWebPrefixStructure
    {
        private readonly string[] Prefixes_SortedByLength;
        public string[] GetPrefixes()
        {
            //Return by value for encapsulation
            return Prefixes_SortedByLength != null ? (string[])Prefixes_SortedByLength.Clone() : new string[] { };
        }

        public int GetPrefixesLength()
        {
            return Prefixes_SortedByLength != null ? Prefixes_SortedByLength.Length : 0;
        }

        public string GetLongestPrefix()
        {
            if (Prefixes_SortedByLength == null || Prefixes_SortedByLength.Length == 0) return null;
            return Prefixes_SortedByLength[Prefixes_SortedByLength.Length - 1];
        }

        private readonly Func<BWebServiceBase> ListenerInitializer;

        public BWebPrefixStructure(string[] _Prefixes, Func<BWebServiceBase> _ListenerInitializer)
        {
            if (_Prefixes != null && _Prefixes.Length > 0)
            {
                Prefixes_SortedByLength = _Prefixes.OrderBy(x => x.Length).ToArray();
            }
            ListenerInitializer = _ListenerInitializer;
        }

        public bool GetCallbackFromRequest(out Func<BWebServiceBase> _Initializer, out string _MatchedPrefix, HttpListenerContext _Context)
        {
            _Initializer = null;
            _MatchedPrefix = null;

            if (_Context == null || Prefixes_SortedByLength == null || Prefixes_SortedByLength.Length == 0 || ListenerInitializer == null)
            {
                return false;
            }
            
            for (var i = (Prefixes_SortedByLength.Length - 1); i >= 0; i--)
            {
                var Prefix = Prefixes_SortedByLength[i];
                if (Regex.IsMatch(_Context.Request.RawUrl, BUtility.WildCardToRegular(Prefix)))
                {
                    _MatchedPrefix = Prefix;
                    _Initializer = ListenerInitializer;
                    return _Initializer != null;
                }
            }
            return false;
        }
    }
}