/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.FileServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        /// <summary>
        /// <para>Initialized File Service</para>
        /// </summary>
        public IBFileServiceInterface FileService = null;

        public bool WithFileService()
        {
            /*
            * File service initialization
            */
            FileService = new BFileServiceAWS(CloudProviderEnvVars["AWS_ACCESS_KEY"], CloudProviderEnvVars["AWS_SECRET_KEY"], CloudProviderEnvVars["AWS_REGION"],
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });

            if (FileService == null || !FileService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "File service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}