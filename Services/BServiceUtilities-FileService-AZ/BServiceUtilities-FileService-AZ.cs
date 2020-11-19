/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License
using System;
using System.Collections.Generic;
using System.Text;
using BCloudServiceUtilities;
using BCloudServiceUtilities.FileServices;

namespace BServiceUtilities_FileService_AZ
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
            FileService = new BFileServiceAZ(RequiredEnvVars["AZ_STORAGE_SERVICE"],
                    RequiredEnvVars["AZ_STORAGE_ACCOUNT"],
                    RequiredEnvVars["AZ_STORAGE_ACCOUNT_KEY"],
                    RequiredEnvVars["AZ_STORAGE_RESOURCE_GROUP"],
                    RequiredEnvVars["AZ_STORAGE_MANAGEMENT_APP_ID"],
                    RequiredEnvVars["AZ_STORAGE_MANAGEMENT_SECRET"],
                    RequiredEnvVars["AZ_SUBSCRIPTION_ID"],
                    RequiredEnvVars["AZ_TENANT_ID"],
                    RequiredEnvVars["AZ_STORAGE_LOCATION"],
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
