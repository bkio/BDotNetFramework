/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.PubSubServices;

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
        /// <para>Initialized Pub/Sub Service</para>
        /// </summary>
        public IBPubSubServiceInterface PubSubService = null;

        public bool WithPubSubService()
        {
            /*
            * Pub/Sub service initialization
            */
            if (!RequiredEnvironmentVariables.ContainsKey("REDIS_ENDPOINT") ||
                !RequiredEnvironmentVariables.ContainsKey("REDIS_PORT") ||
                !int.TryParse(RequiredEnvironmentVariables["REDIS_PORT"], out int RedisPort) ||
                !RequiredEnvironmentVariables.ContainsKey("REDIS_PASSWORD"))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "REDIS_ENDPOINT, REDIS_PORT, REDIS_PASSWORD parameters must be provided and valid."), ProgramID, "Initialization");
                return false;
            }

            PubSubService = new BPubSubServiceRedis(
                RequiredEnvironmentVariables["REDIS_ENDPOINT"],
                RedisPort,
                RequiredEnvironmentVariables["REDIS_PASSWORD"],
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });

            if (PubSubService == null || !PubSubService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Pub/Sub service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}