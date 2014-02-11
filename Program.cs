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
using Mono.Unix.Native;
using Mono.Unix;
using System.Threading;

namespace wmib
{
    internal class Program
    {
        /// <summary>
        /// Copy the selected file to a temporary file name
        /// 
        /// this function is used mostly for restore of corrupted data,
        /// so that the corrupted version of file can be stored in /tmp
        /// for debugging
        /// </summary>
        /// <param name='file'>
        /// File
        /// </param>
        public static bool Temp(string file)
        {
            string path = System.IO.Path.GetTempFileName();
            System.IO.File.Copy(file, path, true);
            if (System.IO.File.Exists(path))
            {
                Syslog.Log("Unfinished transaction from " + file + " was stored as " + path);
                return true;
            }
            return false;
        }

        /// <summary>
        /// This is used to handle UNIX signals
        /// </summary>
        /// <param name='sender'>
        /// Sender.
        /// </param>
        /// <param name='args'>
        /// Arguments.
        /// </param>
        protected static void SigInt(object sender, ConsoleCancelEventArgs args)
        {
            Syslog.WriteNow("SIGINT - Shutting down", true);
            try
            {
                Core.Kill();
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            Syslog.WriteNow("Terminated (emergency)");
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// Processes the terminal parameters
        /// </summary>
        /// <param name='gs'>
        /// Gs.
        /// </param>
        private static void ProcessVerbosity(string[] gs)
        {
            int i = 0;
            List<string> parameters = new List<string>(gs);
            foreach (string item in parameters)
            {
                i++;
                if (item == "--nocolors")
                {
                    Configuration.System.Colors = false;
                    continue;
                }
                if (item == "--traffic" )
                {
                    Configuration.Network.Logging = true;
                }
                if (item == "-h" || item == "--help")
                {
                    Console.WriteLine("This is a wikimedia bot binary\n\n" +
                        "Parameters:\n" +
                        "    --nocolors: Disable colors in system logs\n" +
                        "    -h [--help]: Display help\n" +
                        "    --pid file: Write a pid to a file\n" +
                        "    --traffic: Enable traffic logs\n" +
                        "    -v: Increases verbosity\n\n" +
                        "This software is open source, licensed under GPLv3");
                    Environment.Exit(0);
                }
                if (item == "--pid")
                {
                    if (parameters.Count <= i)
                    {
                        Console.WriteLine("You didn't provide a name for pid file");
                        Environment.Exit(0);
                    }
                    System.IO.File.WriteAllText(parameters[i], System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
                }
                if (item.StartsWith("-v"))
                {
                    foreach (char x in item)
                    {
                        if (x == 'v')
                        {
                            Configuration.System.SelectedVerbosity++;
                        }
                    }
                }
            }
            if (Configuration.System.SelectedVerbosity >= 1)
            {
                Syslog.DebugLog("System verbosity: " + Configuration.System.SelectedVerbosity.ToString());
            }
        }

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name='args'>
        /// The command-line arguments.
        /// </param>
        private static void Main(string[] args)
        {
            try
            {
                Configuration.System.UpTime = DateTime.Now;
				Core.KernelThread = Thread.CurrentThread;
				Core.KernelThread.Name = "Kernel";
                Thread logger = new Thread(Logging.Exec);
                logger.Name = "Logger";
				Core.ThreadManager.RegisterThread(logger);
                ProcessVerbosity(args);
                Syslog.WriteNow(Configuration.System.Version);
                Syslog.WriteNow("Loading...");
                logger.Start();
                Console.CancelKeyPress += SigInt;
                messages.LoadLD();
                if (Configuration.Load() != 0)
                {
                    Syslog.WriteNow("Error while loading the config file, exiting", true);
                    Environment.Exit(-2);
                    return;
                }
                Terminal.Init();
                Core.Help.CreateHelp();
                Core.WriterThread = new System.Threading.Thread(StorageWriter.Exec);
				Core.ThreadManager.RegisterThread(Core.WriterThread);
                Core.WriterThread.Name = "Writer";
                Core.WriterThread.Start();
                if (Core.DatabaseServerIsAvailable)
                {
                    Syslog.Log("Initializing MySQL");
                    Core.DB = new WMIBMySQL();
                }
                Syslog.Log("Loading modules");
                ExtensionHandler.SearchMods();
                Security.Global();
                Syslog.Log("Connecting");
                Core.Connect();
                UnixSignal[] signals = new UnixSignal []
                {
                    new UnixSignal (Signum.SIGINT),
                    new UnixSignal (Signum.SIGTERM),
                    new UnixSignal (Signum.SIGQUIT),
                    new UnixSignal (Signum.SIGHUP),
                };
                while(Core.IsRunning)
                {
                    int index = UnixSignal.WaitAny (signals,-1);
                    Signum signal = signals [index].Signum;
                    switch (signal)
                    {
                        case Signum.SIGINT:
                            SigInt(null, null);
                            return;
                        case Signum.SIGTERM:
                            Syslog.WriteNow("SIGTERM - Shutting down", true);
                            Core.Kill();
                            return;
                    }
                    Thread.Sleep(200);
                }
            }
            catch (Exception fatal)
            {
                Syslog.WriteNow("bot crashed, bellow is debugging information", Syslog.Type.Error);
                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine("Description: " + fatal.Message);
                Console.WriteLine("Stack trace: " + fatal.StackTrace);
                Environment.Exit(-2);
            }
        }
    }
}
