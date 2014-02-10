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

namespace wmib
{
    /// <summary>
    /// StorageWriter
    /// </summary>
    public class StorageWriter
    {
        /// <summary>
        /// List of all data to write
        /// </summary>
        private static readonly List<STI> Data = new List<STI>();

        /// <summary>
        /// Whether storage is running
        /// </summary>
        public static bool IsRunning = true;

        private static bool Write(STI item)
        {
            try
            {
                System.IO.File.AppendAllText(item.file, item.line + "\n");
                return true;
            }
            catch (Exception crashed)
            {
                Syslog.Log("Unable to write data into " + item.file + " skipping this", true);
                Console.WriteLine(crashed.ToString());
                return false;
            }
        }

        /// <summary>
        /// Insert a line to storage writer
        /// </summary>
        /// <param name="File"></param>
        /// <param name="Text"></param>
        /// <param name="Delayed"></param>
        public static void InsertLine(string File, string Text, bool Delayed = true)
        {
            try
            {
                lock (Data)
                {
                    Data.Add(new STI(Text, File, Delayed));
                }
            }
            catch (Exception crashed)
            {
                Core.HandleException(crashed);
            }
        }

        private static void WriteData()
        {
            List<STI> jobs = new List<STI>();
            lock (Data)
            {
                jobs.AddRange(Data);
                Data.Clear();
            }
            foreach (STI item in jobs)
            {
                if (item.DelayedWrite)
                {
                    while (!Write(item))
                    {
                        Syslog.Log("Unable to write data, delaying write", true);
                        Thread.Sleep(6000);
                    }
                }
                else
                {
                    Write(item);
                }
            }
        }

        /// <summary>
        /// Thread
        /// </summary>
        public static void Exec()
        {
            try
            {
                Syslog.Log("KERNEL: loaded writer thread");
                while (IsRunning)
                {
                    try
                    {
                        Thread.Sleep(2000);
                        if (Data.Count > 0)
                        {
                            WriteData();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        IsRunning = false;
                        break;
                    }
                    catch (Exception fail)
                    {
                        Core.HandleException(fail);
                    }
                }
                if (Data.Count > 0)
                {
                    Syslog.Log("KERNEL: Writer thread was requested to stop, but there is still some data to write");
                    WriteData();
                    Syslog.Log("KERNEL: No remaining data, stopping writer thread");
                    return;
                }
                else
                {
                    Syslog.Log("KERNEL: No remaining data, stopping writer thread");
                    return;
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
                Syslog.Log("KERNEL: The writer thread was terminated", true);
                return;
            }
        }
    }

    /// <summary>
    /// Storage Item
    /// </summary>
    public class STI
    {
        /// <summary>
        /// Delayed write
        /// </summary>
        public bool DelayedWrite;
        /// <summary>
        /// Line
        /// </summary>
        public string line;
        /// <summary>
        /// File
        /// </summary>
        public string file;

        /// <summary>
        /// Creates a new instance of STI
        /// </summary>
        /// <param name="Line"></param>
        /// <param name="Name"></param>
        /// <param name="delayed"></param>
        public STI(string Line, string Name, bool delayed = true)
        {
            DelayedWrite = delayed;
            file = Name;
            line = Line;
        }
    }
}
