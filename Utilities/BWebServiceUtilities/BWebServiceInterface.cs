/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Net;
using BCommonUtilities;

namespace BWebServiceUtilities
{
    public struct BWebServiceResponse
    {
        public readonly int StatusCode;
        public readonly Dictionary<string, IEnumerable<string>> Headers;
        public readonly CookieCollection Cookies;
        public readonly BStringOrStream ResponseContent;
        public readonly EBResponseContentType ResponseContentType;

        public BWebServiceResponse(int _StatusCode, Dictionary<string, IEnumerable<string>> _Headers, BStringOrStream _ResponseContent, EBResponseContentType _ResponseContentType)
        {
            StatusCode = _StatusCode;

            if (_Headers == null)
            {
                Headers = new Dictionary<string, IEnumerable<string>>();
            }
            else
            {
                Headers = new Dictionary<string, IEnumerable<string>>(_Headers);
            }

            Cookies = new CookieCollection();

            ResponseContent = _ResponseContent;
            ResponseContentType = _ResponseContentType;
        }

        public BWebServiceResponse(int _StatusCode, Dictionary<string, IEnumerable<string>> _Headers, CookieCollection _Cookies, BStringOrStream _ResponseContent, EBResponseContentType _ResponseContentType)
        {
            StatusCode = _StatusCode;
            if (_Headers == null)
            {
                Headers = new Dictionary<string, IEnumerable<string>>();
            }
            else
            {
                Headers = new Dictionary<string, IEnumerable<string>>(_Headers);
            }

            Cookies = new CookieCollection();
            if (_Cookies != null)
            {
                Cookies.Add(_Cookies);
            }

            ResponseContent = _ResponseContent;
            ResponseContentType = _ResponseContentType;
        }

        public BWebServiceResponse(int _StatusCode, CookieCollection _Cookies, BStringOrStream _ResponseContent, EBResponseContentType _ResponseContentType)
        {
            StatusCode = _StatusCode;

            Headers = new Dictionary<string, IEnumerable<string>>();

            Cookies = new CookieCollection();
            if (_Cookies != null)
            {
                Cookies.Add(_Cookies);
            }

            ResponseContent = _ResponseContent;
            ResponseContentType = _ResponseContentType;
        }

        public BWebServiceResponse(int _StatusCode, BStringOrStream _ResponseContent, EBResponseContentType _ResponseContentType)
        {
            StatusCode = _StatusCode;

            Headers = new Dictionary<string, IEnumerable<string>>();

            Cookies = new CookieCollection();

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