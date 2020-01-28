/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using BCommonUtilities;
using BWebServiceUtilities;

namespace BWebServiceUtilities_GC
{
    public static class BWebUtilities_GC_CloudRun
    {
        private static readonly HashSet<string> IllegalHttpRequestHeaders = new HashSet<string>()
        {
            "upgrade-insecure-requests", "sec-fetch-user", "sec-fetch-site", "sec-fetch-mode",
            "cache-control", "connection",
            "accept", "accept-encoding", "accept-language", "host", "user-agent",
            "x-forwarded-for", "x-forwarded-proto", "x-cloud-trace-context", "forwarded",
            "content-length", "content-type"
        };

        public static void InsertHeadersFromContextInto(HttpListenerContext _Context, Action<string, string> _CollectionAddFunction, string[] _ExcludeHeaderKeys = null)
        {
            if (_ExcludeHeaderKeys != null)
            {
                for (int i = 0; i < _ExcludeHeaderKeys.Length; i++)
                {
                    _ExcludeHeaderKeys[i] = _ExcludeHeaderKeys[i].ToLower();
                }
            }

            foreach (var RequestKey in _Context.Request.Headers.AllKeys)
            {
                var LoweredKey = RequestKey.ToLower();
                if (!IllegalHttpRequestHeaders.Contains(LoweredKey))
                {
                    if (_ExcludeHeaderKeys != null && _ExcludeHeaderKeys.Contains(LoweredKey)) continue;

                    var Values = _Context.Request.Headers.GetValues(RequestKey);
                    foreach (var Value in Values)
                    {
                        _CollectionAddFunction?.Invoke(RequestKey, Value);
                    }
                }
            }
        }

        public static void InsertHeadersFromDictionaryIntoContext(Dictionary<string, IEnumerable<string>> _HttpRequestResponseHeaders, HttpListenerContext Context)
        {
            foreach (var Header in _HttpRequestResponseHeaders)
            {
                if (!IllegalHttpRequestHeaders.Contains(Header.Key.ToLower()))
                {
                    foreach (var Value in Header.Value)
                    {
                        Context.Request.Headers.Add(Header.Key, Value);
                    }
                }
            }
        }

