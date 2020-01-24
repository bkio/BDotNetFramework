/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Net;
using BCommonUtilities;

namespace BWebServiceUtilities
{
    public struct BWebServiceResponse
    {
        public readonly int StatusCode;
        public readonly Tuple<string, string>[] Headers;
        public readonly BStringOrStream ResponseContent;
        public readonly EBResponseContentType ResponseContentType;

        public BWebServiceResponse(int _StatusCode, Tuple<string, string>[] _Headers, BStringOrStream _ResponseContent, EBResponseContentType _ResponseContentType)
        {
            StatusCode = _StatusCode;
            Headers = _Headers;
            ResponseContent = _ResponseContent;
            ResponseContentType = _ResponseContentType;
        }

        public BWebServiceResponse(int _StatusCode, BStringOrStream _ResponseContent, EBResponseContentType _ResponseContentType)
        {
            StatusCode = _StatusCode;
            Headers = null;
            ResponseContent = _ResponseContent;
            ResponseContentType = _ResponseContentType;
        }
    }
    public interface IBWebServiceInterface
    {
        BWebServiceResponse OnRequest(
            HttpListenerContext Context,
            Action<string> _ErrorMessageAction = null);
    }
}