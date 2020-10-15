/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using BCloudServiceUtilities;
using BCommonUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServiceUtilities
{
    public class Controller_DeliveryEnsurer
    {
        private const int MAX_FAILED_QUERY_RETRY_COUNTS = 5;
        private const int MAX_PUBLISH_FAILURE_TRIAL_COUNTS = 10;

        private static Controller_DeliveryEnsurer Instance = null;
        private Controller_DeliveryEnsurer() 
        {
            BackgroundQuerySenderThread.Start(this);
        }
        ~Controller_DeliveryEnsurer()
        {
            try
            {
                BackgroundProcessingQueueEvent.Close();
            }
            catch (Exception) { }
        }
        public static Controller_DeliveryEnsurer Get()
        {
            if (Instance == null)
            {
                Instance = new Controller_DeliveryEnsurer();
            }
            return Instance;
        }

        private IBFileServiceInterface FileService = null;
        public void SetFileService(IBFileServiceInterface _FileService)
        {
            FileService = _FileService;
        }

        private IBDatabaseServiceInterface DatabaseService = null;
        public void SetDatabaseService(IBDatabaseServiceInterface _DatabaseService)
        {
            DatabaseService = _DatabaseService;
        }

        public void WaitUntilActionsCompleted(HttpListenerContext _Context, Action<string> _ErrorMessageAction = null)
        {
            if (ErrorMessageAction == null) ErrorMessageAction = _ErrorMessageAction;
            if (ActionsDictionary.TryRemove(_Context, out ConcurrentQueue<Action_DeliveryEnsurer> DeliveryEnsurerActions))
            {
                var WaitForEvent = new ManualResetEvent(false);
                BackgroundProcessingQueue.Enqueue(new Tuple<ManualResetEvent, ConcurrentQueue<Action_DeliveryEnsurer>>(WaitForEvent, DeliveryEnsurerActions));
                lock (BackgroundProcessingQueueEvent)
                {
                    try
                    {
                        BackgroundProcessingQueueEvent.Set();
                    }
                    catch (Exception) { }
                }

                try
                {
                    WaitForEvent.WaitOne();
                    WaitForEvent.Close();
                }
                catch (Exception) { }
            }
        }
        private Action<string> ErrorMessageAction = null;
        private readonly ConcurrentQueue<Tuple<ManualResetEvent, ConcurrentQueue<Action_DeliveryEnsurer>>> BackgroundProcessingQueue = new ConcurrentQueue<Tuple<ManualResetEvent, ConcurrentQueue<Action_DeliveryEnsurer>>>();
        private readonly ConcurrentDictionary<HttpListenerContext, ConcurrentQueue<Action_DeliveryEnsurer>> ActionsDictionary = new ConcurrentDictionary<HttpListenerContext, ConcurrentQueue<Action_DeliveryEnsurer>>();
        private readonly ManualResetEvent BackgroundProcessingQueueEvent = new ManualResetEvent(false);

        private static void BackgroundQuerySenderRunnable(object _ControllerRef)
        {
            Thread.CurrentThread.IsBackground = true;

            var Self = _ControllerRef as Controller_DeliveryEnsurer;
            try
            {
                do
                {
                    try
                    {
                        Self.BackgroundProcessingQueueEvent.WaitOne();
                    }
                    catch (Exception) { }

                    lock (Self.BackgroundProcessingQueueEvent)
                    {
                        try
                        {
                            Self.BackgroundProcessingQueueEvent.Reset();
                        }
                        catch (Exception) { }
                    }

                    while (Self.BackgroundProcessingQueue.TryDequeue(out Tuple<ManualResetEvent, ConcurrentQueue<Action_DeliveryEnsurer>> DeliveryEnsurerActions))
                    {
                        var CurrentSetEvent = DeliveryEnsurerActions.Item1;
                        var CurrentQueue = DeliveryEnsurerActions.Item2;
                        BTaskWrapper.Run(() =>
                        {
                            while (CurrentQueue.TryDequeue(out Action_DeliveryEnsurer DeliveryEnsurerAction))
                            {
                                //Running in parallel caused: "too much contention on these datastore entities. please try again."
                                //BTaskWrapper.Run(() =>
                                //{
                                Self.ProcessActionInternally(DeliveryEnsurerAction, Self.ErrorMessageAction);
                                //});
                            }
                            try
                            {
                                CurrentSetEvent.Set();
                            }
                            catch (Exception) { }
                        });
                    }

                } while (true);
            }
            catch (Exception) { }
        }
        private readonly Thread BackgroundQuerySenderThread = new Thread(BackgroundQuerySenderRunnable);

        private string ServiceName;
        private Actions.EAction ActionServiceIdentifier;
        public void SetServiceIdentifier(string _ServiceName, Actions.EAction _ActionServiceIdentifier)
        {
            ServiceName = _ServiceName;
            ActionServiceIdentifier = _ActionServiceIdentifier;
        }
        public Actions.EAction GetActionServiceIdentifier()
        {
            return ActionServiceIdentifier;
        }

        public void Retry_FireAndForget_Operation(HttpListenerContext _Context, Action_DeliveryEnsurer _Action, Action<string> _ErrorMessageAction = null)
        {
            if (++_Action.RetryCount > MAX_FAILED_QUERY_RETRY_COUNTS)
            {
                //Stop retrying and record the failed operation

                if (FailedDeliveryEnsurerOperationEntry.GenerateOperationTimestampID(out string OperationTimestampID, _ErrorMessageAction))
                {
                    DB_AddElementsToArrayItem_FireAndForget(
                        _Context,
                       FailedDeliveryEnsurerOperationEntry.DBSERVICE_FAILED_DELIVERY_ENSURER_OPERATIONS_TABLE_PREFIX + ServiceName.ToLower(),
                       FailedDeliveryEnsurerOperationEntry.KEY_NAME_OPERATION_TIMESTAMP_ID,
                       new BPrimitiveType(OperationTimestampID),
                       "operations",
                       new BPrimitiveType[]
                       {
                           new BPrimitiveType(JsonConvert.SerializeObject(new FailedDeliveryEnsurerOperationEntry()
                           {
                               OperationStringified = JsonConvert.SerializeObject(_Action)
                           })) 
                       });
                }
                else
                {
                    _ErrorMessageAction?.Invoke("Retry_FireAndForget_Operation: Function has failed to record the failed operation. Serialized lost action: " + JsonConvert.SerializeObject(_Action));
                }
                return;
            }

            ProcessActionInternally(_Action, _ErrorMessageAction);
        }

        private void ProcessActionInternally(Action_DeliveryEnsurer _Action, Action<string> _ErrorMessageAction = null)
        {
            switch (_Action.QueryType)
            {
                //FS
                case Action_DeliveryEnsurer.QUERY_TYPE_FS_DELETE_FILE:
                    {
                        var Casted = (Action_DeliveryEnsurer_FS_DeleteFile)_Action;

                        FS_DeleteFile_FireAndForget_Internal(Casted.BucketName, Casted.KeyName, Casted.RetryCount, _ErrorMessageAction);
                        break;
                    }
                case Action_DeliveryEnsurer.QUERY_TYPE_FS_DELETE_FOLDER:
                    {
                        var Casted = (Action_DeliveryEnsurer_FS_DeleteFolder)_Action;

                        FS_DeleteFolder_FireAndForget_Internal(Casted.BucketName, Casted.KeyName, Casted.RetryCount, _ErrorMessageAction);
                        break;
                    }

                //DB
                case Action_DeliveryEnsurer.QUERY_TYPE_DB_UPDATE_ITEM:
                case Action_DeliveryEnsurer.QUERY_TYPE_DB_PUT_ITEM:
                    {
                        var Casted = (Action_DeliveryEnsurer_DB_UpdateOrPutItem)_Action;

                        DB_UpdateOrPutItem_FireAndForget_Internal(
                            Casted.QueryType,
                            Casted.TableName,
                            Casted.KeyName,
                            Casted.KeyValue.GetKeyValuePrimitiveReference(),
                            JObject.Parse(Casted.UpdateItemStringified),
                            Casted.RetryCount,
                            _ErrorMessageAction);
                        break;
                    }
                case Action_DeliveryEnsurer.QUERY_TYPE_DB_DELETE_ITEM:
                    {
                        var Casted = (Action_DeliveryEnsurer_DB_DeleteItem)_Action;

                        DB_DeleteItem_FireAndForget_Internal(
                            Casted.TableName,
                            Casted.KeyName,
                            Casted.KeyValue.GetKeyValuePrimitiveReference(),
                            Casted.RetryCount,
                            _ErrorMessageAction);
                        break;
                    }
                case Action_DeliveryEnsurer.QUERY_TYPE_DB_ADD_ELEMENTS_TO_ARRAY_ITEM:
                case Action_DeliveryEnsurer.QUERY_TYPE_DB_REMOVE_ELEMENTS_FROM_ARRAY_ITEM:
                    {
                        var Casted = (Action_DeliveryEnsurer_DB_Add_Remove_ElementsToArrayItem)_Action;
                        var ValueEntries = new List<BPrimitiveType>();
                        foreach (var JEntry in Casted.ElementValueEntries)
                        {
                            ValueEntries.Add(JEntry.GetKeyValuePrimitiveReference());
                        }

                        DB_AddOrRemoveElementsFromArrayItem_FireAndForget_Internal(
                            Casted.QueryType,
                            Casted.TableName,
                            Casted.KeyName,
                            Casted.KeyValue.GetKeyValuePrimitiveReference(),
                            Casted.ElementName,
                            ValueEntries.ToArray(),
                            Casted.RetryCount,
                            _ErrorMessageAction);
                        break;
                    }
            }
        }

        public void BroadcastFailed_FireAndForget_Operation(Action_DeliveryEnsurer _Action, Action<string> _ErrorMessageAction = null)
        {
            Thread.Sleep(2000); //Cooldown
            PublishSerializedAction(JsonConvert.SerializeObject(_Action), ActionServiceIdentifier, _ErrorMessageAction);
        }

        public void PublishSerializedAction(string _SerializedAction, Actions.EAction _ActionServiceIdentifier, Action<string> _ErrorMessageAction = null)
        {
            bool bSuccess = false;
            int TrialCounter = 0;
            do
            {
                bSuccess = Manager_PubSubService.Get().PublishAction(
                    _ActionServiceIdentifier,
                    _SerializedAction,
                    _ErrorMessageAction);

                if (!bSuccess)
                {
                    Thread.Sleep(500);
                }
            }
            while (!bSuccess && TrialCounter++ < MAX_PUBLISH_FAILURE_TRIAL_COUNTS);

            if (!bSuccess)
            {
                _ErrorMessageAction?.Invoke("PublishSerializedAction: Manager_PubSubService.Get().PublishAction has failed " + MAX_PUBLISH_FAILURE_TRIAL_COUNTS + " times. Operation timed out.");
            }
        }

        //FS functions

        public void FS_DeleteFile_FireAndForget(
            HttpListenerContext _Context,
            string _BucketName,
            string _KeyName)
        {
            var RelevantQueue = new ConcurrentQueue<Action_DeliveryEnsurer>();
            if (!ActionsDictionary.TryAdd(_Context, RelevantQueue)) RelevantQueue = ActionsDictionary[_Context];
            RelevantQueue.Enqueue(new Action_DeliveryEnsurer_FS_DeleteFile()
            {
                QueryType = Action_DeliveryEnsurer.QUERY_TYPE_FS_DELETE_FILE,
                BucketName = _BucketName,
                KeyName = _KeyName,
                RetryCount = 0
            });
        }

        public void FS_DeleteFolder_FireAndForget(
            HttpListenerContext _Context,
            string _BucketName,
            string _Folder)
        {
            var RelevantQueue = new ConcurrentQueue<Action_DeliveryEnsurer>();
            if (!ActionsDictionary.TryAdd(_Context, RelevantQueue)) RelevantQueue = ActionsDictionary[_Context];
            RelevantQueue.Enqueue(new Action_DeliveryEnsurer_FS_DeleteFolder()
            {
                QueryType = Action_DeliveryEnsurer.QUERY_TYPE_FS_DELETE_FOLDER,
                BucketName = _BucketName,
                KeyName = _Folder,
                RetryCount = 0
            });
        }

        private void FS_DeleteFile_FireAndForget_Internal(
            string _BucketName,
            string _KeyName,
            int _CurrentRetryCount = 0,
            Action<string> _ErrorMessageAction = null)
        {
            if (FileService == null) return;

            if (!FileService.DeleteFile(_BucketName, _KeyName, _ErrorMessageAction))
            {
                if (FileService.CheckFileExistence(_BucketName, _KeyName, out bool bExists, _ErrorMessageAction) && bExists)
                {
                    BroadcastFailed_FireAndForget_Operation(new Action_DeliveryEnsurer_FS_DeleteFile()
                    {
                        QueryType = Action_DeliveryEnsurer.QUERY_TYPE_FS_DELETE_FILE,
                        BucketName = _BucketName,
                        KeyName = _KeyName,
                        RetryCount = _CurrentRetryCount
                    }, _ErrorMessageAction);
                }
            }
        }

        private void FS_DeleteFolder_FireAndForget_Internal(
            string _BucketName,
            string _Folder,
            int _CurrentRetryCount = 0,
            Action<string> _ErrorMessageAction = null)
        {
            if (FileService == null) return;

            if (!FileService.DeleteFolder(_BucketName, _Folder, _ErrorMessageAction))
            {
                if (FileService.CheckFileExistence(_BucketName, _Folder, out bool bExists, _ErrorMessageAction) && bExists)
                {
                    BroadcastFailed_FireAndForget_Operation(new Action_DeliveryEnsurer_FS_DeleteFolder()
                    {
                        QueryType = Action_DeliveryEnsurer.QUERY_TYPE_FS_DELETE_FOLDER,
                        BucketName = _BucketName,
                        KeyName = _Folder,
                        RetryCount = _CurrentRetryCount
                    }, _ErrorMessageAction);
                }
            }
        }

        //DB functions

        public void DB_UpdateItem_FireAndForget(
            HttpListenerContext _Context,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            JObject _UpdateItem)
        {
            DB_UpdateOrPutItem_FireAndForget(_Context, Action_DeliveryEnsurer.QUERY_TYPE_DB_UPDATE_ITEM, _Table, _KeyName, _KeyValue, _UpdateItem, 0);
        }

        public void DB_PutItem_FireAndForget(
            HttpListenerContext _Context,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            JObject _NewItem) 
        {
            DB_UpdateOrPutItem_FireAndForget(_Context, Action_DeliveryEnsurer.QUERY_TYPE_DB_PUT_ITEM, _Table, _KeyName, _KeyValue, _NewItem, 0);
        }

        private void DB_UpdateOrPutItem_FireAndForget(
            HttpListenerContext _Context,
            string _QueryType,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            JObject _UpdateItem,
            int _Current_Retry_Count)
        {
            var RelevantQueue = new ConcurrentQueue<Action_DeliveryEnsurer>();
            if (!ActionsDictionary.TryAdd(_Context, RelevantQueue)) RelevantQueue = ActionsDictionary[_Context];
            RelevantQueue.Enqueue(new Action_DeliveryEnsurer_DB_UpdateOrPutItem()
            {
                QueryType = _QueryType,
                TableName = _Table,
                KeyName = _KeyName,
                KeyValue = new BPrimitiveType_JStringified(_KeyValue),
                UpdateItemStringified = _UpdateItem.ToString(),
                RetryCount = _Current_Retry_Count
            });
        }

        private void DB_UpdateOrPutItem_FireAndForget_Internal(
            string _QueryType,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            JObject _UpdateItem,
            int _Current_Retry_Count,
            Action<string> _ErrorMessageAction = null)
        {
            if (DatabaseService == null) return;

            bool bResult = false;
            if (_QueryType == Action_DeliveryEnsurer.QUERY_TYPE_DB_UPDATE_ITEM)
                bResult = DatabaseService.UpdateItem(_Table, _KeyName, _KeyValue, _UpdateItem, out JObject _, EBReturnItemBehaviour.DoNotReturn, null, _ErrorMessageAction);
            else
                bResult = DatabaseService.PutItem(_Table, _KeyName, _KeyValue, _UpdateItem, out JObject _, EBReturnItemBehaviour.DoNotReturn, null, _ErrorMessageAction);

            if (!bResult)
            {
                BroadcastFailed_FireAndForget_Operation(new Action_DeliveryEnsurer_DB_UpdateOrPutItem()
                {
                    QueryType = _QueryType,
                    TableName = _Table,
                    KeyName = _KeyName,
                    KeyValue = new BPrimitiveType_JStringified(_KeyValue),
                    UpdateItemStringified = _UpdateItem.ToString(),
                    RetryCount = _Current_Retry_Count
                }, _ErrorMessageAction);
            }
        }

        public void DB_DeleteItem_FireAndForget(
            HttpListenerContext _Context,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue)
        {
            var RelevantQueue = new ConcurrentQueue<Action_DeliveryEnsurer>();
            if (!ActionsDictionary.TryAdd(_Context, RelevantQueue)) RelevantQueue = ActionsDictionary[_Context];
            RelevantQueue.Enqueue(new Action_DeliveryEnsurer_DB_DeleteItem()
            {
                QueryType = Action_DeliveryEnsurer.QUERY_TYPE_DB_DELETE_ITEM,
                TableName = _Table,
                KeyName = _KeyName,
                KeyValue = new BPrimitiveType_JStringified(_KeyValue),
                RetryCount = 0
            });
        }

        private void DB_DeleteItem_FireAndForget_Internal(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            int _Current_RetryCount = 0,
            Action<string> _ErrorMessageAction = null)
        {
            if (DatabaseService == null) return;

            if (!DatabaseService.DeleteItem(_Table, _KeyName, _KeyValue, out JObject _, EBReturnItemBehaviour.DoNotReturn, _ErrorMessageAction))
            {
                BroadcastFailed_FireAndForget_Operation(new Action_DeliveryEnsurer_DB_DeleteItem()
                {
                    QueryType = Action_DeliveryEnsurer.QUERY_TYPE_DB_DELETE_ITEM,
                    TableName = _Table,
                    KeyName = _KeyName,
                    KeyValue = new BPrimitiveType_JStringified(_KeyValue),
                    RetryCount = _Current_RetryCount
                }, _ErrorMessageAction);
            }
        }

        public void DB_AddElementsToArrayItem_FireAndForget(
            HttpListenerContext _Context,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            string _ElementName,
            BPrimitiveType[] _ElementValueEntries)
        {
            DB_AddOrRemoveElementsFromArrayItem_FireAndForget(
                _Context,
                Action_DeliveryEnsurer.QUERY_TYPE_DB_ADD_ELEMENTS_TO_ARRAY_ITEM,
                _Table,
                _KeyName,
                _KeyValue,
                _ElementName,
                _ElementValueEntries,
                0);
        }

        public void DB_RemoveElementsFromArrayItem_FireAndForget(
            HttpListenerContext _Context,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            string _ElementName,
            BPrimitiveType[] _ElementValueEntries)
        {
            DB_AddOrRemoveElementsFromArrayItem_FireAndForget(
                _Context,
                Action_DeliveryEnsurer.QUERY_TYPE_DB_REMOVE_ELEMENTS_FROM_ARRAY_ITEM,
                _Table,
                _KeyName,
                _KeyValue,
                _ElementName,
                _ElementValueEntries,
                0);
        }

        private void DB_AddOrRemoveElementsFromArrayItem_FireAndForget(
            HttpListenerContext _Context,
            string _QueryType,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            string _ElementName,
            BPrimitiveType[] _ElementValueEntries,
            int _Current_RetryCount = 0)
        {
            var RelevantQueue = new ConcurrentQueue<Action_DeliveryEnsurer>();
            if (!ActionsDictionary.TryAdd(_Context, RelevantQueue)) RelevantQueue = ActionsDictionary[_Context];
            RelevantQueue.Enqueue(new Action_DeliveryEnsurer_DB_Add_Remove_ElementsToArrayItem()
            {
                QueryType = _QueryType,
                TableName = _Table,
                KeyName = _KeyName,
                KeyValue = new BPrimitiveType_JStringified(_KeyValue),
                ElementName = _ElementName,
                ElementValueEntries = BPrimitiveType_JStringified.ConvertPrimitivesToPrimitiveTypeStructs(_ElementValueEntries).ToList(),
                RetryCount = _Current_RetryCount
            });
        }

        private void DB_AddOrRemoveElementsFromArrayItem_FireAndForget_Internal(
            string _QueryType,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            string _ElementName,
            BPrimitiveType[] _ElementValueEntries,
            int _Current_RetryCount = 0,
            Action<string> _ErrorMessageAction = null)
        {
            if (DatabaseService == null) return;

            bool bResult;
            if (_QueryType == Action_DeliveryEnsurer.QUERY_TYPE_DB_ADD_ELEMENTS_TO_ARRAY_ITEM)
            {
                bResult = DatabaseService.AddElementsToArrayItem(_Table, _KeyName, _KeyValue, _ElementName, _ElementValueEntries, out JObject _, EBReturnItemBehaviour.DoNotReturn, null, _ErrorMessageAction);
            }
            else
            {
                bResult = DatabaseService.RemoveElementsFromArrayItem(_Table, _KeyName, _KeyValue, _ElementName, _ElementValueEntries, out JObject _, EBReturnItemBehaviour.DoNotReturn, _ErrorMessageAction);
            }

            if (!bResult)
            {
                BroadcastFailed_FireAndForget_Operation(new Action_DeliveryEnsurer_DB_Add_Remove_ElementsToArrayItem()
                {
                    QueryType = _QueryType,
                    TableName = _Table,
                    KeyName = _KeyName,
                    KeyValue = new BPrimitiveType_JStringified(_KeyValue),
                    ElementName = _ElementName,
                    ElementValueEntries = BPrimitiveType_JStringified.ConvertPrimitivesToPrimitiveTypeStructs(_ElementValueEntries).ToList(),
                    RetryCount = _Current_RetryCount
                }, _ErrorMessageAction);
            } 
        }
    }
}