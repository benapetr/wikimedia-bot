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
    public class RegularModule : Module
    {
        public List<char> Separator = new List<char> { ' ', ',', (char)3, '(', ')', '{', '}', (char)2, '<', '>' };
        public struct Job
        {
            public DateTime time;
            public string HTML;
            public string message;
            public config.channel ch;
        }

        public List<Job> jobs = new List<Job>();

        public override void Hook_ACTN(config.channel channel, User invoker, string message)
        {
            chanLog(message, channel, invoker.Nick, invoker.Host, false);
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            chanLog(message, channel, invoker.Nick, invoker.Host);
            if (message == "@logon")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (Module.GetConfig(channel, "Logging.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("ChannelLogged", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("LoggingOn", channel.Language), channel.Name);
                        Module.SetConfig(channel, "Logging.Enabled", true);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@logoff")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!Module.GetConfig(channel, "Logging.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("LogsE1", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        Module.SetConfig(channel, "Logging.Enabled", false);
                        channel.SaveConfig();
                        core.irc._SlowQueue.DeliverMessage(messages.get("NotLogged", channel.Language), channel.Name);
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }
        }

        public int WriteData()
        {
            if (jobs.Count > 0)
            {
                List<Job> line = new List<Job>();
                List<Job> tr = new List<Job>();
                lock (jobs)
                {
                    line.AddRange(jobs);
                    jobs.Clear();
                }
                // clean all logs that can't be written to disk
                foreach (Job curr in line)
                {
                    if (curr.ch.logs_no_write_data)
                    {
                        lock (jobs)
                        {
                            jobs.Add(curr);
                            tr.Add(curr);
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

        public override void Load()
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
                    if (jobs.Count != 0)
                    {
                        core.Log("Logging was requested to stop, but there is still " + jobs.Count.ToString() + " lines, writing now");
                    }
                    WriteData();
                    core.Log("There are no unsaved data, we can disable this module now");
                    break;
                }
                catch (Exception fail)
                {
                    core.handleException(fail);
                }
            }
        }

        /// <summary>
        /// Start work
        /// </summary>
        /// <returns></returns>
        public override bool Construct()
        {
            Name = "LOGS";
            start = true;
            Reload = true;
            Version = "1.0.10";
            return true;
        }

        private void writeLog(string message, string html, config.channel channel, System.DateTime _datetime)
        {
            try
            {
                System.IO.File.AppendAllText(config.path_txt + channel.LogDir + _datetime.Year + timedateToString(_datetime.Month) + timedateToString(_datetime.Day) + ".txt", message);
                System.IO.File.AppendAllText(config.path_htm + channel.LogDir + _datetime.Year + timedateToString(_datetime.Month) + timedateToString(_datetime.Day) + ".htm", html);
            }
            catch (Exception er)
            {
                // nothing
                Console.WriteLine(er.Message);
            }
        }

        public int positionSeparator(string data)
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

        public bool includesSeparator(string text)
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

        public bool matchesSeparator(string text, string data)
        {
            return false;
        }

        public void updateBold(ref string html)
        {
            bool open = false;
            char delimiter = ((char)002);
            int curr = 0;
            if (html.Contains(delimiter.ToString()))
            {
                string temp = html;
                string original = html;
                try
                {
                    while (html.Length > curr)
                    {
                        if (curr > 10000)
                        {
                            core.Log("Maximal cpu on updateBold(" + html + ") aborted call");
                            html = original;
                            return;
                        }
                        if (html[curr] == delimiter)
                        {
                            if (open)
                            {
                                open = false;
                                html.Remove(curr, 1);
                                html.Insert(curr, "</b>");
                                curr = curr + 3;
                                continue;
                            }
                            open = true;
                            html.Remove(curr, 1);
                            html.Insert(curr, "<b>");
                            curr = curr + 2;
                            continue;
                        }
                        curr++;
                    }
                    if (open)
                    {
                        html += "</b>";
                    }
                }
                catch (Exception b)
                {
                    html = original;
                    core.handleException(b);
                }
            }
        }

        public void updateColor(ref string html)
        {

        }

        public void updateHttp(ref string html)
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
                            core.Log("Maximal cpu on updateHttp(" + html + ") aborted call");
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
                    html = original;
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
        public void chanLog(string message, config.channel channel, string user, string host, bool noac = true)
        {
            try
            {
                if (Module.GetConfig(channel, "Logging.Enabled", false))
                {
                    lock (jobs)
                    {
                        string log;
                        string URL = core.Host.Host2Name(host);
                        string srcs = "";
                        string messagehtml = System.Web.HttpUtility.HtmlEncode(message);
                        updateHttp(ref messagehtml);
                        //updateBold(ref messagehtml);
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
                    }
                }
            }
            catch (Exception er)
            {
                // nothing
                Console.WriteLine(er.Message);
            }
        }

        public override void Hook_OnSelf(config.channel channel, User self, string message)
        {
            chanLog(message, channel, config.username, "");
        }

        /// <summary>
        /// Convert the number to format we want to have in log
        /// </summary>
        private string timedateToString(int number)
        {
            if (number <= 9 && number >= 0)
            {
                return "0" + number.ToString();
            }
            return number.ToString();
        }
    }
}
