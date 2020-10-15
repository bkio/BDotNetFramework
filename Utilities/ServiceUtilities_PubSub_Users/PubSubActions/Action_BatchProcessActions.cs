using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CIP
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

        [JsonProperty("versionIndex")]
        public int VersionIndex;

        [JsonProperty("revisionIndex")]
        public int RevisionIndex;

        [JsonProperty("modelId")]
        public string ModelID;

        public Action_BatchProcessFailed(string _ModelId, int _RevisionIndex, int _VersionIndex) 
        {
            ModelID = _ModelId;
            RevisionIndex = _RevisionIndex;
            VersionIndex = _VersionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_BatchProcessFailed Casted &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    VersionIndex == Casted.VersionIndex;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, VersionIndex);
        }

        //Default Instance
        public static readonly Action_BatchProcessFailed DefaultInstance = new Action_BatchProcessFailed();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }
}
