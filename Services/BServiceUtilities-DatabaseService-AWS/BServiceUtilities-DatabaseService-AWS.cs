/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.DatabaseServices;

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
        /// <para>Initialized Database Service</para>
        /// </summary>
        public IBDatabaseServiceInterface DatabaseService = null;

        public bool WithDatabaseService()
        {
            /*
            * Database service initialization
            */
            DatabaseService = new BDatabaseServiceAWS(RequiredEnvironmentVariables["AWS_ACCESS_KEY"], RequiredEnvironmentVariables["AWS_SECRET_KEY"], RequiredEnvironmentVariables["AWS_REGION"],
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