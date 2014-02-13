//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena
using System;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace wmib
{
    public partial class Core : MarshalByRefObject
    {
        public class ThreadManager
        {
            private static List<Thread> threads = new List<Thread>();
            public static bool Writer = false;
            public static bool Logger = false;
            public static List<Thread> ThreadList
            {
                get
                {
                    lock (threads)
                    {
                        return new List<Thread>(threads);
                    }
                }
            }

            public static void RegisterThread(Thread t)
            {
                lock(threads)
                {
                    if (!threads.Contains(t))
                    {
                        threads.Add(t);
                    }
                }
            }

            public static void UnregisterThread(Thread t)
            {
                lock(threads)
                {
                    if (threads.Contains(t))
                    {
                        threads.Remove(t);
                    }
                }
            }

            public static void KillThread(Thread t)
            {
                Syslog.DebugLog("Killing thread: " + t.Name);
                if (t == Core.KernelThread)
                {
                    Syslog.DebugLog("Refusing to kill kernel thread");
                    return;
                }
                if (t.ThreadState == ThreadState.Running || t.ThreadState == ThreadState.WaitSleepJoin)
                {
                    t.Abort();
                } else
                {
                    Syslog.DebugLog("Refusing to kill thread in status: " + t.ThreadState.ToString());
                }
                UnregisterThread(t);
            }
        }
    }
}

