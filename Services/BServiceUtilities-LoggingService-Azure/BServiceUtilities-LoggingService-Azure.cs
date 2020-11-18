/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.LoggingServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>APPINSIGHTS_INSTRUMENTATIONKEY must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        public bool WithLoggingService()
        {
            /*
            * Logging service initialization
            */
            LoggingService = new BLoggingServiceAzure(RequiredEnvironmentVariables["APPINSIGHTS_INSTRUMENTATIONKEY"],
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });

            if (LoggingService == null || !LoggingService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Logging service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}
