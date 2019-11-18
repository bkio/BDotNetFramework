/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using BCommonUtilities;
using Newtonsoft.Json.Linq;

namespace BCloudServiceUtilities.PubSubServices
{
    public class BPubSubServiceAWS : IBPubSubServiceInterface
    {
        /// <summary>
        /// AWS Dynamodb Client that is responsible to serve to this object
        /// </summary>
        private readonly AmazonSQSClient SQSClient;

        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        /// <summary>
        /// 
        /// <para>BPubSubServiceAWS: Parametered Constructor for Managed Service by Amazon</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_AccessKey"/>              AWS Access Key</para>
        /// <para><paramref name="_SecretKey"/>              AWS Secret Key</para>
        /// <para><paramref name="_Region"/>                 AWS Region that Pub/Sub Client will connect to (I.E. eu-west-1) </para>
        /// <para><paramref name="_ErrorMessageAction"/>     Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public BPubSubServiceAWS(
            string _AccessKey,
            string _SecretKey,
            string _Region,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                SQSClient = new AmazonSQSClient(new Amazon.Runtime.BasicAWSCredentials(_AccessKey, _SecretKey), Amazon.RegionEndpoint.GetBySystemName(_Region));
                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BPubSubServiceAWS->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        ~BPubSubServiceAWS()
        {
            SQSClient?.Dispose();
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
        /// <para>EnsureUniqueMessageDelivery:</para>
        /// 
        /// <para>Sets up the unique message delivery ensurer</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.EnsureUniqueMessageDelivery"/> for detailed documentation</para>
        ///
        /// </summary>
        public void EnsureUniqueMessageDelivery(
            IBMemoryServiceInterface _EnsurerMemoryService,
            Action<string> _ErrorMessageAction = null)
        {
            UniqueMessageDeliveryEnsurer = new BPubSubUniqueMessageDeliveryEnsurer(_EnsurerMemoryService, this);
        }
        private BPubSubUniqueMessageDeliveryEnsurer UniqueMessageDeliveryEnsurer = null;

        private bool EnsureQueueExists(string _QueueName, out string _QueueUrl, Action<string> _ErrorMessageAction)
        {
            bool bExists = CheckQueueExists(_QueueName, out _QueueUrl);
            if (!bExists)
            {
                try
                {
                    using (var CreateQueueTask = SQSClient.CreateQueueAsync(_QueueName))
                    {
                        CreateQueueTask.Wait();
                        if (CreateQueueTask.Result != null && CreateQueueTask.Result.QueueUrl != null && CreateQueueTask.Result.QueueUrl.Length > 0)
                        {
                            _QueueUrl = CreateQueueTask.Result.QueueUrl;
                            bExists = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    bExists = false;
                    _ErrorMessageAction?.Invoke("BPubSubServiceAWS->EnsureQueueExists->Callback: " + e.Message + ", Trace: " + e.StackTrace);
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceAWS->EnsureQueueExists->Inner: " + e.InnerException.Message + ", Trace: " + e.StackTrace);
                    }
                }
            }
            return bExists;
        }

        private bool CheckQueueExists(string _QueueName, out string _QueueUrl)
        {
            _QueueUrl = null;

            try
            {
                using (var GetQueueTask = SQSClient.GetQueueUrlAsync(new GetQueueUrlRequest(_QueueName)))
                {
                    GetQueueTask.Wait();
                    if (GetQueueTask.Result != null && GetQueueTask.Result.QueueUrl != null && GetQueueTask.Result.QueueUrl.Length > 0)
                    {
                        _QueueUrl = GetQueueTask.Result.QueueUrl;
                        return true;
                    }
                }
            }
            catch (Exception) {}

            return false;
        }

        private readonly object SubscriberThreadsDictionaryLock = new object();
        private readonly Dictionary<string, BTuple<Thread, BValue<bool>>> SubscriberThreadsDictionary = new Dictionary<string, BTuple<Thread, BValue<bool>>>();

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
                    _ErrorMessageAction?.Invoke("BPubSubServiceAWS->Subscribe->Callback: " + e.Message + ", Trace: " + e.StackTrace);
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceAWS->Subscribe->Callback->Inner: " + e.InnerException.Message + ", Trace: " + e.StackTrace);
                    }
                }

