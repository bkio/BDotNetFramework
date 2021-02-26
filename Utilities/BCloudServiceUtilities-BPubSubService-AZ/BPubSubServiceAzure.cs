/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BCommonUtilities;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json.Linq;

namespace BCloudServiceUtilities.PubSubServices
{
    public class BPubSubServiceAzure : IBPubSubServiceInterface
    {
        /// <summary>
        /// Azure Manager for managing Azure resources
        /// </summary>
        private readonly IAzure AzureManager;


        /// <summary>
        /// Azure Namespace Manager for managing Azure Service Bus Namespaces
        /// </summary>
        private readonly IServiceBusNamespace AzureNamespaceManager;

        /// <summary>
        /// Holds namespace connection string for QueueClient connections.
        /// </summary>
        private readonly string NamespaceConnectionString;

        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        private BPubSubUniqueMessageDeliveryEnsurer UniqueMessageDeliveryEnsurer = null;

        private readonly object SubscriberThreadsDictionaryLock = new object();
        private readonly Dictionary<string, BTuple<Thread, BValue<bool>>> SubscriberThreadsDictionary = new Dictionary<string, BTuple<Thread, BValue<bool>>>();

        /// <summary>
        /// 
        /// <para>BPubSubServiceAzure: Parametered Constructor for Managed Service by Microsoft Azure</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ClientId"/>                      Azure Application/Client Id</para>
        /// <para><paramref name="_ClientSecret"/>                  Azure Client Secret</para>
        /// <para><paramref name="_TenantId"/>                      Azure AD Tenant Id</para>
        /// <para><paramref name="_NamespaceId"/>                   Azure Service Bus Namespace Id</para>
        /// <para><paramref name="_NamespaceConnectionString"/>     Azure Service Bus Namespace Connection String</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public BPubSubServiceAzure(
            string _ClientId,
            string _ClientSecret,
            string _TenantId,
            string _NamespaceId,
            string _NamespaceConnectionString,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                NamespaceConnectionString = _NamespaceConnectionString;

                var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(_ClientId, _ClientSecret, _TenantId, AzureEnvironment.AzureGlobalCloud);

                AzureManager = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                using (var GetNamespaceTask = AzureManager.ServiceBusNamespaces.GetByIdAsync(_NamespaceId))
                {
                    GetNamespaceTask.Wait();
                    AzureNamespaceManager = GetNamespaceTask.Result;
                }

                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BPubSubServiceAzure->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        private bool CheckQueueExists(string _QueueName, out IQueueClient _QueueClient)
        {
            _QueueClient = null;

            try
            {
                using (var GetQueueTask = AzureNamespaceManager.Queues.GetByNameAsync(_QueueName))
                {
                    GetQueueTask.Wait();
                    if (GetQueueTask.Result != null && GetQueueTask.Result.Name != null && GetQueueTask.Result.Name.Length > 0)
                    {
                        _QueueClient = new QueueClient(NamespaceConnectionString, _QueueName);
                        return true;
                    }
                }
            }
            catch (Exception) { }

            return false;
        }

