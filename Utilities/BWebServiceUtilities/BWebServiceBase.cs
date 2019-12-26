/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Net;
using BCloudServiceUtilities;

namespace BWebServiceUtilities
{
    public abstract class BWebServiceBase : IBWebServiceInterface
    {
        private IBTracingServiceInterface TracingService;
        protected IBTracingServiceInterface GetTracingService()
        {
            return TracingService;
        }

        /// <summary>
        /// If the url is like: /abc/def/*/qwe/ghj/*/bk
        /// This map would contain def->[Value of first *], ghj->[Value of second *]
        /// If there is not a value before the first occurence of *, it will be ignored.
        /// </summary>
        protected Dictionary<string, string> RestfulUrlParameters = new Dictionary<string, string>();

        /// <summary>
        /// The map that contains parameters provided after question mark.
        /// </summary>
        protected Dictionary<string, string> UrlParameters = new Dictionary<string, string>();

        private bool bInitialized = false;
        protected bool IsInitialized()
        {
            return bInitialized;
        }
        public void InitializeWebService(HttpListenerContext _Context, string _MatchedPrefix, IBTracingServiceInterface _TracingService = null)
        {
            if (bInitialized) return;
            bInitialized = true;
            
            TracingService = _TracingService;

            var SplittedRawUrl = _Context.Request.RawUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var SplittedMatchedPrefix = _MatchedPrefix.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (SplittedRawUrl != null && SplittedMatchedPrefix != null && SplittedRawUrl.Length >= SplittedMatchedPrefix.Length)
            {
                for (int i = 1; i < SplittedMatchedPrefix.Length; i++)
                {
                    if (SplittedMatchedPrefix[i] == "*")
                    {
                        RestfulUrlParameters[SplittedRawUrl[i - 1]] = SplittedRawUrl[i];
                    }
                }
            }

            var Params = BWebUtilities.AnalyzeURLParametersFromRawURL(_Context.Request.RawUrl);
            if (Params != null)
            {
                foreach (var Param in Params)
                {
                    UrlParameters[Param.Item1] = Param.Item2;
                }
            }
        }

        public abstract BWebServiceResponse OnRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);

        protected BWebServiceBase()
        {
        }
    }
}