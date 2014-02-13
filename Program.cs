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
using System.IO;
using System.Threading;

namespace wmib
{
    internal class Program
    {
		/// <summary>
		/// Log the specified message
		/// </summary>
		/// <param name='msg'>
		/// Message that you want to log.
		/// </param>
		/// <param name='warn'>
		/// If this is true the message will be classified as a warning.
		/// </param>
		[Obsolete]
        public static bool Log(string msg, bool warn = false)
        {
            Logging.Write(msg, warn);
            return true;
        }

		/// <summary>
		/// Writes the message immediately to console with no thread sync
		/// </summary>
		/// <returns>
		/// The now.
		/// </returns>
		/// <param name='msg'>
		/// Message that you want to log.
		/// </param>
		/// <param name='warn'>
		/// If this is true the message will be classified as a warning.
		/// </param>
        [Obsolete]
		public static bool WriteNow(string msg, bool warn = false)
        {
            Logging.Display(DateTime.Now, msg, warn);
            return true;
        }

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
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name='args'>
		/// The command-line arguments.
		/// </param>
        private static void Main(string[] args)
        {
            try
            {
                Syslog.WriteNow("Loading convertor...");
				if (File.Exists("configuration/channels.conf"))
				{
					Syslog.WriteNow("Refusing to run convertor, because config file already exist");
					return;
				}
                if (config.Load() != 0)
                {
                    Syslog.WriteNow("Error while loading the config file, exiting", true);
                    Environment.Exit(-2);
                    return;
                }
				config.Save();
				Syslog.WriteNow("Conversion finished");
				return;
            }
            catch (Exception fatal)
            {
                Syslog.WriteNow("ERROR: bot crashed, bellow is debugging information");
                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine("Description: " + fatal.Message);
                Console.WriteLine("Stack trace: " + fatal.StackTrace);
                Environment.Exit(-2);
            }
        }
    }
}
