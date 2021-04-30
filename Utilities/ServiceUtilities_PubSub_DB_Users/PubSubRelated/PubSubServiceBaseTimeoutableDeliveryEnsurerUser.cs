/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Net;
using BWebServiceUtilities;
using ServiceUtilities.PubSubUsers.PubSubRelated;

namespace ServiceUtilities
{
    public abstract class PubSubServiceBaseTimeoutableDeliveryEnsurerUser : InternalWebServiceBaseTimeoutableDeliveryEnsurerUser
    {
        public PubSubServiceBaseTimeoutableDeliveryEnsurerUser(string _InternalCallPrivateKey) : base(_InternalCallPrivateKey)
        {
        }

        public override BWebServiceResponse OnRequest_Interruptable_DeliveryEnsurerUser(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return PubSubServiceBaseCommon.OnRequest(_Context, "PubSubServiceBaseTimeoutableDeliveryEnsurerUser",
                (ServiceUtilities.Action _Action) =>
                {
                    GetTracingService()?.On_FromGatewayToService_Received(_Context, _ErrorMessageAction);
                    var bResult = Handle(_Context, _Action, _ErrorMessageAction);
                    GetTracingService()?.On_FromServiceToGateway_Sent(_Context, _ErrorMessageAction);
                    return bResult;

                }, _ErrorMessageAction);
        }

        protected abstract bool Handle(HttpListenerContext _Context, Action _DeserializedAction, Action<string> _ErrorMessageAction = null);
    }

    public abstract class PubSubServiceBaseWebhookTimeoutableDeliveryEnsurerUser : InternalWebServiceBaseWebhookTimeoutableDeliveryEnsurerUser
    {
        public PubSubServiceBaseWebhookTimeoutableDeliveryEnsurerUser(string _InternalCallPrivateKey) : base(_InternalCallPrivateKey)
        {
        }

        public override BWebServiceResponse OnRequest_Interruptable_DeliveryEnsurerUser(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return PubSubServiceBaseCommon.OnRequest(_Context, "PubSubServiceBaseWebhookTimeoutableDeliveryEnsurerUser",
                (ServiceUtilities.Action _Action) =>
                {
                    GetTracingService()?.On_FromGatewayToService_Received(_Context, _ErrorMessageAction);
                    var bResult = Handle(_Context, _Action, _ErrorMessageAction);
                    GetTracingService()?.On_FromServiceToGateway_Sent(_Context, _ErrorMessageAction);
                    return bResult;

                }, _ErrorMessageAction);
        }

        protected abstract bool Handle(HttpListenerContext _Context, Action _DeserializedAction, Action<string> _ErrorMessageAction = null);
    }
}