/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using Newtonsoft.Json;

namespace ServiceUtilities
{
    public class Action_OperationTimeout : Action
    {
        public Action_OperationTimeout() { }
        public Action_OperationTimeout(string _TableName, string _EntryKey)
        {
            TableName = _TableName;
            EntryKey = _EntryKey;
        }

        public override bool Equals(object _Other)
        {
            Action_OperationTimeout Casted;
            try
            {
                Casted = (Action_OperationTimeout)_Other;
            }
            catch (Exception)
            {
                return false;
            }
            return
                TableName == Casted.TableName &&
                EntryKey == Casted.EntryKey;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TableName, EntryKey);
        }

        [JsonProperty("dbTableName")]
        public string TableName;

        [JsonProperty("dbEntryKey")]
        public string EntryKey;

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_OPERATION_TIMEOUT;
        }

        //Default Instance
        public static readonly Action_OperationTimeout DefaultInstance = new Action_OperationTimeout();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }
}