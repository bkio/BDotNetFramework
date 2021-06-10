/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using BWebServiceUtilities;
using ServiceUtilities.All;
using Newtonsoft.Json.Linq;

namespace ServiceUtilities
{
    public static class PubSubServiceBaseCommon
    {
        public static BWebServiceResponse OnRequest(HttpListenerContext _Context, string _CallerMethod, Func<ServiceUtilities.Action, bool> _HandleAction,  Action<string> _ErrorMessageAction = null)
        {
            string SerializedData = null;
            using (var InputStream = _Context.Request.InputStream)
            {
                using (var Reader = new StreamReader(InputStream))
                {
                    var JsonMessage = Reader.ReadToEnd();
                    try
                    {
                        var Parsed = JObject.Parse(JsonMessage);
                        if (Parsed.ContainsKey("message"))
                        {
                            var MessageObject = (JObject)Parsed["message"];
                            if (MessageObject.ContainsKey("data"))
                            {
                                var EncodedData = (string)MessageObject["data"];
                                SerializedData = Encoding.UTF8.GetString(Convert.FromBase64String(EncodedData));
                            }
                        }
                        if (SerializedData == null)
                        {
                            SerializedData = JsonMessage;
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke(_CallerMethod + "->OnRequest: Conversion from Base64 to string has failed with " + e.Message + ", trace: " + e.StackTrace + ", payload is: " + JsonMessage);
                        return BWebResponse.BadRequest("Conversion from Base64 to string has failed.");
                    }
                }
            }

            if (!Manager_PubSubService.Get().DeserializeReceivedMessage(SerializedData,
                out Actions.EAction Action,
                out string SerializedAction,
                _ErrorMessageAction))
            {
                return BWebResponse.BadRequest("Deserialization of Pub/Sub Message has failed.");
            }

            bool bResult;
            try
            {
                bResult = _HandleAction.Invoke(Actions.DeserializeAction(Action, SerializedAction));
            }
            catch (Exception e)
            {
                return BWebResponse.BadRequest("Deserialization to Action has failed with " + e.Message + ", trace: " + e.StackTrace);
            }

            if (bResult)
            {
                return BWebResponse.StatusOK("Processed.");
            }

            _ErrorMessageAction?.Invoke(_CallerMethod + "->OnRequest: An error occured. Retrying.");

            //Cooldown
            Thread.Sleep(1000);

            return BWebResponse.BadRequest("An error occurred. Retrying.");
        }

        public static BWebServiceResponse OnRequestWebhook(HttpListenerContext _Context, string _CallerMethod, Func<ServiceUtilities.Action, bool> _HandleAction, Action<string> _ErrorMessageAction = null)
        {
            string SerializedData = null;
            string TopicName = null;
            using (var InputStream = _Context.Request.InputStream)
            {
                using (var Reader = new StreamReader(InputStream))
                {
                    var JsonMessage = Reader.ReadToEnd();
                    try
                    {
                        var Parsed = JObject.Parse(JsonMessage);
                        if (Parsed.ContainsKey("data"))
                        {
                            var DataObject = (JObject)Parsed["data"];

                            if (DataObject.ContainsKey("topicName")/*,
                                DataObject.ContainsKey("namespaceName")
                                && DataObject.ContainsKey("requestUri")
                                && DataObject.ContainsKey("subscriptionName")*/)
                            {
                                TopicName = (string)DataObject["topicName"];
                                //var NamespaceName = (string)DataObject["namespaceName"];
                                //var RequestUri = (string)DataObject["requestUri"];
                                //var SubscriptionName = (string)DataObject["subscriptionName"];

                                _ErrorMessageAction?.Invoke($"{_CallerMethod}->OnRequest[INFO]: Action received from CloudEventSchemaV1_0. TopicName: '{TopicName}'");
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke(_CallerMethod + "->OnRequest[ERROR]: Invalid CloudEventSchemaV1_0 data type. Payload is: " + JsonMessage);
                            return BWebResponse.BadRequest("Invalid CloudEventSchemaV1_0 data type. Json Parse error occurred.");
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke(_CallerMethod + "->OnRequest[ERROR]: Invalid CloudEventSchemaV1_0 data type. Error: " + e.Message + ", trace: " + e.StackTrace + ", payload is: " + JsonMessage);
                        return BWebResponse.BadRequest("Invalid CloudEventSchemaV1_0 data type. Json Parse error occurred.");
                    }
                }
            }

            if (!Manager_PubSubService.Get().ReceiveSingleMessage(TopicName, 
                (string ReceivedTopic, string ReceivedMessage) =>
                {
                    var ParsedMessage = JObject.Parse(ReceivedMessage);
                    if (ParsedMessage.ContainsKey("data"))
                    {
                        SerializedData = ParsedMessage["data"].ToString();
                    }
                    else
                    {
                        SerializedData = ParsedMessage.ToString();
                    }

                    if (!Manager_PubSubService.Get().DeserializeReceivedMessage(SerializedData,
                        out Actions.EAction Action,
                        out string SerializedAction,
                        _ErrorMessageAction))
                    {
                        _ErrorMessageAction?.Invoke($"{_CallerMethod}->SubscribeAction: An error occured when subscribing {Action.ToString()}.");
                    }

                    if (!_HandleAction.Invoke(Actions.DeserializeAction(Action, SerializedAction)))
                    {
                        _ErrorMessageAction?.Invoke($"{_CallerMethod}->DeserializeAction: An error occured when deserializing {Action.ToString()}.");
                    }

                }, _ErrorMessageAction))
            {
                return BWebResponse.BadRequest("Receiving message from subscription has failed.");
            }

            return BWebResponse.StatusOK("Action has been received. It will be processed.");
        }
    }

    public abstract class PubSubServiceBase : InternalWebServiceBase
    {
        public PubSubServiceBase(string _InternalCallPrivateKey) : base(_InternalCallPrivateKey)
        {
        }

        protected override BWebServiceResponse Process(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            return PubSubServiceBaseCommon.OnRequest(_Context, "PubSubServiceBase",
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