/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using BCloudServiceUtilities;
using BCommonUtilities;

namespace BCloudServiceUtilitiesTest.Tests
{
    public class BLoggingServiceTest
    {
        private readonly IBLoggingServiceInterface SelectedLoggingService;

        private readonly Action<string> PrintAction;

        public BLoggingServiceTest(IBLoggingServiceInterface _LoggingService, Action<string> _PrintAction)
        {
            SelectedLoggingService = _LoggingService;
            PrintAction = _PrintAction;
        }

        public bool Start()
        {
            if (!SelectedLoggingService.WriteLogs(new List<BLoggingParametersStruct>()
            {
                new BLoggingParametersStruct(EBLoggingServiceLogType.Debug, "This is a test debug message - 1"),
                new BLoggingParametersStruct(EBLoggingServiceLogType.Info, "This is a test info message - 1"),
                new BLoggingParametersStruct(EBLoggingServiceLogType.Warning, "This is a test warning message - 1"),
                new BLoggingParametersStruct(EBLoggingServiceLogType.Error, "This is a test error message - 1"),
                new BLoggingParametersStruct(EBLoggingServiceLogType.Critical, "This is a test critical message - 1")
            },
            "BTestGroup",
            "BTestStream",
            false,
            PrintAction))
            {
                return false;
            }

            if (!SelectedLoggingService.WriteLogs(new List<BLoggingParametersStruct>()
            {
                new BLoggingParametersStruct(EBLoggingServiceLogType.Debug, "This is a test debug message - 2"),
                new BLoggingParametersStruct(EBLoggingServiceLogType.Info, "This is a test info message - 2"),
                new BLoggingParametersStruct(EBLoggingServiceLogType.Warning, "This is a test warning message - 2"),
                new BLoggingParametersStruct(EBLoggingServiceLogType.Error, "This is a test error message - 2"),
                new BLoggingParametersStruct(EBLoggingServiceLogType.Critical, "This is a test critical message - 2")
            },
            "BTestGroup",
            "BTestStream",
            false,
            PrintAction))
            {
                return false;
            }
            return true;
        }
    }
}