                if (AsJson != null)
                {
                    _OnMessage?.Invoke(TopicParameter, AsJson);
                }

            }, _ErrorMessageAction);
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
        /// <para>CustomSubscribe:</para>
        /// 
        /// <para>Subscribes to given custom topic</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.CustomSubscribe"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CustomSubscribe(string _CustomTopic, Action<string, string> _OnMessage, Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0 && _OnMessage != null && BUtility.CalculateStringMD5(_CustomTopic, out string TopicMD5, _ErrorMessageAction))
            {
                if (EnsureQueueExists(TopicMD5, out string QueueUrl, _ErrorMessageAction))
                {
                    var SubscriptionCancellationVar = new BValue<bool>(false, EBProducerStatus.MultipleProducer);
                    var SubscriptionThread = new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        while (!SubscriptionCancellationVar.Get())
                        {
                            ReceiveMessageResponse Response;
                            try
                            {
                                using (var ReceiveMessageTask = SQSClient.ReceiveMessageAsync(QueueUrl))
                                {
                                    ReceiveMessageTask.Wait();
                                    Response = ReceiveMessageTask.Result;
                                }
                            }
                            catch (Exception e)
                            {
                                Response = null;
                                _ErrorMessageAction?.Invoke("BPubSubServiceAWS->CustomSubscribe: " + e.Message + ", Trace: " + e.StackTrace);
                                if (e.InnerException != null && e.InnerException != e)
                                {
                                    _ErrorMessageAction?.Invoke("BPubSubServiceAWS->CustomSubscribe->Inner: " + e.InnerException.Message + ", Trace: " + e.StackTrace);
                                }
                            }

                            if (Response == null || Response.Messages == null || Response.Messages.Count == 0)
                            {
                                Thread.Sleep(1000);
                                continue;
                            }

                            var AckDictionary = new Dictionary<string, string>();

                            foreach (var MessageContainer in Response.Messages)
                            {
                                if (MessageContainer != null)
                                {
                                    if (!AckDictionary.ContainsKey(MessageContainer.MessageId))
                                    {
                                        AckDictionary.Add(MessageContainer.MessageId, MessageContainer.ReceiptHandle);
                                    }

                                    string Data = MessageContainer.Body;

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
                                }
                            }

                            var AckArray = new List<DeleteMessageBatchRequestEntry>();
                            foreach (var Current in AckDictionary)
                            {
                                AckArray.Add(new DeleteMessageBatchRequestEntry(Current.Key, Current.Value));
                            }

                            try
                            {
                                using (var DeleteMessageBatchTask = SQSClient.DeleteMessageBatchAsync(QueueUrl, AckArray))
                                {
                                    DeleteMessageBatchTask.Wait();
                                }
                            }
                            catch (Exception e)
                            {
                                _ErrorMessageAction?.Invoke("BPubSubServiceAWS->CustomSubscribe: " + e.Message + ", Trace: " + e.StackTrace);
                                if (e.InnerException != null && e.InnerException != e)
                                {
                                    _ErrorMessageAction?.Invoke("BPubSubServiceAWS->CustomSubscribe->Inner: " + e.InnerException.Message + ", Trace: " + e.StackTrace);
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

        /// <summary>
        ///
        /// <para>CustomPublish:</para>
        /// 
        /// <para>Publishes the given message to given custom topic</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.CustomPublish"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool CustomPublish(string _CustomTopic, string _CustomMessage, Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0
                && _CustomMessage != null && _CustomMessage.Length > 0
                && BUtility.CalculateStringMD5(_CustomTopic, out string TopicMD5, _ErrorMessageAction))
            {
                if (EnsureQueueExists(TopicMD5, out string QueueUrl, _ErrorMessageAction))
                {
                    string TimestampHash = null;
                    UniqueMessageDeliveryEnsurer?.Publish_PrependTimestampToMessage(ref _CustomMessage, out TimestampHash);

                    try
                    {
                        if (UniqueMessageDeliveryEnsurer != null)
                        {
                            if (UniqueMessageDeliveryEnsurer.Publish_EnsureUniqueDelivery(_CustomTopic, TimestampHash, _ErrorMessageAction))
                            {
                                using (var SendMessageTask = SQSClient.SendMessageAsync(QueueUrl, _CustomMessage))
                                {
                                    SendMessageTask.Wait();
                                }
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("BPubSubServiceAWS->CustomPublish: UniqueMessageDeliveryEnsurer has failed.");
                                return false;
                            }
                        }
                        else
                        {
                            using (var SendMessageTask = SQSClient.SendMessageAsync(QueueUrl, _CustomMessage))
                            {
                                SendMessageTask.Wait();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceAWS->CustomPublish: " + e.Message + ", Trace: " + e.StackTrace);
                        if (e.InnerException != null && e.InnerException != e)
                        {
                            _ErrorMessageAction?.Invoke("BPubSubServiceAWS->CustomPublish->Inner: " + e.InnerException.Message + ", Trace: " + e.StackTrace);
                        }
                        return false;
                    }
                    return true;
                }
            }
            return false;
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
        /// <para>DeleteCustomTopicGlobally:</para>
        /// 
        /// <para>Deletes all messages and the topic of given workspace</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.DeleteCustomTopicGlobally"/> for detailed documentation</para>
        ///
        /// </summary>
        public void DeleteCustomTopicGlobally(string _CustomTopic, Action<string> _ErrorMessageAction = null)
        {
            if (BUtility.CalculateStringMD5(_CustomTopic, out string TopicMD5, _ErrorMessageAction)
                && CheckQueueExists(TopicMD5, out string QueueUrl))
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

                    using (var DeleteQueueTask = SQSClient.DeleteQueueAsync(QueueUrl))
                    {
                        DeleteQueueTask.Wait();
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BPubSubServiceAWS->DeleteCustomTopicGlobally->Callback: " + e.Message + ", Trace: " + e.StackTrace);
                    if (e.InnerException != null && e.InnerException != e)
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceAWS->DeleteCustomTopicGlobally->Inner: " + e.InnerException.Message + ", Trace: " + e.StackTrace);
                    }
                }
            }
        }
    }
}