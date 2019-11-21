/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.PubSubServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Additional Required Environment variables:</para>
    /// 
    /// <para>PUB_SUB_PROVIDER</para>
    /// <para>PUB_SUB_PROVIDER can be AWS, GC, REDIS</para>
    /// 
    /// <para>If PUB_SUB_PROVIDER is REDIS;</para>
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
            if (!RequiredEnvironmentVariables.ContainsKey("PUB_SUB_PROVIDER"))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "PUB_SUB_PROVIDER environment variable is missing."), ProgramID, "Initialization");
                return false;
            }

            var PubSubProvider = RequiredEnvironmentVariables["PUB_SUB_PROVIDER"];

            /*
            * Pub/Sub service initialization
            */
            if (PubSubProvider == "AWS")
                PubSubService = new BPubSubServiceAWS(CloudProviderEnvVars["AWS_ACCESS_KEY"], CloudProviderEnvVars["AWS_SECRET_KEY"], CloudProviderEnvVars["AWS_REGION"],
                    (string Message) =>
                    {
                        LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                    });
            else if (PubSubProvider == "GC")
                PubSubService = new BPubSubServiceGC(RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"],
                    (string Message) =>
                    {
                        LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                    });
            else if (PubSubProvider == "REDIS")
            {
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
            }

            if (PubSubService == null || !PubSubService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Pub/Sub service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}