        private bool EnsureQueueExists(string _QueueName, out IQueueClient _QueueClient, Action<string> _ErrorMessageAction)
        {
            bool bExists = CheckQueueExists(_QueueName, out _QueueClient);
            if (!bExists)
            {
                try
                {
                    using (var CreateQueueTask = AzureNamespaceManager.Queues.Define(_QueueName).CreateAsync())
                    {
                        CreateQueueTask.Wait();
                        if (CreateQueueTask.Result != null && CreateQueueTask.Result.Name != null && CreateQueueTask.Result.Name.Length > 0)
                        {
                            _QueueClient = new QueueClient(NamespaceConnectionString, _QueueName);
                            bExists = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    bExists = false;
                    _ErrorMessageAction?.Invoke("BPubSubServiceAzure->EnsureQueueExists->Callback: " + e.Message + ", Trace: " + e.StackTrace);
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceAzure->EnsureQueueExists->Inner: " + e.InnerException.Message + ", Trace: " + e.InnerException.StackTrace);
                    }
                }
            }
            return bExists;
        }

        public bool CustomPublish(string _CustomTopic, string _CustomMessage, Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0
                && _CustomMessage != null && _CustomMessage.Length > 0
                && BUtility.CalculateStringMD5(_CustomTopic, out string TopicMD5, _ErrorMessageAction))
            {
                if (EnsureQueueExists(TopicMD5, out IQueueClient _QueueClient, _ErrorMessageAction))
                {
                    string TimestampHash = null;
                    UniqueMessageDeliveryEnsurer?.Publish_PrependTimestampToMessage(ref _CustomMessage, out TimestampHash);

                    try
                    {
                        if (UniqueMessageDeliveryEnsurer != null)
                        {
                            if (UniqueMessageDeliveryEnsurer.Publish_EnsureUniqueDelivery(_CustomTopic, TimestampHash, _ErrorMessageAction))
                            {
                                var AzureMessage = new Message(Encoding.UTF8.GetBytes(_CustomMessage));
                                AzureMessage.Label = TopicMD5;
                                using (var SendMessageTask = _QueueClient.SendAsync(AzureMessage))
                                {
                                    SendMessageTask.Wait();
                                }
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("BPubSubServiceAzure->CustomPublish: UniqueMessageDeliveryEnsurer has failed.");
                                return false;
                            }
                        }
                        else
                        {
                            var AzureMessage = new Message(Encoding.UTF8.GetBytes(_CustomMessage));
                            using (var SendMessageTask = _QueueClient.SendAsync(AzureMessage))
                            {
                                SendMessageTask.Wait();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceAzure->CustomPublish: " + e.Message + ", Trace: " + e.StackTrace);
                        if (e.InnerException != null && e.InnerException != e)
                        {
                            _ErrorMessageAction?.Invoke("BPubSubServiceAzure->CustomPublish->Inner: " + e.InnerException.Message + ", Trace: " + e.InnerException.StackTrace);
                        }
                        return false;
                    }
                    finally
                    {
                        using (var CloseTask = _QueueClient.CloseAsync())
                        {
                            CloseTask.Wait();
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public bool CustomSubscribe(string _CustomTopic, Action<string, string> _OnMessage, Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0 && _OnMessage != null && BUtility.CalculateStringMD5(_CustomTopic, out string TopicMD5, _ErrorMessageAction))
            {
                if (EnsureQueueExists(TopicMD5, out IQueueClient _QueueClient, _ErrorMessageAction))
                {
                    var SubscriptionCancellationVar = new BValue<bool>(false, EBProducerStatus.MultipleProducer);
                    var SubscriptionThread = new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        while (!SubscriptionCancellationVar.Get())
                        {
                            // Define exception receiver handler
                            Func<ExceptionReceivedEventArgs, Task> ExceptionReceiverHandler = (ExceptionReceivedEventArgs exceptionReceivedEventArgs) =>
                                {
                                    _ErrorMessageAction?.Invoke("BPubSubServiceAzure->CustomSubscribe: " + exceptionReceivedEventArgs.Exception.Message + ", Trace: " + exceptionReceivedEventArgs.Exception.StackTrace);
                                    return Task.CompletedTask;
                                };

                            // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
                            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceiverHandler)
                            {
                                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                                // Set it according to how many messages the application wants to process in parallel.
                                MaxConcurrentCalls = 1,

                                // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                                // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                                AutoComplete = false
                            };

                            try
                            {
                                // Register the function that processes messages.
                                _QueueClient.RegisterMessageHandler((Message MessageContainer, CancellationToken token) =>
                                {
                                    if (MessageContainer != null)
                                    {
                                        if (MessageContainer.Label != null &&
                                            MessageContainer.Label.Equals(TopicMD5, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            string Data = Encoding.UTF8.GetString(MessageContainer.Body);

                                            if (UniqueMessageDeliveryEnsurer != null)
                                            {
                                                UniqueMessageDeliveryEnsurer.Subscribe_ClearAndExtractTimestampFromMessage(ref Data, out string TimestampHash);

                                                if (UniqueMessageDeliveryEnsurer.Subscription_EnsureUniqueDelivery(_CustomTopic, TimestampHash, _ErrorMessageAction))
                                                {
                                                    _OnMessage?.Invoke(_CustomTopic, Data);
                                                }
                                            }
                                            else
                                            {
                                                _OnMessage?.Invoke(_CustomTopic, Data);
                                            }

                                            // Complete the message so that it is not received again.
                                            // This can be done only if the queue Client is created in ReceiveMode.PeekLock mode (which is the default).
                                            using (var ReceiveMessageCompleteTask = _QueueClient.CompleteAsync(MessageContainer.SystemProperties.LockToken))
                                            {
                                                ReceiveMessageCompleteTask.Wait();
                                            }
                                        }
                                        else
                                        {
                                            using (var DeadLetterTask = _QueueClient.DeadLetterAsync(MessageContainer.SystemProperties.LockToken))
                                            {
                                                DeadLetterTask.Wait();
                                            }
                                        }
                                    }

                                    return Task.CompletedTask;
                                }, messageHandlerOptions);
                            }
                            catch (Exception e)
                            {
                                _ErrorMessageAction?.Invoke("BPubSubServiceAzure->CustomSubscribe: " + e.Message + ", Trace: " + e.StackTrace);
                                if (e.InnerException != null && e.InnerException != e)
                                {
                                    _ErrorMessageAction?.Invoke("BPubSubServiceAzure->CustomSubscribe->Inner: " + e.InnerException.Message + ", Trace: " + e.InnerException.StackTrace);
                                }
                            }
                        }

                    });
                    SubscriptionThread.Start();

                    lock (SubscriberThreadsDictionaryLock)
                    {
                        SubscriberThreadsDictionary.Add(_CustomTopic, new BTuple<Thread, BValue<bool>>(SubscriptionThread, SubscriptionCancellationVar));
                    }
                    return true;
                }
            }
            return false;
        }

        public void DeleteCustomTopicGlobally(string _CustomTopic, Action<string> _ErrorMessageAction = null)
        {
            if (BUtility.CalculateStringMD5(_CustomTopic, out string TopicMD5, _ErrorMessageAction))
            {
                try
                {
                    lock (SubscriberThreadsDictionaryLock)
                    {
                        if (SubscriberThreadsDictionary.ContainsKey(_CustomTopic))
                        {
                            var SubscriberThread = SubscriberThreadsDictionary[_CustomTopic];
                            if (SubscriberThread != null)
                            {
                                SubscriberThread.Item2.Set(true);
                            }
                            SubscriberThreadsDictionary.Remove(_CustomTopic);
                        }
                    }

                    using (var DeleteQueueTask = AzureNamespaceManager.Queues.DeleteByNameAsync(TopicMD5))
                    {
                        DeleteQueueTask.Wait();
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BPubSubServiceAzure->DeleteCustomTopicGlobally->Callback: " + e.Message + ", Trace: " + e.StackTrace);
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceAzure->DeleteCustomTopicGlobally->Inner: " + e.InnerException.Message + ", Trace: " + e.InnerException.StackTrace);
                    }
                }
            }
        }

        /// <summary>
        ///
        /// <para>DeleteTopicGlobally:</para>
        /// 
        /// <para>Deletes all messages and the topic of given workspace [_QueryParameters.Domain]:[_QueryParameters.SubDomain] topic</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.DeleteTopicGlobally"/> for detailed documentation</para>
        ///
        /// </summary>
        public void DeleteTopicGlobally(BMemoryQueryParameters _QueryParameters, Action<string> _ErrorMessageAction = null)
        {
            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            DeleteCustomTopicGlobally(Topic, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>EnsureUniqueMessageDelivery:</para>
        /// 
        /// <para>Sets up the unique message delivery ensurer</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.EnsureUniqueMessageDelivery"/> for detailed documentation</para>
        ///
        /// </summary>
        public void EnsureUniqueMessageDelivery(IBMemoryServiceInterface _EnsurerMemoryService, Action<string> _ErrorMessageAction = null)
        {
            UniqueMessageDeliveryEnsurer = new BPubSubUniqueMessageDeliveryEnsurer(_EnsurerMemoryService, this);
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        ///
        /// <para>Publish:</para>
        /// 
        /// <para>Publishes the given message to given workspace [_QueryParameters.Domain]:[_QueryParameters.SubDomain]:[_QueryParameters.Identifier] topic</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.Publish"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool Publish(BMemoryQueryParameters _QueryParameters, JObject _Message, Action<string> _ErrorMessageAction = null)
        {
            if (_Message == null) return false;

            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;
            string Message = _Message.ToString();

            return CustomPublish(Topic, Message, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>Subscribe:</para>
        /// 
        /// <para>Subscribes to given workspace [_QueryParameters.Domain]:[_QueryParameters.SubDomain]:[_QueryParameters.Identifier] topic</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.Subscribe"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool Subscribe(BMemoryQueryParameters _QueryParameters, Action<string, JObject> _OnMessage, Action<string> _ErrorMessageAction = null)
        {
            if (_OnMessage == null) return false;

            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            return CustomSubscribe(Topic, (string TopicParameter, string MessageParameter) =>
            {
                JObject AsJson;
                try
                {
                    AsJson = JObject.Parse(MessageParameter);
                }
                catch (Exception e)
                {
                    AsJson = null;
                    _ErrorMessageAction?.Invoke("BPubSubServiceAzure->Subscribe->Callback: " + e.Message + ", Trace: " + e.StackTrace);
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceAzure->Subscribe->Callback->Inner: " + e.InnerException.Message + ", Trace: " + e.InnerException.StackTrace);
                    }
                }

                if (AsJson != null)
                {
                    _OnMessage?.Invoke(TopicParameter, AsJson);
                }

            }, _ErrorMessageAction);
        }
    }
}
