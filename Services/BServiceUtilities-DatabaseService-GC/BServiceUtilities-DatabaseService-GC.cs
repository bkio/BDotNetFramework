/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.DatabaseServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>GOOGLE_CLOUD_PROJECT_ID, GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS) must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        /// <summary>
        /// <para>Initialized Database Service</para>
        /// </summary>
        public IBDatabaseServiceInterface DatabaseService = null;

        public bool WithDatabaseService()
        {
            /*
            * File service initialization
            */
            DatabaseService = new BDatabaseServiceGC(RequiredEnvironmentVariables["GOOGLE_CLOUD_PROJECT_ID"],
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });

            if (DatabaseService == null || !DatabaseService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Database service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}
