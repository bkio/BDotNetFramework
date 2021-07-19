/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.PubSubServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>(REDIS_PUBSUB_ENDPOINT, REDIS_PUBSUB_PORT, REDIS_PUBSUB_PASSWORD) or (REDIS_ENDPOINT, REDIS_PORT, REDIS_PASSWORD) parameters must be provided and valid.</para>
    /// <para>REDIS_PUBSUB_SSL_ENABLED or REDIS_SSL_ENABLED can be sent to set SSL enabled, otherwise it will be false.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        /// <summary>
        /// <para>Initialized Pub/Sub Service</para>
        /// </summary>
        public IBPubSubServiceInterface PubSubService = null;

        public bool WithPubSubService(bool _bFailoverMechanismEnabled = true)
        {
            /*
            * Pub/Sub service initialization
            */
            if (!RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_ENDPOINT") ||
                !RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_PORT") ||
                !int.TryParse(RequiredEnvironmentVariables["REDIS_PUBSUB_PORT"], out int RedisPort) ||
                !RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_PASSWORD"))
            {
                if (!RequiredEnvironmentVariables.ContainsKey("REDIS_ENDPOINT") ||
                !RequiredEnvironmentVariables.ContainsKey("REDIS_PORT") ||
                !int.TryParse(RequiredEnvironmentVariables["REDIS_PORT"], out RedisPort) ||
                !RequiredEnvironmentVariables.ContainsKey("REDIS_PASSWORD"))
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "(REDIS_PUBSUB_ENDPOINT, REDIS_PUBSUB_PORT, REDIS_PUBSUB_PASSWORD) or (REDIS_ENDPOINT, REDIS_PORT, REDIS_PASSWORD) parameters must be provided and valid."), ProgramID, "Initialization");
                    return false;
                }
            }

            bool RedisSslEnabled = false;
            if (RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_SSL_ENABLED") && !bool.TryParse(RequiredEnvironmentVariables["REDIS_PUBSUB_SSL_ENABLED"], out RedisSslEnabled))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Warning, "REDIS_PUBSUB_SSL_ENABLED parameter has been provided, but it has not a valid value. It will be continued without SSL."), ProgramID, "Initialization");
            }
            if (!RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_SSL_ENABLED") && RequiredEnvironmentVariables.ContainsKey("REDIS_SSL_ENABLED") && !bool.TryParse(RequiredEnvironmentVariables["REDIS_SSL_ENABLED"], out RedisSslEnabled))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Warning, "REDIS_SSL_ENABLED parameter has been provided, but it has not a valid value. It will be continued without SSL."), ProgramID, "Initialization");
            }

            string RedisEndpoint = RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_ENDPOINT") ? RequiredEnvironmentVariables["REDIS_PUBSUB_ENDPOINT"] : RequiredEnvironmentVariables["REDIS_ENDPOINT"];
            string RedisPassword = RequiredEnvironmentVariables.ContainsKey("REDIS_PUBSUB_PASSWORD") ? RequiredEnvironmentVariables["REDIS_PUBSUB_PASSWORD"] : RequiredEnvironmentVariables["REDIS_PASSWORD"];

            PubSubService = new BPubSubServiceRedis(
                RedisEndpoint,
                RedisPort,
                RedisPassword,
                RedisSslEnabled,
                _bFailoverMechanismEnabled,
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