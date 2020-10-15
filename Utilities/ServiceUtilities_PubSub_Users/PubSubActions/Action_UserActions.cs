/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServiceUtilities
{
    public abstract class Action_UserAction : Action
    {
        [JsonProperty("userId")]
        public string UserID;
    }

    public class Action_UserCreated : Action_UserAction
    {
        public Action_UserCreated() {}
        public Action_UserCreated(string _UserID, string _UserEmail, string _UserName)
        {
            UserID = _UserID;
            UserEmail = _UserEmail;
            UserName = _UserName;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_UserCreated Casted &&
                       UserID == Casted.UserID &&
                       UserEmail == Casted.UserEmail &&
                       UserName == Casted.UserName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserID, UserEmail, UserName);
        }

        [JsonProperty("userEmail")]
        public string UserEmail;

        [JsonProperty("userName")]
        public string UserName;

        //Default Instance
        public static readonly Action_UserCreated DefaultInstance = new Action_UserCreated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_USER_CREATED;
        }
    }

    public class Action_UserDeleted : Action_UserAction
    {
        public Action_UserDeleted() {}
        public Action_UserDeleted(string _UserID, string _UserEmail, string _UserName, List<string> _UserModels, List<string> _UserSharedModels)
        {
            UserID = _UserID;
            UserEmail = _UserEmail;
            UserName = _UserName;
            UserModels = _UserModels;
            UserSharedModels = _UserSharedModels;
        }

        public override bool Equals(object _Other)
        {
            if (!(_Other is Action_UserDeleted Casted)) return false;

            return
                UserID == Casted.UserID &&
                UserEmail == Casted.UserEmail &&
                UserName == Casted.UserName &&
                UserModels.OrderBy(t => t).SequenceEqual(Casted.UserModels.OrderBy(t => t)) &&
                UserSharedModels.OrderBy(t => t).SequenceEqual(Casted.UserSharedModels.OrderBy(t => t));
        }

        public override int GetHashCode()
        {
            string CombinedModelIDs = "";
            foreach (var ModelID in UserModels)
            {
                CombinedModelIDs += ModelID;
            }
            string CombinedSharedModelIDs = "";
            foreach (var SharedModelID in UserSharedModels)
            {
                CombinedSharedModelIDs += SharedModelID;
            }
            return HashCode.Combine(UserID, UserEmail, UserName, CombinedModelIDs, CombinedSharedModelIDs);
        }

        [JsonProperty("userEmail")]
        public string UserEmail;

        [JsonProperty("userName")]
        public string UserName;

        [JsonProperty("userModels")]
        public List<string> UserModels = new List<string>();

        [JsonProperty("userSharedModels")]
        public List<string> UserSharedModels = new List<string>();

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_USER_DELETED;
        }

        //Default Instance
        public static readonly Action_UserDeleted DefaultInstance = new Action_UserDeleted();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_UserUpdated : Action_UserAction
    {
        public Action_UserUpdated() { }
        public Action_UserUpdated(
            string _UserID, 
            string _OldUserEmail, 
            string _NewUserEmail, 
            string _OldUserName, 
            string _NewUserName,
            JObject _ChangesObject)
        {
            UserID = _UserID;
            OldUserEmail = _OldUserEmail ?? "";
            NewUserEmail = _NewUserEmail ?? "";
            OldUserName = _OldUserName ?? "";
            NewUserName = _NewUserName ?? "";
            ChangesObject = _ChangesObject;
        }

        public override bool Equals(object _Other)
        {
            return _Other is Action_UserUpdated Casted &&
                UserID == Casted.UserID &&
                NewUserEmail == Casted.NewUserEmail &&
                NewUserName == Casted.NewUserName &&
                OldUserEmail == Casted.OldUserEmail &&
                OldUserName == Casted.OldUserName &&
                ChangesObject.ToString() == Casted.ChangesObject.ToString();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UserID, NewUserEmail, NewUserName, OldUserEmail, OldUserName);
        }

        [JsonProperty("oldUserEmail")]
        public string OldUserEmail;

        [JsonProperty("newUserEmail")]
        public string NewUserEmail;

        [JsonProperty("oldUserName")]
        public string OldUserName;

        [JsonProperty("newUserName")]
        public string NewUserName;

        [JsonProperty("changes")]
        public JObject ChangesObject = new JObject();

        //Default Instance
        public static readonly Action_UserUpdated DefaultInstance = new Action_UserUpdated();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }

        public override Actions.EAction GetActionType()
        {
            return Actions.EAction.ACTION_USER_UPDATED;
        }
    }
}