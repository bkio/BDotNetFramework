/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.MemoryServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>REDIS_ENDPOINT, REDIS_PORT, REDIS_PASSWORD must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        /// <summary>
        /// <para>Initialized Memory Service</para>
        /// </summary>
        public IBMemoryServiceInterface MemoryService = null;

        public bool WithMemoryService(bool _bFailoverMechanismEnabled = true, IBPubSubServiceInterface _WithPubSubService = null)
        {
            /*
            * Memory service initialization
            */
            if (!RequiredEnvironmentVariables.ContainsKey("REDIS_ENDPOINT") ||
                !RequiredEnvironmentVariables.ContainsKey("REDIS_PORT") ||
                !int.TryParse(RequiredEnvironmentVariables["REDIS_PORT"], out int RedisPort) ||
                !RequiredEnvironmentVariables.ContainsKey("REDIS_PASSWORD"))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "REDIS_ENDPOINT, REDIS_PORT, REDIS_PASSWORD parameters must be provided and valid."), ProgramID, "Initialization");
                return false;
            }

            MemoryService = new BMemoryServiceRedis(
                RequiredEnvironmentVariables["REDIS_ENDPOINT"],
                RedisPort,
                RequiredEnvironmentVariables["REDIS_PASSWORD"],
                _WithPubSubService,
                _bFailoverMechanismEnabled,
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });

            if (MemoryService == null || !MemoryService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Memory service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}