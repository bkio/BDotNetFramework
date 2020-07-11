/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.VMService;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>GOOGLE_CLOUD_PROJECT_ID, GOOGLE_CLOUD_COMPUTE_ZONE, GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS) must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        /// <summary>
        /// <para>Initialized File Service</para>
        /// </summary>
        public IBVMServiceInterface VMService = null;

        public bool WithVMService()
        {
            /*
            * VM service initialization
            */
            VMService = new BVMServiceGC(ProgramID, RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"], RequiredEnvironmentVariables["GOOGLE_CLOUD_COMPUTE_ZONE"],
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });

            if (VMService == null || !VMService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "VM service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}