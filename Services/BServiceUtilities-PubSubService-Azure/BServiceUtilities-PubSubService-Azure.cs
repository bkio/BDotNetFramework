using BCloudServiceUtilities;
using BCloudServiceUtilities.PubSubServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID, AZURE_NAMESPACE_ID, and AZURE_NAMESPACE_CONNSTR must be provided and valid.</para>
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
                RequiredEnvironmentVariables["AZURE_CLIENT_ID"], 
                RequiredEnvironmentVariables["AZURE_CLIENT_SECRET"], 
                RequiredEnvironmentVariables["AZURE_TENANT_ID"], 
                RequiredEnvironmentVariables["AZURE_NAMESPACE_ID"], 
                RequiredEnvironmentVariables["AZURE_NAMESPACE_CONNSTR"]
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