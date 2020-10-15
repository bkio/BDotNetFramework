/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using BCloudServiceUtilities;
using Newtonsoft.Json.Linq;

namespace ServiceUtilities
{
    public interface IPubSubSubscriberInterface
    {
        void OnMessageReceived(JObject _Message);
    }

    //Important concept:
    //Message means a json object wraps up the serialized action with action type
    //Therefore SerializedMessage means json stringified wrapped object
    //SerializedAction is json stringified action
    public class Manager_PubSubService
    {
        public const string ACTION_KEY_NAME = "actionType";
        public const string SERIALIZED_ACTION_KEY_NAME = "serializedAction";

        private static Manager_PubSubService Instance = null;
        private Manager_PubSubService() {}
        public static Manager_PubSubService Get()
        {
            if (Instance == null)
            {
                Instance = new Manager_PubSubService();
            }
            return Instance;
        }

        private IBPubSubServiceInterface PubSubService;

        public void Setup(IBPubSubServiceInterface _PubSubService)
        {
            PubSubService = _PubSubService;

            ActionStringMap.Clear();
            StringActionMap.Clear();

            foreach (var Action in Actions.ActionStringPrefixMap.Keys)
            {
                string ActionName = Actions.ActionStringPrefixMap[Action] + Resources_DeploymentManager.Get().GetDeploymentBranchNameEscapedLoweredWithUnderscore() + "_" + Resources_DeploymentManager.Get().GetDeploymentBuildNumber();

                ActionStringMap.Add(Action, ActionName);
                StringActionMap.Add(ActionName, Action);
            }
        }

        public Dictionary<string, Actions.EAction> StringActionMap = new Dictionary<string, Actions.EAction>();
        public Dictionary<Actions.EAction, string> ActionStringMap = new Dictionary<Actions.EAction, string>();

        public void OnMessageReceived_Internal(string _, JObject _Message)
        {
            if (_Message != null && _Message.ContainsKey(ACTION_KEY_NAME) && _Message.ContainsKey(SERIALIZED_ACTION_KEY_NAME))
            {
                var Action = StringActionMap[(string)_Message[ACTION_KEY_NAME]];
                var SerializedAction = (JObject)_Message[SERIALIZED_ACTION_KEY_NAME];
                lock (Observers)
                {
                    if (Observers.ContainsKey(Action))
                    {
                        var RelevantList = Observers[Action];
                        foreach (var ObserverWeakPtr in RelevantList)
                        {
                            if (ObserverWeakPtr.TryGetTarget(out IPubSubSubscriberInterface Observer))
                            {
                                Observer?.OnMessageReceived(SerializedAction);
                            }
                        }
                    }
                }
            }
        }

        //Publish to services
        public bool PublishAction(Actions.EAction _Action, string _SerializedAction, Action<string> _ErrorMessageAction = null)
        {
            if (PubSubService == null) return false;

            var ActionName = ActionStringMap[_Action];

            return PubSubService.CustomPublish(ActionName, 
                new JObject()
                {
                    [ACTION_KEY_NAME] = ActionName,
                    [SERIALIZED_ACTION_KEY_NAME] = _SerializedAction
                }.ToString(),
            _ErrorMessageAction);
        }

        public bool DeserializeReceivedMessage(string _SerializedMessage, out Actions.EAction _Action, out string _SerializedAction, Action<string> _ErrorMessageAction = null)
        {
            JObject Parsed = null;
            try
            {
                Parsed = JObject.Parse(_SerializedMessage);
                _Action = StringActionMap[(string)Parsed[ACTION_KEY_NAME]];
                _SerializedAction = (string)Parsed[SERIALIZED_ACTION_KEY_NAME];
            }
            catch (Exception e)
            {
                //Check if message is originated from storage actions
                if (Parsed != null)
                {
                    if (Action_StorageFileUploaded.IsMatch(Parsed))
                    {
                        _Action = Actions.EAction.ACTION_STORAGE_FILE_UPLOADED;
                        _SerializedAction = _SerializedMessage;
                        return true;
                    }
                    else if (Action_StorageFileDeleted.IsMatch(Parsed))
                    {
                        _Action = Actions.EAction.ACTION_STORAGE_FILE_DELETED;
                        _SerializedAction = _SerializedMessage;
                        return true;
                    }
                }

                _ErrorMessageAction?.Invoke("Manager_PubSubService->DeserializeReceivedMessage: Deserialization error: " + e.Message + ", trace: " + e.StackTrace + ", serialized message: " + _SerializedMessage);
                _Action = Actions.EAction.NONE;
                _SerializedAction = null;
                return false;
            }
            return true;
        }

        private readonly Dictionary<Actions.EAction, List<WeakReference<IPubSubSubscriberInterface>>> Observers = new Dictionary<Actions.EAction, List<WeakReference<IPubSubSubscriberInterface>>>();

        public void AddSubscriber(IPubSubSubscriberInterface _Subscriber, Actions.EAction _Action)
        {
            lock (Observers)
            {
                if (Observers.ContainsKey(_Action))
                {
                    var RelevantList = Observers[_Action];

                    foreach (var Weak in RelevantList)
                    {
                        if (Weak.TryGetTarget(out IPubSubSubscriberInterface Strong) && Strong == _Subscriber)
                        {
                            return;
                        }
                    }
                    RelevantList.Add(new WeakReference<IPubSubSubscriberInterface>(_Subscriber));
                }
                else
                {
                    Observers[_Action] = new List<WeakReference<IPubSubSubscriberInterface>>()
                    { 
                        new WeakReference<IPubSubSubscriberInterface>(_Subscriber) 
                    };
                }
            }
        }

        public bool RemoveSubscriber(IPubSubSubscriberInterface _Subscriber, Actions.EAction _Action)
        {
            lock (Observers)
            {
                if (Observers.ContainsKey(_Action))
                {
                    var Counter = 0;
                    foreach (var Weak in Observers[_Action])
                    {
                        if (Weak.TryGetTarget(out IPubSubSubscriberInterface Strong) && Strong == _Subscriber)
                        {
                            Observers[_Action].RemoveAt(Counter);
                            return true;
                        }
                        Counter++;
                    }
                }
                return false;
            }
        }
    }
}