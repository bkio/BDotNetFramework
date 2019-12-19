/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.TracingServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>GOOGLE_CLOUD_PROJECT_ID, GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_CREDENTIALS) must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        /// <summary>
        /// <para>Initialized Tracing Service</para>
        /// </summary>
        public IBTracingServiceInterface TracingService = null;

        public bool WithTracingService()
        {
            /*
            * Tracing service initialization
            */
            TracingService = new BTracingServiceGC(RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"], ProgramID,
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });

            if (TracingService == null || !TracingService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Tracing service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}