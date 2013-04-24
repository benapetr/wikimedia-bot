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

namespace wmib
{
    internal class Program
    {
        public static bool Log(string msg, bool warn = false)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (warn)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("LOG (WARNING)");
            }
            else
            {
                Console.Write("LOG ");
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[" + DateTime.Now.ToString() + "]");
            Console.ResetColor();
            Console.WriteLine(": " + msg);
            return false;
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
            Log("SIGINT");
            Log("Shutting down");
            try
            {
                core.Kill();
            }
            catch (Exception)
            {
                core.irc.Disconnect();
                core.exit = true;
            }
            Log("Terminated");
        }

        private static void Main(string[] args)
        {
            core.domain = AppDomain.CurrentDomain;
            Log(config.version);
            Log("Loading...");
            config.UpTime = System.DateTime.Now;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
            messages.LoadLD();
            if ( config.Load() != 0)
			{
                Log("Error while loading the config file, exiting", true);
				return;
			}
            core.Help.CreateHelp();
            core.WriterThread = new System.Threading.Thread(StorageWriter.Core);
            core.WriterThread.Start();
            Log("Loading modules");
            core.SearchMods();
            IRCTrust.Global();
            Log("Connecting");
            core.Connect();
        }
    }
}
