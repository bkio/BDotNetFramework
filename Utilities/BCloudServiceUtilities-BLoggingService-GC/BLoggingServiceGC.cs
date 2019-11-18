/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using BCommonUtilities;
using Google.Api;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Grpc.Auth;

namespace BCloudServiceUtilities.LoggingServices
{
    public class BLoggingServiceGC : IBLoggingServiceInterface
    {
        /// <summary>
        /// <para>Google Logging Service Client that is responsible to serve to this object</para>
        /// </summary>
        private readonly LoggingServiceV2Client LoggingServiceClient;

        /// <summary>
        /// <para>Holds initialization success</para>
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly ServiceAccountCredential Credential;
        private readonly Grpc.Core.Channel Channel;

        private readonly string ProjectID;

        private static readonly MonitoredResource ResourceName = new MonitoredResource
        {
            Type = "project",
        };

        /// <summary>
        /// 
        /// <para>BLoggingServiceGC: Parametered Constructor for Managed Service by Google</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ProjectID"/>                 GC Project ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public BLoggingServiceGC(
            string _ProjectID,
            Action<string> _ErrorMessageAction = null)
        {
            ProjectID = _ProjectID;
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                if (ApplicationCredentials == null)
                {
                    _ErrorMessageAction?.Invoke("BLoggingServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    var Scopes = new List<string>();
                    foreach (var Scope in LoggingServiceV2Client.DefaultScopes)
                    {
                        if (!Scopes.Contains(Scope))
                        {
                            Scopes.Add(Scope);
                        }
                    }

                    using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                    {
                        Credential = GoogleCredential.FromStream(Stream)
                            .CreateScoped(
                            Scopes.ToArray())
                            .UnderlyingCredential as ServiceAccountCredential;
                        Channel = new Grpc.Core.Channel(
                            LoggingServiceV2Client.DefaultEndpoint.ToString(),
                            Credential.ToChannelCredentials());
                        LoggingServiceClient = LoggingServiceV2Client.Create(Channel);
                    }

                    if (Credential != null && Channel != null && LoggingServiceClient != null)
                    {
                        bInitializationSucceed = true;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BLoggingServiceGC->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IBLoggingServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        /// <summary>
        ///
        /// <para>WriteLogs:</para>
        ///
        /// <para>Writes logs to the logging service</para>
        ///
        /// <para>Check <seealso cref="IBLoggingServiceInterface.WriteLogs"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool WriteLogs(
            List<BLoggingParametersStruct> _Messages,
            string _LogGroupName,
            string _LogStreamName,
            bool _bAsync = true,
            Action<string> _ErrorMessageAction = null)
        {
            if (_Messages == null || _Messages.Count == 0) return false;

            if (_bAsync)
            {
                BTaskWrapper.Run(() =>
                {
                    WriteLogs(_Messages, _LogGroupName, _LogStreamName, false, _ErrorMessageAction);
                });
                return true;
            }
            else
            {
                if (!BUtility.CalculateStringMD5(DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds.ToString(), out string Timestamp, _ErrorMessageAction))
                {
                    _ErrorMessageAction?.Invoke("BLoggingServiceGC->WriteLogs: Timestamp generation has failed.");
                    return false;
                }

                _LogGroupName = BUtility.EncodeStringForTagging(_LogGroupName);
                _LogStreamName = BUtility.EncodeStringForTagging(_LogStreamName);

                string StreamIDBase = _LogGroupName + "-" + _LogStreamName + "-" + Timestamp;
                try
                {
                    var LogEntries = new LogEntry[_Messages.Count];

                    int i = 0;
                    foreach (var Message in _Messages)
                    {
                        LogEntries[i] = new LogEntry
                        {
                            LogName = new LogName(ProjectID, StreamIDBase + "-" + (i + 1).ToString()).ToString(),
                            TextPayload = Message.Message
                        };

                        switch (Message.LogType)
                        {
                            case EBLoggingServiceLogType.Debug:
                                LogEntries[i].Severity = LogSeverity.Debug;
                                break;
                            case EBLoggingServiceLogType.Info:
                                LogEntries[i].Severity = LogSeverity.Info;
                                break;
                            case EBLoggingServiceLogType.Warning:
                                LogEntries[i].Severity = LogSeverity.Warning;
                                break;
                            case EBLoggingServiceLogType.Error:
                                LogEntries[i].Severity = LogSeverity.Error;
                                break;
                            case EBLoggingServiceLogType.Critical:
                                LogEntries[i].Severity = LogSeverity.Critical;
                                break;
                        }

                        i++;
                    }

                    LoggingServiceClient.WriteLogEntries(
                        LogNameOneof.From(new LogName(ProjectID, StreamIDBase)),
                        ResourceName,
                        new Dictionary<string, string>()
                        {
                            ["LogGroup"] = _LogGroupName,
                            ["LogStream"] = _LogStreamName
                        },
                        LogEntries);

                    return true;
                }
                catch (Exception e)
                {
                    _ErrorMessageAction?.Invoke("BLoggingServiceGC->WriteLogs: " + e.Message + ", Trace: " + e.StackTrace);
                }
            }
            return false;
        }
    }
}