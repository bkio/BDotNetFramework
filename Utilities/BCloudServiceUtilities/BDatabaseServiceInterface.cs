/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using BCommonUtilities;

namespace BCloudServiceUtilities
{
    /// <summary>
    /// <para>After performing an operation that causes a change in an item, defines what service shall return</para>
    /// </summary>
    public enum EBReturnItemBehaviour
    {
        DoNotReturn,
        ReturnAllOld,
        ReturnAllNew
    };

    public enum EBDatabaseAttributeConditionType
    {
        AttributeEquals,
        AttributeNotEquals,
        AttributeGreater,
        AttributeGreaterOrEqual,
        AttributeLess,
        AttributeLessOrEqual,
        AttributeExists,
        AttributeNotExist,
        ArrayElementNotExist
    };

    public abstract class BDatabaseAttributeCondition
    {
        public readonly EBDatabaseAttributeConditionType AttributeConditionType;

        private BDatabaseAttributeCondition() {}
        protected BDatabaseAttributeCondition(EBDatabaseAttributeConditionType _AttributeConditionType)
        {
            AttributeConditionType = _AttributeConditionType;
        }

        protected Tuple<string, Tuple<string, BPrimitiveType>> BuiltCondition;
        public Tuple<string, Tuple<string, BPrimitiveType>> GetBuiltCondition()
        {
            if (BuiltCondition != null && BuiltCondition.Item1 != null)
            {
                return new Tuple<string, Tuple<string, BPrimitiveType>>(BuiltCondition.Item1, BuiltCondition.Item2 != null ? new Tuple<string, BPrimitiveType>(BuiltCondition.Item2.Item1, BuiltCondition.Item2.Item2) : null);
            }
            return null;
        }
    };

    public class BDatabaseServiceBase
    {
        protected BDatabaseServiceBase() {}

        protected Newtonsoft.Json.Linq.JToken FromBPrimitiveTypeToJToken(BPrimitiveType _Primitive)
        {
            switch (_Primitive.Type)
            {
                case EBPrimitiveTypeEnum.Double:
                    return _Primitive.AsDouble;
                case EBPrimitiveTypeEnum.Integer:
                    return _Primitive.AsInteger;
                case EBPrimitiveTypeEnum.ByteArray:
                    return Convert.ToBase64String(_Primitive.AsByteArray);
                default:
                    return _Primitive.AsString;
            }
        }

        protected void AddKeyToJson(Newtonsoft.Json.Linq.JObject Destination, string _KeyName, BPrimitiveType _KeyValue)
        {
            if (Destination != null && !Destination.ContainsKey(_KeyName))
            {
                Destination[_KeyName] = FromBPrimitiveTypeToJToken(_KeyValue);
            }
        }
    }

    /// <summary>
    /// <para>Interface for abstracting Database Services to make it usable with multiple cloud solutions</para>
    /// </summary>
    public interface IBDatabaseServiceInterface
    {
        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        bool HasInitializationSucceed();

        BDatabaseAttributeCondition BuildAttributeEqualsCondition(string Attribute, BPrimitiveType Value);
        BDatabaseAttributeCondition BuildAttributeNotEqualsCondition(string Attribute, BPrimitiveType Value);
        BDatabaseAttributeCondition BuildAttributeGreaterCondition(string Attribute, BPrimitiveType Value);
        BDatabaseAttributeCondition BuildAttributeGreaterOrEqualCondition(string Attribute, BPrimitiveType Value);
        BDatabaseAttributeCondition BuildAttributeLessCondition(string Attribute, BPrimitiveType Value);
        BDatabaseAttributeCondition BuildAttributeLessOrEqualCondition(string Attribute, BPrimitiveType Value);
        BDatabaseAttributeCondition BuildAttributeExistsCondition(string Attribute);
        BDatabaseAttributeCondition BuildAttributeNotExistCondition(string Attribute);
        BDatabaseAttributeCondition BuildArrayElementNotExistCondition(BPrimitiveType ArrayElement);

