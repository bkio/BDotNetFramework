/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServiceUtilities
{
    public abstract class Action_ModelAction : Action
    {
        [JsonProperty("modelId")]
        public string ModelID;

        [JsonProperty("userId")]
        public string UserID;

        [JsonProperty("modelSharedWithUserIds")]
        public List<string> ModelSharedWithUserIDs = new List<string>();
    }

    public class Action_ModelCreated : Action_ModelAction
    {
        [JsonProperty("authMethodKey")]
        public string AuthMethodKey;

        public Action_ModelCreated() { }
        public Action_ModelCreated(string _ModelID, string _UserID, List<string> _ModelSharedWithUserIDs, string _AuthMethodKey)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            AuthMethodKey = _AuthMethodKey;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelCreated Casted &&
                    UserID == Casted.UserID &&
                    ModelID == Casted.ModelID &&
                    AuthMethodKey == Casted.AuthMethodKey &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, UserID, AuthMethodKey, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_CREATED;
        }

        //Default Instance
        public static readonly Action_ModelCreated DefaultInstance = new Action_ModelCreated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelDeleted : Action_ModelAction
    {
        [JsonProperty("actionPerformedByUserId")]
        public string ActionPerformedByUserID;

        public Action_ModelDeleted() {}
        public Action_ModelDeleted(string _ModelID, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelDeleted Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_DELETED;
        }

        //Default Instance
        public static readonly Action_ModelDeleted DefaultInstance = new Action_ModelDeleted();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelUpdated : Action_ModelAction
    {
        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        [JsonProperty("actionPerformedByUserId")]
        public string ActionPerformedByUserID;

        public Action_ModelUpdated() { }
        public Action_ModelUpdated(string _ModelID, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID, JObject _ChangesObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelUpdated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    ChangesObject.ToString() == Casted.ChangesObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_UPDATED;
        }

        //Default Instance
        public static readonly Action_ModelUpdated DefaultInstance = new Action_ModelUpdated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelSharedWithUserIdsChanged : Action_ModelAction
    {
        [JsonProperty("actionPerformedByUserId")]
        public string ActionPerformedByUserID;

        [JsonProperty("oldModelSharedWithUserIds")]
        public List<string> OldModelSharedWithUserIDs = new List<string>();

        public Action_ModelSharedWithUserIdsChanged() { }
        public Action_ModelSharedWithUserIdsChanged(string _ModelID, string _UserID, List<string> _ModelSharedWithUserIDs, List<string> _OldModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            OldModelSharedWithUserIDs = _OldModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelSharedWithUserIdsChanged Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    OldModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.OldModelSharedWithUserIDs.OrderBy(t => t)) &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs, OldModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_SHARED_WITH_USER_IDS_CHANGED;
        }

        //Default Instance
        public static readonly Action_ModelSharedWithUserIdsChanged DefaultInstance = new Action_ModelSharedWithUserIdsChanged();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public abstract class Action_ModelRevisionAction : Action_ModelAction
    {
        [JsonProperty("revisionIndex")]
        public int RevisionIndex;

        [JsonProperty("actionPerformedByUserId")]
        public string ActionPerformedByUserID;
    }

    public class Action_ModelRevisionCreated : Action_ModelRevisionAction
    {
        public Action_ModelRevisionCreated() { }
        public Action_ModelRevisionCreated(string _ModelID, int _RevisionIndex, string _ModelOwnerUserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _ModelOwnerUserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionCreated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_CREATED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionCreated DefaultInstance = new Action_ModelRevisionCreated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionDeleted : Action_ModelRevisionAction
    {
        public Action_ModelRevisionDeleted() { }
        public Action_ModelRevisionDeleted(string _ModelID, int _RevisionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionDeleted Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_DELETED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionDeleted DefaultInstance = new Action_ModelRevisionDeleted();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionUpdated : Action_ModelRevisionAction
    {
        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        public Action_ModelRevisionUpdated() { }
        public Action_ModelRevisionUpdated(string _ModelID, int _RevisionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID, JObject _ChangesObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionUpdated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ChangesObject.ToString() == Casted.ChangesObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_UPDATED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionUpdated DefaultInstance = new Action_ModelRevisionUpdated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }
        
    public class Action_ModelRevisionFileEntryDeleted : Action_ModelRevisionAction
    {
        public Action_ModelRevisionFileEntryDeleted() { }
        public Action_ModelRevisionFileEntryDeleted(
            string _ModelID, 
            int _RevisionIndex, 
            string _UserID, 
            List<string> _ModelSharedWithUserIDs, 
            string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionFileEntryDeleted Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionFileEntryDeleted DefaultInstance = new Action_ModelRevisionFileEntryDeleted();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionFileEntryUpdated : Action_ModelRevisionAction
    {
        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        public Action_ModelRevisionFileEntryUpdated() { }
        public Action_ModelRevisionFileEntryUpdated(string _ModelID, int _RevisionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID, JObject _ChangesObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionFileEntryUpdated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ChangesObject.ToString() == Casted.ChangesObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ChangesObject, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_FILE_ENTRY_UPDATED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionFileEntryUpdated DefaultInstance = new Action_ModelRevisionFileEntryUpdated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionFileEntryDeleteAll : Action_ModelRevisionAction
    {
        [JsonProperty("fileEntry")]
        public JObject Entry;

        public Action_ModelRevisionFileEntryDeleteAll() { }
        public Action_ModelRevisionFileEntryDeleteAll(
            string _ModelID, 
            int _RevisionIndex, 
            string _UserID,
            List<string> _ModelSharedWithUserIDs,
            string _ActionPerformedByUserID,
            JObject _Entry)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            Entry = _Entry;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionFileEntryDeleteAll Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    Entry == Casted.Entry &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, Entry, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_FILE_ENTRY_DELETE_ALL;
        }

        //Default Instance
        public static readonly Action_ModelRevisionFileEntryDeleteAll DefaultInstance = new Action_ModelRevisionFileEntryDeleteAll();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionRawFileUploaded : Action_ModelRevisionAction
    {
        [JsonProperty("model")]
        public JObject ModelObject = new JObject();

        public Action_ModelRevisionRawFileUploaded() { }
        public Action_ModelRevisionRawFileUploaded(
            string _ModelID, 
            int _RevisionIndex, 
            string _UserID, 
            List<string> _ModelSharedWithUserIDs, 
            string _ActionPerformedByUserID, 
            JObject _ModelObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            ModelObject = _ModelObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionRawFileUploaded Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    ModelObject.ToString() == Casted.ModelObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, UserID, ActionPerformedByUserID, ModelObject, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_RAW_FILE_UPLOADED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionRawFileUploaded DefaultInstance = new Action_ModelRevisionRawFileUploaded();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }
}