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
        public readonly Tuple<string, string>[] Cookies;
        public readonly BStringOrStream ResponseContent;
        public readonly EBResponseContentType ResponseContentType;

        public BWebServiceResponse(int _StatusCode, Tuple<string, string>[] _Headers, Tuple<string, string>[] _Cookies, BStringOrStream _ResponseContent, EBResponseContentType _ResponseContentType)
        {
            StatusCode = _StatusCode;
            Headers = _Headers;
            Cookies = _Cookies;
            ResponseContent = _ResponseContent;
            ResponseContentType = _ResponseContentType;
        }

        public BWebServiceResponse(int _StatusCode, BStringOrStream _ResponseContent, EBResponseContentType _ResponseContentType)
        {
            StatusCode = _StatusCode;
            Headers = null;
            Cookies = null;
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