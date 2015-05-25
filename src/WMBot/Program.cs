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
using System.Diagnostics;
using System.IO;
using System.Threading;
#if __MonoCS__
using Mono.Unix.Native;
using Mono.Unix;
#endif

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
            string path = Path.GetTempFileName();
            File.Copy(file, path, true);
            if (File.Exists(path))
            {
                Syslog.Log("Unfinished transaction from " + file + " was stored as " + path);
                return true;
            }
            return false;
        }

        /// <summary>
        /// This is used to handle UNIX signals
        /// </summary>
        /// <param name='sender'>Sender</param>
        /// <param name='args'>Arguments</param>
        protected static void SigInt(object sender, ConsoleCancelEventArgs args)
        {
            if (!Core.IsRunning)
            {
                // in case that user hit ctrl + c multiple times, we don't want to
                // call this, once is just enough
                return;
            }
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
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// Processes the terminal parameters
        /// </summary>
        /// <param name='args'>
        /// What user has provided in terminal
        /// </param>
        private static void ParseArgs(string[] args)
        {
            int i = 0;
            List<string> parameters = new List<string>(args);
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
                        "    --nocolors:  Disable colors in system logs\n" +
                        "    -h [--help]: Display help\n" +
                        "    --pid file:  Write a pid to a file\n" +
                        "    --modules:   Try to load all module files and list all modules that are available, separated by comma\n" +
                        "    --traffic:   Enable traffic logs\n" +
                        "    --security:  Will load a security subsystem and serialize all roles and print them to standard output\n" +
                        "                 this can be used to create a custom security configuration if you store the output\n" +
                        "                 to configuration/security.xml and restart the bot\n" +
                        "    -v: Increases verbosity\n\n" +
                        "This software is open source, licensed under GPLv3");
                    Environment.Exit(0);
                }
                if (item == "--modules")
                {
                    ExtensionHandler.DumpMods();
                    Environment.Exit(0);
                }
                if (item == "--security")
                {
                    Syslog.IsQuiet = true;
                    if (Configuration.Load() != 0)
                    {
                        Syslog.IsQuiet = false;
                        Syslog.WriteNow("Error while loading the config file, exiting", true);
                        Environment.Exit(-2);
                    }
                    ExtensionHandler.SearchMods();
                    Security.Init();
                    Console.WriteLine(Security.Dump());
                    Environment.Exit(0);
                }
                if (item == "--pid")
                {
                    if (parameters.Count <= i)
                    {
                        Console.WriteLine("You didn't provide a name for pid file");
                        Environment.Exit(0);
                    }
                    File.WriteAllText(parameters[i], Process.GetCurrentProcess().Id.ToString());
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
                Syslog.DebugLog("System verbosity: " + Configuration.System.SelectedVerbosity);
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
                Configuration.System.Version += " [libirc v. " + libirc.Defs.Version.ToString() + "]";
                Thread logger = new Thread(Logging.Exec) {Name = "Logger"};
                Core.ThreadManager.RegisterThread(logger);
                ParseArgs(args);
                Syslog.WriteNow(Configuration.System.Version);
                Syslog.WriteNow("Loading...");
                logger.Start();
                Console.CancelKeyPress += SigInt;
                messages.LoadLD();
                if (Configuration.Load() != 0)
                {
                    Syslog.WriteNow("Error while loading the config file, exiting", true);
                    Environment.Exit(-2);
                }
                Terminal.Init();
                Core.Help.CreateHelp();
                Core.WriterThread = new Thread(StorageWriter.Exec);
                Core.ThreadManager.RegisterThread(Core.WriterThread);
                Core.WriterThread.Name = "Writer";
                Core.WriterThread.Start();
                if (Core.DatabaseServerIsAvailable)
                {
                    Syslog.Log("Initializing MySQL");
                    Core.MysqlDB = new WMIBMySQL();
                } else
                {
                    Syslog.Log("Mysql is not configured, disabling it");
                }
                if (PostgreSQL.IsWorking)
                {
                    Syslog.Log("Opening connection to PostgreDB");
                    Core.PostgreDB = new PostgreSQL();
                    Core.PostgreDB.Connect();
                } else
                {
                    Syslog.Log("Postgres is not configured, not using");
                }
                // let's use postgre as default
                if (Core.PostgreDB != null)
                {
                    Syslog.Log("Using Postgres as a default SQL provider");
                    Core.DB = Core.PostgreDB;
                }
                else if (Core.MysqlDB != null)
                {
                    Syslog.Log("Using MySQL as a default SQL");
                    Core.DB = Core.MysqlDB;
                }
                // register all commands
                Commands.InitAdminCommands();
                Syslog.Log("Loading modules");
                ExtensionHandler.SearchMods();
                Security.Init();
                Security.Global();
                Syslog.Log("Connecting");
                IRC.Connect();
                #if __MonoCS__
UnixSignal[] signals = 
                {
                    new UnixSignal (Signum.SIGINT),
                    new UnixSignal (Signum.SIGTERM),
                    new UnixSignal (Signum.SIGQUIT),
                    new UnixSignal (Signum.SIGHUP)
                };
#endif
                while(Core.IsRunning)
                {
#if __MonoCS__
                    int index = UnixSignal.WaitAny (signals,-1);
                    Signum signal = signals [index].Signum;
                    switch (signal)
                    {
                        case Signum.SIGINT:
                            SigInt(null, null);
                            goto exit;
                        case Signum.SIGTERM:
                            Syslog.WriteNow("SIGTERM - Shutting down", true);
                            Core.Kill();
                            goto exit;
                    }
#endif
                    Thread.Sleep(200);
                }
#if __MonoCS__
                exit:
#endif
                    // memory cleanup
                    if (Core.DB != null)
                        ((WMIBMySQL)Core.DB).Dispose();
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
