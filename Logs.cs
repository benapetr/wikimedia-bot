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
using System.Text;

namespace wmib
{
    class Logs
    {
        public struct Job {
                public string message;
                public config.channel ch;
        }

        public static List<Job> jobs = new List<Job>();
        public static bool Locked;
        public static Thread WorkerTh;

        public static void ProcessJobs()
        {
            while (true)
            {
                try
                {
                    if (jobs.Count > 0)
                    {
                        Locked = true;
                        List<Job> line = new List<Job>();
                        line.AddRange(jobs);
                        jobs.Clear();
                        Locked = false;
                        foreach (Job curr in line)
                        {
                            writeLog(curr.message, curr.ch);
                        }
                    }
                    Thread.Sleep(20000);
                }
                catch (Exception fail)
                {
                    Console.WriteLine("Exception" + fail.Message);
                }
            }
        }

        /// <summary>
        /// Start work
        /// </summary>
        /// <returns></returns>
        public static bool Initialise()
        {
            Locked = false;
            WorkerTh = new Thread(ProcessJobs);
            WorkerTh.Start();
            return true;
        }

        public static void writeLog(string message, config.channel channel)
        {
            try
            {
                    System.IO.File.AppendAllText(channel.log + DateTime.Now.Year + timedateToString(DateTime.Now.Month) + timedateToString(DateTime.Now.Day) + ".txt", message);
            }
            catch (Exception er)
            {
                // nothing
                Console.WriteLine(er.Message);
            }
        }

        /// <summary>
        /// Log file
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="channel">Channel</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <param name="noac">Action (if true it's logged as message, if false it's action)</param>
        public static void chanLog(string message, config.channel channel, string user, string host, bool noac = true)
        {
            try
            {
                if (channel.logged)
                {
                    while (Locked)
                    {
                        Thread.Sleep(10);
                    }
                    Locked = true;
                    string log;
                    if (!noac)
                    {
                        log = "[" + timedateToString(DateTime.Now.Hour) + ":" +
                            timedateToString(DateTime.Now.Minute) + ":" +
                            timedateToString(DateTime.Now.Second) + "] * " +
                            user + " " + message + "\n";
                    }
                    else
                    {
                        log = "[" + timedateToString(DateTime.Now.Hour) + ":"
                            + timedateToString(DateTime.Now.Minute) + ":" +
                            timedateToString(DateTime.Now.Second) + "] " + "<" +
                            user + ">\t " + message + "\n";
                    }
                    Job line = new Job();
                    line.ch = channel;
                    line.message = log;
                    jobs.Add(line);
                    Locked = false;
                }
            }
            catch (Exception er)
            {
                // nothing
                Console.WriteLine(er.Message);
                Locked = false;
            }
        }

        /// <summary>
        /// Convert the number to format we want to have in log
        /// </summary>
        public static string timedateToString(int number)
        {
            if (number <= 9 && number >= 0)
            {
                return "0" + number.ToString();
            }
            return number.ToString();
        }
    }
}
