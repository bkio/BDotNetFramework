/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;

namespace BCloudServiceUtilities
{
    public enum EBLoggingServiceLogType
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public struct BLoggingParametersStruct
    {
        public readonly EBLoggingServiceLogType LogType;
        public readonly string Message;

        public BLoggingParametersStruct(EBLoggingServiceLogType _LogType, string _Message)
        {
            LogType = _LogType;
            Message = _Message;
        }
    }

    public class BLoggingServiceMessageUtility
    {
        public static System.Collections.Generic.List<BLoggingParametersStruct> Single(EBLoggingServiceLogType _LogType, string _Message)
        {
            return new System.Collections.Generic.List<BLoggingParametersStruct>()
            {
                new BLoggingParametersStruct(_LogType, _Message)
            };
        }
    }

    public interface IBLoggingServiceInterface
    {
        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        bool HasInitializationSucceed();

        /// <summary>
        /// 
        /// <para>WriteLogs</para>
        /// 
        /// <para>Writes logs to the logging service</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Messages"/>                      List of messages to be written</para>
        /// <para><paramref name="_LogGroupName"/>                  Name of the log group (Group)</para>
        /// <para><paramref name="_LogStreamName"/>                 Stream name of the logs (Sub-group)</para>
        /// <para><paramref name="_bAsync"/>                        Sends messages asynchronously if this parameter is set to true</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        bool WriteLogs(
            System.Collections.Generic.List<BLoggingParametersStruct> _Messages,
            string _LogGroupName,
            string _LogStreamName,
            bool _bAsync = true,
            Action<string> _ErrorMessageAction = null);
    }
}
