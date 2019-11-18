﻿/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.FileServices;

namespace BServiceUtilities
{
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
            if (CloudProvider == "AWS")
                FileService = new BFileServiceAWS(CloudProviderEnvVars["AWS_ACCESS_KEY"], CloudProviderEnvVars["AWS_SECRET_KEY"], CloudProviderEnvVars["AWS_REGION"],
                    (string Message) =>
                    {
                        LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                    });
            else if (CloudProvider == "GC")
                FileService = new BFileServiceGC(RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"],
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
