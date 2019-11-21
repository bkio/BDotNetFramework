/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.SMSServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Additional Required Environment variables:</para>
    /// 
    /// <para>SMS_SERVICE_PROVIDER</para>
    /// <para>SMS_SERVICE_PROVIDER can only be TWILIO for now</para>
    /// 
    /// <para>If SMS_SERVICE_PROVIDER is TWILIO;</para>
    /// <para>TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, TWILIO_FROM_PHONE_NO must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        /// <summary>
        /// <para>Initialized SMS Service</para>
        /// </summary>
        public IBSMSServiceInterface SMSService = null;

        public bool WithSMSService()
        {
            if (!RequiredEnvironmentVariables.ContainsKey("SMS_SERVICE_PROVIDER"))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "SMS_SERVICE_PROVIDER environment variable is missing."), ProgramID, "Initialization");
                return false;
            }

            var SMSServiceProvider = RequiredEnvironmentVariables["SMS_SERVICE_PROVIDER"];

            /*
            * SMS service initialization
            */
            if (SMSServiceProvider == "TWILIO")
            {
                if (!RequiredEnvironmentVariables.ContainsKey("TWILIO_ACCOUNT_SID") ||
                    !RequiredEnvironmentVariables.ContainsKey("TWILIO_AUTH_TOKEN") ||
                    !RequiredEnvironmentVariables.ContainsKey("TWILIO_FROM_PHONE_NO"))
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, TWILIO_FROM_PHONE_NO parameters must be provided and valid."), ProgramID, "Initialization");
                    return false;
                }

                SMSService = new BSMSServiceTwilio(
                    RequiredEnvVars["TWILIO_ACCOUNT_SID"],
                    RequiredEnvVars["TWILIO_AUTH_TOKEN"],
                    RequiredEnvVars["TWILIO_FROM_PHONE_NO"],
                    (string Message) =>
                    {
                        LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                    });
            }

            if (SMSService == null || !SMSService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "SMS service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}
