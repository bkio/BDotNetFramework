/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;

namespace BCloudServiceUtilities.LoggingServices
{
    public class BLoggingServiceBasic : IBLoggingServiceInterface
    {
        public bool HasInitializationSucceed()
        {
            return true;
        }

        public bool WriteLogs(List<BLoggingParametersStruct> _Messages, string _LogGroupName, string _LogStreamName, bool _bAsync = true, Action<string> _ErrorMessageAction = null)
        {
            foreach (var Message in _Messages)
            {
                Console.WriteLine(Message.LogType.ToString() + ": " + _LogStreamName + " -> " + _LogGroupName + " -> " + Message.Message);
            }
            return true;
        }
    }
}