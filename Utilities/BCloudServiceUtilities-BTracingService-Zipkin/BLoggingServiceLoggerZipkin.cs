/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace BCloudServiceUtilities
{
    public class BLoggingServiceLoggerZipkin : ILogger, zipkin4net.ILogger
    {
        private readonly IBLoggingServiceInterface SelectedLoggingService;
        private readonly Action<string> BackupLoggingAction;

        private readonly string ProgramUniqueID;

        private bool bRunning = true;
        private readonly Thread TickerThread;
        private readonly ConcurrentQueue<BLoggingParametersStruct> MessageQueue = new ConcurrentQueue<BLoggingParametersStruct>();
        private void TickerThreadRunnable()
        {
            Thread.CurrentThread.IsBackground = true;

            while (bRunning)
            {
                var Logs = new List<BLoggingParametersStruct>();
                while (MessageQueue.TryDequeue(out BLoggingParametersStruct Message))
                {
                    Logs.Add(Message);
                }

                if (!SelectedLoggingService.WriteLogs(Logs, ProgramUniqueID, "Logger", false, BackupLoggingAction))
                {
                    foreach (var Log in Logs)
                    {
                        BackupLoggingAction?.Invoke(Log.LogType.ToString() + ": " + Log.Message);
                    }
                }

                Thread.Sleep(1000);
            }
        }

        public BLoggingServiceLoggerZipkin(IBLoggingServiceInterface _SelectedLoggingService, Action<string> _BackupLoggingAction, string _ProgramUniqueID)
        {
            SelectedLoggingService = _SelectedLoggingService;
            BackupLoggingAction = _BackupLoggingAction;
            ProgramUniqueID = _ProgramUniqueID;

            TickerThread = new Thread(TickerThreadRunnable);
            TickerThread.Start();
        }
        ~BLoggingServiceLoggerZipkin()
        {
            bRunning = false;
        }

        public IDisposable BeginScope<TState>(TState _State)
        {
            return null;
        }

        public bool IsEnabled(LogLevel _LogLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel _LogLevel, EventId _EventId, TState _State, Exception _Exception, Func<TState, Exception, string> _Formatter)
        {
            string Message = null;

            var LogType = EBLoggingServiceLogType.Info;
            switch (_LogLevel)
            {
                case LogLevel.Debug:
                    LogType = EBLoggingServiceLogType.Debug;
                    break;
                case LogLevel.Warning:
                    LogType = EBLoggingServiceLogType.Warning;
                    break;
                case LogLevel.Error:
                    LogType = EBLoggingServiceLogType.Error;
                    break;
                case LogLevel.Critical:
                    LogType = EBLoggingServiceLogType.Critical;
                    break;
                default:
                    break;
            }

            if (_Formatter != null)
            {
                Message = _Formatter(_State, _Exception);
            }
            else if (_Exception != null)
            {
                Message = "Message: " + _Exception.Message + ", Trace: " + _Exception.StackTrace;
            }

            if (Message != null && Message.Length > 0)
            {
                MessageQueue.Enqueue(new BLoggingParametersStruct(LogType, Message));
            }
        }

        public void LogInformation(string _Message)
        {
            if (_Message != null && _Message.Length > 0)
            {
                MessageQueue.Enqueue(new BLoggingParametersStruct(EBLoggingServiceLogType.Info, _Message));
            }
        }

        public void LogWarning(string _Message)
        {
            if (_Message != null && _Message.Length > 0)
            {
                MessageQueue.Enqueue(new BLoggingParametersStruct(EBLoggingServiceLogType.Warning, _Message));
            }
        }

        public void LogError(string _Message)
        {
            if (_Message != null && _Message.Length > 0)
            {
                MessageQueue.Enqueue(new BLoggingParametersStruct(EBLoggingServiceLogType.Error, _Message));
            }
        }
    }
}