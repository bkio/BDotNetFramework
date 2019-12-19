/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using BCommonUtilities;
using Flurl.Http;

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
            out string _ResultResponse,
            out EBResponseContentType _ResultContentType,
            string _Url,
            string _HttpMethod = "GET",
            Tuple<string, string>[] _Headers = null,
            BStringOrStream _Content = null,
            Action<string> _ErrorMessageAction = null)
        {
            _ResultCode = 500;
            _ResultHeaders = null;
            _ResultResponse = null;
            _ResultContentType = EBResponseContentType.None;

            var FlurlRequest = _Url.WithHeader("reserved", "common");
            System.Net.Http.HttpResponseMessage FlurlResponse = null;

            System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> ResponseTask = null;
            try
            {
                if (_Headers != null && _Headers.Length > 0)
                {
                    var Headers = new WebHeaderCollection();
                    foreach (var _Header in _Headers)
                    {
                        FlurlRequest = FlurlRequest.WithHeader(_Header.Item1, _Header.Item2);
                    }
                }

                if (_HttpMethod == "POST" || _HttpMethod == "PUT" || _HttpMethod == "PATCH")
                {
                    if (_Content.Type == EBStringOrStreamEnum.String)
                    {
                        switch (_HttpMethod)
                        {
                            case "POST":
                                ResponseTask = FlurlRequest.PostStringAsync(_Content.String);
                                break;
                            case "PUT":
                                ResponseTask = FlurlRequest.PutStringAsync(_Content.String);
                                break;
                            case "PATCH":
                                ResponseTask = FlurlRequest.PatchStringAsync(_Content.String);
                                break;
                        }
                    }
                    else
                    {
                        using (StreamReader Reader = new StreamReader(_Content.Stream))
                        {
                            switch (_HttpMethod)
                            {
                                case "POST":
                                    ResponseTask = FlurlRequest.PostStringAsync(Reader.ReadToEnd());
                                    break;
                                case "PUT":
                                    ResponseTask = FlurlRequest.PutStringAsync(Reader.ReadToEnd());
                                    break;
                                case "PATCH":
                                    ResponseTask = FlurlRequest.PatchStringAsync(Reader.ReadToEnd());
                                    break;
                            }
                        }
                    }
                }
                else if (_HttpMethod == "GET")
                {
                    ResponseTask = FlurlRequest.GetAsync();
                }
                else if (_HttpMethod == "DELETE")
                {
                    ResponseTask = FlurlRequest.DeleteAsync();
                }
                else if (_HttpMethod == "HEAD")
                {
                    ResponseTask = FlurlRequest.HeadAsync();
                }
                else if (_HttpMethod == "OPTION")
                {
                    ResponseTask = FlurlRequest.OptionsAsync();
                }
                else throw new Exception("Invalid method type " + _HttpMethod);

                ResponseTask.Wait();
                FlurlResponse = ResponseTask.Result;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BWebUtilities->PerformHTTPRequestSynchronously: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            finally
            {
                ResponseTask?.Dispose();
            }

            
            var ResponseHeaders = new List<Tuple<string, string>>();
            foreach (var Header in FlurlResponse.Headers)
            {
                string CombinedValues = "";
                foreach (var Value in Header.Value)
                {
                    CombinedValues += (Value + ",");
                }
                CombinedValues = CombinedValues.TrimEnd(',');

                ResponseHeaders.Add(new Tuple<string, string>(Header.Key, CombinedValues));
            }

            string ContentTypeResult = FlurlResponse.GetHeaderValue("Content-Type");

            _ResultCode = (int)FlurlResponse.StatusCode;
            _ResultHeaders = ResponseHeaders.ToArray();
            _ResultContentType = GetEnumFromMimeString(FlurlResponse.Content.Headers.ContentType.ToString());

            System.Threading.Tasks.Task<string> ReadContentTask = null;
            try
            {
                ReadContentTask = FlurlResponse.Content.ReadAsStringAsync();
                ReadContentTask.Wait();
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BWebUtilities->PerformHTTPRequestSynchronously: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            finally
            {
                _ResultResponse = ReadContentTask?.Result;
                ReadContentTask?.Dispose();
                FlurlResponse?.Dispose();
            }
            
            return true;
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