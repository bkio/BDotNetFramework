using System;
using Newtonsoft.Json;

namespace ServiceUtilities
{
    public abstract class Action_BatchProcessAction : Action
    {
    }

    public class Action_BatchProcessFailed : Action_BatchProcessAction
    {
        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_BATCH_PROCESS_FAILED;
        }

        public Action_BatchProcessFailed() { }

        [JsonProperty("revisionIndex")]
        public int RevisionIndex;

        [JsonProperty("modelUniqueName")]
        public string ModelName;
        
        [JsonProperty("statusMessage")]
        public string StatusMessage;

        public Action_BatchProcessFailed(string _ModelName, int _RevisionIndex, string _StatusMessage) 
        {
            ModelName = _ModelName;
            RevisionIndex = _RevisionIndex;
            StatusMessage = _StatusMessage;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_BatchProcessFailed Casted &&
                    ModelName == Casted.ModelName &&
                    RevisionIndex == Casted.RevisionIndex &&
                    StatusMessage.Equals(Casted.StatusMessage);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelName, RevisionIndex, StatusMessage);
        }

        //Default Instance
        public static readonly Action_BatchProcessFailed DefaultInstance = new Action_BatchProcessFailed();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }
}
