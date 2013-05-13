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
    public class StorageWriter
    {
        /// <summary>
        /// List of all data to write
        /// </summary>
        private static List<STI> Data = new List<STI>();
        public static bool isRunning = true;

        private static bool Write(STI item)
        {
            try
            {
                System.IO.File.AppendAllText(item.file, item.line + "\n");
                return true;
            }
            catch (Exception crashed)
            {
                core.Log("Unable to write data into " + item.file + " skipping this", true);
                Console.WriteLine(crashed.ToString());
                return false;
            }
        }

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
                core.handleException(crashed);
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
                        core.Log("Unable to write data, delaying write", true);
                        Thread.Sleep(6000);
                    }
                }
                else
                {
                    Write(item);
                }
            }
        }

        public static void Core()
        {
            try
            {
                core.Log("KERNEL: loaded writer thread");
                while (isRunning)
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
                        isRunning = false;
                        break;
                    }
                    catch (Exception fail)
                    {
                        core.handleException(fail);
                    }
                }
                if (Data.Count > 0)
                {
                    core.Log("KERNEL: Writer thread was requested to stop, but there is still some data to write");
                    WriteData();
                    core.Log("No remaining data, stopping writer thread");
                    return;
                }
                else
                {
                    core.Log("No remaining data, stopping writer thread");
                    return;
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
                core.Log("KERNEL: The writer thread was terminated", true);
                return;
            }
        }
    }

    public class STI
    {
        public bool DelayedWrite;
        public string line;
        public string file;
        public STI(string Line, string Name, bool delayed = true)
        {
            DelayedWrite = delayed;
            file = Name;
            line = Line;
        }
    }
}
