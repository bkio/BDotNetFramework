/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using BCloudServiceUtilities;
using BCloudServiceUtilities.LoggingServices;
using BCommonUtilities;

namespace BServiceUtilities
{
    /// <summary>
    /// 
    /// <para>Required Environment variables:</para>
    /// 
    /// AWS_ACCESS_KEY, AWS_SECRET_KEY, AWS_REGION environment variables are needed.
    /// 
    /// NOTE: For the application; access/secret key or credentials should have access to logging service
    /// 
    /// <para>Parameters:</para>
    /// <para>1) PROGRAM_ID:                                Program Unique ID</para>
    /// <para>2) PORT:                                      Port of the http server</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        private BServiceInitializer() { }

        //Private: Access, secret keys, project ID etc.
        private Dictionary<string, string> CloudProviderEnvVars;

        /// <summary>
        /// <para>Initialized Logging Service</para>
        /// </summary>
        public IBLoggingServiceInterface LoggingService { get; private set; } = null;

        /// <summary>
        /// <para>HTTP Server Port</para>
        /// </summary>
        public int ServerPort { get; private set; }

        /// <summary>
        /// <para>Program Unique ID</para>
        /// </summary>
        public string ProgramID { get; private set; }

        /// <summary>
        /// <para>Parsed environment variables which are required for the application</para>
        /// </summary>
        public Dictionary<string, string> RequiredEnvironmentVariables { get { return _RequiredEnvironmentVariables; } }
        private Dictionary<string, string> _RequiredEnvironmentVariables = null;

        /// <summary>
        /// <para>Logs created before logging service is initialized will be passed to this action.</para>
        /// </summary>
        public Action<string> PreLoggingServiceLogger = null;

        public static bool Initialize(
            out BServiceInitializer _Result,
            Action<string> _PreLoggingServiceLogger = null,
            string[] _RequiredExtraEnvVars = null)
        {
            var Instance = new BServiceInitializer();
            _Result = null;

            Instance.PreLoggingServiceLogger = _PreLoggingServiceLogger;

            var RequiredEnvVarKeys = new List<string>()
            {
                "PORT",
                "PROGRAM_ID"
            };
            if (_RequiredExtraEnvVars != null)
            {
                RequiredEnvVarKeys.AddRange(_RequiredExtraEnvVars);
            }

            /*
            * Getting environment variables
            */
            if (!BUtility.GetEnvironmentVariables(out Instance._RequiredEnvironmentVariables,
                RequiredEnvVarKeys.ToArray(),
                _PreLoggingServiceLogger)) return false;

            //Cloud provider setup
            if (!BUtility.GetEnvironmentVariables(out Instance.CloudProviderEnvVars,
                new string[]
                {
                    "AWS_ACCESS_KEY",
                    "AWS_SECRET_KEY",
                    "AWS_REGION"
                },
                _PreLoggingServiceLogger)) return false;

            /*
            * Logging service initialization
            */
            Instance.LoggingService = new BLoggingServiceAWS(Instance.CloudProviderEnvVars["AWS_ACCESS_KEY"], Instance.CloudProviderEnvVars["AWS_SECRET_KEY"], Instance.CloudProviderEnvVars["AWS_REGION"], _PreLoggingServiceLogger);
            if (Instance.LoggingService == null || !Instance.LoggingService.HasInitializationSucceed())
            {
                _PreLoggingServiceLogger?.Invoke("Logging service initialization has failed.");
                return false;
            }

            Instance.ProgramID = Instance.RequiredEnvironmentVariables["PROGRAM_ID"];

            /*
            * Parsing http server port
            */
            if (!int.TryParse(Instance.RequiredEnvironmentVariables["PORT"], out int _ServPort))
            {
                Instance.LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, "Given http server port is invalid."), Instance.ProgramID, "Initialization");
                return false;
            }
            Instance.ServerPort = _ServPort;

            _Result = Instance;
            return true;
        }
    }
}