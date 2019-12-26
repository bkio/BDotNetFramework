/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Net;
using System.Text.RegularExpressions;
using BCommonUtilities;

namespace BWebServiceUtilities
{
    public class BWebPrefixStructure
    {
        private readonly string[] Prefixes;
        public string[] GetPrefixes()
        {
            //Return by value for encapsulation
            return Prefixes != null ? (string[])Prefixes.Clone() : new string[] { };
        }
        public int GetPrefixesLength()
        {
            return Prefixes != null ? Prefixes.Length : 0;
        }
        
        private readonly Func<BWebServiceBase> ListenerInitializer;

        public BWebPrefixStructure(string[] _Prefixes, Func<BWebServiceBase> _ListenerInitializer)
        {
            Prefixes = _Prefixes;
            ListenerInitializer = _ListenerInitializer;
        }

        public bool GetCallbackFromRequest(out Func<BWebServiceBase> _Initializer, out string _MatchedPrefix, HttpListenerContext _Context)
        {
            _Initializer = null;
            _MatchedPrefix = null;

            if (_Context == null || Prefixes == null || ListenerInitializer == null)
            {
                return false;
            }
            
            foreach (string Prefix in Prefixes)
            {
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