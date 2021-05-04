/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;

namespace BCloudServiceUtilities
{
    public class BPubSubUniqueMessageDeliveryEnsurer
    {
        private readonly IBMemoryServiceInterface EnsurerMemoryService;
        private readonly IBPubSubServiceInterface PubSubService;

        public BPubSubUniqueMessageDeliveryEnsurer(
            IBMemoryServiceInterface _EnsurerMemoryService,
            IBPubSubServiceInterface _PubSubService)
        {
            EnsurerMemoryService = _EnsurerMemoryService;
            PubSubService = _PubSubService;
        }

        public bool Subscription_EnsureUniqueDelivery(
            string _Topic,
            string _TimestampHash,
            Action<string> _ErrorMessageAction)
        {
            if (_TimestampHash != null && EnsurerMemoryService != null && PubSubService != null && BCommonUtilities.BUtility.CalculateStringMD5(_Topic, out string HashedTopic, _ErrorMessageAction))
            {
                var QueryParameters = new BMemoryQueryParameters()
                {
                    Domain = "PubSubEnsureUniqueDelivery",
                    SubDomain = HashedTopic,
                    Identifier = _TimestampHash
                };
                var bSuccessfullyDeleted = EnsurerMemoryService.DeleteKey(QueryParameters, "Publish", _ErrorMessageAction, false /* Do not publish */);
                return bSuccessfullyDeleted;
            }
            return true;
        }

        public bool Publish_EnsureUniqueDelivery(
            string _Topic,
            string _TimestampHash,
            Action<string> _ErrorMessageAction)
        {
            if (EnsurerMemoryService != null && PubSubService != null && BCommonUtilities.BUtility.CalculateStringMD5(_Topic, out string HashedTopic, _ErrorMessageAction))
            {
                var QueryParameters = new BMemoryQueryParameters()
                {
                    Domain = "PubSubEnsureUniqueDelivery",
                    SubDomain = HashedTopic,
                    Identifier = _TimestampHash
                };

                if (EnsurerMemoryService.SetKeyValue(QueryParameters, new Tuple<string, BCommonUtilities.BPrimitiveType>[] { new Tuple<string, BCommonUtilities.BPrimitiveType>("Publish", new BCommonUtilities.BPrimitiveType("Message")) }, _ErrorMessageAction, false /* Do not publish */))
                {
                    EnsurerMemoryService.SetKeyExpireTime(QueryParameters, TimeSpan.FromSeconds(60), _ErrorMessageAction);
                }
            }
            return true;
        }

        public void Publish_PrependTimestampToMessage(ref string _MessageRef, out string _TimestampHash)
        {
            if (!_MessageRef.StartsWith("[[") 
                && BCommonUtilities.BUtility.CalculateStringMD5(DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds.ToString(), out _TimestampHash))
            {
                var Tmp = "[[" + _TimestampHash + "]]" + _MessageRef;
                _MessageRef = Tmp;
            }
            else
            {
                _TimestampHash = null;
            }
        }

        public void Subscribe_ClearAndExtractTimestampFromMessage(ref string MessageRef, out string Timestamp)
        {
            var IndexOfLastDelimiter = MessageRef.IndexOf("]]");
            if (MessageRef.StartsWith("[[") && IndexOfLastDelimiter != -1)
            {
                Timestamp = MessageRef.Substring(2, IndexOfLastDelimiter - 2);
                MessageRef = MessageRef.Substring(IndexOfLastDelimiter + 2);
            }
            else
            {
                Timestamp = null;
            }
        }
    }

    /// <summary>
    /// Interface for abstracting Pub/Sub Services
    /// </summary>
    public interface IBPubSubServiceInterface
    {
        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        bool HasInitializationSucceed();

        /// <summary>
        /// 
        /// <para>EnsureUniqueMessageDelivery</para>
        /// 
        /// <para>Sets up the unique message delivery ensurer</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_EnsurerMemoryService"/>          Given memory service will be used for ensuring that received messages are processed only once</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void EnsureUniqueMessageDelivery(
            IBMemoryServiceInterface _EnsurerMemoryService,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>Subscribe</para>
        /// 
        /// <para>Subscribes to given workspace [_Domain]:[_SubDomain] topic</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_QueryParameters"/>               Parameters need to be provided for running performing this operation</para>
        /// <para><paramref name="_OnMessage"/>                     Retrieved messages will be pushed to this action</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool Subscribe(
            BMemoryQueryParameters _QueryParameters,
            Action<string, Newtonsoft.Json.Linq.JObject> _OnMessage,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>Publish</para>
        /// 
        /// <para>Publishes the given message to given workspace [_Domain]:[_SubDomain] topic</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_QueryParameters"/>               Parameters need to be provided for running performing this operation</para>
        /// <para><paramref name="_Message"/>                       Message to be sent</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool Publish(
            BMemoryQueryParameters _QueryParameters,
            Newtonsoft.Json.Linq.JObject _Message,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>CustomSubscribe</para>
        /// 
        /// <para>Subscribes to given custom topic</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_CustomTopic"/>               Topic to be subscribed to</para>
        /// <para><paramref name="_OnMessage"/>                 Retrieved messages will be pushed to this action</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool CustomSubscribe(
            string _CustomTopic,
            Action<string, string> _OnMessage,
            Action<string> _ErrorMessageAction = null,
            bool _SubscribeSingleMessage = false);

        /// <summary>
        /// 
        /// <para>CustomPublish</para>
        /// 
        /// <para>Publishes the given message to given custom topic</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_CustomTopic"/>               Topic to be pushed to</para>
        /// <para><paramref name="_CustomMessage"/>             Message to be sent</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool CustomPublish(
            string _CustomTopic,
            string _CustomMessage,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>DeleteCustomTopicGlobally</para>
        /// 
        /// <para>Deletes all messages and the topic of given workspace</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_CustomTopic"/>               Topic to be unsubscribed from</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void DeleteCustomTopicGlobally(
            string _CustomTopic,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>DeleteTopicGlobally</para>
        /// 
        /// <para>Deletes all messages and the topic of given workspace [_Domain]:[_SubDomain] topic</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_QueryParameters"/>           Parameters need to be provided for running performing this operation</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        void DeleteTopicGlobally(
            BMemoryQueryParameters _QueryParameters,
            Action<string> _ErrorMessageAction = null);
    }
}