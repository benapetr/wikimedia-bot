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
        public static List<char> Separator = new List<char> { ' ', ',', (char)3 , '(', ')', '{', '}', (char)2, '<', '>' };
        public struct Job {
                public DateTime time;
                public string HTML;
                public string message;
                public config.channel ch;
        }

        public static List<Job> jobs = new List<Job>();
        public static bool Locked;
        public static Thread WorkerTh;

        public static void CheckLock()
        {
            while (Locked)
            {
                Thread.Sleep(100);
            }
        }

        public static int WriteData()
        {
            if (jobs.Count > 0)
            {
                List<Job> line = new List<Job>();
                List<Job> tr = new List<Job>();
                CheckLock();
                Locked = true;
                lock (jobs)
                {
                    line.AddRange(jobs);
                    jobs.Clear();
                }
                Locked = false;
                // clean all logs that can't be written to disk
                foreach (Job curr in line)
                {
                    if (curr.ch.logs_no_write_data)
                    {
                        lock (jobs)
                        {
                            Locked = true;
                            jobs.Add(curr);
                            tr.Add(curr);
                            Locked = false;
                        }
                    }
                }
                // remove them from queue
                foreach (Job curr in tr)
                {
                    line.Remove(curr);
                }
                // write to disk
                foreach (Job curr in line)
                {
                    writeLog(curr.message, curr.HTML, curr.ch, curr.time);
                }
            }
            return 2;
        }

        public static void ProcessJobs()
        {
            while (true)
            {
                try
                {
                    WriteData();
                    Thread.Sleep(20000);
                }
                catch (ThreadAbortException)
                {
                    WriteData();
                    break;
                }
                catch (Exception fail)
                {
                    Locked = false;
                    core.handleException(fail);
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

        private static void writeLog(string message, string html, config.channel channel, System.DateTime _datetime)
        {
            try
            {
                System.IO.File.AppendAllText(config.path_txt + channel.Log + _datetime.Year + timedateToString(_datetime.Month) + timedateToString(_datetime.Day) + ".txt", message);
                System.IO.File.AppendAllText(config.path_htm + channel.Log + _datetime.Year + timedateToString(_datetime.Month) + timedateToString(_datetime.Day) + ".htm", html);
            }
            catch (Exception er)
            {
                // nothing
                Console.WriteLine(er.Message);
            }
        }

        public static int positionSeparator(string data)
        {
            int pi = -1;
            int temp;
            foreach (char separator in Separator)
            {
                if (data.Contains(separator.ToString()))
                {
                    temp = data.IndexOf(separator.ToString());
                    if (pi == -1 || pi > temp)
                    {
                        pi = temp;
                    }
                }
            }
            if (pi == -1)
            {
                return 0;
            }
            return pi;
        }

        public static bool includesSeparator(string text)
        {
            foreach (char separator in Separator)
            {
                if (text.Contains(separator.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool matchesSeparator(string text, string data)
        {
            return false;
        }

        public static void updateHttp(ref string html)
        {
            int curr = 0;
            if (html.Contains("https://") || html.Contains("http://"))
            {
                string URL;
                string temp = html;
                string original = html;
                try
                {
                    while (temp.Length > 6)
                    {
                        if (curr > 10000)
                        {
                            Program.Log("Maximal cpu on updateHttp(" + html + ") aborted call");
                            html = original;
                            return;
                        }
                        if (temp.StartsWith("http://") || temp.StartsWith("https://"))
                        {
                            URL = temp;
                            int position = temp.Length;
                            if (includesSeparator(temp))
                            {
                                URL = temp.Substring(0, positionSeparator(temp));
                            }
                            html = html.Insert(curr + URL.Length, "</a>");
                            html = html.Insert(curr, "<a target=\"_new\" href=\"" + URL + "\">");
                            curr = curr + (URL.Length * 2) + "</a><a target=\"_new\" href=\"\">".Length;
                            temp = temp.Substring(URL.Length);
                        }
                        else
                        {
                            curr++;
                            temp = temp.Substring(1);
                        }
                    }
                }
                catch (Exception er)
                {
                    core.handleException(er);
                }
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
                if (channel.Logged)
                {
                    lock (jobs)
                    {
                        Locked = true;
                        string log;
                        string URL = Statistics.Host2Name(host);
                        string srcs = "";
                        string messagehtml = System.Web.HttpUtility.HtmlEncode(message);
                        updateHttp(ref messagehtml);
                        if (!noac)
                        {
                            if (URL != "")
                            {
                                srcs = "<font class=\"date\"><b>" + timedateToString(DateTime.Now.Hour) + ":" +
                                    timedateToString(DateTime.Now.Minute) + ":" +
                                    timedateToString(DateTime.Now.Second) + "</b></font><font>* <a target=\"_blank\" href=\"" + URL + "\">" + user +
                                    "</a> " + messagehtml + "</font><br>\n";
                            }
                            else
                            {
                                srcs = "<font class=\"date\"><b>" + timedateToString(DateTime.Now.Hour) + ":" +
                                    timedateToString(DateTime.Now.Minute) + ":" +
                                    timedateToString(DateTime.Now.Second) + "</b></font><font>* " + user + " " +
                                    messagehtml + "</font><br>\n";
                            }
                            log = "[" + timedateToString(DateTime.Now.Hour) + ":" +
                                timedateToString(DateTime.Now.Minute) + ":" +
                                timedateToString(DateTime.Now.Second) + "] * " +
                                user + " " + message + "\n";
                        }
                        else
                        {
                            if (URL != "")
                            {
                                srcs = "<font class=\"date\"><b>" + timedateToString(DateTime.Now.Hour) + ":" +
                                        timedateToString(DateTime.Now.Minute) + ":" +
                                        timedateToString(DateTime.Now.Second) + "</b></font><font class=\"nick\"><b> &lt;<a target=\"_blank\" href=\"" + URL +
                                        "\">" + user + "</a>&gt; </b></font><font>" + messagehtml + "</font><br>\n";
                            }
                            else
                            {
                                srcs = "<font class=\"date\"><b>" + timedateToString(DateTime.Now.Hour) + ":" +
                                        timedateToString(DateTime.Now.Minute) + ":" +
                                        timedateToString(DateTime.Now.Second) + "</b></font><font class=\"nick\"><b> &lt;" + user + "&gt; </b></font><font>" +
                                        messagehtml + "</font><br>\n";
                            }

                            log = "[" + timedateToString(DateTime.Now.Hour) + ":"
                                + timedateToString(DateTime.Now.Minute) + ":" +
                                timedateToString(DateTime.Now.Second) + "] " + "<" +
                                user + ">\t " + message + "\n";
                        }
                        Job line = new Job();
                        line.ch = channel;
                        line.time = DateTime.Now;
                        line.HTML = srcs;
                        line.message = log;
                        jobs.Add(line);
                        Locked = false;
                    }
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
        private static string timedateToString(int number)
        {
            if (number <= 9 && number >= 0)
            {
                return "0" + number.ToString();
            }
            return number.ToString();
        }
    }
}
