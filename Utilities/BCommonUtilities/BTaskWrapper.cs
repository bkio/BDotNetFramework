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

        private readonly List<Tuple<Task, CancellationTokenSource>> CreatedTasks = new List<Tuple<Task, CancellationTokenSource>>();
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
                                if (CurrentTask.Item1.IsCanceled || CurrentTask.Item1.IsCompleted || CurrentTask.Item1.IsFaulted)
                                {
                                    try
                                    {
                                        CurrentTask.Item2?.Dispose();
                                    }
                                    catch (Exception) { }

                                    CreatedTasks.RemoveAt(i);
                                    CurrentTask.Item1.Dispose();
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

        public static void Run(Action _Action, CancellationTokenSource _CancellationTokenSource = null)
        {
            if (_Action != null)
            {
                lock (Get().CreatedTasks_Lock)
                {
                    if (_CancellationTokenSource != null)
                    {
                        Get().CreatedTasks.Add(new Tuple<Task, CancellationTokenSource>(Task.Run(_Action, _CancellationTokenSource.Token), _CancellationTokenSource));
                    }
                    else
                    {
                        Get().CreatedTasks.Add(new Tuple<Task, CancellationTokenSource>(Task.Run(_Action), null));
                    }
                }
            }
        }
	}
}