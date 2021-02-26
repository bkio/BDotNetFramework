/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.FileServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AZ_STORAGE_SERVICE, AZ_STORAGE_ACCOUNT AZ_STORAGE_ACCOUNT_KEY, AZ_STORAGE_RESOURCE_GROUP, AZ_STORAGE_MANAGEMENT_APP_ID, AZ_STORAGE_MANAGEMENT_SECRET, AZ_SUBSCRIPTION_ID, AZ_TENANT_ID, AZ_STORAGE_LOCATION  must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        public IBFileServiceInterface FileService = null;

        public bool WithFileService()
        {
            /*
            * File service initialization
            */
            FileService = new BFileServiceAZ(RequiredEnvironmentVariables["AZ_STORAGE_SERVICE"],
                    RequiredEnvironmentVariables["AZ_STORAGE_ACCOUNT"],
                    RequiredEnvironmentVariables["AZ_STORAGE_ACCOUNT_KEY"],
                    RequiredEnvironmentVariables["AZ_STORAGE_RESOURCE_GROUP"],
                    RequiredEnvironmentVariables["AZ_STORAGE_MANAGEMENT_APP_ID"],
                    RequiredEnvironmentVariables["AZ_STORAGE_MANAGEMENT_SECRET"],
                    RequiredEnvironmentVariables["AZ_SUBSCRIPTION_ID"],
                    RequiredEnvironmentVariables["AZ_TENANT_ID"],
                    RequiredEnvironmentVariables["AZ_STORAGE_LOCATION"],
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
