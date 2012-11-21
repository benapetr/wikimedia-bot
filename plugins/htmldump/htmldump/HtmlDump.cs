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
    public class RegularModule : Module
    {
        public override bool Construct()
        {
            Name = "Html dump";
            Version = "1.0.8.1";
            Reload = false;
            start = true;
            return true;
        }

        public override bool Hook_OnRegister()
        {
            lock (config.channels)
            {
                foreach (config.channel chan in config.channels)
                {
                    SetConfig(chan, "HTML.Update", true);
                }
            }
            return true;
        }

        // This function is called on start of bot
        public override void Load()
        {
            Thread.Sleep(10000);
            while (true)
            {
                lock (config.channels)
                {
                    foreach (config.channel chan in config.channels)
                    {
                        if (GetConfig(chan, "HTML.Update", true))
                        {
                            HtmlDump dump = new HtmlDump(chan);
                            dump.Make();
                            core.Log("Making dump for " + chan.Name);
                            SetConfig(chan, "HTML.Update", false);
                        }
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
                core.Log("Creating a directory for dump");
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
                Thread.Sleep(2000);
                string text = CreateHeader("System info");
                text += "<h1>System data</h1><p class=info>List of channels:</p>\n";
                text += "<table class=\"channels\">\n<tr><th>Channel name</th><th>Options</th></tr>\n";
                lock (config.channels)
                {
                    foreach (config.channel chan in config.channels)
                    {
                        text = text + "<tr>";
                        text = text + "<td><a href=\"" + System.Web.HttpUtility.UrlEncode(chan.Name) + ".htm\">" + chan.Name + "</a></td><td>";
                        text += "infobot: " + Module.GetConfig(chan, "Infobot.Enabled", true).ToString() + ", recentchanges: " + Module.GetConfig(chan, "RC.Enabled", false).ToString() + ", logs: " + Module.GetConfig(chan, "Logging.Enabled", false).ToString() + ", suppress: " + chan.suppress.ToString() + ", seen: " + Module.GetConfig(chan, "Seen.Enabled", false).ToString() + ", rss: " + Module.GetConfig(chan, "Rss.Enabled", false).ToString() + ", statistics: " + Module.GetConfig(chan, "Statistics.Enabled", false).ToString() + "</td></tr>\n";
                    }
                }


                text += "</table>Uptime: " + core.getUptime() + " Memory usage: " + (System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64 / 1024).ToString() + "kb Database size: " + getSize();

                lock (Module.module)
                {
                    foreach (Module mm in Module.module)
                    {
                        mm.Hook_BeforeSysWeb(ref text);
                    }

                    text += "<br>Core version: " + config.version + "<br><h2>Plugins</h2><table class=\"modules\">";

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
                        text = text + "<tr><td>" + module.Name + " (" + module.Version + ")</td><td>" + status + " (startup date: " + module.Date.ToString() + ")</td></tr>\n";
                    }
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
                Dictionary<string, string> ModuleData = new Dictionary<string, string>();
                lock (Module.module)
                {
                    foreach (Module xx in Module.module)
                    {
                        try
                        {
                            if (xx.working)
                            {
                                string html = xx.Extension_DumpHtml(Channel);
                                if (html != null && html != "")
                                {
                                    ModuleData.Add(xx.Name.ToLower(), xx.Extension_DumpHtml(Channel));
                                }
                            }
                        }
                        catch (Exception fail)
                        {
                            core.Log("Unable to retrieve web data", true);
                            core.handleException(fail);
                        }
                    }
                }
                string text = CreateHeader(Channel.Name);
                if (ModuleData.ContainsKey("infobot core"))
                {
                    if (Module.GetConfig(Channel, "Infobot.Enabled", true))
                    {
                        text += "<h4>Infobot</h4>\n";
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
                            text += ModuleData["infobot core"];
                        }
                    }
                    ModuleData.Remove("infobot core");
                }
                if (ModuleData.ContainsKey("rc"))
                {
                    if (Module.GetConfig(Channel, "RC.Enabled", false))
                    {
                        text += "\n<br><h4>Recent changes</h4>";
                        text += ModuleData["rc"];
                        ModuleData.Remove("rc");
                    }
                }
                if (ModuleData.ContainsKey("statistics") && ModuleData["statistics"] != null)
                {
                    if (Module.GetConfig(Channel, "Statistics.Enabled", false))
                    {
                        text += ModuleData["statistics"];
                        ModuleData.Remove("statistics");
                    }
                }
                if (ModuleData.ContainsKey("feed"))
                {
                    if (Module.GetConfig(Channel, "Rss.Enable", false))
                    {
                        text += ModuleData["feed"];
                        ModuleData.Remove("feed");
                    }
                }
                foreach (KeyValuePair<string, string> item in ModuleData)
                {
                    if (item.Value != null && item.Value != "")
                    {
                        text += item.Value;
                    }
                }
                text += CreateFooter();
                File.WriteAllText(dumpname, text);
            }
            catch (Exception b)
            {
                core.handleException(b);
            }
        }
    }
}
