/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.IO;
using System.Threading;

namespace ServiceUtilities.All
{
    public class ServicesDebugOnlyUtilities
    {
        public static bool CalledFromMain()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Console.CancelKeyPress += OnCancelKeyPress;

            bool bPrinted = false;
            while (!File.Exists("interactive_debug_env_vars.json"))
            {
                if (!bPrinted)
                {
                    bPrinted = true;
                    Console.WriteLine("Waiting for MicroserviceLocalRunner to complete initialization. Please wait...");
                }
                Thread.Sleep(1000);
            }

            var Parsed = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("interactive_debug_env_vars.json"));
            foreach (var Current in Parsed)
            {
                Environment.SetEnvironmentVariable(Current.Key, (string)Current.Value);
            }

            if (bPrinted)
            {
                Console.WriteLine("MicroserviceLocalRunner has completed initialization.");
            }

            DeleteDebugEnvironmentVariablesFile();

            return true;
        }

        public static void OnProcessExit(object _Sender, EventArgs _EventArgs)
        {
            lock (OnProcessExitCalled)
            {
                if (OnProcessExitCalled.Get()) return;
                OnProcessExitCalled.Set(true);

                DeleteDebugEnvironmentVariablesFile();
            }
        }

        public static void DeleteDebugEnvironmentVariablesFile()
        {
            try
            {
                File.Delete("interactive_debug_env_vars.json");
            }
            catch (Exception) { }
        }
        public static void OnCancelKeyPress(object _Sender, ConsoleCancelEventArgs _EventArgs)
        {
            OnProcessExit(_Sender, _EventArgs);
            _EventArgs.Cancel = true;
        }
        private static readonly Atomic<bool> OnProcessExitCalled = new Atomic<bool>(false);
        public class Atomic<T>
        {
            private T Value;
            private readonly object Lock = new object();

            private Atomic() { }
            public Atomic(T _bInitialValue)
            {
                Value = _bInitialValue;
            }

            public T Get()
            {
                return Value;
            }
            public void Set(T _Value)
            {
                lock (Lock)
                {
                    Value = _Value;
                }
            }
        }
    }
}