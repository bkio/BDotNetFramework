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

        public static void InsertHeadersFromDictionaryInto(Dictionary<string, IEnumerable<string>> _Dictionary, Action<string, string> _CollectionAddFunction, string[] _ExcludeHeaderKeys = null)
        {
            if (_ExcludeHeaderKeys != null)
            {
                for (int i = 0; i < _ExcludeHeaderKeys.Length; i++)
                {
                    _ExcludeHeaderKeys[i] = _ExcludeHeaderKeys[i].ToLower();
                }
            }

            foreach (var RequestKeyVals in _Dictionary)
            {
                var LoweredKey = RequestKeyVals.Key.ToLower();
                if (!IllegalHttpRequestHeaders.Contains(LoweredKey))
                {
                    if (_ExcludeHeaderKeys != null && _ExcludeHeaderKeys.Contains(LoweredKey)) continue;

                    foreach (var Value in RequestKeyVals.Value)
                    {
                        _CollectionAddFunction?.Invoke(RequestKeyVals.Key, Value);
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

        public class InterServicesRequestRequest
        {
            public string DestinationServiceUrl;
            public string RequestMethod;
            public EBResponseContentType ContentType;
            public Dictionary<string, IEnumerable<string>> Headers = null;
            public BStringOrStream Content = null;
            public bool bWithAuthToken = true;
            public HttpListenerContext UseContextHeaders = null;
            public IEnumerable<string> ExcludeHeaderKeysForRequest = null;
        }
        public class InterServicesRequestResponse
        {
            public bool bSuccess = false;
            public int ResponseCode = BWebResponse.Error_InternalError_Code;
            public EBResponseContentType ContentType = BWebResponse.Error_InternalError_ContentType;
            public BStringOrStream Content = null;
            public Dictionary<string, IEnumerable<string>> ResponseHeaders = new Dictionary<string, IEnumerable<string>>();

            public static InterServicesRequestResponse InternalErrorOccured(string _Message)
            {
                return new InterServicesRequestResponse()
                {
                    Content = new BStringOrStream(_Message)
                };
            }
        }
        public static InterServicesRequestResponse InterServicesRequest(InterServicesRequestRequest _Request, Action<string> _ErrorMessageAction)
        {
            var bHttpRequestSuccess = false;
            var HttpRequestResponseCode = 500;
            var HttpRequestResponseContentType = EBResponseContentType.None;
            BStringOrStream HttpRequestResponseContent = null;
            Dictionary<string, IEnumerable<string>> HttpRequestResponseHeaders = null;

            var Request = (HttpWebRequest)WebRequest.Create(_Request.DestinationServiceUrl);
            Request.Method = _Request.RequestMethod;
            Request.ServerCertificateValidationCallback = (a, b, c, d) => true;
            Request.AllowAutoRedirect = false;

            if (_Request.bWithAuthToken)
            {
                //If context-headers already contain authorization; we must rename it to client-authorization to prevent override.
                if (_Request.UseContextHeaders != null 
                    && BWebUtilities.DoesContextContainHeader(out List<string> AuthorizationHeaderValues, out string CaseSensitive_FoundHeaderKey,_Request.UseContextHeaders, "authorization")
                    && BUtility.CheckAndGetFirstStringFromList(AuthorizationHeaderValues, out string ClientAuthorization))
                {
                    _Request.UseContextHeaders.Request.Headers.Remove(CaseSensitive_FoundHeaderKey);
                    _Request.UseContextHeaders.Request.Headers.Add("client-authorization", ClientAuthorization);
                }
                if (!AddAccessTokenForServiceExecution(Request, _Request.DestinationServiceUrl, _ErrorMessageAction))
                {
                    return InterServicesRequestResponse.InternalErrorOccured("Request has failed due to an internal api gateway error.");
                }
            }

            var ExcludeHeaderKeysForRequest = LowerContentOfStrings(_Request.ExcludeHeaderKeysForRequest);

            if (_Request.UseContextHeaders != null)
            {
                InsertHeadersFromContextInto(_Request.UseContextHeaders, (string _Key, string _Value) =>
                {
                    if (ExcludeHeaderKeysForRequest != null && ExcludeHeaderKeysForRequest.Contains(_Key.ToLower())) return;

                    Request.Headers.Add(_Key, _Value);
                });
            }
            if (_Request.Headers != null)
            {
                InsertHeadersFromDictionaryInto(_Request.Headers, (string _Key, string _Value) =>
                {
                    if (ExcludeHeaderKeysForRequest != null && ExcludeHeaderKeysForRequest.Contains(_Key.ToLower())) return;

                    Request.Headers.Add(_Key, _Value);
                });
            }

            try
            {
                if (_Request.RequestMethod != "GET" && _Request.RequestMethod != "DELETE"
                    && _Request.Content != null && ((_Request.Content.Type == EBStringOrStreamEnum.Stream && _Request.Content.Stream != null) || (_Request.Content.Type == EBStringOrStreamEnum.String && _Request.Content.String != null && _Request.Content.String.Length > 0)))
                {
                    Request.ContentType = GetMimeStringFromEnum_GC(_Request.ContentType);

                    using (var OStream = Request.GetRequestStream())
                    {
                        if (_Request.Content.Type == EBStringOrStreamEnum.Stream)
                        {
                            _Request.Content.Stream.CopyTo(OStream);
                        }
                        else
                        {
                            using (var RStream = new StreamWriter(OStream))
                            {
                                RStream.Write(_Request.Content.String);
                            }
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
                        _ErrorMessageAction?.Invoke("Error: InterServicesRequest: " + e.Message + ", Trace: " + e.StackTrace);
                        bHttpRequestSuccess = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("Error: InterServicesRequest: " + e.Message + ", Trace: " + e.StackTrace);
                bHttpRequestSuccess = false;
            }

            if (!bHttpRequestSuccess)
            {
                _ErrorMessageAction?.Invoke("Error: Request has failed due to an internal api gateway error. Service endpoint: " + _Request.DestinationServiceUrl);
                return InterServicesRequestResponse.InternalErrorOccured("Request has failed due to an internal api gateway error.");
            }

            if (_Request.UseContextHeaders != null)
            {
                InsertHeadersFromDictionaryIntoContext(HttpRequestResponseHeaders, _Request.UseContextHeaders);
            }

            return new InterServicesRequestResponse()
            {
                bSuccess = true,
                ResponseCode = HttpRequestResponseCode,
                ContentType = HttpRequestResponseContentType,
                ResponseHeaders = HttpRequestResponseHeaders,
                Content = HttpRequestResponseContent
            };
        }

        private static List<string> LowerContentOfStrings(IEnumerable<string> _Strings)
        {
            List<string> Lowered = null;
            if (_Strings != null)
            {
                Lowered = new List<string>(_Strings.Count());
                foreach (var ExcludeKey in _Strings)
                {
                    Lowered.Add(ExcludeKey.ToLower());
                }
            }
            return Lowered;
        }

        public static BWebServiceResponse RequestRedirection(
            HttpListenerContext _Context,
            string _FullEndpoint,
            Action<string> _ErrorMessageAction = null,
            bool _bWithAuthToken = true,
            IEnumerable<string> _ExcludeHeaderKeysForRequest = null)
        {
            using (var InputStream = _Context.Request.InputStream)
            {
                using (var RequestStream = InputStream)
                {
                    var Result = InterServicesRequest(new InterServicesRequestRequest()
                    {
                        DestinationServiceUrl = _FullEndpoint,
                        RequestMethod = _Context.Request.HttpMethod,
                        ContentType = GetEnumFromMimeString_GC(_Context.Request.ContentType),
                        Content = new BStringOrStream(RequestStream, _Context.Request.ContentLength64),
                        bWithAuthToken = _bWithAuthToken,
                        UseContextHeaders = _Context,
                        ExcludeHeaderKeysForRequest = _ExcludeHeaderKeysForRequest
                    }, _ErrorMessageAction);

                    return new BWebServiceResponse(
                        Result.ResponseCode,
                        Result.ResponseHeaders,
                        Result.Content,
                        Result.ContentType);
                }
            }
            
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

                BWebUtilities.InjectHeadersIntoDictionary(_Response.Headers, _HttpRequestResponseHeaders);

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
        public static string GetMimeStringFromEnum_GC(EBResponseContentType ContentType)
        {
            switch (ContentType)
            {
                case EBResponseContentType.ByteArray:
                    return "application/octet-stream";
                case EBResponseContentType.JSON:
                    return "application/json";
                case EBResponseContentType.JS:
                    return "application/javascript";
                case EBResponseContentType.CSS:
                    return "text/css";
                case EBResponseContentType.ZIP:
                    return "application/zip";
                case EBResponseContentType.PDF:
                    return "application/pdf";
                case EBResponseContentType.JPG:
                    return "image/jpeg";
                case EBResponseContentType.PNG:
                    return "image/png";
                case EBResponseContentType.GIF:
                    return "image/gif";
                default:
                    return "text/html";
            }
        }

        //Replicated method with BwebServiceUtilities
        //Change that too if there is any modification needed.
        public static EBResponseContentType GetEnumFromMimeString_GC(string _ContentType)
        {
            if (_ContentType == null || _ContentType.Length == 0) return EBResponseContentType.None;
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