        public static bool GetAccessTokenForServiceExecution(out string _TokenType, out string _AccessKey, string _ForExecutingRequestUrl, Action<string> _ErrorMessageAction)
        {
            _TokenType = null;
            _AccessKey = null;

            var FullEndpoint = "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/identity?audience=" + _ForExecutingRequestUrl;

            using (var Handler = new HttpClientHandler())
            {
                Handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
                Handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;

                using (var Client = new HttpClient(Handler))
                {
                    Client.DefaultRequestHeaders.TryAddWithoutValidation("Metadata-Flavor", "Google");

                    try
                    {
                        using (var RequestTask = Client.GetAsync(FullEndpoint))
                        {
                            RequestTask.Wait();

                            using (var Response = RequestTask.Result)
                            {
                                using (var Content = Response.Content)
                                {
                                    using (var ReadResponseTask = Content.ReadAsStringAsync())
                                    {
                                        ReadResponseTask.Wait();

                                        if (Response.StatusCode != HttpStatusCode.OK)
                                        {
                                            throw new Exception("Request returned: " + Response.StatusCode + ", with content: " + ReadResponseTask.Result);
                                        }

                                        _TokenType = "Bearer";
                                        _AccessKey = ReadResponseTask.Result;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("Error in GetAccessTokenForServiceExecution: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }
                    return true;
                }
            }
        }

        public static bool AddAccessTokenForServiceExecution(HttpWebRequest _Request, string _ForExecutingRequestUrl, Action<string> _ErrorMessageAction)
        {
            if (!GetAccessTokenForServiceExecution(out string TokenType, out string AccessKey, _ForExecutingRequestUrl, _ErrorMessageAction))
            {
                return false;
            }
            _Request.Headers.Set("authorization", TokenType + " " + AccessKey);
            return true;
        }

        public static bool AddAccessTokenForServiceExecution(HttpClient _Client, string _ForExecutingRequestUrl, Action<string> _ErrorMessageAction)
        {
            if (!GetAccessTokenForServiceExecution(out string TokenType, out string AccessKey, _ForExecutingRequestUrl, _ErrorMessageAction))
            {
                return false;
            }
            _Client.DefaultRequestHeaders.TryAddWithoutValidation("authorization", TokenType + " " + AccessKey);
            return true;
        }

        public static BWebServiceResponse RequestRedirection(
            HttpListenerContext _Context,
            string _FullEndpoint,
            bool _bWithAuthToken,
            Action<string> _ErrorMessageAction)
        {
            var bHttpRequestSuccess = false;
            var HttpRequestResponseCode = 200;
            var HttpRequestResponseContentType = EBResponseContentType.None;
            BStringOrStream HttpRequestResponseContent = null;
            Dictionary<string, IEnumerable<string>> HttpRequestResponseHeaders = null;

            var Request = (HttpWebRequest)WebRequest.Create(_FullEndpoint);
            Request.Method = _Context.Request.HttpMethod;
            Request.ServerCertificateValidationCallback = (a, b, c, d) => true;
            Request.AllowAutoRedirect = false;

            if (_bWithAuthToken)
            {
                if (!AddAccessTokenForServiceExecution(Request, _FullEndpoint, _ErrorMessageAction))
                {
                    return BWebResponse.InternalError("Request has failed due to an internal api gateway error.");
                }
            }
            InsertHeadersFromContextInto(_Context, Request.Headers.Add);

            try
            {
                if (_Context.Request.HttpMethod != "GET" && _Context.Request.HttpMethod != "DELETE")
                {
                    Request.ContentType = _Context.Request.ContentType;

                    using (var OStream = Request.GetRequestStream())
                    {
                        using (var RStream = _Context.Request.InputStream)
                        {
                            RStream.CopyTo(OStream);
                        }
                    }
                }

                try
                {
                    using (var Response = (HttpWebResponse)Request.GetResponse())
                    {
                        AnalyzeResponse(Response, out bHttpRequestSuccess, out HttpRequestResponseCode, out HttpRequestResponseContentType, out HttpRequestResponseContent, out HttpRequestResponseHeaders, _ErrorMessageAction);
                    }
                }
                catch (Exception e)
                {
                    if (e is WebException)
                    {
                        using (var ErrorResponse = (HttpWebResponse)(e as WebException).Response)
                        {
                            AnalyzeResponse(ErrorResponse, out bHttpRequestSuccess, out HttpRequestResponseCode, out HttpRequestResponseContentType, out HttpRequestResponseContent, out HttpRequestResponseHeaders, _ErrorMessageAction);
                        }
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("Error: RequestRedirection: " + e.Message + ", Trace: " + e.StackTrace);
                        bHttpRequestSuccess = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("Error: RequestRedirection: " + e.Message + ", Trace: " + e.StackTrace);
                bHttpRequestSuccess = false;
            }

            if (!bHttpRequestSuccess)
            {
                _ErrorMessageAction?.Invoke("Error: Request has failed due to an internal api gateway error. Service endpoint: " + _FullEndpoint);
                return BWebResponse.InternalError("Request has failed due to an internal api gateway error.");
            }

            InsertHeadersFromDictionaryIntoContext(HttpRequestResponseHeaders, _Context);

            return new BWebServiceResponse(
                HttpRequestResponseCode,
                HttpRequestResponseHeaders,
                HttpRequestResponseContent,
                HttpRequestResponseContentType);
        }

        private static void AnalyzeResponse(
            HttpWebResponse _Response,
            out bool _bHttpRequestSuccess,
            out int _HttpRequestResponseCode,
            out EBResponseContentType _HttpRequestResponseContentType,
            out BStringOrStream _HttpRequestResponseContent,
            out Dictionary<string, IEnumerable<string>> _HttpRequestResponseHeaders,
            Action<string> _ErrorMessageAction)
        {
            _bHttpRequestSuccess = false;
            _HttpRequestResponseCode = 200;
            _HttpRequestResponseContentType = EBResponseContentType.None;
            _HttpRequestResponseContent = null;
            _HttpRequestResponseHeaders = new Dictionary<string, IEnumerable<string>>();

            try
            {
                _HttpRequestResponseCode = (int)_Response.StatusCode;

                foreach (var RHeader in _Response.Headers.AllKeys)
                {
                    if (!_HttpRequestResponseHeaders.ContainsKey(RHeader))
                    {
                        _HttpRequestResponseHeaders.Add(RHeader, new List<string>());
                    }
                    ((List<string>)(_HttpRequestResponseHeaders[RHeader])).AddRange(_Response.Headers.GetValues(RHeader));
                }

                _HttpRequestResponseContentType = GetEnumFromMimeString_GC(_Response.ContentType);

                using (var ResStream = _Response.GetResponseStream())
                {
                    using (var Reader = new StreamReader(ResStream))
                    {
                        _HttpRequestResponseContent = new BStringOrStream(Reader.ReadToEnd());
                    }
                }

                _bHttpRequestSuccess = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("Error: RequestRedirection-AnalyzeResponse: " + e.Message + ", Trace: " + e.StackTrace);
                _bHttpRequestSuccess = false;
            }
        }

        //Replicated method with BwebServiceUtilities
        //Change that too if there is any modification needed.
        public static EBResponseContentType GetEnumFromMimeString_GC(string _ContentType)
        {
            _ContentType = _ContentType.ToLower();
            switch (_ContentType)
            {
                case "application/octet-stream":
                    return EBResponseContentType.ByteArray;
                case "application/json":
                    return EBResponseContentType.JSON;
                case "application/zip":
                    return EBResponseContentType.ZIP;
                case "application/pdf":
                    return EBResponseContentType.PDF;
                case "image/jpeg":
                    return EBResponseContentType.JPG;
                case "image/png":
                    return EBResponseContentType.PNG;
                case "image/gif":
                    return EBResponseContentType.GIF;
                case "application/javascript":
                    return EBResponseContentType.JS;
                case "text/css":
                    return EBResponseContentType.CSS;
                default:
                    return EBResponseContentType.TextHtml;
            }
        }
    }
}