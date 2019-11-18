/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Net;
using BCloudServiceUtilities;
using BCommonUtilities;

namespace BWebServiceUtilities
{
    public abstract class BWebServiceBase : IBWebServiceInterface
    {
        private IBTracingServiceInterface TracingService;
        protected IBTracingServiceInterface GetTracingService()
        {
            return TracingService;
        }

        private bool bInitialized = false;
        protected bool IsInitialized()
        {
            return bInitialized;
        }
        public void InitializeWebService(IBTracingServiceInterface _TracingService = null)
        {
            if (bInitialized) return;
            bInitialized = true;
            TracingService = _TracingService;
        }

        public abstract BWebServiceResponse OnRequest(HttpListenerContext Context, Action<string> _ErrorMessageAction = null);

        protected BWebServiceBase()
        {
        }
    }
}