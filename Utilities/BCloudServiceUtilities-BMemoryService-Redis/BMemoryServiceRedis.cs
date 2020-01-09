/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BCommonUtilities;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace BCloudServiceUtilities.MemoryServices
{
    public class BMemoryServiceRedis : BRedisCommonFunctionalities, IBMemoryServiceInterface
    {
        private readonly IBPubSubServiceInterface PubSubService = null;

        /// <summary>
        /// 
        /// <para>BMemoryServiceRedis: Parametered Constructor</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_RedisEndpoint"/>           Redis Endpoint without Port</para>
        /// <para><paramref name="_RedisPort"/>               Redis Endpoint Port</para>
        /// <para><paramref name="_RedisPassword"/>           Redis Server Password</para>
        /// <para><paramref name="_PubSubService"/>           Pub/Sub Service Instance</para>
        /// 
        /// </summary>
        public BMemoryServiceRedis(
            string _RedisEndpoint,
            int _RedisPort,
            string _RedisPassword,
            IBPubSubServiceInterface _PubSubService,
            Action<string> _ErrorMessageAction = null) : base("BMemoryServiceRedis", _RedisEndpoint, _RedisPort, _RedisPassword, _ErrorMessageAction)
        {
            PubSubService = _PubSubService;
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IBMemoryServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        private BPrimitiveType ConvertRedisValueToPrimitiveType(RedisValue _Input)
        {
            if (_Input.IsNullOrEmpty) return null;
            if (_Input.IsInteger &&
                _Input.TryParse(out int AsInteger))
            {
                return new BPrimitiveType(AsInteger);
            }

            string AsString = _Input.ToString();
            if (double.TryParse(AsString, out double AsDouble))
            {
                if (AsDouble % 1 == 0)
                {
                    return new BPrimitiveType((int)AsDouble);
                }
                return new BPrimitiveType(AsDouble);
            }
            return new BPrimitiveType(AsString);
        }

        private RedisValue ConvertPrimitiveTypeToRedisValue(BPrimitiveType _Input)
        {
            if (_Input != null)
            {
                if (_Input.Type == EBPrimitiveTypeEnum.Double)
                {
                    return _Input.AsDouble;
                }
                else if (_Input.Type == EBPrimitiveTypeEnum.Integer)
                {
                    return _Input.AsInteger;
                }
                else if (_Input.Type == EBPrimitiveTypeEnum.String)
                {
                    return _Input.AsString;
                }
                else if (_Input.Type == EBPrimitiveTypeEnum.ByteArray)
                {
                    return _Input.AsByteArray;
                }
            }
            return new RedisValue();
        }

        /// <summary>
        ///
        /// <para>SetKeyExpireTime:</para>
        ///
        /// <para>Sets given namespace's expire time</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.SetKeyExpireTime"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetKeyExpireTime(
             BMemoryQueryParameters _QueryParameters,
             TimeSpan _TTL,
             Action<string> _ErrorMessageAction = null)
        {
            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            try
            {
                var bDoesKeyExist = RedisConnection.GetDatabase().KeyExpire(Topic, _TTL);
                if (!bDoesKeyExist)
                {
                    RedisConnection.GetDatabase().SetAdd(Topic, "");
                    return RedisConnection.GetDatabase().KeyExpire(Topic, _TTL);
                }
                return true;
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return SetKeyExpireTime(_QueryParameters, _TTL, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->SetKeyExpireTime: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }
        }

        /// <summary>
        ///
        /// <para>GetKeyExpireTime:</para>
        ///
        /// <para>Gets given namespace's expire time</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.GetKeyExpireTime"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool GetKeyExpireTime(
            BMemoryQueryParameters _QueryParameters,
            out TimeSpan _TTL,
            Action<string> _ErrorMessageAction = null)
        {
            _TTL = TimeSpan.Zero;

            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            TimeSpan? TTL;
            try
            {
                TTL = RedisConnection.GetDatabase().KeyTimeToLive(Topic);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeyExpireTime(_QueryParameters, out _TTL, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->GetKeyExpireTime: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }

            if (TTL.HasValue)
            {
                _TTL = TTL.Value;
                return true;
            }
            return false;
        }

        /// <summary>
        ///
        /// <para>SetKeyValue:</para>
        ///
        /// <para>Sets given keys' values within given namespace and publishes message to [_QueryParameters.Domain]:[_QueryParameters.SubDomain] topic</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.SetKeyValue"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetKeyValue(
            BMemoryQueryParameters _QueryParameters,
            Tuple<string, BPrimitiveType>[] _KeyValues,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            if (_KeyValues.Length == 0) return false;

            HashEntry[] ArrayAsHashEntries = new HashEntry[_KeyValues.Length];

            JObject ChangesObject = new JObject();

            int i = 0;
            foreach (Tuple<string, BPrimitiveType> KeyValue in _KeyValues)
            {
                if (KeyValue.Item2 != null)
                {
                    if (KeyValue.Item2.Type == EBPrimitiveTypeEnum.Double)
                    {
                        ArrayAsHashEntries[i++] = new HashEntry(KeyValue.Item1, KeyValue.Item2.AsDouble.ToString());
                        ChangesObject[KeyValue.Item1] = KeyValue.Item2.AsDouble;
                    }
                    else if (KeyValue.Item2.Type == EBPrimitiveTypeEnum.Integer)
                    {
                        ArrayAsHashEntries[i++] = new HashEntry(KeyValue.Item1, KeyValue.Item2.AsInteger);
                        ChangesObject[KeyValue.Item1] = KeyValue.Item2.AsInteger;
                    }
                    else if (KeyValue.Item2.Type == EBPrimitiveTypeEnum.String)
                    {
                        ArrayAsHashEntries[i++] = new HashEntry(KeyValue.Item1, KeyValue.Item2.AsString);
                        ChangesObject[KeyValue.Item1] = KeyValue.Item2.AsString;
                    }
                    else if (KeyValue.Item2.Type == EBPrimitiveTypeEnum.ByteArray)
                    {
                        ArrayAsHashEntries[i++] = new HashEntry(KeyValue.Item1, KeyValue.Item2.AsByteArray);
                        ChangesObject[KeyValue.Item1] = KeyValue.Item2.ToString();
                    }
                }
            }

            if (i == 0) return false;
            if (i != _KeyValues.Length)
            {
                HashEntry[] ShrankArray = new HashEntry[i];
                for (int k = 0; k < i; k++)
                {
                    ShrankArray[k] = ArrayAsHashEntries[k];
                }
                ArrayAsHashEntries = ShrankArray;
            }

            JObject PublishObject = new JObject
            {
                ["operation"] = "SetKeyValue",
                ["changes"] = ChangesObject
            };

            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            FailoverCheck();
            try
            {
                RedisConnection.GetDatabase().HashSet(Topic, ArrayAsHashEntries);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return SetKeyValue(_QueryParameters, _KeyValues, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->SetKeyValue: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }
            if (PubSubService == null || !_bPublishChange) return true; //Means PubSubService is not needed and memory set has succeed.
            return PubSubService.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
        }

        /// <summary>
        ///
        /// <para>SetKeyValue:</para>
        ///
        /// <para>Sets given keys' values within given namespace and publishes message to [_Domain]:[_SubDomain] topic;</para>
        /// <para>With a condition; if key does not exist.</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.SetKeyValueConditionally"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool SetKeyValueConditionally(
            BMemoryQueryParameters _QueryParameters,
            Tuple<string, BPrimitiveType> _KeyValue,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            if (_KeyValue == null) return false;

            JObject ChangesObject = new JObject();

            HashEntry Entry = new HashEntry();
            if (_KeyValue.Item2 != null)
            {
                if (_KeyValue.Item2.Type == EBPrimitiveTypeEnum.Double)
                {
                    Entry = new HashEntry(_KeyValue.Item1, _KeyValue.Item2.AsDouble.ToString());
                    ChangesObject[_KeyValue.Item1] = _KeyValue.Item2.AsDouble;
                }
                else if (_KeyValue.Item2.Type == EBPrimitiveTypeEnum.Integer)
                {
                    Entry = new HashEntry(_KeyValue.Item1, _KeyValue.Item2.AsInteger);
                    ChangesObject[_KeyValue.Item1] = _KeyValue.Item2.AsInteger;
                }
                else if (_KeyValue.Item2.Type == EBPrimitiveTypeEnum.String)
                {
                    Entry = new HashEntry(_KeyValue.Item1, _KeyValue.Item2.AsString);
                    ChangesObject[_KeyValue.Item1] = _KeyValue.Item2.AsString;
                }
                else if (_KeyValue.Item2.Type == EBPrimitiveTypeEnum.ByteArray)
                {
                    Entry = new HashEntry(_KeyValue.Item1, _KeyValue.Item2.AsByteArray);
                    ChangesObject[_KeyValue.Item1] = _KeyValue.Item2.ToString();
                }
            }

            JObject PublishObject = new JObject
            {
                ["operation"] = "SetKeyValue", //Identical with SetKeyValue
                ["changes"] = ChangesObject
            };

            string CompiledQueryParameters = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            var RedisValues = new RedisValue[]
            {
                Entry.Name,
                Entry.Value
            };
            var Script = @"
                if redis.call('hexists', KEYS[1], ARGV[1]) == 0 then
                return redis.call('hset', KEYS[1], ARGV[1], ARGV[2])
                else
                return nil
                end";

            FailoverCheck();
            try
            {
                var Result = (RedisValue)RedisConnection.GetDatabase().ScriptEvaluate(Script,
                    new RedisKey[]
                    {
                        CompiledQueryParameters
                    },
                    RedisValues);
                if (Result.IsNull) return false;
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    var Result = (RedisValue)RedisConnection.GetDatabase().ScriptEvaluate(Script,
                        new RedisKey[]
                        {
                            CompiledQueryParameters
                        },
                        RedisValues);
                    if (Result.IsNull) return false;
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->SetKeyValue: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }

            if (PubSubService == null || !_bPublishChange) return true; //Means PubSubService is not needed and memory set has succeed.
            PubSubService.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
            return true;
        }

        /// <summary>
        ///
        /// <para>GetKeyValue:</para>
        ///
        /// <para>Gets given key's value within given namespace [_QueryParameters.Domain]:[_QueryParameters.SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.GetKeyValue"/> for detailed documentation</para>
        ///
        /// </summary>
        public BPrimitiveType GetKeyValue(
            BMemoryQueryParameters _QueryParameters,
            string _Key,
            Action<string> _ErrorMessageAction = null)
        {
            RedisValue ReturnedValue;

            FailoverCheck();
            try
            {
                ReturnedValue = RedisConnection.GetDatabase().HashGet(_QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier, _Key);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeyValue(_QueryParameters, _Key, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->GetKeyValue: " + e.Message + ", Trace: " + e.StackTrace);
                    return null;
                }
            }
            return ConvertRedisValueToPrimitiveType(ReturnedValue);
        }

        /// <summary>
        ///
        /// <para>GetKeysValues:</para>
        ///
        /// <para>Gets given keys' values' within given namespace [_Domain]:[_SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.GetKeysValues"/> for detailed documentation</para>
        ///
        /// </summary>
        public Dictionary<string, BPrimitiveType> GetKeysValues(
            BMemoryQueryParameters _QueryParameters,
            List<string> _Keys,
            Action<string> _ErrorMessageAction = null)
        {
            if (_Keys == null || _Keys.Count == 0) return null;

            string CompiledQueryParameters = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            Dictionary<string, BPrimitiveType> Results = new Dictionary<string, BPrimitiveType>();

            RedisValue[] KeysAsRedisValues = new RedisValue[_Keys.Count];

            string Script = "return redis.call('hmget',KEYS[1]";

            int i = 0;
            foreach (var _Key in _Keys)
            {
                Script += ",ARGV[" + (i + 1) + "]";
                KeysAsRedisValues[i] = _Keys[i];
                i++;
            }
            Script += ")";

            RedisValue[] ScriptEvaluationResult;

            FailoverCheck();
            try
            {
                ScriptEvaluationResult = (RedisValue[])RedisConnection.GetDatabase().ScriptEvaluate(Script,
                    new RedisKey[]
                    {
                        CompiledQueryParameters
                    },
                    KeysAsRedisValues);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeysValues(_QueryParameters, _Keys, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->GetKeysValues: " + e.Message + ", Trace: " + e.StackTrace);
                    return null;
                }
            }

            if (ScriptEvaluationResult != null && 
                ScriptEvaluationResult.Length == _Keys.Count)
            {
                int j = 0;
                foreach (var _Key in _Keys)
                {
                    Results[_Key] = ConvertRedisValueToPrimitiveType(ScriptEvaluationResult[j++]);
                }
            }
            else
            {
                _ErrorMessageAction?.Invoke("BMemoryServiceRedis->GetKeysValues: redis.call returned null or result length is not equal to keys length.");
                return null;
            }
            return Results;
        }

        /// <summary>
        ///
        /// <para>GetAllKeyValues:</para>
        ///
        /// <para>Gets all keys and keys' values of given namespace [_QueryParameters.Domain]:[_QueryParameters.SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.GetAllKeyValues"/> for detailed documentation</para>
        ///
        /// </summary>
        public Tuple<string, BPrimitiveType>[] GetAllKeyValues(
            BMemoryQueryParameters _QueryParameters,
            Action<string> _ErrorMessageAction = null)
        {
            HashEntry[] ReturnedKeyValues;

            FailoverCheck();
            try
            {
                ReturnedKeyValues = RedisConnection.GetDatabase().HashGetAll(_QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetAllKeyValues(_QueryParameters, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->GetAllKeyValues: " + e.Message + ", Trace: " + e.StackTrace);
                    return null;
                }
            }

            if (ReturnedKeyValues.Length == 0) return null;

            Tuple<string, BPrimitiveType>[] Result = new Tuple<string, BPrimitiveType>[ReturnedKeyValues.Length];

            int i = 0;
            foreach (HashEntry Entry in ReturnedKeyValues)
            {
                if (Entry != null)
                {
                    Result[i++] = new Tuple<string, BPrimitiveType>(Entry.Name.ToString(), ConvertRedisValueToPrimitiveType(Entry.Value));
                }
            }

            if (i == 0) return null;
            if (i != ReturnedKeyValues.Length)
            {
                Tuple<string, BPrimitiveType>[] ShrankArray = new Tuple<string, BPrimitiveType>[i];
                for (int k = 0; k < i; k++)
                {
                    ShrankArray[k] = Result[k];
                }
                Result = ShrankArray;
            }

            return Result;
        }

        /// <summary>
        ///
        /// <para>DeleteKey:</para>
        ///
        /// <para>Deletes given key within given namespace [_QueryParameters.Domain]:[_QueryParameters.SubDomain] and publishes message to [_QueryParameters.Domain]:[_QueryParameters.SubDomain] topic</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.DeleteKey"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DeleteKey(
            BMemoryQueryParameters _QueryParameters,
            string _Key,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            bool bResult;
            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            FailoverCheck();
            try
            {
                bResult = RedisConnection.GetDatabase().HashDelete(Topic, _Key);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return DeleteKey(_QueryParameters, _Key, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->DeleteKey->HashDelete: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }

            if (_bPublishChange && bResult)
            {
                JObject PublishObject = new JObject
                {
                    ["operation"] = "DeleteKey",
                    ["changes"] = _Key
                };

                PubSubService?.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
            }

            return bResult;
        }

        /// <summary>
        ///
        /// <para>DeleteAllKeys:</para>
        ///
        /// <para>Deletes all keys for given namespace and publishes message to [_QueryParameters.Domain]:[_QueryParameters.SubDomain] topic</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.DeleteAllKeys"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool DeleteAllKeys(
            BMemoryQueryParameters _QueryParameters,
            bool _bWaitUntilCompletion = false,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            bool bResult;
            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            FailoverCheck();
            try
            {
                bResult = RedisConnection.GetDatabase().KeyDelete(Topic, (_bWaitUntilCompletion ? CommandFlags.None : CommandFlags.FireAndForget));
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return DeleteAllKeys(_QueryParameters, _bWaitUntilCompletion, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->DeleteAllKeys: " + e.Message + ", Trace: " + e.StackTrace);
                    return false;
                }
            }

            if (bResult && _bPublishChange)
            {
                JObject PublishObject = new JObject
                {
                    ["operation"] = "DeleteAllKeys"
                };

                PubSubService?.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
            }

            return bResult;
        }

        /// <summary>
        ///
        /// <para>GetKeys:</para>
        ///
        /// <para>Gets all keys of given workspace [_QueryParameters.Domain]:[_QueryParameters.SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.GetKeys"/> for detailed documentation</para>
        ///
        /// </summary>
        public string[] GetKeys(
            BMemoryQueryParameters _QueryParameters,
            Action<string> _ErrorMessageAction = null)
        {
            RedisValue[] Results;

            FailoverCheck();
            try
            {
                Results = RedisConnection.GetDatabase().HashKeys(_QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeys(_QueryParameters, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->GetKeys: " + e.Message + ", Trace: " + e.StackTrace);
                    return null;
                }
            }

            if (Results.Length == 0) return null;

            string[] ResultsAsStrings = new string[Results.Length];

            int i = 0;
            foreach (RedisValue Current in Results)
            {
                if (!Current.IsNullOrEmpty)
                {
                    ResultsAsStrings[i++] = Current.ToString();
                }
            }

            if (i == 0) return null;
            if (i != Results.Length)
            {
                string[] ShrankArray = new string[i];
                for (int k = 0; k < i; k++)
                {
                    ShrankArray[k] = ResultsAsStrings[k];
                }
                ResultsAsStrings = ShrankArray;
            }

            return ResultsAsStrings;
        }

        /// <summary>
        ///
        /// <para>GetKeysCount:</para>
        ///
        /// <para>Returns number of keys of given workspace [_QueryParameters.Domain]:[_QueryParameters.SubDomain]</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.GetKeysCount"/> for detailed documentation</para>
        ///
        /// </summary>
        public long GetKeysCount(
            BMemoryQueryParameters _QueryParameters,
            Action<string> _ErrorMessageAction = null)
        {
            long Count;

            FailoverCheck();
            try
            {
                Count = RedisConnection.GetDatabase().HashLength(_QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetKeysCount(_QueryParameters, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->GetKeysCount: " + e.Message + ", Trace: " + e.StackTrace);
                    return 0;
                }
            }
            return Count;
        }

        /// <summary>
        ///
        /// <para>IncrementKeyValues:</para>
        ///
        /// <para>Increments given keys' by given values within given namespace and publishes message to [_Domain]:[_SubDomain] topic</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.IncrementKeyValues"/> for detailed documentation</para>
        ///
        /// </summary>
        public void IncrementKeyValues(
            BMemoryQueryParameters _QueryParameters,
            Tuple<string, long>[] _KeysAndIncrementByValues,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            if (_KeysAndIncrementByValues == null || _KeysAndIncrementByValues.Length == 0) return;

            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            RedisValue[] ArgumentsAsRedisValues = new RedisValue[_KeysAndIncrementByValues.Length * 2];

            string Script = "";
            string ScriptReturn = "return ";

            int i = 0;
            foreach (var _KeyIncrBy in _KeysAndIncrementByValues)
            {
                Script += "local r" + (i + 1) + "=redis.call('hincrby',KEYS[1],ARGV[" + (i + 1) + "],ARGV[" + (i + 2) + "])" + Environment.NewLine;
                ScriptReturn += (i > 0 ? ("..\" \".. r") : "r") + (i + 1);
                ArgumentsAsRedisValues[i] = _KeyIncrBy.Item1;
                ArgumentsAsRedisValues[i + 1] = _KeyIncrBy.Item2;
                i += 2;
            }
            Script += ScriptReturn;

            string ScriptEvaluationResult;

            FailoverCheck();
            try
            {
                ScriptEvaluationResult = (string)RedisConnection.GetDatabase().ScriptEvaluate(Script,
                    new RedisKey[]
                    {
                    Topic
                    },
                    ArgumentsAsRedisValues);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    IncrementKeyValues(_QueryParameters, _KeysAndIncrementByValues, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->IncrementKeyValues: " + e.Message + ", Trace: " + e.StackTrace);
                }
                return;
            }

            if (_bPublishChange)
            {
                string[] ScriptEvaluationResults = ScriptEvaluationResult.Split(' ');

                JObject ChangesObject = new JObject();

                if (ScriptEvaluationResults != null &&
                    ScriptEvaluationResults.Length == _KeysAndIncrementByValues.Length)
                {
                    int j = 0;
                    foreach (Tuple<string, long> Entry in _KeysAndIncrementByValues)
                    {
                        if (Entry != null)
                        {
                            if (int.TryParse(ScriptEvaluationResults[j], out int NewValue))
                            {
                                ChangesObject[Entry.Item1] = NewValue;
                            }
                        }
                        j++;
                    }
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->IncrementKeyValues: redis.call returned null or result length is not equal to keys length.");
                    return;
                }

                if (ChangesObject.Count == 0) return;

                JObject PublishObject = new JObject
                {
                    ["operation"] = "SetKeyValue", //We publish the results, therefore for listeners, this action is identical with SetKeyValue
                    ["changes"] = ChangesObject
                };

                PubSubService?.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
            }
        }

        /// <summary>
        ///
        /// <para>IncrementKeyByValueAndGet:</para>
        ///
        /// <para>Increments given key by given value within given namespace, publishes message to [_Domain]:[_SubDomain] topic and returns new value</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.IncrementKeyByValueAndGet"/> for detailed documentation</para>
        ///
        /// </summary>
        public long IncrementKeyByValueAndGet(
            BMemoryQueryParameters _QueryParameters,
            Tuple<string, long> _KeyValue,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            if (_KeyValue == null) return 0;

            long Result = 0;
            
            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            FailoverCheck();
            try
            {
                Result = RedisConnection.GetDatabase().HashIncrement(Topic, _KeyValue.Item1, _KeyValue.Item2);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return IncrementKeyByValueAndGet(_QueryParameters, _KeyValue, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->IncrementKeyByValueAndGet: " + e.Message + ", Trace: " + e.StackTrace);
                    return 0;
                }
            }

            if (_bPublishChange)
            {
                JObject ChangesObject = new JObject
                {
                    [_KeyValue.Item1] = Result
                };

                JObject PublishObject = new JObject
                {
                    ["operation"] = "SetKeyValue", //We publish the results, therefore for listeners, this action is identical with SetKeyValue
                    ["changes"] = ChangesObject
                };

                PubSubService?.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
            }
            return Result;
        }

        private bool PushToList(
            bool _bToTail,
            BMemoryQueryParameters _QueryParameters,
            string _ListName,
            BPrimitiveType[] _Values,
            bool _bPushIfListExists = false,
            Action<string> _ErrorMessageAction = null,
            bool _bAsync = false,
            bool _bPublishChange = true)
        {
            if (_Values.Length == 0) return false;

            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            var Transaction = RedisConnection.GetDatabase().CreateTransaction();

            if (_bPushIfListExists)
            {
                Transaction.AddCondition(Condition.KeyExists(Topic + ":" + _ListName));
            }

            RedisValue[] AsRedisValues = new RedisValue[_Values.Length];
            JArray ChangesArray = new JArray();

            int i = 0;
            foreach (BPrimitiveType _Value in _Values)
            {
                if (_Value.Type == EBPrimitiveTypeEnum.Double)
                {
                    AsRedisValues[i++] = _Value.AsDouble;
                    ChangesArray.Add(_Value.AsDouble);
                }
                else if (_Value.Type == EBPrimitiveTypeEnum.Integer)
                {
                    AsRedisValues[i++] = _Value.AsInteger;
                    ChangesArray.Add(_Value.AsInteger);
                }
                else if (_Value.Type == EBPrimitiveTypeEnum.String)
                {
                    AsRedisValues[i++] = _Value.AsString;
                    ChangesArray.Add(_Value.AsString);
                }
                else if (_Value.Type == EBPrimitiveTypeEnum.ByteArray)
                {
                    AsRedisValues[i++] = _Value.AsByteArray;
                    ChangesArray.Add(_Value.ToString());
                }
            }

            var ChangesObject = new JObject
            {
                ["List"] = _ListName,
                ["Pushed"] = ChangesArray
            };

            var PublishObject = new JObject
            {
                ["operation"] = _bToTail ? "PushToListTail" : "PushToListHead",
                ["changes"] = ChangesObject
            };

            Task CreatedTask = null;
            if (_bToTail)
            {
                CreatedTask = Transaction.ListRightPushAsync(Topic + ":" + _ListName, AsRedisValues);
            }
            else
            {
                CreatedTask = Transaction.ListLeftPushAsync(Topic + ":" + _ListName, AsRedisValues);
            }

            if (_bAsync)
            {
                FailoverCheck();
                try
                {
                    Transaction.Execute();
                    try
                    {
                        CreatedTask?.Dispose();
                    }
                    catch (Exception) { }
                }
                catch (Exception e)
                {
                    if (e is RedisException || e is TimeoutException)
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        return PushToList(_bToTail, _QueryParameters, _ListName, _Values, _bPushIfListExists, _ErrorMessageAction, _bAsync, _bPublishChange);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("BMemoryServiceRedis->PushToList: " + e.Message + ", Trace: " + e.StackTrace);
                    }
                }
                if (_bPublishChange)
                {
                    PubSubService?.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
                }
                return true;
            }
            else
            {
                bool Committed = false;

                FailoverCheck();
                try
                {
                    Committed = Transaction.Execute();
                    try
                    {
                        CreatedTask?.Dispose();
                    }
                    catch (Exception) {}
                }
                catch (Exception e)
                {
                    if (e is RedisException || e is TimeoutException)
                    {
                        OnFailoverDetected(_ErrorMessageAction);
                        return PushToList(_bToTail, _QueryParameters, _ListName, _Values, _bPushIfListExists, _ErrorMessageAction, _bAsync, _bPublishChange);
                    }
                    else
                    {
                        _ErrorMessageAction?.Invoke("BMemoryServiceRedis->PushToList: " + e.Message + ", Trace: " + e.StackTrace);
                    }
                }

                if (Committed)
                {
                    if (_bPublishChange)
                    {
                        PubSubService?.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
                    }
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        ///
        /// <para>PushToListTail:</para>
        ///
        /// <para>Pushes the value(s) to the tail of given list, returns if push succeeds (If _bAsync is true, after execution order point, always returns true).</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.PushToListTail"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool PushToListTail(
            BMemoryQueryParameters _QueryParameters,
            string _ListName,
            BPrimitiveType[] _Values,
            bool _bPushIfListExists = false, 
            Action<string> _ErrorMessageAction = null,
            bool _bAsync = false,
            bool _bPublishChange = true)
        {
            return PushToList(
                true,
                _QueryParameters,
                _ListName,
                _Values,
                _bPushIfListExists,
                _ErrorMessageAction,
                _bAsync,
                _bPublishChange);
        }

        /// <summary>
        ///
        /// <para>PushToListHead:</para>
        ///
        /// <para>Pushes the value(s) to the head of given list, returns if push succeeds (If _bAsync is true, after execution order point, always returns true).</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.PushToListHead"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool PushToListHead(
            BMemoryQueryParameters _QueryParameters,
            string _ListName,
            BPrimitiveType[] _Values,
            bool _bPushIfListExists = false, 
            Action<string> _ErrorMessageAction = null,
            bool _bAsync = false,
            bool _bPublishChange = true)
        {
            return PushToList(
                false,
                _QueryParameters,
                _ListName,
                _Values,
                _bPushIfListExists,
                _ErrorMessageAction,
                _bAsync,
                _bPublishChange);
        }

        private BPrimitiveType PopFromList(
            bool _bFromTail,
            BMemoryQueryParameters _QueryParameters,
            string _ListName,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            BPrimitiveType PoppedAsPrimitive = null;

            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            RedisValue PoppedValue;

            FailoverCheck();
            try
            {
                if (_bFromTail)
                {
                    PoppedValue = RedisConnection.GetDatabase().ListRightPop(Topic + ":" + _ListName);
                }
                else
                {
                    PoppedValue = RedisConnection.GetDatabase().ListLeftPop(Topic + ":" + _ListName);
                }

                if (PoppedValue.IsNullOrEmpty)
                {
                    return null;
                }
                PoppedAsPrimitive = ConvertRedisValueToPrimitiveType(PoppedValue);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return PopFromList(_bFromTail, _QueryParameters, _ListName, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->PopFromList: " + e.Message + ", Trace: " + e.StackTrace);
                    return null;
                }
            }

            if (_bPublishChange)
            {
                JObject ChangesObject = new JObject
                {
                    ["List"] = _ListName
                };

                if (PoppedAsPrimitive.Type == EBPrimitiveTypeEnum.Double)
                {
                    ChangesObject["Popped"] = PoppedAsPrimitive.AsDouble;
                }
                else if (PoppedAsPrimitive.Type == EBPrimitiveTypeEnum.Integer)
                {
                    ChangesObject["Popped"] = PoppedAsPrimitive.AsInteger;
                }
                else if (PoppedAsPrimitive.Type == EBPrimitiveTypeEnum.String)
                {
                    ChangesObject["Popped"] = PoppedAsPrimitive.AsString;
                }
                else if (PoppedAsPrimitive.Type == EBPrimitiveTypeEnum.ByteArray)
                {
                    ChangesObject["Popped"] = PoppedAsPrimitive.ToString();
                }
                else return null;

                JObject PublishObject = new JObject
                {
                    ["operation"] = _bFromTail ? "PopLastElementOfList" : "PopFirstElementOfList",
                    ["changes"] = ChangesObject
                };

                PubSubService?.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
            }
            
            return PoppedAsPrimitive;
        }

        /// <summary>
        ///
        /// <para>PopLastElementOfList:</para>
        ///
        /// <para>Pops the value from the tail of given list</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.PopLastElementOfList"/> for detailed documentation</para>
        ///
        /// </summary>
        public BPrimitiveType PopLastElementOfList(
            BMemoryQueryParameters _QueryParameters,
            string _ListName, 
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            return PopFromList(
                true,
                _QueryParameters,
                _ListName,
                _ErrorMessageAction,
                _bPublishChange);
        }

        /// <summary>
        ///
        /// <para>PopFirstElementOfList:</para>
        ///
        /// <para>Pops the value from the head of given list</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.PopFirstElementOfList"/> for detailed documentation</para>
        ///
        /// </summary>
        public BPrimitiveType PopFirstElementOfList(
            BMemoryQueryParameters _QueryParameters,
            string _ListName, 
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            return PopFromList(
                false,
                _QueryParameters,
                _ListName,
                _ErrorMessageAction,
                _bPublishChange);
        }

        /// <summary>
        ///
        /// <para>GetAllElementsOfList:</para>
        ///
        /// <para>Gets all values from the given list</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.GetAllElementsOfList"/> for detailed documentation</para>
        ///
        /// </summary>
        public BPrimitiveType[] GetAllElementsOfList(
            BMemoryQueryParameters _QueryParameters,
            string _ListName, 
            Action<string> _ErrorMessageAction = null)
        {
            RedisValue[] ReturnedValues = null;

            FailoverCheck();
            try
            {
                ReturnedValues = RedisConnection.GetDatabase().ListRange(_QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier + ":" + _ListName);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return GetAllElementsOfList(_QueryParameters, _ListName, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->GetAllElementsOfList: " + e.Message + ", Trace: " + e.StackTrace);
                }
            }

            if (ReturnedValues == null || ReturnedValues.Length == 0) return null;

            BPrimitiveType[] Result = new BPrimitiveType[ReturnedValues.Length];
            int i = 0;
            foreach (RedisValue Value in ReturnedValues)
            {
                Result[i++] = ConvertRedisValueToPrimitiveType(Value);
            }

            return Result;
        }

        /// <summary>
        ///
        /// <para>EmptyList:</para>
        ///
        /// <para>Empties the list</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.EmptyList"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool EmptyList(
            BMemoryQueryParameters _QueryParameters,
            string _ListName,
            bool _bWaitUntilCompletion = false,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            bool bResult = false;

            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            JObject PublishObject = new JObject
            {
                ["operation"] = "EmptyList"
            };

            FailoverCheck();
            try
            {
                bResult = RedisConnection.GetDatabase().KeyDelete(Topic + ":" + _ListName, (_bWaitUntilCompletion ? CommandFlags.None : CommandFlags.FireAndForget));
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return EmptyList(_QueryParameters, _ListName, _bWaitUntilCompletion, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->EmptyList: " + e.Message + ", Trace: " + e.StackTrace);
                }
            }

            if (_bPublishChange)
            {
                PubSubService?.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
            }

            return bResult;
        }

        /// <summary>
        ///
        /// <para>EmptyListAndSublists:</para>
        ///
        /// <para>Fetches all elements in _ListName, iterates and empties all sublists (_SublistPrefix + Returned SublistName)</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.EmptyListAndSublists"/> for detailed documentation</para>
        ///
        /// </summary>
        public void EmptyListAndSublists(
            BMemoryQueryParameters _QueryParameters,
            string _ListName,
            string _SublistPrefix,
            bool _bWaitUntilCompletion = false,
            Action<string> _ErrorMessageAction = null,
            bool _bPublishChange = true)
        {
            string Topic = _QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier;

            JObject PublishObject = new JObject
            {
                ["operation"] = "EmptyListAndSublists"
            };

            string Script = @"
                local results=redis.call('lrange',KEYS[1],0,-1)
                for _,key in ipairs(results) do 
                    redis.call('del',ARGV[1] .. key)
                end
                redis.call('del',KEYS[1])";

            FailoverCheck();
            try
            {
                RedisConnection.GetDatabase().ScriptEvaluate(Script,
                    new RedisKey[]
                    {
                        Topic + ":" + _ListName
                    },
                    new RedisValue[]
                    {
                        Topic + ":" + _SublistPrefix
                    },
                    (_bWaitUntilCompletion ? CommandFlags.None : CommandFlags.FireAndForget));
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    EmptyListAndSublists(_QueryParameters, _ListName, _SublistPrefix, _bWaitUntilCompletion, _ErrorMessageAction, _bPublishChange);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->EmptyListAndSublists: " + e.Message + ", Trace: " + e.StackTrace);
                }
                return;
            }

            if (_bPublishChange)
            {
                PubSubService?.Publish(_QueryParameters, PublishObject, _ErrorMessageAction);
            }
        }

        /// <summary>
        ///
        /// <para>ListSize:</para>
        ///
        /// <para>Returns number of elements of the given list</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.ListSize"/> for detailed documentation</para>
        ///
        /// </summary>
        public long ListSize(
            BMemoryQueryParameters _QueryParameters,
            string _ListName,
            Action<string> _ErrorMessageAction = null)
        {
            long Result = 0;

            FailoverCheck();
            try
            {
                Result = RedisConnection.GetDatabase().ListLength(_QueryParameters.Domain + ":" + _QueryParameters.SubDomain + ":" + _QueryParameters.Identifier + ":" + _ListName);
            }
            catch (Exception e)
            {
                if (e is RedisException || e is TimeoutException)
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    return ListSize(_QueryParameters, _ListName, _ErrorMessageAction);
                }
                else
                {
                    _ErrorMessageAction?.Invoke("BMemoryServiceRedis->ListSize: " + e.Message + ", Trace: " + e.StackTrace);
                }
            }
            return Result;
        }

        /// <summary>
        ///
        /// <para>ListContains:</para>
        ///
        /// <para>Returns if given list contains given value or not</para>
        ///
        /// <para>Check <seealso cref="IBMemoryServiceInterface.ListContains"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool ListContains(
            BMemoryQueryParameters _QueryParameters,
            string _ListName, 
            BPrimitiveType _Value, 
            Action<string> _ErrorMessageAction = null)
        {
            BPrimitiveType[] Elements = GetAllElementsOfList(
                _QueryParameters,
                _ListName,
                _ErrorMessageAction);

            if (Elements == null || Elements.Length == 0) return false;
            foreach (BPrimitiveType Primitive in Elements)
            {
                if (Primitive.Equals(_Value))
                {
                    return true;
                }
            }
            return false;
        }

        public IBPubSubServiceInterface GetPubSubService()
        {
            return PubSubService;
        }
    }
}