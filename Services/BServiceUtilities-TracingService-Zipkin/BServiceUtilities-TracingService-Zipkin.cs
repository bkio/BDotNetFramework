/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using BCloudServiceUtilities;
using BCloudServiceUtilities.TracingServices;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// <para>ZIPKIN_SERVER_IP, ZIPKIN_SERVER_PORT must be provided and valid.</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        /// <summary>
        /// <para>Initialized Tracing Service</para>
        /// </summary>
        public IBTracingServiceInterface TracingService = null;

        public bool WithTracingService()
        {
            /*
            * Tracing service initialization
            */
            if (!RequiredEnvironmentVariables.ContainsKey("ZIPKIN_SERVER_IP") ||
                !RequiredEnvironmentVariables.ContainsKey("ZIPKIN_SERVER_PORT"))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "ZIPKIN_SERVER_IP, ZIPKIN_SERVER_PORT parameters must be provided and valid."), ProgramID, "Initialization");
                return false;
            }

            var LoggingServiceLogger = new BLoggingServiceLoggerZipkin(
            LoggingService,
            PreLoggingServiceLogger,
            ProgramID);
            if (!int.TryParse(RequiredEnvironmentVariables["ZIPKIN_SERVER_PORT"], out int ZipkinServerPort))
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Given zipkin server port is invalid."), ProgramID, "Initialization");
                return false;
            }

            TracingService = new BTracingServiceZipkin(
                LoggingServiceLogger,
                ProgramID,
                RequiredEnvironmentVariables["ZIPKIN_SERVER_IP"],
                ZipkinServerPort,
                (string Message) =>
                {
                    LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                });

            if (TracingService == null || !TracingService.HasInitializationSucceed())
            {
                LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Tracing service initialization has failed."), ProgramID, "Initialization");
                return false;
            }

            return true;
        }
    }
}