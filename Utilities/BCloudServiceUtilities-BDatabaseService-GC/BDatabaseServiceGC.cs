/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Datastore.V1;
using Grpc.Auth;
using Newtonsoft.Json.Linq;
using BCommonUtilities;

namespace BCloudServiceUtilities.DatabaseServices
{
    public class BDatabaseServiceGC : BDatabaseServiceBase, IBDatabaseServiceInterface
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly DatastoreClient DSClient;
        private readonly DatastoreDb DSDB;

        private readonly ServiceAccountCredential Credential;
        private readonly Grpc.Core.Channel Channel;

        /// <summary>
        /// 
        /// <para>BDatabaseServiceGC: Parametered Constructor for Managed Service by Google</para>
        ///
        /// <para><paramref name="_ProjectID"/>                     GC Project ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public BDatabaseServiceGC(
            string _ProjectID,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                string ApplicationCredentialsPlain = Environment.GetEnvironmentVariable("GOOGLE_PLAIN_CREDENTIALS");
                if (ApplicationCredentials == null && ApplicationCredentialsPlain == null)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS) environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    if (ApplicationCredentials == null)
                    {
                        if (!BUtility.HexDecode(out ApplicationCredentialsPlain, ApplicationCredentialsPlain, _ErrorMessageAction))
                        {
                            throw new Exception("Hex decode operation for application credentials plain has failed.");
                        }
                        Credential = GoogleCredential.FromJson(ApplicationCredentialsPlain)
                                         .CreateScoped(DatastoreClient.DefaultScopes)
                                         .UnderlyingCredential as ServiceAccountCredential;
                    }
                    else
                    {
                        using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                        {
                            Credential = GoogleCredential.FromStream(Stream)
                                         .CreateScoped(DatastoreClient.DefaultScopes)
                                         .UnderlyingCredential as ServiceAccountCredential;
                        }
                    }

                    if (Credential != null)
                    {
                        Channel = new Grpc.Core.Channel(
                            DatastoreClient.DefaultEndpoint.ToString(),
                            Credential.ToChannelCredentials());

                        DSClient = DatastoreClient.Create(Channel);
                    }

                    if (DSClient != null)
                    {
                        DSDB = DatastoreDb.Create(_ProjectID, "", DSClient);

                        bInitializationSucceed = DSDB != null;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceGC->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

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

        /// <summary>
        /// Map that holds loaded kind definition instances
        /// </summary>
        private readonly Dictionary<string, KeyFactory> LoadedKindKeyFactories = new Dictionary<string, KeyFactory>();
        private readonly object LoadedKindKeyFactories_DictionaryLock = new object();

        /// <summary>
        /// Searches kind key factories in LoadedKindKeyFactories, if not loaded, loads, stores and returns
        /// </summary>
        private bool LoadStoreAndGetKindKeyFactory(
            string _Kind,
            out KeyFactory _ResultKeyFactory,
            Action<string> _ErrorMessageAction = null)
        {
            bool bResult = true;
            lock (LoadedKindKeyFactories_DictionaryLock)
            {
                if (!LoadedKindKeyFactories.ContainsKey(_Kind))
                {
                    try
                    {
                        _ResultKeyFactory = DSDB.CreateKeyFactory(_Kind);
                    }
                    catch (Exception e)
                    {
                        _ResultKeyFactory = null;
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->LoadStoreAndGetKindKeyFactory: Exception: " + e.Message);
                        return false;
                    }

                    if (_ResultKeyFactory != null)
                    {
                        LoadedKindKeyFactories[_Kind] = _ResultKeyFactory;
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->LoadStoreAndGetKindKeyFactory: CreateKeyFactory returned null.");
                        bResult = false;
                    }
                }
                else
                {
                    _ResultKeyFactory = LoadedKindKeyFactories[_Kind];
                }
            }
            return bResult;
        }

        private void ChangeExcludeFromIndexes(Value _Value)
        {
            switch (_Value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.ArrayValue:
                    break;
                default:
                    _Value.ExcludeFromIndexes = true;
                    break;
            }
        }
        private Entity FromJsonToEntity(KeyFactory Factory, string _KeyName, BPrimitiveType _KeyValue, JObject JsonObject)
        {
            if (JsonObject != null)
            {
                var Result = FromJsonToEntity(JsonObject);
                Result.Key = Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, _KeyValue));
                return Result;
            }
            return null;
        }
        private Entity FromJsonToEntity(JObject JsonObject)
        {
            var Result = new Entity();
            foreach (var Current in JsonObject)
            {
                var Name = Current.Key;
                var Value = Current.Value;
                Result.Properties[Name] = GetDSValueFromJToken(Value);
                ChangeExcludeFromIndexes(Result.Properties[Name]);
            }
            return Result;
        }
        private Value GetDSValueFromJToken(JToken _Value)
        {
            switch (_Value.Type)
            {
                case JTokenType.Object:
                    return new Value()
                    {
                        EntityValue = FromJsonToEntity((JObject)_Value)
                    };
                case JTokenType.Array:
                    var AsArray = (JArray)_Value;
                    var AsArrayValue = new ArrayValue();
                    foreach (var Current in AsArray)
                    {
                        var CurVal = GetDSValueFromJToken(Current);
                        ChangeExcludeFromIndexes(CurVal);
                        AsArrayValue.Values.Add(CurVal);
                    }
                    return new Value()
                    {
                        ArrayValue = AsArrayValue
                    };
                case JTokenType.Integer:
                    return new Value()
                    {
                        IntegerValue = (long)_Value
                    };
                case JTokenType.Float:
                    return new Value()
                    {
                        DoubleValue = (double)_Value
                    };
                case JTokenType.Boolean:
                    return new Value()
                    {
                        BooleanValue = (bool)_Value
                    };
                case JTokenType.String:
                    return new Value()
                    {
                        StringValue = (string)_Value
                    };
                default:
                    return new Value()
                    {
                        StringValue = _Value.ToString()
                    };
            }
        }
        private JObject FromEntityToJson(Entity _Entity)
        {
            if (_Entity != null && _Entity.Properties != null)
            {
                var Result = new JObject();
                foreach (var Current in _Entity.Properties)
                {
                    Result[Current.Key] = FromValueToJsonToken(Current.Value);
                }
                return Result;
            }
            return null;
        }
        private JToken FromValueToJsonToken(Value _Value)
        {
            switch (_Value.ValueTypeCase)
            {
                case Value.ValueTypeOneofCase.EntityValue:
                    return FromEntityToJson(_Value.EntityValue);
                case Value.ValueTypeOneofCase.ArrayValue:
                    var AsJArray = new JArray();
                    foreach (var ArrayVal in _Value.ArrayValue.Values)
                    {
                        AsJArray.Add(FromValueToJsonToken(ArrayVal));
                    }
                    return AsJArray;
                case Value.ValueTypeOneofCase.BooleanValue:
                    return _Value.BooleanValue;
                case Value.ValueTypeOneofCase.IntegerValue:
                    return _Value.IntegerValue;
                case Value.ValueTypeOneofCase.DoubleValue:
                    return _Value.DoubleValue;
                case Value.ValueTypeOneofCase.StringValue:
                    return _Value.StringValue;
                default:
                    return _Value.ToString();
            }
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

        private string GetFinalKeyFromNameValue(string _KeyName, BPrimitiveType _KeyValue)
        {
            return _KeyName + ":" + _KeyValue.ToString();
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
        public bool GetItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            string[] _ValuesToGet,
            out JObject _Result,
            Action<string> _ErrorMessageAction = null)
        {
            _Result = null;

            if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
            {
                Entity ReturnedEntity = null;
                try
                {
                    ReturnedEntity = DSDB.Lookup(Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, _KeyValue)));
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceGC->GetItem: Exception: " + e.Message);
                    return false;
                }
                
                if (ReturnedEntity != null)
                {
                    _Result = FromEntityToJson(ReturnedEntity);
                    AddKeyToJson(_Result, _KeyName, _KeyValue);
                    BUtility.SortJObject(_Result, true);
                }
                return true;
            }
            return false;
        }
        private bool GetItemInTransaction(
            DatastoreTransaction _Transaction,
            KeyFactory _Factory,
            string _KeyName,
            BPrimitiveType _KeyValue,
            out JObject _Result,
            Action<string> _ErrorMessageAction = null)
        {
            _Result = null;

            Entity ReturnedEntity = null;
            try
            {
                ReturnedEntity = _Transaction.Lookup(_Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, _KeyValue)));
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceGC->GetItemInTransaction: Exception: " + e.Message);
                return false;
            }

            if (ReturnedEntity != null)
            {
                _Result = FromEntityToJson(ReturnedEntity);
                AddKeyToJson(_Result, _KeyName, _KeyValue);
                BUtility.SortJObject(_Result, true);
            }
            return true;
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
        public bool PutItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            JObject _Item, 
            out JObject _ReturnItem, 
            EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn,
            BDatabaseAttributeCondition _ConditionExpression = null, 
            Action<string> _ErrorMessageAction = null)
        {
            return PutOrUpdateItem(EBPutOrUpdateItemType.PutItem, _Table, _KeyName, _KeyValue, _Item, out _ReturnItem, _ReturnItemBehaviour, _ConditionExpression, _ErrorMessageAction);
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
        public bool UpdateItem(
           string _Table,
           string _KeyName,
           BPrimitiveType _KeyValue,
           JObject _UpdateItem,
           out JObject _ReturnItem,
           EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn,
           BDatabaseAttributeCondition _ConditionExpression = null,
           Action<string> _ErrorMessageAction = null)
        {
            return PutOrUpdateItem(EBPutOrUpdateItemType.UpdateItem, _Table, _KeyName, _KeyValue, _UpdateItem, out _ReturnItem, _ReturnItemBehaviour, _ConditionExpression, _ErrorMessageAction);
        }

        private enum EBPutOrUpdateItemType
        {
            PutItem,
            UpdateItem
        }
        private bool PutOrUpdateItem(
            EBPutOrUpdateItemType _PutOrUpdateItemType,
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            JObject _NewItem, 
            out JObject _ReturnItem, 
            EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn, 
            BDatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            var NewItem = new JObject(_NewItem);

            if (NewItem.ContainsKey(_KeyName))
            {
                NewItem.Remove(_KeyName);
            }

            if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
            {
                JObject ReturnedPreOperationObject = null;
                using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                {
                    if (_PutOrUpdateItemType == EBPutOrUpdateItemType.UpdateItem)
                    {
                        if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out ReturnedPreOperationObject, _ErrorMessageAction))
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceGC->PutOrUpdateItem: GetItemInTransaction failed.");
                            return false;
                        }
                    }

                    if (_ConditionExpression != null)
                    {
                        //If it's PutItem, GetItemInTransaction has not been called yet.
                        if (_PutOrUpdateItemType == EBPutOrUpdateItemType.PutItem)
                        {
                            if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out ReturnedPreOperationObject, _ErrorMessageAction))
                            {
                                _ErrorMessageAction?.Invoke("BDatabaseServiceGC->PutOrUpdateItem: GetItemInTransaction failed.");
                                return false;
                            }
                        }

                        var BuiltCondition = _ConditionExpression.GetBuiltCondition();
                        if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeEquals
                            || _ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeNotEquals
                            || _ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeGreater
                            || _ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeGreaterOrEqual
                            || _ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeLess
                            || _ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeLessOrEqual)
                        {
                            if (BuiltCondition.Item1 == null || BuiltCondition.Item2 == null || BuiltCondition.Item2.Item1 == null || BuiltCondition.Item2.Item2 == null)
                            {
                                _ErrorMessageAction?.Invoke("BDatabaseServiceGC->PutOrUpdateItem: Invalid condition expression.");
                                return false;
                            }

                            bool bConditionSatisfied = false;
                            if (ReturnedPreOperationObject != null && ReturnedPreOperationObject.ContainsKey(BuiltCondition.Item1))
                            {
                                switch (BuiltCondition.Item2.Item2.Type)
                                {
                                    case EBPrimitiveTypeEnum.Double:
                                        if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeEquals)
                                        {
                                            bConditionSatisfied = (double)ReturnedPreOperationObject[BuiltCondition.Item1] == BuiltCondition.Item2.Item2.AsDouble;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeNotEquals)
                                        {
                                            bConditionSatisfied = (double)ReturnedPreOperationObject[BuiltCondition.Item1] != BuiltCondition.Item2.Item2.AsDouble;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeGreater)
                                        {
                                            bConditionSatisfied = (double)ReturnedPreOperationObject[BuiltCondition.Item1] > BuiltCondition.Item2.Item2.AsDouble;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeGreaterOrEqual)
                                        {
                                            bConditionSatisfied = (double)ReturnedPreOperationObject[BuiltCondition.Item1] >= BuiltCondition.Item2.Item2.AsDouble;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeLess)
                                        {
                                            bConditionSatisfied = (double)ReturnedPreOperationObject[BuiltCondition.Item1] < BuiltCondition.Item2.Item2.AsDouble;
                                        }
                                        else
                                        {
                                            bConditionSatisfied = (double)ReturnedPreOperationObject[BuiltCondition.Item1] <= BuiltCondition.Item2.Item2.AsDouble;
                                        }
                                        break;
                                    case EBPrimitiveTypeEnum.Integer:
                                        if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeEquals)
                                        {
                                            bConditionSatisfied = (long)ReturnedPreOperationObject[BuiltCondition.Item1] == BuiltCondition.Item2.Item2.AsInteger;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeNotEquals)
                                        {
                                            bConditionSatisfied = (long)ReturnedPreOperationObject[BuiltCondition.Item1] != BuiltCondition.Item2.Item2.AsInteger;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeGreater)
                                        {
                                            bConditionSatisfied = (long)ReturnedPreOperationObject[BuiltCondition.Item1] > BuiltCondition.Item2.Item2.AsInteger;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeGreaterOrEqual)
                                        {
                                            bConditionSatisfied = (long)ReturnedPreOperationObject[BuiltCondition.Item1] >= BuiltCondition.Item2.Item2.AsInteger;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeLess)
                                        {
                                            bConditionSatisfied = (long)ReturnedPreOperationObject[BuiltCondition.Item1] < BuiltCondition.Item2.Item2.AsInteger;
                                        }
                                        else
                                        {
                                            bConditionSatisfied = (long)ReturnedPreOperationObject[BuiltCondition.Item1] <= BuiltCondition.Item2.Item2.AsInteger;
                                        }
                                        break;
                                    case EBPrimitiveTypeEnum.ByteArray:
                                        var First = (string)ReturnedPreOperationObject[BuiltCondition.Item1];
                                        var Second = Convert.ToBase64String(BuiltCondition.Item2.Item2.AsByteArray);

                                        if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeEquals)
                                        {
                                            bConditionSatisfied = First == Second;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeNotEquals)
                                        {
                                            bConditionSatisfied = First != Second;
                                        }
                                        else
                                        {
                                            _ErrorMessageAction?.Invoke("BDatabaseServiceGC->PutOrUpdateItem: Invalid condition expression.");
                                            return false;
                                        }
                                        break;
                                    default:
                                        First = (string)ReturnedPreOperationObject[BuiltCondition.Item1];
                                        Second = BuiltCondition.Item2.Item2.AsString;

                                        if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeEquals)
                                        {
                                            bConditionSatisfied = First == Second;
                                        }
                                        else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeNotEquals)
                                        {
                                            bConditionSatisfied = First != Second;
                                        }
                                        else
                                        {
                                            _ErrorMessageAction?.Invoke("BDatabaseServiceGC->PutOrUpdateItem: Invalid condition expression.");
                                            return false;
                                        }
                                        break;
                                }
                            }

                            if (!bConditionSatisfied)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (BuiltCondition.Item1 == null)
                            {
                                _ErrorMessageAction?.Invoke("BDatabaseServiceGC->PutOrUpdateItem: Invalid condition expression.");
                                return false;
                            }

                            bool bConditionSatisfied = false;

                            if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeNotExist)
                            {
                                bConditionSatisfied = true;

                                if (ReturnedPreOperationObject != null)
                                {
                                    if (BuiltCondition.Item1 == _KeyName || ReturnedPreOperationObject.ContainsKey(BuiltCondition.Item1))
                                    {
                                        bConditionSatisfied = false;
                                    }
                                }
                            }
                            else if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.AttributeExists)
                            {
                                if (ReturnedPreOperationObject != null)
                                {
                                    if (BuiltCondition.Item1 == _KeyName || ReturnedPreOperationObject.ContainsKey(BuiltCondition.Item1))
                                    {
                                        bConditionSatisfied = true;
                                    }
                                }
                            }

                            if (!bConditionSatisfied)
                            {
                                return false;
                            }
                        }
                    }

                    if (_PutOrUpdateItemType == EBPutOrUpdateItemType.UpdateItem)
                    {
                        if (ReturnedPreOperationObject != null)
                        {
                            var CopyObject = new JObject(ReturnedPreOperationObject);
                            CopyObject.Merge(NewItem, new JsonMergeSettings
                            {
                                MergeArrayHandling = MergeArrayHandling.Replace
                            });
                            NewItem = CopyObject;
                        }
                    }

                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllOld)
                    {
                        if (ReturnedPreOperationObject == null)
                        {
                            if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out _ReturnItem, _ErrorMessageAction))
                            {
                                _ErrorMessageAction?.Invoke("BDatabaseServiceGC->PutOrUpdateItem: GetItemInTransaction failed.");
                                return false;
                            }
                            if (_ReturnItem == null)
                            {
                                _ReturnItem = new JObject();
                            }
                        }
                        else
                        {
                            _ReturnItem = ReturnedPreOperationObject;
                        }
                    }

                    var ItemAsEntity = FromJsonToEntity(Factory, _KeyName, _KeyValue, NewItem);

                    try
                    {
                        Transaction.Upsert(ItemAsEntity);
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->PutOrUpdateItem->Transaction.Upsert: Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }

                    try
                    {
                        Transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->PutOrUpdateItem->Transaction.Commit: Table: " + _Table + ", Key: " + _KeyValue.ToString() + ", Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }

                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllNew)
                    {
                        GetItem(_Table, _KeyName, _KeyValue, null, out _ReturnItem, _ErrorMessageAction);
                    }
                }
                return true;
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
        public bool AddElementsToArrayItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            string _ElementName,
            BPrimitiveType[] _ElementValueEntries,
            out JObject _ReturnItem,
            EBReturnItemBehaviour _ReturnItemBehaviour,
            BDatabaseAttributeCondition _ConditionExpression,
            Action<string> _ErrorMessageAction)
        {
            _ReturnItem = null;

            if (_ElementValueEntries == null || _ElementValueEntries.Length == 0)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: ElementValueEntries must contain values.");
                return false;
            }
            var ExpectedType = _ElementValueEntries[0].Type;
            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (_ElementValueEntry.Type != ExpectedType)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: ElementValueEntries must contain elements with the same type.");
                    return false;
                }
            }

            if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
            {
                JObject ReturnedPreOperationObject = null;
                using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                {
                    if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out ReturnedPreOperationObject, _ErrorMessageAction))
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->AddElementsToArrayItem: GetItemInTransaction failed.");
                        return false;
                    }
                    
                    JArray ItemAsArray = null;

                    if (ReturnedPreOperationObject != null && ReturnedPreOperationObject.ContainsKey(_ElementName))
                    {
                        if (ReturnedPreOperationObject[_ElementName].Type != JTokenType.Array)
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceGC->AddElementsToArrayItem: Item is not an array.");
                            return false;
                        }
                        ItemAsArray = (JArray)ReturnedPreOperationObject[_ElementName];
                    }

                    if (_ConditionExpression != null)
                    {
                        if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.ArrayElementNotExist)
                        {
                            var BuiltCondition = _ConditionExpression.GetBuiltCondition();

                            if (BuiltCondition.Item2 == null || BuiltCondition.Item2.Item2 == null)
                            {
                                _ErrorMessageAction?.Invoke("BDatabaseServiceGC->AddElementsToArrayItem: Invalid condition expression.");
                                return false;
                            }

                            if (ItemAsArray != null)
                            {
                                foreach (var CurTok in ItemAsArray)
                                {
                                    if (CompareJTokenWithBPrimitive(CurTok, BuiltCondition.Item2.Item2))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceGC->AddElementsToArrayItem: Condition is not valid for this operation.");
                            return false;
                        }
                    }

                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllOld)
                    {
                        if (ReturnedPreOperationObject != null)
                        {
                            _ReturnItem = new JObject(ReturnedPreOperationObject);
                        }
                        else
                        {
                            _ReturnItem = new JObject();
                        }
                    }

                    if (ItemAsArray == null)
                    {
                        ItemAsArray = new JArray();
                        foreach (var _ElementValueEntry in _ElementValueEntries)
                        {
                            ItemAsArray.Add(FromBPrimitiveTypeToJToken(_ElementValueEntry));
                        }
                    }
                    else
                    {
                        foreach (var _ElementValueEntry in _ElementValueEntries)
                        {
                            ItemAsArray.Add(FromBPrimitiveTypeToJToken(_ElementValueEntry));
                        }
                    }

                    if (ReturnedPreOperationObject == null)
                    {
                        ReturnedPreOperationObject = new JObject()
                        {
                            [_ElementName] = ItemAsArray
                        };
                    }
                    else
                    {
                        ReturnedPreOperationObject[_ElementName] = ItemAsArray;
                    }

                    //Key will be recreated by FromJsonToEntity
                    if (ReturnedPreOperationObject.ContainsKey(_KeyName))
                    {
                        ReturnedPreOperationObject.Remove(_KeyName);
                    }

                    var ItemAsEntity = FromJsonToEntity(Factory, _KeyName, _KeyValue, ReturnedPreOperationObject);
                    try
                    {
                        Transaction.Upsert(ItemAsEntity);
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->AddElementsToArrayItem: Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }

                    try
                    {
                        Transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->AddElementsToArrayItem: Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }

                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllNew)
                    {
                        GetItem(_Table, _KeyName, _KeyValue, null, out _ReturnItem, _ErrorMessageAction);
                    }
                }
                return true;
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
        public bool RemoveElementsFromArrayItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            string _ElementName,
            BPrimitiveType[] _ElementValueEntries,
            out JObject _ReturnItem,
            EBReturnItemBehaviour _ReturnItemBehaviour,
            Action<string> _ErrorMessageAction)
        {
            _ReturnItem = null;

            if (_ElementValueEntries == null || _ElementValueEntries.Length == 0)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: ElementValueEntries must contain values.");
                return false;
            }
            var ExpectedType = _ElementValueEntries[0].Type;
            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (_ElementValueEntry.Type != ExpectedType)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: ElementValueEntries must contain elements with the same type.");
                    return false;
                }
            }

            if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
            {
                JObject ReturnedPreOperationObject = null;
                using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                {
                    if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out ReturnedPreOperationObject, _ErrorMessageAction))
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->RemoveElementsFromArrayItem: GetItemInTransaction failed.");
                        return false;
                    }

                    if (ReturnedPreOperationObject == null)
                    {
                        //Does not exist
                        return true;
                    }

                    JArray ItemAsArray = null;

                    if (ReturnedPreOperationObject.ContainsKey(_ElementName))
                    {
                        if (ReturnedPreOperationObject[_ElementName].Type != JTokenType.Array)
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceGC->RemoveElementsFromArrayItem: Item is not an array.");
                            return false;
                        }
                        ItemAsArray = (JArray)ReturnedPreOperationObject[_ElementName];
                        if (ItemAsArray == null)
                        {
                            //Does not exist as an array
                            return true;
                        }
                    }

                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllOld)
                    {
                        if (ReturnedPreOperationObject != null)
                        {
                            _ReturnItem = new JObject(ReturnedPreOperationObject);
                        }
                    }

                    var NewArray = new JArray();
                    foreach (var CurToken in ItemAsArray)
                    {
                        bool bFound = false;
                        foreach (var _ElementValueEntry in _ElementValueEntries)
                        {
                            if (CompareJTokenWithBPrimitive(CurToken, _ElementValueEntry))
                            {
                                bFound = true;
                                break;
                            }
                        }
                        if (!bFound)
                        {
                            NewArray.Add(CurToken);
                        }
                    }
                    ItemAsArray = NewArray;
                    ReturnedPreOperationObject[_ElementName] = ItemAsArray;

                    //Key will be recreated by FromJsonToEntity
                    if (ReturnedPreOperationObject.ContainsKey(_KeyName))
                    {
                        ReturnedPreOperationObject.Remove(_KeyName);
                    }

                    var ItemAsEntity = FromJsonToEntity(Factory, _KeyName, _KeyValue, ReturnedPreOperationObject);
                    try
                    {
                        Transaction.Upsert(ItemAsEntity);
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->RemoveElementsFromArrayItem: Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }

                    try
                    {
                        Transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->RemoveElementsFromArrayItem: Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }

                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllNew)
                    {
                        GetItem(_Table, _KeyName, _KeyValue, null, out _ReturnItem, _ErrorMessageAction);
                    }
                }
                return true;
            }
            return false;
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
        public bool IncrementOrDecrementItemValue(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            out double _NewValue,
            string _ValueAttribute,
            double _IncrementOrDecrementBy,
            bool _bDecrement = false,
            Action<string> _ErrorMessageAction = null)
        {
            _NewValue = _IncrementOrDecrementBy;

            if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
            {
                using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                {
                    if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out JObject ReturnItem, _ErrorMessageAction))
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->IncrementOrDecrementItemValue: GetItemInTransaction failed.");
                        return false;
                    }

                    if (ReturnItem != null
                        && ReturnItem.ContainsKey(_ValueAttribute))
                    {
                        _NewValue = (double)ReturnItem[_ValueAttribute];
                        if (_bDecrement)
                        {
                            _NewValue -= _IncrementOrDecrementBy;
                        }
                        else
                        {
                            _NewValue += _IncrementOrDecrementBy;
                        }
                        ReturnItem[_ValueAttribute] = _NewValue;
                    }
                    else
                    {
                        if (ReturnItem == null)
                        {
                            ReturnItem = new JObject()
                            {
                                [_ValueAttribute] = _IncrementOrDecrementBy
                            };
                        }
                        else
                        {
                            ReturnItem[_ValueAttribute] = _IncrementOrDecrementBy;
                        }
                    }

                    if (ReturnItem.ContainsKey(_KeyName))
                    {
                        ReturnItem.Remove(_KeyName);
                    }

                    try
                    {
                        Transaction.Upsert(FromJsonToEntity(Factory, _KeyName, _KeyValue, ReturnItem));
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->IncrementOrDecrementItemValue: Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }

                    try
                    {
                        Transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->IncrementOrDecrementItemValue: Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }
                }
                return true;
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
        public bool DeleteItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            out JObject _ReturnItem,
            EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn,
            Action<string> _ErrorMessageAction = null)
        {
            _ReturnItem = null;

            if (LoadStoreAndGetKindKeyFactory(_Table, out KeyFactory Factory, _ErrorMessageAction))
            {
                using (DatastoreTransaction Transaction = DSDB.BeginTransaction())
                {
                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllOld)
                    {
                        if (!GetItemInTransaction(Transaction, Factory, _KeyName, _KeyValue, out _ReturnItem, _ErrorMessageAction))
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceGC->DeleteItem: GetItemInTransaction failed.");
                            return false;
                        }
                    }

                    try
                    {
                        Transaction.Delete(Factory.CreateKey(GetFinalKeyFromNameValue(_KeyName, _KeyValue)));
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->DeleteItem: Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }

                    try
                    {
                        Transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceGC->DeleteItem: Exception: " + e.Message + ", Trace: " + e.StackTrace);
                        return false;
                    }
                }
                return true;
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
        public bool ScanTable(
            string _Table, 
            out List<JObject> _ReturnItem, 
            Action<string> _ErrorMessageAction = null)
        {
            DatastoreQueryResults QueryResult = null;
            try
            {
                QueryResult = DSDB.RunQuery(new Query(_Table));
            }
            catch (Exception e)
            {
                _ReturnItem = null;
                _ErrorMessageAction?.Invoke("BDatabaseServiceGC->ScanTable: Exception: " + e.Message);
                return false;
            }

            _ReturnItem = new List<JObject>();
            if (QueryResult != null)
            {
                foreach (var Current in QueryResult.Entities)
                {
                    if (Current != null)
                    {
                        var AsJson = FromEntityToJson(Current);
                        string KeyCombined = Current.Key.Path[0].Name;
                        string[] KeySplitted = KeyCombined.Split(':');
                        if (KeySplitted != null && KeySplitted.Length >= 2)
                        {
                            string KeyName = KeySplitted[0];
                            string KeyValue = "";

                            for (int i = 1; i < KeySplitted.Length; i++)
                            {
                                KeyValue += KeySplitted[i];
                            }

                            AddKeyToJson(AsJson, KeyName, new BPrimitiveType(KeyValue));
                            BUtility.SortJObject(AsJson, true);

                            _ReturnItem.Add(AsJson);
                        }
                    }
                }
            }
            return true;
        }

        private class BAttributeEqualsConditionDatastore : BDatabaseAttributeCondition
        {
            public BAttributeEqualsConditionDatastore(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeEquals)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, BPrimitiveType>("Value", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeEqualsCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeEqualsConditionDatastore(Attribute, Value);
        }

        private class BAttributeNotEqualsConditionDatastore : BDatabaseAttributeCondition
        {
            public BAttributeNotEqualsConditionDatastore(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeNotEquals)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, BPrimitiveType>("Value", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeNotEqualsCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeNotEqualsConditionDatastore(Attribute, Value);
        }

        private class BAttributeGreaterConditionDatastore : BDatabaseAttributeCondition
        {
            public BAttributeGreaterConditionDatastore(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeGreater)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, BPrimitiveType>("Value", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeGreaterCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeGreaterConditionDatastore(Attribute, Value);
        }

        private class BAttributeGreaterOrEqualConditionDatastore : BDatabaseAttributeCondition
        {
            public BAttributeGreaterOrEqualConditionDatastore(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeGreaterOrEqual)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, BPrimitiveType>("Value", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeGreaterOrEqualCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeGreaterOrEqualConditionDatastore(Attribute, Value);
        }

        private class BAttributeLessConditionDatastore : BDatabaseAttributeCondition
        {
            public BAttributeLessConditionDatastore(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeLess)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, BPrimitiveType>("Value", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeLessCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeLessConditionDatastore(Attribute, Value);
        }

        private class BAttributeLessOrEqualConditionDatastore : BDatabaseAttributeCondition
        {
            public BAttributeLessOrEqualConditionDatastore(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeLessOrEqual)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute,
                    new Tuple<string, BPrimitiveType>("Value", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeLessOrEqualCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeLessOrEqualConditionDatastore(Attribute, Value);
        }

        private class BAttributeExistsConditionDatastore : BDatabaseAttributeCondition
        {
            public BAttributeExistsConditionDatastore(string Attribute) : base(EBDatabaseAttributeConditionType.AttributeExists)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }
        public BDatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute)
        {
            return new BAttributeExistsConditionDatastore(Attribute);
        }

        private class BAttributeNotExistConditionDatastore : BDatabaseAttributeCondition
        {
            public BAttributeNotExistConditionDatastore(string Attribute) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>(Attribute, null);
            }
        }
        public BDatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute)
        {
            return new BAttributeNotExistConditionDatastore(Attribute);
        }

        private class BArrayElementNotExistConditionDatastore : BDatabaseAttributeCondition
        {
            public BArrayElementNotExistConditionDatastore(BPrimitiveType ArrayElement) : base(EBDatabaseAttributeConditionType.ArrayElementNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>("N/A", new Tuple<string, BPrimitiveType>("N/A", ArrayElement));
            }
        }
        public BDatabaseAttributeCondition BuildArrayElementNotExistCondition(BPrimitiveType ArrayElement)
        {
            return new BArrayElementNotExistConditionDatastore(ArrayElement);
        }
    }
}