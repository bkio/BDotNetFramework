/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BCommonUtilities;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Grpc.Core;
using Newtonsoft.Json.Linq;

namespace BCloudServiceUtilities.PubSubServices
{
    public class BPubSubServiceGC : IBPubSubServiceInterface
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly string ProjectID;

        private readonly ServiceAccountCredential Credential;

        private readonly Dictionary<string, PublisherServiceApiClient> PublisherTopicDictionary = new Dictionary<string, PublisherServiceApiClient>();
        private readonly List<BTuple<string, SubscriberServiceApiClient, SubscriptionName>> SubscriberTopicList = new List<BTuple<string, SubscriberServiceApiClient, SubscriptionName>>();
        private readonly Dictionary<SubscriptionName, BTuple<Thread, BValue<bool>>> SubscriberThreadsDictionary = new Dictionary<SubscriptionName, BTuple<Thread, BValue<bool>>>();
        private readonly object PublisherTopicDictionaryLock = new object();
        private readonly object SubscriberTopicListLock = new object();
        private readonly object SubscriberThreadsDictionaryLock = new object();

        /// <summary>
        /// 
        /// <para>BPubSubServiceGC: Parametered Constructor for Managed Service by Google</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ProjectID"/>              GC Project ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>     Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public BPubSubServiceGC(
            string _ProjectID,
            Action<string> _ErrorMessageAction = null)
        {
            ProjectID = _ProjectID;
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                if (ApplicationCredentials == null)
                {
                    _ErrorMessageAction?.Invoke("BPubSubServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    var Scopes = new List<string>();
                    foreach (var Scope in PublisherServiceApiClient.DefaultScopes)
                    {
                        if (!Scopes.Contains(Scope))
                        {
                            Scopes.Add(Scope);
                        }
                    }
                    foreach (var Scope in SubscriberServiceApiClient.DefaultScopes)
                    {
                        if (!Scopes.Contains(Scope))
                        {
                            Scopes.Add(Scope);
                        }
                    }

                    using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                    {
                        Credential = GoogleCredential.FromStream(Stream)
                            .CreateScoped(
                            Scopes.ToArray())
                            .UnderlyingCredential as ServiceAccountCredential;
                    }

                    if (Credential != null)
                    {
                        bInitializationSucceed = true;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BPubSubServiceGC->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        private static string GetGoogleFriendlyTopicName(string Input)
        {
            return Base32.Encode(Encoding.UTF8.GetBytes(Input)).ToLower();
        }
        private static string GetTopicNameFromGoogleFriendlyName(string Input)
        {
            return Encoding.UTF8.GetString(Base32.Decode(Input.ToUpper()));
        }

        private bool GetPublisher(out PublisherServiceApiClient Result, TopicName GoogleFriendlyTopicName, Action<string> _ErrorMessageAction = null)
        {
            lock (PublisherTopicDictionaryLock)
            {
                if (PublisherTopicDictionary.ContainsKey(GoogleFriendlyTopicName.TopicId))
                {
                    Result = PublisherTopicDictionary[GoogleFriendlyTopicName.TopicId];
                    return true;
                }

                try
                {
                    Result = PublisherServiceApiClient.Create();
                    PublisherTopicDictionary[GoogleFriendlyTopicName.TopicId] = Result;
                }
                catch (Exception e)
                {
                    Result = null;

                    _ErrorMessageAction?.Invoke("BPubSubServiceGC->GetPublisher: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }

                if (!EnsureTopicExistence(GoogleFriendlyTopicName, Result, _ErrorMessageAction))
                {
                    Result = null;
                    return false;
                }
                
                return true;
            }
        }

        private bool GetSubscriber(out SubscriberServiceApiClient APIClientVar, out SubscriptionName SubscriptionNameVar, string GoogleFriendlyTopicName, Action<string> _ErrorMessageAction = null)
        {
            lock (SubscriberTopicListLock)
            {
                APIClientVar = null;
                SubscriptionNameVar = null;

                var TopicInstance = new TopicName(ProjectID, GoogleFriendlyTopicName);

                if (!EnsureTopicExistence(TopicInstance, null, _ErrorMessageAction))
                {
                    return false;
                }

                try
                {
                    APIClientVar = SubscriberServiceApiClient.Create();
                }
                catch (Exception e)
                {
                    APIClientVar = null;
                    _ErrorMessageAction?.Invoke("BPubSubServiceGC->GetSubscriber: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }

                string SubscriptionIDBase = GoogleFriendlyTopicName + "-";
                int SubscriptionIDIncrementer = 1;
                SubscriptionNameVar = new SubscriptionName(ProjectID, SubscriptionIDBase + SubscriptionIDIncrementer);

                bool bSubscriptionSuccess = false;
                while (!bSubscriptionSuccess)
                {
                    try
                    {
                        APIClientVar.CreateSubscription(SubscriptionNameVar, TopicInstance, null, 600);
                        bSubscriptionSuccess = true;
                    }
                    catch (Exception e)
                    {
                        if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.AlreadyExists)
                        {
                            SubscriptionIDIncrementer++;
                            SubscriptionNameVar = new SubscriptionName(ProjectID, SubscriptionIDBase + SubscriptionIDIncrementer);
                        }
                        else
                        {
                            SubscriptionNameVar = null;
                            _ErrorMessageAction?.Invoke("BPubSubServiceGC->GetSubscriber: " + e.Message + ", Trace: " + e.StackTrace);
                            return false;
                        }
                    }
                }
                
                SubscriberTopicList.Add(new BTuple<string, SubscriberServiceApiClient, SubscriptionName>(GoogleFriendlyTopicName, APIClientVar, SubscriptionNameVar));

                return true;
            }
        }

        private bool EnsureTopicExistence(TopicName _TopicInstance, PublisherServiceApiClient _PublisherAPIClient = null, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                if (_PublisherAPIClient == null)
                {
                    _PublisherAPIClient = PublisherServiceApiClient.Create();
                }
                _PublisherAPIClient.CreateTopic(_TopicInstance);
            }
            catch (Exception e)
            {
                if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.AlreadyExists)
                {
                    //That is fine.
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BPubSubServiceGC->EnsureTopicExistence: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }
            return true;
        }
        private bool DeleteTopic(TopicName _TopicInstance, Action<string> _ErrorMessageAction)
        {
            var PublisherAPIClient = PublisherServiceApiClient.Create();
            try
            {
                PublisherAPIClient.DeleteTopic(_TopicInstance);
            }
            catch (Exception e)
            {
                if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.NotFound)
                {
                    //That is fine.
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BPubSubServiceGC->DeleteTopic: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }
            return true;
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

        /// <summary>
        ///
        /// <para>Subscribe:</para>
        /// 
        /// <para>Subscribes to given workspace [_QueryParameters.Domain]:[_QueryParameters.SubDomain]:[_QueryParameters.Identifier] topic</para>
        /// 
        /// <para>Check <seealso cref="IBPubSubServiceInterface.Subscribe"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool Subscribe(
            BMemoryQueryParameters _QueryParameters,
            Action<string, JObject> _OnMessage,
            Action<string> _ErrorMessageAction = null)
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
                    _ErrorMessageAction?.Invoke("BPubSubServiceGC->Subscribe->Callback: " + e.Message + ", Trace: " + e.StackTrace);
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
        public bool Publish(
            BMemoryQueryParameters _QueryParameters,
            JObject _Message,
            Action<string> _ErrorMessageAction = null)
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
        public bool CustomSubscribe(
            string _CustomTopic,
            Action<string, string> _OnMessage,
            Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0 && _OnMessage != null)
            {
                _CustomTopic = GetGoogleFriendlyTopicName(_CustomTopic);

                if (GetSubscriber(out SubscriberServiceApiClient APIClientVar, out SubscriptionName SubscriptionNameVar, _CustomTopic, _ErrorMessageAction))
                {
                    var SubscriptionCancellationVar = new BValue<bool>(false, EBProducerStatus.MultipleProducer);
                    var SubscriptionThread = new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        while (!SubscriptionCancellationVar.Get())
                        {
                            PullResponse Response = null;
                            try
                            {
                                Response = APIClientVar.Pull(SubscriptionNameVar, true, 1000);
                            }
                            catch (Exception e)
                            {
                                Response = null;

                                _ErrorMessageAction?.Invoke("BPubSubServiceGC->CustomSubscribe: " + e.Message + ", Trace: " + e.StackTrace);
                                if (e.InnerException != null && e.InnerException != e)
                                {
                                    _ErrorMessageAction?.Invoke("BPubSubServiceGC->CustomSubscribe->Inner: " + e.InnerException.Message + ", Trace: " + e.StackTrace);
                                }
                            }

                            if (Response == null || Response.ReceivedMessages == null || !Response.ReceivedMessages.Any())
                            {
                                Thread.Sleep(1000);
                                continue;
                            }

                            var MessageContainers = Response.ReceivedMessages.ToArray();
                            if (MessageContainers != null && MessageContainers.Length > 0)
                            {
                                var AckArray = new List<string>();

                                foreach (var MessageContainer in MessageContainers)
                                {
                                    if (MessageContainer != null)
                                    {
                                        if (!AckArray.Contains(MessageContainer.AckId))
                                        {
                                            AckArray.Add(MessageContainer.AckId);
                                        }

                                        string Topic = GetTopicNameFromGoogleFriendlyName(_CustomTopic);
                                        string Data = MessageContainer.Message.Data.ToStringUtf8();

                                        if (UniqueMessageDeliveryEnsurer != null)
                                        {
                                            UniqueMessageDeliveryEnsurer.Subscribe_ClearAndExtractTimestampFromMessage(ref Data, out string TimestampHash);

                                            if (UniqueMessageDeliveryEnsurer.Subscription_EnsureUniqueDelivery(Topic, TimestampHash, _ErrorMessageAction))
                                            {
                                                _OnMessage?.Invoke(Topic, Data);
                                            }
                                        }
                                        else
                                        {
                                            _OnMessage?.Invoke(Topic, Data);
                                        }
                                    }
                                }
                                
                                try
                                {
                                    APIClientVar.Acknowledge(SubscriptionNameVar, AckArray);
                                }
                                catch (Exception e)
                                {
                                    if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.InvalidArgument)
                                    {
                                        //That is fine, probably due to previous subscriptions
                                    }
                                    else
                                    {
                                        _ErrorMessageAction?.Invoke("BPubSubServiceGC->CustomSubscribe: " + e.Message + ", Trace: " + e.StackTrace);
                                        if (e.InnerException != null && e.InnerException != e)
                                        {
                                            _ErrorMessageAction?.Invoke("BPubSubServiceGC->CustomSubscribe->Inner: " + e.InnerException.Message + ", Trace: " + e.StackTrace);
                                        }
                                    }
                                }
                            }
                        }
                        
                    });
                    SubscriptionThread.Start();

                    lock (SubscriberThreadsDictionaryLock)
                    {
                        SubscriberThreadsDictionary.Add(SubscriptionNameVar, new BTuple<Thread, BValue<bool>>(SubscriptionThread, SubscriptionCancellationVar));
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
        public bool CustomPublish(
            string _CustomTopic,
            string _CustomMessage,
            Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0
                && _CustomMessage != null && _CustomMessage.Length > 0)
            {
                var CustomTopicGoogleFriendly = GetGoogleFriendlyTopicName(_CustomTopic);

                string TimestampHash = null;
                UniqueMessageDeliveryEnsurer?.Publish_PrependTimestampToMessage(ref _CustomMessage, out TimestampHash);

                ByteString MessageByteString = null;
                try
                {
                    MessageByteString = ByteString.CopyFromUtf8(_CustomMessage);
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BPubSubServiceGC->CustomPublish: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }

                var TopicInstance = new TopicName(ProjectID, CustomTopicGoogleFriendly);

                var MessageContent = new PubsubMessage()
                {
                    Data = MessageByteString
                };

                if (GetPublisher(out PublisherServiceApiClient Client, TopicInstance, _ErrorMessageAction))
                {
                    try
                    {
                        if (UniqueMessageDeliveryEnsurer != null)
                        {
                            if (UniqueMessageDeliveryEnsurer.Publish_EnsureUniqueDelivery(_CustomTopic, TimestampHash, _ErrorMessageAction))
                            {
                                using (var CreatedTask = Client.PublishAsync(TopicInstance, new PubsubMessage[] { MessageContent }))
                                {
                                    CreatedTask.Wait();
                                }
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("BPubSubServiceGC->CustomPublish: UniqueMessageDeliveryEnsurer has failed.");
                                return false;
                            }
                        }
                        else
                        {
                            using (var CreatedTask = Client.PublishAsync(TopicInstance, new PubsubMessage[] { MessageContent }))
                            {
                                CreatedTask.Wait();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.NotFound)
                        {
                            lock (PublisherTopicDictionaryLock)
                            {
                                PublisherTopicDictionary.Remove(TopicInstance.TopicId);
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("BPubSubServiceGC->CustomPublish: " + e.Message + ", Trace: " + e.StackTrace);
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
        public void DeleteTopicGlobally(
            BMemoryQueryParameters _QueryParameters,
            Action<string> _ErrorMessageAction = null)
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
        public void DeleteCustomTopicGlobally(
            string _CustomTopic,
            Action<string> _ErrorMessageAction = null)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0)
            {
                _CustomTopic = GetGoogleFriendlyTopicName(_CustomTopic);

                var TopicsToBeDeleted = new List<string>();
                var SubscriptionToBeRemoved = new List<BTuple<SubscriberServiceApiClient, SubscriptionName>>();
                var IndicesToBeRemoved = new List<int>();

                lock (SubscriberTopicListLock)
                {
                    int i = 0;
                    foreach (var SubscriberTopic in SubscriberTopicList)
                    {
                        if (SubscriberTopic != null && SubscriberTopic.Item1 == _CustomTopic)
                        {
                            if (!TopicsToBeDeleted.Contains(_CustomTopic))
                            {
                                TopicsToBeDeleted.Add(_CustomTopic);
                            }
                            
                            SubscriptionToBeRemoved.Add(new BTuple<SubscriberServiceApiClient, SubscriptionName>(
                                SubscriberTopic.Item2,
                                SubscriberTopic.Item3));

                            IndicesToBeRemoved.Add(i);
                        }
                        i++;
                    }

                    for (int j = (IndicesToBeRemoved.Count - 1); j >= 0; j--)
                    {
                        SubscriberTopicList.RemoveAt(IndicesToBeRemoved[j]);
                    }

                    foreach (var Current in SubscriptionToBeRemoved)
                    {
                        if (Current != null && Current.Item2 != null)
                        {
                            if (Current.Item2 != null)
                            {
                                lock (SubscriberThreadsDictionaryLock)
                                {
                                    if (SubscriberThreadsDictionary.ContainsKey(Current.Item2))
                                    {
                                        var SubscriberThread = SubscriberThreadsDictionary[Current.Item2];
                                        if (SubscriberThread != null)
                                        {
                                            SubscriberThread.Item2.Set(true);
                                        }
                                        SubscriberThreadsDictionary.Remove(Current.Item2);
                                    }
                                }
                                try
                                {
                                    Current.Item1?.DeleteSubscription(Current.Item2);
                                }
                                catch (Exception e)
                                {
                                    if (e is RpcException && (e as RpcException).Status.StatusCode == StatusCode.NotFound)
                                    {
                                        //That is fine.
                                    }
                                    else
                                    {
                                        _ErrorMessageAction?.Invoke("BPubSubServiceGC->DeleteCustomTopicGlobally: " + e.Message + ", Trace: " + e.StackTrace);
                                        if (e.InnerException != null && e.InnerException != e)
                                        {
                                            _ErrorMessageAction?.Invoke("BPubSubServiceGC->DeleteCustomTopicGlobally->Inner: " + e.InnerException.Message + ", Trace: " + e.StackTrace);
                                        }
                                    }
                                }
                            }
                        }
                    
                        lock (PublisherTopicDictionaryLock)
                        {
                            if (PublisherTopicDictionary.ContainsKey(_CustomTopic))
                            {
                                if (!TopicsToBeDeleted.Contains(_CustomTopic))
                                {
                                    TopicsToBeDeleted.Add(_CustomTopic);
                                }
                                PublisherTopicDictionary.Remove(_CustomTopic);
                            }

                            foreach (var Topic in TopicsToBeDeleted)
                            {
                                if (Topic != null)
                                {
                                    DeleteTopic(new TopicName(ProjectID, Topic), _ErrorMessageAction);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}