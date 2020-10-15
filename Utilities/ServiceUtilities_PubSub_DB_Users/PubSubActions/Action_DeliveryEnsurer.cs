/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System.Collections.Generic;
using BCommonUtilities;
using Newtonsoft.Json;

namespace ServiceUtilities
{
    public abstract class Action_DeliveryEnsurer : Action
    {
        public const string RETRY_COUNT_PROPERTY = "retryCount";

        public const string QUERY_TYPE_PROPERTY = "queryType";

        [JsonProperty(RETRY_COUNT_PROPERTY)]
        public int RetryCount = 0;

        [JsonProperty(QUERY_TYPE_PROPERTY)]
        public string QueryType;

        public override Actions.EAction GetActionType()
        {
            return Controller_DeliveryEnsurer.Get().GetActionServiceIdentifier();
        }

        public const string QUERY_TYPE_FS_DELETE_FILE = "fsDeleteFile";
        public const string QUERY_TYPE_FS_DELETE_FOLDER = "fsDeleteFolder";

        public const string QUERY_TYPE_DB_UPDATE_ITEM = "dbUpdateItem";
        public const string QUERY_TYPE_DB_PUT_ITEM = "dbPutItem";
        public const string QUERY_TYPE_DB_DELETE_ITEM = "dbDeleteItem";
        public const string QUERY_TYPE_DB_ADD_ELEMENTS_TO_ARRAY_ITEM = "dbAddElementsToArrayItem";
        public const string QUERY_TYPE_DB_REMOVE_ELEMENTS_FROM_ARRAY_ITEM = "dbRemoveElementsFromArrayItem";

        public static Action DeserializeDeliveryEnsurerAction(string _SerializedAction)
        {
            var DeserializedBase = JsonConvert.DeserializeObject<Action_DeliveryEnsurer>(_SerializedAction);
            switch (DeserializedBase.QueryType)
            {
                //FS
                case QUERY_TYPE_FS_DELETE_FILE:
                    {
                        return JsonConvert.DeserializeObject<Action_DeliveryEnsurer_FS_DeleteFile>(_SerializedAction);
                    }
                case QUERY_TYPE_FS_DELETE_FOLDER:
                    {
                        return JsonConvert.DeserializeObject<Action_DeliveryEnsurer_FS_DeleteFolder>(_SerializedAction);
                    }

                //DB
                case QUERY_TYPE_DB_UPDATE_ITEM:
                case QUERY_TYPE_DB_PUT_ITEM:
                    {
                        return JsonConvert.DeserializeObject<Action_DeliveryEnsurer_DB_UpdateOrPutItem>(_SerializedAction);
                    }
                case QUERY_TYPE_DB_DELETE_ITEM:
                    {
                        return JsonConvert.DeserializeObject<Action_DeliveryEnsurer_DB_DeleteItem>(_SerializedAction);
                    }
                case QUERY_TYPE_DB_ADD_ELEMENTS_TO_ARRAY_ITEM:
                case QUERY_TYPE_DB_REMOVE_ELEMENTS_FROM_ARRAY_ITEM:
                    {
                        return JsonConvert.DeserializeObject<Action_DeliveryEnsurer_DB_Add_Remove_ElementsToArrayItem>(_SerializedAction);
                    }
            }
            return null;
        }

    }

    public abstract class Action_DeliveryEnsurer_FS : Action_DeliveryEnsurer
    {
        public const string BUCKET_NAME_PROPERTY = "bucketName";
        public const string KEY_NAME_PROPERTY = "keyName";

        [JsonProperty(BUCKET_NAME_PROPERTY)]
        public string BucketName;

        [JsonProperty(KEY_NAME_PROPERTY)]
        public string KeyName;
    }

    //FS Related Structures

    public class Action_DeliveryEnsurer_FS_DeleteFile : Action_DeliveryEnsurer_FS
    {
        //Default Instance
        public static readonly Action_DeliveryEnsurer_FS_DeleteFile DefaultInstance = new Action_DeliveryEnsurer_FS_DeleteFile();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_DeliveryEnsurer_FS_DeleteFolder : Action_DeliveryEnsurer_FS
    {
        //Default Instance
        public static readonly Action_DeliveryEnsurer_FS_DeleteFolder DefaultInstance = new Action_DeliveryEnsurer_FS_DeleteFolder();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    //DB Related Structures

    public abstract class Action_DeliveryEnsurer_DB : Action_DeliveryEnsurer
    {
        public const string TABLE_NAME_PROPERTY = "tableName";
        public const string KEY_NAME_PROPERTY = "keyName";
        public const string KEY_VALUE = "keyValue";

        [JsonProperty(TABLE_NAME_PROPERTY)]
        public string TableName;

        [JsonProperty(KEY_NAME_PROPERTY)]
        public string KeyName;
    }

    public class Action_DeliveryEnsurer_DB_UpdateOrPutItem : Action_DeliveryEnsurer_DB
    {
        public const string UPDATE_ITEM_STRINGIFIED_PROPERTY = "updateItemStringified";

        [JsonProperty(KEY_VALUE)]
        public BPrimitiveType_JStringified KeyValue;

        [JsonProperty(UPDATE_ITEM_STRINGIFIED_PROPERTY)]
        public string UpdateItemStringified;

        //Default Instance
        public static readonly Action_DeliveryEnsurer_DB_UpdateOrPutItem DefaultInstance = new Action_DeliveryEnsurer_DB_UpdateOrPutItem();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_DeliveryEnsurer_DB_DeleteItem : Action_DeliveryEnsurer_DB
    {
        [JsonProperty(KEY_VALUE)]
        public BPrimitiveType_JStringified KeyValue;

        //Default Instance
        public static readonly Action_DeliveryEnsurer_DB_DeleteItem DefaultInstance = new Action_DeliveryEnsurer_DB_DeleteItem();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }

    public class Action_DeliveryEnsurer_DB_Add_Remove_ElementsToArrayItem : Action_DeliveryEnsurer_DB
    {
        public const string ELEMENT_NAME_PROPERTY = "elementName";
        public const string ELEMENT_VALUE_ENTRIES_PROPERTY = "elementValueEntries";

        [JsonProperty(KEY_VALUE)]
        public BPrimitiveType_JStringified KeyValue;

        [JsonProperty(ELEMENT_NAME_PROPERTY)]
        public string ElementName;

        [JsonProperty(ELEMENT_VALUE_ENTRIES_PROPERTY)]
        public List<BPrimitiveType_JStringified> ElementValueEntries = new List<BPrimitiveType_JStringified>();

        //Default Instance
        public static readonly Action_DeliveryEnsurer_DB_Add_Remove_ElementsToArrayItem DefaultInstance = new Action_DeliveryEnsurer_DB_Add_Remove_ElementsToArrayItem();
        protected override Action GetStaticDefaultInstance() { return DefaultInstance; }
    }
}