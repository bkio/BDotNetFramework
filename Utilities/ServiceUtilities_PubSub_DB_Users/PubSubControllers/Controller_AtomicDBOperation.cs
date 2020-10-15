/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Threading;
using BCloudServiceUtilities;
using BCommonUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServiceUtilities
{
    public class Controller_AtomicDBOperation
    {
        private class PubSubTimeoutSubscriber : IPubSubSubscriberInterface
        {
            private readonly Action<Action_OperationTimeout> OnNotificationReceivedAction;

            private PubSubTimeoutSubscriber() {}
            public PubSubTimeoutSubscriber(Action<Action_OperationTimeout> _OnNotificationReceivedAction)
            {
                OnNotificationReceivedAction = _OnNotificationReceivedAction;
            }

            public void OnMessageReceived(JObject _Message)
            {
                OnNotificationReceivedAction?.Invoke(JsonConvert.DeserializeObject<Action_OperationTimeout>(_Message.ToString()));
            }
        }

        private static Controller_AtomicDBOperation Instance = null;
        private Controller_AtomicDBOperation() { }
        public static Controller_AtomicDBOperation Get()
        {
            if (Instance == null)
            {
                Instance = new Controller_AtomicDBOperation();
            }
            return Instance;
        }

        private IBMemoryServiceInterface MemoryService;
        private BMemoryQueryParameters QueryParameters;
        public void SetMemoryService(IBMemoryServiceInterface _MemoryService, BMemoryQueryParameters _QueryParameters)
        {
            MemoryService = _MemoryService;
            QueryParameters = _QueryParameters;
        }

        private const string ATOMIC_DB_OP_CTRL_MEM_PREFIX = "atomic-db-op-mem-check-";
        private const int TIMEOUT_TRIAL_SECONDS = 10;

        //In case of false return, operation shall be cancelled with an internal error.
        public bool GetClearanceForDBOperation(WebServiceBaseTimeoutableProcessor _ServiceProcessor, string _DBTableName, string _Identifier, Action<string> _ErrorMessageAction = null)
        {
            if (!_ServiceProcessor.IsDoNotGetDBClearanceSet()) return true;

            var CreatedAction = new Action_OperationTimeout(_DBTableName, _Identifier);
            lock (_ServiceProcessor.RelevantTimeoutStructures)
            {
                bool bFound = false;
                foreach (var CTS in _ServiceProcessor.RelevantTimeoutStructures)
                {
                    if (CTS.Equals(CreatedAction))
                    {
                        CreatedAction = CTS;
                        bFound = true;
                        break;
                    }
                }
                if (!bFound)
                {
                    _ServiceProcessor.RelevantTimeoutStructures.Add(CreatedAction);
                }
            }

            var MemoryEntryValue = ATOMIC_DB_OP_CTRL_MEM_PREFIX + _DBTableName + "-" + _Identifier;

            if (_ServiceProcessor.IsUseQueueSetClearanceActionsSet() 
                && _ServiceProcessor.TryRemoveSetClearanceAwaitItem(MemoryEntryValue)) return true;

            int TrialCounter = 0;

            bool bResult;
            do
            {
                bResult = MemoryService.SetKeyValueConditionally(
                    QueryParameters, 
                    new Tuple<string, BPrimitiveType>(MemoryEntryValue, new BPrimitiveType("busy")), 
                    _ErrorMessageAction);

                if (!bResult) Thread.Sleep(1000);
            }
            while (!bResult && TrialCounter++ < TIMEOUT_TRIAL_SECONDS);

            if (TrialCounter >= TIMEOUT_TRIAL_SECONDS)
            {
                _ErrorMessageAction?.Invoke("Atomic DB Operation Controller->GetClearanceForDBOperation: A timeout has occured for operation type " + _DBTableName + ", for ID " + _Identifier + ", existing operation has been overriden by the new request.");

                Manager_PubSubService.Get().PublishAction(Actions.EAction.ACTION_OPERATION_TIMEOUT, JsonConvert.SerializeObject(new Action_OperationTimeout()
                {
                    TableName = _DBTableName,
                    EntryKey = _Identifier
                }),
                _ErrorMessageAction);

                //Timeout for other operation has occured.
                return MemoryService.SetKeyValue(QueryParameters, new Tuple<string, BPrimitiveType>[]
                {
                    new Tuple<string, BPrimitiveType>(MemoryEntryValue, new BPrimitiveType("busy"))
                }, 
                _ErrorMessageAction);
            }

            return true;
        }

        public void WaitUntilSetClearancesCompleted(WebServiceBaseTimeoutableProcessor _ServiceProcessor, Action<string> _ErrorMessageAction)
        {
            var SetClearanceItems = _ServiceProcessor.GetAndEmptySetClearanceAwaitItems();
            if (SetClearanceItems != null)
            {
                foreach (var Item in SetClearanceItems)
                {
                    MemoryService.DeleteKey(QueryParameters, Item, _ErrorMessageAction);
                }
            }
        }

        public void SetClearanceForDBOperationForOthers(WebServiceBaseTimeoutableProcessor _ServiceProcessor, string _DBTableName, string _Identifier, Action<string> _ErrorMessageAction = null)
        {
            if (!_ServiceProcessor.IsDoNotGetDBClearanceSet()) return;

            var MemoryEntryValue = ATOMIC_DB_OP_CTRL_MEM_PREFIX + _DBTableName + "-" + _Identifier;

            if (_ServiceProcessor.IsUseQueueSetClearanceActionsSet())
            {
                _ServiceProcessor.AddSetClearanceAwaitItem(MemoryEntryValue);
            }
            else if (!MemoryService.DeleteKey(QueryParameters, MemoryEntryValue, _ErrorMessageAction))
            {
                _ErrorMessageAction?.Invoke("Atomic DB Operation Controller->SetClearanceForDBOperationForOthers: DeleteKey failed for operation type " + _DBTableName + ", for ID " + _Identifier);
            }
        }

        private PubSubTimeoutSubscriber Subscriber = null;
        public void StartTimeoutCheckOperation(Action<Action_OperationTimeout> _OnNotificationReceivedAction)
        {
            Subscriber = new PubSubTimeoutSubscriber(_OnNotificationReceivedAction);
            Manager_PubSubService.Get().AddSubscriber(Subscriber, Actions.EAction.ACTION_OPERATION_TIMEOUT);
        }
    }
}