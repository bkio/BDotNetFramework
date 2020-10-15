/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Net;
using BWebServiceUtilities;
using ServiceUtilities;

namespace ServiceUtilities.PubSubUsers.PubSubRelated
{
    public abstract class WebServiceBaseTimeoutableDeliveryEnsurerUser : WebServiceBaseTimeoutable
    {
        public readonly WebServiceBaseTimeoutableDeliveryEnsurerUserProcessor InnerDeliveryEnsurerUserProcessor;

        protected WebServiceBaseTimeoutableDeliveryEnsurerUser()
        {
            InnerDeliveryEnsurerUserProcessor = new WebServiceBaseTimeoutableDeliveryEnsurerUserProcessor(new WeakReference<WebServiceBaseTimeoutableProcessor>(InnerProcessor), OnRequest_Interruptable_DeliveryEnsurerUser);
        }

        public override BWebServiceResponse OnRequest_Interruptable(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return InnerDeliveryEnsurerUserProcessor.OnRequest_Interruptable(_Context, _ErrorMessageAction);
        }

        public abstract BWebServiceResponse OnRequest_Interruptable_DeliveryEnsurerUser(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }
    public abstract class InternalWebServiceBaseTimeoutableDeliveryEnsurerUser : InternalWebServiceBaseTimeoutable
    {
        public readonly WebServiceBaseTimeoutableDeliveryEnsurerUserProcessor InnerDeliveryEnsurerUserProcessor;

        protected InternalWebServiceBaseTimeoutableDeliveryEnsurerUser(string _InternalCallPrivateKey) : base(_InternalCallPrivateKey)
        {
            InnerDeliveryEnsurerUserProcessor = new WebServiceBaseTimeoutableDeliveryEnsurerUserProcessor(new WeakReference<WebServiceBaseTimeoutableProcessor>(InnerProcessor), OnRequest_Interruptable_DeliveryEnsurerUser);
        }

        public override BWebServiceResponse OnRequest_Interruptable(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return InnerDeliveryEnsurerUserProcessor.OnRequest_Interruptable(_Context, _ErrorMessageAction);
        }

        public abstract BWebServiceResponse OnRequest_Interruptable_DeliveryEnsurerUser(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null);
    }
    public class WebServiceBaseTimeoutableDeliveryEnsurerUserProcessor
    {
        public HttpListenerContext CachedContext;

        private readonly Func<HttpListenerContext, Action<string>, BWebServiceResponse> OnRequestCallback;

        public readonly WeakReference<WebServiceBaseTimeoutableProcessor> OwnerProcessor;

        public WebServiceBaseTimeoutableDeliveryEnsurerUserProcessor(WeakReference<WebServiceBaseTimeoutableProcessor> _OwnerProcessor, Func<HttpListenerContext, Action<string>, BWebServiceResponse> _OnRequestCallback)
        {
            OwnerProcessor = _OwnerProcessor;
            if (OwnerProcessor.TryGetTarget(out WebServiceBaseTimeoutableProcessor StrongOwnerProcessor))
            {
                StrongOwnerProcessor.UseQueueSetClearanceActions();
            }
            OnRequestCallback = _OnRequestCallback;
        }

        public BWebServiceResponse OnRequest_Interruptable(HttpListenerContext _Context, Action<string> _ErrorMessageAction)
        {
            CachedContext = _Context;
            var Result = OnRequestCallback?.Invoke(_Context, _ErrorMessageAction);
            Controller_DeliveryEnsurer.Get().WaitUntilActionsCompleted(_Context, _ErrorMessageAction);
            if (OwnerProcessor.TryGetTarget(out WebServiceBaseTimeoutableProcessor StrongOwner))
            {
                Controller_AtomicDBOperation.Get().WaitUntilSetClearancesCompleted(StrongOwner, _ErrorMessageAction);
            }
            return Result ?? BWebResponse.InternalError("WebServiceBaseTimeoutableDeliveryEnsurerUserProcessor has failed.");
        }
    }
}