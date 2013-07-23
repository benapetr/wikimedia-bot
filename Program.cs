//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace wmib
{
    internal class Program
    {
        public static bool Log(string msg, bool warn = false)
        {
            Logging.Write(msg, warn);
            return true;
        }

        public static bool WriteNow(string msg, bool warn = false)
        {
            Logging.Display(DateTime.Now, msg, warn);
            return true;
        }

        public static bool Temp(string file)
        {
            string path = System.IO.Path.GetTempFileName();
            System.IO.File.Copy(file, path, true);
            if (System.IO.File.Exists(path))
            {
                Log("Unfinished transaction from " + file + " was stored as " + path);
                return true;
            }
            return false;
        }

        protected static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            WriteNow("SIGINT");
            WriteNow("Shutting down");
            try
            {
                core.Kill();
            }
            catch (Exception)
            {
                core.irc.Disconnect();
                core._Status = core.Status.ShuttingDown;
            }
            WriteNow("Terminated");
        }

        private static void processVerbosity(string[] gs)
        {
            foreach (string item in gs)
            {
                if (item == "--nocolors")
                {
                    config.Colors = false;
                    continue;
                }
                if (item == "-h" || item == "--help")
                {
                    Console.WriteLine("This is a wikimedia bot binary\n\n" +
                        "Parameters:\n" +
                        "    --nocolors: Disable colors in system logs\n" +
                        "    -h [--help]: Display help\n" +
                        "    -v: Increases verbosity\n\n" +
                        "This software is open source, licensed under GPLv3");
                    Environment.Exit(0);
                }
                if (item.StartsWith("-v"))
                {
                    foreach (char x in item)
                    {
                        if (x == 'v')
                        {
                            config.SelectedVerbosity++;
                        }
                    }
                }
            }
            if (config.SelectedVerbosity >= 1)
            {
                core.DebugLog("System verbosity: " + config.SelectedVerbosity.ToString());
            }
        }

        private static void Main(string[] args)
        {
            try
            {
                Thread logger = new Thread(Logging.Exec);
                core.domain = AppDomain.CurrentDomain;
                WriteNow(config.version);
                WriteNow("Loading...");
                config.UpTime = DateTime.Now;
                processVerbosity(args);
                logger.Start();
                Console.CancelKeyPress += myHandler;
                messages.LoadLD();
                if (config.Load() != 0)
                {
                    WriteNow("Error while loading the config file, exiting", true);
                    return;
                }
                Terminal.Init();
                core.Help.CreateHelp();
                core.WriterThread = new System.Threading.Thread(StorageWriter.Core);
                core.WriterThread.Start();
                Log("Loading modules");
                core.SearchMods();
                IRCTrust.Global();
                Log("Connecting");
                core.Connect();
            }
            catch (Exception fatal)
            {
                WriteNow("ERROR: bot crashed, bellow is debugging information");
                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine("Description: " + fatal.Message);
                Console.WriteLine("Stack trace: " + fatal.StackTrace);
                Environment.Exit(-2);
            }
        }
    }
}
