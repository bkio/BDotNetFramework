﻿/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.VMServices;

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
            VMService = new BVMServiceAZ(
                RequiredEnvironmentVariables["AZ_CLIENT_ID"],
                RequiredEnvironmentVariables["AZ_CLIENT_SECRET"],
                RequiredEnvironmentVariables["AZ_TENANT_ID"],
                RequiredEnvironmentVariables["AZ_RESOURCE_GROUP_NAME"],
                ProgramID,
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