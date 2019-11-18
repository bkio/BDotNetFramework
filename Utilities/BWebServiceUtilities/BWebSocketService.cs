/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BCloudServiceUtilities;
using Fleck;

namespace BWebServiceUtilities
{
    /// <summary>
    /// Send message action should not be called in OnConnected call, parameter is passed for it's reference
    /// </summary>
    public interface IBWebSocketClient
    {
        void OnListenerConnected(string _Endpoint, string _UniqueConnectionID, Action<string> _SendMessageAction);
        void OnListenerMessage(string _Endpoint, string _UniqueConnectionID, string _Message, Action<string> _SendMessageAction);
        void OnListenerDisconnected(string _Endpoint, string _UniqueConnectionID);
    }

    public class BWebSocketService
    {
        private readonly List<Tuple<string, Func<IBWebSocketClient>>> Endpoints = new List<Tuple<string, Func<IBWebSocketClient>>>();

        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        private readonly IBMemoryServiceInterface MemoryService;

        /// <summary>
        /// <para>An endpoint's parameters:</para>
        /// <para>1: Endpoint that starts with "/" like /example</para>
        /// <para>2: OnConnected callback with endpoint with / at the beginning, unique connection ID and send message action. Send message action should not be called in OnConnected call.</para>
        /// <para>3: OnMessage callback with endpoint with / at the beginning, unique connection ID, message and send message action</para>
        /// <para>4: OnDisconnected callback endpoint with / at the beginning, with unique connection ID</para>
        /// </summary>
        public BWebSocketService(
            int _WSSServerPort, 
            string _WSSCertificatePath, 
            string _WSSCertificatePasswordPath,
            Tuple<string, Func<IBWebSocketClient>>[] _Endpoints,
            IBMemoryServiceInterface _MemoryService,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                MemoryService = _MemoryService;

                WSSServerPort = _WSSServerPort;
                WSSCertificatePath = _WSSCertificatePath;
                WSSCertificatePassword = File.ReadAllText(_WSSCertificatePasswordPath).TrimEnd('\n').TrimEnd('\r');

                if (_Endpoints != null && _Endpoints.Length > 0)
                {
                    foreach (var _Endpoint in _Endpoints)
                    {
                        Endpoints.Add(_Endpoint);
                    }
                }
                else
                {
                    bInitializationSucceed = false;
                    return;
                }

                X509Cert = new X509Certificate2(WSSCertificatePath, WSSCertificatePassword);

                WSSServer = new WebSocketServer("wss://0.0.0.0:" + WSSServerPort);
                WSSServer.ListenerSocket.NoDelay = true;
                WSSServer.RestartAfterListenError = true;
                WSSServer.Certificate = X509Cert;
                FleckLog.LogAction = (Level, Message, Ex) => 
                {
                    switch (Level)
                    {
                        case LogLevel.Debug:
                            break;
                        case LogLevel.Error:
                            _ErrorMessageAction?.Invoke("WebSocket->Error: " + Message + ", Exception: " + Ex?.Message + ", Trace: " + Ex?.StackTrace);
                            break;
                        case LogLevel.Warn:
                            _ErrorMessageAction?.Invoke("WebSocket->Warning: " + Message + ", Exception: " + Ex?.Message + ", Trace: " + Ex?.StackTrace);
                            break;
                        default:
                            break;
                    }
                };
                
                WSSServer.Start(Socket =>
                {
                    Socket.OnOpen = () =>
                    {
                        if (Socket != null && !WSSConnections.ContainsKey(Socket))
                        {
                            if (Socket.IsAvailable && Socket.ConnectionInfo != null)
                            {
                                bool bFound = false;
                                foreach (var Endpoint in Endpoints)
                                {
                                    var IndexOfFirstQuestionMark = Socket.ConnectionInfo.Path.IndexOf('?');
                                    string PathWithoutParameters = IndexOfFirstQuestionMark >= 0 ? Socket.ConnectionInfo.Path.Substring(0, IndexOfFirstQuestionMark) : Socket.ConnectionInfo.Path;

                                    if (Endpoint.Item1 == PathWithoutParameters)
                                    {
                                        if (GetUniqueConnectionID(out long NewUniqueConnectionID, _ErrorMessageAction))
                                        {
                                            if (WSSConnections.TryAdd(Socket, new BWSSClient(Endpoint.Item1, NewUniqueConnectionID.ToString(), Endpoint.Item2, Socket.Send)))
                                            {
                                                bFound = true;
                                            }
                                        }
                                        break;
                                    }
                                }
                                if (!bFound)
                                {
                                    try
                                    {
                                        Socket.Close();
                                    }
                                    catch (Exception) { }
                                }
                            }
                            else
                            {
                                try
                                {
                                    Socket.Close();
                                }
                                catch (Exception) {}
                            }
                        }
                    };
                    Socket.OnClose = () =>
                    {
                        if (Socket != null)
                        {
                            if (WSSConnections.TryRemove(Socket, out BWSSClient OutClient))
                            {
                                OutClient?.OnClose();
                            }
                        }
                    };
                    Socket.OnMessage = (string Message) =>
                    {
                        if (Socket != null)
                        {
                            if (Socket.IsAvailable)
                            {
                                if (WSSConnections.TryGetValue(Socket, out BWSSClient OutClient))
                                {
                                    OutClient?.OnMessage(Message);
                                }
                            }
                            else
                            {
                                if (WSSConnections.TryRemove(Socket, out BWSSClient OutClient))
                                {
                                    OutClient?.OnClose();
                                }
                                try
                                {
                                    Socket.Close();
                                }
                                catch (Exception) { }
                            }
                        }
                    };
                });

                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BWebSocketService->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        ~BWebSocketService()
        {
            foreach (var Pair in WSSConnections)
            {
                Pair.Value?.OnClose();
                try
                {
                    Pair.Key?.Close();
                }
                catch (Exception) {}
            }
            WSSConnections.Clear();

            WSSServer?.Dispose();
            X509Cert?.Dispose();
        }

        private readonly X509Certificate2 X509Cert = null;

        private readonly WebSocketServer WSSServer = null;

        private readonly ConcurrentDictionary<IWebSocketConnection, BWSSClient> WSSConnections = new ConcurrentDictionary<IWebSocketConnection, BWSSClient>();

        private readonly int WSSServerPort;
        private readonly string WSSCertificatePath;
        private readonly string WSSCertificatePassword;

        private static readonly BMemoryQueryParameters MemoryParameters = new BMemoryQueryParameters()
        {
            Domain = "WebSocketService",
            SubDomain = "Connections",
            Identifier = "Global"
        };

        private bool GetUniqueConnectionID(out long _NewUniqueConnectionID, Action<string> _ErrorMessageAction = null)
        {
            bool bSuccess = true;

            _NewUniqueConnectionID = MemoryService.IncrementKeyByValueAndGet(MemoryParameters, new Tuple<string, long>("UniqueConnectionID", 1), 
                (string Message) =>
                {
                    bSuccess = false;
                    _ErrorMessageAction?.Invoke(Message);
                });

            return bSuccess;
        }

        private class BWSSClient
        {
            private readonly string UniqueConnectionID;

            private readonly string Endpoint;

            private readonly IBWebSocketClient WebSocketClient = null;

            private readonly Func<string, Task> SendAction = null;
            private void Send(string Message)
            {
                if (SendAction != null)
                {
                    using (var CreatedTask = SendAction.Invoke(Message))
                    {
                        CreatedTask.Wait();
                    }
                }
            }

            public BWSSClient(
                string _Endpoint,
                string _UniqueConnectionID,
                Func<IBWebSocketClient> _WebSocketClientInitializer,
                Func<string, Task> _SendAction)
            {
                WebSocketClient = _WebSocketClientInitializer?.Invoke();
                if (WebSocketClient != null)
                {
                    Endpoint = _Endpoint;

                    SendAction = _SendAction;

                    UniqueConnectionID = _UniqueConnectionID;

                    WebSocketClient.OnListenerConnected(Endpoint, UniqueConnectionID, Send);
                }
            }

            public void OnClose()
            {
                WebSocketClient?.OnListenerDisconnected(Endpoint, UniqueConnectionID);
            }

            public void OnMessage(string Message)
            {
                WebSocketClient?.OnListenerMessage(Endpoint, UniqueConnectionID, Message, Send);
            }
        }
    }
}