/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        public static EBResponseContentType GetEnumFromMimeString(string _ContentType)
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

        public static bool DoesContextContainHeader(out string HeaderValues, HttpListenerContext Context, string HeaderKey)
        {
            HeaderKey = HeaderKey.ToLower();
            HeaderValues = "";

            foreach (var RequestKey in Context.Request.Headers.AllKeys)
            {
                string Key = RequestKey.ToLower();
                if (Key == HeaderKey)
                {
                    foreach (var HeaderValue in Context.Request.Headers.GetValues(RequestKey))
                    {
                        HeaderValues += HeaderValue + ",";
                    }
                    HeaderValues = HeaderValues.TrimEnd(',');

                    return true;
                }
            }
            return false;
        }

        public static void ConvertHeadersFromHttpContentToTuples(HttpContentHeaders ContentHeaders, List<Tuple<string, string>> HttpRequestResponseHeaders)
        {
            foreach (var Header in ContentHeaders)
            {
                var HeadersString = "";
                foreach (var HeaderValue in Header.Value)
                {
                    HeadersString += HeaderValue + ",";
                }
                HeadersString = HeadersString.TrimEnd(',');

                HttpRequestResponseHeaders.Add(new Tuple<string, string>(Header.Key, HeadersString));
            }
        }
        public static void ConvertHeadersFromHttpContentToTuples(HttpResponseHeaders ResponseHeaders, List<Tuple<string, string>> HttpRequestResponseHeaders)
        {
            foreach (var Header in ResponseHeaders)
            {
                var HeadersString = "";
                foreach (var HeaderValue in Header.Value)
                {
                    HeadersString += HeaderValue + ",";
                }
                HeadersString = HeadersString.TrimEnd(',');

                HttpRequestResponseHeaders.Add(new Tuple<string, string>(Header.Key, HeadersString));
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