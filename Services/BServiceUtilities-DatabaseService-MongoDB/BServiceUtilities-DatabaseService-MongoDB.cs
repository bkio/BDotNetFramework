/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.DatabaseServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>MONGO_DB_CONNECTION_STRING, MONGO_DB_DATABASE must be provided and valid.</para>
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
            DatabaseService = new BDatabaseServiceMongoDB(RequiredEnvironmentVariables["MONGODB_CLIENT_CONFIG"], RequiredEnvironmentVariables["MONGODB_PASSWORD"], RequiredEnvironmentVariables["MONGODB_DATABASE"],
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