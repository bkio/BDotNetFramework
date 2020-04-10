/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BCommonUtilities
{
    /// <summary>
    /// <para>BTaskWrapper is implemented due to Dispose issue for created task.</para>
    /// <para>MSDN Documentation: "Always call Dispose before you release your last reference to the Task."</para>
    /// </summary>
    public class BTaskWrapper
    {
        private BTaskWrapper()
        {
            DisposerThread = new Thread(DisposerThreadRunnable);
            DisposerThread.Start();
        }
        ~BTaskWrapper()
        {
            bRunning = false;
        }
        private static BTaskWrapper Instance = null;
        private static BTaskWrapper Get()
        {
            if (Instance == null)
            {
                Instance = new BTaskWrapper();
            }
            return Instance;
        }

        private readonly List<Task> CreatedTasks = new List<Task>();
        private readonly object CreatedTasks_Lock = new object();
        private bool bRunning = true;

        private readonly Thread DisposerThread = null;
        private void DisposerThreadRunnable()
        {
            Thread.CurrentThread.IsBackground = true;

            while (bRunning)
            {
                Thread.Sleep(2500);

                lock (Get().CreatedTasks_Lock)
                {
                    for (var i = Get().CreatedTasks.Count - 1; i >= 0; i--)
                    {
                        var CurrentTask = Get().CreatedTasks[i];

                        bool bCheckSucceed = false;
                        try
                        {
                            if (CurrentTask != null)
                            {
                                if (CurrentTask.IsCanceled || CurrentTask.IsCompleted || CurrentTask.IsFaulted)
                                {
                                    CreatedTasks.RemoveAt(i);
                                    CurrentTask.Dispose();
                                }
                                bCheckSucceed = true;
                            }
                        }
                        catch (Exception) { }

                        if (!bCheckSucceed)
                        {
                            CreatedTasks.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public static void Run(Action _Action)
        {
            if (_Action != null)
            {
                lock (Get().CreatedTasks_Lock)
                {
                    Get().CreatedTasks.Add(Task.Run(_Action));
                }
            }
        }
	}
}