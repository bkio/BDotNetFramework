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
    /// <para>1) PROGRAM_ID:                                Program Unique ID</para>
    /// <para>2) PORT:                                      Port of the http server</para>
    /// 
    /// </summary>
    public partial class BServiceInitializer
    {
        private BServiceInitializer() { }

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

        public static bool Initialize(
            out BServiceInitializer _Result,
            string[][] _RequiredExtraEnvVars = null)
        {
            var Instance = new BServiceInitializer();
            _Result = null;

            Instance.LoggingService = new BLoggingServiceBasic();

            var RequiredEnvVarKeys = new List<string[]>()
            {
                new string[] { "PORT" },
                new string[] { "PROGRAM_ID" }
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
                (string Message) =>
                {
                    Instance.LoggingService.WriteLogs(BLoggingServiceMessageUtility.Single(EBLoggingServiceLogType.Critical, Message), ProgramID, "Initialization");
                })) return false;

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