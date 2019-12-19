/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using BCommonUtilities;

namespace BWebServiceUtilities
{
    public enum EBResponseContentType
    {
        None,
        TextHtml,
        ByteArray,
        JSON,
        ZIP,
        PDF,
        JPG,
        PNG,
        GIF,
        JS,
        CSS
    }

    public static class BWebUtilities
    {
        public static string GetMimeStringFromEnum(EBResponseContentType ContentType)
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

        private static EBResponseContentType GetEnumFromMimeString(string _ContentType)
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

        /// <summary>
        /// Note: Do not forget to dispose HttpWebResponse(_ResultResponse) and inner streams
        /// </summary>
        public static bool PerformHTTPRequestSynchronously(
            out int _ResultCode,
            out Tuple<string, string>[] _ResultHeaders,
            out Tuple<string, string, string>[] _ResultCookies,
            out HttpWebResponse _ResultResponse,
            out EBResponseContentType _ResultContentType,
            string _Url,
            string _HttpMethod = "GET",
            Tuple<string, string>[] _Headers = null,
            Tuple<string, string, string>[] _Cookies = null,
            BStringOrStream _Content = null,
            long _ContentLength = 0,
            string _ContentType = null,
            Action<string> _ErrorMessageAction = null)
        {
            _ResultCode = 500;
            _ResultHeaders = null;
            _ResultCookies = null;
            _ResultResponse = null;
            _ResultContentType = EBResponseContentType.None;

            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(_Url);
            Request.Method = _HttpMethod;

            try
            {
                if (_Headers != null && _Headers.Length > 0)
                {
                    var Headers = new WebHeaderCollection();
                    foreach (var _Header in _Headers)
                    {
                        Headers.Add(_Header.Item1, _Header.Item2);
                    }
                    Request.Headers = Headers;
                }

                if (_Cookies != null && _Cookies.Length > 0)
                {
                    var Cookies = new CookieContainer();
                    foreach (var _Cookie in _Cookies)
                    {
                        Cookies.Add(new Cookie(_Cookie.Item1, _Cookie.Item2) { Domain = _Cookie.Item3 });
                    }
                    Request.CookieContainer = Cookies;
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BWebUtilities->PerformHTTPRequestSynchronously: " + e.Message + ", Trace: " + e.StackTrace);
            }

            if (_Content != null && (_HttpMethod == "POST" || _HttpMethod == "PUT" || _HttpMethod == "DELETE"))
            {
                if (_ContentType != null)
                {
                    Request.ContentType = _ContentType;
                }
                if (_ContentLength > 0)
                {
                    Request.ContentLength = _ContentLength;
                }

                if (_Content.Type == EBStringOrStreamEnum.String)
                {
                    try
                    {
                        byte[] ContentAsBytes = Encoding.UTF8.GetBytes(_Content.String);

                        using (Stream RequestBody = Request.GetRequestStream())
                        {
                            RequestBody.Write(ContentAsBytes, 0, ContentAsBytes.Length);
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BWebUtilities->PerformHTTPRequestSynchronously: Request body write failed: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }
                }
                else
                {
                    using (Stream RequestBody = Request.GetRequestStream())
                    {
                        _Content.Stream.CopyTo(RequestBody);
                    }
                }
            }

            try
            {
                var Response = (HttpWebResponse)Request.GetResponse();

                Tuple<string, string>[] ResponseHeaders = null;
                if (Response.Headers != null && Response.Headers.Count > 0)
                {
                    ResponseHeaders = new Tuple<string, string>[Response.Headers.Count];
                    for (int j = 0; j < Response.Headers.Count; j++)
                    {
                        string CombinedValues = "";
                        var Values = Response.Headers.GetValues(Response.Headers.Keys[j]);
                        if (Values != null && Values.Length > 0)
                        {
                            foreach (var Value in Values)
                            {
                                CombinedValues += (Value + ",");
                            }
                            CombinedValues = CombinedValues.TrimEnd(',');
                        }
                        ResponseHeaders[j] = new Tuple<string, string>(Response.Headers.Keys[j], CombinedValues);
                    }
                }
                Tuple<string, string, string>[] ResponseCookies = null;
                if (Response.Cookies != null && Response.Cookies.Count > 0)
                {
                    ResponseCookies = new Tuple<string, string, string>[Response.Cookies.Count];
                    for (int j = 0; j < Response.Cookies.Count; j++)
                    {
                        var CurrentCookie = Response.Cookies[j];
                        if (CurrentCookie != null)
                        {
                            ResponseCookies[j] = new Tuple<string, string, string>(CurrentCookie.Name, CurrentCookie.Value, CurrentCookie.Domain);
                        }
                    }
                }

                _ResultCode = (int)Response.StatusCode;
                _ResultHeaders = ResponseHeaders;
                _ResultCookies = ResponseCookies;
                _ResultContentType = GetEnumFromMimeString(Response.ContentType);
                _ResultResponse = Response;
                return true;
            }
            catch (WebException e)
            {
                var Response = (HttpWebResponse)e.Response;
                if (Response != null)
                {
                    _ResultCode = (int)Response.StatusCode;
                    _ResultHeaders = null;
                    _ResultCookies = null;
                    _ResultContentType = GetEnumFromMimeString(Response.ContentType);
                    _ResultResponse = Response;
                    return true;
                }
                _ErrorMessageAction?.Invoke("BWebUtilities->PerformHTTPRequestSynchrounously: Response is null after WebException. Message: " + e.Message);
                return false;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BWebUtilities->PerformHTTPRequestSynchronously: Response read failed: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
        }

        public static void InjectHeadersFromTupleArraysIntoContext(Tuple<string, string>[] _Headers, HttpListenerContext _Context)
        {
            if (_Headers != null && _Headers.Length > 0)
            {
                foreach (var Header in _Headers)
                {
                    if (Header != null)
                    {
                        _Context.Request.Headers.Set(Header.Item1, Header.Item2);
                    }
                }
            }
        }

        public static void ExtractHeadersCookiesAsTupleArraysFromContext(out Tuple<string, string>[] _Headers, out Tuple<string, string, string>[] _Cookies, HttpListenerContext _Context)
        {
            var TempHeaderList = new List<Tuple<string, string>>();
            if (_Context.Request.Headers != null && _Context.Request.Headers.AllKeys != null)
            {
                foreach (var HeaderKey in _Context.Request.Headers.AllKeys)
                {
                    if (HeaderKey != null)
                    {
                        var HeaderValues = _Context.Request.Headers.GetValues(HeaderKey);
                        if (HeaderValues != null)
                        {
                            foreach (var HeaderValue in HeaderValues)
                            {
                                if (HeaderValue != null)
                                {
                                    TempHeaderList.Add(new Tuple<string, string>(HeaderKey, HeaderValue));
                                }
                            }
                        }
                    }
                }
            }
            if (TempHeaderList.Count == 0)
            {
                _Headers = null;
            }
            else
            {
                _Headers = TempHeaderList.ToArray();
            }

            var TempCookieList = new List<Tuple<string, string, string>>();
            if (_Context.Request.Cookies != null)
            {
                for (int i = 0; i < _Context.Request.Cookies.Count; i++)
                {
                    var CurrentCookie = _Context.Request.Cookies[i];
                    if (CurrentCookie != null)
                    {
                        TempCookieList.Add(new Tuple<string, string, string>(CurrentCookie.Name, CurrentCookie.Value, CurrentCookie.Domain));
                    }
                }
            }
            if (TempCookieList.Count == 0)
            {
                _Cookies = null;
            }
            else
            {
                _Cookies = TempCookieList.ToArray();
            }
        }

        public static string ReplaceHostPart(string Source, string NewHostname)
        {
            if (Source.StartsWith("http://"))
            {
                Source = Source.Substring("http://".Length);
            }
            else if (Source.StartsWith("https://"))
            {
                Source = Source.Substring("https://".Length);
            }

            int FirstSlash = Source.IndexOf('/');
            if (FirstSlash < 0)
            {
                return NewHostname;
            }
            Source = Source.Substring(FirstSlash);

            return NewHostname + Source;
        }

        public static Tuple<string, string>[] AnalyzeURLParametersFromRawURL(string _RawURL)
        {
            if (_RawURL == null) return null;

            _RawURL = _RawURL.TrimStart('/');
            if (_RawURL.Length == 0) return null;

            int FirstQMIndex = _RawURL.IndexOf('?');
            if (FirstQMIndex == -1) return null;

            _RawURL = _RawURL.Substring(FirstQMIndex + 1);

            List<Tuple<string, string>> Parameters = new List<Tuple<string, string>>();

            string[] Splitted = _RawURL.Split('&');
            if (Splitted != null && Splitted.Length > 0)
            {
                foreach (string Parameter in Splitted)
                {
                    string[] SplittedKeyValue = Parameter.Split('=');
                    if (SplittedKeyValue != null && SplittedKeyValue.Length == 2)
                    {
                        Parameters.Add(new Tuple<string, string>(SplittedKeyValue[0], SplittedKeyValue[1]));
                    }
                }
            }

            if (Parameters.Count == 0) return null;
            return Parameters.ToArray();
        }

        public static bool GetEndpointListFromDirectoryTreeNode(out List<Tuple<string, string>> _EndpointList, BDirectoryTreeNode _ParentNode, Action<string> _ErrorMessageAction = null)
        {
            _EndpointList = new List<Tuple<string, string>>();

            if (!ConvertDirectoryTreeNodeToPath(_EndpointList, _ParentNode, _ErrorMessageAction))
            {
                _EndpointList = null;
                return false;
            }

            //index.html fix iteration
            var InitialEndpointListSize = _EndpointList.Count;
            for (int i = 0; i < InitialEndpointListSize; i++)
            {
                var Lowered = _EndpointList[i].Item1.ToLower();
                if (Lowered.EndsWith("/index.html") || Lowered.EndsWith("/index.htm"))
                {
                    var WithoutIndexHtml = _EndpointList[i].Item1.Substring(0, _EndpointList[i].Item1.LastIndexOf('/'));

                    _EndpointList.Add(new Tuple<string, string>(WithoutIndexHtml + "/", _EndpointList[i].Item2));
                }
            }
            return true;
        }
        private static bool ConvertDirectoryTreeNodeToPath(List<Tuple<string, string>> _EndpointList, BDirectoryTreeNode _CurrentNode, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                if (_CurrentNode.GetNodeType() == EBDirectoryTreeNodeType.File)
                {
                    string Path = _CurrentNode.GetName();

                    var CurrentParent = _CurrentNode.GetParent();
                    while (CurrentParent != null && CurrentParent.GetParent() != null /*Root Directory*/)
                    {
                        Path = CurrentParent.GetName() + "/" + Path;
                        CurrentParent = CurrentParent.GetParent();
                    }

                    Path = "/" + Path;

                    _EndpointList.Add(new Tuple<string, string>(Path, CurrentParent != null ? ("/" + CurrentParent.GetName() + Path) : Path));
                }
                else
                {
                    if (_CurrentNode.GetChildren() != null)
                    {
                        foreach (var ChildNode in _CurrentNode.GetChildren())
                        {
                            if (!ConvertDirectoryTreeNodeToPath(_EndpointList, ChildNode, _ErrorMessageAction))
                            {
                                return false;
                            }
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BWebUtilities->ConvertDirectoryTreeNodeToPath has failed with " + e.Message + ", trace: " + e.StackTrace);
                return false;
            }
            return true;
        }
    }
}