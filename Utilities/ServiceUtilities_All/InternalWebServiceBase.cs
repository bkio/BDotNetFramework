/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Net;
using BWebServiceUtilities;

namespace ServiceUtilities.All
{
    public abstract class InternalWebServiceBase : BppWebServiceBase
    {
        protected readonly string InternalCallPrivateKey;
        public InternalWebServiceBase(string _InternalCallPrivateKey)
        {
            InternalCallPrivateKey = _InternalCallPrivateKey;
        }
        private InternalWebServiceBase() { }

        protected override BWebServiceResponse OnRequestPP(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            if (UrlParameters.ContainsKey("secret") && UrlParameters["secret"] == InternalCallPrivateKey)
            {
                return Process(_Context, _ErrorMessageAction);
            }
            return BWebResponse.Forbidden("You are trying to access to a private service.");
        }

        protected abstract BWebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }
}