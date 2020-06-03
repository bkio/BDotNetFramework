/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using BCommonUtilities;
using Newtonsoft.Json.Linq;

namespace BCloudServiceUtilities.DatabaseServices
{
    public class BDatabaseServiceAWS : BDatabaseServiceBase, IBDatabaseServiceInterface
    {
        /// <summary>
        /// <para>AWS Dynamodb Client that is responsible to serve to this object</para>
        /// </summary>
        private readonly AmazonDynamoDBClient DynamoDBClient;

        /// <summary>
        /// <para>Holds initialization success</para>
        /// </summary>
        private readonly bool bInitializationSucceed;

        /// <summary>
        /// 
        /// <para>BDatabaseServiceAWS: Parametered Constructor for Managed Service by Amazon</para>
        /// 
        /// <para><paramref name="_AccessKey"/>                     AWS Access Key</para>
        /// <para><paramref name="_SecretKey"/>                     AWS Secret Key</para>
        /// <para><paramref name="_Region"/>                        AWS Region that DynamoDB Client will connect to (I.E. eu-west-1)</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>        
        /// 
        /// </summary>
        public BDatabaseServiceAWS(
            string _AccessKey,
            string _SecretKey,
            string _Region,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                DynamoDBClient = new AmazonDynamoDBClient(new Amazon.Runtime.BasicAWSCredentials(_AccessKey, _SecretKey), Amazon.RegionEndpoint.GetBySystemName(_Region));
                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        /// <summary>
        /// 
        /// <para>BDatabaseServiceAWS: Parametered Constructor for Local DynamoDB Edition</para>
        /// 
        /// <para><paramref name="_ServiceURL"/>                     Service URL for DynamoDB</para>
        /// <para><paramref name="_ErrorMessageAction"/>             Error messages will be pushed to this action</para>           
        /// 
        /// </summary>
        public BDatabaseServiceAWS(
            string _ServiceURL,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                DynamoDBClient = new AmazonDynamoDBClient("none", "none", new AmazonDynamoDBConfig
                {
                    ServiceURL = _ServiceURL
                });
                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        ~BDatabaseServiceAWS()
        {
            DynamoDBClient?.Dispose();
        }

        /// <summary>
        /// <para>Map that holds loaded table definition instances</para>
        /// </summary>
        private readonly Dictionary<string, Table> LoadedTables = new Dictionary<string, Table>();
        private readonly object LoadedTables_DictionaryLock = new object();

        /// <summary>
        /// <para>Searches table definition in LoadedTables, if not loaded, loads, stores and returns</para>
        /// </summary>
        private bool LoadStoreAndGetTable(
            string _Table, 
            out Table _ResultTable, 
            Action<string> _ErrorMessageAction = null)
        {
            bool bResult = true;
            lock (LoadedTables_DictionaryLock)
            {
                if (!LoadedTables.ContainsKey(_Table))
                {
                    if (Table.TryLoadTable(DynamoDBClient, _Table, out _ResultTable))
                    {
                        LoadedTables[_Table] = _ResultTable;
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->LoadStoreAndGetTable: Table has not been found.");
                        bResult = false;
                    }
                }
                else
                {
                    _ResultTable = LoadedTables[_Table];
                }
            }
            return bResult;
        }

        /// <summary>
        /// 
        /// <para>HasInitializationSucceed</para>
        /// 
        /// <para>Check <seealso cref="IBDatabaseServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        /// 
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
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

            bool bGetAll = false;
            if (_ValuesToGet == null || _ValuesToGet.Length == 0)
            {
                bGetAll = true;
            }

            //Try getting table definition
            if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
            {
                GetItemOperationConfig Config;
                if (!bGetAll)
                {
                    List<string> ValuesToGet = new List<string>(_ValuesToGet);
                    Config = new GetItemOperationConfig
                    {
                        AttributesToGet = ValuesToGet,
                        ConsistentRead = true
                    };
                }
                else
                {
                    Config = new GetItemOperationConfig
                    {
                        ConsistentRead = true
                    };
                }

                //Get item from the table
                try
                {               
                    using (var _GetItem = TableObject.GetItemAsync(_KeyValue.ToString(), Config))
                    {
                        _GetItem.Wait();

                        var ReturnedDocument = _GetItem.Result;
                        if (ReturnedDocument != null)
                        {
                            //Convert to string and parse as JObject
                            _Result = JObject.Parse(ReturnedDocument.ToJson());
                            AddKeyToJson(_Result, _KeyName, _KeyValue);
                            BUtility.SortJObject(_Result);
                        }
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->GetItem: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
                return true;
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
            _ReturnItem = null;

            var Item = new JObject(_Item);
            if (Item != null && !Item.ContainsKey(_KeyName))
            {
                switch (_KeyValue.Type)
                {
                    case EBPrimitiveTypeEnum.Double:
                        Item[_KeyName] = _KeyValue.AsDouble;
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        Item[_KeyName] = _KeyValue.AsInteger;
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        Item[_KeyName] = Convert.ToBase64String(_KeyValue.AsByteArray);
                        break;
                    default:
                        Item[_KeyName] = _KeyValue.AsString;
                        break;
                }
            }

            //First convert JObject to AWS Document
            Document ItemAsDocument = null;
            try
            {
                ItemAsDocument = Document.FromJson(Item.ToString());
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->PutItem->JObject-Document Conversion: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }

            if (ItemAsDocument != null)
            {
                //Try getting table definition
                if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
                {
                    var Config = new PutItemOperationConfig();
                    
                    //Set return value expectation
                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                    {
                        Config.ReturnValues = ReturnValues.None;
                    }
                    else if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllNew)
                    {
                        Config.ReturnValues = ReturnValues.AllNewAttributes;
                    }
                    else
                    {
                        Config.ReturnValues = ReturnValues.AllOldAttributes;
                    }

                    //Set condition expression
                    if (_ConditionExpression != null)
                    {
                        var BuiltCondition = _ConditionExpression.GetBuiltCondition();
                        if (BuiltCondition != null)
                        {
                            Expression ConditionalExpression = new Expression
                            {
                                ExpressionStatement = BuiltCondition.Item1
                            };
                            if (BuiltCondition.Item2 != null)
                            {
                                switch (BuiltCondition.Item2.Item2.Type)
                                {
                                    case EBPrimitiveTypeEnum.String:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsString;
                                        break;
                                    case EBPrimitiveTypeEnum.Integer:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsInteger;
                                        break;
                                    case EBPrimitiveTypeEnum.Double:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsDouble;
                                        break;
                                    case EBPrimitiveTypeEnum.ByteArray:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.ToString();
                                        break;
                                }
                            }
                            Config.ConditionalExpression = ConditionalExpression;
                        }
                    }

                    //Put item to the table
                    try
                    {
                        using (var _PutItem = TableObject.PutItemAsync(ItemAsDocument, Config))
                        {
                            _PutItem.Wait();

                            var ReturnedDocument = _PutItem.Result;

                            if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                            {
                                return true;
                            }
                            else if (ReturnedDocument != null)
                            {
                                //Convert to string and parse as JObject
                                _ReturnItem = JObject.Parse(ReturnedDocument.ToJson());
                                BUtility.SortJObject(_ReturnItem);
                                return true;
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->PutItem: TableObject.PutItem returned null.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!(e is ConditionalCheckFailedException))
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->PutItem: " + e.Message + ", Trace: " + e.StackTrace);
                        }
                        return false;
                    }
                }
            }
            else
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->PutItem->JObject-Document Conversion: ItemAsDocument is null.");
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
            _ReturnItem = null;

            var UpdateItem = new JObject(_UpdateItem);
            if (UpdateItem != null && !UpdateItem.ContainsKey(_KeyName))
            {
                switch (_KeyValue.Type)
                {
                    case EBPrimitiveTypeEnum.Double:
                        UpdateItem[_KeyName] = _KeyValue.AsDouble;
                        break;
                    case EBPrimitiveTypeEnum.Integer:
                        UpdateItem[_KeyName] = _KeyValue.AsInteger;
                        break;
                    case EBPrimitiveTypeEnum.ByteArray:
                        UpdateItem[_KeyName] = Convert.ToBase64String(_KeyValue.AsByteArray);
                        break;
                    default:
                        UpdateItem[_KeyName] = _KeyValue.AsString;
                        break;
                }
            }

            //First convert JObject to AWS Document
            Document ItemAsDocument = null;
            try
            {
                ItemAsDocument = Document.FromJson(UpdateItem.ToString());
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->UpdateItem->JObject-Document Conversion: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }

            if (ItemAsDocument != null)
            {
                //Try getting table definition
                if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
                {
                    UpdateItemOperationConfig Config = new UpdateItemOperationConfig();

                    //Set return value expectation
                    if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                    {
                        Config.ReturnValues = ReturnValues.None;
                    }
                    else if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllOld)
                    {
                        Config.ReturnValues = ReturnValues.AllOldAttributes;
                    }
                    else
                    {
                        Config.ReturnValues = ReturnValues.AllNewAttributes;
                    }

                    //Set condition expression
                    if (_ConditionExpression != null)
                    {
                        var BuiltCondition = _ConditionExpression.GetBuiltCondition();
                        if (BuiltCondition != null)
                        {
                            Expression ConditionalExpression = new Expression
                            {
                                ExpressionStatement = BuiltCondition.Item1
                            };
                            if (BuiltCondition.Item2 != null)
                            {
                                switch (BuiltCondition.Item2.Item2.Type)
                                {
                                    case EBPrimitiveTypeEnum.String:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsString;
                                        break;
                                    case EBPrimitiveTypeEnum.Integer:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsInteger;
                                        break;
                                    case EBPrimitiveTypeEnum.Double:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.AsDouble;
                                        break;
                                    case EBPrimitiveTypeEnum.ByteArray:
                                        ConditionalExpression.ExpressionAttributeValues[BuiltCondition.Item2.Item1] = BuiltCondition.Item2.Item2.ToString();
                                        break;
                                }
                            }
                            Config.ConditionalExpression = ConditionalExpression;
                        }
                    }

                    //Update item in the table
                    try
                    {
                        using (var ItemTask = TableObject.UpdateItemAsync(ItemAsDocument, Config))
                        {
                            ItemTask.Wait();

                            var ReturnedDocument = ItemTask.Result;

                            if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                            {
                                return true;
                            }
                            else if (ReturnedDocument != null)
                            {
                                //Convert to string and parse as JObject
                                _ReturnItem = JObject.Parse(ReturnedDocument.ToJson());
                                BUtility.SortJObject(_ReturnItem);
                                return true;
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->UpdateItem: TableObject.UpdateItem returned null.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!(e is ConditionalCheckFailedException))
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->UpdateItem: " + e.Message + ", Trace: " + e.StackTrace);
                        }
                        return false;
                    }
                }
            }
            else
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->UpdateItem->JObject-Document Conversion: ItemAsDocument is null.");
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

            if (DynamoDBClient == null)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: DynamoDBClient is null.");
                return false;
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: Key is null.");
                return false;
            }

            UpdateItemRequest Request = new UpdateItemRequest
            {
                TableName = _Table,
                Key = new Dictionary<string, AttributeValue>()
            };

            if (_KeyValue.Type == EBPrimitiveTypeEnum.Integer)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsInteger.ToString() };
            }
            else if (_KeyValue.Type == EBPrimitiveTypeEnum.Double)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsDouble.ToString() };
            }
            else if (_KeyValue.Type == EBPrimitiveTypeEnum.String)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.AsString };
            }
            else if (_KeyValue.Type == EBPrimitiveTypeEnum.ByteArray)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.ToString() };
            }

            //Set return value expectation
            if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
            {
                Request.ReturnValues = ReturnValue.NONE;
            }
            else if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllOld)
            {
                Request.ReturnValues = ReturnValue.ALL_OLD;
            }
            else
            {
                Request.ReturnValues = ReturnValue.ALL_NEW;
            }

            var SetAsList = new List<string>();
            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (ExpectedType == EBPrimitiveTypeEnum.Integer || ExpectedType == EBPrimitiveTypeEnum.Double)
                {
                    if (_ElementValueEntry.Type == EBPrimitiveTypeEnum.Integer)
                    {
                        SetAsList.Add(_ElementValueEntry.AsInteger.ToString());
                    }
                    else
                    {
                        SetAsList.Add(_ElementValueEntry.AsDouble.ToString());
                    }
                }
                else
                {
                    if (ExpectedType == EBPrimitiveTypeEnum.ByteArray)
                    {
                        SetAsList.Add(_ElementValueEntry.ToString());
                    }
                    else
                    {
                        SetAsList.Add(_ElementValueEntry.AsString);
                    }
                }
            }

            Request.ExpressionAttributeNames = new Dictionary<string, string>()
            {
                {
                    "#V", _ElementName
                }
            };
            Request.UpdateExpression = "ADD #V :vals";

            if (ExpectedType == EBPrimitiveTypeEnum.Integer || ExpectedType == EBPrimitiveTypeEnum.Double)
            {
                Request.ExpressionAttributeValues.Add(":vals", new AttributeValue { NS = SetAsList });
            }
            else
            {
                Request.ExpressionAttributeValues.Add(":vals", new AttributeValue { SS = SetAsList });
            }

            if (_ConditionExpression != null)
            {
                if (_ConditionExpression.AttributeConditionType == EBDatabaseAttributeConditionType.ArrayElementNotExist)
                {
                    var BuiltCondition = _ConditionExpression.GetBuiltCondition();

                    if (BuiltCondition.Item1 == null || BuiltCondition.Item2 == null || BuiltCondition.Item2.Item2 == null)
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: Invalid condition expression.");
                        return false;
                    }
                    
                    if (BuiltCondition.Item2.Item2.Type == EBPrimitiveTypeEnum.Integer)
                    {
                        Request.ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { NS = new List<string>() { BuiltCondition.Item2.Item2.AsInteger.ToString() } });
                    }
                    else if (BuiltCondition.Item2.Item2.Type == EBPrimitiveTypeEnum.Double)
                    {
                        Request.ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { NS = new List<string>() { BuiltCondition.Item2.Item2.AsDouble.ToString() } });
                    }
                    else if (BuiltCondition.Item2.Item2.Type == EBPrimitiveTypeEnum.String)
                    {
                        Request.ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { SS = new List<string>() { BuiltCondition.Item2.Item2.AsString } });
                    }
                    else if (BuiltCondition.Item2.Item2.Type == EBPrimitiveTypeEnum.ByteArray)
                    {
                        Request.ExpressionAttributeValues.Add(BuiltCondition.Item2.Item1, new AttributeValue { SS = new List<string>() { BuiltCondition.Item2.Item2.ToString() } });
                    }

                    Request.ConditionExpression = BuiltCondition.Item1.Replace("$ARRAY_NAME$", _ElementName);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: Condition is not valid for this operation.");
                    return false;
                }
            }

            //Update item in the table
            try
            {
                using (var _UpdateItem = DynamoDBClient.UpdateItemAsync(Request))
                {
                    _UpdateItem.Wait();

                    if (_ReturnItemBehaviour != EBReturnItemBehaviour.DoNotReturn)
                    {
                        var Response = _UpdateItem.Result;

                        if (Response != null && Response.Attributes != null)
                        {
                            _ReturnItem = JObject.Parse(Document.FromAttributeMap(Response.Attributes).ToJson());
                            BUtility.SortJObject(_ReturnItem);
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: DynamoDBClient.UpdateItem returned null.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!(e is ConditionalCheckFailedException))
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->AddElementsToArrayItem: " + e.Message + ", Trace: " + e.StackTrace);
                }
                return false;
            }
            return true;
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

            if (DynamoDBClient == null)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->RemoveElementsFromArrayItem: DynamoDBClient is null.");
                return false;
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->RemoveElementsFromArrayItem: Key is null.");
                return false;
            }

            UpdateItemRequest Request = new UpdateItemRequest
            {
                TableName = _Table,
                Key = new Dictionary<string, AttributeValue>()
            };

            if (_KeyValue.Type == EBPrimitiveTypeEnum.Integer)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsInteger.ToString() };
            }
            else if (_KeyValue.Type == EBPrimitiveTypeEnum.Double)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsDouble.ToString() };
            }
            else if (_KeyValue.Type == EBPrimitiveTypeEnum.String)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.AsString };
            }
            else if (_KeyValue.Type == EBPrimitiveTypeEnum.ByteArray)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.ToString() };
            }

            //Set return value expectation
            if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
            {
                Request.ReturnValues = ReturnValue.NONE;
            }
            else if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllOld)
            {
                Request.ReturnValues = ReturnValue.ALL_OLD;
            }
            else
            {
                Request.ReturnValues = ReturnValue.ALL_NEW;
            }

            var SetAsList = new List<string>();
            foreach (var _ElementValueEntry in _ElementValueEntries)
            {
                if (ExpectedType == EBPrimitiveTypeEnum.Integer || ExpectedType == EBPrimitiveTypeEnum.Double)
                {
                    if (_ElementValueEntry.Type == EBPrimitiveTypeEnum.Integer)
                    {
                        SetAsList.Add(_ElementValueEntry.AsInteger.ToString());
                    }
                    else
                    {
                        SetAsList.Add(_ElementValueEntry.AsDouble.ToString());
                    }
                }
                else
                {
                    if (ExpectedType == EBPrimitiveTypeEnum.ByteArray)
                    {
                        SetAsList.Add(_ElementValueEntry.ToString());
                    }
                    else
                    {
                        SetAsList.Add(_ElementValueEntry.AsString);
                    }
                }
            }

            Request.ExpressionAttributeNames = new Dictionary<string, string>()
            {
                {
                    "#V", _ElementName
                }
            };
            Request.UpdateExpression = "DELETE #V :vals";

            if (ExpectedType == EBPrimitiveTypeEnum.Integer || ExpectedType == EBPrimitiveTypeEnum.Double)
            {
                Request.ExpressionAttributeValues.Add(":vals", new AttributeValue { NS = SetAsList });
            }
            else
            {
                Request.ExpressionAttributeValues.Add(":vals", new AttributeValue { SS = SetAsList });
            }

            //Update item in the table
            try
            {
                using (var _UpdateItem = DynamoDBClient.UpdateItemAsync(Request))
                {
                    _UpdateItem.Wait();

                    if (_ReturnItemBehaviour != EBReturnItemBehaviour.DoNotReturn)
                    {
                        var Response = _UpdateItem.Result;

                        if (Response != null && Response.Attributes != null)
                        {
                            _ReturnItem = JObject.Parse(Document.FromAttributeMap(Response.Attributes).ToJson());
                            BUtility.SortJObject(_ReturnItem);
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->RemoveElementsFromArrayItem: DynamoDBClient.UpdateItem returned null.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!(e is ConditionalCheckFailedException))
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->RemoveElementsFromArrayItem: " + e.Message + ", Trace: " + e.StackTrace);
                }
                return false;
            }
            return true;
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
            _NewValue = 0.0f;

            if (DynamoDBClient == null)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->IncrementOrDecrementItemValue: DynamoDBClient is null.");
                return false;
            }

            if (_KeyValue == null)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->IncrementOrDecrementItemValue: Key is null.");
                return false;
            }

            UpdateItemRequest Request = new UpdateItemRequest
            {
                TableName = _Table,
                Key = new Dictionary<string, AttributeValue>()
            };

            if (_KeyValue.Type == EBPrimitiveTypeEnum.Integer)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsInteger.ToString() };
            }
            else if (_KeyValue.Type == EBPrimitiveTypeEnum.Double)
            {
                Request.Key[_KeyName] = new AttributeValue { N = _KeyValue.AsDouble.ToString() };
            }
            else if (_KeyValue.Type == EBPrimitiveTypeEnum.String)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.AsString };
            }
            else if (_KeyValue.Type == EBPrimitiveTypeEnum.ByteArray)
            {
                Request.Key[_KeyName] = new AttributeValue { S = _KeyValue.ToString() };
            }

            //Set return value expectation
            Request.ReturnValues = ReturnValue.UPDATED_NEW;

            Request.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":incr"] = new AttributeValue
                {
                    N = (_IncrementOrDecrementBy * (_bDecrement ? -1 : 1)).ToString()
                },
                [":start"] = new AttributeValue
                {
                    N = "0"
                }
            };
            Request.ExpressionAttributeNames = new Dictionary<string, string>()
            {
                {
                    "#V", _ValueAttribute
                }
            };
            Request.UpdateExpression = "SET #V = if_not_exists(#V, :start) + :incr";

            //Update item in the table
            try
            {
                using (var _UpdateItem = DynamoDBClient.UpdateItemAsync(Request))
                {
                    _UpdateItem.Wait();

                    var Response = _UpdateItem.Result;

                    if (Response != null && Response.Attributes != null && Response.Attributes.ContainsKey(_ValueAttribute))
                    {
                        if (!double.TryParse(Response.Attributes[_ValueAttribute].N, out _NewValue))
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->IncrementOrDecrementItemValue: Cast from returned attribute to double has failed.");
                            return false;
                        }
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->IncrementOrDecrementItemValue: DynamoDBClient.UpdateItem returned null.");
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->IncrementOrDecrementItemValue: " + e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
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

            //Try getting table definition
            if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
            {
                DeleteItemOperationConfig Config = new DeleteItemOperationConfig();

                //Set return value expectation
                if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                {
                    Config.ReturnValues = ReturnValues.None;
                }
                else if (_ReturnItemBehaviour == EBReturnItemBehaviour.ReturnAllNew)
                {
                    Config.ReturnValues = ReturnValues.AllNewAttributes;
                }
                else
                {
                    Config.ReturnValues = ReturnValues.AllOldAttributes;
                }

                //Delete item from the table
                try
                {
                    using (var _DeleteItem = TableObject.DeleteItemAsync(_KeyValue.ToString(), Config))
                    {
                        _DeleteItem.Wait();

                        var ReturnedDocument = _DeleteItem.Result;

                        if (_ReturnItemBehaviour == EBReturnItemBehaviour.DoNotReturn)
                        {
                            return true;
                        }
                        else if (ReturnedDocument != null)
                        {
                            //Convert to string and parse as JObject
                            _ReturnItem = JObject.Parse(ReturnedDocument.ToJson());
                            BUtility.SortJObject(_ReturnItem);
                            return true;
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->DeleteItem: TableObject.DeleteItem returned null.");
                        }
                    }
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->DeleteItem: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
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
            _ReturnItem = null;

            //Try getting table definition
            if (LoadStoreAndGetTable(_Table, out Table TableObject, _ErrorMessageAction))
            {
                var Config = new ScanOperationConfig()
                {
                    Select = SelectValues.AllAttributes
                };

                //Scan the table
                Search ReturnedSearch = null;

                try
                {
                    ReturnedSearch = TableObject.Scan(Config);
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BDatabaseServiceAWS->ScanTable: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }

                if (ReturnedSearch != null)
                {
                    List<JObject> TempResults = new List<JObject>();
                    try
                    {
                        do
                        {
                            using (var _GetNextSet = ReturnedSearch.GetNextSetAsync())
                            {
                                _GetNextSet.Wait();
                                List<Document> DocumentList = _GetNextSet.Result;

                                foreach (var Document in DocumentList)
                                {
                                    var CreatedJson = JObject.Parse(Document.ToJson());
                                    BUtility.SortJObject(CreatedJson);
                                    TempResults.Add(CreatedJson);
                                }
                            }
                        }
                        while (!ReturnedSearch.IsDone);

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
            }
            return false;
        }

        private class BAttributeEqualsConditionDynamodb : BDatabaseAttributeCondition
        {
            public BAttributeEqualsConditionDynamodb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeEquals)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute + " = :val",
                    new Tuple<string, BPrimitiveType>(":val", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeEqualsCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeEqualsConditionDynamodb(Attribute, Value);
        }

        private class BAttributeNotEqualsConditionDynamodb : BDatabaseAttributeCondition
        {
            public BAttributeNotEqualsConditionDynamodb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeNotEquals)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute + " <> :val",
                    new Tuple<string, BPrimitiveType>(":val", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeNotEqualsCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeNotEqualsConditionDynamodb(Attribute, Value);
        }

        private class BAttributeGreaterConditionDynamodb : BDatabaseAttributeCondition
        {
            public BAttributeGreaterConditionDynamodb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeGreater)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute + " > :val",
                    new Tuple<string, BPrimitiveType>(":val", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeGreaterCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeGreaterConditionDynamodb(Attribute, Value);
        }

        private class BAttributeGreaterOrEqualConditionDynamodb : BDatabaseAttributeCondition
        {
            public BAttributeGreaterOrEqualConditionDynamodb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeGreaterOrEqual)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute + " >= :val",
                    new Tuple<string, BPrimitiveType>(":val", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeGreaterOrEqualCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeGreaterOrEqualConditionDynamodb(Attribute, Value);
        }

        private class BAttributeLessConditionDynamodb : BDatabaseAttributeCondition
        {
            public BAttributeLessConditionDynamodb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeLess)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute + " < :val",
                    new Tuple<string, BPrimitiveType>(":val", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeLessCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeLessConditionDynamodb(Attribute, Value);
        }

        private class BAttributeLessOrEqualConditionDynamodb : BDatabaseAttributeCondition
        {
            public BAttributeLessOrEqualConditionDynamodb(string Attribute, BPrimitiveType Value) : base(EBDatabaseAttributeConditionType.AttributeLessOrEqual)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    Attribute + " <= :val",
                    new Tuple<string, BPrimitiveType>(":val", Value)
                );
            }
        }
        public BDatabaseAttributeCondition BuildAttributeLessOrEqualCondition(string Attribute, BPrimitiveType Value)
        {
            return new BAttributeLessOrEqualConditionDynamodb(Attribute, Value);
        }

        private class BAttributeExistsConditionDynamodb : BDatabaseAttributeCondition
        {
            public BAttributeExistsConditionDynamodb(string Attribute) : base(EBDatabaseAttributeConditionType.AttributeExists)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>("attribute_exists(" + Attribute + ")", null);
            }
        }
        public BDatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute)
        {
            return new BAttributeExistsConditionDynamodb(Attribute);
        }

        private class BAttributeNotExistConditionDynamodb : BDatabaseAttributeCondition
        {
            public BAttributeNotExistConditionDynamodb(string Attribute) : base(EBDatabaseAttributeConditionType.AttributeNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>("attribute_not_exists(" + Attribute + ")", null);
            }
        }
        public BDatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute)
        {
            return new BAttributeNotExistConditionDynamodb(Attribute);
        }

        private class BArrayElementNotExistConditionDynamodb : BDatabaseAttributeCondition
        {
            public BArrayElementNotExistConditionDynamodb(BPrimitiveType ArrayElement) : base(EBDatabaseAttributeConditionType.ArrayElementNotExist)
            {
                BuiltCondition = new Tuple<string, Tuple<string, BPrimitiveType>>
                (
                    "NOT CONTAINS $ARRAY_NAME$ :cond_val",
                    new Tuple<string, BPrimitiveType>(":cond_val", ArrayElement)
                );
            }
        }
        public BDatabaseAttributeCondition BuildArrayElementNotExistCondition(BPrimitiveType ArrayElement)
        {
            return new BArrayElementNotExistConditionDynamodb(ArrayElement);
        }
    }
}