/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.MailServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>SENDGRID_API_KEY, SENDGRID_SENDER_EMAIL, SENDGRID_SENDER_NAME must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        /// <summary>
        /// <para>Initialized Mail Service</para>
        /// </summary>
        public IBMailServiceInterface MailService = null;

        public bool WithMailService()
        {
            /*
            * Mail service initialization
            */
            if (!RequiredEnvironmentVariables.ContainsKey("SENDGRID_API_KEY") ||
                !RequiredEnvironmentVariables.ContainsKey("SENDGRID_SENDER_EMAIL") ||
                !RequiredEnvironmentVariables.ContainsKey("SENDGRID_SENDER_NAME"))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "SENDGRID_API_KEY, SENDGRID_SENDER_EMAIL, SENDGRID_SENDER_NAME parameters must be provided and valid."), ProgramID, "Initialization");
                return false;
            }

            MailService = new BMailServiceSendGrid(
                RequiredEnvVars["SENDGRID_API_KEY"],
                RequiredEnvVars["SENDGRID_SENDER_EMAIL"],
                RequiredEnvVars["SENDGRID_SENDER_NAME"],
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });

            if (MailService == null || !MailService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Mail service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}