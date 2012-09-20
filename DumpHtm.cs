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
using System.IO;
using System.Threading;

namespace wmib
{
    public class HtmlDump
    {
        /// <summary>
        /// Channel name
        /// </summary>
        public config.channel Channel;

        /// <summary>
        /// Dump
        /// </summary>
        public string dumpname;

        // This function is called on start of bot
        public static void Start()
        {
            while (true)
            {
                foreach (config.channel chan in config.channels)
                {
                    if (chan.info.changed || chan.Keys.update)
                    {
                        chan.info.changed = false;
                        HtmlDump dump = new HtmlDump(chan);
                        dump.Make();
                        chan.Keys.update = false;
                    }
                }
                Stat();
                Thread.Sleep(320000);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="channel"></param>
        public HtmlDump(config.channel channel)
        {
            dumpname = config.DumpDir + "/" + channel.Name + ".htm";
            if (!System.IO.Directory.Exists(config.DumpDir))
            {
                Program.Log("Creating a directory for dump");
                System.IO.Directory.CreateDirectory(config.DumpDir);
            }
            Channel = channel;
        }

        /// <summary>
        /// Html code
        /// </summary>
        /// <returns></returns>
        private string CreateFooter()
        {
            return "</body></html>\n";
        }

        /// <summary>
        /// Html
        /// </summary>
        /// <returns></returns>
        private static string CreateHeader(string page_name)
        {
            string html = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\n\n\n\n<html><head>";
            if (config.css != "")
            {
                html += "<link rel=\"stylesheet\" href=\"" + config.css + "\" type=\"text/css\"/>";
            }
            html += "<title>" + page_name + "</title>\n\n<meta charset=\"UTF-8\"></head><body>\n";
            return html;
        }

        /// <summary>
        /// Remove html
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Encode(string text)
        {
            text = text.Replace("<", "&lt;");
            text = text.Replace(">", "&gt;");
            return text;
        }

        /// <summary>
        /// Insert another table row
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string AddLine(string name, string value)
        {
            return "<tr><td>" + Encode(name) + "</td><td>" + Encode(value) + "</td></tr>\n";
        }

        /// <summary>
        /// Insert another table row
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string AddLink(string name, string value)
        {
            return "<tr><td>" + Encode(name) + "</td><td><a href=\"#" + Encode(value) + "\">" + Encode(value) + "</a></td></tr>\n";
        }

        /// <summary>
        /// Insert another table row
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string AddKey(string name, string value)
        {
            return "<tr id=\"" + Encode(name) + "\"><td>" + Encode(name) + "</td><td>" + Encode(value) + "</td></tr>\n";
        }

        public static string getSize()
        {
            string[] a = Directory.GetFiles("configuration");
            // 2
            // Calculate total bytes of all files in a loop.
            long b = 0;
            foreach (string name in a)
            {
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }
            return b.ToString();
        }

        /// <summary>
        /// Create stat for bot
        /// </summary>
        public static void Stat()
        {
            try
            {
                string text = CreateHeader("System info");
                text += "<h1>System data</h1><p class=info>List of channels:</p>";
                text += "<table class=\"channels\">\n<tr><th>Channel name</th><th>Options</th></tr>";
                lock (config.channels)
                {
                    foreach (config.channel chan in config.channels)
                    {
                        text = text + "<tr>";
                        text = text + "<td><a href=\"" + System.Web.HttpUtility.UrlEncode(chan.Name) + ".htm\">" + chan.Name + "</a></td><td>infobot: " + chan.Info.ToString() + ", recentchanges: " + chan.Feed.ToString() + ", logs: " + chan.Logged.ToString() + ", suppress: " + chan.suppress.ToString() + ", seen: " + chan.Seen.ToString() + "</td></tr>\n";
                    }
                }


                text += "</table>Uptime: " + core.getUptime() + " Memory usage: " + (System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64 / 1024).ToString() + "kb Database size: " + getSize();
                text += "</body></html>";
                File.WriteAllText(config.DumpDir + "/systemdata.htm", text);
            }
            catch (Exception b)
            {
                core.handleException(b);
            }
        }

        /// <summary>
        /// Generate a dump file
        /// </summary>
        public void Make()
        {
            try
            {
                string text = CreateHeader(Channel.Name);
                text = text + "<h4>Infobot</h4>\n";
                if (Channel.shared != "" && Channel.shared != "local")
                {
                    config.channel temp = core.getChannel(Channel.shared);
                    if (temp != null)
                    {
                        text += "Linked to <a href=" + System.Web.HttpUtility.UrlEncode(temp.Name) + ".htm>" + temp.Name + "</a>\n";
                    }
                    else
                    {
                        text += "Channel is linked to " + Channel.shared + " which isn't in my db, that's weird";
                    }
                }
                else
                {
                    text = text + "\n<table border=1 class=\"infobot\" width=100%>\n<tr><th width=10%>Key</th><th>Value</th></tr>\n";
                    Channel.Keys.locked = true;
                    List<infobot_core.item> list = new List<infobot_core.item>();
                    lock (Channel.Keys.text)
                    {
                        if (Channel.infobot_sorted != false)
                        {
                            list = Channel.Keys.SortedItem();
                        }
                        else
                        {
                            list.AddRange(Channel.Keys.text);
                        }
                    }
                    if (Channel.Keys.text.Count > 0)
                    {
                        foreach (infobot_core.item Key in list)
                        {
                            text += AddKey(Key.key, Key.text);
                        }
                    }
                    text = text + "</table>\n";
                    text = text + "<h4>Aliases</h4>\n<table class=\"infobot\" border=1 width=100%>\n";
                    lock (Channel.Keys.Alias)
                    {
                        foreach (infobot_core.staticalias data in Channel.Keys.Alias)
                        {
                            text += AddLink(data.Name, data.Key);
                        }
                    }
                    text = text + "</table><br>\n";
                    Channel.Keys.locked = false;
                }
                if (Channel.Feed)
                {
                    text += "\n<h4>Recent changes</h4>";
                    text = text + Channel.RC.ToTable();
                }
                if (Channel.statistics_enabled)
                {
                    text += "\n<br>\n<h4>Most active users :)</h4>\n<br>\n\n<table class=\"infobot\" width=100% border=1>";
                    text += "<tr><td>N.</td><th>Nick</th><th>Messages (average / day)</th><th>Number of posted messages</th><th>Active since</th></tr>";
                    int id = 0;
                    int totalms = 0;
                    DateTime startime = DateTime.Now;
                    lock (Channel.info.data)
                    {
                        Channel.info.data.Sort();
                        Channel.info.data.Reverse();
                        foreach (Statistics.list user in Channel.info.data)
                        {
                            id++;
                            totalms += user.messages;
                            if (id > 100)
                            {
                                continue;
                            }
                            if (startime > user.logging_since)
                            {
                                startime = user.logging_since;
                            }
                            System.TimeSpan uptime = System.DateTime.Now - user.logging_since;
                            float average = user.messages;
                            average = ((float)user.messages / (float)(uptime.Days + 1));
                            if (user.URL != "")
                            {
                                text += "<tr><td>" + id.ToString() + ".</td><td><a target=\"_blank\" href=\"" + user.URL + "\">" + user.user + "</a></td><td>" + average.ToString() + "</td><td>" + user.messages.ToString() + "</td><td>" + user.logging_since.ToString() + "</td></tr>";
                            }
                            else
                            {
                                text += "<tr><td>" + id.ToString() + ".</td><td>" + user.user + "</td><td>" + average.ToString() + "</td><td>" + user.messages.ToString() + "</td><td>" + user.logging_since.ToString() + "</td></tr>";
                            }
                            text += "  \n";
                        }
                    }
                    System.TimeSpan uptime_total = System.DateTime.Now - startime;
                    float average2 = totalms;
                    average2 = (float)totalms / (1 + uptime_total.Days);
                    text += "<tr><td>N/A</td><th>Total:</th><th>" + average2.ToString() + "</th><th>" + totalms.ToString() + "</th><td>N/A</td></tr>";
                    text += "  \n";
                    text += "</table>";
                }
                text = text + CreateFooter();
                File.WriteAllText(dumpname, text);
            }
            catch (Exception b)
            {
                Channel.Keys.locked = false;
                core.handleException(b);
            }
        }
    }
}
