/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Threading;
using StackExchange.Redis;

namespace BCloudServiceUtilities
{
    public class BRedisCommonFunctionalities
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        protected readonly bool bInitializationSucceed;

        /// <summary>
        /// Holds redis connection instance
        /// </summary>
        protected readonly ConnectionMultiplexer RedisConnection;

        private readonly ConfigurationOptions RedisConfig;

        private readonly string ChildDescriptor;

        protected readonly bool bFailoverMechanismEnabled = true;

        protected BRedisCommonFunctionalities(
            string _ChildDescriptor,
            string _RedisEndpoint,
            int _RedisPort,
            string _RedisPassword,
            bool _RedisSslEnabled = false,
            bool _bFailoverMechanismEnabled = true,
            Action<string> _ErrorMessageAction = null)
        {
            ChildDescriptor = _ChildDescriptor;

            try
            {
                Failover_Mutex = new Mutex(false, "REDIS_FAILOVER_MUTEX");
            }
            catch (Exception e)
            {
                bInitializationSucceed = false;
                _ErrorMessageAction?.Invoke(ChildDescriptor + "->Base->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                return;
            }

            bFailoverMechanismEnabled = _bFailoverMechanismEnabled;

            RedisConfig = new ConfigurationOptions
            {
                EndPoints =
                {
                    { _RedisEndpoint, _RedisPort }
                },
                SyncTimeout = 20000,
                AbortOnConnectFail = false
            };

            if (_RedisPassword != null && _RedisPassword.Length > 0 && _RedisPassword != "N/A")
            {
                RedisConfig.Password = _RedisPassword;
            }

            if (_RedisSslEnabled)
            {
                RedisConfig.Ssl = _RedisSslEnabled;
            }

            try
            {
                RedisConnection = ConnectionMultiplexer.Connect(RedisConfig);
            }
            catch (Exception e)
            {
                if (bFailoverMechanismEnabled && (e is RedisException || e is TimeoutException))
                {
                    OnFailoverDetected(_ErrorMessageAction);
                    RedisConnection = ConnectionMultiplexer.Connect(RedisConfig);
                }
                else
                {
                    bInitializationSucceed = false;
                    _ErrorMessageAction?.Invoke(ChildDescriptor + "->Base->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                }
            }

            if (RedisConnection == null)
            {
                bInitializationSucceed = false;
            }
            else
            {
                bInitializationSucceed = true;
            }
        }

        ~BRedisCommonFunctionalities()
        {
            RedisConnection?.Dispose();
        }

        private int FailoverID = 0;
        private bool bFailoverState = false;
        private Action<string> Failover_MessageChannel = null;

        private readonly Mutex Failover_Mutex = null;
        private void Lock_Failover_Mutex()
        {
            try
            {
                if (Failover_Mutex != null)
                {
                    Failover_Mutex.WaitOne();
                }
            }
            catch (Exception) { }
        }
        private void Unlock_Failover_Mutex()
        {
            try
            {
                if (Failover_Mutex != null)
                {
                    Failover_Mutex.ReleaseMutex();
                }
            }
            catch (Exception) { }
        }
        protected void OnFailoverDetected(Action<string> _ErrorMessageAction)
        {
            int LocalFailoverID = FailoverID;
            Lock_Failover_Mutex();
            {
                if (LocalFailoverID == FailoverID)
                {
                    FailoverID++;

                    bFailoverState = true;
                    //There must not be any print statement or function call to redis
                    CheckForFixInFailover();
                    bFailoverState = false;

                    Failover_MessageChannel = _ErrorMessageAction;
                    Failover_MessageChannel?.Invoke(ChildDescriptor + "->Base->Failover: Recovered from fail state.");
                }
            }
            Unlock_Failover_Mutex();
        }

        private void CheckForFixInFailover()
        {
            //There must not be any print statement or function call to redis

            bool bLocalTestSuccess = false;
            while (!bLocalTestSuccess)
            {
                Thread.Sleep(5000);

                for (int i = 0; i < 4; i++)
                {
                    Thread.Sleep(250);

                    try
                    {
                        RedisConnection.GetDatabase().HashSet("system:system:0", new HashEntry[]
                        {
                            new HashEntry("recoverytest", "recoverytestvalue")
                        });
                    }
                    catch (Exception)
                    {
                        bLocalTestSuccess = false;
                        break;
                    }

                    bLocalTestSuccess = true;
                }
            }
        }

        /// <summary>
        /// Blocks the caller thread if failover mechanism is running
        /// </summary>
        public void FailoverCheck()
        {
            while (bFailoverState)
            {
                Thread.Sleep(1000);
            }
        }
    }
}