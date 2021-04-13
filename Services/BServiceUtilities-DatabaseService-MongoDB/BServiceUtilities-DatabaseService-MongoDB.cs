/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.DatabaseServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>((MONGODB_CONNECTION_STRING) or (MONGODB_CLIENT_CONFIG, MONGODB_PASSWORD) or (MONGODB_HOST, MONGODB_PORT)) and MONGODB_DATABASE must be provided and valid.</para>
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
            if (!RequiredEnvironmentVariables.ContainsKey("MONGODB_DATABASE"))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "((MONGODB_CONNECTION_STRING) or (MONGODB_CLIENT_CONFIG, MONGODB_PASSWORD) or (MONGODB_HOST, MONGODB_PORT)) and MONGODB_DATABASE must be provided and valid."), ProgramID, "Initialization");
                return false;
            }

            if (RequiredEnvironmentVariables.ContainsKey("MONGODB_CLIENT_CONFIG")
                && RequiredEnvironmentVariables.ContainsKey("MONGODB_PASSWORD"))
            {
                DatabaseService = new BDatabaseServiceMongoDB(RequiredEnvironmentVariables["MONGODB_CLIENT_CONFIG"], RequiredEnvironmentVariables["MONGODB_PASSWORD"], RequiredEnvironmentVariables["MONGODB_DATABASE"],
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });
            }
            else if (RequiredEnvironmentVariables.ContainsKey("MONGODB_CONNECTION_STRING"))
            {
                DatabaseService = new BDatabaseServiceMongoDB(RequiredEnvironmentVariables["MONGODB_CONNECTION_STRING"], RequiredEnvironmentVariables["MONGODB_DATABASE"],
                    (string Message) =>
                    {
                        LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                    });
            }
            else if (RequiredEnvironmentVariables.ContainsKey("MONGODB_HOST")
                && RequiredEnvironmentVariables.ContainsKey("MONGODB_PORT")
                && int.TryParse(RequiredEnvironmentVariables["MONGODB_PORT"], out int MongoDbPort))
            {
                DatabaseService = new BDatabaseServiceMongoDB(RequiredEnvironmentVariables["MONGODB_HOST"], MongoDbPort, RequiredEnvironmentVariables["MONGODB_DATABASE"],
                    (string Message) =>
                    {
                        LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                    });
            }
            else
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "((MONGODB_CONNECTION_STRING) or (MONGODB_CLIENT_CONFIG, MONGODB_PASSWORD) or (MONGODB_HOST, MONGODB_PORT)) and MONGODB_DATABASE must be provided and valid."), ProgramID, "Initialization");
                return false;
            }

            if (DatabaseService == null || !DatabaseService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Database service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}