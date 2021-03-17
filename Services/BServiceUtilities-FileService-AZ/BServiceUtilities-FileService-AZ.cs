/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.FileServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>AZ_STORAGE_SERVICE_URL, AZ_STORAGE_ACCOUNT_NAME, AZ_STORAGE_ACCOUNT_ACCESS_KEY, AZ_RESOURCE_GROUP_NAME, AZ_RESOURCE_GROUP_LOCATION, AZ_CLIENT_ID, AZ_CLIENT_SECRET, AZ_SUBSCRIPTION_ID, AZ_TENANT_ID  must be provided and valid.</para>
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
            FileService = new BFileServiceAZ(RequiredEnvironmentVariables["AZ_STORAGE_SERVICE_URL"],
                    RequiredEnvironmentVariables["AZ_STORAGE_ACCOUNT_NAME"],
                    RequiredEnvironmentVariables["AZ_STORAGE_ACCOUNT_ACCESS_KEY"],
                    RequiredEnvironmentVariables["AZ_RESOURCE_GROUP_NAME"],
                    RequiredEnvironmentVariables["AZ_RESOURCE_GROUP_LOCATION"],
                    RequiredEnvironmentVariables["AZ_CLIENT_ID"],
                    RequiredEnvironmentVariables["AZ_CLIENT_SECRET"],
                    RequiredEnvironmentVariables["AZ_SUBSCRIPTION_ID"],
                    RequiredEnvironmentVariables["AZ_TENANT_ID"],
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
