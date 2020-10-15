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

    public abstract class Action_ModelRevisionVersionAction : Action_ModelRevisionAction
    {
        [JsonProperty("versionIndex")]
        public int VersionIndex;
    }

    public class Action_ModelRevisionVersionCreated : Action_ModelRevisionVersionAction
    {
        public Action_ModelRevisionVersionCreated() { }
        public Action_ModelRevisionVersionCreated(string _ModelID, int _RevisionIndex, int _VersionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            VersionIndex = _VersionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionVersionCreated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    VersionIndex == Casted.VersionIndex &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, VersionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_VERSION_CREATED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionVersionCreated DefaultInstance = new Action_ModelRevisionVersionCreated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionVersionDeleted : Action_ModelRevisionVersionAction
    {
        public Action_ModelRevisionVersionDeleted() { }
        public Action_ModelRevisionVersionDeleted(string _ModelID, int _RevisionIndex, int _VersionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            VersionIndex = _VersionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionVersionDeleted Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    VersionIndex == Casted.VersionIndex &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, VersionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_VERSION_DELETED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionVersionDeleted DefaultInstance = new Action_ModelRevisionVersionDeleted();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionVersionUpdated : Action_ModelRevisionVersionAction
    {
        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        public Action_ModelRevisionVersionUpdated() { }
        public Action_ModelRevisionVersionUpdated(string _ModelID, int _RevisionIndex, int _VersionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID, JObject _ChangesObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            VersionIndex = _VersionIndex;
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionVersionUpdated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    VersionIndex == Casted.VersionIndex &&
                    ChangesObject.ToString() == Casted.ChangesObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, VersionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_VERSION_UPDATED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionVersionUpdated DefaultInstance = new Action_ModelRevisionVersionUpdated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionVersionFileEntryDeleted : Action_ModelRevisionVersionAction
    {
        public Action_ModelRevisionVersionFileEntryDeleted() { }
        public Action_ModelRevisionVersionFileEntryDeleted(string _ModelID, int _RevisionIndex, int _VersionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            VersionIndex = _VersionIndex;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionVersionFileEntryDeleted Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    VersionIndex == Casted.VersionIndex &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, VersionIndex, UserID, ActionPerformedByUserID, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_VERSION_FILE_ENTRY_DELETED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionVersionFileEntryDeleted DefaultInstance = new Action_ModelRevisionVersionFileEntryDeleted();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionVersionFileEntryUpdated : Action_ModelRevisionVersionAction
    {
        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        public Action_ModelRevisionVersionFileEntryUpdated() { }
        public Action_ModelRevisionVersionFileEntryUpdated(string _ModelID, int _RevisionIndex, int _VersionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID, JObject _ChangesObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            VersionIndex = _VersionIndex;
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionVersionFileEntryUpdated Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    VersionIndex == Casted.VersionIndex &&
                    ChangesObject.ToString() == Casted.ChangesObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, VersionIndex, UserID, ActionPerformedByUserID, ChangesObject, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_VERSION_FILE_ENTRY_UPDATED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionVersionFileEntryUpdated DefaultInstance = new Action_ModelRevisionVersionFileEntryUpdated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionVersionFileEntryDeleteAll : Action_ModelRevisionVersionAction
    {
        [JsonProperty("fileEntry")]
        public JObject Entry;

        public Action_ModelRevisionVersionFileEntryDeleteAll() { }
        public Action_ModelRevisionVersionFileEntryDeleteAll(
            string _ModelID, 
            int _RevisionIndex, 
            int _VersionIndex, 
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
            VersionIndex = _VersionIndex;
            Entry = _Entry;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionVersionFileEntryDeleteAll Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    VersionIndex == Casted.VersionIndex &&
                    Entry == Casted.Entry &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, VersionIndex, UserID, ActionPerformedByUserID, Entry, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_VERSION_FILE_ENTRY_DELETE_ALL;
        }

        //Default Instance
        public static readonly Action_ModelRevisionVersionFileEntryDeleteAll DefaultInstance = new Action_ModelRevisionVersionFileEntryDeleteAll();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_ModelRevisionVersionRawFileUploaded : Action_ModelRevisionVersionAction
    {
        [JsonProperty("model")]
        public JObject ModelObject = new JObject();

        public Action_ModelRevisionVersionRawFileUploaded() { }
        public Action_ModelRevisionVersionRawFileUploaded(string _ModelID, int _RevisionIndex, int _VersionIndex, string _UserID, List<string> _ModelSharedWithUserIDs, string _ActionPerformedByUserID, JObject _ModelObject)
        {
            ModelID = _ModelID;
            UserID = _UserID;
            ModelSharedWithUserIDs = _ModelSharedWithUserIDs;
            ActionPerformedByUserID = _ActionPerformedByUserID;
            RevisionIndex = _RevisionIndex;
            VersionIndex = _VersionIndex;
            ModelObject = _ModelObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_ModelRevisionVersionRawFileUploaded Casted &&
                    UserID == Casted.UserID &&
                    ActionPerformedByUserID == Casted.ActionPerformedByUserID &&
                    ModelID == Casted.ModelID &&
                    RevisionIndex == Casted.RevisionIndex &&
                    VersionIndex == Casted.VersionIndex &&
                    ModelObject.ToString() == Casted.ModelObject.ToString() &&
                    ModelSharedWithUserIDs.OrderBy(t => t).SequenceEqual(Casted.ModelSharedWithUserIDs.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModelID, RevisionIndex, VersionIndex, UserID, ActionPerformedByUserID, ModelObject, ModelSharedWithUserIDs);
        }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_MODEL_REVISION_VERSION_RAW_FILE_UPLOADED;
        }

        //Default Instance
        public static readonly Action_ModelRevisionVersionRawFileUploaded DefaultInstance = new Action_ModelRevisionVersionRawFileUploaded();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }
}