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
    public class module_html : Module
    {
        public override bool Construct()
        {
            base.Create("HTML", true);
            return true;
        }
        // This function is called on start of bot
        public override void Load()
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
                HtmlDump.Stat();
                Thread.Sleep(320000);
            }
        }
    }

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
                text += "<h1>System data</h1><p class=info>List of channels:</p>\n";
                text += "<table class=\"channels\">\n<tr><th>Channel name</th><th>Options</th></tr>\n";
                lock (config.channels)
                {
                    foreach (config.channel chan in config.channels)
                    {
                        text = text + "<tr>";
                        text = text + "<td><a href=\"" + System.Web.HttpUtility.UrlEncode(chan.Name) + ".htm\">" + chan.Name + "</a></td><td>infobot: " + chan.Info.ToString() + ", recentchanges: " + chan.Feed.ToString() + ", logs: " + chan.Logged.ToString() + ", suppress: " + chan.suppress.ToString() + ", seen: " + chan.Seen.ToString() +  ", rss: " + chan.EnableRss.ToString() +  ", statistics: " + chan.statistics_enabled.ToString() +"</td></tr>\n";
                    }
                }


                text += "</table>Uptime: " + core.getUptime() + " Memory usage: " + (System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64 / 1024).ToString() + "kb Database size: " + getSize();

                foreach (Module mm in Module.module)
                {
                    mm.Hook_BeforeSysWeb(ref text);
                }

                text += "<br>Core version: " + config.version + "<br><h2>Plugins</h2><br><br><table class=\"modules\">";
                foreach (Module module in Module.module)
                {
                    string status = "Terminated";
                    if (module.working)
                    {
                        status = "OK";
                        if (module.Warning)
                        {
                            status += " - RECOVERING";
                        }
                    }
                    text = text + "<tr><td>" + module.Name + "</td><td>" + status + "</td></tr>\n";
                }
                text += "</table>\n\n</body></html>";
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
                if (Channel.EnableRss)
                { 
                    text += "\n<br>\n<h4>Rss</h4>\n<br>\n\n<table class=\"infobot\" width=100% border=1>";
                    text += "<tr><th>Name</th><th>URL</th><th>Enabled</th></tr>";
                    lock (Channel.Rss.Content)
                    {
                        foreach (Feed.item feed in Channel.Rss.Content)
                        {
                            text += "\n<tr><td>" + feed.name + "</td><td><a href=\"" + feed.URL + "\">" + feed.URL + "</a></td><td>" + (!feed.disabled).ToString() + "</td></tr>";
                        }
                    }
                    text += "</table>\n";
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
