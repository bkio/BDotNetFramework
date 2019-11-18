using System;
using System.Collections.Generic;
using BCloudServiceUtilities;
using BCommonUtilities;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace BCloudServiceUtilities_BDatabaseService_MongoDB
{
    public class BDatabaseServiceMongoDB : BDatabaseServiceBase, IBDatabaseServiceInterface
    {
        /// <summary>
        /// <para>Holds initialization success</para>
        /// </summary>
        private readonly bool bInitializationSucceed;

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IBFileServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        private readonly IMongoDatabase MongoDB = null;

        private readonly Dictionary<string, IMongoCollection<BsonDocument>> TableMap = new Dictionary<string, IMongoCollection<BsonDocument>>();
        private readonly object TableMap_DictionaryLock = new object();
        private IMongoCollection<BsonDocument> GetTable(string _TableName, Action<string> _ErrorMessageAction = null)
        {
            lock (TableMap_DictionaryLock)
            {
                if (!TableMap.ContainsKey(_TableName))
                {
                    var TableObj = MongoDB.GetCollection<BsonDocument>(_TableName);
                    if (TableObj != null)
                    {
                        TableMap.Add(_TableName, TableObj);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceMongoDB->GetTable: Given table(collection) does not exist.");
                        return null;
                    }
                }
            }
            return TableMap[_TableName];
        }

        /// <summary>
        /// 
        /// <para>BDatabaseServiceGC: Parametered Constructor for Managed Service by Google</para>
        ///
        /// <para><paramref name="_MongoHost"/>                     MongoDB Host</para>
        /// <para><paramref name="_MongoPort"/>                     MongoDB Port</para>
        /// <para><paramref name="_MongoDatabase"/>                 MongoDB Database Name</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public BDatabaseServiceMongoDB(
            string _MongoHost,
            int _MongoPort,
            string _MongoDatabase,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                var Client = new MongoClient("mongodb://" + _MongoHost + ":" + _MongoPort);
                MongoDB = Client.GetDatabase(_MongoDatabase);
                bInitializationSucceed = MongoDB != null;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceMongoDB->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        /// <summary>
        /// 
        /// <para>GetItem</para>
        /// 
        /// <para>Gets an item from a table, if _ValuesToGet is null; will retrieve all.</para>
        /// 
        /// <para>Check <seealso cref="IBDatabaseServiceInterface.GetItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool GetItem(string _Table, string _KeyName, BPrimitiveType _KeyValue, string[] _ValuesToGet, out JObject _Result, Action<string> _ErrorMessageAction = null)
        {
            _Result = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// <para>PutItem</para>
        /// 
        /// <para>Puts an item to a table</para>
        /// <para>Note: Whether _ReturnItemBehaviour set to All or Updated, returns All</para>
        /// 
        /// <para>Check <seealso cref="IBDatabaseServiceInterface.PutItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool PutItem(string _Table, string _KeyName, BPrimitiveType _KeyValue, JObject _Item, out JObject _ReturnItem, EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn, BDatabaseAttributeCondition _ConditionExpression = null, Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// <para>UpdateItem</para>
        /// 
        /// <para>Updates an item in a table</para>
        /// 
        /// <para>Check <seealso cref="IBDatabaseServiceInterface.UpdateItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool UpdateItem(string _Table, string _KeyName, BPrimitiveType _KeyValue, JObject _UpdateItem, out JObject _ReturnItem, EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn, BDatabaseAttributeCondition _ConditionExpression = null, Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// <para>DeleteItem</para>
        /// 
        /// <para>Deletes an item from a table</para>
        /// <para>Note: Whether _ReturnItemBehaviour set to All or Updated, returns All</para>
        /// 
        /// <para>Check <seealso cref="IBDatabaseServiceInterface.DeleteItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool DeleteItem(string _Table, string _KeyName, BPrimitiveType _KeyValue, out JObject _ReturnItem, EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn, Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// <para>AddElementsToArrayItem</para>
        /// 
        /// <para>Adds element to the array item</para>
        /// 
        /// <para>Check <seealso cref="IBDatabaseServiceInterface.AddElementsToArrayItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool AddElementsToArrayItem(string _Table, string _KeyName, BPrimitiveType _KeyValue, string _ElementName, BPrimitiveType[] _ElementValueEntries, out JObject _ReturnItem, EBReturnItemBehaviour _ReturnItemBehaviour, BDatabaseAttributeCondition _ConditionExpression, Action<string> _ErrorMessageAction)
        {
            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// <para>RemoveElementsFromArrayItem</para>
        /// 
        /// <para>Removes element from the array item</para>
        /// 
        /// <para>Check <seealso cref="IBDatabaseServiceInterface.RemoveElementsFromArrayItem"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool RemoveElementsFromArrayItem(string _Table, string _KeyName, BPrimitiveType _KeyValue, string _ElementName, BPrimitiveType[] _ElementValueEntries, out JObject _ReturnItem, EBReturnItemBehaviour _ReturnItemBehaviour, Action<string> _ErrorMessageAction)
        {
            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// <para>IncrementOrDecrementItemValue</para>
        /// 
        /// <para>Updates an item in a table, if item does not exist, creates a new one with only increment/decrement value</para>
        /// 
        /// <para>Check <seealso cref="IBDatabaseServiceInterface.IncrementOrDecrementItemValue"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool IncrementOrDecrementItemValue(string _Table, string _KeyName, BPrimitiveType _KeyValue, out double _NewValue, string _ValueAttribute, double _IncrementOrDecrementBy, bool _bDecrement = false, Action<string> _ErrorMessageAction = null)
        {
            _NewValue = 0.0f;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// <para>ScanTable</para>
        /// 
        /// <para>Scans the table for attribute specified by _Key</para>
        /// 
        /// <para>Check <seealso cref="IBDatabaseServiceInterface.ScanTable"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool ScanTable(string _Table, out List<JObject> _ReturnItem, Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            throw new NotImplementedException();
        }

        public BDatabaseAttributeCondition BuildArrayElementNotExistCondition(BPrimitiveType ArrayElement)
        {
            throw new NotImplementedException();
        }

        public BDatabaseAttributeCondition BuildAttributeEqualsCondition(string Attribute, BPrimitiveType Value)
        {
            throw new NotImplementedException();
        }

        public BDatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute)
        {
            throw new NotImplementedException();
        }

        public BDatabaseAttributeCondition BuildAttributeGreaterCondition(string Attribute, BPrimitiveType Value)
        {
            throw new NotImplementedException();
        }

        public BDatabaseAttributeCondition BuildAttributeGreaterOrEqualCondition(string Attribute, BPrimitiveType Value)
        {
            throw new NotImplementedException();
        }

        public BDatabaseAttributeCondition BuildAttributeLessCondition(string Attribute, BPrimitiveType Value)
        {
            throw new NotImplementedException();
        }

        public BDatabaseAttributeCondition BuildAttributeLessOrEqualCondition(string Attribute, BPrimitiveType Value)
        {
            throw new NotImplementedException();
        }

        public BDatabaseAttributeCondition BuildAttributeNotEqualsCondition(string Attribute, BPrimitiveType Value)
        {
            throw new NotImplementedException();
        }

        public BDatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute)
        {
            throw new NotImplementedException();
        }
    }
}
