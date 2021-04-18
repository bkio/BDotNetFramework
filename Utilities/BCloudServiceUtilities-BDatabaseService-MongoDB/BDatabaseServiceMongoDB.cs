/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BCommonUtilities;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace BCloudServiceUtilities.DatabaseServices
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

        private bool TableExists(string _TableName)
        {
            var filter = new BsonDocument("name", _TableName);
            var options = new ListCollectionNamesOptions { Filter = filter };

            return MongoDB.ListCollectionNames(options).Any();
        }

        /// <summary>
        /// 
        /// <para>TryCreateTable</para>
        /// 
        /// <para>If given table (collection) does not exist in the database, it will create a new table (collection)</para>
        /// 
        /// 
        /// </summary>
        private bool TryCreateTable(string _TableName, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                if (!TableExists(_TableName))
                {
                    MongoDB.CreateCollection(_TableName);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BDatabaseServiceMongoDB->TryCreateTable: Given table(collection) couldn't create. Error: {ex.Message} \n Trace: {ex.StackTrace}");
                return false;
            }
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
                    if (!TryCreateTable(_TableName, _ErrorMessageAction))
                    {
                        return null;
                    }

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
                _ErrorMessageAction?.Invoke($"BDatabaseServiceMongoDB->Constructor: {e.Message} \n Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        public BDatabaseServiceMongoDB(
            string _ConnectionString,
            string _MongoDatabase,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                var Client = new MongoClient(_ConnectionString);
                MongoDB = Client.GetDatabase(_MongoDatabase);
                bInitializationSucceed = MongoDB != null;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"BDatabaseServiceMongoDB->Constructor: {e.Message} \n Trace: {e.StackTrace}");
                bInitializationSucceed = false;
            }
        }

        public BDatabaseServiceMongoDB(
            string _MongoClientConfigJson,
            string _MongoPassword,
            string _MongoDatabase,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                var _ClientConfigString = _MongoClientConfigJson;
                // Parse the Client Config Json if it's a base64 encoded (for running on local environment with launchSettings.json) 
                Span<byte> buffer = new Span<byte>(new byte[_ClientConfigString.Length]);
                if(Convert.TryFromBase64String(_ClientConfigString, buffer, out int bytesParsed))
                {
                    if(bytesParsed > 0)
                    {
                        _ClientConfigString = Encoding.UTF8.GetString(buffer);
                    }
                }

                var _ClientConfigJObject = JObject.Parse(_ClientConfigString);
                
                var _HostTokens = _ClientConfigJObject.SelectTokens("$...hostname");
                var _Hosts = new List<string>();
                foreach (var item in _HostTokens)
                {
                    _Hosts.Add(item.ToObject<string>());
                }
                
                var _PortTokens = _ClientConfigJObject.SelectTokens("$....port");
                var _Ports = new List<int>();
                foreach (var item in _PortTokens)
                {
                    _Ports.Add(item.ToObject<int>());
                }

                var _ReplicaSetName = _ClientConfigJObject.SelectToken("replicaSets[0]._id").ToObject<string>();
                var _DatabaseName = _ClientConfigJObject.SelectToken("auth.usersWanted[0].db").ToObject<string>();
                var _UserName = _ClientConfigJObject.SelectToken("auth.usersWanted[0].user").ToObject<string>();
                var _AuthMechnasim = _ClientConfigJObject.SelectToken("auth.autoAuthMechanism").ToObject<string>();
                int _MongoDBPort = 27017;

                var _ServerList = new List<MongoServerAddress>();
                for (int i = 0; i < _Hosts.Count; i++)
                {
                    if (i < _Ports.Count)
                        _MongoDBPort = _Ports[i];

                    _ServerList.Add(new MongoServerAddress(_Hosts[i], _MongoDBPort));
                }

                MongoInternalIdentity _InternalIdentity = new MongoInternalIdentity(_DatabaseName, _UserName);
                PasswordEvidence _PasswordEvidence = new PasswordEvidence(_MongoPassword);
                MongoCredential _MongoCredential = new MongoCredential(_AuthMechnasim, _InternalIdentity, _PasswordEvidence);
                //MongoCredential _MongoCredential = MongoCredential.CreateCredential(_DatabaseName, _UserName, _MongoPassword);

                var _ClientSettings = new MongoClientSettings();
                _ClientSettings.Servers = _ServerList.ToArray();
                _ClientSettings.ConnectionMode = ConnectionMode.ReplicaSet;
                _ClientSettings.ReplicaSetName = _ReplicaSetName;
                _ClientSettings.Credential = _MongoCredential;
                var Client = new MongoClient(_ClientSettings);
                MongoDB = Client.GetDatabase(_MongoDatabase);
                bInitializationSucceed = MongoDB != null;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke($"BDatabaseServiceMongoDB->Constructor: {e.Message} \n Trace: {e.StackTrace}");
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

            try
            {
                var Filter = Builders<BsonDocument>.Filter.Eq(_KeyName, _KeyValue.ToString());
                BsonDocument Document = Table.Find(Filter).FirstOrDefault();

                if (Document != null)
                {
                    _Result = BsonToJObject(Document);
                }
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BDatabaseServiceMongoDB->GetItem: {ex.Message} \n {ex.StackTrace}");
            }

            return false;
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
        public bool PutItem(string _Table, string _KeyName, BPrimitiveType _KeyValue, JObject _PutItem, out JObject _ReturnItem, EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn, BDatabaseAttributeCondition _ConditionExpression = null, Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            var Table = GetTable(_Table);
            if (Table == null) return false;

            try
            {
                JObject NewObject = (JObject)_PutItem.DeepClone();
                AddKeyToJson(NewObject, _KeyName, _KeyValue);

                var Filter = Builders<BsonDocument>.Filter.Eq(_KeyName, _KeyValue.ToString());

                if (_ConditionExpression != null)
                {
                    Filter = Builders<BsonDocument>.Filter.And(Filter, (_ConditionExpression as BDatabaseAttributeConditionMongo).Filter);
                }

                BsonDocument Document = new BsonDocument { { "$set", JObjectToBson(NewObject) } }; //use $set for preventing to get element name is not valid exception. more info https://stackoverflow.com/a/35441075

                Table.ReplaceOne(Filter, Document, new UpdateOptions() { IsUpsert = true });
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BDatabaseServiceMongoDB->PutItem: {ex.Message} : \n {ex.StackTrace}");
            }

            return false;
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

            try
            {
                var Filter = Builders<BsonDocument>.Filter.Eq(_KeyName, _KeyValue.ToString());

                if (_ConditionExpression != null)
                {
                    Filter = Builders<BsonDocument>.Filter.And(Filter, (_ConditionExpression as BDatabaseAttributeConditionMongo).Filter);
                }

                JObject NewObject = (JObject)_UpdateItem.DeepClone();
                AddKeyToJson(NewObject, _KeyName, _KeyValue);

                BsonDocument Document = new BsonDocument { { "$set", JObjectToBson(NewObject) } }; //use $set for preventing to get element name is not valid exception. more info https://stackoverflow.com/a/35441075

                if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                {
                    Table.UpdateOne(Filter, Document, new UpdateOptions() { IsUpsert = true });
                    return true;
                }
                else
                {
                    BsonDocument OldDocument = Table.FindOneAndUpdate(Filter, Document);

                    if (Document != null)
                    {
                        _ReturnItem = JObject.Parse(Document.ToJson());
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BDatabaseServiceMongoDB->UpdateItem: {ex.Message} : \n {ex.StackTrace}");
            }

            return false;
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

            var Filter = Builders<BsonDocument>.Filter.Eq(_KeyName, _KeyValue.ToString());

            try
            {
                if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                {
                    Table.DeleteOne(Filter);
                    _ReturnItem = null;
                    return true;
                }
                else
                {
                    BsonDocument Document = Table.FindOneAndDelete(Filter);

                    if (Document != null)
                    {
                        _ReturnItem = BsonToJObject(Document);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"BDatabaseServiceMongoDB->DeleteItem: {ex.Message} : \n {ex.StackTrace}");
            }

            return false;
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

            if (_ElementValueEntries == null || _ElementValueEntries.Length == 0)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceMongoDB->AddElementsToArrayItem: ElementValueEntries must contain values.");
                return false;
            }
            var ExpectedType = _ElementValueEntries[0].Type;

            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (_ElementValueEntry.Type != ExpectedType)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceMongoDB->AddElementsToArrayItem: ElementValueEntries must contain elements with the same type.");
                    return false;
                }
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceMongoDB->AddElementsToArrayItem: Key is null.");
                return false;
            }

            var Table = GetTable(_Table);
            if (Table == null) return false;

            var Filter = Builders<BsonDocument>
             .Filter.Eq(_KeyName, _KeyValue.ToString());

            if (_ConditionExpression != null)
            {
                if(_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.ArrayElementNotExist)
                {
                    Filter = Builders<BsonDocument>.Filter.And(Filter, (_ConditionExpression as BAttributeArrayElementNotExistConditionMongoDb).GetArrayElementFilter(_ElementName));
                }
                else
                {
                    Filter = Builders<BsonDocument>.Filter.And(Filter, (_ConditionExpression as BDatabaseAttributeConditionMongo).Filter);
                }
            }

            List<object> TempList = new List<object>();

            foreach (var Element in _ElementValueEntries)
            {
                switch (Element.Type)
                {
                    case EBPrimitiveTypeEnum.String:
                        TempList.Add(Element.AsString);
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        TempList.Add(Element.AsInteger);
                        break;
                    case EBPrimitiveTypeEnum.Double:
                        TempList.Add(Element.AsDouble);
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        TempList.Add(Element.AsByteArray);
                        break;
                }
            }

            UpdateDefinition<BsonDocument> Update = Builders<BsonDocument>.Update.PushEach(_ElementName, TempList);

            try
            {
                if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                {
                    Table.UpdateOne(Filter, Update);
                    return true;
                }
                else
                {
                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllOld)
                    {
                        BsonDocument Document = Table.FindOneAndUpdate(Filter, Update, new FindOneAndUpdateOptions<BsonDocument, BsonDocument>() { ReturnDocument = ReturnDocument.Before });
                        _ReturnItem = BsonToJObject(Document);
                        return true;
                    }
                    else
                    {
                        BsonDocument Document = Table.FindOneAndUpdate(Filter, Update, new FindOneAndUpdateOptions<BsonDocument, BsonDocument>() { ReturnDocument = ReturnDocument.After });
                        _ReturnItem = BsonToJObject(Document);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"{ex.Message} : \n {ex.StackTrace}");
            }
            return false;
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

            if (_ElementValueEntries == null || _ElementValueEntries.Length == 0)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceMongoDB->AddElementsToArrayItem: ElementValueEntries must contain values.");
                return false;
            }
            var ExpectedType = _ElementValueEntries[0].Type;

            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (_ElementValueEntry.Type != ExpectedType)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceMongoDB->AddElementsToArrayItem: ElementValueEntries must contain elements with the same type.");
                    return false;
                }
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceMongoDB->AddElementsToArrayItem: Key is null.");
                return false;
            }

            var Table = GetTable(_Table);
            if (Table == null) return false;

            var Filter = Builders<BsonDocument>
             .Filter.Eq(_KeyName, _KeyValue.ToString());

            List<object> TempList = new List<object>();

            foreach (var Element in _ElementValueEntries)
            {
                switch (Element.Type)
                {
                    case EBPrimitiveTypeEnum.String:
                        TempList.Add(Element.AsString);
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        TempList.Add(Element.AsInteger);
                        break;
                    case EBPrimitiveTypeEnum.Double:
                        TempList.Add(Element.AsDouble);
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        TempList.Add(Element.AsByteArray);
                        break;
                }
            }

            UpdateDefinition<BsonDocument> Update = Builders<BsonDocument>.Update.PullAll(_ElementName, TempList);

            try
            {

                if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                {
                    Table.UpdateOne(Filter, Update);
                    return true;
                }
                else
                {
                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllOld)
                    {
                        BsonDocument Document = Table.FindOneAndUpdate(Filter, Update, new FindOneAndUpdateOptions<BsonDocument, BsonDocument>() { ReturnDocument = ReturnDocument.Before });
                        _ReturnItem = JObject.Parse(Document.ToJson());
                        return true;
                    }
                    else
                    {
                        BsonDocument Document = Table.FindOneAndUpdate(Filter, Update, new FindOneAndUpdateOptions<BsonDocument, BsonDocument>() { ReturnDocument = ReturnDocument.After });
                        _ReturnItem = JObject.Parse(Document.ToJson());
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"{ex.Message} : \n {ex.StackTrace}");
                return false;
            }
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

            var Filter = Builders<BsonDocument>
                .Filter.Eq(_KeyName, _KeyValue.ToString());

            UpdateDefinition<BsonDocument> Update = null;

            if (_bDecrement)
            {
                Update = Builders<BsonDocument>.Update.Inc(_ValueAttribute, -_IncrementOrDecrementBy);
            }
            else
            {
                Update = Builders<BsonDocument>.Update.Inc(_ValueAttribute, _IncrementOrDecrementBy);
            }

            try
            {
                BsonDocument Document = Table.FindOneAndUpdate(Filter, Update, new FindOneAndUpdateOptions<BsonDocument, BsonDocument>() { ReturnDocument = ReturnDocument.After });
                _NewValue = Document.GetValue(_ValueAttribute).AsDouble;
                return true;
            }
            catch (Exception ex)
            {
                _ErrorMessageAction?.Invoke($"{ex.Message} : \n {ex.StackTrace}");
            }

            return false;
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

            List<JObject> Results = new List<JObject>();

            var Filter = Builders<BsonDocument>.Filter.Empty;

            IFindFluent<BsonDocument, BsonDocument> ReturnedSearch;

            try
            {
                ReturnedSearch = Table.Find(Filter);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceMongoDB->ScanTable: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }

            if (ReturnedSearch != null)
            {
                List<JObject> TempResults = new List<JObject>();
                try
                {
                    foreach (var Document in ReturnedSearch.ToList())
                    {
                        var CreatedJson = BsonToJObject(Document);
                        BUtility.SortJObject(CreatedJson, true);
                        TempResults.Add(CreatedJson);
                    }

                    _ReturnItem = TempResults;
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->ScanTable: JsonReaderException: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
                return true;
            }
            else
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->ScanTable: TableObject.ScanTable returned null.");
            }

            return false;
        }

        private class BDatabaseAttributeConditionMongo : BDatabaseAttributeCondition
        {
            public FilterDefinition<BsonDocument> Filter;
            public BDatabaseAttributeConditionMongo(EBDatabaseAttributeConditionType _ConditionType) : base(_ConditionType)
            {

            }

        }

        private class BAttributeArrayElementNotExistConditionMongoDb : BDatabaseAttributeConditionMongo
        {
            private BPrimitiveType ArrayElement;
            public BAttributeArrayElementNotExistConditionMongoDb(BPrimitiveType _ArrayElement) : base(EBDatabaseAttributeConditionType.ArrayElementNotExist)
            {
                ArrayElement = _ArrayElement;

            }

            public FilterDefinition<BsonDocument> GetArrayElementFilter(string ArrName)
            {
                switch (ArrayElement.Type)
                {
                    case EBPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Eq(ArrName, ArrayElement.AsDouble);
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Eq(ArrName, ArrayElement.AsInteger);
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Eq(ArrName, ArrayElement.AsByteArray);
                        break;
                    case EBPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Eq(ArrName, ArrayElement.AsString);
                        break;
                }
                return Filter;
            }

        }

        public BDatabaseAttributeCondition BuildArrayElementNotExistCondition(BPrimitiveType ArrayElement)
        {

            return new BAttributeArrayElementNotExistConditionMongoDb(ArrayElement);
        }

        private class BAttributeEqualsConditionMongoDb : BDatabaseAttributeConditionMongo
        {
            public BAttributeEqualsConditionMongoDb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                switch (Value.Type)
                {
                    case EBPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Eq(Attribute, Value.AsDouble);
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Eq(Attribute, Value.AsInteger);
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Eq(Attribute, Value.AsByteArray);
                        break;
                    case EBPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Eq(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }

        public BDatabaseAttributeCondition BuildAttributeEqualsCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeEqualsConditionMongoDb(Attribute, Value);
        }

        private class BAttributeExistConditionMongoDb : BDatabaseAttributeConditionMongo
        {
            public BAttributeExistConditionMongoDb(string Attribute) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                Filter = Builders<BsonDocument>.Filter.Exists(Attribute, true);
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }

        public BDatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute)
        {
            return new BAttributeExistConditionMongoDb(Attribute);
        }

        private class BAttributeGreaterMongoDb : BDatabaseAttributeConditionMongo
        {
            public BAttributeGreaterMongoDb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                switch (Value.Type)
                {
                    case EBPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Gt(Attribute, Value.AsDouble);
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Gt(Attribute, Value.AsInteger);
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Gt(Attribute, Value.AsByteArray);
                        break;
                    case EBPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Gt(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }

        public BDatabaseAttributeCondition BuildAttributeGreaterCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeGreaterMongoDb(Attribute, Value);
        }

        private class BAttributeGreaterOrEqualMongoDb : BDatabaseAttributeConditionMongo
        {
            public BAttributeGreaterOrEqualMongoDb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                switch (Value.Type)
                {
                    case EBPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Gte(Attribute, Value.AsDouble);
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Gte(Attribute, Value.AsInteger);
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Gte(Attribute, Value.AsByteArray);
                        break;
                    case EBPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Gte(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }

        public BDatabaseAttributeCondition BuildAttributeGreaterOrEqualCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeGreaterOrEqualMongoDb(Attribute, Value);
        }

        private class BAttributeLessMongoDb : BDatabaseAttributeConditionMongo
        {
            public BAttributeLessMongoDb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                switch (Value.Type)
                {
                    case EBPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Lt(Attribute, Value.AsDouble);
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Lt(Attribute, Value.AsInteger);
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Lt(Attribute, Value.AsByteArray);
                        break;
                    case EBPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Lt(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }

        public BDatabaseAttributeCondition BuildAttributeLessCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeLessMongoDb(Attribute, Value);
        }

        private class BAttributeLessOrEqualMongoDb : BDatabaseAttributeConditionMongo
        {
            public BAttributeLessOrEqualMongoDb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                switch (Value.Type)
                {
                    case EBPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Lte(Attribute, Value.AsDouble);
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Lte(Attribute, Value.AsInteger);
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Lte(Attribute, Value.AsByteArray);
                        break;
                    case EBPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Lte(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }

        public BDatabaseAttributeCondition BuildAttributeLessOrEqualCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeLessOrEqualMongoDb(Attribute, Value);
        }

        private class BAttributeNotEqualsConditionMongoDb : BDatabaseAttributeConditionMongo
        {
            public BAttributeNotEqualsConditionMongoDb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                switch (Value.Type)
                {
                    case EBPrimitiveTypeEnum.Double:
                        Filter = Builders<BsonDocument>.Filter.Ne(Attribute, Value.AsDouble);
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        Filter = Builders<BsonDocument>.Filter.Ne(Attribute, Value.AsInteger);
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        Filter = Builders<BsonDocument>.Filter.Ne(Attribute, Value.AsByteArray);
                        break;
                    case EBPrimitiveTypeEnum.String:
                        Filter = Builders<BsonDocument>.Filter.Ne(Attribute, Value.AsString);
                        break;
                }
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }

        public BDatabaseAttributeCondition BuildAttributeNotEqualsCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeNotEqualsConditionMongoDb(Attribute, Value);
        }

        private class BAttributeNotExistConditionMongoDb : BDatabaseAttributeConditionMongo
        {
            public BAttributeNotExistConditionMongoDb(string Attribute) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                Filter = Builders<BsonDocument>.Filter.Exists(Attribute, false);
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }

        public BDatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute)
        {
            return new BAttributeNotExistConditionMongoDb(Attribute);
        }

        private T WaitAndGetResult<T>(Task<T> WaitTask)
        {
            WaitTask.Wait();

            T Result = WaitTask.Result;

            WaitTask.Dispose();

            return Result;
        }

        private bool CompareJTokenWithBPrimitive(JToken _Token, BPrimitiveType _Primitive)
        {
            switch (_Primitive.Type)
            {
                case EBPrimitiveTypeEnum.Double:
                    return _Primitive.AsDouble == (double)_Token;
                case EBPrimitiveTypeEnum.Integer:
                    return _Primitive.AsInteger == (long)_Token;
                case EBPrimitiveTypeEnum.ByteArray:
                    return Convert.ToBase64String(_Primitive.AsByteArray) == (string)_Token;
                default:
                    return _Primitive.AsString == (string)_Token;
            }
        }

        private JObject BsonToJObject(BsonDocument _Document)
        {
            //remove database id as it is not part of what we store
            _Document.Remove("_id");

            //Set strict mode to convert numbers to valid json otherwise it generates something like NumberLong(5) where you expect a 5
            var JsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };

            return JObject.Parse(_Document.ToJson(JsonWriterSettings));
        }

        private BsonDocument JObjectToBson(JObject _JsonObject)
        {
            // https://stackoverflow.com/a/62104268
            //Write JObject to MemoryStream
            using var stream = new MemoryStream();
            using (var writer = new BsonDataWriter(stream) { CloseOutput = false })
            {
                _JsonObject.WriteTo(writer);
            }
            stream.Position = 0; //for reading the steam immediately 

            //Read the object from MemoryStream
            BsonDocument bsonData;
            using (var reader = new BsonBinaryReader(stream))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                bsonData = BsonDocumentSerializer.Instance.Deserialize(context);
            }
            return bsonData;
        }
    }
}
