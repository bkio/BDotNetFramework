/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using BCommonUtilities;
using BWebServiceUtilities;
using ServiceUtilities.All;

namespace ServiceUtilities
{
    public abstract class InternalWebServiceBaseTimeoutable : InternalWebServiceBase
    {
        public readonly WebServiceBaseTimeoutableProcessor InnerProcessor;

        protected InternalWebServiceBaseTimeoutable(string _InternalCallPrivateKey) : base(_InternalCallPrivateKey)
        {
            InnerProcessor = new WebServiceBaseTimeoutableProcessor(OnRequest_Interruptable);
        }

        protected override BWebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return InnerProcessor.ProcessRequest(_Context, _ErrorMessageAction);
        }
        public abstract BWebServiceResponse OnRequest_Interruptable(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }

    public abstract class WebServiceBaseTimeoutable : BppWebServiceBase
    {
        public readonly WebServiceBaseTimeoutableProcessor InnerProcessor;

        protected WebServiceBaseTimeoutable() 
        {
            InnerProcessor = new WebServiceBaseTimeoutableProcessor(OnRequest_Interruptable);
        }

        protected override BWebServiceResponse OnRequestPP(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return InnerProcessor.ProcessRequest(_Context, _ErrorMessageAction);
        }
        public abstract BWebServiceResponse OnRequest_Interruptable(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }

    public class WebServiceBaseTimeoutableProcessor
    {
        private static readonly HashSet<WeakReference<WebServiceBaseTimeoutableProcessor>> InterruptableWebServiceProcessors = new HashSet<WeakReference<WebServiceBaseTimeoutableProcessor>>();

        private readonly Func<HttpListenerContext, Action<string>, BWebServiceResponse> OnRequestCallback;

        public WebServiceBaseTimeoutableProcessor(Func<HttpListenerContext, Action<string>, BWebServiceResponse> _OnRequestCallback)
        {
            OnRequestCallback = _OnRequestCallback;
            lock (InterruptableWebServiceProcessors)
            {
                InterruptableWebServiceProcessors.Add(new WeakReference<WebServiceBaseTimeoutableProcessor>(this));
            }
        }
        ~WebServiceBaseTimeoutableProcessor()
        {
            lock (InterruptableWebServiceProcessors)
            {
                foreach (var Weak in InterruptableWebServiceProcessors)
                {
                    if (Weak.TryGetTarget(out WebServiceBaseTimeoutableProcessor Strong) && Strong == this)
                    {
                        InterruptableWebServiceProcessors.Remove(Weak);
                        return;
                    }
                }
            }
            try
            {
                WaitUntilSignal.Close();
            }
            catch (Exception) { }
        }

        public static void OnTimeoutNotificationReceived(Action_OperationTimeout _Notification)
        {
            lock (InterruptableWebServiceProcessors)
            {
                foreach (var ProcessorWeakPtr in InterruptableWebServiceProcessors)
                {
                    if (ProcessorWeakPtr.TryGetTarget(out WebServiceBaseTimeoutableProcessor Processor))
                    {
                        foreach (var TimeoutStructure in Processor.RelevantTimeoutStructures)
                        {
                            if (TimeoutStructure.Equals(_Notification))
                            {
                                Processor.TimeoutOccurred();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool bDoNotGetDBClearance = false;
        public bool IsDoNotGetDBClearanceSet()
        {
            return bDoNotGetDBClearance;
        }

        private readonly ConcurrentQueue<BWebServiceResponse> Responses = new ConcurrentQueue<BWebServiceResponse>();
        private readonly ManualResetEvent WaitUntilSignal = new ManualResetEvent(false);
        public BWebServiceResponse ProcessRequest(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            bDoNotGetDBClearance =
                BWebUtilities.DoesContextContainHeader(out List<string> DNGDBCs, out string _, _Context, "do-not-get-db-clearance")
                && BUtility.CheckAndGetFirstStringFromList(DNGDBCs, out string DNGDBC)
                && DNGDBC == "true";

            BTaskWrapper.Run(() =>
            {
                var Response =  OnRequestCallback?.Invoke(_Context, _ErrorMessageAction);
                if (Response.HasValue)
                {
                    Responses.Enqueue(Response.Value);
                }

                try
                {
                    WaitUntilSignal.Set();
                }
                catch (Exception) { }
            });

            try
            {
                WaitUntilSignal.WaitOne();
            }
            catch (Exception) { }

            if (!Responses.TryDequeue(out BWebServiceResponse FirstResponse))
            {
                FirstResponse = BWebResponse.InternalError("Unexpected error in concurrence.");
            }
            return FirstResponse;
        }

        private void TimeoutOccurred()
        {
            Responses.Enqueue(BWebResponse.InternalError("Database operation timed out."));
            try
            {
                WaitUntilSignal.Set();
            }
            catch (Exception) { }
        }

        private bool bQueueSetClearanceActions = false;
        public bool IsUseQueueSetClearanceActionsSet()
        {
            return bQueueSetClearanceActions;
        }
        public void UseQueueSetClearanceActions()
        {
            bQueueSetClearanceActions = true;
        }

        public readonly List<Action_OperationTimeout> RelevantTimeoutStructures = new List<Action_OperationTimeout>();

        private readonly HashSet<string> SetClearanceAwaitItems = new HashSet<string>();
        public void AddSetClearanceAwaitItem(string _MemoryEntryValue)
        {
            lock (SetClearanceAwaitItems)
            {
                SetClearanceAwaitItems.Add(_MemoryEntryValue);
            }
        }
        public string[] GetAndEmptySetClearanceAwaitItems()
        {
            lock (SetClearanceAwaitItems)
            {
                if (SetClearanceAwaitItems.Count == 0) return null;

                var Result = SetClearanceAwaitItems.ToArray();
                SetClearanceAwaitItems.Clear();
                return Result;
            }
        }
        public bool TryRemoveSetClearanceAwaitItem(string _MemoryEntryValue)
        {
            lock (SetClearanceAwaitItems)
            {
                return SetClearanceAwaitItems.Remove(_MemoryEntryValue);
            }
        }
    }
}