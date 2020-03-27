/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Net;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using BCommonUtilities;
using BCloudServiceUtilities;
using System.Linq;

namespace BWebServiceUtilities
{
    public class BWebService
    {
        private readonly BWebPrefixStructure[] PrefixesToListen;

        private readonly HttpListener Listener = new HttpListener();

        private readonly IBTracingServiceInterface TracingService;

        private readonly List<string> ServerNames = new List<string>()
        {
            "http://localhost",
            "http://127.0.0.1"
        };
        private readonly int ServerPort = 8080;

        public BWebService(BWebPrefixStructure[] _PrefixesToListen, int _ServerPort = 8080, IBTracingServiceInterface _TracingService = null, string _OverrideServerNames = null)
        {
            if (_PrefixesToListen == null || _PrefixesToListen.Length == 0)
                throw new ArgumentException("PrefixesToListen");

            if (_OverrideServerNames == null)
            {
                _OverrideServerNames = "http://*";
            }

            ServerPort = _ServerPort;

            TracingService = _TracingService;

            if (_OverrideServerNames != null && _OverrideServerNames.Length > 0)
            {
                string[] _OverrideServerNameArray = _OverrideServerNames.Split(';');
                if (_OverrideServerNameArray != null && _OverrideServerNameArray.Length > 0)
                {
                    ServerNames.Clear();
                    foreach (var _OverrideServerName in _OverrideServerNameArray)
                    {
                        ServerNames.Add(_OverrideServerName);
                    }
                }
            }

            foreach (string ServerName in ServerNames)
            {
                Listener.Prefixes.Add(ServerName + ":" + ServerPort + "/");
            }

            if (Listener.Prefixes.Count == 0)
                throw new ArgumentException("Invalid prefixes (Count 0)");

            PrefixesToListen = _PrefixesToListen;

            Listener.Start();
        }

        private bool LookForListenersFromRequest(out BWebServiceBase _Callback, HttpListenerContext _Context)
        {
            KeyValuePair<string, Func<BWebServiceBase>> ShortestMatch;
            int SortestLength = int.MaxValue;

            foreach (var CurrentPrefixes in PrefixesToListen)
            {
                if (CurrentPrefixes != null)
                {
                    if (CurrentPrefixes.GetCallbackFromRequest(out Func<BWebServiceBase> _CallbackInitializer, out string _MatchedPrefix, _Context))
                    {
                        if (_MatchedPrefix.Length < SortestLength)
                        {
                            SortestLength = _MatchedPrefix.Length;
                            ShortestMatch = new KeyValuePair<string, Func<BWebServiceBase>>(_MatchedPrefix, _CallbackInitializer);
                        }
                    }
                }
            }

            if (SortestLength < int.MaxValue)
            {
                _Callback = ShortestMatch.Value.Invoke();
                _Callback.InitializeWebService(_Context, ShortestMatch.Key, TracingService);
                return true;
            }

            _Callback = null;
            return false;
        }

