/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Linq;
using System.Net;

namespace BWebServiceUtilities
{
    public class BWebPrefixStructure
    {
        private string[] Prefixes;
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

        public bool GetCallbackFromRequest(out Func<BWebServiceBase> _Initializer, HttpListenerContext _Context)
        {
            if (_Context == null || Prefixes == null || ListenerInitializer == null)
            {
                _Initializer = null;
                return false;
            }
            
            if (_Context.Request.RawUrl.Length == 0 && Prefixes.Contains("LISTEN_ROOT"))
            {
                _Initializer = ListenerInitializer;
                return _Initializer != null;
            }

            var URL_SplittedBySlash = _Context.Request.RawUrl.Split(new char[] { '/' }, StringSplitOptions.None);

            foreach (string Prefix in Prefixes)
            {
                if (Prefix == "*")
                {
                    _Initializer = ListenerInitializer;
                    return _Initializer != null;
                }

                if (URL_SplittedBySlash != null && URL_SplittedBySlash.Length > 0)
                {
                    var Prefix_SplittedBySlash = Prefix.Split(new char[] { '/' }, StringSplitOptions.None);

                    if (Prefix_SplittedBySlash != null && Prefix_SplittedBySlash.Length > 0)
                    {
                        if (Prefix_SplittedBySlash.Length == URL_SplittedBySlash.Length)
                        {
                            bool bSuccess = true;
                            for (int i = 0; i < Prefix_SplittedBySlash.Length; i++)
                            {
                                bool bEndsWithAsterix = Prefix_SplittedBySlash[i].EndsWith("*");

                                if (Prefix_SplittedBySlash[i] == "*" || 
                                    ((!bEndsWithAsterix && URL_SplittedBySlash[i] == Prefix_SplittedBySlash[i]) ||
                                    (bEndsWithAsterix && URL_SplittedBySlash[i].Contains(Prefix_SplittedBySlash[i].Trim('*')))))
                                {
                                    continue;
                                }

                                bSuccess = false;
                            }
                            if (bSuccess)
                            {
                                _Initializer = ListenerInitializer;
                                return _Initializer != null;
                            }
                        }
                        else if (Prefix.EndsWith("*") && _Context.Request.RawUrl.TrimStart('/').Contains(Prefix.TrimStart('/').TrimEnd('*')))
                        {
                            _Initializer = ListenerInitializer;
                            return _Initializer != null;
                        }
                    }
                }
            }
            _Initializer = null;
            return false;
        }
    }
}