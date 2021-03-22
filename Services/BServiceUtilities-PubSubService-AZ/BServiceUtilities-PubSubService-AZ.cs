using BCloudServiceUtilities;
using BCloudServiceUtilities.PubSubServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AZ_CLIENT_ID, AZ_CLIENT_SECRET, AZ_TENANT_ID, AZ_SERVICEBUS_NAMESPACE_ID, and AZ_SERVICEBUS_NAMESPACE_CONNECTION_STRING must be provided and valid.</para>
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
            PubSubService = new BPubSubServiceAzure(
                RequiredEnvironmentVariables["AZ_CLIENT_ID"], 
                RequiredEnvironmentVariables["AZ_CLIENT_SECRET"], 
                RequiredEnvironmentVariables["AZ_TENANT_ID"], 
                RequiredEnvironmentVariables["AZ_SERVICEBUS_NAMESPACE_ID"], 
                RequiredEnvironmentVariables["AZ_SERVICEBUS_NAMESPACE_CONNECTION_STRING"],
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