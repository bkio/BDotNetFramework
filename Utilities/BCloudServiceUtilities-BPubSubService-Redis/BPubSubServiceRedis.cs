/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace BCloudServiceUtilities.PubSubServices
{
    public class BPubSubServiceRedis : BRedisCommonFunctionalities, IBPubSubServiceInterface
    {
        /// <summary>
        /// 
        /// <para>BPubSubServiceRedis: Parametered Constructor</para>
        /// <para>Note: Redis Pub/Sub service does not keep messages in a permanent queue, therefore if there is not any listener, message will be lost, unlike other Pub/Sub services.</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_RedisEndpoint"/>                 Redis Endpoint without Port</para>
        /// <para><paramref name="_RedisPort"/>                     Redis Endpoint Port</para>
        /// <para><paramref name="_RedisPassword"/>                 Redis Server Password</para>
        /// 
        /// </summary>
        public BPubSubServiceRedis(
            string _RedisEndpoint,
            int _RedisPort,
            string _RedisPassword,
            bool _bFailoverMechanismEnabled = true,
            Action<string> _ErrorMessageAction = null) : base("BPubSubServiceRedis", _RedisEndpoint, _RedisPort, _RedisPassword, false, _bFailoverMechanismEnabled,  _ErrorMessageAction)
        {
        }

        /// <summary>
        /// 
        /// <para>BPubSubServiceRedis: Parametered Constructor</para>
        /// <para>Note: Redis Pub/Sub service does not keep messages in a permanent queue, therefore if there is not any listener, message will be lost, unlike other Pub/Sub services.</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_RedisEndpoint"/>                 Redis Endpoint without Port</para>
        /// <para><paramref name="_RedisPort"/>                     Redis Endpoint Port</para>
        /// <para><paramref name="_RedisPassword"/>                 Redis Server Password</para>
        /// <para><paramref name="_RedisSslEnabled"/>               Redis Server SSL Connection Enabled/Disabled</para>
        /// 
        /// </summary>
        public BPubSubServiceRedis(
            string _RedisEndpoint,
            int _RedisPort,
            string _RedisPassword,
            bool _RedisSslEnabled,
            bool _bFailoverMechanismEnabled = true,
            Action<string> _ErrorMessageAction = null) : base("BPubSubServiceRedis", _RedisEndpoint, _RedisPort, _RedisPassword, _RedisSslEnabled, _bFailoverMechanismEnabled, _ErrorMessageAction)
        {
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
                    _ErrorMessageAction?.Invoke("BPubSubServiceRedis->Subscribe->Callback: " + e.Message + ", Trace: " + e.StackTrace);
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
            Action<string> _ErrorMessageAction = null, bool _SubscribeSingleMessage = false)
        {
            if (_CustomTopic != null && _CustomTopic.Length > 0 && _OnMessage != null)
            {
                FailoverCheck();
                try
                {
                    RedisConnection.GetSubscriber().Subscribe(
                        _CustomTopic,
                        (RedisChannel Channel, RedisValue Value) =>
                        {
                            if (UniqueMessageDeliveryEnsurer != null)
                            {
                                var Message = Value.ToString();

                                UniqueMessageDeliveryEnsurer.Subscribe_ClearAndExtractTimestampFromMessage(ref Message, out string TimestampHash);

                                if (UniqueMessageDeliveryEnsurer.Subscription_EnsureUniqueDelivery(Channel, TimestampHash, _ErrorMessageAction))
                                {
                                    _OnMessage?.Invoke(Channel, Message);
                                }
                            }
                            else
                            {
                                _OnMessage?.Invoke(Channel, Value.ToString());
                            }
                        });
                }
                catch (Exception e)
                {
                    if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        return CustomSubscribe(_CustomTopic, _OnMessage, _ErrorMessageAction);
                    }

                    _ErrorMessageAction?.Invoke("BPubSubServiceRedis->CustomSubscribe: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
                return true;
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
                FailoverCheck();
                try
                {
                    if (UniqueMessageDeliveryEnsurer != null)
                    {
                        UniqueMessageDeliveryEnsurer.Publish_PrependTimestampToMessage(ref _CustomMessage, out string TimestampHash);

                        if (UniqueMessageDeliveryEnsurer.Publish_EnsureUniqueDelivery(_CustomTopic, TimestampHash, _ErrorMessageAction))
                        {
                            using (var CreatedTask = RedisConnection.GetDatabase().PublishAsync(_CustomTopic, _CustomMessage))
                            {
                                CreatedTask.Wait();
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("BPubSubServiceRedis->CustomPublish: UniqueMessageDeliveryEnsurer has failed.");
                            return false;
                        }
                    }
                    else
                    {
                        using (var CreatedTask = RedisConnection.GetDatabase().PublishAsync(_CustomTopic, _CustomMessage))
                        {
                            CreatedTask.Wait();
                        }
                    }
                }
                catch (Exception e)
                {
                    if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        return CustomPublish(_CustomTopic, _CustomMessage, _ErrorMessageAction);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceRedis->CustomPublish: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }
                }
                return true;
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
                try
                {
                    RedisConnection.GetSubscriber().Unsubscribe(_CustomTopic, null);
                }
                catch (Exception e)
                {
                    if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        DeleteCustomTopicGlobally(_CustomTopic, _ErrorMessageAction);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("BPubSubServiceRedis->DeleteTopicGlobally: " + e.Message + ", Trace: " + e.StackTrace);
                    }
                }
            }
        }
    }
}