        public void Run(Action<string> _ServerLogAction = null)
        {
            BTaskWrapper.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                _ServerLogAction?.Invoke("BWebserver->Run: Server is running.");
                try
                {
                    while (Listener.IsListening)
                    {
                        var Context = Listener.GetContext();

                        BTaskWrapper.Run(() =>
                        {
                            Thread.CurrentThread.IsBackground = true;

                            try
                            {
                                Context.Response.AddHeader("Access-Control-Allow-Origin", "*");

                                if (Context.Request.HttpMethod == "OPTIONS")
                                {
                                    Context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With, api-key, token, auto-close-response");
                                    Context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
                                    Context.Response.AddHeader("Access-Control-Max-Age", "-1");
                                    Context.Response.StatusCode = BWebResponse.Status_OK_Code;
                                }
                                else
                                {
                                    if (PrefixesToListen == null)
                                    {
                                        _ServerLogAction?.Invoke("BWebserver->Run: PrefixesToListen is null.");
                                        WriteInternalError(Context.Response, "Code: WS-PTLN.");
                                        return;
                                    }
                                    if (!LookForListenersFromRequest(out BWebServiceBase _Callback, Context))
                                    {
                                        _ServerLogAction?.Invoke("BWebserver->Run: Request is not being listened. Request: " + Context.Request.RawUrl);
                                        WriteNotFound(Context.Response, "Request is not being listened.");
                                        return;
                                    }

                                    var Response = _Callback.OnRequest(Context, _ServerLogAction);

                                    Context.Response.StatusCode = Response.StatusCode;

                                    foreach (var CurrentHeader in Response.Headers)
                                    {
                                        foreach (var Value in CurrentHeader.Value)
                                        {
                                            Context.Response.AppendHeader(CurrentHeader.Key, Value);
                                        }
                                    }
                                    
                                    Context.Response.ContentType = BWebUtilities.GetMimeStringFromEnum(Response.ResponseContentType);

                                    if (Response.ResponseContent.Type == EBStringOrStreamEnum.String)
                                    {
                                        byte[] Buffer = Encoding.UTF8.GetBytes(Response.ResponseContent.String);
                                        if (Buffer != null)
                                        {
                                            Context.Response.ContentLength64 = Buffer.Length;
                                            if (Buffer.Length > 0)
                                            {
                                                Context.Response.OutputStream.Write(Buffer, 0, Buffer.Length);
                                            }
                                        }
                                        else
                                        {
                                            Context.Response.ContentLength64 = 0;
                                        }
                                    }
                                    else
                                    {
                                        if (Response.ResponseContent.Stream != null && Response.ResponseContent.StreamLength > 0)
                                        {
                                            Context.Response.ContentLength64 = Response.ResponseContent.StreamLength;
                                            Response.ResponseContent.Stream.CopyTo(Context.Response.OutputStream);
                                        }
                                        else
                                        {
                                            _ServerLogAction?.Invoke("BWebserver->Error: Response is stream, but stream object is " + (Response.ResponseContent.Stream == null ? "null" : "valid") + " and content length is " + Response.ResponseContent.StreamLength);
                                            WriteInternalError(Context.Response, "Code: WS-STRMINV.");
                                            return;
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                _ServerLogAction?.Invoke("Uncaught exception-2: " + e.Message + ", trace: " + e.StackTrace);
                            }
                            finally
                            {
                                //Always close the stream
                                try { Context.Response.OutputStream.Close(); } catch (Exception) { }
                                try { Context.Response.OutputStream.Dispose(); } catch (Exception) { }
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    _ServerLogAction?.Invoke("Uncaught exception-1: " + e.Message + ", trace: " + e.StackTrace);
                }
            });
        }
        
        private static void WriteInternalError(HttpListenerResponse _WriteTo, string _CustomMessage)
        {
            string Resp = BWebResponse.Error_InternalError_String(_CustomMessage);
            byte[] Buff = Encoding.UTF8.GetBytes(Resp);

            _WriteTo.ContentType = BWebUtilities.GetMimeStringFromEnum(BWebResponse.Error_InternalError_ContentType);
            _WriteTo.StatusCode = BWebResponse.Error_InternalError_Code;
            _WriteTo.ContentLength64 = Buff.Length;
            _WriteTo.OutputStream.Write(Buff, 0, Buff.Length);
        }
        private static void WriteNotFound(HttpListenerResponse _WriteTo, string _CustomMessage)
        {
            string Resp = BWebResponse.Error_NotFound_String(_CustomMessage);
            byte[] Buff = Encoding.UTF8.GetBytes(Resp);

            _WriteTo.ContentType = BWebUtilities.GetMimeStringFromEnum(BWebResponse.Error_NotFound_ContentType);
            _WriteTo.StatusCode = BWebResponse.Error_NotFound_Code;
            _WriteTo.ContentLength64 = Buff.Length;
            _WriteTo.OutputStream.Write(Buff, 0, Buff.Length);
        }

        public void Stop()
        {
            if (Listener != null)
            {
                try { Listener.Stop(); } catch (Exception) { }
                try { Listener.Close(); } catch (Exception) { }
            }
        }
    }
}