        /// <summary>
        /// 
        /// <para>GetItem</para>
        /// 
        /// <para>Gets an item from a table, if _ValuesToGet is null; will retrieve all.</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of item</para>
        /// <para><paramref name="_ValuesToGet"/>                   Defines which values shall be retrieved</para>
        /// <para><paramref name="_Result"/>                        Result as JSON Object</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool GetItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            string[] _ValuesToGet,
            out Newtonsoft.Json.Linq.JObject _Result,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>PutItem</para>
        /// 
        /// <para>Puts an item to a table</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of item</para>
        /// <para><paramref name="_Item"/>                          Item to be put</para>
        /// <para><paramref name="_ReturnItem"/>                    In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>           In case item exists, defines what service shall return</para>
        /// <para><paramref name="_ConditionExpression"/>           Condition expression to be performed remotely</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool PutItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            Newtonsoft.Json.Linq.JObject _Item,
            out Newtonsoft.Json.Linq.JObject _ReturnItem,
            EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn,
            BDatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>UpdateItem</para>
        /// 
        /// <para>Updates an item in a table</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of item</para>
        /// <para><paramref name="_UpdateItem"/>                    Item to be updated with</para>
        /// <para><paramref name="_ReturnItem"/>                    In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>           In case item exists, defines what service shall return</para>
        /// <para><paramref name="_ConditionExpression"/>           Condition expression to be performed remotely</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool UpdateItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            Newtonsoft.Json.Linq.JObject _UpdateItem,
            out Newtonsoft.Json.Linq.JObject _ReturnItem,
            EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn,
            BDatabaseAttributeCondition _ConditionExpression = null,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>AddElementsToArrayItem</para>
        /// 
        /// <para>Adds element to the array item</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of array item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of array item</para>
        /// <para><paramref name="_ElementName"/>                   Name of the array element</para>
        /// <para><paramref name="_ElementValueEntries"/>           Items to be put into array element</para>
        /// <para><paramref name="_ReturnItem"/>                    In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>           In case item exists, defines what service shall return</para>
        /// <para><paramref name="_ConditionExpression"/>           Condition expression to be performed remotely</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool AddElementsToArrayItem(
            string _Table, 
            string _KeyName,
            BPrimitiveType _KeyValue, 
            string _ElementName,
            BPrimitiveType[] _ElementValueEntries, 
            out Newtonsoft.Json.Linq.JObject _ReturnItem, 
            EBReturnItemBehaviour _ReturnItemBehaviour, 
            BDatabaseAttributeCondition _ConditionExpression, 
            Action<string> _ErrorMessageAction);

        /// <summary>
        /// 
        /// <para>RemoveElementsFromArrayItem</para>
        /// 
        /// <para>Removes element from the array item</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                     Table name</para>
        /// <para><paramref name="_KeyName"/>                   Name of the key of array item</para>
        /// <para><paramref name="_KeyValue"/>                  Value of the key of array item</para>
        /// <para><paramref name="_ElementName"/>               Name of the array element</para>
        /// <para><paramref name="_ElementValueEntries"/>       Items to be removed from array element</para>
        /// <para><paramref name="_ReturnItem"/>                In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>       In case item exists, defines what service shall return</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool RemoveElementsFromArrayItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            string _ElementName,
            BPrimitiveType[] _ElementValueEntries,
            out Newtonsoft.Json.Linq.JObject _ReturnItem,
            EBReturnItemBehaviour _ReturnItemBehaviour,
            Action<string> _ErrorMessageAction);

        /// <summary>
        /// 
        /// <para>IncrementOrDecrementItemValue</para>
        /// 
        /// <para>Updates an item in a table, if item does not exist, creates a new one with only increment/decrement value</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                         Table name</para>
        /// <para><paramref name="_KeyName"/>                       Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                      Value of the key of item</para>
        /// <para><paramref name="_NewValue"/>                      New value after increment/decrement</para>
        /// <para><paramref name="_ValueAttribute"/>                Name of the value</para>
        /// <para><paramref name="_IncrementOrDecrementBy"/>        Increment or decrement the value by this</para>
        /// <para><paramref name="_bDecrement"/>                    If true, will be decremented, otherwise incremented</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool IncrementOrDecrementItemValue(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            out double _NewValue,
            string _ValueAttribute,
            double _IncrementOrDecrementBy,
            bool _bDecrement = false,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>DeleteItem</para>
        /// 
        /// <para>Deletes an item from a table</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                     Table name</para>
        /// <para><paramref name="_KeyName"/>                   Name of the key of item</para>
        /// <para><paramref name="_KeyValue"/>                  Value of the key of item</para>
        /// <para><paramref name="_ReturnItem"/>                In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ReturnItemBehaviour"/>       In case item exists, defines what service shall return</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                  Operation success</returns>
        /// 
        /// </summary>
        bool DeleteItem(
            string _Table,
            string _KeyName,
            BPrimitiveType _KeyValue,
            out Newtonsoft.Json.Linq.JObject _ReturnItem,
            EBReturnItemBehaviour _ReturnItemBehaviour = EBReturnItemBehaviour.DoNotReturn,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>ScanTable</para>
        /// 
        /// <para>Scans the table for attribute specified by _Key</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Table"/>                 Table name</para>
        /// <para><paramref name="_ReturnItem"/>            In case item exists, fills his variable with returned item</para>
        /// <para><paramref name="_ErrorMessageAction"/>    Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                              Operation success</returns>
        /// 
        /// </summary>
        bool ScanTable(
            string _Table,
            out List<Newtonsoft.Json.Linq.JObject> _ReturnItem,
            Action<string> _ErrorMessageAction = null);
    }
}